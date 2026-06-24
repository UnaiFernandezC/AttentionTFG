using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TrackingUIController : MonoBehaviour
{
    public RectTransform CanvasRT  { get; private set; }
    public RectTransform ObjectRT  { get; private set; }

    Image           _objGlow1, _objGlow2, _objCore, _objShine;
    RectTransform   _progressFillRT;   // scaled by anchorMax.x — avoids Image.Type.Filled bugs
    Image           _progressFill;
    TextMeshProUGUI _progressPct, _statusLbl;
    GameObject      _resultPanel;
    TextMeshProUGUI _resultTitle, _resultSub;

    static Color C(float r,float g,float b,float a=1f)=>new Color(r,g,b,a);
    static readonly Color BG     = C(0.08f,0.10f,0.16f);
    static readonly Color HDR    = C(0.05f,0.08f,0.15f);
    static readonly Color PANEL  = C(0.08f,0.12f,0.22f);
    static readonly Color ACCENT = C(0.40f,0.72f,1.00f);
    static readonly Color DIM2   = C(0.30f,0.42f,0.62f);
    static readonly Color CGREEN = C(0.25f,0.90f,0.52f);
    static readonly Color CRED   = C(0.90f,0.28f,0.30f);
    static Vector2 V(float x,float y)=>new Vector2(x,y);

    // -------------------------------------------------------------------------
    public void BuildUI(Action onRestart, Action onMenu)
    {
        var cGO = new GameObject("Canvas_Tracking");
        cGO.transform.SetParent(transform,false);
        var cv = cGO.AddComponent<Canvas>();
        cv.renderMode   = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 5;
        var sc = cGO.AddComponent<CanvasScaler>();
        sc.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920f,1080f);
        sc.matchWidthOrHeight  = 0.5f;
        cGO.AddComponent<GraphicRaycaster>();
        CanvasRT = cGO.GetComponent<RectTransform>();

        // --- Background
        MkImg(CanvasRT,"BG",BG,V(0,0),V(1,1),V(0,0),V(0,0));
        MkImg(CanvasRT,"GradT",C(0.10f,0.20f,0.38f,0.30f),V(0,0.70f),V(1,1),V(0,0),V(0,0));

        // --- Header (top 88 px, y: 0.919–1.0)
        var hdr = MkImg(CanvasRT,"Hdr",HDR,V(0,1),V(1,1),V(0,-44),V(0,88));
        MkImg(hdr,"Line",ACCENT,V(0,0),V(1,0),V(0,1.5f),V(0,3));
        MkImg(hdr,"AccL",ACCENT,V(0,0.18f),V(0,0.82f),V(3,0),V(6,0));

        // Title — auto-sizing so it never overflows on narrower screens
        var ttl = MkTxt(hdr,"T","SEGUIMIENTO DE OBJETO",Color.white,32,V(0.03f,0.10f),V(0.62f,0.90f));
        ttl.fontStyle        = FontStyles.Bold;
        ttl.alignment        = TextAlignmentOptions.MidlineLeft;
        ttl.characterSpacing = 1.5f;
        ttl.enableAutoSizing = true;
        ttl.fontSizeMin      = 18f;
        ttl.fontSizeMax      = 32f;

        MkTxt(hdr,"Cat","ATENCION",DIM2,16,V(0.63f,0.12f),V(0.97f,0.88f))
            .alignment = TextAlignmentOptions.MidlineRight;

        // --- Info bar (below header, y: 0.856–0.916, ~65 px)
        //     IMPORTANT: must stay below y=0.919 so nothing overlaps the header
        BuildProgressSection(CanvasRT);

        // --- Tracking object (rendered above info bar in z-order)
        BuildObject(CanvasRT);

        // --- Status label (below info bar, floating over game area)
        _statusLbl = MkTxt(CanvasRT,"Status","",ACCENT,26,V(0.03f,0.79f),V(0.70f,0.851f));
        _statusLbl.fontStyle = FontStyles.Bold;
        _statusLbl.alignment = TextAlignmentOptions.MidlineLeft;

        // --- Bottom bar (80 px)
        var bot = MkImg(CanvasRT,"Bot",HDR,V(0,0),V(1,0),V(0,40),V(0,80));
        MkImg(bot,"BotLine",ACCENT,V(0,1),V(1,1),V(0,-1.5f),V(0,3));
        MkTxt(bot,"Instr","Manten el cursor sobre el objeto sin perderlo.",
            C(ACCENT.r+0.10f,ACCENT.g+0.10f,ACCENT.b+0.10f,1f),
            19,V(0.01f,0),V(0.78f,1)).alignment=TextAlignmentOptions.MidlineLeft;
        MkImg(bot,"Sep",C(1,1,1,0.10f),V(0.78f,0.1f),V(0.782f,0.9f),V(0,0),V(0,0));

        BuildResultPanel(CanvasRT, onRestart, onMenu);
    }

    // -------------------------------------------------------------------------
    void BuildObject(RectTransform R)
    {
        var go = new GameObject("TrackObj");
        go.transform.SetParent(R,false);
        ObjectRT = go.AddComponent<RectTransform>();
        ObjectRT.anchorMin = ObjectRT.anchorMax = new Vector2(0.5f,0.5f);
        ObjectRT.pivot     = new Vector2(0.5f,0.5f);
        ObjectRT.sizeDelta = Vector2.zero;
        ObjectRT.anchoredPosition = Vector2.zero;
        go.AddComponent<Image>().color = Color.clear;
        go.GetComponent<Image>().raycastTarget = false;

        _objGlow1 = AddCircleLayer(go.transform,"G1",new Vector2(200f,200f),C(ACCENT.r,ACCENT.g,ACCENT.b,0.06f));
        _objGlow2 = AddCircleLayer(go.transform,"G2",new Vector2(135f,135f),C(ACCENT.r,ACCENT.g,ACCENT.b,0.14f));
        _objCore  = AddCircleLayer(go.transform,"Core",new Vector2(88f,88f),ACCENT);
        _objShine = AddCircleLayer(go.transform,"Shine",new Vector2(28f,28f),C(1,1,1,0.45f));
        _objShine.rectTransform.anchoredPosition = new Vector2(-22f,25f);
    }

    Image AddCircleLayer(Transform parent,string name,Vector2 size,Color col)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent,false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f,0.5f);
        rt.pivot     = new Vector2(0.5f,0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = Vector2.zero;
        var img = go.AddComponent<Image>();
        img.color = col;
        img.raycastTarget = false;
        return img;
    }

    // -------------------------------------------------------------------------
    void BuildProgressSection(RectTransform R)
    {
        // Thin info-bar sitting between the header bottom and the game area.
        // Header occupies y = 0.919 – 1.0.
        // This bar occupies y = 0.856 – 0.916  (safe gap below header).
        var sect = MkImg(R,"ProgSect",C(0.05f,0.08f,0.16f,0.96f),V(0,0.856f),V(1,0.916f),V(0,0),V(0,0));
        // Subtle separator lines
        MkImg(sect,"LineT",C(1,1,1,0.06f),V(0,1),V(1,1),V(0,-1f),V(0,2));
        MkImg(sect,"LineB",C(1,1,1,0.06f),V(0,0),V(1,0),V(0, 1f),V(0,2));

        // Left label
        var lbl = MkTxt(sect,"PL","PROGRESO",DIM2,14,V(0.01f,0.50f),V(0.16f,1.00f));
        lbl.alignment = TextAlignmentOptions.MidlineLeft;

        // Progress bar trough (horizontal strip, lower half of sect)
        var barBg = MkImg(sect,"PBarBg",C(0.04f,0.07f,0.14f),V(0.16f,0.22f),V(0.80f,0.56f),V(0,0),V(0,0));
        MkImg(barBg,"BShine",C(1,1,1,0.06f),V(0,0.5f),V(1,1),V(0,0),V(0,0));

        // Fill child — we set anchorMax.x to match progress (0→1).
        // This avoids Image.Type.Filled which can misbehave without a source sprite.
        var fillGO = new GameObject("PFill");
        fillGO.transform.SetParent(barBg.transform,false);
        _progressFillRT                 = fillGO.AddComponent<RectTransform>();
        _progressFillRT.anchorMin       = Vector2.zero;
        _progressFillRT.anchorMax       = new Vector2(0f,1f);   // starts at 0 width
        _progressFillRT.sizeDelta       = Vector2.zero;
        _progressFillRT.anchoredPosition = Vector2.zero;
        _progressFill       = fillGO.AddComponent<Image>();
        _progressFill.color = ACCENT;

        // Right percentage label
        _progressPct = MkTxt(sect,"PP","0%",ACCENT,28,V(0.82f,0f),V(0.99f,1f));
        _progressPct.fontStyle = FontStyles.Bold;
        _progressPct.alignment = TextAlignmentOptions.MidlineRight;
    }

    // -------------------------------------------------------------------------
    void BuildResultPanel(RectTransform R, Action onRestart, Action onMenu)
    {
        _resultPanel = new GameObject("ResultPanel");
        _resultPanel.transform.SetParent(R,false);
        var er = _resultPanel.AddComponent<RectTransform>();
        er.anchorMin = Vector2.zero; er.anchorMax = Vector2.one;
        er.sizeDelta = Vector2.zero; er.anchoredPosition = Vector2.zero;
        _resultPanel.AddComponent<Image>().color = C(0,0,0,0.85f);

        var card = MkImg(er,"Card",PANEL,V(0.5f,0.5f),V(0.5f,0.5f),V(0,0),V(780f,440f));
        MkImg(card,"Sh",C(1,1,1,0.03f),V(0,0.5f),V(1,1),V(0,0),V(0,0));
        MkImg(card,"LineT",ACCENT,V(0,1),V(1,1),V(0,-4),V(0,8));
        MkImg(card,"AccL",ACCENT,V(0,0.08f),V(0,0.92f),V(4,0),V(8,0));

        _resultTitle = MkTxt(card,"RT","",Color.white,56,V(0.05f,0.72f),V(0.95f,0.97f));
        _resultTitle.fontStyle = FontStyles.Bold;
        _resultSub   = MkTxt(card,"RS","",C(0.48f,0.62f,0.80f),24,V(0.05f,0.26f),V(0.95f,0.72f));
        _resultSub.overflowMode = TextOverflowModes.Overflow;

        MkBtn(card,"Jugar de nuevo",ACCENT,  V(0.05f,0.21f),V(0.49f,0.34f),onRestart);
        MkBtn(card,"Volver a la seccion", C(0.20f,0.26f,0.38f), V(0.51f,0.21f),V(0.95f,0.34f),onMenu);
        MkBtn(card,"Menu principal", C(0.10f,0.13f,0.22f), V(0.05f,0.05f),V(0.95f,0.18f),() => SceneLoader.GoToMainMenu());

        _resultPanel.SetActive(false);
    }

    // -------------------------------------------------------------------------
    public void UpdateObjectVisuals(bool tracking, float pulseT)
    {
        float pulse = 1f + 0.06f * Mathf.Sin(pulseT * 3.5f);
        if (_objGlow1 != null) _objGlow1.rectTransform.localScale = Vector3.one * pulse;

        Color targetCore  = tracking ? new Color(0.25f,0.95f,0.52f) : ACCENT;
        Color targetGlow1 = tracking ? C(0.25f,0.95f,0.52f,0.10f)  : C(ACCENT.r,ACCENT.g,ACCENT.b,0.06f);
        Color targetGlow2 = tracking ? C(0.25f,0.95f,0.52f,0.20f)  : C(ACCENT.r,ACCENT.g,ACCENT.b,0.14f);

        float speed = 6f * Time.deltaTime;
        if (_objCore  != null) _objCore.color  = Color.Lerp(_objCore.color,  targetCore,  speed);
        if (_objGlow1 != null) _objGlow1.color = Color.Lerp(_objGlow1.color, targetGlow1, speed);
        if (_objGlow2 != null) _objGlow2.color = Color.Lerp(_objGlow2.color, targetGlow2, speed);
    }

    // Scale the fill by changing anchorMax.x instead of using fillAmount.
    // fillAmount on a sprite-less Image can misbehave (shows full rect at any value).
    public void SetProgress(float t)
    {
        t = Mathf.Clamp01(t);

        if (_progressFillRT != null)
        {
            var am = _progressFillRT.anchorMax;
            am.x = t;
            _progressFillRT.anchorMax = am;
        }

        Color fillColor = Color.Lerp(ACCENT, CGREEN, t);
        if (_progressFill != null) _progressFill.color = fillColor;
        if (_progressPct  != null)
        {
            _progressPct.text  = Mathf.RoundToInt(t * 100f) + "%";
            _progressPct.color = fillColor;
        }
    }

    public void SetStatus(string msg, Color col)
    {
        if (_statusLbl) { _statusLbl.text = msg; _statusLbl.color = col; }
    }

    public void ShowResult(bool win, string sub)
    {
        _resultTitle.text  = win ? "Completado!" : "Tiempo agotado";
        _resultTitle.color = win ? CGREEN : CRED;
        _resultSub.text    = sub;
        _resultPanel.SetActive(true);
    }

    // -------------------------------------------------------------------------
    RectTransform MkImg(RectTransform p,string n,Color col,Vector2 am,Vector2 aM,Vector2 pos,Vector2 sd)
    {
        var go = new GameObject(n); go.transform.SetParent(p,false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = am; rt.anchorMax = aM; rt.pivot = new Vector2(.5f,.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = sd;
        go.AddComponent<Image>().color = col;
        return rt;
    }

    TextMeshProUGUI MkTxt(RectTransform p,string n,string txt,Color col,float sz,Vector2 am,Vector2 aM)
    {
        var go = new GameObject(n); go.transform.SetParent(p,false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = am; rt.anchorMax = aM; rt.pivot = new Vector2(.5f,.5f);
        rt.anchoredPosition = Vector2.zero; rt.sizeDelta = Vector2.zero;
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = txt; t.color = col; t.fontSize = sz;
        t.alignment = TextAlignmentOptions.Center;
        t.overflowMode = TextOverflowModes.Overflow;
        return t;
    }

    void MkBtn(RectTransform p,string lbl,Color bg,Vector2 am,Vector2 aM,Action click)
    {
        var rt = MkImg(p,"Btn_"+lbl,bg,am,aM,V(0,0),V(0,0));
        MkImg(rt,"Sh",C(1,1,1,.09f),V(0,.5f),V(1,1),V(0,0),V(0,0));
        var b = rt.gameObject.AddComponent<Button>(); b.targetGraphic = rt.GetComponent<Image>();
        var cb = b.colors;
        cb.normalColor      = Color.white;
        cb.highlightedColor = new Color(1,1,1,.82f);
        cb.pressedColor     = new Color(.72f,.72f,.72f);
        b.colors = cb;
        b.onClick.AddListener(()=>click?.Invoke());
        var t = MkTxt(rt,"T",lbl,Color.white,24,V(0,0),V(1,1));
        t.fontStyle = FontStyles.Bold;
    }
}
