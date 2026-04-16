using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Minijuego "Ruta optima" — Planificacion.
///
/// 3 rondas secuenciales. Cada ronda el jugador debe ir de INICIO a META
/// en el menor numero de pasos posible.
///
/// Ronda 1: grid pequeño sin obstaculos  (muy facil)
/// Ronda 2: grid mediano con pocos obstaculos
/// Ronda 3: grid mayor con mas obstaculos
///
/// Todo configurable desde el Inspector.
/// </summary>
public class OptimalPathController : MinigameBase
{
    // ─── Inspector ────────────────────────────────────────────────────────

    [Header("Ronda 1 - Muy facil")]
    public int cols1 = 4;
    public int rows1 = 4;
    public int obs1  = 0;

    [Header("Ronda 2 - Media")]
    public int cols2 = 5;
    public int rows2 = 5;
    public int obs2  = 3;

    [Header("Ronda 3 - Complicada")]
    public int cols3 = 6;
    public int rows3 = 6;
    public int obs3  = 7;

    [Header("Semilla aleatoria (0 = distinto cada vez)")]
    public int randomSeed = 0;

    // ─── Estado global de sesion ──────────────────────────────────────────

    const int ROUNDS = 3;
    int _round;          // 0-based
    int _totalSteps;
    int _totalOptimal;

    // ─── Estado de ronda ──────────────────────────────────────────────────

    int    _cols, _rows, _numObs;
    bool[] _blocked;
    bool[] _visited;
    int    _startIdx, _goalIdx, _playerIdx;
    int    _steps, _optimal;
    bool   _roundOver;

    // ─── UI ───────────────────────────────────────────────────────────────

    GameObject        _gridGO;
    Image[]           _cellBg;
    TextMeshProUGUI[] _cellLbl;
    Button[]          _cellBtn;

    TextMeshProUGUI _stepsVal;
    TextMeshProUGUI _optVal;
    TextMeshProUGUI _statusLbl;
    TextMeshProUGUI _roundLbl;
    Image[]         _dots;

    GameObject      _transPanel;
    TextMeshProUGUI _transTitle;
    TextMeshProUGUI _transSub;

    GameObject      _endPanel;
    Image           _endBar;
    TextMeshProUGUI _endTitle;
    TextMeshProUGUI _endSub;

    // ─── Colores ──────────────────────────────────────────────────────────

    static readonly Color BG     = Hex(0.08f,0.09f,0.18f);
    static readonly Color PANEL  = Hex(0.12f,0.13f,0.24f);
    static readonly Color HDR    = Hex(0.10f,0.11f,0.22f);
    static readonly Color ACCENT = Hex(0.25f,0.55f,1.00f);
    static readonly Color GREEN  = Hex(0.20f,0.78f,0.48f);
    static readonly Color RED    = Hex(0.85f,0.25f,0.32f);
    static readonly Color YELLOW = Hex(1.00f,0.84f,0.22f);
    static readonly Color DIM    = Hex(0.55f,0.58f,0.75f);
    static readonly Color GREY   = Hex(0.28f,0.30f,0.42f);
    static readonly Color DOTOFF = Hex(0.25f,0.27f,0.45f);

    // Colores de celda
    static readonly Color CN = Hex(0.20f,0.22f,0.38f); // normal
    static readonly Color CB = Hex(0.07f,0.07f,0.12f); // bloqueada
    static readonly Color CS = Hex(0.22f,0.80f,0.50f); // inicio
    static readonly Color CG = Hex(0.88f,0.26f,0.32f); // meta
    static readonly Color CP = Hex(0.25f,0.70f,1.00f); // jugador
    static readonly Color CV = Hex(0.28f,0.32f,0.55f); // visitada
    static readonly Color CA = Hex(0.32f,0.38f,0.65f); // adyacente valida

    static Color Hex(float r, float g, float b) { return new Color(r, g, b); }

    // ═════════════════════════════════════════════════════════════════════
    //  MINIGAME BASE
    // ═════════════════════════════════════════════════════════════════════

