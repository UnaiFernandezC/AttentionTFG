using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PathMemoryUIController : MonoBehaviour
{
    static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);
    static Vector2 V(float x, float y) => new Vector2(x, y);

    static readonly Color BgDark   = C(0.07f, 0.09f, 0.15f);
    static readonly Color PanelBg  = C(0.10f, 0.13f, 0.22f);
    static readonly Color BtnBlue  = C(0.18f, 0.24f, 0.40f);
    static readonly Color BtnHover = C(0.26f, 0.36f, 0.56f);
    static readonly Color BtnPress = C(0.12f, 0.16f, 0.28f);
    static readonly Color ColWhite = Color.white;
    static readonly Color ColDim   = C(0.55f, 0.65f, 0.80f);

    Canvas        _hud;
    RectTransform _hudRoot;

    TextMeshProUGUI _bannerText;
    TextMeshProUGUI _countdownText;
    TextMeshProUGUI _progressText;

    GameObject      _countdownGO;
    GameObject      _progressGO;
    GameObject      _resultOverlayGO;

    Action _onReiniciar;
    Action _onMenu;

    public void Init(Action onReiniciar, Action onMenu)
    {
        _onReiniciar = onReiniciar;
        _onMenu      = onMenu;
    }

    public void BuildHUD()
    {
        _hud = new GameObject("PathHUDCanvas").AddComponent<Canvas>();
        _hud.renderMode   = RenderMode.ScreenSpaceOverlay;
        _hud.sortingOrder = 5;
        var sc = _hud.gameObject.AddComponent<CanvasScaler>();
        sc.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920f, 1080f);
        sc.matchWidthOrHeight  = 0.5f;
        _hud.gameObject.AddComponent<GraphicRaycaster>();
        _hudRoot = _hud.GetComponent<RectTransform>();

        BuildFrameBands();
        BuildBanner();
        BuildCountdown();
        BuildProgress();
        BuildBottomBar();
    }

    void BuildFrameBands()
    {

        MkImg(_hudRoot, "Top", BgDark, V(0f, 0.89f), V(1f, 1f), Vector2.zero, Vector2.zero);

        MkImg(_hudRoot, "Bot", BgDark, V(0f, 0f), V(1f, 0.075f), Vector2.zero, Vector2.zero);
    }

    void BuildBanner()
    {
        var bg = MkImg(_hudRoot, "BannerBg", PanelBg,
                       V(0.20f, 0.90f), V(0.80f, 0.98f), Vector2.zero, Vector2.zero);
        _bannerText = MkTxt(bg, "BannerTxt", "", ColWhite, 30,
                            V(0f, 0f), V(1f, 1f));
        _bannerText.fontStyle = FontStyles.Bold;
    }

    void BuildCountdown()
    {
        _countdownGO = new GameObject("CountdownBlock");
        _countdownGO.transform.SetParent(_hudRoot, false);
        var rt = _countdownGO.AddComponent<RectTransform>();
        rt.anchorMin = V(0.38f, 0.10f); rt.anchorMax = V(0.62f, 0.30f);
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

        var bg = MkImg(rt, "Bg", C(0f, 0f, 0f, 0.45f),
                       V(0f, 0f), V(1f, 1f), Vector2.zero, Vector2.zero);
        _countdownText = MkTxt(bg, "Txt", "4", C(1f, 0.85f, 0.15f), 90,
                               V(0f, 0f), V(1f, 1f));
        _countdownText.fontStyle = FontStyles.Bold;
        _countdownGO.SetActive(false);
    }

    void BuildProgress()
    {
        _progressGO = new GameObject("ProgressBlock");
        _progressGO.transform.SetParent(_hudRoot, false);
        var rt = _progressGO.AddComponent<RectTransform>();
        rt.anchorMin = V(0.30f, 0.78f); rt.anchorMax = V(0.70f, 0.89f);
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

        var bg = MkImg(rt, "Bg", PanelBg, V(0f, 0f), V(1f, 1f), Vector2.zero, Vector2.zero);
        _progressText = MkTxt(bg, "Txt", "Paso 0 / 0", ColDim, 24,
                              V(0f, 0f), V(1f, 1f));
        _progressGO.SetActive(false);
    }

    void BuildBottomBar()
    {

        var bgRe = MkImg(_hudRoot, "BtnRe", BtnBlue,
                         V(0.32f, 0.008f), V(0.68f, 0.065f), Vector2.zero, Vector2.zero);
        MkTxt(bgRe, "L", "REINICIAR", ColDim, 22, V(0f, 0f), V(1f, 1f));
        var btnRe = bgRe.gameObject.AddComponent<Button>();
        btnRe.targetGraphic = bgRe.GetComponent<Image>();
        SetBtnColors(btnRe, BtnBlue, BtnHover, BtnPress);
        btnRe.onClick.AddListener(() => _onReiniciar?.Invoke());

    }

    public void SetBannerText(string text, Color color)
    {
        if (_bannerText == null) return;
        _bannerText.text  = text;
        _bannerText.color = color;
    }

    public void ShowCountdown(int seconds)
    {
        if (_countdownGO == null) return;
        if (seconds <= 0) { _countdownGO.SetActive(false); return; }
        _countdownGO.SetActive(true);
        _countdownText.text  = seconds.ToString();

        _countdownText.color = seconds >= 3 ? C(0.20f, 0.90f, 0.40f) :
                               seconds == 2 ? C(1.00f, 0.85f, 0.15f) :
                                              C(0.95f, 0.30f, 0.30f);
    }

    public void HideCountdown()
    {
        if (_countdownGO != null) _countdownGO.SetActive(false);
    }

    public void ShowProgress(int current, int total)
    {
        if (_progressGO == null) return;
        _progressGO.SetActive(true);
        _progressText.text = $"Paso  {current}  /  {total}";
    }

    public void HideProgress()
    {
        if (_progressGO != null) _progressGO.SetActive(false);
    }

    public void ShowResult(bool win, string title, string subtitle,
                           string retryLabel, Action onRetry, Action onMenu)
    {

        ClearResult();

        var overlayRT = MkImg(_hudRoot, "Overlay",
                              new Color(0f, 0f, 0f, 0.78f),
                              V(0f, 0f), V(1f, 1f), Vector2.zero, Vector2.zero);
        _resultOverlayGO = overlayRT.gameObject;

        var card = MkImg(overlayRT, "Card", C(0.10f, 0.13f, 0.24f),
                         V(0.28f, 0.28f), V(0.72f, 0.72f), Vector2.zero, Vector2.zero);

        Color titleCol = win ? C(0.20f, 0.90f, 0.50f) : C(0.95f, 0.30f, 0.30f);
        MkTxt(card, "T", title, titleCol, 46, V(0.04f, 0.58f), V(0.96f, 0.88f));
        MkTxt(card, "S", subtitle, C(0.72f, 0.85f, 0.96f), 22,
              V(0.06f, 0.42f), V(0.94f, 0.60f));

        Color retryCol = win ? C(0.18f, 0.56f, 0.32f) : C(0.18f, 0.38f, 0.72f);
        var bgRe = MkImg(card, "Re", retryCol,
                         V(0.06f, 0.19f), V(0.48f, 0.31f), Vector2.zero, Vector2.zero);
        MkTxt(bgRe, "L", retryLabel, ColWhite, 20, V(0f, 0f), V(1f, 1f));
        var btnRe = bgRe.gameObject.AddComponent<Button>();
        btnRe.targetGraphic = bgRe.GetComponent<Image>();
        btnRe.onClick.AddListener(() => onRetry?.Invoke());

        var bgMe = MkImg(card, "Me", C(0.22f, 0.22f, 0.34f),
                         V(0.52f, 0.19f), V(0.94f, 0.31f), Vector2.zero, Vector2.zero);
        MkTxt(bgMe, "L", "VOLVER A LA SECCION", ColDim, 16, V(0f, 0f), V(1f, 1f));
        var btnMe = bgMe.gameObject.AddComponent<Button>();
        btnMe.targetGraphic = bgMe.GetComponent<Image>();
        btnMe.onClick.AddListener(() => onMenu?.Invoke());

        var bgMa = MkImg(card, "Ma", C(0.14f, 0.16f, 0.26f),
                         V(0.06f, 0.05f), V(0.94f, 0.16f), Vector2.zero, Vector2.zero);
        MkTxt(bgMa, "L", "MENU PRINCIPAL", ColDim, 16, V(0f, 0f), V(1f, 1f));
        var btnMa = bgMa.gameObject.AddComponent<Button>();
        btnMa.targetGraphic = bgMa.GetComponent<Image>();
        btnMa.onClick.AddListener(() => SceneLoader.GoToMainMenu());
    }

    public void ClearResult()
    {
        if (_resultOverlayGO != null)
        {
            UnityEngine.Object.Destroy(_resultOverlayGO);
            _resultOverlayGO = null;
        }
    }

    static RectTransform MkImg(RectTransform p, string name, Color col,
                                Vector2 amin, Vector2 amax, Vector2 pos, Vector2 sd)
    {
        var go = new GameObject(name);
        go.transform.SetParent(p, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = amin; rt.anchorMax = amax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = sd;
        go.AddComponent<Image>().color = col;
        return rt;
    }

    static TextMeshProUGUI MkTxt(RectTransform p, string name, string text,
                                  Color col, float size, Vector2 amin, Vector2 amax)
    {
        var go = new GameObject(name);
        go.transform.SetParent(p, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = amin; rt.anchorMax = amax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero; rt.sizeDelta = Vector2.zero;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.color = col; tmp.fontSize = size;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.overflowMode = TextOverflowModes.Overflow;
        return tmp;
    }

    static void SetBtnColors(Button btn, Color n, Color h, Color p)
    {
        var cb = btn.colors;
        cb.normalColor = n; cb.highlightedColor = h; cb.pressedColor = p;
        cb.selectedColor = n; btn.colors = cb;
    }
}
