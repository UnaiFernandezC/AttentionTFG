// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using System.Collections;
using UnityEngine;

public class DontPressGameManager : MinigameBase
{

    [Header("Rondas")]
    public int rounds      = 3;
    public int roundsToWin = 2;

    [Header("Temporizador")]
    public float waitMin      = 2.0f;
    public float waitMax      = 5.0f;
    public float activeWindow = 2.5f;

    [Header("Señales falsas (0 = Easy, 1 = Medium, 2 = Hard)")]
    public int fakeOutCount = 0;

    [Header("Pausa entre rondas (s)")]
    public float pauseBetweenRounds = 1.6f;

    [Header("Señal verde falsa")]
    [Tooltip("Probabilidad de que aparezca una señal verde falsa antes del verde real (0=nunca, 1=siempre)")]
    [SerializeField] float fakeGreenChance = 0.4f;

    [Header("Distractor naranja")]
    [Tooltip("Probabilidad de que aparezca el color naranja (no pulsar) durante la espera")]
    [SerializeField] float fakeOutChance = 0.3f;

    DontPressTimerManager  _timer;
    DontPressUIController  _ui;

    int  _currentRound;
    int  _correctCount;
    int  _errors;
    long _totalReactionMs;
    int  _validReactions;
    bool _roundActive;
    bool _waitingPhase;
    float _activeStart;

    static readonly Color C_GREEN  = new Color(0.20f, 0.86f, 0.50f, 0.30f);
    static readonly Color C_RED    = new Color(0.90f, 0.18f, 0.22f, 0.35f);
    static readonly Color C_YELLOW = new Color(0.95f, 0.80f, 0.15f, 0.28f);
    static readonly Color C_GRAY   = new Color(0.40f, 0.44f, 0.50f, 0.28f);

    static readonly Color TXT_DIM    = new Color(0.40f, 0.55f, 0.65f);
    static readonly Color TXT_GREEN  = new Color(0.22f, 0.86f, 0.54f);
    static readonly Color TXT_RED    = new Color(0.90f, 0.22f, 0.28f);
    static readonly Color TXT_YELLOW = new Color(0.95f, 0.80f, 0.15f);

    protected override string GetIntroDescription()
    {
        var diff = GameManager.Instance != null
            ? GameManager.Instance.CurrentDifficulty
            : DifficultyLevel.Easy;

        if (diff == DifficultyLevel.Hard)
            return "Pulsa SOLO con el VERDE FIJO.\n" +
                   "Si el verde PARPADEA... ¡es trampa, no pulses!";

        return "Espera quieto... y pulsa SOLO cuando el boton se ponga VERDE.\n" +
               "¡Si pulsas antes, fallo!";
    }

    void ApplyDifficulty()
    {
        var diff = GameManager.Instance != null
            ? GameManager.Instance.CurrentDifficulty
            : DifficultyLevel.Easy;
        switch (diff)
        {
            case DifficultyLevel.Medium:
                rounds          = 4;
                roundsToWin     = 3;
                waitMin         = 1.5f;
                waitMax         = 4.5f;
                activeWindow    = 2.0f;
                fakeOutCount    = 1;
                fakeGreenChance = 0f;      // el verde falso solo aparece en Hard
                fakeOutChance   = 0.5f;
                break;
            case DifficultyLevel.Hard:
                rounds          = 5;
                roundsToWin     = 4;
                waitMin         = 1.0f;
                waitMax         = 4.0f;
                activeWindow    = 1.5f;
                fakeOutCount    = 2;
                fakeGreenChance = 0.55f;   // falsa alarma: verde PARPADEANTE
                fakeOutChance   = 0.6f;
                break;
            default:   // Easy
                rounds          = 3;
                roundsToWin     = 2;
                waitMin         = 2.0f;
                waitMax         = 5.0f;
                activeWindow    = 2.5f;
                fakeOutCount    = 0;
                fakeGreenChance = 0f;
                fakeOutChance   = 0.3f;
                break;
        }
    }

