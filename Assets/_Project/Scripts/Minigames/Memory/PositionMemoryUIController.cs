using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PositionMemoryUIController : MonoBehaviour
{

    static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);
    static Vector2 V(float x, float y) => new Vector2(x, y);

    static readonly Color BG       = C(0.06f, 0.07f, 0.13f);
    static readonly Color HDR      = C(0.04f, 0.05f, 0.11f);
    static readonly Color PANEL    = C(0.08f, 0.11f, 0.20f);
    static readonly Color ACCENT   = C(0.58f, 0.28f, 0.92f);
    static readonly Color DIM      = C(0.40f, 0.48f, 0.68f);
    static readonly Color CELL_OFF = C(0.12f, 0.15f, 0.25f);
    static readonly Color CELL_ON  = C(0.65f, 0.35f, 1.00f);
    static readonly Color CELL_SEL = C(0.35f, 0.55f, 0.90f);
    static readonly Color CGREEN   = C(0.25f, 0.90f, 0.52f);
    static readonly Color CRED     = C(0.90f, 0.28f, 0.30f);
    static readonly Color CORANGE  = C(0.96f, 0.62f, 0.18f);

    Image[]           _cellImgs;
    Button[]          _cellBtns;
    bool[]            _selected;

    TextMeshProUGUI   _phaseLbl;
    TextMeshProUGUI   _infoLbl;
    TextMeshProUGUI   _roundLbl;
    TextMeshProUGUI   _scoreLbl;

    GameObject        _confirmBtnGO;
    GameObject        _resultPanel;
    TextMeshProUGUI   _resultTitle;
    TextMeshProUGUI   _resultSub;

    Action<int>       _onCellToggled;
    Action            _onConfirm;

    public void BuildUI(int rows, int cols,
                        Action<int> onCellToggled, Action onConfirm,
                        Action onRestart, Action onMenu)
    {
        int total = rows * cols;
        _selected       = new bool[total];
        _onCellToggled  = onCellToggled;
        _onConfirm      = onConfirm;

        var cGO = new GameObject("Canvas_PositionMemory");
        cGO.transform.SetParent(transform, false);
        var cv = cGO.AddComponent<Canvas>();
        cv.renderMode   = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 5;
        var sc = cGO.AddComponent<CanvasScaler>();
        sc.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920f, 1080f);
        sc.matchWidthOrHeight  = 0.5f;
        cGO.AddComponent<GraphicRaycaster>();
        var R = cGO.GetComponent<RectTransform>();

        MkImg(R, "BG",    BG,                             V(0,0),      V(1,1),     V(0,0), V(0,0));
        MkImg(R, "GradT", C(0.16f, 0.06f, 0.28f, 0.20f), V(0, 0.55f), V(1, 1),    V(0,0), V(0,0));

        var hdr = MkImg(R, "Hdr", HDR, V(0,1), V(1,1), V(0,-44), V(0,88));
        MkImg(hdr, "Line", ACCENT, V(0,0),     V(1,0),     V(0, 1.5f), V(0,3));
        MkImg(hdr, "AccL", ACCENT, V(0,0.18f), V(0,0.82f), V(3, 0),    V(6,0));

        var ttl = MkTxt(hdr, "T", "MEMORIA DE POSICIONES", Color.white, 32,
                        V(0.03f, 0.12f), V(0.52f, 0.88f));
        ttl.fontStyle = FontStyles.Bold;
        ttl.alignment = TextAlignmentOptions.MidlineLeft;
        ttl.characterSpacing = 2f;

        MkTxt(hdr, "Cat", "MEMORIA", DIM, 16,
              V(0.52f, 0.12f), V(0.72f, 0.88f)).alignment = TextAlignmentOptions.MidlineRight;

        _roundLbl = MkTxt(hdr, "Round", "Ronda 1/3", Color.white, 22,
                          V(0.72f, 0.12f), V(0.88f, 0.88f));
        _roundLbl.fontStyle = FontStyles.Bold;
        _roundLbl.alignment = TextAlignmentOptions.MidlineRight;

        _scoreLbl = MkTxt(hdr, "Score", "0 pts", ACCENT, 26,
                          V(0.88f, 0.12f), V(0.99f, 0.88f));
        _scoreLbl.fontStyle = FontStyles.Bold;
        _scoreLbl.alignment = TextAlignmentOptions.MidlineRight;

        _phaseLbl = MkTxt(R, "Phase", "", ACCENT, 40, V(0.1f, 0.865f), V(0.9f, 0.932f));
        _phaseLbl.fontStyle = FontStyles.Bold;

        _infoLbl = MkTxt(R, "Info", "", DIM, 22, V(0.1f, 0.808f), V(0.9f, 0.865f));

        BuildGrid(R, rows, cols);

        _confirmBtnGO = BuildConfirmBtn(R);
        _confirmBtnGO.SetActive(false);

        var bot = MkImg(R, "Bot", HDR, V(0,0), V(1,0), V(0,40), V(0,80));
        MkImg(bot, "BotLine", ACCENT, V(0,1), V(1,1), V(0,-1.5f), V(0,3));
        MkTxt(bot, "Instr", "Observa las casillas · Luego selecciona las que recuerdas",
              C(ACCENT.r + 0.12f, ACCENT.g + 0.12f, ACCENT.b + 0.12f, 1f),
              19, V(0.01f, 0), V(0.78f, 1)).alignment = TextAlignmentOptions.MidlineLeft;
        MkImg(bot, "Sep", C(1,1,1,0.10f), V(0.78f, 0.1f), V(0.782f, 0.9f), V(0,0), V(0,0));
        MkBtn(bot, "Menu", C(0.12f, 0.20f, 0.36f), V(0.80f, 0.08f), V(0.99f, 0.92f), onMenu);

        BuildResultPanel(R, onRestart, onMenu);
    }

    void BuildGrid(RectTransform R, int rows, int cols)
    {
        float cellSize = 108f;
        float gap      = 12f;
        float totalW   = cols * cellSize + (cols - 1) * gap;
        float totalH   = rows * cellSize + (rows - 1) * gap;

        var gridGO = new GameObject("Grid");
        gridGO.transform.SetParent(R, false);
        var gridRT = gridGO.AddComponent<RectTransform>();
        gridRT.anchorMin        = new Vector2(0.5f, 0.5f);
        gridRT.anchorMax        = new Vector2(0.5f, 0.5f);
        gridRT.pivot            = new Vector2(0.5f, 0.5f);
        gridRT.sizeDelta        = new Vector2(totalW, totalH);
        gridRT.anchoredPosition = new Vector2(0f, -8f);
        gridGO.AddComponent<Image>().color          = Color.clear;
        gridGO.GetComponent<Image>().raycastTarget  = false;

        int total = rows * cols;
        _cellImgs = new Image[total];
        _cellBtns = new Button[total];

        for (int i = 0; i < total; i++)
        {
            int row = i / cols;
            int col = i % cols;

            float x = col * (cellSize + gap) - totalW * 0.5f + cellSize * 0.5f;
            float y = (rows - 1 - row) * (cellSize + gap) - totalH * 0.5f + cellSize * 0.5f;

            var cellGO = new GameObject("Cell_" + i);
            cellGO.transform.SetParent(gridRT, false);
            var cellRT = cellGO.AddComponent<RectTransform>();
            cellRT.anchorMin        = new Vector2(0.5f, 0.5f);
            cellRT.anchorMax        = new Vector2(0.5f, 0.5f);
            cellRT.pivot            = new Vector2(0.5f, 0.5f);
            cellRT.sizeDelta        = new Vector2(cellSize, cellSize);
            cellRT.anchoredPosition = new Vector2(x, y);

            var img = cellGO.AddComponent<Image>();
            img.color      = CELL_OFF;
            _cellImgs[i]   = img;

            var shGO = new GameObject("Sh");
            shGO.transform.SetParent(cellRT, false);
            var shRT = shGO.AddComponent<RectTransform>();
            shRT.anchorMin = V(0, 0.5f); shRT.anchorMax = V(1, 1);
            shRT.sizeDelta = V(0, 0); shRT.anchoredPosition = V(0, 0);
            shGO.AddComponent<Image>().color = C(1, 1, 1, 0.07f);

            int capturedIndex = i;
            var btn = cellGO.AddComponent<Button>();
            btn.targetGraphic = img;
            var cb = btn.colors;
            cb.normalColor      = Color.white;
            cb.highlightedColor = C(1, 1, 1, 0.85f);
            cb.pressedColor     = C(0.72f, 0.72f, 0.72f);
            btn.colors       = cb;
            btn.interactable = false;
            btn.onClick.AddListener(() => _onCellToggled?.Invoke(capturedIndex));
            _cellBtns[i] = btn;
        }
    }

    GameObject BuildConfirmBtn(RectTransform R)
    {
        var rt = MkImg(R, "ConfirmBtn", ACCENT,
                       V(0.35f, 0.055f), V(0.65f, 0.135f), V(0,0), V(0,0));
        MkImg(rt, "Sh", C(1, 1, 1, 0.13f), V(0, 0.5f), V(1, 1), V(0,0), V(0,0));
        var btn = rt.gameObject.AddComponent<Button>();
        btn.targetGraphic = rt.GetComponent<Image>();
        var cb = btn.colors;
        cb.normalColor      = Color.white;
        cb.highlightedColor = C(1, 1, 1, 0.85f);
        cb.pressedColor     = C(0.72f, 0.72f, 0.72f);
        btn.colors = cb;
        btn.onClick.AddListener(() => _onConfirm?.Invoke());
        var t = MkTxt(rt, "T", "CONFIRMAR", Color.white, 30, V(0,0), V(1,1));
        t.fontStyle = FontStyles.Bold;
        return rt.gameObject;
    }

    void BuildResultPanel(RectTransform R, Action onRestart, Action onMenu)
    {
        _resultPanel = new GameObject("ResultPanel");
        _resultPanel.transform.SetParent(R, false);
        var er = _resultPanel.AddComponent<RectTransform>();
        er.anchorMin = V(0,0); er.anchorMax = V(1,1);
        er.sizeDelta = V(0,0); er.anchoredPosition = V(0,0);
        _resultPanel.AddComponent<Image>().color = C(0, 0, 0, 0.86f);

        var card = MkImg(er, "Card", PANEL, V(0.5f,0.5f), V(0.5f,0.5f), V(0,0), V(820f,420f));
        MkImg(card, "Sh",    C(1, 1, 1, 0.03f), V(0, 0.5f),    V(1, 1),     V(0, 0),  V(0, 0));
        MkImg(card, "LineT", ACCENT,             V(0, 1),        V(1, 1),     V(0, -4), V(0, 8));
        MkImg(card, "AccL",  ACCENT,             V(0, 0.08f),    V(0, 0.92f), V(4, 0),  V(8, 0));

        _resultTitle = MkTxt(card, "RT", "", Color.white, 52, V(0.05f, 0.74f), V(0.95f, 0.97f));
        _resultTitle.fontStyle = FontStyles.Bold;
        _resultSub = MkTxt(card, "RS", "", C(0.48f, 0.62f, 0.80f), 23, V(0.05f, 0.24f), V(0.95f, 0.72f));
        _resultSub.overflowMode = TextOverflowModes.Overflow;

        MkBtn(card, "Jugar de nuevo", ACCENT,                V(0.05f, 0.04f), V(0.46f, 0.18f), onRestart);
        MkBtn(card, "Menu",           C(0.14f, 0.22f, 0.38f), V(0.54f, 0.04f), V(0.95f, 0.18f), onMenu);

        _resultPanel.SetActive(false);
    }

    public void SetPhaseLabel(string text, Color col)
    {
        if (_phaseLbl != null) { _phaseLbl.text = text; _phaseLbl.color = col; }
    }

    public void SetInfoLabel(string text)
    {
        if (_infoLbl != null) _infoLbl.text = text;
    }

    public void UpdateRound(int current, int total)
    {
        if (_roundLbl != null) _roundLbl.text = "Ronda " + current + "/" + total;
    }

    public void UpdateScore(int score)
    {
        if (_scoreLbl != null) _scoreLbl.text = score + " pts";
    }

    public void ShowMemorizePhase(List<int> targets)
    {
        for (int i = 0; i < _cellImgs.Length; i++)
        {
            _cellImgs[i].color   = CELL_OFF;
            _cellBtns[i].interactable = false;
        }
        foreach (int t in targets)
            _cellImgs[t].color = CELL_ON;
        if (_confirmBtnGO != null) _confirmBtnGO.SetActive(false);
    }

    public void ShowRecallPhase()
    {
        for (int i = 0; i < _cellImgs.Length; i++)
        {
            _selected[i]              = false;
            _cellImgs[i].color        = CELL_OFF;
            _cellBtns[i].interactable = true;
        }
        if (_confirmBtnGO != null) _confirmBtnGO.SetActive(true);
    }

    public void ToggleCell(int idx)
    {
        if (idx < 0 || idx >= _selected.Length) return;
        _selected[idx]     = !_selected[idx];
        _cellImgs[idx].color = _selected[idx] ? CELL_SEL : CELL_OFF;
    }

    public List<int> GetSelectedIndices()
    {
        var list = new List<int>();
        for (int i = 0; i < _selected.Length; i++)
            if (_selected[i]) list.Add(i);
        return list;
    }

    public void ShowRoundResult(HashSet<int> targets, List<int> playerSelected)
    {
        if (_confirmBtnGO != null) _confirmBtnGO.SetActive(false);
        for (int i = 0; i < _cellBtns.Length; i++)
            _cellBtns[i].interactable = false;

        var playerSet = new HashSet<int>(playerSelected);

        for (int i = 0; i < _cellImgs.Length; i++)
        {
            bool inTarget = targets.Contains(i);
            bool inPlayer = playerSet.Contains(i);

            if      ( inTarget &&  inPlayer) _cellImgs[i].color = CGREEN;
            else if (!inTarget &&  inPlayer) _cellImgs[i].color = CRED;
            else if ( inTarget && !inPlayer) _cellImgs[i].color = CORANGE;
            else                             _cellImgs[i].color = CELL_OFF;
        }
    }

    public void ShowFinalResult(bool win, string sub)
    {
        _resultTitle.text  = win ? "¡Memoria excelente!" : "Sigue practicando";
        _resultTitle.color = win ? CGREEN : CRED;
        _resultSub.text    = sub;
        _resultPanel.SetActive(true);
    }

    RectTransform MkImg(RectTransform p, string n, Color col,
                        Vector2 am, Vector2 aM, Vector2 pos, Vector2 sd)
    {
        var go = new GameObject(n);
        go.transform.SetParent(p, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = am; rt.anchorMax = aM;
        rt.pivot     = V(0.5f, 0.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = sd;
        go.AddComponent<Image>().color = col;
        return rt;
    }

    TextMeshProUGUI MkTxt(RectTransform p, string n, string txt, Color col,
                           float sz, Vector2 am, Vector2 aM)
    {
        var go = new GameObject(n);
        go.transform.SetParent(p, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = am; rt.anchorMax = aM;
        rt.pivot     = V(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero; rt.sizeDelta = Vector2.zero;
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text      = txt;
        t.color     = col;
        t.fontSize  = sz;
        t.alignment = TextAlignmentOptions.Center;
        t.overflowMode = TextOverflowModes.Overflow;
        return t;
    }

    void MkBtn(RectTransform p, string lbl, Color bg,
               Vector2 am, Vector2 aM, Action click)
    {
        var rt = MkImg(p, "Btn_" + lbl, bg, am, aM, V(0,0), V(0,0));
        MkImg(rt, "Sh", C(1, 1, 1, 0.09f), V(0, 0.5f), V(1, 1), V(0,0), V(0,0));
        var b = rt.gameObject.AddComponent<Button>();
        b.targetGraphic = rt.GetComponent<Image>();
        var cb = b.colors;
        cb.normalColor      = Color.white;
        cb.highlightedColor = C(1, 1, 1, 0.82f);
        cb.pressedColor     = C(0.72f, 0.72f, 0.72f);
        b.colors = cb;
        b.onClick.AddListener(() => click?.Invoke());
        var t = MkTxt(rt, "T", lbl, Color.white, 24, V(0,0), V(1,1));
        t.fontStyle = FontStyles.Bold;
    }
}
