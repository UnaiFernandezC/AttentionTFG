// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Memoria de Ruta con capa de planificacion:
/// 1) Memoriza la ruta iluminada.  2) Aparecen muros sobre la ruta (0/1/2 segun
/// dificultad).  3) Muevete casilla a casilla hasta la META desviandote lo
/// minimo.  3 rondas; se gana con 2 o mas rutas completadas.
/// </summary>
public class PathMemoryGameManager : MinigameBase
{
    const int ROUNDS           = 3;
    const int MAX_ROUND_ERRORS = 3;

    [Header("Memorizacion en segundos (fallback si no hay GameManager)")]
    [SerializeField] float displaySecondsLevel1 = 5f;   // Easy
    [SerializeField] float displaySecondsLevel2 = 4f;   // Medium
    [SerializeField] float displaySecondsLevel3 = 3f;   // Hard

    // --- Config por dificultad (ApplyDifficulty) ---
    int   _gridSize     = 4;
    int   _routeCells   = 4;
    int   _blockedCount = 0;
    float _memoSeconds  = 5f;

    PathMemoryGridManager  _grid;
    PathMemoryPathManager  _pathMgr;
    PathMemoryPlayerInput  _input;
    PathMemoryUIController _ui;

    List<Vector2Int> _route;
    List<Vector2Int> _blocked;
    int   _round;
    int   _optimal;
    int   _roundErrors;
    int   _roundsWon;
    int   _totalMoves;
    int   _totalOptimal;
    int   _totalErrors;
    bool  _roundActive;
    float _lastMoveTime;

    readonly Dictionary<Vector2Int, PathMemoryGridManager.CellState> _painted = new();
    readonly Dictionary<Vector2Int, string>                          _labels  = new();

    static readonly Color GREEN  = new Color(0.20f, 0.90f, 0.50f);
    static readonly Color YELLOW = new Color(1.00f, 0.85f, 0.20f);
    static readonly Color RED    = new Color(0.95f, 0.30f, 0.30f);
    static Color BLUE => IntroPanel.CategoryColor("Planificacion");

    protected override void Start()
    {
        minigameName = "Memoria de Ruta";
        category     = MinigameCategory.Planning;
        base.Start();
    }

    protected override string GetIntroDescription() =>
        "Memoriza el camino iluminado. Despues apareceran muros:\n" +
        "llega a la META desviandote lo minimo.";

    protected override void OnMinigameComplete() { }
    protected override void OnMinigameFailed()   { }

    protected override void OnMinigameStart()
    {
        _grid    = gameObject.AddComponent<PathMemoryGridManager>();
        _pathMgr = gameObject.AddComponent<PathMemoryPathManager>();
        _input   = gameObject.AddComponent<PathMemoryPlayerInput>();
        _ui      = gameObject.AddComponent<PathMemoryUIController>();

        ApplyDifficulty();

        _ui.Init(OnReiniciarRonda);
        _ui.BuildHUD();

        _grid.CellClicked += OnCellClicked;

        _roundsWon    = 0;
        _totalMoves   = 0;
        _totalOptimal = 0;
        _totalErrors  = 0;

        StartRound(0);
    }

    void ApplyDifficulty()
    {
        var diff = GameManager.Instance != null
            ? GameManager.Instance.CurrentDifficulty
            : DifficultyLevel.Easy;

        switch (diff)
        {
            case DifficultyLevel.Medium:
                _gridSize = 5; _routeCells = 6; _blockedCount = 1; _memoSeconds = 4f;
                break;
            case DifficultyLevel.Hard:
                _gridSize = 6; _routeCells = 8; _blockedCount = 2; _memoSeconds = 3f;
                break;
            default:
                _gridSize = 4; _routeCells = 4; _blockedCount = 0; _memoSeconds = 5f;
                break;
        }

        if (GameManager.Instance == null)
        {
            float f = diff == DifficultyLevel.Hard   ? displaySecondsLevel3 :
                      diff == DifficultyLevel.Medium ? displaySecondsLevel2 :
                                                       displaySecondsLevel1;
            if (f > 0.5f) _memoSeconds = f;
        }
    }

    // ------------------------------------------------------------------ rondas