    protected override void OnMinigameStart()
    {
        EnsureES();
        _round        = 0;
        _totalSteps   = 0;
        _totalOptimal = 0;
        BuildUI();
        StartRound(0);
    }

    protected override void OnMinigameComplete() { }
    protected override void OnMinigameFailed()   { }

    // ═════════════════════════════════════════════════════════════════════
    //  GESTION DE RONDAS
    // ═════════════════════════════════════════════════════════════════════

    void RoundConfig(int r, out int c, out int rw, out int obs)
    {
        if      (r == 0) { c = cols1; rw = rows1; obs = obs1; }
        else if (r == 1) { c = cols2; rw = rows2; obs = obs2; }
        else             { c = cols3; rw = rows3; obs = obs3; }
    }

    void StartRound(int r)
    {
        _round    = r;
        _roundOver = false;

        RoundConfig(r, out _cols, out _rows, out _numObs);

        // Semilla diferente por ronda para mapas distintos
        int s = randomSeed != 0 ? randomSeed + r : System.Environment.TickCount;
        Random.InitState(s);

        GenerateGrid();
        RebuildCells();     // destruye y recrea las celdas en el GridLayoutGroup

        if (_transPanel != null) _transPanel.SetActive(false);
        if (_endPanel   != null) _endPanel.SetActive(false);

        UpdateRoundUI();
        RefreshCells();
    }

    void ResetRound()
    {
        StopAllCoroutines();
        if (_transPanel != null) _transPanel.SetActive(false);
        if (_endPanel   != null) _endPanel.SetActive(false);
        // Regenerar la misma ronda con la misma semilla
        StartRound(_round);
    }

    // ═════════════════════════════════════════════════════════════════════
    //  GENERACION DEL GRID
    // ═════════════════════════════════════════════════════════════════════

    void GenerateGrid()
    {
        int total  = _cols * _rows;
        _blocked   = new bool[total];
        _visited   = new bool[total];
        _startIdx  = 0;
        _goalIdx   = total - 1;
        _playerIdx = _startIdx;
        _steps     = 0;

        // Colocar obstaculos garantizando que siempre haya camino
        int placed = 0, tries = 0;
        while (placed < _numObs && tries < 3000)
        {
            tries++;
            int idx = Random.Range(0, total);
            if (idx == _startIdx || idx == _goalIdx || _blocked[idx]) continue;
            _blocked[idx] = true;
            if (BFS(_startIdx, _goalIdx) < 0)
                _blocked[idx] = false;
            else
                placed++;
        }

        _optimal           = BFS(_startIdx, _goalIdx);
        _visited[_startIdx] = true;
    }

    // ─── BFS para calcular distancia minima ───────────────────────────────

    int BFS(int from, int to)
    {
        if (from == to) return 0;
        int total = _cols * _rows;
        int[] dist = new int[total];
        for (int i = 0; i < total; i++) dist[i] = -1;
        Queue<int> q = new Queue<int>();
        q.Enqueue(from);
        dist[from] = 0;
        int[] dr = { -1, 1,  0, 0 };
        int[] dc = {  0, 0, -1, 1 };
        while (q.Count > 0)
        {
            int cur = q.Dequeue();
            int rr  = cur / _cols, cc = cur % _cols;
            for (int d = 0; d < 4; d++)
            {
                int nr = rr + dr[d], nc = cc + dc[d];
                if (nr < 0 || nr >= _rows || nc < 0 || nc >= _cols) continue;
                int ni = nr * _cols + nc;
                if (_blocked[ni] || dist[ni] >= 0) continue;
                dist[ni] = dist[cur] + 1;
                if (ni == to) return dist[ni];
                q.Enqueue(ni);
            }
        }
        return -1;
    }

    // ═════════════════════════════════════════════════════════════════════
    //  MOVIMIENTO
    // ═════════════════════════════════════════════════════════════════════

