// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Interfaz del minijuego No Pulses Todavia (Control de impulsos).
/// Construida 100% por codigo con estetica espacial:
/// - Gran botonazo central circular con ARO DE ESTADO (rojo = espera, verde = ¡ahora!).
/// - Glow por capas que respira durante la espera (tension visual sutil).
/// - HUD flotante redondeado con la paleta naranja de Control de impulsos.
/// Solo presentacion: la logica de rondas/temporizador vive en el GameManager.
/// </summary>
public class DontPressUIController : MonoBehaviour
{

    static Vector2 V(float x, float y) => new Vector2(x, y);
    static Color   C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

    // Paleta naranja de la categoria Control de impulsos
    static readonly Color ACCENT  = C(0.95f, 0.55f, 0.12f);
    static readonly Color PANEL   = C(0.10f, 0.13f, 0.24f, 0.94f);
    static readonly Color PANEL2  = C(0.07f, 0.10f, 0.20f, 0.88f);
    static readonly Color DIM     = C(0.55f, 0.65f, 0.85f);
    static readonly Color CRED    = C(0.90f, 0.22f, 0.28f);
    static readonly Color CGREEN  = C(0.22f, 0.86f, 0.54f);
    static readonly Color CYELLOW = C(0.95f, 0.80f, 0.15f);

    public DontPressButtonController ButtonCtrl  { get; private set; }
    public Button                    MainButton  { get; private set; }
    /// <summary>Rect del boton central (para pulsos/sacudidas de GameFeel).</summary>
    public RectTransform             ButtonRect  { get; private set; }

    Image[]          _roundDots;
    TextMeshProUGUI  _statusText;
    Image            _statusDot;
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

        // Fondo espacial coherente (nebulosas + estrellas + planeta)
        KidUI.BuildSpaceBackground(R);

        // Cabecera flotante redondeada con acento naranja
        var hdr = Pill(R, "Hdr", PANEL, V(0.015f, 0.925f), V(0.985f, 0.988f), 1.3f);
        var hdrLine = Pill(hdr, "Line", ACCENT, V(0f, 0f), V(1f, 0f), 4f);
        hdrLine.anchoredPosition = V(0f, 2f);
        hdrLine.sizeDelta        = V(-30f, 4f);
        hdrLine.GetComponent<Image>().raycastTarget = false;

        var ttl = MkTxt(hdr, "T", "NO PULSES TODAVIA", Color.white, 32,
                        V(0.02f, 0.12f), V(0.58f, 0.88f));
        ttl.fontStyle        = FontStyles.Bold;
        ttl.alignment        = TextAlignmentOptions.MidlineLeft;
        ttl.characterSpacing = 2f;

        var cat = MkTxt(hdr, "Cat", "CONTROL DE IMPULSOS", ACCENT, 17,
                        V(0.58f, 0.12f), V(0.98f, 0.88f));
        cat.alignment        = TextAlignmentOptions.MidlineRight;
        cat.characterSpacing = 3f;
        UITween.PopIn(hdr, 0.45f, 0.90f);

        // Marcadores de ronda (circulos reales, centrados bajo la cabecera)
        _roundDots = BuildRoundDots(R, rounds);

        // Panel-leyenda flotante a la izquierda (ROJO espera / VERDE pulsa)
        BuildLegendPanel(R);

        // Gran botonazo central con aro de estado
        BuildMainButton(R);

        // Chip de estado bajo el boton (punto de color + mensaje)
        var statusChip = Pill(R, "StatusChip", PANEL2, V(0.28f, 0.225f), V(0.72f, 0.295f), 1.6f);
        _statusDot = KidUI.CircleAt(statusChip, "StateDot", DIM, V(0.05f, 0.5f), 16f)
                          .GetComponent<Image>();
        _statusDot.raycastTarget = false;
        _statusText = MkTxt(statusChip, "StatusTxt",
                            "Espera a que cambie a verde...",
                            DIM, 26, V(0.09f, 0f), V(0.97f, 1f));
        _statusText.alignment = TextAlignmentOptions.Center;
        UITween.PopIn(statusChip, 0.45f, 0.88f, 0.10f);