    void StartRound(int r)
    {
        StopAllCoroutines();
        _round       = r;
        _roundErrors = 0;
        _roundActive = false;
        _painted.Clear();
        _labels.Clear();

        if (_grid.GridCanvas != null)
            Destroy(_grid.GridCanvas.gameObject);
        _grid.BuildGrid(_gridSize, _gridSize);
        _grid.SetInputEnabled(false);

        _route   = _pathMgr.GetRoute(_gridSize, _routeCells);
        _blocked = _pathMgr.PickBlockedCells(_route, _blockedCount, _gridSize);
        _optimal = _pathMgr.ShortestPath(_route[0], _route[_route.Count - 1], _blocked, _gridSize);
        if (_optimal < 0) _optimal = _route.Count - 1;

        _input.Init(_route, _blocked);

        _ui.HideCountdown();
        _ui.HideProgress();

        StartCoroutine(RoundFlow());
    }

    IEnumerator RoundFlow()
    {
        // --- Fase 1: memorizar la ruta ---
        _ui.SetBannerText($"RONDA {_round + 1} / {ROUNDS}  —  ¡MEMORIZA!", GREEN);
        for (int i = 0; i < _route.Count; i++)
        {
            if      (i == 0)                Paint(_route[i], PathMemoryGridManager.CellState.Start, "S");
            else if (i == _route.Count - 1) Paint(_route[i], PathMemoryGridManager.CellState.Goal,  "M");
            else                            Paint(_route[i], PathMemoryGridManager.CellState.Route, i.ToString());
        }
        GameFeel.PlayPop();

        float t = 0f;
        while (t < _memoSeconds)
        {
            _ui.ShowCountdown(Mathf.CeilToInt(_memoSeconds - t));
            yield return null;
            t += Time.deltaTime;
        }
        _ui.HideCountdown();

        // --- Fase 2: ocultar la ruta y revelar los muros ---
        for (int i = 1; i < _route.Count - 1; i++)
            Paint(_route[i], PathMemoryGridManager.CellState.Normal);

        yield return new WaitForSeconds(0.4f);

        if (_blocked.Count > 0)
        {
            _ui.SetBannerText(_blocked.Count == 1 ? "¡MURO SORPRESA!" : "¡MUROS SORPRESA!", YELLOW);
            foreach (var b in _blocked)
                Paint(b, PathMemoryGridManager.CellState.Blocked, "X");
            GameFeel.PlayError();
            GameFeel.Shake(_grid.GridRoot, 10f, 0.3f);
            yield return new WaitForSeconds(1.1f);
        }

        // --- Fase 3: turno del jugador ---
        _ui.SetBannerText("¡TU TURNO! Llega a la META", BLUE);
        Paint(_route[0],                PathMemoryGridManager.CellState.PlayerCurrent, "TU");
        Paint(_route[_route.Count - 1], PathMemoryGridManager.CellState.Goal,          "M");
        _ui.ShowProgress(0, _optimal);

        _input.SetActive(true);
        _grid.SetInputEnabled(true);
        _roundActive  = true;
        _lastMoveTime = Time.realtimeSinceStartup;
    }

    // ------------------------------------------------------------------ input

    void OnCellClicked(Vector2Int pos)
    {
        if (!_roundActive) return;

        float rtMs      = (Time.realtimeSinceStartup - _lastMoveTime) * 1000f;
        Vector2Int prev = _input.Current;
        var result      = _input.TryMove(pos);

        if (result == PathMemoryPlayerInput.MoveResult.Inactive) return;
        _lastMoveTime = Time.realtimeSinceStartup;

        if (result == PathMemoryPlayerInput.MoveResult.Invalid)
        {
            _roundErrors++;
            _totalErrors++;
            ReportEvent(false, rtMs);
            GameFeel.PlayError();
            StartCoroutine(FlashWrong(pos));
            CheckRoundFail();
            return;
        }

        // Movimiento valido: ¿acerca a la meta por el camino optimo?
        int prevDist = _pathMgr.ShortestPath(prev, _input.Goal, _blocked, _gridSize);
        int newDist  = _pathMgr.ShortestPath(pos,  _input.Goal, _blocked, _gridSize);
        bool closer  = newDist >= 0 && (prevDist < 0 || newDist < prevDist);
        ReportEvent(closer, rtMs);

        // Pintar rastro en la casilla que dejamos
        if (prev == _route[0])
            Paint(prev, PathMemoryGridManager.CellState.Start, "S");
        else
            Paint(prev, _input.IsOnRoute(prev)
                ? PathMemoryGridManager.CellState.PlayerCorrect
                : PathMemoryGridManager.CellState.Detour, "");

        if (result == PathMemoryPlayerInput.MoveResult.Goal)
        {
            Paint(pos, PathMemoryGridManager.CellState.PlayerCorrect, "M");
            HandleGoal();
            return;
        }

        Paint(pos, PathMemoryGridManager.CellState.PlayerCurrent, "TU");
        GameFeel.PlayPop();
        _ui.ShowProgress(_input.Moves, _optimal);

        if (!closer)
        {
            _roundErrors++;
            _totalErrors++;
            _ui.SetBannerText("Te alejas de la META...", YELLOW);
            CheckRoundFail();
        }
    }