    void TryMove(int target)
    {
        if (_roundOver) return;
        int total = _cols * _rows;
        if (target < 0 || target >= total || _blocked[target]) return;

        int pr = _playerIdx / _cols, pc = _playerIdx % _cols;
        int tr = target    / _cols, tc = target    % _cols;
        if (Mathf.Abs(pr - tr) + Mathf.Abs(pc - tc) != 1) return;

        _playerIdx       = target;
        _steps++;
        _visited[target] = true;

        RefreshCells();
        StartCoroutine(PulseCell(target));

        if (_playerIdx == _goalIdx)
        {
            _roundOver = true;
            StartCoroutine(HandleRoundEnd());
        }
    }

    IEnumerator HandleRoundEnd()
    {
        _totalSteps   += _steps;
        _totalOptimal += _optimal;

        yield return new WaitForSeconds(0.5f);

        if (_round >= ROUNDS - 1)
        {
            // Todas las rondas completadas
            int score = Mathf.Max(100, 1000 - (_totalSteps - _totalOptimal) * 50);
            CompleteMinigame(score);
            ShowEnd();
        }
        else
        {
            StartCoroutine(Transition());
        }
    }

    IEnumerator Transition()
    {
        _transPanel.SetActive(true);
        _transTitle.text = "Ronda " + (_round + 1) + " completada!";
        _transSub.text   = "Pasos: " + _steps + "  (optimo: " + _optimal + ")";

        yield return new WaitForSeconds(1f);
        for (int i = 3; i >= 1; i--)
        {
            _transSub.text = "Siguiente ronda en " + i + "...";
            yield return new WaitForSeconds(1f);
        }

        _transPanel.SetActive(false);
        StartRound(_round + 1);
    }

    // ═════════════════════════════════════════════════════════════════════
    //  REFRESCO DE CELDAS
    // ═════════════════════════════════════════════════════════════════════

    void RefreshCells()
    {
        if (_cellBg == null || _cellBg.Length != _cols * _rows) return;

        int total = _cols * _rows;
        int pr    = _playerIdx / _cols, pc = _playerIdx % _cols;

        // Calcular adyacentes validas
        bool[] adj  = new bool[total];
        int[]  dr   = { -1, 1,  0, 0 };
        int[]  dc   = {  0, 0, -1, 1 };
        for (int d = 0; d < 4; d++)
        {
            int nr = pr + dr[d], nc = pc + dc[d];
            if (nr >= 0 && nr < _rows && nc >= 0 && nc < _cols)
            {
                int ni = nr * _cols + nc;
                if (!_blocked[ni]) adj[ni] = true;
            }
        }

        for (int i = 0; i < total; i++)
        {
            Color  col;
            string lbl = "";

            if (i == _playerIdx)
            {
                col = CP;
                lbl = (i == _goalIdx) ? "META!" : "TU";
            }
            else if (i == _goalIdx)
            {
                col = CG;
                lbl = "META";
            }
            else if (i == _startIdx)
            {
                col = _visited[i] ? CV : CS;
                lbl = "INICIO";
            }
            else if (_blocked[i])
            {
                col = CB;
            }
            else if (adj[i])
            {
                col = CA;
            }
            else if (_visited[i])
            {
                col = CV;
            }
            else
            {
                col = CN;
            }

            _cellBg[i].color         = col;
            _cellLbl[i].text         = lbl;
            _cellBtn[i].interactable = adj[i] && !_roundOver;
        }

        if (_stepsVal != null) _stepsVal.text = _steps.ToString();
        if (_optVal   != null) _optVal.text   = _optimal >= 0 ? _optimal.ToString() : "?";

        if (_statusLbl != null)
        {
            if (_roundOver)
                _statusLbl.text = EvalMsg();
            else if (_steps == 0)
                _statusLbl.text = "Haz clic en las casillas azules claras para moverte";
            else
                _statusLbl.text = "Bien! Sigue hacia META";
        }
    }

    string EvalMsg()
    {
        int extra = _steps - _optimal;
        if (extra == 0) return "Perfecto! Camino optimo!";
        if (extra <= 2) return "Muy bien! Casi perfecto!";
        if (extra <= 5) return "Bien! Puedes mejorar!";
        return "Llegaste! Intenta con menos pasos.";
    }

