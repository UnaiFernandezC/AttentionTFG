// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI del minijuego "¿Dónde estaba?": una rejilla de casillas donde aparecen
/// objetos de colores y luego se pregunta por su posición. 100% por código.
/// </summary>
public class PositionMemoryUIController : MonoBehaviour
{

    static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);
    static Vector2 V(float x, float y) => new Vector2(x, y);

    static readonly Color BG       = C(0.05f, 0.07f, 0.13f);
    static readonly Color HDR      = C(0.04f, 0.05f, 0.11f);
    static readonly Color ACCENT   = C(0.58f, 0.28f, 0.92f);
    static readonly Color DIM      = C(0.40f, 0.48f, 0.68f);
    static readonly Color CELL_BG  = C(0.13f, 0.16f, 0.28f);
    static readonly Color CELL_HID = C(0.17f, 0.20f, 0.34f);
    static readonly Color CGREEN   = C(0.25f, 0.90f, 0.52f);
    static readonly Color CRED     = C(0.90f, 0.28f, 0.30f);

    /// <summary>Definición de un objeto: forma + color + nombre para el niño.</summary>
    public struct ObjDef
    {
        public int    Shape;   // 0 círculo, 1 cuadrado, 2 rombo, 3 anillo, 4 diana, 5 ventana
        public Color  Tint;
        public string Nombre;
    }

    public static readonly ObjDef[] OBJECTS =
    {
        new ObjDef { Shape = 0, Tint = new Color(0.95f, 0.28f, 0.28f), Nombre = "la pelota roja"     },
        new ObjDef { Shape = 1, Tint = new Color(0.25f, 0.55f, 0.98f), Nombre = "la caja azul"       },
        new ObjDef { Shape = 2, Tint = new Color(0.97f, 0.82f, 0.15f), Nombre = "el rombo amarillo"  },
        new ObjDef { Shape = 3, Tint = new Color(0.22f, 0.85f, 0.50f), Nombre = "el anillo verde"    },
        new ObjDef { Shape = 4, Tint = new Color(0.70f, 0.32f, 0.95f), Nombre = "la diana morada"    },
        new ObjDef { Shape = 5, Tint = new Color(0.98f, 0.56f, 0.15f), Nombre = "la ventana naranja" },
    };

    TextMeshProUGUI _phaseLbl;
    TextMeshProUGUI _infoLbl;
    TextMeshProUGUI _roundLbl;
    TextMeshProUGUI _scoreLbl;
    Image           _countdownFill;

    RectTransform   _questionBox;
    TextMeshProUGUI _questionLbl;
    RectTransform   _questionIconHolder;

    RectTransform     _gridPanel;
    RectTransform[]   _cellRTs;
    Image[]           _cellImgs;
    Button[]          _cellBtns;
    GameObject[]      _cellContents;   // icono del objeto (o null)
    TextMeshProUGUI[] _cellMarks;      // "?" cuando está tapada

    Action<int> _onCellClicked;
    int _rows, _cols;

    public void BuildUI(int rows, int cols, Action<int> onCellClicked)
    {
        _rows = rows; _cols = cols;
        _onCellClicked = onCellClicked;

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

        MkImg(R, "BG",    BG,                            V(0, 0),     V(1, 1), V(0,0), V(0,0));
        MkImg(R, "GradT", C(0.16f, 0.06f, 0.28f, 0.18f), V(0, 0.55f), V(1, 1), V(0,0), V(0,0));

        var hdr = MkImg(R, "Hdr", HDR, V(0,1), V(1,1), V(0,-44), V(0,88));
        MkImg(hdr, "Line", ACCENT, V(0,0),     V(1,0),     V(0, 1.5f), V(0,3));
        MkImg(hdr, "AccL", ACCENT, V(0,0.18f), V(0,0.82f), V(3, 0),    V(6,0));

        var ttl = MkTxt(hdr, "T", "¿DÓNDE ESTABA?", Color.white, 34,
                        V(0.03f, 0.12f), V(0.52f, 0.88f));
        ttl.fontStyle = FontStyles.Bold;
        ttl.alignment = TextAlignmentOptions.MidlineLeft;
        ttl.characterSpacing = 2f;

        MkTxt(hdr, "Cat", "MEMORIA", DIM, 16,
              V(0.52f, 0.12f), V(0.70f, 0.88f)).alignment = TextAlignmentOptions.MidlineRight;

        _roundLbl = MkTxt(hdr, "Round", "Ronda 1/2", Color.white, 22,
                          V(0.70f, 0.12f), V(0.86f, 0.88f));
        _roundLbl.fontStyle = FontStyles.Bold;
        _roundLbl.alignment = TextAlignmentOptions.MidlineRight;

        _scoreLbl = MkTxt(hdr, "Score", "0 pts", ACCENT, 26,
                          V(0.86f, 0.12f), V(0.99f, 0.88f));
        _scoreLbl.fontStyle = FontStyles.Bold;
        _scoreLbl.alignment = TextAlignmentOptions.MidlineRight;

        _phaseLbl = MkTxt(R, "Phase", "", ACCENT, 36, V(0.1f, 0.858f), V(0.9f, 0.925f));
        _phaseLbl.fontStyle = FontStyles.Bold;

        _infoLbl = MkTxt(R, "Info", "", DIM, 21, V(0.1f, 0.806f), V(0.9f, 0.858f));

        var cdBg = MkImg(R, "CdBg", C(0.04f, 0.06f, 0.12f),
                         V(0, 0.790f), V(1, 0.806f), V(0,0), V(0,0));
        var cfGO = new GameObject("CdFill");
        cfGO.transform.SetParent(cdBg, false);
        var cfRT = cfGO.AddComponent<RectTransform>();
        cfRT.anchorMin = Vector2.zero; cfRT.anchorMax = Vector2.one;
        cfRT.sizeDelta = Vector2.zero; cfRT.anchoredPosition = Vector2.zero;
        _countdownFill = cfGO.AddComponent<Image>();
        _countdownFill.color      = ACCENT;
        _countdownFill.type       = Image.Type.Filled;
        _countdownFill.fillMethod = Image.FillMethod.Horizontal;
        _countdownFill.fillAmount = 0f;

        // ------------------------------------------------ banner de pregunta
        _questionBox = MkImg(R, "QBox", C(0.10f, 0.13f, 0.24f),
                             V(0.26f, 0.660f), V(0.74f, 0.775f), V(0,0), V(0,0));
        MkImg(_questionBox, "QLine", ACCENT, V(0,0), V(1,0), V(0,1.5f), V(0,3));
        _questionLbl = MkTxt(_questionBox, "QT", "", Color.white, 30, V(0.22f, 0), V(0.98f, 1));
        _questionLbl.fontStyle = FontStyles.Bold;
        _questionLbl.alignment = TextAlignmentOptions.MidlineLeft;

        _questionIconHolder = MkImg(_questionBox, "QIcon", Color.clear,
                                    V(0.02f, 0.10f), V(0.20f, 0.90f), V(0,0), V(0,0));
        _questionIconHolder.GetComponent<Image>().raycastTarget = false;
        _questionBox.gameObject.SetActive(false);

        // ------------------------------------------------ rejilla
        var gridGO = new GameObject("GridPanel");
        gridGO.transform.SetParent(R, false);
        _gridPanel = gridGO.AddComponent<RectTransform>();
        _gridPanel.anchorMin        = V(0.5f, 0.5f);
        _gridPanel.anchorMax        = V(0.5f, 0.5f);
        _gridPanel.pivot            = V(0.5f, 0.5f);
        _gridPanel.anchoredPosition = new Vector2(0f, -80f);

        BuildGrid();

        var bot = MkImg(R, "Bot", HDR, V(0,0), V(1,0), V(0,40), V(0,80));
        MkImg(bot, "BotLine", ACCENT, V(0,1), V(1,1), V(0,-1.5f), V(0,3));
        MkTxt(bot, "Instr", "Memoriza dónde está cada objeto · Luego responde a las preguntas",
              C(ACCENT.r + 0.12f, ACCENT.g + 0.12f, ACCENT.b + 0.12f, 1f),
              19, V(0.01f, 0), V(0.99f, 1)).alignment = TextAlignmentOptions.MidlineLeft;
    }

    void BuildGrid()
    {
        int total     = _rows * _cols;
        float cellSz  = _rows >= 3 ? 130f : 150f;
        float gap     = 14f;
        float totalW  = _cols * cellSz + (_cols - 1) * gap;
        float totalH  = _rows * cellSz + (_rows - 1) * gap;
        _gridPanel.sizeDelta = new Vector2(totalW, totalH);

        _cellRTs      = new RectTransform[total];
        _cellImgs     = new Image[total];
        _cellBtns     = new Button[total];
        _cellContents = new GameObject[total];
        _cellMarks    = new TextMeshProUGUI[total];

        for (int r = 0; r < _rows; r++)
        for (int c = 0; c < _cols; c++)
        {
            int idx = r * _cols + c;
            float x = c * (cellSz + gap) - (totalW - cellSz) * 0.5f;
            float y = -r * (cellSz + gap) + (totalH - cellSz) * 0.5f;

            var go = new GameObject("Cell_" + idx);
            go.transform.SetParent(_gridPanel, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = V(0.5f, 0.5f);
            rt.pivot     = V(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(cellSz, cellSz);
            rt.anchoredPosition = new Vector2(x, y);

            var img = go.AddComponent<Image>();
            img.color = CELL_BG;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            var cb = btn.colors;
            cb.normalColor      = Color.white;
            cb.highlightedColor = C(1, 1, 1, 0.85f);
            cb.pressedColor     = C(0.72f, 0.72f, 0.72f);
            btn.colors = cb;
            int captured = idx;
            btn.onClick.AddListener(() => _onCellClicked?.Invoke(captured));
            btn.interactable = false;
            ButtonJuice.Attach(go);

            var mark = MkTxt(rt, "Mark", "", C(0.55f, 0.60f, 0.85f), cellSz * 0.42f,
                             V(0, 0), V(1, 1));
            mark.fontStyle = FontStyles.Bold;

            _cellRTs[idx]   = rt;
            _cellImgs[idx]  = img;
            _cellBtns[idx]  = btn;
            _cellMarks[idx] = mark;

            UITween.PopIn(rt, 0.32f, 0.80f, idx * 0.03f);
        }
    }

    // ------------------------------------------------ fases

    public void PlaceObjects(Dictionary<int, ObjDef> placements)
    {
        ClearAllCells();
        foreach (var kv in placements)
        {
            _cellImgs[kv.Key].color = CELL_BG;
            _cellContents[kv.Key]   = BuildObjectIcon(_cellRTs[kv.Key], kv.Value, 0.72f);
            UITween.PopIn((RectTransform)_cellContents[kv.Key].transform, 0.35f, 0.6f);
        }
    }

    public void CoverAllCells()
    {
        for (int i = 0; i < _cellImgs.Length; i++)
        {
            if (_cellContents[i] != null)
            {
                Destroy(_cellContents[i]);
                _cellContents[i] = null;
            }
            _cellImgs[i].color  = CELL_HID;
            _cellMarks[i].text  = "?";
        }
    }

    public void ShowQuestion(ObjDef obj)
    {
        _questionBox.gameObject.SetActive(true);
        _questionLbl.text = "¿Dónde estaba " + obj.Nombre + "?";

        foreach (Transform ch in _questionIconHolder) Destroy(ch.gameObject);
        BuildObjectIcon(_questionIconHolder, obj, 0.85f);
        UITween.PopIn(_questionBox, 0.30f, 0.85f);
    }

    public void HideQuestion() => _questionBox.gameObject.SetActive(false);

    /// <summary>Revela una casilla con el objeto que había (o vacía) y marco de color.</summary>
    public void RevealCell(int idx, ObjDef? obj, bool asCorrect)
    {
        _cellMarks[idx].text = "";
        _cellImgs[idx].color = asCorrect
            ? new Color(CGREEN.r * 0.35f, CGREEN.g * 0.35f, CGREEN.b * 0.35f)
            : new Color(CRED.r   * 0.35f, CRED.g   * 0.35f, CRED.b   * 0.35f);

        if (_cellContents[idx] != null) { Destroy(_cellContents[idx]); _cellContents[idx] = null; }
        if (obj.HasValue)
            _cellContents[idx] = BuildObjectIcon(_cellRTs[idx], obj.Value, 0.72f);

        UITween.PulseOnce(_cellRTs[idx], asCorrect ? 1.15f : 1.05f, 0.30f);
    }

    public void ResetCellVisual(int idx)
    {
        if (_cellContents[idx] != null) { Destroy(_cellContents[idx]); _cellContents[idx] = null; }
        _cellImgs[idx].color = CELL_HID;
        _cellMarks[idx].text = "?";
    }

    public RectTransform GetCellRT(int idx) => _cellRTs[idx];

    public void EnableInput(bool enable)
    {
        foreach (var b in _cellBtns) b.interactable = enable;
    }

    void ClearAllCells()
    {
        for (int i = 0; i < _cellImgs.Length; i++)
        {
            if (_cellContents[i] != null) { Destroy(_cellContents[i]); _cellContents[i] = null; }
            _cellImgs[i].color = CELL_BG;
            _cellMarks[i].text = "";
        }
    }

    // ------------------------------------------------ iconos de objetos

    GameObject BuildObjectIcon(RectTransform parent, ObjDef obj, float scale)
    {
        var holder = new GameObject("Obj");
        holder.transform.SetParent(parent, false);
        var hRT = holder.AddComponent<RectTransform>();
        hRT.anchorMin = V(0.5f - scale * 0.5f, 0.5f - scale * 0.5f);
        hRT.anchorMax = V(0.5f + scale * 0.5f, 0.5f + scale * 0.5f);
        hRT.sizeDelta = Vector2.zero;

        Sprite circle = SimonUIController.MakeCircleSprite();

        switch (obj.Shape)
        {
            case 0: // pelota
                MkShape(hRT, circle, obj.Tint, V(0,0), V(1,1));
                MkShape(hRT, circle, C(1,1,1,0.35f), V(0.15f,0.55f), V(0.45f,0.85f));
                break;
            case 1: // caja
                MkShape(hRT, null, obj.Tint, V(0.05f,0.05f), V(0.95f,0.95f));
                MkShape(hRT, null, C(1,1,1,0.25f), V(0.05f,0.60f), V(0.95f,0.95f));
                break;
            case 2: // rombo
                var d = MkShape(hRT, null, obj.Tint, V(0.15f,0.15f), V(0.85f,0.85f));
                d.localEulerAngles = new Vector3(0, 0, 45f);
                break;
            case 3: // anillo
                MkShape(hRT, circle, obj.Tint, V(0,0), V(1,1));
                MkShape(hRT, circle, CELL_BG, V(0.28f,0.28f), V(0.72f,0.72f));
                break;
            case 4: // diana
                MkShape(hRT, circle, obj.Tint, V(0,0), V(1,1));
                MkShape(hRT, circle, Color.white, V(0.22f,0.22f), V(0.78f,0.78f));
                MkShape(hRT, circle, obj.Tint, V(0.40f,0.40f), V(0.60f,0.60f));
                break;
            default: // ventana
                MkShape(hRT, null, obj.Tint, V(0.05f,0.05f), V(0.95f,0.95f));
                MkShape(hRT, null, C(0,0,0,0.35f), V(0.44f,0.05f), V(0.56f,0.95f));
                MkShape(hRT, null, C(0,0,0,0.35f), V(0.05f,0.44f), V(0.95f,0.56f));
                break;
        }
        return holder;
    }

    RectTransform MkShape(RectTransform p, Sprite sprite, Color col, Vector2 am, Vector2 aM)
    {
        var go = new GameObject("S");
        go.transform.SetParent(p, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = am; rt.anchorMax = aM;
        rt.pivot = V(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero; rt.sizeDelta = Vector2.zero;
        var img = go.AddComponent<Image>();
        img.color = col;
        img.raycastTarget = false;
        if (sprite != null) img.sprite = sprite;
        return rt;
    }

    // ------------------------------------------------ HUD

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

    public void SetCountdown(float t)
    {
        if (_countdownFill == null) return;
        t = Mathf.Clamp01(t);
        _countdownFill.fillAmount = t;
        _countdownFill.color = Color.Lerp(CRED, ACCENT, t);
    }

    // ------------------------------------------------ helpers

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
}
