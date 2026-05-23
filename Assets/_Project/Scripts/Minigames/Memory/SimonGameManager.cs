using System.Collections;
using UnityEngine;

/// <summary>
/// Orquestador principal del minijuego "Simón Dice".
/// Extiende MinigameBase → recibe intro panel automático con color de categoría
/// y EventSystem garantizado antes de que se construya la UI del juego.
///
/// ── Máquina de estados ──────────────────────────────────────────────────────
///   Idle            → Esperando que el jugador pulse "Empezar"
///   ShowingSequence → Reproduciendo la secuencia de colores
///   PlayerTurn      → Esperando input del jugador
///   RoundWon        → El jugador completó la ronda (breve pausa antes de la siguiente)
///   GameOver        → El jugador cometió un error
///
/// ── Dificultad ──────────────────────────────────────────────────────────────
///   Easy   → stepDuration=0.80s  stepGap=0.35s  previewPause=0.90s
///   Medium → stepDuration=0.55s  stepGap=0.25s  previewPause=0.65s
///   Hard   → stepDuration=0.35s  stepGap=0.18s  previewPause=0.45s
/// </summary>
public class SimonGameManager : MinigameBase
{
    // ── Componentes hermanos ──────────────────────────────────────────────────
    SimonSequenceManager _seq;
    SimonAudioManager    _audio;
    SimonUIController    _ui;

    // ── Estado ────────────────────────────────────────────────────────────────
    enum State { Idle, ShowingSequence, PlayerTurn, RoundWon, GameOver }
    State _state = State.Idle;

    // ── Dificultad (configurable desde Inspector) ─────────────────────────────
    [Header("Dificultad")]
    [Tooltip("Duración (segundos) de cada flash de color en la secuencia.")]
    [SerializeField] float stepDurationEasy   = 0.80f;
    [SerializeField] float stepDurationMedium = 0.55f;
    [SerializeField] float stepDurationHard   = 0.35f;

    [Tooltip("Pausa (segundos) entre cada color de la secuencia.")]
    [SerializeField] float stepGapEasy   = 0.35f;
    [SerializeField] float stepGapMedium = 0.25f;
    [SerializeField] float stepGapHard   = 0.18f;

    [Tooltip("Pausa antes de que el jugador pueda introducir su respuesta.")]
    [SerializeField] float previewPauseEasy   = 0.90f;
    [SerializeField] float previewPauseMedium = 0.65f;
    [SerializeField] float previewPauseHard   = 0.45f;

    [Header("Puntuación")]
    [Tooltip("Puntos base por ronda completada.")]
    [SerializeField] int pointsPerRound = 100;

    // ── Cache de config por dificultad ────────────────────────────────────────
    float _stepDuration;
    float _stepGap;
    float _previewPause;

    // ── Récord ────────────────────────────────────────────────────────────────
    const string PREF_RECORD = "simon_record_easy";
    string PREF_RECORD_Runtime = PREF_RECORD;

    int _record;
    int _sessionScore;

    // ── Coroutine activa ──────────────────────────────────────────────────────
    Coroutine _gameLoop;

    // ═════════════════════════════════════════════════════════════════════════
    // MinigameBase overrides
    // ═════════════════════════════════════════════════════════════════════════

    // Establece nombre y categoría antes de que MinigameBase construya el intro panel
    protected override void Start()
    {
        minigameName = "Simón Dice";
        category     = MinigameCategory.Memory;
        base.Start();
    }

    protected override string GetIntroDescription() =>
        "Observa la secuencia de colores que se ilumina.\n" +
        "Cuando sea tu turno, repítela en el mismo orden.\n\n" +
        "Cada ronda se añade un color más. ¿Hasta dónde llegarás?";

    // Llamado por MinigameBase cuando el jugador pulsa COMENZAR o [ESPACIO]
    protected override void OnMinigameStart()
    {
        _seq   = GetComponent<SimonSequenceManager>();
        _audio = GetComponent<SimonAudioManager>();
        _ui    = GetComponent<SimonUIController>();

        ApplyDifficulty();
        _record       = PlayerPrefs.GetInt(PREF_RECORD_Runtime, 0);
        _sessionScore = 0;

        // Construir UI del juego
        _ui.BuildUI();
        _ui.SetRecord(_record);
        _ui.SetRound(0);
        _ui.SetStatus("");

        // Conectar botones de colores
        foreach (var btn in _ui.Buttons)
        {
            btn.SetInteractive(false);
            btn.OnPressed += HandleButtonPressed;
        }

        // Conectar navegación (reiniciar = recargar escena; menú = selector)
        _ui.OnRestartPressed += () => RestartMinigame();
        _ui.OnMenuPressed    += () => ReturnToGameSelector();

        // Arrancar
        _seq.ResetSequence();
        if (_gameLoop != null) StopCoroutine(_gameLoop);
        _gameLoop = StartCoroutine(GameLoop());
        _state = State.Idle;
    }

    protected override void OnMinigameComplete() { }
    protected override void OnMinigameFailed()   { }

    // ═════════════════════════════════════════════════════════════════════════
    // Flujo principal
    // ═════════════════════════════════════════════════════════════════════════

