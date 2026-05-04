using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AttractionUIController : MonoBehaviour
{
    static Vector2 V(float x, float y) => new Vector2(x, y);
    static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

    static readonly Color BG       = C(0.04f, 0.07f, 0.12f);
    static readonly Color HDR      = C(0.02f, 0.05f, 0.10f);
    static readonly Color PANEL    = C(0.06f, 0.10f, 0.18f);
    static readonly Color ACCENT   = C(0.18f, 0.80f, 0.58f);
    static readonly Color DIM      = C(0.35f, 0.50f, 0.60f);
    static readonly Color CSAFE    = C(0.20f, 0.85f, 0.50f, 0.22f);
    static readonly Color CSAFE_A  = C(0.20f, 0.85f, 0.50f, 0.65f);
    static readonly Color CRED     = C(0.90f, 0.28f, 0.30f);
    static readonly Color CYELLOW  = C(0.96f, 0.82f, 0.20f);
    static readonly Color CGREEN   = C(0.22f, 0.86f, 0.54f);

    public RectTransform CanvasRT  { get; private set; }
    public RectTransform GameAreaRT{ get; private set; }
    public RectTransform CursorRT  { get; private set; }

    Image           _safeBarFill;
    TextMeshProUGUI _safeLbl;
    Image[]         _lifeIcons;
    Image           _dangerFill;
    Image           _safeZoneImg;
    Image           _flashOverlay;

    GameObject      _resultPanel;
    TextMeshProUGUI _resultTitle;
    TextMeshProUGUI _resultSub;

    public void BuildUI(float safeZoneRadius, Action onRestart, Action onMenu)
    {

        var cGO = new GameObject("Canvas_Attraction");
        cGO.transform.SetParent(transform, false);
        var cv = cGO.AddComponent<Canvas>();
        cv.renderMode   = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 5;
        var sc = cGO.AddComponent<CanvasScaler>();
        sc.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = V(1920f, 1080f);
        sc.matchWidthOrHeight  = 0.5f;
        cGO.AddComponent<GraphicRaycaster>();
        CanvasRT = cGO.GetComponent<RectTransform>();
        var R = CanvasRT;

        MkImg(R, "BG",    BG,                            V(0,0), V(1,1), V(0,0), V(0,0));
        MkImg(R, "Grad1", C(0.00f,0.10f,0.20f,0.25f),   V(0,0), V(1,1), V(0,0), V(0,0));

        BuildGrid(R);

        var hdr = MkImg(R, "Hdr", HDR, V(0,1), V(1,1), V(0,-44), V(0,88));
        MkImg(hdr, "Line", ACCENT, V(0,0),     V(1,0),     V(0,1.5f), V(0,3));
        MkImg(hdr, "AccL", ACCENT, V(0,0.18f), V(0,0.82f), V(3,0),    V(6,0));

        var ttl = MkTxt(hdr, "T", "ATRACCION EMOCIONAL", Color.white, 30,
                        V(0.03f,0.12f), V(0.50f,0.88f));
        ttl.fontStyle = FontStyles.Bold; ttl.alignment = TextAlignmentOptions.MidlineLeft;
        ttl.characterSpacing = 1.5f;

        MkTxt(hdr, "Cat", "GESTION EMOCIONAL", DIM, 15,
              V(0.50f,0.12f), V(0.68f,0.88f)).alignment = TextAlignmentOptions.MidlineRight;

        MkTxt(hdr, "Instr", "Mantente en la zona verde", DIM, 17,
              V(0.68f,0.12f), V(0.88f,0.88f)).alignment = TextAlignmentOptions.MidlineRight;

        BuildLivesHUD(hdr);

        var gaGO = new GameObject("GameArea");
        gaGO.transform.SetParent(R, false);
        GameAreaRT = gaGO.AddComponent<RectTransform>();
        GameAreaRT.anchorMin = V(0,0); GameAreaRT.anchorMax = V(1,1);
        GameAreaRT.sizeDelta = V(0,0); GameAreaRT.anchoredPosition = V(0,0);
        gaGO.AddComponent<Image>().color = Color.clear;

        BuildSafeZone(GameAreaRT, safeZoneRadius);

        BuildCursor(GameAreaRT);

        BuildSafeTimeBar(R);

        BuildDangerIndicator(R);

        var flashGO = new GameObject("FlashOverlay");
        flashGO.transform.SetParent(R, false);
        var flashRT = flashGO.AddComponent<RectTransform>();
        flashRT.anchorMin = V(0,0); flashRT.anchorMax = V(1,1);
        flashRT.sizeDelta = V(0,0); flashRT.anchoredPosition = V(0,0);
        _flashOverlay = flashGO.AddComponent<Image>();
        _flashOverlay.color = new Color(0.9f, 0.1f, 0.1f, 0f);
        flashGO.SetActive(false);

        var bot = MkImg(R, "Bot", HDR, V(0,0), V(1,0), V(0,40), V(0,80));
        MkImg(bot, "BotLine", ACCENT, V(0,1), V(1,1), V(0,-1.5f), V(0,3));
        MkTxt(bot, "Info",
              "Mueve el raton para resistir la atraccion  •  Los estimulos rojos atraen tu cursor",
              C(ACCENT.r+0.05f, ACCENT.g-0.10f, ACCENT.b-0.08f, 1f),
              16, V(0.01f,0), V(0.78f,1)).alignment = TextAlignmentOptions.MidlineLeft;
        MkImg(bot, "Sep", C(1,1,1,0.10f), V(0.78f,0.1f), V(0.782f,0.9f), V(0,0), V(0,0));
        MkBtn(bot, "Menu", C(0.12f,0.18f,0.32f), V(0.80f,0.08f), V(0.99f,0.92f), onMenu);

        BuildResultPanel(R, onRestart, onMenu);
    }

    void BuildGrid(RectTransform R)
    {

        for (int i = 1; i < 6; i++)
        {
            float t = i / 6f;
            MkImg(R, "GH_"+i, C(1,1,1,0.025f), V(0, t-0.001f), V(1, t+0.001f), V(0,0), V(0,0));
            MkImg(R, "GV_"+i, C(1,1,1,0.025f), V(t-0.0006f, 0), V(t+0.0006f, 1), V(0,0), V(0,0));
        }
    }

    void BuildLivesHUD(RectTransform hdr)
    {

        _lifeIcons = new Image[3];
        float[] xFrac = { 0.890f, 0.921f, 0.952f };
        for (int i = 0; i < 3; i++)
        {
            var liGO = new GameObject("Life_" + i);
            liGO.transform.SetParent(hdr, false);
            var liRT = liGO.AddComponent<RectTransform>();

            liRT.anchorMin        = V(xFrac[i], 0.5f);
            liRT.anchorMax        = V(xFrac[i], 0.5f);
            liRT.pivot            = V(0.5f, 0.5f);
            liRT.sizeDelta        = V(42f, 42f);
            liRT.anchoredPosition = V(0f, 0f);
            var img = liGO.AddComponent<Image>();
            img.sprite = MakeCircleSprite(64);
            img.color  = CGREEN;
            _lifeIcons[i] = img;
        }
    }

    void BuildSafeZone(RectTransform parent, float radius)
    {
        var szGO = new GameObject("SafeZone");
        szGO.transform.SetParent(parent, false);
        var szRT = szGO.AddComponent<RectTransform>();
        szRT.anchorMin = V(0.5f,0.5f); szRT.anchorMax = V(0.5f,0.5f);
        szRT.sizeDelta = V(radius*2f, radius*2f);
        szRT.anchoredPosition = V(0,0);
        _safeZoneImg = szGO.AddComponent<Image>();
        _safeZoneImg.sprite = MakeCircleSprite(256);
        _safeZoneImg.color  = CSAFE;

        var ringGO = new GameObject("SafeRing");
        ringGO.transform.SetParent(parent, false);
        var ringRT = ringGO.AddComponent<RectTransform>();
        ringRT.anchorMin = V(0.5f,0.5f); ringRT.anchorMax = V(0.5f,0.5f);
        ringRT.sizeDelta = V(radius*2f + 6f, radius*2f + 6f);
        ringRT.anchoredPosition = V(0,0);
        var ringImg = ringGO.AddComponent<Image>();
        ringImg.sprite = MakeCircleSprite(256);
        ringImg.color  = C(0.20f, 0.85f, 0.50f, 0.35f);

        MkTxt(szRT, "SafeLbl", "ZONA\nSEGURA",
              C(0.25f, 0.95f, 0.60f, 0.55f), 20, V(0,0), V(1,1));
    }

    void BuildCursor(RectTransform parent)
    {

        var cGO = new GameObject("PlayerCursor");
        cGO.transform.SetParent(parent, false);
        CursorRT = cGO.AddComponent<RectTransform>();
        CursorRT.anchorMin = V(0.5f,0.5f); CursorRT.anchorMax = V(0.5f,0.5f);
        CursorRT.sizeDelta = V(36f, 36f);
        CursorRT.anchoredPosition = V(0,0);
        var curImg = cGO.AddComponent<Image>();
        curImg.sprite = MakeCircleSprite(64);
        curImg.color  = Color.white;

        var ringGO = new GameObject("CursorRing");
        ringGO.transform.SetParent(CursorRT, false);
        var ringRT = ringGO.AddComponent<RectTransform>();
        ringRT.anchorMin = V(0.5f,0.5f); ringRT.anchorMax = V(0.5f,0.5f);
        ringRT.sizeDelta = V(54f, 54f);
        ringRT.anchoredPosition = V(0,0);
        var ringImg = ringGO.AddComponent<Image>();
        ringImg.sprite = MakeCircleSprite(64);
        ringImg.color  = C(1f, 1f, 1f, 0.22f);

        CursorRT.SetAsLastSibling();
    }

    void BuildSafeTimeBar(RectTransform R)
    {

        var panel = MkImg(R, "SafePanel", C(0.03f,0.06f,0.12f,0.85f),
                          V(0,0.12f), V(0,0.88f), V(52f,0), V(38f,0));
        MkImg(panel, "L", ACCENT, V(1,0), V(1,1), V(-1.5f,0), V(3,0));

        var barBG = MkImg(panel, "BarBG", C(0.02f,0.05f,0.10f),
                          V(0.15f,0.08f), V(0.85f,0.92f), V(0,0), V(0,0));

        var fillGO = new GameObject("Fill");
        fillGO.transform.SetParent(barBG, false);
        var fillRT = fillGO.AddComponent<RectTransform>();
        fillRT.anchorMin = V(0,0); fillRT.anchorMax = V(1,1);
        fillRT.sizeDelta = V(0,0); fillRT.anchoredPosition = V(0,0);
        _safeBarFill            = fillGO.AddComponent<Image>();
        _safeBarFill.sprite     = null;
        _safeBarFill.color      = CGREEN;
        _safeBarFill.type       = Image.Type.Filled;
        _safeBarFill.fillMethod = Image.FillMethod.Vertical;
        _safeBarFill.fillOrigin = 0;
        _safeBarFill.fillAmount = 0f;

        _safeLbl = MkTxt(panel, "Lbl", "0s", CGREEN, 15,
                         V(0,0.92f), V(1,1.06f));
        _safeLbl.fontStyle = FontStyles.Bold;

        MkTxt(panel, "IcoLbl", "SEG.", DIM, 12, V(0,-0.08f), V(1,0.04f));
    }

    void BuildDangerIndicator(RectTransform R)
    {

        var panel = MkImg(R, "DangerPanel", C(0.03f,0.06f,0.12f,0.85f),
                          V(1,0), V(1,0), V(-52f, 52f), V(80f,80f));

        MkImg(panel, "BG", C(0.02f,0.05f,0.10f),
              V(0.1f,0.1f), V(0.9f,0.9f), V(0,0), V(0,0));

        var dFillGO = new GameObject("DangerFill");
        dFillGO.transform.SetParent(panel, false);
        var dRT = dFillGO.AddComponent<RectTransform>();
        dRT.anchorMin = V(0.1f,0.1f); dRT.anchorMax = V(0.9f,0.9f);
        dRT.sizeDelta = V(0,0); dRT.anchoredPosition = V(0,0);
        _dangerFill             = dFillGO.AddComponent<Image>();
        _dangerFill.sprite      = MakeCircleSprite(128);
        _dangerFill.color       = new Color(CRED.r, CRED.g, CRED.b, 0f);
        _dangerFill.type        = Image.Type.Filled;
        _dangerFill.fillMethod  = Image.FillMethod.Radial360;
        _dangerFill.fillOrigin  = 2;
        _dangerFill.fillAmount  = 0f;

        MkTxt(panel, "DLbl", "!", CRED, 28, V(0,0), V(1,1)).fontStyle = FontStyles.Bold;
    }

    void BuildResultPanel(RectTransform R, Action onRestart, Action onMenu)
    {
        _resultPanel = new GameObject("ResultPanel");
        _resultPanel.transform.SetParent(R, false);
        var er = _resultPanel.AddComponent<RectTransform>();
        er.anchorMin = V(0,0); er.anchorMax = V(1,1);
        er.sizeDelta = V(0,0); er.anchoredPosition = V(0,0);
        _resultPanel.AddComponent<Image>().color = C(0,0,0,0.88f);

        var card = MkImg(er, "Card", PANEL, V(0.5f,0.5f), V(0.5f,0.5f), V(0,0), V(900f,480f));
        MkImg(card, "Sh",    C(1,1,1,0.03f), V(0,0.5f),  V(1,1),     V(0,0),  V(0,0));
        MkImg(card, "LineT", ACCENT,          V(0,1),     V(1,1),     V(0,-4), V(0,8));
        MkImg(card, "AccL",  ACCENT,          V(0,0.08f), V(0,0.92f), V(4,0),  V(8,0));

        _resultTitle = MkTxt(card, "RT", "", Color.white, 44,
                             V(0.05f,0.76f), V(0.95f,0.97f));
        _resultTitle.fontStyle = FontStyles.Bold;
        _resultTitle.enableAutoSizing = true;
        _resultTitle.fontSizeMin = 26f; _resultTitle.fontSizeMax = 46f;

        _resultSub = MkTxt(card, "RS", "", C(0.50f,0.68f,0.80f), 21,
                           V(0.05f,0.22f), V(0.95f,0.74f));
        _resultSub.overflowMode = TextOverflowModes.Overflow;
        _resultSub.alignment    = TextAlignmentOptions.Center;
        _resultSub.lineSpacing  = 10f;

        MkBtn(card, "Jugar de nuevo", ACCENT,              V(0.05f,0.04f), V(0.46f,0.17f), onRestart);
        MkBtn(card, "Menu",          C(0.14f,0.22f,0.38f), V(0.54f,0.04f), V(0.95f,0.17f), onMenu);

        _resultPanel.SetActive(false);
    }

    public void UpdateSafeBar(float safeTime, float target)
    {
        float frac = Mathf.Clamp01(safeTime / target);
        _safeBarFill.fillAmount = frac;
        _safeBarFill.color      = Color.Lerp(CGREEN, ACCENT, frac);
        _safeLbl.text           = safeTime.ToString("0.0") + "s";
    }

    public void UpdateLives(int lives, int maxLives)
    {
        if (_lifeIcons == null) return;
        for (int i = 0; i < _lifeIcons.Length; i++)
            _lifeIcons[i].color = (i < lives) ? CGREEN : C(0.25f,0.25f,0.25f,0.40f);
    }

    public void UpdateDangerIndicator(float danger, bool inSafe)
    {
        if (_dangerFill == null) return;
        _dangerFill.fillAmount = danger;
        _dangerFill.color      = new Color(CRED.r, CRED.g, CRED.b, danger * 0.75f);
    }

    public void SetSafeZoneActive(bool active)
    {
        if (_safeZoneImg == null) return;
        _safeZoneImg.color = active ? CSAFE_A : CSAFE;
    }

    public void FlashHit()
    {
        if (_flashOverlay == null) return;
        _flashOverlay.gameObject.SetActive(true);
        _flashOverlay.color = new Color(0.9f, 0.1f, 0.1f, 0.35f);

        StartCoroutine(FlashRoutine());
    }

    System.Collections.IEnumerator FlashRoutine()
    {
        float t = 0f;
        while (t < 0.4f)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(0.35f, 0f, t / 0.4f);
            _flashOverlay.color = new Color(0.9f, 0.1f, 0.1f, a);
            yield return null;
        }
        _flashOverlay.gameObject.SetActive(false);
    }

    public void ShowResult(bool won, int score, float safeTime, float target)
    {
        if (won)
        {
            _resultTitle.text  = "Control emocional logrado";
            _resultTitle.color = CGREEN;
            _resultSub.text    =
                "Aguantaste " + safeTime.ToString("0.0") + " segundos en la zona segura.\n" +
                "Puntuacion:   " + score + " pts\n\n" +
                "Resististe la atraccion y mantuviste el control.\n" +
                "Eso mismo hacemos cuando gestionamos bien nuestras emociones.";
        }
        else
        {
            _resultTitle.text  = "Los estimulos te superaron";
            _resultTitle.color = CRED;
            _resultSub.text    =
                "Tiempo en zona segura:   " + safeTime.ToString("0.0") + "s / " +
                target.ToString("0") + "s\n\n" +
                "Los estimulos negativos tienen fuerza, pero podemos resistirlos.\n" +
                "Anticipa su posicion y mantente alejado de los bordes.";
        }
        _resultPanel.SetActive(true);
    }

    public static Sprite MakeCircleSprite(int res = 128)
    {
        var tex     = new Texture2D(res, res, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        var center  = new Vector2(res * 0.5f, res * 0.5f);
        float radius = res * 0.5f;
        var pixels  = new Color[res * res];

        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                float dist  = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                float alpha = Mathf.Clamp01(1f - (dist - radius + 1.5f) / 2f);
                pixels[y * res + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        return Sprite.Create(tex,
            new Rect(0, 0, res, res),
            new Vector2(0.5f, 0.5f));
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
        t.text = txt; t.color = col; t.fontSize = sz;
        t.alignment = TextAlignmentOptions.Center;
        t.overflowMode = TextOverflowModes.Overflow;
        return t;
    }

    void MkBtn(RectTransform p, string lbl, Color bg, Vector2 am, Vector2 aM, Action click)
    {
        var rt = MkImg(p, "Btn_" + lbl, bg, am, aM, V(0,0), V(0,0));
        MkImg(rt, "Sh", C(1,1,1,0.09f), V(0,0.5f), V(1,1), V(0,0), V(0,0));
        var b = rt.gameObject.AddComponent<Button>();
        b.targetGraphic = rt.GetComponent<Image>();
        var cb = b.colors;
        cb.normalColor = Color.white; cb.highlightedColor = C(1,1,1,0.82f);
        cb.pressedColor = C(0.72f,0.72f,0.72f);
        b.colors = cb;
        b.onClick.AddListener(() => click?.Invoke());
        var t = MkTxt(rt, "T", lbl, Color.white, 24, V(0,0), V(1,1));
        t.fontStyle = FontStyles.Bold;
    }
}
