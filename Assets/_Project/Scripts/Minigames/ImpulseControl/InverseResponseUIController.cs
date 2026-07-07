// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static InverseResponseStimulusManager;

public class InverseResponseUIController : MonoBehaviour
{

    static Vector2 V(float x, float y) => new Vector2(x, y);
    static Color   C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

    static readonly Color BG      = C(0.05f, 0.07f, 0.13f);
    static readonly Color HDR     = C(0.03f, 0.04f, 0.09f);
    static readonly Color PANEL   = C(0.07f, 0.10f, 0.20f);
    static readonly Color ACCENT  = C(0.18f, 0.80f, 0.58f);
    static readonly Color DIM     = C(0.38f, 0.52f, 0.63f);
    static readonly Color CRED    = C(0.88f, 0.22f, 0.28f);
    static readonly Color CGREEN  = C(0.20f, 0.86f, 0.52f);
    static readonly Color CYELLOW = C(0.96f, 0.80f, 0.15f);
    static readonly Color CARROW  = C(0.92f, 0.92f, 0.96f);

    public InverseResponseInputHandler InputHandler { get; private set; }

    /// <summary>Rect de la flecha central (para sacudidas de GameFeel).</summary>
    public RectTransform ArrowRect => _arrowParent;

    RectTransform    _arrowParent;
    Image[]          _arrowParts;
    Image            _arrowBg;

    TextMeshProUGUI  _ruleName;
    TextMeshProUGUI  _ruleDesc;
    Image            _rulePanelBg;
    RectTransform    _rulePanelRT;
    GameRule         _lastRule;
    bool             _hasLastRule;

    TextMeshProUGUI  _scoreText;
    TextMeshProUGUI  _feedbackText;
    Image            _timerBar;
    Image            _flashOverlay;

    public void BuildUI(int totalStimuli, InverseResponseInputHandler inputHandler)
    {
        InputHandler = inputHandler;

        var cGO = new GameObject("Canvas_InverseResp");
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

        MkImg(R, "BG", BG, V(0,0), V(1,1), V(0,0), V(0,0));

        BuildHeader(R, totalStimuli);

        BuildTimerBar(R);

        BuildArrowZone(R);

        BuildRuleBanner(R);

        _feedbackText = MkTxt(R, "Feedback", "", CGREEN, 32,
                              V(0.15f, 0.40f), V(0.85f, 0.47f));
        _feedbackText.fontStyle = FontStyles.Bold;
        _feedbackText.alignment = TextAlignmentOptions.Center;

        BuildDirectionButtons(R);

        var flashGO = new GameObject("Flash");
        flashGO.transform.SetParent(R, false);
        var fRT = flashGO.AddComponent<RectTransform>();
        fRT.anchorMin = V(0,0); fRT.anchorMax = V(1,1);
        fRT.sizeDelta = fRT.anchoredPosition = Vector2.zero;
        _flashOverlay = flashGO.AddComponent<Image>();
        _flashOverlay.color = C(0,0,0,0);
        flashGO.SetActive(false);

        var bot = MkImg(R, "Bot", HDR, V(0,0), V(1,0), V(0,40), V(0,80));
        MkImg(bot, "LineT", ACCENT, V(0,1), V(1,1), V(0,-1.5f), V(0,3));
        MkTxt(bot, "Info",
              "Flechas del teclado o WASD  •  Pulsa segun la regla activa",
              C(ACCENT.r, ACCENT.g - 0.08f, ACCENT.b - 0.05f),
              16, V(0.01f,0), V(0.78f,1)).alignment = TextAlignmentOptions.MidlineLeft;
        MkImg(bot, "Sep", C(1,1,1,0.08f), V(0.78f,0.1f), V(0.782f,0.9f), V(0,0), V(0,0));
    }