        // Overlay de flash a pantalla completa (decorativo: nunca bloquea clics)
        var flashGO = new GameObject("Flash");
        flashGO.transform.SetParent(R, false);
        var fRT = flashGO.AddComponent<RectTransform>();
        fRT.anchorMin = V(0, 0); fRT.anchorMax = V(1, 1);
        fRT.sizeDelta = V(0, 0); fRT.anchoredPosition = V(0, 0);
        _flashOverlay = flashGO.AddComponent<Image>();
        _flashOverlay.color         = C(0, 0, 0, 0);
        _flashOverlay.raycastTarget = false;
        flashGO.SetActive(false);

        // Pastilla inferior de instruccion
        var bot = Pill(R, "Bot", PANEL, V(0.10f, 0.014f), V(0.90f, 0.072f), 1.4f);
        KidUI.CircleAt(bot, "BotDot", ACCENT, V(0.035f, 0.5f), 14f)
             .GetComponent<Image>().raycastTarget = false;
        MkTxt(bot, "Info",
              "Resiste el impulso  •  Solo pulsa cuando el boton se ponga VERDE",
              C(0.95f, 0.90f, 0.78f), 19, V(0.06f, 0f), V(0.97f, 1f))
            .alignment = TextAlignmentOptions.MidlineLeft;
        UITween.PopIn(bot, 0.45f, 0.90f, 0.08f);
    }

    // ---------------------------------------------------------------- HUD

    Image[] BuildRoundDots(RectTransform R, int rounds)
    {
        var dots = new Image[rounds];

        var rowGO = new GameObject("RoundDots");
        rowGO.transform.SetParent(R, false);
        var rowRT = rowGO.AddComponent<RectTransform>();
        rowRT.anchorMin = rowRT.anchorMax = V(0.5f, 0.885f);
        rowRT.pivot     = V(0.5f, 0.5f);
        rowRT.sizeDelta = Vector2.zero;
        rowRT.anchoredPosition = Vector2.zero;

        float spacing = 40f;
        float startX  = -(rounds - 1) * spacing * 0.5f;
        for (int i = 0; i < rounds; i++)
        {
            var gO = new GameObject("Dot_" + i);
            gO.transform.SetParent(rowRT, false);
            var rt = gO.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = V(0.5f, 0.5f);
            rt.pivot            = V(0.5f, 0.5f);
            rt.sizeDelta        = V(24f, 24f);
            rt.anchoredPosition = V(startX + i * spacing, 0f);
            var img = gO.AddComponent<Image>();
            img.sprite        = KidUI.CircleSpr;   // circulo antialiasado real
            img.color         = C(1f, 1f, 1f, 0.18f);
            img.raycastTarget = false;
            dots[i] = img;
        }
        return dots;
    }

    void BuildLegendPanel(RectTransform R)
    {
        var panel = Pill(R, "InstrPanel", PANEL2, V(0.018f, 0.32f), V(0.155f, 0.68f), 1.2f);
        var edge  = Pill(panel, "Line", ACCENT, V(1f, 0.10f), V(1f, 0.90f), 4f);
        edge.anchoredPosition = V(-2f, 0f);
        edge.sizeDelta        = V(4f, 0f);
        edge.GetComponent<Image>().raycastTarget = false;

        // ROJO = no pulses
        KidUI.CircleAt(panel, "DotR", CRED, V(0.16f, 0.82f), 20f)
             .GetComponent<Image>().raycastTarget = false;
        MkTxt(panel, "T1", "ROJO", CRED, 22, V(0.26f, 0.74f), V(0.95f, 0.90f))
            .fontStyle = FontStyles.Bold;
        MkTxt(panel, "D1", "No pulses", DIM, 16, V(0.10f, 0.60f), V(0.95f, 0.74f));

        var sep = Pill(panel, "Sep", C(1f, 1f, 1f, 0.08f), V(0.10f, 0.53f), V(0.90f, 0.545f), 4f);
        sep.GetComponent<Image>().raycastTarget = false;

        // VERDE = pulsa ya
        KidUI.CircleAt(panel, "DotG", CGREEN, V(0.16f, 0.42f), 20f)
             .GetComponent<Image>().raycastTarget = false;
        MkTxt(panel, "T2", "VERDE", CGREEN, 22, V(0.26f, 0.34f), V(0.95f, 0.50f))
            .fontStyle = FontStyles.Bold;
        MkTxt(panel, "D2", "¡Pulsa ya!", DIM, 16, V(0.10f, 0.20f), V(0.95f, 0.34f));

        // Barra de cuenta atras redondeada (ventana verde)
        var barBg = Pill(panel, "BarBG", C(0.02f, 0.04f, 0.10f, 0.92f),
                         V(0.10f, 0.07f), V(0.90f, 0.15f), 3f);
        barBg.GetComponent<Image>().raycastTarget = false;
        var fillGO = new GameObject("CDFill");
        fillGO.transform.SetParent(barBg, false);
        var fillRT = fillGO.AddComponent<RectTransform>();
        fillRT.anchorMin = V(0f, 0f); fillRT.anchorMax = V(1f, 1f);
        fillRT.sizeDelta = V(-4f, -4f); fillRT.anchoredPosition = V(0f, 0f);
        _countdownBar               = fillGO.AddComponent<Image>();
        _countdownBar.sprite        = KidUI.RoundedSprite;
        _countdownBar.color         = CGREEN;
        _countdownBar.type          = Image.Type.Filled;
        _countdownBar.fillMethod    = Image.FillMethod.Horizontal;
        _countdownBar.fillOrigin    = 0;
        _countdownBar.fillAmount    = 0f;
        _countdownBar.raycastTarget = false;

        UITween.PopIn(panel, 0.45f, 0.86f, 0.12f);
    }

    // ---------------------------------------------------------------- BOTONAZO

    void BuildMainButton(RectTransform R)
    {
        // Halo exterior grande (respira durante la espera via ButtonCtrl)
        var glowGO = new GameObject("Glow");
        glowGO.transform.SetParent(R, false);
        var glowRT = glowGO.AddComponent<RectTransform>();
        glowRT.anchorMin = glowRT.anchorMax = V(0.5f, 0.5f);
        glowRT.pivot     = V(0.5f, 0.5f);
        glowRT.sizeDelta = V(430f, 430f);
        glowRT.anchoredPosition = V(0, 20f);
        var glowImg = glowGO.AddComponent<Image>();
        glowImg.sprite        = KidUI.CircleSpr;
        glowImg.color         = C(0.80f, 0.18f, 0.22f, 0.22f);
        glowImg.raycastTarget = false;

        // ARO DE ESTADO: anillo grueso alrededor del boton (rojo espera / verde ya)
        var ringGO = new GameObject("StateRing");
        ringGO.transform.SetParent(R, false);
        var ringRT = ringGO.AddComponent<RectTransform>();
        ringRT.anchorMin = ringRT.anchorMax = V(0.5f, 0.5f);
        ringRT.pivot     = V(0.5f, 0.5f);
        ringRT.sizeDelta = V(330f, 330f);
        ringRT.anchoredPosition = V(0, 20f);
        var ringImg = ringGO.AddComponent<Image>();
        ringImg.sprite        = KidUI.CircleSpr;
        ringImg.color         = C(1f, 1f, 1f, 0.10f);
        ringImg.raycastTarget = false;

        // Nucleo del botonazo
        var btnGO = new GameObject("MainBtn");
        btnGO.transform.SetParent(R, false);
        var btnRT = btnGO.AddComponent<RectTransform>();
        btnRT.anchorMin = btnRT.anchorMax = V(0.5f, 0.5f);
        btnRT.pivot     = V(0.5f, 0.5f);
        btnRT.sizeDelta = V(290f, 290f);
        btnRT.anchoredPosition = V(0, 20f);

        var btnImg = btnGO.AddComponent<Image>();
        btnImg.sprite = KidUI.CircleSpr;
        btnImg.color  = C(0.14f, 0.19f, 0.30f);

        MainButton = btnGO.AddComponent<Button>();
        MainButton.targetGraphic = btnImg;
        var cb = MainButton.colors;
        cb.normalColor      = Color.white;
        cb.highlightedColor = C(0.92f, 0.92f, 0.92f);
        cb.pressedColor     = C(0.70f, 0.70f, 0.70f);
        MainButton.colors   = cb;

        // Brillo especular (esquina superior izquierda del boton)
        var shineGO = new GameObject("Shine");
        shineGO.transform.SetParent(btnGO.transform, false);
        var shineRT = shineGO.AddComponent<RectTransform>();
        shineRT.anchorMin = shineRT.anchorMax = V(0.5f, 0.5f);
        shineRT.pivot     = V(0.5f, 0.5f);
        shineRT.sizeDelta = V(64f, 64f);
        shineRT.anchoredPosition = V(-72f, 82f);
        var shineImg = shineGO.AddComponent<Image>();
        shineImg.sprite        = KidUI.CircleSpr;
        shineImg.color         = C(1f, 1f, 1f, 0.16f);
        shineImg.raycastTarget = false;

        var btnTxtGO = new GameObject("BtnTxt");
        btnTxtGO.transform.SetParent(btnGO.transform, false);
        var tRT = btnTxtGO.AddComponent<RectTransform>();
        tRT.anchorMin = V(0, 0); tRT.anchorMax = V(1, 1);
        tRT.sizeDelta = V(0, 0); tRT.anchoredPosition = V(0, 0);
        var btnTxt = btnTxtGO.AddComponent<TextMeshProUGUI>();
        btnTxt.text          = "Preparado";
        btnTxt.color         = Color.white;
        btnTxt.fontSize      = 38f;
        btnTxt.fontStyle     = FontStyles.Bold;
        btnTxt.alignment     = TextAlignmentOptions.Center;
        btnTxt.overflowMode  = TextOverflowModes.Overflow;
        btnTxt.raycastTarget = false;

        ButtonCtrl             = btnGO.AddComponent<DontPressButtonController>();
        ButtonCtrl.ButtonImage = btnImg;
        ButtonCtrl.GlowImage   = glowImg;
        ButtonCtrl.RingImage   = ringImg;
        ButtonCtrl.ButtonText  = btnTxt;
        ButtonCtrl.SetIdle();
        ButtonRect = btnRT;

        // Entrada juicy del botonazo (una sola vez; sin escritores por frame)
        UITween.PopIn(btnRT, 0.55f, 0.70f, 0.05f);
    }

    // ---------------------------------------------------------------- API publica

    public void SetRoundDot(int index, bool? correct)
    {
        if (_roundDots == null || index >= _roundDots.Length) return;
        _roundDots[index].color = correct == null  ? C(1f, 1f, 1f, 0.18f)
                                : correct == true  ? CGREEN
                                                   : CRED;
        if (correct != null)
            UITween.PulseOnce(_roundDots[index].rectTransform, 1.35f, 0.28f);
    }

    public void SetStatusText(string txt, Color col)
    {
        if (_statusText == null) return;
        _statusText.text  = txt;
        _statusText.color = col;
        if (_statusDot != null)
        {
            _statusDot.color = col;
            UITween.PulseOnce(_statusDot.rectTransform, 1.30f, 0.22f);
        }
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
            _flashOverlay.color = Color.Lerp(start, C(0, 0, 0, 0), t / 0.45f);
            yield return null;
        }
        _flashOverlay.gameObject.SetActive(false);
    }

    // ---------------------------------------------------------------- helpers

    /// <summary>Se conserva por compatibilidad; internamente se usa KidUI.CircleSpr.</summary>
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

    /// <summary>Pastilla redondeada (Image con sprite 9-slice de KidUI).</summary>
    RectTransform Pill(RectTransform p, string n, Color col,
                       Vector2 am, Vector2 aM, float cornerScale)
    {
        var rt  = MkImg(p, n, col, am, aM, V(0, 0), V(0, 0));
        var img = rt.GetComponent<Image>();
        img.sprite                  = KidUI.RoundedSprite;
        img.type                    = Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = cornerScale;
        return rt;
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