    protected override void OnMinigameStart()
    {
        ApplyDifficulty();
        KidUI.EnsureEventSystem();

        _timer = GetComponent<DontPressTimerManager>();
        _ui    = GetComponent<DontPressUIController>();

        _timer.WaitMin      = waitMin;
        _timer.WaitMax      = waitMax;
        _timer.ActiveWindow = activeWindow;
        _timer.FakeOutCount = fakeOutCount;

        _timer.OnActivated += HandleActivated;
        _timer.OnTimeout   += HandleTimeout;
        _timer.OnFakeOut   += HandleFakeOut;

        _ui.BuildUI(rounds);
        _ui.MainButton.onClick.AddListener(HandleButtonClick);

        _currentRound     = 0;
        _correctCount     = 0;
        _errors           = 0;
        _totalReactionMs  = 0;
        _validReactions   = 0;
        _roundActive      = false;
        _waitingPhase     = false;

        for (int i = 0; i < rounds; i++)
            _ui.SetRoundDot(i, null);

        StartCoroutine(StartRoundDelayed(0.5f));
    }

    protected override void OnMinigameComplete() { }
    protected override void OnMinigameFailed()   { }

    void Update()
    {
        if (!IsPlaying) return;

        _ui.ButtonCtrl.Tick();

        if (_roundActive && !_waitingPhase)
        {
            _timer.Tick();
            _ui.UpdateCountdown(_timer.ActiveElapsed, activeWindow);
        }
    }