    void BuildHeader(RectTransform R, int totalStimuli)
    {
        var hdr = MkImg(R, "Hdr", HDR, V(0,1), V(1,1), V(0,-44), V(0,88));
        MkImg(hdr, "LineB", ACCENT, V(0,0), V(1,0), V(0,1.5f), V(0,3));
        MkImg(hdr, "AccL",  ACCENT, V(0,0.18f), V(0,0.82f), V(3,0), V(6,0));

        var ttl = MkTxt(hdr, "T", "RESPUESTA INVERSA", Color.white, 28,
                        V(0.03f,0.12f), V(0.48f,0.88f));
        ttl.fontStyle = FontStyles.Bold;
        ttl.alignment = TextAlignmentOptions.MidlineLeft;
        ttl.characterSpacing = 1.5f;

        MkTxt(hdr, "Cat", "CONTROL DE IMPULSOS", DIM, 14,
              V(0.48f,0.12f), V(0.68f,0.88f)).alignment = TextAlignmentOptions.MidlineRight;

        _scoreText = MkTxt(hdr, "Score", "0 / " + totalStimuli, CGREEN, 24,
                           V(0.80f,0.12f), V(0.98f,0.88f));
        _scoreText.fontStyle = FontStyles.Bold;
        _scoreText.alignment = TextAlignmentOptions.MidlineRight;
    }

    void BuildTimerBar(RectTransform R)
    {

        var panel = MkImg(R, "TimerPanel", C(0.04f,0.07f,0.14f,0.90f),
                          V(0,0.10f), V(0,0.90f), V(26f,0), V(20f,0));
        MkImg(panel, "R", ACCENT, V(1,0), V(1,1), V(-1f,0), V(2,0));

        var barBG = MkImg(panel, "BarBG", C(0.02f,0.04f,0.09f),
                          V(0.15f,0.04f), V(0.85f,0.96f), V(0,0), V(0,0));

        var fillGO = new GameObject("Fill");
        fillGO.transform.SetParent(barBG, false);
        var fillRT = fillGO.AddComponent<RectTransform>();
        fillRT.anchorMin = V(0,0); fillRT.anchorMax = V(1,1);
        fillRT.sizeDelta = fillRT.anchoredPosition = Vector2.zero;
        _timerBar             = fillGO.AddComponent<Image>();
        _timerBar.color       = CGREEN;
        _timerBar.type        = Image.Type.Filled;
        _timerBar.fillMethod  = Image.FillMethod.Vertical;
        _timerBar.fillOrigin  = 1;
        _timerBar.fillAmount  = 1f;

        MkTxt(panel, "Lbl", "T", DIM, 10, V(0,-0.04f), V(1,0.04f));
    }

    void BuildArrowZone(RectTransform R)
    {

        var bgGO = new GameObject("ArrowBG");
        bgGO.transform.SetParent(R, false);
        var bgRT = bgGO.AddComponent<RectTransform>();
        bgRT.anchorMin = bgRT.anchorMax = V(0.5f, 0.70f);
        bgRT.pivot     = V(0.5f, 0.5f);
        bgRT.sizeDelta = V(250f, 250f);
        bgRT.anchoredPosition = Vector2.zero;
        _arrowBg = bgGO.AddComponent<Image>();
        _arrowBg.sprite = MakeCircleSprite(128);
        _arrowBg.color  = C(0.10f, 0.14f, 0.26f);

        var arrowGO = new GameObject("Arrow");
        arrowGO.transform.SetParent(R, false);
        _arrowParent = arrowGO.AddComponent<RectTransform>();
        _arrowParent.anchorMin = _arrowParent.anchorMax = V(0.5f, 0.70f);
        _arrowParent.pivot     = V(0.5f, 0.5f);
        _arrowParent.sizeDelta = V(200f, 200f);
        _arrowParent.anchoredPosition = Vector2.zero;

        _arrowParts = BuildArrowShape(_arrowParent, CARROW);
    }

