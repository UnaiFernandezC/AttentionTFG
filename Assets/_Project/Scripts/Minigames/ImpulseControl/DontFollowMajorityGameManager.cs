using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Orquesta "No sigas la mayoría".
///
/// Flujo por ronda:
///   1. GenerateRound()          → RuleManager elige mayoría / minoría
///   2. Generate(container, …)   → StimulusGenerator coloca las flechas
///   3. AcceptInput = true       → jugador elige dirección (teclado o botón)
///   4. HandleResponse(dir)      → evalúa, feedback, actualiza UI
///   5. Breve pausa              → siguiente ronda o final
///
/// Victoria: correctCount >= passCount
/// Puntuación: 60–100 pts por ronda correcta según velocidad de respuesta.
///
/// Cómo ajustar dificultad desde el Inspector:
///   · totalArrows / minorityCount  → cuánto destaca la minoría
///   · responseTime                 → presión de tiempo
///   · totalRounds / passCount      → exigencia para ganar
/// </summary>
public class DontFollowMajorityGameManager : MinigameBase
{
    // ------------------------------------------------------------------ //
    // Inspector
    // ------------------------------------------------------------------ //
    [Header("Rondas")]
    public int   totalRounds  = 10;
    public int   passCount    = 7;    // rondas correctas para ganar

    [Header("Tiempo de respuesta (segundos)")]
    public float responseTime         = 3.5f;
    public float pauseAfterResponse   = 0.70f;

    [Header("Estímulos")]
    public int totalArrows   = 10;
    public int minorityCount = 2;     // flechas de la dirección correcta

    // ------------------------------------------------------------------ //
    // Componentes
    // ------------------------------------------------------------------ //
    DontFollowMajorityRuleManager       _rule;
    DontFollowMajorityStimulusGenerator _gen;
    DontFollowMajorityInputHandler      _input;
    DontFollowMajorityUIController      _ui;

    // ------------------------------------------------------------------ //
    // Estado
    // ------------------------------------------------------------------ //
    int   _round;
    int   _correct;
    int   _score;
    float _elapsed;
    bool  _waitingForNext;

    // ------------------------------------------------------------------ //
    // MinigameBase
    // ------------------------------------------------------------------ //
    protected override string GetIntroDescription()
    {
        return
            "Aparecen " + totalArrows + " flechas: la mayoría apuntan en\n" +
            "una dirección, solo " + minorityCount + " apuntan en otra.\n" +
            "Elige la dirección con MENOS flechas.\n" +
            "¡No sigas el instinto de ir con la mayoría!";
    }

    protected override void OnMinigameStart()
    {
        EnsureEventSystem();

        _rule  = GetComponent<DontFollowMajorityRuleManager>();
        _gen   = GetComponent<DontFollowMajorityStimulusGenerator>();
        _input = GetComponent<DontFollowMajorityInputHandler>();
        _ui    = GetComponent<DontFollowMajorityUIController>();

        // Propaga configuración al generador
        _gen.totalArrows   = totalArrows;
        _gen.minorityCount = minorityCount;

        // Construye UI — los botones del D-pad van por el InputHandler
        // para respetar el flag AcceptInput y evitar doble disparo
        _ui.BuildUI(totalRounds,
                    d => _input.PressDirection(d),
                    () => RestartMinigame(),
                    () => ReturnToGameSelector());

        _input.OnDirectionInput += HandleResponse;

        _round          = 0;
        _correct        = 0;
        _score          = 0;
        _elapsed        = 0f;
        _waitingForNext = false;

        _ui.SetScore(0);
        StartCoroutine(DelayedStart());
    }

    protected override void OnMinigameComplete() { }
    protected override void OnMinigameFailed()   { }

    // ------------------------------------------------------------------ //
    // Update — temporizador de respuesta
    // ------------------------------------------------------------------ //
    void Update()
    {
        if (!IsPlaying || _waitingForNext || !_input.AcceptInput) return;

        _elapsed += Time.deltaTime;
        _ui.SetTimerBar(1f - _elapsed / responseTime);

        if (_elapsed >= responseTime)
        {
            _input.AcceptInput = false;
            HandleTimeout();
        }
    }

    // ------------------------------------------------------------------ //
    // Flujo de rondas
    // ------------------------------------------------------------------ //

    IEnumerator DelayedStart()
    {
        yield return new WaitForSeconds(0.35f);
        NextRound();
    }

    void NextRound()
    {
        if (!IsPlaying) return;

        _round++;
        _elapsed = 0f;
        _ui.HideFeedback();
        _ui.SetTimerBar(1f);

        _rule.GenerateRound();
        _gen.Generate(_ui.StimulusContainer,
                      _rule.MajorityDirection,
                      _rule.CorrectDirection);

        _input.AcceptInput = true;
    }

    void HandleResponse(DFMDirection dir)
    {
        if (!IsPlaying || _waitingForNext) return;

        bool correct = _rule.IsCorrect(dir);
        if (correct)
        {
            int pts = ComputePoints();
            _correct++;
            _score += pts;
        }

        _ui.SetRoundDot(_round - 1, correct);
        _ui.SetScore(_score);
        _ui.ShowFeedback(correct,
                         DontFollowMajorityRuleManager.DirectionName(_rule.CorrectDirection));

        StartCoroutine(AdvanceAfterDelay());
    }

    void HandleTimeout()
    {
        if (_waitingForNext) return;

        _ui.SetRoundDot(_round - 1, false);
        _ui.ShowFeedback(false,
                         DontFollowMajorityRuleManager.DirectionName(_rule.CorrectDirection));

        StartCoroutine(AdvanceAfterDelay());
    }

    IEnumerator AdvanceAfterDelay()
    {
        _waitingForNext = true;
        yield return new WaitForSeconds(pauseAfterResponse);
        _waitingForNext = false;

        _gen.Clear();

        if (_round >= totalRounds)
            EndGame();
        else
            NextRound();
    }

    void EndGame()
    {
        bool won = _correct >= passCount;
        CompleteMinigame(won ? _score : 0);
        _ui.ShowFinalResult(won, _correct, totalRounds, _score);
    }

    // ------------------------------------------------------------------ //
    // Puntuación — más puntos por respuesta rápida
    // ------------------------------------------------------------------ //
    int ComputePoints()
    {
        float ratio = Mathf.Clamp01(1f - _elapsed / responseTime);
        return Mathf.RoundToInt(Mathf.Lerp(60f, 100f, ratio));
    }

    // ------------------------------------------------------------------ //
    // Helpers
    // ------------------------------------------------------------------ //
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
