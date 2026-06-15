using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathMemoryGameManager : MinigameBase
{
    static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

    PathMemoryGridManager  _grid;
    PathMemoryPathManager  _pathMgr;
    PathMemoryPlayerInput  _input;
    PathMemoryUIController _ui;

    List<Vector2Int>              _route;
    PathMemoryPathManager.LevelConfig _cfg;
    int  _currentLevel = 0;
    bool _gameActive   = false;

    [Header("Tiempo de memorización por nivel (segundos)")]
    [SerializeField] float displaySecondsLevel1 = 5f;
    [SerializeField] float displaySecondsLevel2 = 4f;
    [SerializeField] float displaySecondsLevel3 = 3f;

    protected override void Start()
    {
        minigameName = "Memoria de Ruta";
        category     = MinigameCategory.Planning;
        base.Start();
    }

    protected override string GetIntroDescription() =>
        "Observa las casillas que se iluminan en el tablero.\n\n" +
        "Cuando se apaguen, tócalas en el mismo orden.\n\n" +
        "Empieza por la casilla AZUL y termina en la DORADA.";

    protected override void OnMinigameStart()
    {

        _grid    = gameObject.AddComponent<PathMemoryGridManager>();
        _pathMgr = gameObject.AddComponent<PathMemoryPathManager>();
        _input   = gameObject.AddComponent<PathMemoryPlayerInput>();
        _ui      = gameObject.AddComponent<PathMemoryUIController>();

        _ui.Init(OnReiniciar, OnSalir);
        _ui.BuildHUD();

        _input.OnCorrectStep  += HandleCorrectStep;
        _input.OnWrongStep    += HandleWrongStep;
        _input.OnRouteComplete += HandleVictory;

        _currentLevel = 0;
        StartLevel(_currentLevel);
    }

    void StartLevel(int level)
    {
        _cfg = _pathMgr.GetConfig(level);

        float[] inspectorTimes = { displaySecondsLevel1, displaySecondsLevel2, displaySecondsLevel3 };
        _cfg = new PathMemoryPathManager.LevelConfig
            { gridSize = _cfg.gridSize, displaySeconds = inspectorTimes[Mathf.Clamp(level, 0, 2)] };
        _route = _pathMgr.GetRoute(level);

        if (_grid != null && _grid.GridCanvas != null)
            Destroy(_grid.GridCanvas.gameObject);

        _grid.BuildGrid(_cfg.gridSize, _cfg.gridSize);
        _grid.CellClicked += OnCellClicked;

        _ui.HideCountdown();
        _ui.HideProgress();

        _input.Init(_route);

        _gameActive = true;
        StartCoroutine(Phase1ShowRoute());
    }

    IEnumerator Phase1ShowRoute()
    {
        _ui.SetBannerText($"NIVEL {_currentLevel + 1} / {PathMemoryPathManager.TotalLevels}" +
                          "  —  ¡MEMORIZA!", C(0.20f, 0.90f, 0.50f));
        _grid.SetInputEnabled(false);

        for (int i = 0; i < _route.Count; i++)
        {
            if      (i == 0)                  _grid.SetCellState(_route[i], PathMemoryGridManager.CellState.Start, "S");
            else if (i == _route.Count - 1)   _grid.SetCellState(_route[i], PathMemoryGridManager.CellState.Goal,  "M");
            else                               _grid.SetCellState(_route[i], PathMemoryGridManager.CellState.Route, i.ToString());
        }

        float elapsed = 0f;
        float total   = _cfg.displaySeconds;
        while (elapsed < total)
        {
            _ui.ShowCountdown(Mathf.CeilToInt(total - elapsed));
            yield return null;
            elapsed += Time.deltaTime;
        }
        _ui.HideCountdown();

        StartCoroutine(Phase2HideRoute());
    }

    IEnumerator Phase2HideRoute()
    {
        _ui.SetBannerText("¡PREPARADO!", C(1.00f, 0.85f, 0.20f));

        for (int i = 1; i < _route.Count - 1; i++)
            _grid.SetCellState(_route[i], PathMemoryGridManager.CellState.Normal);

        yield return new WaitForSeconds(0.8f);
        StartCoroutine(Phase3PlayerTurn());
    }

    IEnumerator Phase3PlayerTurn()
    {
        _ui.SetBannerText("¡TU TURNO! Toca las casillas en orden", C(0.40f, 0.70f, 1.00f));
        _ui.ShowProgress(_input.CompletedSteps, _input.TotalSteps);

        _input.Init(_route);
        _input.SetActive(true);
        _grid.SetInputEnabled(true);

        _grid.SetCellState(_route[0],              PathMemoryGridManager.CellState.Start, "S");
        _grid.SetCellState(_route[_route.Count-1], PathMemoryGridManager.CellState.Goal,  "M");

        yield break;
    }

    void OnCellClicked(Vector2Int pos)
    {
        if (!_gameActive) return;
        _input.HandleCellClick(pos);
    }

    void HandleCorrectStep(Vector2Int pos, int routeIdx)
    {
        _grid.SetCellState(pos, PathMemoryGridManager.CellState.PlayerCorrect, "");
        _ui.ShowProgress(_input.CompletedSteps, _input.TotalSteps);
    }

    void HandleWrongStep(Vector2Int pos)
    {
        _grid.SetCellState(pos, PathMemoryGridManager.CellState.PlayerWrong, "X");
        _grid.SetInputEnabled(false);
        _gameActive = false;
        StartCoroutine(ShowDefeatDelayed());
    }

    IEnumerator ShowDefeatDelayed()
    {
        _ui.SetBannerText("¡RUTA INCORRECTA!", C(0.95f, 0.30f, 0.30f));
        yield return new WaitForSeconds(0.9f);
        _ui.HideProgress();
        _ui.ShowResult(
            win:         false,
            title:       "¡RUTA INCORRECTA!",
            subtitle:    "No te preocupes, ¡inténtalo de nuevo!",
            retryLabel:  "REINTENTAR",
            onRetry:     OnReiniciar,
            onMenu:      OnSalir);
    }

    void HandleVictory()
    {
        _grid.SetInputEnabled(false);
        _gameActive = false;
        _grid.SetCellState(_route[_route.Count-1], PathMemoryGridManager.CellState.PlayerCorrect, "");
        StartCoroutine(ShowVictoryDelayed());
    }

    IEnumerator ShowVictoryDelayed()
    {
        yield return new WaitForSeconds(0.9f);
        _ui.HideProgress();

        bool isLastLevel = (_currentLevel >= PathMemoryPathManager.TotalLevels - 1);

        if (isLastLevel)
        {
            _ui.SetBannerText("¡JUEGO COMPLETADO!", C(0.92f, 0.78f, 0.06f));
            _ui.ShowResult(
                win:        true,
                title:      "¡JUEGO COMPLETADO!",
                subtitle:   "¡Increíble! Has completado los 3 niveles. ¡Eres un campeón!",
                retryLabel: "JUGAR DE NUEVO",
                onRetry:    OnJugarDeNuevo,
                onMenu:     OnSalir);
        }
        else
        {
            _ui.SetBannerText($"¡NIVEL {_currentLevel + 1} COMPLETADO!", C(0.20f, 0.90f, 0.50f));
            _ui.ShowResult(
                win:        true,
                title:      $"¡NIVEL {_currentLevel + 1} COMPLETADO!",
                subtitle:   "¡Excelente memoria! ¿Listo para el siguiente?",
                retryLabel: "SIGUIENTE NIVEL",
                onRetry:    OnSiguienteNivel,
                onMenu:     OnSalir);
        }
    }

    void OnReiniciar()
    {
        _ui.ClearResult();
        StopAllCoroutines();
        _gameActive = false;

        _grid.ResetAllCells();
        _grid.SetInputEnabled(false);
        _input.Reset();
        _ui.HideCountdown();
        _ui.HideProgress();

        _route = _pathMgr.GetRoute(_currentLevel);
        _input.Init(_route);

        _gameActive = true;
        StartCoroutine(Phase1ShowRoute());
    }

    void OnSiguienteNivel()
    {
        _ui.ClearResult();
        StopAllCoroutines();
        _gameActive = false;

        _grid.CellClicked -= OnCellClicked;

        _currentLevel++;
        StartLevel(_currentLevel);
    }

    void OnJugarDeNuevo()
    {
        _ui.ClearResult();
        StopAllCoroutines();
        _gameActive = false;

        _grid.CellClicked -= OnCellClicked;

        _currentLevel = 0;
        StartLevel(_currentLevel);
    }

    void OnSalir()
    {
        StopAllCoroutines();
        ReturnToGameSelector();
    }

    protected override void OnMinigameComplete() { }
    protected override void OnMinigameFailed()   { }
}