    void BuildRuleBanner(RectTransform R)
    {

        var panelGO = new GameObject("RulePanel");
        panelGO.transform.SetParent(R, false);
        var panelRT = panelGO.AddComponent<RectTransform>();
        panelRT.anchorMin = panelRT.anchorMax = V(0.5f, 0.515f);
        panelRT.pivot     = V(0.5f, 0.5f);
        panelRT.sizeDelta = V(560f, 92f);
        panelRT.anchoredPosition = Vector2.zero;
        _rulePanelRT = panelRT;
        _rulePanelBg = panelGO.AddComponent<Image>();
        _rulePanelBg.color = C(0.08f, 0.12f, 0.24f);

        var accL = new GameObject("AccL");
        accL.transform.SetParent(panelGO.transform, false);
        var aRT = accL.AddComponent<RectTransform>();
        aRT.anchorMin = V(0,0.1f); aRT.anchorMax = V(0,0.9f);
        aRT.sizeDelta = V(5,0); aRT.anchoredPosition = V(3,0);
        accL.AddComponent<Image>().color = CYELLOW;

        _ruleName = MkTxt(panelRT, "RuleName", "INVERSA", CYELLOW, 30,
                          V(0.04f, 0.45f), V(0.42f, 0.98f));
        _ruleName.fontStyle = FontStyles.Bold;
        _ruleName.alignment = TextAlignmentOptions.MidlineLeft;

        var sepGO = new GameObject("Sep");
        sepGO.transform.SetParent(panelGO.transform, false);
        var sepRT = sepGO.AddComponent<RectTransform>();
        sepRT.anchorMin = V(0.42f, 0.12f); sepRT.anchorMax = V(0.422f, 0.88f);
        sepRT.sizeDelta = sepRT.anchoredPosition = Vector2.zero;
        sepGO.AddComponent<Image>().color = C(1,1,1,0.12f);

        _ruleDesc = MkTxt(panelRT, "RuleDesc", "Pulsa la direccion CONTRARIA",
                          C(0.75f, 0.85f, 0.90f), 20,
                          V(0.44f, 0.05f), V(0.98f, 0.95f));
        _ruleDesc.alignment  = TextAlignmentOptions.MidlineLeft;
        _ruleDesc.fontStyle  = FontStyles.Normal;
        _ruleDesc.enableWordWrapping = false;
    }

    void BuildDirectionButtons(RectTransform R)
    {

        const float CY    = 0.285f;
        const float CX    = 0.500f;
        const float VSEP  = 100f / 1080f;
        const float HSEP  = 100f / 1920f;
        const float BSIZE = 80f;

        var layout = new (float ax, float ay, ArrowDirection dir)[]
        {
            (CX,        CY + VSEP, ArrowDirection.Up),
            (CX,        CY - VSEP, ArrowDirection.Down),
            (CX - HSEP, CY,        ArrowDirection.Left),
            (CX + HSEP, CY,        ArrowDirection.Right),
        };

        for (int i = 0; i < layout.Length; i++)
        {
            var (ax, ay, dir) = layout[i];
            BuildDirButton(R, ax, ay, BSIZE, dir);
        }
    }

