// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using System.Collections;
using UnityEngine;

public class SimonGameManager : MinigameBase
{

    SimonSequenceManager _seq;
    SimonAudioManager    _audio;
    SimonUIController    _ui;

    enum State { Idle, ShowingSequence, PlayerTurn, GameOver }
    State _state = State.Idle;

    // ------------------------------------------------ dificultad (runtime)
    int   _colorCount   = 4;
    int   _initialSteps = 2;
    int   _stepsToWin   = 5;
    float _stepDuration = 0.80f;
    float _stepGap      = 0.35f;
    float _previewPause = 0.90f;

    const int MAX_ERRORS      = 3;
    const int POINTS_PER_STEP = 100;

    int   _accumulatedScore;
    int   _totalErrors;
    int   _correctPresses;
    int   _totalPresses;
    float _pressTimer;

    /// <summary>Clave de récord POR PERFIL Y DIFICULTAD (antes era global y el
    /// récord de un niño en Fácil aparecía a otro niño jugando en Difícil).</summary>
    string RecordKey
    {
        get
        {
            string prof = (ProfileManager.Instance != null && ProfileManager.Instance.HasActiveProfile)
                ? ProfileManager.Instance.ActiveProfile.id
                : "guest";
            int diff = GameManager.Instance != null ? (int)GameManager.Instance.CurrentDifficulty : 0;
            return $"simon_record_{prof}_{diff}";
        }
    }
    int _record;

    Coroutine _gameLoop;

    protected override void Start()
    {
        minigameName = "Simón Dice";
        category     = MinigameCategory.Memory;
        base.Start();
    }

    protected override string GetIntroDescription() =>
        "Mira los colores que se encienden.\n" +
        "¡Repítelos en el mismo orden!";

    void ApplyDifficulty()
    {
        var diff = GameManager.Instance != null
            ? GameManager.Instance.CurrentDifficulty
            : DifficultyLevel.Easy;

        switch (diff)
        {
            case DifficultyLevel.Medium:
                _colorCount   = 4;  _initialSteps = 3;  _stepsToWin = 7;
                _stepDuration = 0.62f; _stepGap = 0.26f; _previewPause = 0.70f;
                break;
            case DifficultyLevel.Hard:
                _colorCount   = 6;  _initialSteps = 3;  _stepsToWin = 8;
                _stepDuration = 0.45f; _stepGap = 0.20f; _previewPause = 0.55f;
                break;
            default:
                _colorCount   = 4;  _initialSteps = 2;  _stepsToWin = 5;
                _stepDuration = 0.80f; _stepGap = 0.35f; _previewPause = 0.90f;
                break;
        }
    }

    protected override void OnMinigameStart()
    {
        ApplyDifficulty();

        _seq   = GetComponent<SimonSequenceManager>();
        _audio = GetComponent<SimonAudioManager>();
        _ui    = GetComponent<SimonUIController>();

        _seq.Initialize(_colorCount);

        _record           = PlayerPrefs.GetInt(RecordKey, 0);
        _accumulatedScore = 0;
        _totalErrors      = 0;
        _correctPresses   = 0;
        _totalPresses     = 0;

        _ui.BuildUI(_colorCount);
        _ui.SetRecord(_record);
        _ui.SetProgress(0, TotalRounds);
        _ui.SetStatus("");

        foreach (var btn in _ui.Buttons)
            btn.SetInteractive(false);

        _ui.OnRestartPressed += () => RestartMinigame();
        _ui.OnMenuPressed    += () => ReturnToGameSelector();

        if (_gameLoop != null) StopCoroutine(_gameLoop);
        _gameLoop = StartCoroutine(GameLoop());
    }

    protected override void OnMinigameComplete() { }
    protected override void OnMinigameFailed()   { }

    /// <summary>Ronda actual contando desde 1 (la secuencia inicial ya trae
    /// _initialSteps colores, por eso no se muestra la longitud en bruto).</summary>
    int CurrentRound => Mathf.Max(1, _seq.Round - _initialSteps + 1);

    /// <summary>Rondas totales para ganar (p. ej. Medio: de 3 a 7 colores = 5 rondas).</summary>
    int TotalRounds => Mathf.Max(1, _stepsToWin - _initialSteps + 1);

