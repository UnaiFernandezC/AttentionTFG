// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Controlador principal del minijuego Camino Laser (Atencion).
///
/// El jugador rota espejos para redirigir un rayo laser hasta el objetivo.
/// SIEMPRE se juegan los 3 puzzles antes de decidir si se gano o perdio.
/// - Facil  : 3 puzzles (5x5), 45 s, ganar 2/3.
/// - Medio  : 3 puzzles (6x6), 30 s, ganar 2/3.
/// - Dificil: 3 puzzles (7x7), 35 s, ganar 3/3.
/// </summary>
[RequireComponent(typeof(LaserUIController))]
public class LaserGameManager : MinigameBase
{
    [Header("Dificultad (se sobreescribe con GameManager)")]
    [SerializeField] private int puzzlesNeededToWin = 2;
    [SerializeField] private int totalPuzzles       = 3;

    // --- Estado ----------------------------------------------------------------
    LaserUIController _ui;
    LaserGridManager  _grid;

    LaserLevelData[] _levels;
    int              _currentPuzzle;
    int              _puzzlesSolved;

    float _timeLeft;
    bool  _timerRunning;
    bool  _puzzleSolved;
    bool  _gameEnded;
    float _puzzleStartTime;
    float _rtSumMs;
    int   _rtCount;

    // --- MinigameBase ----------------------------------------------------------
    protected override string GetIntroDescription() =>
        "Haz clic en los espejos NARANJAS ( / o \\ ) para girarlos.\n" +
        "Guia el laser amarillo hasta la casilla roja META.\n\n" +
        "Todos los espejos empiezan en posicion incorrecta.\nTienes que girarlos todos!";

    protected override void OnMinigameStart()
    {
        ApplyDifficulty();
        EnsureEventSystem();

        _ui   = GetComponent<LaserUIController>();
        _grid = new LaserGridManager();

        _puzzlesSolved = 0;
        _currentPuzzle = 0;
        _gameEnded     = false;

        var first = _levels[0];
        _ui.BuildUI(first.rows, first.cols, RestartMinigame, ReturnToGameSelector);
        _ui.OnCellClicked += HandleCellClick;

        LoadPuzzle(_currentPuzzle);
    }

    protected override void OnMinigameComplete() { }
    protected override void OnMinigameFailed()   { }

    // --- Carga de puzzle -------------------------------------------------------
    void LoadPuzzle(int index)
    {
        _puzzleSolved = false;
        _timerRunning = false;
        _ui.HideWinFlash();

        var data  = _levels[index];
        _timeLeft = data.timeLimit;

        _grid.LoadLevel(data);
        _ui.RefreshGrid(_grid);
        _ui.SetPuzzleLabel(index + 1, totalPuzzles);
        _ui.SetHint(data.hint);
        _ui.SetTimer(_timeLeft, data.timeLimit);

        _puzzleStartTime = Time.time;
        _timerRunning    = true;
    }

    // --- Interaccion -----------------------------------------------------------
    void HandleCellClick(int row, int col)
    {
        if (!IsPlaying || _gameEnded || _puzzleSolved) return;

        bool rotated = _grid.ToggleMirror(row, col);
        if (!rotated) return;

        _ui.RefreshGrid(_grid);
        _ui.FlashMirrorClick(row, col);

        if (_grid.LaserReachedTarget)
        {
            _puzzleSolved = true;
            _timerRunning = false;
            _puzzlesSolved++;

            // Telemetria: tiempo real de resolucion del puzzle
            float solveMs = (Time.time - _puzzleStartTime) * 1000f;
            ReportEvent(true, solveMs);
            _rtSumMs += solveMs;
            _rtCount++;

            GameFeel.PlaySuccess();
            GameFeel.Confetti(25);
            _ui.ShowWinFlash("Muy bien! Laser llegado a META");
            StartCoroutine(AdvancePuzzle());
        }
    }

    IEnumerator AdvancePuzzle()
    {
        yield return new WaitForSeconds(1.4f);
        _ui.HideWinFlash();

        _currentPuzzle++;

        // SIEMPRE jugar todos los puzzles antes de terminar
        if (_currentPuzzle >= totalPuzzles)
        {
            EndGame();
        }
        else
        {
            LoadPuzzle(_currentPuzzle);
        }
    }

    // --- Timer -----------------------------------------------------------------
    void Update()
    {
        if (!IsPlaying || _gameEnded || !_timerRunning) return;

        _timeLeft -= Time.deltaTime;
        _ui.SetTimer(Mathf.Max(0f, _timeLeft), _levels[_currentPuzzle].timeLimit);

        if (_timeLeft <= 0f)
        {
            _timerRunning  = false;

            // Puzzle no resuelto a tiempo
            ReportEvent(false, -1f);
            GameFeel.PlayError();

            _currentPuzzle++;

            if (_currentPuzzle >= totalPuzzles)
            {
                EndGame();
            }
            else
            {
                LoadPuzzle(_currentPuzzle);
            }
        }
    }

    // --- Fin del juego ---------------------------------------------------------
    void EndGame()
    {
        if (_gameEnded) return;
        _gameEnded = true;

        bool won   = _puzzlesSolved >= puzzlesNeededToWin;
        int  score = CalculateScore(won);

        if (won) CompleteMinigame(score);
        else     FailMinigame();

        float ratio = totalPuzzles > 0 ? (float)_puzzlesSolved / totalPuzzles : 0f;
        int   stars = GameFeel.StarsFromRatio(won, ratio);

        string timeStat = _rtCount > 0
            ? "Media por puzzle: " + (_rtSumMs / _rtCount / 1000f).ToString("0.0") + " s"
            : "Media por puzzle: -";

        ShowResults(won, stars, score,
            new[] { "Puzzles resueltos: " + _puzzlesSolved + "/" + totalPuzzles, timeStat },
            null,
            won ? "¡Dominas los espejos!" : "Sigue el rayo con el dedo antes de girar");
    }

    int CalculateScore(bool won)
    {
        if (!won) return Mathf.Max(0, _puzzlesSolved * 120);
        int baseScore  = 500;
        int solveBonus = _puzzlesSolved * 200;
        int speedBonus = Mathf.RoundToInt(_timeLeft * 12f);
        return baseScore + solveBonus + Mathf.Max(0, speedBonus);
    }

    // --- Dificultad ------------------------------------------------------------
    void ApplyDifficulty()
    {
        var diff = GameManager.Instance != null
            ? GameManager.Instance.CurrentDifficulty
            : DifficultyLevel.Easy;

        switch (diff)
        {
            case DifficultyLevel.Easy:
                _levels            = LaserLevels.Easy;
                totalPuzzles       = _levels.Length;
                puzzlesNeededToWin = 2;
                break;

            case DifficultyLevel.Medium:
                _levels            = LaserLevels.Medium;
                totalPuzzles       = _levels.Length;
                puzzlesNeededToWin = 2;
                break;

            case DifficultyLevel.Hard:
                _levels            = LaserLevels.Hard;
                totalPuzzles       = _levels.Length;
                puzzlesNeededToWin = 3;
                break;
        }

        if (GameManager.Instance == null)
        {
            _levels            = LaserLevels.Easy;
            totalPuzzles       = _levels.Length;
            puzzlesNeededToWin = 2;
        }
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