    void BuildDirButton(RectTransform R, float ax, float ay,
                        float size, ArrowDirection dir)
    {

        var btnGO = new GameObject("DirBtn_" + dir);
        btnGO.transform.SetParent(R, false);
        var btnRT = btnGO.AddComponent<RectTransform>();
        btnRT.anchorMin = btnRT.anchorMax = V(ax, ay);
        btnRT.pivot     = V(0.5f, 0.5f);
        btnRT.sizeDelta = V(size, size);
        btnRT.anchoredPosition = Vector2.zero;

        var btnImg = btnGO.AddComponent<Image>();
        btnImg.sprite = MakeCircleSprite(64);
        btnImg.color  = C(0.12f, 0.16f, 0.28f);

        var btn = btnGO.AddComponent<Button>();
        btn.targetGraphic = btnImg;
        var cb = btn.colors;
        cb.normalColor      = Color.white;
        cb.highlightedColor = C(1f, 1f, 1f, 0.78f);
        cb.pressedColor     = C(0.60f, 0.60f, 0.60f);
        btn.colors = cb;
        btn.onClick.AddListener(() => InputHandler?.PressDirection(dir));
        ButtonJuice.Attach(btnGO);

        var miniGO = new GameObject("Mini");
        miniGO.transform.SetParent(btnGO.transform, false);
        var miniRT = miniGO.AddComponent<RectTransform>();
        miniRT.anchorMin = miniRT.anchorMax = V(0.5f, 0.5f);
        miniRT.pivot     = V(0.5f, 0.5f);
        miniRT.sizeDelta = V(size * 0.72f, size * 0.72f);
        miniRT.anchoredPosition = Vector2.zero;
        miniRT.localRotation = Quaternion.Euler(0, 0, DirectionToDeg(dir));
        BuildArrowShape(miniRT, CARROW);

        string key = dir == ArrowDirection.Up   ? "W / ↑"
                   : dir == ArrowDirection.Down  ? "S / ↓"
                   : dir == ArrowDirection.Left  ? "A / ←"
                   :                               "D / →";
        var lblGO = new GameObject("Key");
        lblGO.transform.SetParent(R, false);
        var lblRT = lblGO.AddComponent<RectTransform>();
        lblRT.anchorMin = lblRT.anchorMax = V(ax, ay);
        lblRT.pivot     = V(0.5f, 1.0f);
        lblRT.sizeDelta = V(120f, 28f);
        lblRT.anchoredPosition = V(0f, -(size * 0.5f + 6f));
        var lbl = lblGO.AddComponent<TextMeshProUGUI>();
        lbl.text      = key;
        lbl.color     = DIM;
        lbl.fontSize  = 14f;
        lbl.alignment = TextAlignmentOptions.Center;
        lbl.overflowMode = TextOverflowModes.Overflow;
    }

    public void ShowArrow(ArrowDirection dir, GameRule rule)
    {

        bool ruleChanged = _hasLastRule && rule != _lastRule;
        _lastRule    = rule;
        _hasLastRule = true;

        _arrowParent.localRotation = Quaternion.Euler(0, 0, DirectionToDeg(dir));
        UITween.PopIn(_arrowParent, 0.18f, 0.72f);

        if (ruleChanged)
        {
            GameFeel.PlayPop();
            UITween.PulseOnce(_rulePanelRT, 1.12f, 0.30f);
            GameFeel.FloatingText("¡CAMBIO DE REGLA!", C(0.96f, 0.62f, 0.18f),
                                  new Vector2(0f, -60f), 40f);
        }

        bool inv = rule == GameRule.Inverse;
        _arrowBg.color = inv ? C(0.12f, 0.08f, 0.24f) : C(0.08f, 0.20f, 0.14f);

        _rulePanelBg.color = inv ? C(0.10f, 0.08f, 0.22f) : C(0.06f, 0.16f, 0.10f);

        if (inv)
        {
            _ruleName.text  = "INVERSA";
            _ruleName.color = CYELLOW;
            _ruleDesc.text  = "Pulsa la direccion CONTRARIA";
        }
        else
        {
            _ruleName.text  = "IGUAL";
            _ruleName.color = CGREEN;
            _ruleDesc.text  = "Pulsa la misma direccion";
        }

        foreach (var p in _arrowParts) if (p) p.color = CARROW;
    }

    public void HideArrow()
    {
        foreach (var p in _arrowParts) if (p) p.color = C(0,0,0,0);
        if (_arrowBg) _arrowBg.color = C(0,0,0,0);
    }

    public void ShowArrowVisible()
    {
        foreach (var p in _arrowParts) if (p) p.color = CARROW;
    }

    public void UpdateTimerBar(float elapsed, float total)
    {
        if (_timerBar == null) return;
        float f = 1f - Mathf.Clamp01(elapsed / total);
        _timerBar.fillAmount = f;
        _timerBar.color = Color.Lerp(CRED, CGREEN, f);
    }