    IEnumerator GameLoop()
    {
        _seq.ResetSequence();
        for (int i = 0; i < _initialSteps - 1; i++)
            _seq.AddStep();

        while (true)
        {
            _seq.AddStep();
            _ui.SetProgress(CurrentRound, TotalRounds);
            SetAllInteractive(false);

            _state = State.ShowingSequence;
            _ui.SetStatus("Mira con atención…", new Color(0.38f, 0.52f, 0.68f));
            yield return new WaitForSeconds(0.55f);

            yield return ShowSequence();
            yield return new WaitForSeconds(_previewPause);

            _state = State.PlayerTurn;
            _ui.SetStatus("¡Tu turno!", new Color(0.22f, 0.86f, 0.54f));
            SetAllInteractive(true);

            bool[] failed = { false };
            yield return WaitForPlayerInput(failed);
            SetAllInteractive(false);

            if (failed[0])
            {
                yield return HandleGameOver();
                yield break;
            }

            _accumulatedScore += POINTS_PER_STEP * _seq.Round;

            if (_seq.Round >= _stepsToWin)
            {
                yield return HandleWin();
                yield break;
            }

            _state = State.Idle;
            _ui.SetStatus("¡Muy bien! Uno más…", new Color(0.96f, 0.78f, 0.18f));
            _audio.PlaySuccess();
            GameFeel.PlayStar();
            yield return new WaitForSeconds(1.00f);
        }
    }

    IEnumerator ShowSequence()
    {
        for (int i = 0; i < _seq.Round; i++)
        {
            int idx = _seq.GetStep(i);
            _audio.PlayColor(idx);
            yield return _ui.Buttons[idx].Flash(_stepDuration);
            yield return new WaitForSeconds(_stepGap);
        }
    }

    IEnumerator WaitForPlayerInput(bool[] failedOut)
    {
        bool done   = false;
        bool failed = false;
        _pressTimer = Time.time;

        void OnInput(int idx)
        {
            if (_state != State.PlayerTurn || done) return;

            float rtMs = (Time.time - _pressTimer) * 1000f;
            _pressTimer = Time.time;

            bool correct = _seq.Submit(idx, out bool roundComplete);
            StartCoroutine(_ui.Buttons[idx].PlayerPress(0.18f));

            _totalPresses++;
            ReportEvent(correct, rtMs);

            if (!correct)
            {
                _totalErrors++;
                _audio.PlayFail();
                GameFeel.Error(null);
                StartCoroutine(_ui.FlashError());

                if (_totalErrors >= MAX_ERRORS)
                {
                    failed = true;
                    done   = true;
                }
                else
                {
                    _ui.SetStatus("¡Uy! Te quedan " + (MAX_ERRORS - _totalErrors) +
                                  (MAX_ERRORS - _totalErrors == 1 ? " intento" : " intentos"),
                                  new Color(0.90f, 0.45f, 0.20f));
                }
            }
            else
            {
                _correctPresses++;
                _audio.PlayColor(idx);
                if (roundComplete) done = true;
            }
        }

        foreach (var btn in _ui.Buttons) btn.OnPressed += OnInput;
        while (!done) yield return null;
        foreach (var btn in _ui.Buttons) btn.OnPressed -= OnInput;

        failedOut[0] = failed;
    }

    IEnumerator HandleGameOver()
    {
        _state = State.GameOver;
        _ui.SetStatus("¡Oh no! Esa no era…", new Color(0.90f, 0.22f, 0.28f));

        SaveRecord(_seq.Round - 1);

        FailMinigame();

        yield return new WaitForSeconds(0.9f);

        ShowResults(false, 0, _accumulatedScore,
            new string[]
            {
                "Colores seguidos: " + Mathf.Max(0, _seq.Round - 1),
                "Meta: " + _stepsToWin,
                "Récord: " + _record
            },
            "¡Casi!",
            "Vuelve a intentarlo. ¡Tú puedes!");
    }

    IEnumerator HandleWin()
    {
        _state = State.GameOver;
        _ui.SetStatus("¡Lo lograste!", new Color(0.22f, 0.86f, 0.54f));
        _audio.PlaySuccess();
        GameFeel.Confetti(60);

        SaveRecord(_seq.Round);

        CompleteMinigame(_accumulatedScore);

        yield return new WaitForSeconds(1.1f);

        float ratio = _totalPresses > 0 ? (float)_correctPresses / _totalPresses : 1f;
        int   stars = GameFeel.StarsFromRatio(true, ratio);

        ShowResults(true, stars, _accumulatedScore,
            new string[]
            {
                "Colores seguidos: " + _seq.Round,
                "Fallos: " + _totalErrors,
                "Récord: " + _record
            },
            "¡Increíble memoria!",
            "Repetiste toda la secuencia.");
    }

    void SaveRecord(int reached)
    {
        if (reached > _record)
        {
            _record = reached;
            PlayerPrefs.SetInt(RecordKey, _record);
            PlayerPrefs.Save();
        }
        _ui.SetRecord(_record);
    }

    void SetAllInteractive(bool value)
    {
        if (_ui?.Buttons == null) return;
        foreach (var btn in _ui.Buttons)
            btn.SetInteractive(value);
    }
}