    IEnumerator PulseCell(int idx)
    {
        if (_cellBtn == null || idx >= _cellBtn.Length) yield break;
        RectTransform rt = _cellBtn[idx].GetComponent<RectTransform>();
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 14f;
            float s = 1f + 0.08f * Mathf.Sin(t * Mathf.PI);
            rt.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
        rt.localScale = Vector3.one;
    }

    void UpdateRoundUI()
    {
        if (_roundLbl != null)
            _roundLbl.text = "Ronda " + (_round + 1) + " / " + ROUNDS;

        if (_dots != null)
            for (int i = 0; i < _dots.Length; i++)
                _dots[i].color = i <= _round ? ACCENT : DOTOFF;
    }

    // ═════════════════════════════════════════════════════════════════════
    //  PANEL FIN
    // ═════════════════════════════════════════════════════════════════════

    void ShowEnd()
    {
        int extra  = _totalSteps - _totalOptimal;
        bool perf  = extra == 0;
        bool good  = extra <= 4;
        _endBar.color  = perf ? YELLOW : (good ? GREEN : ACCENT);
        _endTitle.text = perf ? "Camino optimo en todas!" : (good ? "Muy bien!" : "Completado!");
        _endSub.text   = "Pasos totales: " + _totalSteps +
                         "\nCamino optimo: " + _totalOptimal;
        _endPanel.SetActive(true);
    }

    // ═════════════════════════════════════════════════════════════════════
    //  CONSTRUCCION DE UI
    // ═════════════════════════════════════════════════════════════════════

    void BuildUI()
    {
        // Canvas
        GameObject cGO = new GameObject("Canvas");
        cGO.transform.SetParent(transform, false);
        Canvas cv = cGO.AddComponent<Canvas>();
        cv.renderMode   = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 10;
        CanvasScaler sc = cGO.AddComponent<CanvasScaler>();
        sc.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920f, 1080f);
        sc.matchWidthOrHeight  = 0.5f;
        cGO.AddComponent<GraphicRaycaster>();
        RectTransform R = cGO.GetComponent<RectTransform>();

        // Fondo
        MkImg(R, "BG", BG, V2(0,0), V2(1,1), V2(0,0), V2(0,0));

        // ── Header ──
        RectTransform hdr = MkImg(R, "Hdr", HDR, V2(0,1), V2(1,1), V2(0,-40), V2(0,80));
        MkImg(hdr, "HL", ACCENT, V2(0,0), V2(1,0), V2(0,1.5f), V2(0,3));

        var ht = MkTxt(hdr, "T", "Ruta optima", Color.white, 40, V2(0.03f,0), V2(0.50f,1));
        ht.fontStyle = FontStyles.Bold;
        ht.alignment = TextAlignmentOptions.MidlineLeft;

        // Indicador de ronda (centro-derecha del header)
        _roundLbl = MkTxt(hdr, "RL", "Ronda 1 / 3", DIM, 26, V2(0.52f,0), V2(0.78f,1));
        _roundLbl.alignment = TextAlignmentOptions.MidlineRight;

        // Dots de ronda (extremo derecho)
        _dots = new Image[ROUNDS];
        for (int i = 0; i < ROUNDS; i++)
        {
            int ii = i;
            GameObject dot = new GameObject("Dot" + i);
            dot.transform.SetParent(hdr, false);
            RectTransform drt = dot.AddComponent<RectTransform>();
            drt.anchorMin        = new Vector2(1f, 0.5f);
            drt.anchorMax        = new Vector2(1f, 0.5f);
            drt.pivot            = new Vector2(0.5f, 0.5f);
            drt.anchoredPosition = new Vector2(-45f - (ROUNDS - 1 - i) * 26f, 0f);
            drt.sizeDelta        = new Vector2(16f, 16f);
            _dots[i]             = dot.AddComponent<Image>();
            _dots[i].color       = DOTOFF;
        }

