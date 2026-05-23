using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Orquesta "Cuenta Atrás Silenciosa".
///
/// Flujo por ronda:
///   READY    → el jugador ve el tiempo objetivo → pulsa ¡EMPIEZA!
///   COUNTING → el temporizador corre oculto     → pulsa ¡YA!
///   RESULT   → se evalúa la diferencia          → pulsa Continuar
///   (repite para cada ronda)
///   FINAL    → resultado global
///
/// Victoria: correctCount >= roundsToWin
/// </summary>
public class SilentCountdownGameManager : MinigameBase
{
    // ------------------------------------------------------------------ //
    // Inspector
    // ------------------------------------------------------------------ //
    [Header("Configuración de rondas")]
    public int totalRounds = 3;
    public int roundsToWin = 2;

    [Header("Tiempos objetivo por ronda (segundos)")]
    public float[] roundTargets = { 3f, 5f, 7f };

    [Header("Márgenes de precisión (segundos)")]
    [Tooltip("Diferencia máxima para calificación PERFECTO (100 pts)")]
    public float perfectMargin = 0.40f;
    [Tooltip("Diferencia máxima para calificación BIEN (60–99 pts). Por encima → FALLO")]
    public float goodMargin    = 1.00f;

    // ------------------------------------------------------------------ //
    // Componentes
    // ------------------------------------------------------------------ //
    SilentCountdownTimerManager   _timer;
    SilentCountdownInputHandler   _input;
    SilentCountdownScoreEvaluator _evaluator;
    SilentCountdownUIController   _ui;

    // ------------------------------------------------------------------ //
    // Estado
    // ------------------------------------------------------------------ //
    enum Phase { Ready, Counting, Result, Final }
    Phase _phase;
    int   _currentRound;   // 0-based
    int   _correctCount;
    int   _totalScore;
    float _targetTime;

    // ------------------------------------------------------------------ //
    // MinigameBase
    // ------------------------------------------------------------------ //
    protected override string GetIntroDescription()
    {
        return
            "Se muestra un tiempo. Memorízalo bien.\n" +
            "Pulsa ¡EMPIEZA! y cuenta mentalmente.\n" +
            "Cuando creas que ha pasado, pulsa ¡YA!\n" +
            "¡Sin mirar el reloj! Solo tú y el tiempo.";
    }

    void ApplyDifficulty()
    {
        var diff = GameManager.Instance != null
            ? GameManager.Instance.CurrentDifficulty
            : DifficultyLevel.Easy;
        switch (diff)
        {
            case DifficultyLevel.Medium:
                totalRounds  = 4;
                roundsToWin  = 3;
                roundTargets = new float[] { 3f, 5f, 7f, 9f };
                break;
            case DifficultyLevel.Hard:
                totalRounds  = 5;
                roundsToWin  = 4;
                roundTargets = new float[] { 3f, 5f, 7f, 9f, 12f };
                break;
        }
    }

    protected override void OnMinigameStart()
    {
        ApplyDifficulty();
        EnsureEventSystem();

        _timer     = GetComponent<SilentCountdownTimerManager>();
        _input     = GetComponent<SilentCountdownInputHandler>();
        _evaluator = GetComponent<SilentCountdownScoreEvaluator>();
        _ui        = GetComponent<SilentCountdownUIController>();

        _ui.BuildUI(totalRounds,
                    OnMainButton,
                    () => RestartMinigame(),
                    () => ReturnToGameSelector());

        // Aplica los márgenes configurados en el Inspector de este componente
        _evaluator.perfectMargin = perfectMargin;
        _evaluator.goodMargin    = goodMargin;

        _input.OnPlayerPressed += OnPlayerPressed;

        _currentRound = 0;
        _correctCount = 0;
        _totalScore   = 0;

        BeginRound();
    }

    protected override void OnMinigameComplete() { }
    protected override void OnMinigameFailed()   { }

    void Update()
    {
        // Permite avanzar desde la pantalla de resultado con Espacio (el ratón usa el botón)
        if (_phase == Phase.Result && Input.GetKeyDown(KeyCode.Space))
        {
            AdvanceRound();
        }
    }

    // ------------------------------------------------------------------ //
    // Flujo de ronda
    // ------------------------------------------------------------------ //

    void BeginRound()
    {
        _targetTime        = GetTargetForRound(_currentRound);
        _timer.Reset();
        _input.AcceptInput = false;
        _phase             = Phase.Ready;
        _ui.ShowReady(_targetTime);
    }

    /// <summary>
    /// Llamado por el botón central unificado (¡EMPIEZA! / ¡YA! / Continuar).
    /// </summary>
    void OnMainButton()
    {
        switch (_phase)
        {
            case Phase.Ready:
                StartCounting();
                break;

            case Phase.Counting:
                // Delega en el InputHandler para que el evento sea coherente
                // con las pulsaciones de teclado/ratón.
                _input.PressButton();
                break;

            case Phase.Result:
                AdvanceRound();
                break;
        }
    }

    /// <summary>Recibe el evento del InputHandler cuando el jugador pulsa.</summary>
    void OnPlayerPressed()
    {
        if (_phase != Phase.Counting) return;
        float actual = _timer.StopCounting();
        ShowResult(actual);
    }

    void StartCounting()
    {
        _phase             = Phase.Counting;
        _input.AcceptInput = true;
        _timer.StartCounting();
        _ui.ShowCounting();
    }

    void ShowResult(float actual)
    {
        _input.AcceptInput = false;
        _phase             = Phase.Result;

        var  result  = _evaluator.Evaluate(_targetTime, actual);
        bool correct = _evaluator.IsCorrect(result);

        if (correct) _correctCount++;
        _totalScore += result.Points;

        _ui.SetRoundDot(_currentRound, correct);
        _ui.SetScore(_totalScore);

        string ratingText;
        Color  ratingColor;

        switch (result.Rating)
        {
            case SilentCountdownScoreEvaluator.Rating.Perfect:
                ratingText  = "¡PERFECTO!";
                ratingColor = new Color(0.22f, 0.86f, 0.54f);
                _ui.Flash(new Color(0.22f, 0.86f, 0.54f, 0.28f));
                break;
            case SilentCountdownScoreEvaluator.Rating.Good:
                ratingText  = "¡BIEN!";
                ratingColor = new Color(0.95f, 0.80f, 0.15f);
                _ui.Flash(new Color(0.95f, 0.80f, 0.15f, 0.18f));
                break;
            default:
                ratingText  = "FALLASTE";
                ratingColor = new Color(0.90f, 0.22f, 0.28f);
                _ui.Flash(new Color(0.90f, 0.22f, 0.28f, 0.18f));
                break;
        }

        _ui.ShowRoundResult(
            _targetTime,
            actual,
            result.Difference,
            result.SignedDiff >= 0f,
            ratingText,
            ratingColor
        );
    }

    void AdvanceRound()
    {
        _currentRound++;

        if (_currentRound >= totalRounds)
            EndGame();
        else
            BeginRound();
    }

    void EndGame()
    {
        _phase = Phase.Final;
        bool won = _correctCount >= roundsToWin;
        CompleteMinigame(won ? _totalScore : 0);
        _ui.ShowFinalResult(won, _correctCount, totalRounds, _totalScore);
    }

    // ------------------------------------------------------------------ //
    // Helpers
    // ------------------------------------------------------------------ //

    float GetTargetForRound(int round)
    {
        if (roundTargets != null && round < roundTargets.Length)
            return roundTargets[round];
        return 3f + round * 2f;   // fallback: 3 s, 5 s, 7 s, …
    }

    static void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() == null)
        {
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }
    }
}
