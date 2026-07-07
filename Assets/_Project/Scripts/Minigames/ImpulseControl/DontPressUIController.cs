// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DontPressUIController : MonoBehaviour
{

    static Vector2 V(float x, float y) => new Vector2(x, y);
    static Color   C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

    static readonly Color BG      = C(0.05f, 0.08f, 0.14f);
    static readonly Color HDR     = C(0.03f, 0.05f, 0.10f);
    static readonly Color ACCENT  = C(0.18f, 0.80f, 0.58f);
    static readonly Color DIM     = C(0.40f, 0.55f, 0.65f);
    static readonly Color CRED    = C(0.90f, 0.22f, 0.28f);
    static readonly Color CGREEN  = C(0.22f, 0.86f, 0.54f);
    static readonly Color CYELLOW = C(0.95f, 0.80f, 0.15f);

    public DontPressButtonController ButtonCtrl  { get; private set; }
    public Button                    MainButton  { get; private set; }
    /// <summary>Rect del boton central (para pulsos/sacudidas de GameFeel).</summary>
    public RectTransform             ButtonRect  { get; private set; }

    Image[]          _roundDots;
    TextMeshProUGUI  _statusText;
    Image            _countdownBar;
    Image            _flashOverlay;

    public void BuildUI(int rounds)
    {

        var cGO = new GameObject("Canvas_DontPress");
        cGO.transform.SetParent(transform, false);
        var cv = cGO.AddComponent<Canvas>();
        cv.renderMode   = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 5;
        var sc = cGO.AddComponent<CanvasScaler>();
        sc.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = V(1920f, 1080f);
        sc.matchWidthOrHeight  = 0.5f;
        cGO.AddComponent<GraphicRaycaster>();
        var R = cGO.GetComponent<RectTransform>();

        MkImg(R, "BG",   BG,                           V(0,0), V(1,1), V(0,0), V(0,0));
        MkImg(R, "Grad", C(0.00f,0.08f,0.18f,0.30f),  V(0,0), V(1,1), V(0,0), V(0,0));
        BuildGrid(R);

        var hdr = MkImg(R, "Hdr", HDR, V(0,1), V(1,1), V(0,-44), V(0,88));
        MkImg(hdr, "LineB", ACCENT,  V(0,0), V(1,0), V(0,1.5f), V(0,3));
        MkImg(hdr, "AccL",  ACCENT,  V(0,0.18f), V(0,0.82f), V(3,0), V(6,0));

        var ttl = MkTxt(hdr, "T", "NO PULSES TODAVIA", Color.white, 30,
                        V(0.03f,0.12f), V(0.52f,0.88f));
        ttl.fontStyle = FontStyles.Bold;
        ttl.alignment = TextAlignmentOptions.MidlineLeft;
        ttl.characterSpacing = 1.5f;

        MkTxt(hdr, "Cat", "CONTROL DE IMPULSOS", DIM, 15,
              V(0.52f,0.12f), V(0.72f,0.88f)).alignment = TextAlignmentOptions.MidlineRight;

        _roundDots = BuildRoundDots(hdr, rounds);

        BuildInstructionPanel(R);

        BuildMainButton(R);

        _statusText = MkTxt(R, "StatusTxt",
                            "Espera a que cambie a verde...",
                            DIM, 28, V(0.20f, 0.28f), V(0.80f, 0.38f));
        _statusText.alignment = TextAlignmentOptions.Center;
        _statusText.fontStyle = FontStyles.Italic;

        var flashGO = new GameObject("Flash");
        flashGO.transform.SetParent(R, false);
        var fRT = flashGO.AddComponent<RectTransform>();
        fRT.anchorMin = V(0,0); fRT.anchorMax = V(1,1);
        fRT.sizeDelta = V(0,0); fRT.anchoredPosition = V(0,0);
        _flashOverlay = flashGO.AddComponent<Image>();
        _flashOverlay.color = C(0,0,0,0);
        flashGO.SetActive(false);

        var bot = MkImg(R, "Bot", HDR, V(0,0), V(1,0), V(0,40), V(0,80));
        MkImg(bot, "LineT", ACCENT, V(0,1), V(1,1), V(0,-1.5f), V(0,3));
        MkTxt(bot, "Info",
              "Resiste el impulso  •  Solo pulsa cuando el boton se ponga VERDE",
              C(ACCENT.r, ACCENT.g - 0.08f, ACCENT.b - 0.05f),
              16, V(0.01f,0), V(0.78f,1)).alignment = TextAlignmentOptions.MidlineLeft;
        MkImg(bot, "Sep", C(1,1,1,0.10f), V(0.78f,0.1f), V(0.782f,0.9f), V(0,0), V(0,0));
    }

    void BuildGrid(RectTransform R)
    {
        for (int i = 1; i < 6; i++)
        {
            float t = i / 6f;
            MkImg(R, "GH_"+i, C(1,1,1,0.02f), V(0,t-0.001f), V(1,t+0.001f), V(0,0), V(0,0));
            MkImg(R, "GV_"+i, C(1,1,1,0.02f), V(t-0.0006f,0), V(t+0.0006f,1), V(0,0), V(0,0));
        }
    }

    Image[] BuildRoundDots(RectTransform hdr, int rounds)
    {
        var dots = new Image[rounds];
        float startX = 0.75f;
        float spacing = 0.04f;
        for (int i = 0; i < rounds; i++)
        {
            var gO = new GameObject("Dot_" + i);
            gO.transform.SetParent(hdr, false);
            var rt = gO.AddComponent<RectTransform>();
            rt.anchorMin        = V(startX + i * spacing, 0.5f);
            rt.anchorMax        = V(startX + i * spacing, 0.5f);
            rt.pivot            = V(0.5f, 0.5f);
            rt.sizeDelta        = V(26f, 26f);
            rt.anchoredPosition = V(0f, 0f);
            var img = gO.AddComponent<Image>();
            img.sprite = MakeCircleSprite(32);
            img.color  = C(0.25f, 0.30f, 0.40f);
            dots[i]    = img;
        }
        return dots;
    }

    void BuildInstructionPanel(RectTransform R)
    {
        var panel = MkImg(R, "InstrPanel", C(0.04f,0.07f,0.14f,0.88f),
                          V(0,0.15f), V(0,0.85f), V(90f,0), V(160f,0));
        MkImg(panel, "Line", ACCENT, V(1,0), V(1,1), V(-1.5f,0), V(3,0));

        MkTxt(panel, "T1", "ROJO", CRED, 22,
              V(0.1f,0.72f), V(0.9f,0.92f)).fontStyle = FontStyles.Bold;
        MkTxt(panel, "D1", "No pulses", DIM, 16,
              V(0.1f,0.58f), V(0.9f,0.72f));

        MkImg(panel, "Sep", C(1,1,1,0.08f), V(0.1f,0.53f), V(0.9f,0.54f), V(0,0), V(0,0));

        MkTxt(panel, "T2", "VERDE", CGREEN, 22,
              V(0.1f,0.35f), V(0.9f,0.52f)).fontStyle = FontStyles.Bold;
        MkTxt(panel, "D2", "¡Pulsa ya!", DIM, 16,
              V(0.1f,0.20f), V(0.9f,0.36f));

        MkImg(panel, "BarBG", C(0.02f,0.04f,0.08f), V(0.1f,0.06f), V(0.9f,0.16f), V(0,0), V(0,0));
        var fillGO = new GameObject("CDFill");
        fillGO.transform.SetParent(panel, false);
        var fillRT = fillGO.AddComponent<RectTransform>();
        fillRT.anchorMin = V(0.1f,0.06f); fillRT.anchorMax = V(0.9f,0.16f);
        fillRT.sizeDelta = V(0,0); fillRT.anchoredPosition = V(0,0);
        _countdownBar             = fillGO.AddComponent<Image>();
        _countdownBar.color       = CGREEN;
        _countdownBar.type        = Image.Type.Filled;
        _countdownBar.fillMethod  = Image.FillMethod.Horizontal;
        _countdownBar.fillOrigin  = 0;
        _countdownBar.fillAmount  = 0f;
    }

    void BuildMainButton(RectTransform R)
    {

        var glowGO = new GameObject("Glow");
        glowGO.transform.SetParent(R, false);
        var glowRT = glowGO.AddComponent<RectTransform>();
        glowRT.anchorMin = glowRT.anchorMax = V(0.5f, 0.5f);
        glowRT.pivot     = V(0.5f, 0.5f);
        glowRT.sizeDelta = V(380f, 380f);
        glowRT.anchoredPosition = V(0, 20f);
        var glowImg = glowGO.AddComponent<Image>();
        glowImg.sprite = MakeCircleSprite(128);
        glowImg.color  = C(0.80f, 0.18f, 0.22f, 0.22f);

        var btnGO = new GameObject("MainBtn");
        btnGO.transform.SetParent(R, false);
        var btnRT = btnGO.AddComponent<RectTransform>();
        btnRT.anchorMin = btnRT.anchorMax = V(0.5f, 0.5f);
        btnRT.pivot     = V(0.5f, 0.5f);
        btnRT.sizeDelta = V(280f, 280f);
        btnRT.anchoredPosition = V(0, 20f);

        var btnImg = btnGO.AddComponent<Image>();
        btnImg.sprite = MakeCircleSprite(256);
        btnImg.color  = C(0.14f, 0.19f, 0.30f);

        MainButton = btnGO.AddComponent<Button>();
        MainButton.targetGraphic = btnImg;
        var cb = MainButton.colors;
        cb.normalColor      = Color.white;
        cb.highlightedColor = C(0.90f, 0.90f, 0.90f);
        cb.pressedColor     = C(0.70f, 0.70f, 0.70f);
        MainButton.colors   = cb;

        var btnTxtGO = new GameObject("BtnTxt");
        btnTxtGO.transform.SetParent(btnGO.transform, false);
        var tRT = btnTxtGO.AddComponent<RectTransform>();
        tRT.anchorMin = V(0,0); tRT.anchorMax = V(1,1);
        tRT.sizeDelta = V(0,0); tRT.anchoredPosition = V(0,0);
        var btnTxt = btnTxtGO.AddComponent<TextMeshProUGUI>();
        btnTxt.text      = "Preparado";
        btnTxt.color     = Color.white;
        btnTxt.fontSize  = 38f;
        btnTxt.fontStyle = FontStyles.Bold;
        btnTxt.alignment = TextAlignmentOptions.Center;
        btnTxt.overflowMode = TextOverflowModes.Overflow;

        var ringGO = new GameObject("BtnRing");
        ringGO.transform.SetParent(R, false);
        var ringRT = ringGO.AddComponent<RectTransform>();
        ringRT.anchorMin = ringRT.anchorMax = V(0.5f, 0.5f);
        ringRT.pivot     = V(0.5f, 0.5f);
        ringRT.sizeDelta = V(296f, 296f);
        ringRT.anchoredPosition = V(0, 20f);
        var ringImg = ringGO.AddComponent<Image>();
        ringImg.sprite = MakeCircleSprite(256);
        ringImg.color  = C(1f, 1f, 1f, 0.08f);
        ringGO.transform.SetAsFirstSibling();

        ButtonCtrl            = btnGO.AddComponent<DontPressButtonController>();
        ButtonCtrl.ButtonImage = btnImg;
        ButtonCtrl.GlowImage   = glowImg;
        ButtonCtrl.ButtonText  = btnTxt;
        ButtonCtrl.SetIdle();
        ButtonRect = btnRT;
    }

    public void SetRoundDot(int index, bool? correct)
    {
        if (_roundDots == null || index >= _roundDots.Length) return;
        _roundDots[index].color = correct == null  ? C(0.25f,0.30f,0.40f)
                                : correct == true  ? CGREEN
                                                   : CRED;
    }

    public void SetStatusText(string txt, Color col)
    {
        if (_statusText == null) return;
        _statusText.text  = txt;
        _statusText.color = col;
    }

    public void UpdateCountdown(float elapsed, float window)
    {
        if (_countdownBar == null) return;
        float frac = 1f - Mathf.Clamp01(elapsed / window);
        _countdownBar.fillAmount = frac;
        _countdownBar.color = Color.Lerp(CRED, CGREEN, frac);
    }

    public void HideCountdown()
    {
        if (_countdownBar != null)
            _countdownBar.fillAmount = 0f;
    }

    public void Flash(Color col)
    {
        if (_flashOverlay == null) return;
        _flashOverlay.gameObject.SetActive(true);
        _flashOverlay.color = col;
        StartCoroutine(FlashRoutine());
    }

    IEnumerator FlashRoutine()
    {
        Color start = _flashOverlay.color;
        float t = 0f;
        while (t < 0.45f)
        {
            t += Time.deltaTime;
            _flashOverlay.color = Color.Lerp(start, C(0,0,0,0), t / 0.45f);
            yield return null;
        }
        _flashOverlay.gameObject.SetActive(false);
    }

    public static Sprite MakeCircleSprite(int res = 128)
    {
        var tex    = new Texture2D(res, res, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        var center = new Vector2(res * 0.5f, res * 0.5f);
        float r    = res * 0.5f;
        var px     = new Color[res * res];
        for (int y = 0; y < res; y++)
            for (int x = 0; x < res; x++)
            {
                float d   = Vector2.Distance(new Vector2(x+.5f, y+.5f), center);
                float a   = Mathf.Clamp01(1f - (d - r + 1.5f) / 2f);
                px[y*res+x] = new Color(1,1,1,a);
            }
        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0,0,res,res), V(0.5f,0.5f));
    }

    RectTransform MkImg(RectTransform p, string n, Color col,
                        Vector2 am, Vector2 aM, Vector2 pos, Vector2 sd)
    {
        var go = new GameObject(n);
        go.transform.SetParent(p, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = am; rt.anchorMax = aM;
        rt.pivot = V(0.5f,0.5f);
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
        rt.pivot = V(0.5f,0.5f);
        rt.anchoredPosition = Vector2.zero; rt.sizeDelta = Vector2.zero;
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = txt; t.color = col; t.fontSize = sz;
        t.alignment    = TextAlignmentOptions.Center;
        t.overflowMode = TextOverflowModes.Overflow;
        return t;
    }
}