    IEnumerator StartRoundDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);
        StartRound();
    }

    void StartRound()
    {
        _roundActive  = true;
        _waitingPhase = true;

        _ui.ButtonCtrl.SetWaiting();
        _ui.SetStatusText("Espera... no pulses todavia", TXT_DIM);
        _ui.HideCountdown();
        _timer.StartRound();

        if (UnityEngine.Random.value < fakeGreenChance)
            StartCoroutine(FakeGreenRoutine());

        if (UnityEngine.Random.value < fakeOutChance)
            StartCoroutine(OrangeDistractorRoutine());
    }

    IEnumerator OrangeDistractorRoutine()
    {
        float delay = UnityEngine.Random.Range(waitMin * 0.4f, waitMin * 0.9f);
        float elapsed = 0f;
        while (elapsed < delay)
        {
            if (!_waitingPhase || !_roundActive) yield break;
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (!_waitingPhase || !_roundActive) yield break;

        _ui.ButtonCtrl.SetOrange();
        _ui.SetStatusText("¡Naranja! No pulses", TXT_YELLOW);
        _ui.Flash(C_YELLOW);

        yield return new WaitForSeconds(0.7f);

        if (_waitingPhase && _roundActive)
        {
            _ui.ButtonCtrl.SetWaiting();
            _ui.SetStatusText("Espera... no pulses todavia", TXT_DIM);
        }
    }

    IEnumerator FakeGreenRoutine()
    {
        // Falsa alarma (solo Hard): el boton se pone verde pero PARPADEANDO.
        // El verde real es FIJO. Pulsar aqui cuenta como impulso (tooEarly).
        float delay = UnityEngine.Random.Range(waitMin * 0.3f, waitMin * 0.8f);
        float elapsed = 0f;
        while (elapsed < delay)
        {
            if (!_waitingPhase || !_roundActive) yield break;
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (!_waitingPhase || !_roundActive) yield break;

        _ui.ButtonCtrl.SetFakeGreen();
        _ui.SetStatusText("¡Parpadea! Ese verde es de mentira", TXT_YELLOW);
        _ui.Flash(C_YELLOW);
        GameFeel.PlayPop();

        yield return new WaitForSeconds(1.0f);

        if (_waitingPhase && _roundActive)
        {
            _ui.ButtonCtrl.SetWaiting();
            _ui.SetStatusText("Espera... no pulses todavia", TXT_DIM);
        }
    }

    void HandleActivated()
    {
        _waitingPhase = false;
        _activeStart  = Time.time;

        _ui.ButtonCtrl.SetActive();
        _ui.SetStatusText("¡AHORA! ¡Pulsa el boton!", TXT_GREEN);
        _ui.Flash(C_GREEN);
        GameFeel.PlayPop();
        UITween.PulseOnce(_ui.ButtonRect, 1.12f, 0.22f);
    }

    void HandleFakeOut()
    {
        _ui.ButtonCtrl.SetFakeOut();
        _ui.SetStatusText("¡Ojo! — señal falsa", TXT_YELLOW);
        _ui.Flash(C_YELLOW);
        StartCoroutine(RestoreWaitingAfterFake());
    }

    IEnumerator RestoreWaitingAfterFake()
    {
        yield return new WaitForSeconds(0.42f);
        if (_waitingPhase && _roundActive)
        {
            _ui.ButtonCtrl.SetWaiting();
            _ui.SetStatusText("Espera... no pulses todavia", TXT_DIM);
        }
    }

    void HandleButtonClick()
    {
        if (!IsPlaying || !_roundActive) return;

        if (_waitingPhase)
        {

            EndRound(correct: false, tooEarly: true);
        }
        else
        {

            bool valid = _timer.RegisterCorrectPress();
            if (!valid) return;

            long reactionMs = (long)((Time.time - _activeStart) * 1000f);
            _totalReactionMs += reactionMs;
            _validReactions++;

            EndRound(correct: true, tooEarly: false, reactionMs: reactionMs);
        }
    }

    void HandleTimeout()
    {
        if (!_roundActive) return;
        EndRound(correct: false, tooEarly: false, timeout: true);
    }

    void EndRound(bool correct, bool tooEarly = false,
                  bool timeout = false, long reactionMs = 0)
    {
        _timer.StopRound();
        _roundActive  = false;
        _waitingPhase = false;

        if (correct)
        {
            _correctCount++;
            ReportEvent(true, reactionMs);   // RT real de la ronda

            _ui.ButtonCtrl.SetCorrect();
            _ui.SetStatusText("¡Bien hecho!  " + reactionMs + " ms", TXT_GREEN);
            _ui.Flash(C_GREEN);
            GameFeel.Success(_ui.ButtonRect);
            GameFeel.FloatingText(reactionMs + " ms", TXT_GREEN,
                                  new Vector2(0f, 250f), 42f);
        }
        else if (tooEarly)
        {
            _errors++;
            ReportEvent(false, -1f);   // pulsacion prematura (impulso)

            _ui.ButtonCtrl.SetEarly();
            _ui.SetStatusText("Demasiado pronto — ¡espera al verde fijo!", TXT_RED);
            _ui.Flash(C_RED);
            GameFeel.Error(_ui.ButtonRect);
            GameFeel.FloatingText("¡Demasiado pronto!", TXT_RED,
                                  new Vector2(0f, 250f), 38f);
        }
        else
        {
            ReportEvent(false, -1f);   // omision: no pulso a tiempo

            _ui.ButtonCtrl.SetMissed();
            _ui.SetStatusText("Tiempo agotado — ¡mas rapido la proxima!", TXT_DIM);
            _ui.Flash(C_GRAY);
            GameFeel.PlayError();
            GameFeel.FloatingText("¡Se escapo!", TXT_DIM,
                                  new Vector2(0f, 250f), 38f);
        }

        _ui.SetRoundDot(_currentRound, correct);
        _ui.HideCountdown();
        _currentRound++;

        bool allDone = _currentRound >= rounds;
        bool canWin  = (_correctCount + (rounds - _currentRound)) >= roundsToWin;

        if (allDone || !canWin)
            StartCoroutine(FinishGame(_correctCount >= roundsToWin));
        else
            StartCoroutine(StartRoundDelayed(pauseBetweenRounds));
    }

    IEnumerator FinishGame(bool won)
    {
        yield return new WaitForSeconds(1.2f);

        int score = CalculateScore(won);
        if (won) CompleteMinigame(score);
        else     FailMinigame();

        float ratio = rounds > 0 ? (float)_correctCount / rounds : 0f;
        int   stars = GameFeel.StarsFromRatio(won, ratio);
        long  avgMs = _validReactions > 0 ? _totalReactionMs / _validReactions : 0;

        string rtStat = _validReactions > 0
            ? "Velocidad media: " + avgMs + " ms"
            : "Velocidad media: -";

        ShowResults(won, stars, score,
            new[]
            {
                "Rondas perfectas: " + _correctCount + "/" + rounds,
                rtStat,
                "Pulsaciones antes de tiempo: " + _errors
            },
            null,
            won ? "¡Resististe el impulso como un campeon!"
                : "Truco: espera al VERDE FIJO, sin prisa");
    }

    int CalculateScore(bool won)
    {
        if (!won) return 0;
        int  baseS   = 400;
        int  rounds_ = _correctCount * 80;
        long avgMs   = _validReactions > 0 ? _totalReactionMs / _validReactions : 9999L;

        int  speed   = Mathf.Max(0, Mathf.RoundToInt((700f - (float)avgMs) * 0.4f));
        return baseS + rounds_ + speed;
    }
}
