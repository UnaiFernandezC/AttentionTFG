// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Reaccion rapida (Atencion): pulsa cuando el circulo se pone VERDE.
/// - Pulsar ANTES del verde cuenta como ronda fallada (entrena inhibicion).
/// - En dificil hay rondas trampa AMARILLAS: si el nino aguanta 1 s sin pulsar,
///   cuenta como acierto (control de impulsos).
/// </summary>
public class QuickReactionGameManager : MinigameBase
{

    [Header("Rondas")]
    public int rounds      = 3;
    public int roundsToWin = 2;

    [Header("Tiempo límite de reacción por ronda (s)")]
    [Tooltip("Un valor por ronda. Si hay más rondas que valores, el último se repite.")]
    public float[] roundTimeLimits = new float[] { 3f, 2f, 1f };

    [Header("Pausa entre rondas (s)")]
    public float pauseBetweenRounds = 1.4f;

    ReactionManager           _reaction;
    QuickReactionInputHandler _input;
    QuickReactionUIController _ui;

    int    _currentRound;
    int    _correctCount;
    long   _totalMs;
    int    _validCount;
    float  _pulseT;
    float  _currentTimeLimit;
    bool   _stimulusActive;
    bool[] _trapPlan;
    bool   _isTrap;

    const float TRAP_HOLD_TIME = 1f;

    protected override string GetIntroDescription()
    {
        var diff = GameManager.Instance != null
            ? GameManager.Instance.CurrentDifficulty
            : DifficultyLevel.Easy;

        if (diff == DifficultyLevel.Hard)
            return "Cuando el circulo se ponga VERDE, pulsa rapido.\n" +
                   "¡Cuidado! Si se pone AMARILLO es trampa: NO pulses y aguanta.\n\n" +
                   "Si pulsas antes de tiempo, pierdes la ronda.\n" +
                   "5 rondas: gana consiguiendo 4.";

        string info = diff == DifficultyLevel.Medium
            ? "Tienes 4 rondas. Ganas si consigues 3."
            : "Tienes 3 rondas. Ganas si consigues 2.";
        return "Mira el circulo.\n" +
               "Cuando se ponga VERDE, pulsa lo mas rapido que puedas.\n\n" +
               "Si pulsas antes del verde... fallo de ronda.\n" + info;
    }

    void ApplyDifficulty()
    {
        var diff = GameManager.Instance != null
            ? GameManager.Instance.CurrentDifficulty
            : DifficultyLevel.Easy;

        bool traps = false;
        switch (diff)
        {
            case DifficultyLevel.Medium:
                rounds            = 4;
                roundsToWin       = 3;
                roundTimeLimits   = new float[] { 2.5f, 1.5f, 1f, 0.8f };
                pauseBetweenRounds = 1.1f;
                break;
            case DifficultyLevel.Hard:
                rounds            = 5;
                roundsToWin       = 4;
                roundTimeLimits   = new float[] { 2f, 1.2f, 0.8f, 0.6f, 0.5f };
                pauseBetweenRounds = 0.8f;
                traps             = true;
                break;
        }

        // Plan de rondas trampa (solo dificil): 2 rondas amarillas, nunca la primera
        _trapPlan = new bool[rounds];
        if (traps && rounds >= 3)
        {
            int placed = 0;
            int safety = 0;
            while (placed < 2 && safety < 100)
            {
                int idx = Random.Range(1, rounds);
                if (!_trapPlan[idx]) { _trapPlan[idx] = true; placed++; }
                safety++;
            }
        }
    }

    protected override void OnMinigameStart()
    {
        ApplyDifficulty();
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

    IEnumerator RunRound()
    {
        _stimulusActive = false;
        _isTrap = _trapPlan != null && _currentRound < _trapPlan.Length && _trapPlan[_currentRound];
        _ui.SetWaiting();

        yield return new WaitForSeconds(0.35f);

        int limitIdx = Mathf.Clamp(_currentRound, 0, roundTimeLimits.Length - 1);
        _currentTimeLimit           = _isTrap ? TRAP_HOLD_TIME : roundTimeLimits[limitIdx];
        _reaction.ReactionTimeLimit = _currentTimeLimit;

        _input.AcceptInput = true;
        _reaction.StartRound();
    }

    void HandleStimulus()
    {
        _stimulusActive = true;
        if (_isTrap) _ui.SetTrapStimulus();
        else         _ui.SetStimulus(_currentTimeLimit);
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

        bool tooEarly = _reaction.WasTooEarly;
        bool timeout  = _reaction.WasTimeout;
        long ms       = _reaction.LastReactionMs;

        bool correct;
        if (_isTrap)
        {
            // Trampa amarilla: acierto = aguantar sin pulsar hasta el final
            correct = timeout;

            if (correct)
            {
                _correctCount++;
                _ui.ShowTrapResult(true);
                GameFeel.PlaySuccess();
                GameFeel.FloatingText("¡Aguantaste!", new Color(0.25f, 0.90f, 0.52f),
                                      new Vector2(0f, 260f));
                ReportEvent(true, -1f);
            }
            else if (tooEarly)
            {
                // Pulso antes incluso de ver el amarillo
                _ui.ShowRoundResult(true, false, ms, "");
                GameFeel.Error(null);
                GameFeel.FloatingText("¡Espera al verde!", new Color(0.96f, 0.72f, 0.18f),
                                      new Vector2(0f, 260f));
                ReportEvent(false, -1f);
            }
            else
            {
                _ui.ShowTrapResult(false);
                GameFeel.Error(null);
                GameFeel.FloatingText("¡Era trampa! No habia que pulsar",
                                      new Color(0.96f, 0.72f, 0.18f),
                                      new Vector2(0f, 260f), 40f);
                ReportEvent(false, ms);
            }
        }
        else
        {
            string evalMsg = (tooEarly || timeout) ? "" : ReactionManager.EvaluateTime(ms);
            correct = !tooEarly && !timeout;

            if (correct)
            {
                _correctCount++;
                _totalMs   += ms;
                _validCount++;
                GameFeel.PlaySuccess();
                GameFeel.FloatingText(ms + " ms", new Color(0.25f, 0.90f, 0.52f),
                                      new Vector2(0f, 260f));
                ReportEvent(true, ms);
            }
            else if (tooEarly)
            {
                GameFeel.Error(null);
                GameFeel.FloatingText("¡Espera al verde!", new Color(0.96f, 0.72f, 0.18f),
                                      new Vector2(0f, 260f));
                ReportEvent(false, -1f);
            }
            else // timeout
            {
                GameFeel.PlayError();
                ReportEvent(false, -1f);
            }

            _ui.ShowRoundResult(tooEarly, timeout, ms, evalMsg);
        }

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
        if (won) CompleteMinigame(score);
        else     FailMinigame();

        float ratio = (float)_correctCount / rounds;
        int   stars = GameFeel.StarsFromRatio(won, ratio);

        string avgStat = _validCount > 0
            ? "Reaccion media: " + (_totalMs / _validCount) + " ms"
            : "Reaccion media: -";

        ShowResults(won, stars, score,
            new[] { "Aciertos: " + _correctCount + "/" + rounds, avgStat },
            null,
            won ? "¡Reflejos de rayo!" : "Espera al verde con calma y lo lograras");
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

    void Update()
    {
        if (!IsPlaying) return;

        _pulseT += Time.deltaTime;
        _ui.PulseGlow(_pulseT);

        if (_stimulusActive)
            _ui.UpdateCountdown(_reaction.StimulusElapsed, _currentTimeLimit);
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