        // ── Panel izquierdo – estadisticas ──
        RectTransform lp = MkImg(R, "LP", PANEL, V2(0.01f,0.10f), V2(0.22f,0.91f), V2(0,0), V2(0,0));
        BuildStats(lp);

        // ── Contenedor del grid (derecha, centrado) ──
        // Centro de la zona derecha: x=0.23..0.99 → 0.61, y=0.10..0.91 → 0.505
        _gridGO = new GameObject("Grid");
        _gridGO.transform.SetParent(R, false);
        RectTransform grt = _gridGO.AddComponent<RectTransform>();
        grt.anchorMin        = new Vector2(0.61f, 0.505f);
        grt.anchorMax        = new Vector2(0.61f, 0.505f);
        grt.pivot            = new Vector2(0.5f,  0.5f);
        grt.anchoredPosition = Vector2.zero;
        grt.sizeDelta        = new Vector2(520f, 520f); // tamaño provisional

        GridLayoutGroup glg   = _gridGO.AddComponent<GridLayoutGroup>();
        glg.startCorner       = GridLayoutGroup.Corner.UpperLeft;
        glg.startAxis         = GridLayoutGroup.Axis.Horizontal;
        glg.padding           = new RectOffset(0,0,0,0);
        glg.constraint        = GridLayoutGroup.Constraint.FixedColumnCount;
        glg.constraintCount   = 4; // se actualiza en RebuildCells

        // ── Barra inferior ──
        RectTransform bot = MkImg(R, "Bot", HDR, V2(0,0), V2(1,0), V2(0,45), V2(0,90));
        MkBtn(bot, "Reiniciar ronda", GREY,   V2(0.04f,0.12f), V2(0.35f,0.88f), () => ResetRound());
        MkBtn(bot, "Volver al menu",  GREY,   V2(0.65f,0.12f), V2(0.96f,0.88f), () => ReturnToGameSelector());

        // ── Panel de transicion entre rondas ──
        BuildTransPanel(R);

