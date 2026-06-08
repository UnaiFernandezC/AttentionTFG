using System.Collections;
using UnityEngine;

public class SimonGameManager : MinigameBase
{

    SimonSequenceManager _seq;
    SimonAudioManager    _audio;
    SimonUIController    _ui;

    enum State { Idle, ShowingSequence, PlayerTurn, RoundWon, GameOver }
    State _state = State.Idle;

    [Header("Número de colores de la escena")]
    [Tooltip("Cuántos colores distintos usa el juego en esta escena (4 o 5).")]
    [SerializeField] int colorCount = 4;

    [Header("Fase 1")]
    [Tooltip("La fase termina cuando el jugador alcanza esta longitud de secuencia.")]
    [SerializeField] int   stepsToWin1   = 3;
    [Tooltip("Duración del flash de cada color (segundos).")]
    [SerializeField] float stepDuration1 = 0.80f;
    [Tooltip("Pausa entre flashes (segundos).")]
    [SerializeField] float stepGap1      = 0.35f;
    [Tooltip("Pausa antes del turno del jugador (segundos).")]
    [SerializeField] float previewPause1 = 0.90f;

    [Header("Fase 2")]
    [SerializeField] int   stepsToWin2   = 4;
    [SerializeField] float stepDuration2 = 0.60f;
    [SerializeField] float stepGap2      = 0.28f;
    [SerializeField] float previewPause2 = 0.70f;

    [Header("Fase 3")]
    [SerializeField] int   stepsToWin3   = 5;
    [SerializeField] float stepDuration3 = 0.45f;
    [SerializeField] float stepGap3      = 0.22f;
    [SerializeField] float previewPause3 = 0.55f;

    [Header("Puntuación")]
    [SerializeField] int pointsPerStep = 100;

    float _stepDuration;
    float _stepGap;
    float _previewPause;
    int   _currentPhase;
    int   _accumulatedScore;

    const string PREF_RECORD = "simon_record";
    int _record;

    Coroutine _gameLoop;

    protected override void Start()
    {
        minigameName = "Simón Dice";
        category     = MinigameCategory.Memory;
        base.Start();
    }

    protected override string GetIntroDescription() =>
        "Observa la secuencia de colores que se ilumina.\n" +
        "Cuando sea tu turno, repítela en el mismo orden.\n\n" +
        "Supera las 3 fases para ganar. ¡Buena suerte!";

    protected override void OnMinigameStart()
    {
        _seq   = GetComponent<SimonSequenceManager>();
        _audio = GetComponent<SimonAudioManager>();
        _ui    = GetComponent<SimonUIController>();

        _seq.Initialize(colorCount);

        _record           = PlayerPrefs.GetInt(PREF_RECORD, 0);
        _accumulatedScore = 0;
        _currentPhase     = 0;

        _ui.BuildUI(colorCount);
        _ui.SetRecord(_record);
        _ui.SetRound(0);
        _ui.SetPhase(1, 3);
        _ui.SetStatus("");

        foreach (var btn in _ui.Buttons)
        {
            btn.SetInteractive(false);
            btn.OnPressed += _ => { };
        }

        _ui.OnRestartPressed += () => RestartMinigame();
        _ui.OnMenuPressed    += () => ReturnToGameSelector();

        if (_gameLoop != null) StopCoroutine(_gameLoop);
        _gameLoop = StartCoroutine(GameLoop());
    }

    protected override void OnMinigameComplete() { }
    protected override void OnMinigameFailed()   { }