    void CheckRoundFail()
    {
        if (_roundErrors <= MAX_ROUND_ERRORS) return;

        _roundActive = false;
        _input.SetActive(false);
        _grid.SetInputEnabled(false);
        StartCoroutine(RoundLost());
    }

    IEnumerator RoundLost()
    {
        _ui.SetBannerText("¡DEMASIADOS DESPISTES!", RED);
        GameFeel.ScreenFlash(RED, 0.18f, 0.3f);
        yield return new WaitForSeconds(1.4f);
        NextRound();
    }

    void HandleGoal()
    {
        _roundActive = false;
        _input.SetActive(false);
        _grid.SetInputEnabled(false);

        _roundsWon++;
        _totalMoves   += _input.Moves;
        _totalOptimal += _optimal;

        GameFeel.PlaySuccess();
        GameFeel.Confetti(25);
        bool perfect = _input.Moves <= _optimal;
        _ui.SetBannerText(perfect ? "¡META! ¡CAMINO PERFECTO!" : "¡META!", GREEN);
        if (perfect) GameFeel.PlayStar();

        StartCoroutine(GoalDelayed());
    }

    IEnumerator GoalDelayed()
    {
        yield return new WaitForSeconds(1.2f);
        NextRound();
    }

    void NextRound()
    {
        if (_round >= ROUNDS - 1) FinishGame();
        else                      StartRound(_round + 1);
    }

    // ------------------------------------------------------------------ final

    void FinishGame()
    {
        _ui.HideProgress();

        bool  success = _roundsWon >= 2;
        float eff     = _totalMoves > 0
            ? Mathf.Clamp01((float)_totalOptimal / _totalMoves)
            : 0f;
        int effPct = Mathf.RoundToInt(eff * 100f);
        float ratio = (_roundsWon / (float)ROUNDS) * 0.5f + eff * 0.5f;

        var stats = new[]
        {
            "Rutas completadas: " + _roundsWon + " / " + ROUNDS,
            "Eficiencia: " + effPct + "%",
            "Despistes: " + _totalErrors
        };

        if (success)
        {
            int score = _roundsWon * 200 + Mathf.RoundToInt(eff * 400f);
            CompleteMinigame(score);
            ShowResults(true, GameFeel.StarsFromRatio(true, ratio), score, stats,
                _totalErrors == 0 ? "¡Memoria de mapa!" : null,
                _totalErrors == 0 ? "Ni un solo despiste" : null);
        }
        else
        {
            FailMinigame();
            ShowResults(false, 0, 0, stats,
                "¡Casi!",
                "Mira bien la ruta antes de moverte");
        }
    }

    // ------------------------------------------------------------------ utils

    void OnReiniciarRonda()
    {
        if (!IsPlaying) return;
        StartRound(_round);
    }

    void Paint(Vector2Int pos, PathMemoryGridManager.CellState state, string label = "")
    {
        _grid.SetCellState(pos, state, label);
        _painted[pos] = state;
        _labels[pos]  = label;
    }

    IEnumerator FlashWrong(Vector2Int pos)
    {
        var prevState = _painted.TryGetValue(pos, out var st)
            ? st : PathMemoryGridManager.CellState.Normal;
        var prevLabel = _labels.TryGetValue(pos, out var lb) ? lb : "";

        _grid.SetCellState(pos, PathMemoryGridManager.CellState.PlayerWrong, "X");
        yield return new WaitForSeconds(0.35f);
        _grid.SetCellState(pos, prevState, prevLabel);
    }

    void OnDestroy()
    {
        if (_grid != null) _grid.CellClicked -= OnCellClicked;
    }
}
