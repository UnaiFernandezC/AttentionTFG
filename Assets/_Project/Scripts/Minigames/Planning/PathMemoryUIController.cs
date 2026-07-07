// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// HUD del minijuego Memoria de Ruta: banner superior, cuenta atras de
/// memorizacion, contador de pasos y boton de reiniciar ronda.
/// Los resultados finales los muestra MinigameBase.ShowResults.
/// </summary>
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

    GameObject _countdownGO;
    GameObject _progressGO;

    Action _onReiniciar;

    public void Init(Action onReiniciar)
    {
        _onReiniciar = onReiniciar;
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
        _progressText = MkTxt(bg, "Txt", "Pasos 0", ColDim, 24,
                              V(0f, 0f), V(1f, 1f));
        _progressGO.SetActive(false);
    }

    void BuildBottomBar()
    {
        var bgRe = MkImg(_hudRoot, "BtnRe", BtnBlue,
                         V(0.32f, 0.008f), V(0.68f, 0.065f), Vector2.zero, Vector2.zero);
        MkTxt(bgRe, "L", "REINICIAR RONDA", ColDim, 22, V(0f, 0f), V(1f, 1f));
        var btnRe = bgRe.gameObject.AddComponent<Button>();
        btnRe.targetGraphic = bgRe.GetComponent<Image>();
        SetBtnColors(btnRe, BtnBlue, BtnHover, BtnPress);
        btnRe.onClick.AddListener(() => _onReiniciar?.Invoke());
        ButtonJuice.Attach(bgRe.gameObject);
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

    public void ShowProgress(int moves, int optimal)
    {
        if (_progressGO == null) return;
        _progressGO.SetActive(true);
        _progressText.text = optimal > 0
            ? $"Pasos  {moves}   ·   Camino perfecto: {optimal}"
            : $"Pasos  {moves}";
    }

    public void HideProgress()
    {
        if (_progressGO != null) _progressGO.SetActive(false);
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