        // ── Panel de fin ──
        BuildEndPanel(R);
    }

    void BuildStats(RectTransform p)
    {
        var t0 = MkTxt(p, "T0", "TUS PASOS", DIM, 22, V2(0.06f,0.82f), V2(0.94f,0.94f));
        t0.fontStyle = FontStyles.Bold;

        _stepsVal = MkTxt(p, "SV", "0", Color.white, 80, V2(0.06f,0.62f), V2(0.94f,0.82f));
        _stepsVal.fontStyle = FontStyles.Bold;

        MkImg(p, "D1", new Color(1,1,1,0.08f), V2(0.1f,0.61f), V2(0.9f,0.615f), V2(0,0), V2(0,0));

        var t1 = MkTxt(p, "T1", "OPTIMO", DIM, 22, V2(0.06f,0.49f), V2(0.94f,0.60f));
        t1.fontStyle = FontStyles.Bold;

        _optVal = MkTxt(p, "OV", "?", YELLOW, 64, V2(0.06f,0.32f), V2(0.94f,0.49f));
        _optVal.fontStyle = FontStyles.Bold;

        MkImg(p, "D2", new Color(1,1,1,0.08f), V2(0.1f,0.31f), V2(0.9f,0.315f), V2(0,0), V2(0,0));

        _statusLbl = MkTxt(p, "St",
            "Haz clic en las casillas azules para moverte",
            DIM, 17, V2(0.04f,0.01f), V2(0.96f,0.30f));
        _statusLbl.overflowMode = TextOverflowModes.Overflow;
        _statusLbl.alignment    = TextAlignmentOptions.Center;
    }

    void BuildTransPanel(RectTransform R)
    {
        _transPanel = new GameObject("Trans");
        _transPanel.transform.SetParent(R, false);
        RectTransform tr = _transPanel.AddComponent<RectTransform>();
        tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one;
        tr.sizeDelta = Vector2.zero; tr.anchoredPosition = Vector2.zero;
        _transPanel.AddComponent<Image>().color = new Color(0,0,0,0.82f);

        RectTransform card = MkImg(tr, "Card", PANEL, V2(0.5f,0.5f), V2(0.5f,0.5f), V2(0,0), V2(680,300));
        MkImg(card, "Bar", GREEN, V2(0,1), V2(1,1), V2(0,-12), V2(0,24));

        _transTitle = MkTxt(card, "Ti", "", Color.white, 52, V2(0.05f,0.50f), V2(0.95f,0.90f));
        _transTitle.fontStyle = FontStyles.Bold;
        _transSub   = MkTxt(card, "Su", "", DIM, 30, V2(0.05f,0.08f), V2(0.95f,0.50f));

        _transPanel.SetActive(false);
    }

    void BuildEndPanel(RectTransform R)
    {
        _endPanel = new GameObject("End");
        _endPanel.transform.SetParent(R, false);
        RectTransform er = _endPanel.AddComponent<RectTransform>();
        er.anchorMin = Vector2.zero; er.anchorMax = Vector2.one;
        er.sizeDelta = Vector2.zero; er.anchoredPosition = Vector2.zero;
        _endPanel.AddComponent<Image>().color = new Color(0,0,0,0.82f);

        RectTransform card = MkImg(er, "Card", PANEL, V2(0.5f,0.5f), V2(0.5f,0.5f), V2(0,0), V2(700,400));
        _endBar   = MkImg(card, "Bar", GREEN, V2(0,1), V2(1,1), V2(0,-13), V2(0,26)).GetComponent<Image>();
        _endTitle = MkTxt(card, "Ti", "", Color.white, 54, V2(0.05f,0.55f), V2(0.95f,0.92f));
        _endTitle.fontStyle = FontStyles.Bold;
        _endSub   = MkTxt(card, "Su", "", DIM, 30, V2(0.05f,0.27f), V2(0.95f,0.55f));

        MkBtn(card, "Jugar de nuevo", ACCENT, V2(0.06f,0.04f), V2(0.46f,0.22f), () =>
        {
            StopAllCoroutines();
            _totalSteps = 0; _totalOptimal = 0;
            _endPanel.SetActive(false);
            StartRound(0);
        });
        MkBtn(card, "Menu", GREY, V2(0.54f,0.04f), V2(0.94f,0.22f), () => ReturnToGameSelector());

        _endPanel.SetActive(false);
    }

    // ═════════════════════════════════════════════════════════════════════
    //  GESTION DE CELDAS
    // ═════════════════════════════════════════════════════════════════════

    void CalcCell(out float cell, out float gW, out float gH)
    {
        float sp    = 6f;
        float maxW  = (1920f * 0.74f - sp * (_cols - 1)) / _cols;
        float maxH  = (1080f * 0.78f - sp * (_rows - 1)) / _rows;
        cell = Mathf.Min(maxW, maxH, 130f);
        gW   = _cols * cell + (_cols - 1) * sp;
        gH   = _rows * cell + (_rows - 1) * sp;
    }

    /// <summary>
    /// Destruye todas las celdas existentes y crea las nuevas
    /// para la ronda actual. Actualiza el GridLayoutGroup en el mismo frame.
    /// </summary>
    void RebuildCells()
    {
        // Destruir celdas anteriores (DestroyImmediate para que sea instantaneo)
        for (int i = _gridGO.transform.childCount - 1; i >= 0; i--)
            DestroyImmediate(_gridGO.transform.GetChild(i).gameObject);

        float cell, gW, gH;
        CalcCell(out cell, out gW, out gH);

        // Actualizar tamaño del contenedor
        RectTransform grt = _gridGO.GetComponent<RectTransform>();
        grt.sizeDelta = new Vector2(gW, gH);

        // Actualizar GridLayoutGroup
        GridLayoutGroup glg    = _gridGO.GetComponent<GridLayoutGroup>();
        glg.cellSize           = new Vector2(cell, cell);
        glg.spacing            = new Vector2(6f, 6f);
        glg.constraintCount    = _cols;

        // Crear celdas
        int   total    = _cols * _rows;
        float fontSize = Mathf.Clamp(cell * 0.18f, 11f, 22f);

        _cellBg  = new Image[total];
        _cellLbl = new TextMeshProUGUI[total];
        _cellBtn = new Button[total];

        for (int i = 0; i < total; i++)
        {
            int idx = i;  // captura para el listener

            GameObject go = new GameObject("C" + i);
            go.transform.SetParent(_gridGO.transform, false);

            Image bg  = go.AddComponent<Image>();
            bg.color  = CN;
            _cellBg[i] = bg;

            Button btn = go.AddComponent<Button>();
            btn.targetGraphic = bg;
            ColorBlock cb     = btn.colors;
            cb.normalColor      = Color.white;
            cb.highlightedColor = new Color(1, 1, 1, 0.88f);
            cb.pressedColor     = new Color(0.75f, 0.75f, 0.75f);
            cb.disabledColor    = new Color(0.55f, 0.55f, 0.55f, 0.55f);
            btn.colors          = cb;
            btn.onClick.AddListener(() => TryMove(idx));
            _cellBtn[i] = btn;

            // Label dentro de la celda
            GameObject lGO = new GameObject("L");
            lGO.transform.SetParent(go.transform, false);
            RectTransform lrt = lGO.AddComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.sizeDelta = Vector2.zero; lrt.anchoredPosition = Vector2.zero;
            TextMeshProUGUI lbl = lGO.AddComponent<TextMeshProUGUI>();
            lbl.text         = "";
            lbl.color        = Color.white;
            lbl.fontSize     = fontSize;
            lbl.fontStyle    = FontStyles.Bold;
            lbl.alignment    = TextAlignmentOptions.Center;
            lbl.overflowMode = TextOverflowModes.Ellipsis;
            _cellLbl[i]      = lbl;
        }

        // Forzar recalculo de layout para que las celdas se posicionen
        LayoutRebuilder.ForceRebuildLayoutImmediate(grt);
    }

    // ═════════════════════════════════════════════════════════════════════
    //  HELPERS UI
    // ═════════════════════════════════════════════════════════════════════

    static Vector2 V2(float x, float y) { return new Vector2(x, y); }

    RectTransform MkImg(RectTransform p, string n, Color c,
                        Vector2 amin, Vector2 amax, Vector2 pos, Vector2 sd)
    {
        GameObject go = new GameObject(n);
        go.transform.SetParent(p, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = amin; rt.anchorMax = amax;
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = sd;
        go.AddComponent<Image>().color = c;
        return rt;
    }

    TextMeshProUGUI MkTxt(RectTransform p, string n, string text,
                          Color c, float size, Vector2 amin, Vector2 amax)
    {
        GameObject go = new GameObject(n);
        go.transform.SetParent(p, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = amin; rt.anchorMax = amax;
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero; rt.sizeDelta = Vector2.zero;
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text         = text;
        tmp.color        = c;
        tmp.fontSize     = size;
        tmp.alignment    = TextAlignmentOptions.Center;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        return tmp;
    }

    void MkBtn(RectTransform p, string label, Color bgC,
               Vector2 amin, Vector2 amax,
               UnityEngine.Events.UnityAction click)
    {
        RectTransform bg = MkImg(p, "B" + label, bgC, amin, amax, V2(0,0), V2(0,0));
        Button b  = bg.gameObject.AddComponent<Button>();
        b.targetGraphic = bg.GetComponent<Image>();
        ColorBlock cb   = b.colors;
        cb.normalColor      = Color.white;
        cb.highlightedColor = new Color(1, 1, 1, 0.85f);
        cb.pressedColor     = new Color(0.7f, 0.7f, 0.7f);
        b.colors = cb;
        b.onClick.AddListener(click);
        var t = MkTxt(bg, "T", label, Color.white, 28, V2(0,0), V2(1,1));
        t.fontStyle = FontStyles.Bold;
    }

    static void EnsureES()
    {
        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }
    }
}
