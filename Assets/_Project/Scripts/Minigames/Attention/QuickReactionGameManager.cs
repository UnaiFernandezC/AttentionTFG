using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// GameManager del minijuego "Reacción rápida".
/// Hereda MinigameBase → intro panel automático.
///
/// Mecánica:
///   • Cada ronda: espera aleatoria → aparece estímulo verde → mide reacción.
///   • Click antes del estímulo → ronda fallada (demasiado pronto).
///   • Sin click antes del límite → ronda fallada (tiempo agotado).
///   • Límites de reacción por ronda (en segundos):
///       Ronda 1 → 3 s
///       Ronda 2 → 2 s
///       Ronda 3 → 1 s
///     (configurable vía el array roundTimeLimits en el Inspector)
///
/// Dificultad en Inspector:
///   Fácil   → waitMin=2.5, waitMax=5.5, rounds=3, roundsToWin=2
///   Medio   → waitMin=1.5, waitMax=4.5, rounds=4, roundsToWin=3
///   Difícil → waitMin=0.8, waitMax=6.0, rounds=5, roundsToWin=4
/// </summary>
public class QuickReactionGameManager : MinigameBase
{
    // ------------------------------------------------------------------ //
    // Inspector
    // ------------------------------------------------------------------ //

    [Header("Rondas")]
    public int rounds      = 3;
    public int roundsToWin = 2;

    [Header("Tiempo límite de reacción por ronda (s)")]
    [Tooltip("Un valor por ronda. Si hay más rondas que valores, el último se repite.")]
    public float[] roundTimeLimits = new float[] { 3f, 2f, 1f };

    [Header("Pausa entre rondas (s)")]
    public float pauseBetweenRounds = 1.4f;

    // ------------------------------------------------------------------ //
    // Componentes
    // ------------------------------------------------------------------ //

    ReactionManager           _reaction;
    QuickReactionInputHandler _input;
    QuickReactionUIController _ui;

    // ------------------------------------------------------------------ //
    // Estado
    // ------------------------------------------------------------------ //

    int   _currentRound;
    int   _correctCount;
    long  _totalMs;
    int   _validCount;
    float _pulseT;
    float _currentTimeLimit;   // límite de la ronda en curso (para la UI)
    bool  _stimulusActive;     // true mientras el estímulo está visible

    // ════════════════════════════════════════════════════════════════════

    protected override string GetIntroDescription() =>
        "Espera a que el círculo se ponga VERDE.\n" +
        "Haz click (o pulsa ESPACIO) lo más rápido posible.\n\n" +
        "• Click ANTES de verde → ronda fallida\n" +
        "• Sin reaccionar a tiempo → ronda fallida\n" +
        "  Ronda 1: 3 s · Ronda 2: 2 s · Ronda 3: 1 s\n\n" +
        "Consigue " + roundsToWin + " de " + rounds + " rondas para ganar.";

    protected override void OnMinigameStart()
    {
        EnsureEventSystem();

        _reaction = GetComponent<ReactionManager>();
        _input    = GetComponent<QuickReactionInputHandler>();
        _ui       = GetComponent<QuickReactionUIController>();

        _currentRound  = 0;
        _correctCount  = 0;
        _totalMs       = 0;
        _validCount    = 0;
        _stimulusActive = false;

        _ui.BuildUI(rounds, () => RestartMinigame(), () => ReturnToGameSelector());

        _reaction.OnStimulusAppeared   += HandleStimulus;
        _reaction.OnReactionRegistered += HandleReaction;
        _input.OnInputDetected         += HandleInput;

        StartCoroutine(RunRound());
    }

    protected override void OnMinigameComplete() { }
    protected override void OnMinigameFailed()   { }

    // ════════════════════════════════════════════════════════════════════
    // Flujo de rondas
    // ════════════════════════════════════════════════════════════════════

    IEnumerator RunRound()
    {
        _stimulusActive = false;
        _ui.SetWaiting();

        yield return new WaitForSeconds(0.35f);

        // Asignar límite de tiempo para esta ronda
        int limitIdx = Mathf.Clamp(_currentRound, 0, roundTimeLimits.Length - 1);
        _currentTimeLimit            = roundTimeLimits[limitIdx];
        _reaction.ReactionTimeLimit  = _currentTimeLimit;

        _input.AcceptInput = true;
        _reaction.StartRound();
    }

    void HandleStimulus()
    {
        _stimulusActive = true;
        _ui.SetStimulus(_currentTimeLimit);
    }

    void HandleInput()
    {
        bool consumed = _reaction.RegisterInput();
        if (!consumed) return;

        _input.AcceptInput  = false;
        _stimulusActive     = false;
    }

    void HandleReaction()
    {
        _stimulusActive    = false;
        _input.AcceptInput = false;

        bool   tooEarly = _reaction.WasTooEarly;
        bool   timeout  = _reaction.WasTimeout;
        long   ms       = _reaction.LastReactionMs;
        string evalMsg  = (tooEarly || timeout) ? "" : ReactionManager.EvaluateTime(ms);

        bool correct = !tooEarly && !timeout;
        if (correct)
        {
            _correctCount++;
            _totalMs   += ms;
            _validCount++;
        }

        _ui.ShowRoundResult(tooEarly, timeout, ms, evalMsg);
        _ui.SetRoundDot(_currentRound, correct);

        _currentRound++;

        int  remaining    = rounds - _currentRound;
        bool canStillWin  = (_correctCount + remaining) >= roundsToWin;
        bool alreadyWon   = _correctCount >= roundsToWin;
        bool allDone      = _currentRound >= rounds;

        if (alreadyWon || allDone || !canStillWin)
            StartCoroutine(EndGame(alreadyWon));
        else
            StartCoroutine(NextRoundDelay());
    }

    IEnumerator NextRoundDelay()
    {
        yield return new WaitForSeconds(pauseBetweenRounds);
        StartCoroutine(RunRound());
    }

    IEnumerator EndGame(bool won)
    {
        yield return new WaitForSeconds(1.0f);

        int score = CalculateScore(won);
        CompleteMinigame(score);

        string avgStr = _validCount > 0
            ? "Tiempo medio: " + (_totalMs / _validCount) + " ms\n"
            : "";
        string sub = avgStr +
                     "Rondas correctas: " + _correctCount + "/" + rounds + "\n" +
                     "+" + score + " puntos";

        _ui.ShowFinalResult(won, sub);
    }

    int CalculateScore(bool won)
    {
        if (!won) return 0;
        int  base_  = 500;
        int  bonus  = _correctCount * 100;
        long avgMs  = _validCount > 0 ? _totalMs / _validCount : 9999;
        int  speed  = Mathf.Max(0, Mathf.RoundToInt((500f - avgMs) * 0.6f));
        return base_ + bonus + speed;
    }

    // ════════════════════════════════════════════════════════════════════
    // Update — pulso visual + actualización del arco de cuenta atrás
    // ════════════════════════════════════════════════════════════════════

    void Update()
    {
        if (!IsPlaying) return;

        _pulseT += Time.deltaTime;
        _ui.PulseGlow(_pulseT);

        // Alimentar el arco radial mientras el estímulo esté activo
        if (_stimulusActive)
            _ui.UpdateCountdown(_reaction.StimulusElapsed, _currentTimeLimit);
    }

    // ════════════════════════════════════════════════════════════════════
    // Helper
    // ════════════════════════════════════════════════════════════════════

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
