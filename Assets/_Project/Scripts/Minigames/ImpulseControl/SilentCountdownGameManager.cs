// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using System.Collections;
using UnityEngine;

/// <summary>
/// EL SEMAFORO ESCONDIDO (inhibicion + espera paciente):
/// una cuenta atras visible se OCULTA en los ultimos segundos y el niño debe
/// pulsar exactamente cuando cree que llega a 0.
///  - Pulsar mientras el numero aun es visible = impulsivo (fallo claro).
///  - Acierto si la pulsacion cae dentro de la ventana de precision.
/// 5 rondas. ReportEvent(acierto, |desvio| en ms).
/// </summary>
public class SilentCountdownGameManager : MinigameBase
{
    const int ROUNDS      = 5;
    const int NEED_TO_WIN = 3;

    // ------------- parametros de dificultad (se fijan en ApplyDifficulty)
    int   _startCount = 5;     // numero inicial de la cuenta
    int   _hideAt     = 2;     // al llegar a este numero, se esconde
    float _window     = 0.8f;  // ventana de acierto (± segundos)

    SilentCountdownTimerManager   _timer;
    SilentCountdownInputHandler   _input;
    SilentCountdownUIController   _ui;
    SilentCountdownScoreEvaluator _eval;

    int   _round;
    int   _hits;
    int   _impulsive;
    int   _score;
    float _devSumMs;
    int   _devCount;
    bool  _roundResolved;
    bool  _ended;

    static readonly Color TXT_GREEN  = new Color(0.25f, 0.90f, 0.52f);
    static readonly Color TXT_RED    = new Color(0.95f, 0.35f, 0.35f);
    static readonly Color TXT_ORANGE = new Color(0.96f, 0.62f, 0.18f);

    protected override string GetIntroDescription()
    {
        return "La cuenta atras se esconde antes de llegar a 0.\n" +
               "¡Sigue contando en tu cabeza y pulsa justo en el 0!";
    }

    void ApplyDifficulty()
    {
        var diff = GameManager.Instance != null
            ? GameManager.Instance.CurrentDifficulty
            : DifficultyLevel.Easy;

        switch (diff)
        {
            case DifficultyLevel.Medium:
                _startCount = 5; _hideAt = 3; _window = 0.5f;
                break;
            case DifficultyLevel.Hard:
                _startCount = 6; _hideAt = 4; _window = 0.35f;
                break;
            default:
                _startCount = 5; _hideAt = 2; _window = 0.8f;
                break;
        }
    }

    protected override void OnMinigameStart()
    {
        ApplyDifficulty();
        KidUI.EnsureEventSystem();

        _timer = GetComponent<SilentCountdownTimerManager>();
        _input = GetComponent<SilentCountdownInputHandler>();
        _ui    = GetComponent<SilentCountdownUIController>();
        _eval  = GetComponent<SilentCountdownScoreEvaluator>();

        _ui.BuildUI(ROUNDS, _input);

        _timer.OnTick        += HandleTick;
        _timer.OnHidden      += HandleHidden;
        _timer.OnZeroTimeout += HandleTimeout;
        _input.OnPress       += HandlePress;
        _input.AcceptInput    = false;

        _round = 0; _hits = 0; _impulsive = 0; _score = 0;
        _devSumMs = 0f; _devCount = 0;
        _ended = false;

        StartCoroutine(RunRound(0.6f));
    }

    protected override void OnMinigameComplete() { }
    protected override void OnMinigameFailed()   { }

    IEnumerator RunRound(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (!IsPlaying) yield break;

        _roundResolved = false;
        _ui.SetRoundLabel(_round + 1, ROUNDS);
        _ui.ShowGetReady(_hideAt);

        yield return new WaitForSeconds(0.9f);
        if (!IsPlaying) yield break;

        _input.AcceptInput = true;
        _timer.StartCountdown(_startCount, _hideAt, 2.2f);
    }