    public void UpdateScore(int correct, int errors, int total)
    {
        if (_scoreText == null) return;
        _scoreText.text  = correct + " / " + total;
        _scoreText.color = errors > 2 ? CYELLOW : CGREEN;
    }

    public void ShowFeedback(bool correct, string msg)
    {
        if (_feedbackText != null)
        {
            _feedbackText.text  = msg;
            _feedbackText.color = correct ? CGREEN : CRED;
        }
        Flash(correct ? C(0.08f, 0.50f, 0.18f, 0.18f) : C(0.72f, 0.08f, 0.12f, 0.22f));
        StartCoroutine(ClearFeedback(0.70f));
    }

    IEnumerator ClearFeedback(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (_feedbackText != null) _feedbackText.text = "";
    }

    void Flash(Color col)
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
        while (t < 0.40f)
        {
            t += Time.deltaTime;
            _flashOverlay.color = Color.Lerp(start, C(0,0,0,0), t / 0.40f);
            yield return null;
        }
        _flashOverlay.gameObject.SetActive(false);
    }

    static float DirectionToDeg(ArrowDirection d)
    {
        switch (d)
        {
            case ArrowDirection.Right: return   0f;
            case ArrowDirection.Up:    return  90f;
            case ArrowDirection.Left:  return 180f;
            default:                   return 270f;
        }
    }

    Image[] BuildArrowShape(RectTransform parent, Color col)
    {

        float w = parent.sizeDelta.x;
        float scale = w / 200f;

        var parts = new Image[3];

        var sGO = new GameObject("S");
        sGO.transform.SetParent(parent, false);
        var sRT = sGO.AddComponent<RectTransform>();
        sRT.anchoredPosition = V(-12f * scale, 0f);
        sRT.sizeDelta        = V(110f * scale, 22f * scale);
        sRT.pivot            = V(0.5f, 0.5f);
        parts[0] = sGO.AddComponent<Image>();
        parts[0].color = col;

        var tGO = new GameObject("T");
        tGO.transform.SetParent(parent, false);
        var tRT = tGO.AddComponent<RectTransform>();
        tRT.anchoredPosition = V(38f * scale, 24f * scale);
        tRT.sizeDelta        = V(64f * scale, 22f * scale);
        tRT.pivot            = V(0.5f, 0.5f);
        tRT.localRotation    = Quaternion.Euler(0, 0, -40f);
        parts[1] = tGO.AddComponent<Image>();
        parts[1].color = col;

        var bGO = new GameObject("B");
        bGO.transform.SetParent(parent, false);
        var bRT = bGO.AddComponent<RectTransform>();
        bRT.anchoredPosition = V(38f * scale, -24f * scale);
        bRT.sizeDelta        = V(64f * scale, 22f * scale);
        bRT.pivot            = V(0.5f, 0.5f);
        bRT.localRotation    = Quaternion.Euler(0, 0, 40f);
        parts[2] = bGO.AddComponent<Image>();
        parts[2].color = col;

        return parts;
    }

    public static Sprite MakeCircleSprite(int res = 128)
    {
        var tex = new Texture2D(res, res, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        var center = new Vector2(res * 0.5f, res * 0.5f);
        float r    = res * 0.5f;
        var px     = new Color[res * res];
        for (int y = 0; y < res; y++)
            for (int x = 0; x < res; x++)
            {
                float d = Vector2.Distance(new Vector2(x+.5f, y+.5f), center);
                float a = Mathf.Clamp01(1f - (d - r + 1.5f) / 2f);
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
        rt.pivot = V(0.5f, 0.5f);
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
        rt.pivot = V(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero; rt.sizeDelta = Vector2.zero;
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = txt; t.color = col; t.fontSize = sz;
        t.alignment    = TextAlignmentOptions.Center;
        t.overflowMode = TextOverflowModes.Overflow;
        return t;
    }
}
