using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SimonUIController : MonoBehaviour
{

    static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);
    static Vector2 V(float x, float y) => new Vector2(x, y);

    static readonly Color BG      = C(0.05f, 0.08f, 0.14f);
    static readonly Color HDR     = C(0.04f, 0.07f, 0.13f);
    static readonly Color PANEL   = C(0.07f, 0.11f, 0.21f);
    static readonly Color PANEL2  = C(0.09f, 0.14f, 0.26f);
    static readonly Color ACCENT  = C(0.18f, 0.80f, 0.58f);
    static readonly Color DIM     = C(0.38f, 0.52f, 0.68f);
    static readonly Color CRED    = C(0.90f, 0.22f, 0.28f);
    static readonly Color CGREEN  = C(0.22f, 0.86f, 0.54f);
    static readonly Color CYELLOW = C(0.96f, 0.78f, 0.18f);

    static readonly Color[] BTN_COLORS = {
        C(0.92f, 0.22f, 0.25f),
        C(0.22f, 0.52f, 0.96f),
        C(0.18f, 0.82f, 0.44f),
        C(0.96f, 0.78f, 0.14f),
        C(0.72f, 0.28f, 0.92f),
    };

    static readonly string[] BTN_LABELS = { "●", "●", "●", "●", "●" };

    int             _buttonCount = 4;
    Canvas          _canvas;
    RectTransform   _canvasRT;

    TextMeshProUGUI _roundHeaderLbl;
    TextMeshProUGUI _roundLbl;
    TextMeshProUGUI _recordLbl;
    TextMeshProUGUI _statusLbl;

    public SimonButtonController[] Buttons { get; private set; }

    GameObject      _resultPanel;
    TextMeshProUGUI _resultTitle;
    TextMeshProUGUI _resultSub;
    TextMeshProUGUI _resultRecord;

    Image           _errorFlash;

    public Action          OnRestartPressed;
    public Action          OnMenuPressed;

    public void BuildUI(int buttonCount = 4)
    {
        _buttonCount = Mathf.Clamp(buttonCount, 2, 5);

        var cGO = new GameObject("Canvas_Simon");
        cGO.transform.SetParent(transform, false);
        _canvas = cGO.AddComponent<Canvas>();
        _canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 5;
        var sc = cGO.AddComponent<CanvasScaler>();
        sc.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920f, 1080f);
        sc.matchWidthOrHeight  = 0.5f;
        cGO.AddComponent<GraphicRaycaster>();
        _canvasRT = cGO.GetComponent<RectTransform>();

        MkImg(_canvasRT, "BG", BG, V(0,0), V(1,1), V(0,0), V(0,0));

        MkImg(_canvasRT, "GradTop", C(0.10f, 0.22f, 0.40f, 0.18f), V(0, 0.72f), V(1, 1), V(0,0), V(0,0));

        _errorFlash = MkImg(_canvasRT, "ErrFlash", C(CRED.r, CRED.g, CRED.b, 0f),
                            V(0,0), V(1,1), V(0,0), V(0,0)).GetComponent<Image>();

        BuildHUD(_canvasRT);
        BuildStatusArea(_canvasRT);
        BuildButtonGrid(_canvasRT);
        BuildFooter(_canvasRT);
        BuildResultPanel(_canvasRT);
    }

    void BuildHUD(RectTransform R)
    {
        var hdr = MkImg(R, "HDR", HDR, V(0,1), V(1,1), V(0,-44), V(0,88));
        MkImg(hdr, "LineB", ACCENT, V(0,0), V(1,0), V(0,1.5f), V(0,3));
        MkImg(hdr, "AccL",  ACCENT, V(0,0.15f), V(0,0.85f), V(3,0), V(6,0));

        var ttl = MkTxt(hdr, "Title", "SIMÓN DICE", Color.white, 34,
                        V(0.03f,0.10f), V(0.48f,0.90f));
        ttl.fontStyle = FontStyles.Bold;
        ttl.alignment = TextAlignmentOptions.MidlineLeft;
        ttl.characterSpacing = 3f;

        MkTxt(hdr, "Cat", "MEMORIA", DIM, 16,
              V(0.03f,0f), V(0.30f,0.42f)).alignment = TextAlignmentOptions.MidlineLeft;

        var rdBox = MkImg(hdr, "RdBox", PANEL2, V(0.52f,0.10f), V(0.73f,0.90f), V(0,0), V(0,0));
        MkImg(rdBox, "LineT", ACCENT, V(0,1), V(1,1), V(0,-2), V(0,4));
        _roundHeaderLbl = MkTxt(rdBox, "RdLbl", "FASE", DIM, 13, V(0,0.52f), V(1,0.96f));
        _roundHeaderLbl.alignment = TextAlignmentOptions.Center;
        _roundLbl = MkTxt(rdBox, "RdVal", "—", Color.white, 28, V(0,0.05f), V(1,0.55f));
        _roundLbl.fontStyle = FontStyles.Bold;
        _roundLbl.alignment = TextAlignmentOptions.Center;

        var rcBox = MkImg(hdr, "RcBox", PANEL2, V(0.75f,0.10f), V(0.97f,0.90f), V(0,0), V(0,0));
        MkImg(rcBox, "LineT", CYELLOW, V(0,1), V(1,1), V(0,-2), V(0,4));
        MkTxt(rcBox, "RcLbl", "RÉCORD", DIM, 13, V(0,0.52f), V(1,0.96f)).alignment = TextAlignmentOptions.Center;
        _recordLbl = MkTxt(rcBox, "RcVal", "0", CYELLOW, 28, V(0,0.05f), V(1,0.55f));
        _recordLbl.fontStyle = FontStyles.Bold;
        _recordLbl.alignment = TextAlignmentOptions.Center;
    }

    void BuildStatusArea(RectTransform R)
    {
        _statusLbl = MkTxt(R, "StatusLbl", "", DIM, 30,
                           V(0.10f, 0.82f), V(0.90f, 0.90f));
        _statusLbl.fontStyle = FontStyles.Bold;
        _statusLbl.alignment = TextAlignmentOptions.Center;
        _statusLbl.characterSpacing = 1.5f;
    }

    void BuildButtonGrid(RectTransform R)
    {
        bool fiveButtons = _buttonCount >= 5;

        float gridW   = 520f;
        float gridH   = fiveButtons ? 620f : 520f;
        float btnSize = fiveButtons ? 200f : 220f;
        float glowSize = fiveButtons ? 230f : 250f;
        float shineOff = fiveButtons ? -42f : -46f;

        Vector2[] offsets = fiveButtons
            ? new Vector2[] {
                new Vector2(-130f,  150f),
                new Vector2( 130f,  150f),
                new Vector2(-130f,  -60f),
                new Vector2( 130f,  -60f),
                new Vector2(   0f, -255f),
            }
            : new Vector2[] {
                new Vector2(-140f,  140f),
                new Vector2( 140f,  140f),
                new Vector2(-140f, -140f),
                new Vector2( 140f, -140f),
            };

        var gridGO = new GameObject("ButtonGrid");
        gridGO.transform.SetParent(R, false);
        var gridRT = gridGO.AddComponent<RectTransform>();
        gridRT.anchorMin = new Vector2(0.5f, 0.5f);
        gridRT.anchorMax = new Vector2(0.5f, 0.5f);
        gridRT.pivot     = new Vector2(0.5f, 0.5f);
        gridRT.sizeDelta = new Vector2(gridW, gridH);
        gridRT.anchoredPosition = new Vector2(0f, fiveButtons ? -30f : -20f);
        gridGO.AddComponent<Image>().color = Color.clear;

        Buttons = new SimonButtonController[_buttonCount];

        Sprite circleSprite = MakeCircleSprite(128);

        for (int i = 0; i < _buttonCount; i++)
        {
            Color col = BTN_COLORS[i];

            var glowGO = new GameObject($"Glow_{i}");
            glowGO.transform.SetParent(gridRT, false);
            var glowRT = glowGO.AddComponent<RectTransform>();
            glowRT.anchorMin = glowRT.anchorMax = new Vector2(0.5f, 0.5f);
            glowRT.pivot     = new Vector2(0.5f, 0.5f);
            glowRT.sizeDelta = new Vector2(glowSize, glowSize);
            glowRT.anchoredPosition = offsets[i];
            var glowImg = glowGO.AddComponent<Image>();
            glowImg.sprite        = circleSprite;
            glowImg.color         = Color.clear;
            glowImg.raycastTarget = false;

            var btnGO = new GameObject($"Button_{i}");
            btnGO.transform.SetParent(gridRT, false);
            var btnRT = btnGO.AddComponent<RectTransform>();
            btnRT.anchorMin = btnRT.anchorMax = new Vector2(0.5f, 0.5f);
            btnRT.pivot     = new Vector2(0.5f, 0.5f);
            btnRT.sizeDelta = new Vector2(btnSize, btnSize);
            btnRT.anchoredPosition = offsets[i];

            var btnImg = btnGO.AddComponent<Image>();
            btnImg.sprite = circleSprite;
            btnImg.color  = col;

            var btn = btnGO.AddComponent<Button>();
            btn.targetGraphic = btnImg;
            var cols = btn.colors;
            cols.normalColor      = Color.white;
            cols.highlightedColor = new Color(1.1f, 1.1f, 1.1f);
            cols.pressedColor     = new Color(0.85f, 0.85f, 0.85f);
            btn.colors = cols;

            var shineGO = new GameObject("Shine");
            shineGO.transform.SetParent(btnGO.transform, false);
            var shineRT = shineGO.AddComponent<RectTransform>();
            shineRT.anchorMin = shineRT.anchorMax = new Vector2(0.5f, 0.5f);
            shineRT.pivot     = new Vector2(0.5f, 0.5f);
            shineRT.sizeDelta = new Vector2(80f, 80f);
            shineRT.anchoredPosition = new Vector2(shineOff, -shineOff);
            var shineImg = shineGO.AddComponent<Image>();
            shineImg.sprite        = circleSprite;
            shineImg.color         = new Color(1f, 1f, 1f, 0.08f);
            shineImg.raycastTarget = false;

            var numTxt = MkTxt(btnRT, "Num", (i + 1).ToString(),
                               new Color(1f, 1f, 1f, 0.18f), 36,
                               V(0, 0), V(1, 1));
            numTxt.fontStyle = FontStyles.Bold;
            numTxt.alignment = TextAlignmentOptions.Center;

            var ctrl = btnGO.AddComponent<SimonButtonController>();
            ctrl.Init(i, btnImg, glowImg, shineImg);
            Buttons[i] = ctrl;
        }

        if (!fiveButtons)
        {
            MkImg(gridRT, "SepH", C(1, 1, 1, 0.06f), V(0, 0.5f), V(1, 0.5f), V(0, 0), V(0, 2));
            MkImg(gridRT, "SepV", C(1, 1, 1, 0.06f), V(0.5f, 0), V(0.5f, 1), V(0, 0), V(2, 0));
        }
    }

    void BuildFooter(RectTransform R)
    {
        var ft = MkImg(R, "Footer", HDR, V(0,0), V(1,0), V(0,40), V(0,80));
        MkImg(ft, "LineT", C(ACCENT.r,ACCENT.g,ACCENT.b,0.30f),
              V(0,1), V(1,1), V(0,-1.5f), V(0,3));

        MkBtn(ft, "↺  Reiniciar", PANEL2, V(0.02f,0.10f), V(0.55f,0.90f), () => OnRestartPressed?.Invoke());
    }

    void BuildResultPanel(RectTransform R)
    {
        _resultPanel = new GameObject("ResultPanel");
        _resultPanel.transform.SetParent(R, false);
        var er = _resultPanel.AddComponent<RectTransform>();
        er.anchorMin = Vector2.zero; er.anchorMax = Vector2.one;
        er.sizeDelta = Vector2.zero; er.anchoredPosition = Vector2.zero;
        _resultPanel.AddComponent<Image>().color = C(0f, 0f, 0f, 0.82f);

        var card = MkImg(er, "Card", PANEL, V(0.5f,0.5f), V(0.5f,0.5f), V(0,0), V(820f, 480f));
        MkImg(card, "Sh",    C(1,1,1,0.03f), V(0,0.5f), V(1,1), V(0,0), V(0,0));
        MkImg(card, "LineT", CRED,            V(0,1),    V(1,1), V(0,-4), V(0,8));
        MkImg(card, "AccL",  CRED,            V(0,0.08f),V(0,0.92f), V(4,0), V(8,0));

        _resultTitle = MkTxt(card, "RT", "", CRED, 54, V(0.04f, 0.72f), V(0.96f, 0.97f));
        _resultTitle.fontStyle = FontStyles.Bold;
        _resultTitle.alignment = TextAlignmentOptions.Center;

        _resultSub = MkTxt(card, "RS", "", Color.white, 26, V(0.04f, 0.52f), V(0.96f, 0.72f));
        _resultSub.alignment = TextAlignmentOptions.Center;

        _resultRecord = MkTxt(card, "RR", "", CYELLOW, 20, V(0.04f, 0.36f), V(0.96f, 0.52f));
        _resultRecord.alignment = TextAlignmentOptions.Center;

        MkBtn(card, "↺  Jugar de nuevo", ACCENT,              V(0.04f,0.06f), V(0.49f,0.22f),
              () => OnRestartPressed?.Invoke());
        MkBtn(card, "Elegir minijuego",  C(0.18f,0.24f,0.38f), V(0.51f,0.06f), V(0.96f,0.22f),
              () => OnMenuPressed?.Invoke());

        _resultPanel.SetActive(false);
    }

    public void SetPhase(int phase, int total)
    {
        if (_roundHeaderLbl) _roundHeaderLbl.text = $"FASE {phase}/{total}";
        if (_roundLbl) _roundLbl.text = "—";
    }

    public void SetRound(int round) =>
        _roundLbl.text = round > 0 ? round.ToString() : "—";

    public void SetRecord(int record) =>
        _recordLbl.text = record.ToString();

    public void SetStatus(string text, Color? color = null)
    {
        _statusLbl.text  = text;
        _statusLbl.color = color ?? DIM;
    }

    public void ShowResult(bool isNewRecord, int round, int record)
    {
        _resultTitle.text  = "¡Fin del juego!";
        _resultTitle.color = CRED;
        _resultSub.text    = $"Has llegado hasta la ronda  {round}";
        _resultRecord.text = isNewRecord
            ? $"🏆  ¡NUEVO RÉCORD!  {record}"
            : $"Récord:  {record}";
        _resultPanel.SetActive(true);
    }

    public void ShowWin(bool isNewRecord, int round, int record)
    {
        _resultTitle.text  = "¡Lo lograste!";
        _resultTitle.color = CGREEN;
        _resultSub.text    = $"¡Completaste todas las  {round}  rondas!";
        _resultRecord.text = isNewRecord
            ? $"🏆  ¡NUEVO RÉCORD!  {record}"
            : $"Récord:  {record}";

        _resultPanel.SetActive(true);
    }

    public void HideResult() => _resultPanel.SetActive(false);

    public System.Collections.IEnumerator FlashError()
    {
        _errorFlash.color = new Color(CRED.r, CRED.g, CRED.b, 0.28f);
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 4f;
            float a = Mathf.Lerp(0.28f, 0f, t);
            _errorFlash.color = new Color(CRED.r, CRED.g, CRED.b, a);
            yield return null;
        }
        _errorFlash.color = Color.clear;
    }

    static Sprite _circleSprite128;

    public static Sprite MakeCircleSprite(int res = 128)
    {
        if (res == 128 && _circleSprite128 != null) return _circleSprite128;

        var tex    = new Texture2D(res, res, TextureFormat.RGBA32, false);
        var pixels = new Color[res * res];
        float r    = res / 2f;

        for (int y = 0; y < res; y++)
        for (int x = 0; x < res; x++)
        {
            float dx = x - r + 0.5f, dy = y - r + 0.5f;
            float d  = Mathf.Sqrt(dx * dx + dy * dy);

            float alpha = Mathf.Clamp01(r - d + 1.0f);
            pixels[y * res + x] = new Color(1f, 1f, 1f, alpha);
        }

        tex.SetPixels(pixels);
        tex.Apply();

        var spr = Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f));

        if (res == 128) _circleSprite128 = spr;
        return spr;
    }

    RectTransform MkImg(RectTransform p, string n, Color col,
                        Vector2 am, Vector2 aM, Vector2 pos, Vector2 sd)
    {
        var go = new GameObject(n); go.transform.SetParent(p, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = am; rt.anchorMax = aM;
        rt.pivot     = new Vector2(.5f, .5f);
        rt.anchoredPosition = pos; rt.sizeDelta = sd;
        go.AddComponent<Image>().color = col;
        return rt;
    }

    TextMeshProUGUI MkTxt(RectTransform p, string n, string txt,
                          Color col, float sz, Vector2 am, Vector2 aM)
    {
        var go = new GameObject(n); go.transform.SetParent(p, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = am; rt.anchorMax = aM;
        rt.pivot     = new Vector2(.5f, .5f);
        rt.anchoredPosition = Vector2.zero; rt.sizeDelta = Vector2.zero;
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = txt; t.color = col; t.fontSize = sz;
        t.alignment    = TextAlignmentOptions.Center;
        t.overflowMode = TextOverflowModes.Overflow;
        return t;
    }

    void MkBtn(RectTransform p, string lbl, Color bg,
               Vector2 am, Vector2 aM, Action click)
    {
        var rt = MkImg(p, "Btn_" + lbl, bg, am, aM, V(0,0), V(0,0));
        MkImg(rt, "Sh", C(1,1,1,.09f), V(0,.5f), V(1,1), V(0,0), V(0,0));
        var b = rt.gameObject.AddComponent<Button>();
        b.targetGraphic = rt.GetComponent<Image>();
        var cols = b.colors;
        cols.normalColor      = Color.white;
        cols.highlightedColor = new Color(1,1,1,.82f);
        cols.pressedColor     = new Color(.72f,.72f,.72f);
        b.colors = cols;
        b.onClick.AddListener(() => click?.Invoke());
        var t = MkTxt(rt, "T", lbl, Color.white, 22, V(0,0), V(1,1));
        t.fontStyle = FontStyles.Bold;
    }
}
