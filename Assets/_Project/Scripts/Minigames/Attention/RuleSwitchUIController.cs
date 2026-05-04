using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RuleSwitchUIController : MonoBehaviour
{

    static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);
    static Vector2 V(float x, float y) => new Vector2(x, y);

    static readonly Color BG      = C(0.08f, 0.10f, 0.16f);
    static readonly Color HDR     = C(0.05f, 0.08f, 0.15f);
    static readonly Color PANEL   = C(0.08f, 0.12f, 0.22f);
    static readonly Color ACCENT  = C(0.40f, 0.72f, 1.00f);
    static readonly Color DIM2    = C(0.30f, 0.42f, 0.62f);
    static readonly Color CGREEN  = C(0.25f, 0.90f, 0.52f);
    static readonly Color CRED    = C(0.90f, 0.28f, 0.30f);
    static readonly Color CYELLOW = C(0.96f, 0.72f, 0.18f);

    public RectTransform GameAreaRT { get; private set; }

    Image           _ruleDot;
    TextMeshProUGUI _ruleLbl;
    TextMeshProUGUI _scoreLbl;
    TextMeshProUGUI _progressLbl;
    Image           _timerFill;
    TextMeshProUGUI _statusLbl;

    GameObject      _resultPanel;
    TextMeshProUGUI _resultTitle, _resultSub;

    public RectTransform BuildUI(Action onRestart, Action onMenu)
    {

        var cGO = new GameObject("Canvas_RuleSwitch");
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

        MkImg(R, "BG",    BG,                                 V(0, 0),     V(1, 1),     V(0, 0), V(0, 0));
        MkImg(R, "GradT", C(0.10f, 0.20f, 0.38f, 0.24f),     V(0, 0.70f), V(1, 1),     V(0, 0), V(0, 0));

        var hdr = MkImg(R, "Hdr", HDR, V(0, 1), V(1, 1), V(0, -44), V(0, 88));
        MkImg(hdr, "Line", ACCENT, V(0, 0),     V(1, 0),     V(0, 1.5f), V(0, 3));
        MkImg(hdr, "AccL", ACCENT, V(0, 0.18f), V(0, 0.82f), V(3, 0),    V(6, 0));
        var ttl = MkTxt(hdr, "T", "CAMBIO DE REGLA", Color.white, 35, V(0.03f, 0.12f), V(0.62f, 0.88f));
        ttl.fontStyle = FontStyles.Bold; ttl.alignment = TextAlignmentOptions.MidlineLeft; ttl.characterSpacing = 2f;
        MkTxt(hdr, "Cat", "ATENCIÓN", DIM2, 16, V(0.62f, 0.12f), V(0.97f, 0.88f)).alignment = TextAlignmentOptions.MidlineRight;

        var info = MkImg(R, "InfoBar", C(0, 0, 0, 0.12f), V(0, 0.857f), V(1, 0.918f), V(0, 0), V(0, 0));

        BuildRuleDot(info);

        _ruleLbl = MkTxt(info, "RuleLbl", "Pulsa solo los ROJOS", ACCENT, 22,
                         V(0.04f, 0), V(0.52f, 1));
        _ruleLbl.fontStyle = FontStyles.Bold;
        _ruleLbl.alignment = TextAlignmentOptions.MidlineLeft;

        _scoreLbl = MkTxt(info, "Score", "0 pts", Color.white, 24, V(0.60f, 0), V(0.79f, 1));
        _scoreLbl.fontStyle = FontStyles.Bold;
        _scoreLbl.alignment = TextAlignmentOptions.MidlineRight;

        _progressLbl = MkTxt(info, "Prog", "0/15", DIM2, 18, V(0.80f, 0), V(0.99f, 1));
        _progressLbl.alignment = TextAlignmentOptions.MidlineRight;

        var timerBg = MkImg(R, "TimerBg", C(0.04f, 0.07f, 0.14f),
                            V(0, 0.817f), V(1, 0.857f), V(0, 0), V(0, 0));
        MkImg(timerBg, "TBShine", C(1, 1, 1, 0.04f), V(0, 0.55f), V(1, 1), V(0, 0), V(0, 0));

        var tfGO = new GameObject("TimerFill");
        tfGO.transform.SetParent(timerBg, false);
        var tfRT = tfGO.AddComponent<RectTransform>();
        tfRT.anchorMin = Vector2.zero; tfRT.anchorMax = Vector2.one;
        tfRT.sizeDelta = Vector2.zero; tfRT.anchoredPosition = Vector2.zero;
        _timerFill = tfGO.AddComponent<Image>();
        _timerFill.color      = ACCENT;
        _timerFill.type       = Image.Type.Filled;
        _timerFill.fillMethod = Image.FillMethod.Horizontal;
        _timerFill.fillAmount = 1f;

        var areaGO = new GameObject("GameArea");
        areaGO.transform.SetParent(R, false);
        GameAreaRT = areaGO.AddComponent<RectTransform>();
        GameAreaRT.anchorMin        = new Vector2(0.25f, 0.26f);
        GameAreaRT.anchorMax        = new Vector2(0.75f, 0.82f);
        GameAreaRT.sizeDelta        = Vector2.zero;
        GameAreaRT.anchoredPosition = Vector2.zero;
        areaGO.AddComponent<Image>().color = Color.clear;
        areaGO.GetComponent<Image>().raycastTarget = false;

        _statusLbl = MkTxt(R, "Status", "", DIM2, 30, V(0.10f, 0.16f), V(0.90f, 0.25f));
        _statusLbl.fontStyle = FontStyles.Bold;
        _statusLbl.alignment = TextAlignmentOptions.Center;

        var bot = MkImg(R, "Bot", HDR, V(0, 0), V(1, 0), V(0, 40), V(0, 80));
        MkImg(bot, "BotLine", ACCENT, V(0, 1), V(1, 1), V(0, -1.5f), V(0, 3));
        MkTxt(bot, "Instr", "Click en el estímulo para elegirlo · Sin click = rechazarlo",
              C(ACCENT.r + 0.10f, ACCENT.g + 0.10f, ACCENT.b + 0.10f, 1f),
              19, V(0.01f, 0), V(0.78f, 1)).alignment = TextAlignmentOptions.MidlineLeft;
        MkImg(bot, "Sep", C(1, 1, 1, 0.10f), V(0.78f, 0.1f), V(0.782f, 0.9f), V(0, 0), V(0, 0));
        MkBtn(bot, "Menu", C(0.12f, 0.20f, 0.36f), V(0.80f, 0.08f), V(0.99f, 0.92f), onMenu);

        BuildResultPanel(R, onRestart, onMenu);

        return GameAreaRT;
    }

    void BuildRuleDot(RectTransform parent)
    {
        var dotGO = new GameObject("RuleDot");
        dotGO.transform.SetParent(parent, false);
        var dotRT = dotGO.AddComponent<RectTransform>();
        dotRT.anchorMin = new Vector2(0.008f, 0.18f);
        dotRT.anchorMax = new Vector2(0.030f, 0.82f);
        dotRT.sizeDelta = Vector2.zero;
        dotRT.anchoredPosition = Vector2.zero;
        _ruleDot = dotGO.AddComponent<Image>();
        _ruleDot.color         = Color.white;
        _ruleDot.raycastTarget = false;
    }

    void BuildResultPanel(RectTransform R, Action onRestart, Action onMenu)
    {
        _resultPanel = new GameObject("ResultPanel");
        _resultPanel.transform.SetParent(R, false);
        var er = _resultPanel.AddComponent<RectTransform>();
        er.anchorMin = Vector2.zero; er.anchorMax = Vector2.one;
        er.sizeDelta = Vector2.zero; er.anchoredPosition = Vector2.zero;
        _resultPanel.AddComponent<Image>().color = C(0, 0, 0, 0.86f);

        var card = MkImg(er, "Card", PANEL, V(0.5f, 0.5f), V(0.5f, 0.5f), V(0, 0), V(820f, 420f));
        MkImg(card, "Sh",    C(1, 1, 1, 0.03f), V(0, 0.5f),    V(1, 1),     V(0, 0),  V(0, 0));
        MkImg(card, "LineT", ACCENT,             V(0, 1),       V(1, 1),     V(0, -4), V(0, 8));
        MkImg(card, "AccL",  ACCENT,             V(0, 0.08f),   V(0, 0.92f), V(4, 0),  V(8, 0));

        _resultTitle = MkTxt(card, "RT", "", Color.white, 52, V(0.05f, 0.74f), V(0.95f, 0.97f));
        _resultTitle.fontStyle = FontStyles.Bold;
        _resultSub = MkTxt(card, "RS", "", C(0.48f, 0.62f, 0.80f), 23, V(0.05f, 0.24f), V(0.95f, 0.72f));
        _resultSub.overflowMode = TextOverflowModes.Overflow;

        MkBtn(card, "Jugar de nuevo", ACCENT,                V(0.05f, 0.04f), V(0.46f, 0.18f), onRestart);
        MkBtn(card, "Menu",           C(0.14f, 0.22f, 0.38f), V(0.54f, 0.04f), V(0.95f, 0.18f), onMenu);

        _resultPanel.SetActive(false);
    }

    public void SetRuleLabel(string text, Color dotColor)
    {
        if (_ruleLbl  != null) _ruleLbl.text   = text;
        if (_ruleDot  != null) _ruleDot.color  = dotColor;
    }

    public void SetRuleIndicatorOnly(Color dotColor)
    {
        if (_ruleDot != null) _ruleDot.color = dotColor;
    }

    public void UpdateScore(int score)
    {
        if (_scoreLbl != null) _scoreLbl.text = score + " pts";
    }

    public void UpdateProgress(int current, int total)
    {
        if (_progressLbl != null) _progressLbl.text = current + "/" + total;
    }

    public void SetTimerBar(float t)
    {
        if (_timerFill == null) return;
        t = Mathf.Clamp01(t);
        _timerFill.fillAmount = t;
        _timerFill.color = Color.Lerp(CRED, ACCENT, t);
    }

    public void ShowStatus(string msg, Color col)
    {
        if (_statusLbl == null) return;
        _statusLbl.text  = msg;
        _statusLbl.color = col;
    }

    public void ClearStatus() => ShowStatus("", DIM2);

    public void ShowFinalResult(bool win, string sub)
    {
        _resultTitle.text  = win ? "¡Bien adaptado!" : "Necesitas practicar";
        _resultTitle.color = win ? CGREEN : CRED;
        _resultSub.text    = sub;
        _resultPanel.SetActive(true);
    }

    RectTransform MkImg(RectTransform p, string n, Color col, Vector2 am, Vector2 aM, Vector2 pos, Vector2 sd)
    {
        var go = new GameObject(n); go.transform.SetParent(p, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = am; rt.anchorMax = aM; rt.pivot = new Vector2(.5f, .5f);
        rt.anchoredPosition = pos; rt.sizeDelta = sd;
        go.AddComponent<Image>().color = col;
        return rt;
    }

    TextMeshProUGUI MkTxt(RectTransform p, string n, string txt, Color col, float sz, Vector2 am, Vector2 aM)
    {
        var go = new GameObject(n); go.transform.SetParent(p, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = am; rt.anchorMax = aM; rt.pivot = new Vector2(.5f, .5f);
        rt.anchoredPosition = Vector2.zero; rt.sizeDelta = Vector2.zero;
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = txt; t.color = col; t.fontSize = sz;
        t.alignment = TextAlignmentOptions.Center; t.overflowMode = TextOverflowModes.Overflow;
        return t;
    }

    void MkBtn(RectTransform p, string lbl, Color bg, Vector2 am, Vector2 aM, Action click)
    {
        var rt = MkImg(p, "Btn_" + lbl, bg, am, aM, V(0, 0), V(0, 0));
        MkImg(rt, "Sh", C(1, 1, 1, .09f), V(0, .5f), V(1, 1), V(0, 0), V(0, 0));
        var b = rt.gameObject.AddComponent<Button>(); b.targetGraphic = rt.GetComponent<Image>();
        var cb = b.colors;
        cb.normalColor      = Color.white;
        cb.highlightedColor = new Color(1, 1, 1, .82f);
        cb.pressedColor     = new Color(.72f, .72f, .72f);
        b.colors = cb;
        b.onClick.AddListener(() => click?.Invoke());
        var t = MkTxt(rt, "T", lbl, Color.white, 24, V(0, 0), V(1, 1));
        t.fontStyle = FontStyles.Bold;
    }
}