    IEnumerator GameLoop()
    {
        int[] stepsToWin     = { stepsToWin1,   stepsToWin2,   stepsToWin3   };
        float[] durations    = { stepDuration1, stepDuration2, stepDuration3 };
        float[] gaps         = { stepGap1,      stepGap2,      stepGap3      };
        float[] previews     = { previewPause1, previewPause2, previewPause3 };

        for (_currentPhase = 0; _currentPhase < 3; _currentPhase++)
        {

            _stepDuration = durations[_currentPhase];
            _stepGap      = gaps[_currentPhase];
            _previewPause = previews[_currentPhase];
            int phaseSteps = stepsToWin[_currentPhase];

            _seq.ResetSequence();

            _ui.SetPhase(_currentPhase + 1, 3);
            _ui.SetRound(0);

            bool gameOver = false;

            while (_seq.Round < phaseSteps)
            {

                _seq.AddStep();
                _ui.SetRound(_seq.Round);
                SetAllInteractive(false);

                _state = State.ShowingSequence;
                _ui.SetStatus("Observa la secuencia…", new Color(0.38f, 0.52f, 0.68f));
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
                    gameOver = true;
                    break;
                }

                _state = State.RoundWon;
                _accumulatedScore += pointsPerStep * _seq.Round;
                _ui.SetStatus("¡Correcto! +1 color", new Color(0.96f, 0.78f, 0.18f));
                _audio.PlaySuccess();
                yield return new WaitForSeconds(1.00f);
            }

            if (gameOver)
            {
                yield return HandleGameOver();
                yield break;
            }

            if (_currentPhase < 2)
                yield return PhaseTransition(_currentPhase + 1);
        }

        yield return HandleWin();
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

        void OnInput(int idx)
        {
            if (_state != State.PlayerTurn || done) return;

            bool correct = _seq.Submit(idx, out bool roundComplete);
            StartCoroutine(_ui.Buttons[idx].PlayerPress(0.18f));

            if (!correct)
            {
                _audio.PlayFail();
                StartCoroutine(_ui.FlashError());
                failed = true;
                done   = true;
            }
            else
            {
                _audio.PlayColor(idx);
                if (roundComplete) done = true;
            }
        }

        foreach (var btn in _ui.Buttons) btn.OnPressed += OnInput;
        while (!done) yield return null;
        foreach (var btn in _ui.Buttons) btn.OnPressed -= OnInput;

        failedOut[0] = failed;
    }

    IEnumerator PhaseTransition(int nextPhase)
    {
        _state = State.Idle;
        SetAllInteractive(false);

        _ui.SetStatus($"¡Fase {nextPhase - 1} completada!\nPreparate para la fase {nextPhase}…",
                      new Color(0.96f, 0.78f, 0.18f));
        _audio.PlaySuccess();
        yield return new WaitForSeconds(2.20f);

        _ui.SetStatus("");
    }

    IEnumerator HandleGameOver()
    {
        _state = State.GameOver;
        _ui.SetStatus("¡Oh no! Secuencia incorrecta", new Color(0.90f, 0.22f, 0.28f));

        bool newRecord = false;
        int totalSteps = _seq.Round + _currentPhase * 10;
        if (totalSteps > _record)
        {
            _record = totalSteps;
            newRecord = true;
            PlayerPrefs.SetInt(PREF_RECORD, _record);
            PlayerPrefs.Save();
        }
        _ui.SetRecord(_record);
        CompleteMinigame(_accumulatedScore);

        yield return new WaitForSeconds(0.80f);
        _ui.ShowResult(newRecord, _seq.Round, _record);
    }

    IEnumerator HandleWin()
    {
        _state = State.GameOver;
        _ui.SetStatus("¡Lo lograste! ¡Eres increíble!", new Color(0.22f, 0.86f, 0.54f));
        _audio.PlaySuccess();

        bool newRecord = false;
        int totalSteps = 999;
        if (_record < totalSteps)
        {
            newRecord = true;
            _record   = totalSteps;
            PlayerPrefs.SetInt(PREF_RECORD, _record);
            PlayerPrefs.Save();
        }
        _ui.SetRecord(_record);
        CompleteMinigame(_accumulatedScore);

        yield return new WaitForSeconds(0.80f);
        _ui.ShowWin(newRecord, 3, 3);
    }

    void SetAllInteractive(bool value)
    {
        if (_ui?.Buttons == null) return;
        foreach (var btn in _ui.Buttons)
            btn.SetInteractive(value);
    }
}
