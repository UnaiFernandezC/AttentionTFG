// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class OptimalPathController : MinigameBase
{

    [Header("Semilla aleatoria (0 = distinto cada vez)")]
    public int randomSeed = 0;

    const int ROUNDS = 3;

    // --- Config por dificultad (ApplyDifficulty) ---
    int   _gridSide       = 4;
    int[] _obsPerRound    = { 0, 1, 1 };
    int   _maxRoundErrors = 5;
    bool  _useToll        = false;

    int _round;
    int _totalSteps;
    int _totalOptimal;
    int _totalErrors;
    int _tollHits;
    int _roundErrors;

    int    _cols, _rows, _numObs;
    bool[] _blocked;
    bool[] _visited;
    int    _startIdx, _goalIdx, _playerIdx;
    int    _tollIdx = -1;
    int    _steps, _optimal;
    bool   _roundOver;
    bool   _planning;
    float  _roundStartTime;

    GameObject        _gridGO;
    Image[]           _cellBg;
    TextMeshProUGUI[] _cellLbl;
    Button[]          _cellBtn;

    TextMeshProUGUI _stepsVal;
    TextMeshProUGUI _optVal;
    TextMeshProUGUI _statusLbl;
    TextMeshProUGUI _roundLbl;
    TextMeshProUGUI _planLbl;
    Image[]         _dots;

    GameObject      _transPanel;
    TextMeshProUGUI _transTitle;
    TextMeshProUGUI _transSub;

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

    static readonly Color CN = Hex(0.20f,0.22f,0.38f);
    static readonly Color CB = Hex(0.07f,0.07f,0.12f);
    static readonly Color CS = Hex(0.22f,0.80f,0.50f);
    static readonly Color CG = Hex(0.88f,0.26f,0.32f);
    static readonly Color CP = Hex(0.25f,0.70f,1.00f);
    static readonly Color CV = Hex(0.28f,0.32f,0.55f);
    static readonly Color CA = Hex(0.32f,0.38f,0.65f);
    static readonly Color CT = Hex(0.95f,0.55f,0.12f);   // peaje

    static Color Hex(float r, float g, float b) { return new Color(r, g, b); }

    protected override string GetIntroDescription() =>
        "Primero PIENSA tu ruta con calma y luego muevete hasta la META.\n" +
        "Intenta llegar con los minimos pasos. ¡Cuidado con los muros!";

    protected override void OnMinigameStart()
    {
        EnsureES();
        ApplyDifficulty();
        _round        = 0;
        _totalSteps   = 0;
        _totalOptimal = 0;
        _totalErrors  = 0;
        _tollHits     = 0;
        BuildUI();
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
                _gridSide = 5; _obsPerRound = new[] { 3, 4, 4 };
                _maxRoundErrors = 4; _useToll = false;
                break;
            case DifficultyLevel.Hard:
                _gridSide = 6; _obsPerRound = new[] { 6, 8, 8 };
                _maxRoundErrors = 3; _useToll = true;
                break;
            default:
                _gridSide = 4; _obsPerRound = new[] { 0, 1, 1 };
                _maxRoundErrors = 5; _useToll = false;
                break;
        }
    }

    protected override void OnMinigameComplete() { }
    protected override void OnMinigameFailed()   { }

    void StartRound(int r)
    {
        _round       = r;
        _roundOver   = false;
        _roundErrors = 0;

        _cols   = _gridSide;
        _rows   = _gridSide;
        _numObs = _obsPerRound[Mathf.Clamp(r, 0, _obsPerRound.Length - 1)];

        int s = randomSeed != 0 ? randomSeed + r : System.Environment.TickCount;
        Random.InitState(s);

        GenerateGrid();
        RebuildCells();

        if (_transPanel != null) _transPanel.SetActive(false);

        UpdateRoundUI();
        RefreshCells();
        StartCoroutine(PlanPhase());
    }

    IEnumerator PlanPhase()
    {
        _planning = true;
        RefreshCells();
        for (int i = 3; i >= 1; i--)
        {
            if (_statusLbl != null) _statusLbl.text = "Piensa tu ruta antes de moverte...";
            if (_planLbl   != null) _planLbl.text   = "Piensa tu ruta... " + i;
            GameFeel.PlayPop();
            yield return new WaitForSeconds(1f);
        }
        if (_planLbl != null) _planLbl.text = "";
        _planning = false;
        _roundStartTime = Time.realtimeSinceStartup;
        if (_statusLbl != null) _statusLbl.text = "¡YA! Toca las casillas claras para moverte";
        RefreshCells();
    }

    void ResetRound()
    {
        StopAllCoroutines();
        _planning = false;
        if (_planLbl    != null) _planLbl.text = "";
        if (_transPanel != null) _transPanel.SetActive(false);
        StartRound(_round);
    }

    void GenerateGrid()
    {
        int total  = _cols * _rows;
        _blocked   = new bool[total];
        _visited   = new bool[total];
        _startIdx  = 0;
        _goalIdx   = total - 1;
        _playerIdx = _startIdx;
        _steps     = 0;
        _tollIdx   = -1;

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

        if (_useToll)
            PlaceToll(total);

        _optimal            = BFS(_startIdx, _goalIdx);
        _visited[_startIdx] = true;
    }

    void PlaceToll(int total)
    {
        for (int t = 0; t < 200; t++)
        {
            int idx = Random.Range(0, total);
            if (idx == _startIdx || idx == _goalIdx || _blocked[idx]) continue;

            // El peaje debe poder rodearse: la meta sigue alcanzable sin pisarlo.
            _blocked[idx] = true;
            bool avoidable = BFS(_startIdx, _goalIdx) >= 0;
            _blocked[idx] = false;
            if (!avoidable) continue;

            _tollIdx = idx;
            return;
        }
    }

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

    void TryMove(int target)
    {
        if (_roundOver || _planning) return;
        int total = _cols * _rows;
        if (target < 0 || target >= total || _blocked[target]) return;

        int pr = _playerIdx / _cols, pc = _playerIdx % _cols;
        int tr = target    / _cols, tc = target    % _cols;
        if (Mathf.Abs(pr - tr) + Mathf.Abs(pc - tc) != 1) return;

        int oldDist = BFS(_playerIdx, _goalIdx);
        int newDist = BFS(target, _goalIdx);

        bool firstVisit  = !_visited[target];
        _playerIdx       = target;
        _steps++;
        _visited[target] = true;

        RefreshCells();
        StartCoroutine(PulseCell(target));

        if (target == _tollIdx && firstVisit)
        {
            _tollHits++;
            GameFeel.PlayError();
            GameFeel.FloatingText("-50 ¡Peaje!", CT, new Vector2(0f, 120f));
        }

        if (_playerIdx == _goalIdx)
        {
            _roundOver = true;
            StartCoroutine(HandleRoundEnd());
            return;
        }

        if (newDist >= 0 && oldDist >= 0 && newDist > oldDist)
        {
            _roundErrors++;
            _totalErrors++;
            GameFeel.PlayError();
            if (_cellBtn != null && target < _cellBtn.Length)
                GameFeel.Shake(_cellBtn[target].GetComponent<RectTransform>(), 8f, 0.25f);

            int left = _maxRoundErrors - _roundErrors;
            if (left >= 0 && _statusLbl != null)
                _statusLbl.text = "Te alejas de la META. Desvios restantes: " + Mathf.Max(0, left);

            if (_roundErrors > _maxRoundErrors)
            {
                _roundOver = true;
                StartCoroutine(RoundResetRoutine());
            }
        }
        else
        {
            GameFeel.PlayPop();
        }
    }

    IEnumerator RoundResetRoutine()
    {
        if (_statusLbl != null)
            _statusLbl.text = "Demasiados desvios. Repites esta ronda.";
        GameFeel.ScreenFlash(RED, 0.18f, 0.3f);
        yield return new WaitForSeconds(1.2f);
        StartRound(_round);
    }

    IEnumerator HandleRoundEnd()
    {
        _totalSteps   += _steps;
        _totalOptimal += _optimal;

        float rtMs = (Time.realtimeSinceStartup - _roundStartTime) * 1000f;
        ReportEvent(_steps == _optimal, rtMs);

        GameFeel.PlaySuccess();
        GameFeel.Confetti(25);

        yield return new WaitForSeconds(0.6f);

        if (_round >= ROUNDS - 1)
            FinishGame();
        else
            StartCoroutine(Transition());
    }

    void FinishGame()
    {
        int extra = _totalSteps - _totalOptimal;
        int score = Mathf.Max(100, 1000 - extra * 50 - _tollHits * 50);
        float eff = _totalSteps > 0 ? (float)_totalOptimal / _totalSteps : 1f;
        int   pct = Mathf.RoundToInt(eff * 100f);

        CompleteMinigame(score);
        ShowResults(true, GameFeel.StarsFromRatio(true, eff), score,
            new[]
            {
                "Pasos: " + _totalSteps + "  ·  Optimo: " + _totalOptimal,
                "Eficiencia: " + pct + "%",
                "Desvios: " + _totalErrors + (_useToll ? "  ·  Peajes: " + _tollHits : "")
            },
            extra == 0 ? "¡Ruta perfecta!" : null,
            extra == 0 ? "Camino optimo en todas las rondas" : null);
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

    void RefreshCells()
    {
        if (_cellBg == null || _cellBg.Length != _cols * _rows) return;

        int total = _cols * _rows;
        int pr    = _playerIdx / _cols, pc = _playerIdx % _cols;

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
            else if (i == _tollIdx && !_visited[i])
            {
                col = CT;
                lbl = "-50";
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
            _cellBtn[i].interactable = adj[i] && !_roundOver && !_planning;
        }

        if (_stepsVal != null) _stepsVal.text = _steps.ToString();
        if (_optVal   != null) _optVal.text   = _optimal >= 0 ? _optimal.ToString() : "?";

        if (_statusLbl != null && _roundOver)
            _statusLbl.text = EvalMsg();
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
            if (rt == null) yield break;
            t += Time.deltaTime * 14f;
            float s = 1f + 0.08f * Mathf.Sin(t * Mathf.PI);
            rt.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
        if (rt != null) rt.localScale = Vector3.one;
    }

    void UpdateRoundUI()
    {
        if (_roundLbl != null)
            _roundLbl.text = "Ronda " + (_round + 1) + " / " + ROUNDS;

        if (_dots != null)
            for (int i = 0; i < _dots.Length; i++)
                _dots[i].color = i <= _round ? ACCENT : DOTOFF;
    }

    void BuildUI()
    {

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

        MkImg(R, "BG", BG, V2(0,0), V2(1,1), V2(0,0), V2(0,0));

        RectTransform hdr = MkImg(R, "Hdr", HDR, V2(0,1), V2(1,1), V2(0,-40), V2(0,80));
        MkImg(hdr, "HL", ACCENT, V2(0,0), V2(1,0), V2(0,1.5f), V2(0,3));

        var ht = MkTxt(hdr, "T", "Ruta optima", Color.white, 40, V2(0.03f,0), V2(0.50f,1));
        ht.fontStyle = FontStyles.Bold;
        ht.alignment = TextAlignmentOptions.MidlineLeft;

        _roundLbl = MkTxt(hdr, "RL", "Ronda 1 / 3", DIM, 26, V2(0.52f,0), V2(0.78f,1));
        _roundLbl.alignment = TextAlignmentOptions.MidlineRight;

        _dots = new Image[ROUNDS];
        for (int i = 0; i < ROUNDS; i++)
        {
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

        RectTransform lp = MkImg(R, "LP", PANEL, V2(0.01f,0.10f), V2(0.22f,0.91f), V2(0,0), V2(0,0));
        BuildStats(lp);

        _gridGO = new GameObject("Grid");
        _gridGO.transform.SetParent(R, false);
        RectTransform grt = _gridGO.AddComponent<RectTransform>();
        grt.anchorMin        = new Vector2(0.61f, 0.505f);
        grt.anchorMax        = new Vector2(0.61f, 0.505f);
        grt.pivot            = new Vector2(0.5f,  0.5f);
        grt.anchoredPosition = Vector2.zero;
        grt.sizeDelta        = new Vector2(520f, 520f);

        GridLayoutGroup glg   = _gridGO.AddComponent<GridLayoutGroup>();
        glg.startCorner       = GridLayoutGroup.Corner.UpperLeft;
        glg.startAxis         = GridLayoutGroup.Axis.Horizontal;
        glg.padding           = new RectOffset(0,0,0,0);
        glg.constraint        = GridLayoutGroup.Constraint.FixedColumnCount;
        glg.constraintCount   = 4;

        _planLbl = MkTxt(R, "Plan", "", YELLOW, 42, V2(0.28f, 0.905f), V2(0.98f, 0.985f));
        _planLbl.fontStyle    = FontStyles.Bold;
        _planLbl.overflowMode = TextOverflowModes.Overflow;

        RectTransform bot = MkImg(R, "Bot", HDR, V2(0,0), V2(1,0), V2(0,45), V2(0,90));
        MkBtn(bot, "Reiniciar ronda", GREY,   V2(0.04f,0.12f), V2(0.96f,0.88f), () => ResetRound());

        BuildTransPanel(R);
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
            "Piensa tu ruta antes de moverte...",
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

    void CalcCell(out float cell, out float gW, out float gH)
    {
        float sp    = 6f;
        float maxW  = (1920f * 0.74f - sp * (_cols - 1)) / _cols;
        float maxH  = (1080f * 0.78f - sp * (_rows - 1)) / _rows;
        cell = Mathf.Min(maxW, maxH, 130f);
        gW   = _cols * cell + (_cols - 1) * sp;
        gH   = _rows * cell + (_rows - 1) * sp;
    }

    void RebuildCells()
    {

        for (int i = _gridGO.transform.childCount - 1; i >= 0; i--)
            DestroyImmediate(_gridGO.transform.GetChild(i).gameObject);

        float cell, gW, gH;
        CalcCell(out cell, out gW, out gH);

        RectTransform grt = _gridGO.GetComponent<RectTransform>();
        grt.sizeDelta = new Vector2(gW, gH);

        GridLayoutGroup glg    = _gridGO.GetComponent<GridLayoutGroup>();
        glg.cellSize           = new Vector2(cell, cell);
        glg.spacing            = new Vector2(6f, 6f);
        glg.constraintCount    = _cols;

        int   total    = _cols * _rows;
        float fontSize = Mathf.Clamp(cell * 0.18f, 11f, 22f);

        _cellBg  = new Image[total];
        _cellLbl = new TextMeshProUGUI[total];
        _cellBtn = new Button[total];

        for (int i = 0; i < total; i++)
        {
            int idx = i;

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

        LayoutRebuilder.ForceRebuildLayoutImmediate(grt);
    }

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
        ButtonJuice.Attach(bg.gameObject);
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
