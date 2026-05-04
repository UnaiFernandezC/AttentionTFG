using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

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

    DontPressTimerManager  _timer;
    DontPressUIController  _ui;

    int  _currentRound;
    int  _correctCount;
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

    protected override string GetIntroDescription() =>
        "Aparece un boton ROJO: NO lo pulses todavia.\n" +
        "Cuando cambie a VERDE: pulsalo lo antes posible.\n\n" +
        "Pulsar antes de tiempo cuenta como fallo.\n" +
        "Consigue " + roundsToWin + " de " + rounds + " rondas para ganar.";

    protected override void OnMinigameStart()
    {
        EnsureEventSystem();

        _timer = GetComponent<DontPressTimerManager>();
        _ui    = GetComponent<DontPressUIController>();

        _timer.WaitMin      = waitMin;
        _timer.WaitMax      = waitMax;
        _timer.ActiveWindow = activeWindow;
        _timer.FakeOutCount = fakeOutCount;

        _timer.OnActivated += HandleActivated;
        _timer.OnTimeout   += HandleTimeout;
        _timer.OnFakeOut   += HandleFakeOut;

        _ui.BuildUI(rounds, () => RestartMinigame(), () => ReturnToGameSelector());
        _ui.MainButton.onClick.AddListener(HandleButtonClick);

        _currentRound     = 0;
        _correctCount     = 0;
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
    }

    void HandleActivated()
    {
        _waitingPhase = false;
        _activeStart  = Time.time;

        _ui.ButtonCtrl.SetActive();
        _ui.SetStatusText("¡AHORA! ¡Pulsa el boton!", TXT_GREEN);
        _ui.Flash(C_GREEN);
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
            _ui.ButtonCtrl.SetCorrect();
            _ui.SetStatusText("¡Bien hecho!  " + reactionMs + " ms", TXT_GREEN);
            _ui.Flash(C_GREEN);
        }
        else if (tooEarly)
        {
            _ui.ButtonCtrl.SetEarly();
            _ui.SetStatusText("Demasiado pronto — impulso no controlado", TXT_RED);
            _ui.Flash(C_RED);
        }
        else
        {
            _ui.ButtonCtrl.SetMissed();
            _ui.SetStatusText("Tiempo agotado — reaccion demasiado lenta", TXT_DIM);
            _ui.Flash(C_GRAY);
        }

        _ui.SetRoundDot(_currentRound, correct);
        _ui.HideCountdown();
        _currentRound++;

        int remaining   = rounds - _currentRound;
        bool alreadyWon = _correctCount >= roundsToWin;
        bool canStillWin= (_correctCount + remaining) >= roundsToWin;
        bool allDone    = _currentRound >= rounds;

        if (alreadyWon || allDone || !canStillWin)
            StartCoroutine(FinishGame(alreadyWon));
        else
            StartCoroutine(StartRoundDelayed(pauseBetweenRounds));
    }

    IEnumerator FinishGame(bool won)
    {
        yield return new WaitForSeconds(1.2f);

        int score = CalculateScore(won);
        CompleteMinigame(score);
        _ui.ShowFinalResult(won, _correctCount, rounds, score);
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