    void HandleTick(int number)
    {
        if (!IsPlaying) return;
        GameFeel.PlayPop();
        _ui.ShowNumber(number);
    }

    void HandleHidden()
    {
        if (!IsPlaying) return;
        _ui.HideNumber();
    }

    void HandlePress()
    {
        if (!IsPlaying || _roundResolved || !_timer.Running) return;
        _roundResolved = true;
        _input.AcceptInput = false;

        bool  wasHidden = _timer.IsHidden;
        float deviation = _timer.Elapsed - _timer.TargetTime;   // + tarde, - pronto
        _timer.Stop();

        bool roundHit = false;

        if (!wasHidden)
        {
            // Pulsacion impulsiva: el numero todavia se veia
            _impulsive++;
            ReportEvent(false, -1f);

            GameFeel.Error(_ui.ButtonRect);
            _ui.ShowRoundFeedback(false, "¡Aun se veia el numero!", TXT_RED);
            GameFeel.FloatingText("¡Espera a que se esconda!", TXT_ORANGE,
                                  new Vector2(0f, 220f), 40f);
        }
        else
        {
            var res = _eval.Evaluate(deviation, _window);
            float devMs = Mathf.Abs(deviation) * 1000f;
            ReportEvent(res.Acierto, devMs);

            if (res.Acierto)
            {
                roundHit = true;
                _hits++;
                _score += res.Points;
                _devSumMs += devMs;
                _devCount++;

                GameFeel.Success(_ui.SemaphoreRect);
                GameFeel.FloatingText(res.Label, TXT_GREEN, new Vector2(0f, 220f), 46f);
                if (res.Points >= 200) GameFeel.Confetti(25);
            }
            else
            {
                _devSumMs += devMs;
                _devCount++;
                GameFeel.Error(_ui.SemaphoreRect);
            }

            _ui.ShowRoundFeedback(res.Acierto,
                res.Label + "  (" + FormatDeviation(deviation) + ")",
                res.Acierto ? TXT_GREEN : TXT_RED);
        }

        _ui.SetRoundDot(_round, roundHit);
        NextOrEnd();
    }

    void HandleTimeout()
    {
        if (!IsPlaying || _roundResolved) return;
        _roundResolved = true;
        _input.AcceptInput = false;

        ReportEvent(false, -1f);
        GameFeel.PlayError();
        _ui.ShowRoundFeedback(false, "El 0 se escapo... ¡pulsa antes!", TXT_RED);
        _ui.SetRoundDot(_round, false);
        NextOrEnd();
    }

    void NextOrEnd()
    {
        _round++;
        if (_round >= ROUNDS) StartCoroutine(EndGame());
        else                  StartCoroutine(RunRound(1.5f));
    }

    IEnumerator EndGame()
    {
        if (_ended) yield break;
        _ended = true;

        yield return new WaitForSeconds(1.2f);

        bool  won   = _hits >= NEED_TO_WIN;
        float ratio = (float)_hits / ROUNDS;
        int   final = won ? _score + _hits * 50 : 0;

        if (won) CompleteMinigame(final);
        else     FailMinigame();

        int stars = GameFeel.StarsFromRatio(won, ratio);

        string devStat = _devCount > 0
            ? "Desvio medio: " + Mathf.RoundToInt(_devSumMs / _devCount) + " ms"
            : "Desvio medio: -";

        ShowResults(won, stars, final,
            new[]
            {
                "Aciertos: " + _hits + "/" + ROUNDS,
                devStat,
                "Pulsaciones impulsivas: " + _impulsive
            },
            null,
            won ? "¡Que paciencia y que punteria!"
                : "Cuenta despacio en tu cabeza: 3... 2... 1...");
    }

    static string FormatDeviation(float dev)
    {
        string sign = dev >= 0f ? "+" : "-";
        return sign + Mathf.Abs(dev).ToString("0.0") + " s";
    }
}