    IEnumerator GameLoop()
    {
        while (true)
        {
            // 1. Añadir paso y actualizar UI
            _seq.AddStep();
            _ui.SetRound(_seq.Round);
            SetAllInteractive(false);

            // 2. Breve pausa antes de mostrar la secuencia
            _state = State.ShowingSequence;
            _ui.SetStatus("Observa la secuencia…", new Color(0.38f, 0.52f, 0.68f));
            yield return new WaitForSeconds(0.55f);

            // 3. Mostrar secuencia
            yield return ShowSequence();

            // 4. Pausa antes del turno del jugador
            yield return new WaitForSeconds(_previewPause);

            // 5. Turno del jugador
            _state = State.PlayerTurn;
            _ui.SetStatus("¡Tu turno!", new Color(0.22f, 0.86f, 0.54f));
            SetAllInteractive(true);

            // 6. Esperar hasta que el jugador complete o falle
            bool[] inputResult = { false };
            yield return WaitForPlayerInput(inputResult);

            SetAllInteractive(false);

            if (inputResult[0])
            {
                yield return HandleGameOver();
                yield break;
            }

            // 7. Ronda completada
            _state = State.RoundWon;
            _sessionScore += pointsPerRound * _seq.Round;
            _ui.SetStatus("¡Correcto! +1 color", new Color(0.96f, 0.78f, 0.18f));
            _audio.PlaySuccess();
            yield return new WaitForSeconds(1.10f);
        }
    }

    // ── Mostrar secuencia ─────────────────────────────────────────────────────

    IEnumerator ShowSequence()
    {
        for (int i = 0; i < _seq.Round; i++)
        {
            int colorIdx = _seq.GetStep(i);
            var btn      = _ui.Buttons[colorIdx];

            _audio.PlayColor(colorIdx);
            yield return btn.Flash(_stepDuration);
            yield return new WaitForSeconds(_stepGap);
        }
    }

    // ── Esperar input del jugador ─────────────────────────────────────────────

    IEnumerator WaitForPlayerInput(bool[] failedOut)
    {
        bool done   = false;
        bool failed = false;

        void OnInput(int colorIdx)
        {
            if (_state != State.PlayerTurn) return;
            if (done) return;

            bool correct = _seq.Submit(colorIdx, out bool roundComplete);

            // Flash visual del botón pulsado
            StartCoroutine(_ui.Buttons[colorIdx].PlayerPress(0.18f));

            if (!correct)
            {
                _audio.PlayFail();
                StartCoroutine(_ui.FlashError());
                failed = true;
                done   = true;
            }
            else
            {
                _audio.PlayColor(colorIdx);
                if (roundComplete)
                    done = true;
            }
        }

        foreach (var btn in _ui.Buttons)
            btn.OnPressed += OnInput;

        while (!done)
            yield return null;

        foreach (var btn in _ui.Buttons)
            btn.OnPressed -= OnInput;

        failedOut[0] = failed;
    }

    // ── Game Over ─────────────────────────────────────────────────────────────

    IEnumerator HandleGameOver()
    {
        _state = State.GameOver;
        _ui.SetStatus("¡Oh no! Secuencia incorrecta", new Color(0.90f, 0.22f, 0.28f));

        // Actualizar récord
        bool newRecord = false;
        if (_seq.Round > _record)
        {
            _record   = _seq.Round;
            newRecord = true;
            PlayerPrefs.SetInt(PREF_RECORD_Runtime, _record);
            PlayerPrefs.Save();
        }
        _ui.SetRecord(_record);

        // Registrar puntuación (MinigameBase la pasa al GameManager global)
        CompleteMinigame(_sessionScore);

        yield return new WaitForSeconds(0.80f);
        _ui.ShowResult(newRecord, _seq.Round, _record);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Helpers
    // ═════════════════════════════════════════════════════════════════════════

    void SetAllInteractive(bool value)
    {
        foreach (var btn in _ui.Buttons)
            btn.SetInteractive(value);
    }

    void HandleButtonPressed(int colorIdx)
    {
        // Los botones sólo son interactivos en PlayerTurn
        // (SetInteractive(false) los bloquea fuera de ese estado)
    }

    void ApplyDifficulty()
    {
        DifficultyLevel diff = GameManager.Instance != null
            ? GameManager.Instance.CurrentDifficulty
            : DifficultyLevel.Easy;

        switch (diff)
        {
            case DifficultyLevel.Medium:
                _stepDuration       = stepDurationMedium;
                _stepGap            = stepGapMedium;
                _previewPause       = previewPauseMedium;
                PREF_RECORD_Runtime = "simon_record_medium";
                break;
            case DifficultyLevel.Hard:
                _stepDuration       = stepDurationHard;
                _stepGap            = stepGapHard;
                _previewPause       = previewPauseHard;
                PREF_RECORD_Runtime = "simon_record_hard";
                break;
            default:
                _stepDuration       = stepDurationEasy;
                _stepGap            = stepGapEasy;
                _previewPause       = previewPauseEasy;
                PREF_RECORD_Runtime = PREF_RECORD;
                break;
        }
    }
}
