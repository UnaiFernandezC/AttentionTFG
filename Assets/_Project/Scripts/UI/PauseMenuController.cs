using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PauseMenuController : MonoBehaviour
{

    public static PauseMenuController Instance { get; private set; }

    const string K_VOL_MASTER = "pm_vol_master";

    static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);
    static Vector2 V(float x, float y) => new Vector2(x, y);

    static readonly Color BG     = C(0.04f, 0.06f, 0.12f, 0.97f);
    static readonly Color PANEL  = C(0.07f, 0.10f, 0.20f);
    static readonly Color HDR    = C(0.05f, 0.08f, 0.16f);
    static readonly Color ACCENT = C(0.18f, 0.80f, 0.58f);
    static readonly Color DIM    = C(0.38f, 0.52f, 0.68f);
    static readonly Color CRED   = C(0.90f, 0.22f, 0.28f);
    static readonly Color CDIFF  = C(0.30f, 0.60f, 1.00f);

    bool       _isOpen;
    bool       _animating;
    Canvas     _canvas;
    CanvasGroup _cg;
    RectTransform _mainPanel;
    float _volMaster;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        _volMaster = PlayerPrefs.GetFloat(K_VOL_MASTER, 0.80f);
        AudioListener.volume = _volMaster;
    }

    void Start()
    {
        BuildUI();
        SetVisible(false, instant: true);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (_isOpen) Resume(); else Open();
        }
    }

    public void Open()
    {
        if (_animating) return;
        _isOpen = true;
        Time.timeScale = 0f;
        SetVisible(true);
        StartCoroutine(AnimateIn());
    }

    public void Resume()
    {
        if (_animating) return;
        StartCoroutine(AnimateOut(() =>
        {
            _isOpen = false;
            Time.timeScale = 1f;
            SetVisible(false, instant: true);
        }));
    }

    void BuildUI()
    {

        var cGO = new GameObject("PauseCanvas");
        cGO.transform.SetParent(transform, false);
        _canvas = cGO.AddComponent<Canvas>();
        _canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 100;
        var sc = cGO.AddComponent<CanvasScaler>();
        sc.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920f, 1080f);
        sc.matchWidthOrHeight  = 0.5f;
        cGO.AddComponent<GraphicRaycaster>();
        _cg = cGO.AddComponent<CanvasGroup>();
        var R = cGO.GetComponent<RectTransform>();

        MkImg(R, "Overlay", C(0.02f, 0.04f, 0.10f, 0.88f), V(0,0), V(1,1), V(0,0), V(0,0));

        for (int i = 0; i < 9; i++)
        {
            float y = (i + 1) / 10f;
            MkImg(R, $"Scan{i}", C(0.20f, 0.45f, 0.80f, 0.02f), V(0,y), V(1,y), V(0,1), V(0,2));
        }

        var panGO = new GameObject("MainPanel");
        panGO.transform.SetParent(R, false);
        _mainPanel = panGO.AddComponent<RectTransform>();
        _mainPanel.anchorMin = _mainPanel.anchorMax = new Vector2(0.5f, 0.5f);
        _mainPanel.pivot     = new Vector2(0.5f, 0.5f);
        _mainPanel.sizeDelta = new Vector2(680f, 420f);
        _mainPanel.anchoredPosition = Vector2.zero;
        panGO.AddComponent<Image>().color = BG;

        MkImg(_mainPanel, "BdrT",   ACCENT, V(0,1),      V(1,1),      V(0,-2),    V(0,4));
        MkImg(_mainPanel, "BdrB",   DIM,    V(0,0),      V(1,0),      V(0,2),     V(0,2));
        MkImg(_mainPanel, "BdrL",   ACCENT, V(0,0.1f),   V(0,0.9f),   V(3,0),     V(6,0));
        MkImg(_mainPanel, "NtchTL", ACCENT, V(0,1),      V(0,1),      V(24,-24),  V(40,4));
        MkImg(_mainPanel, "NtchTR", ACCENT, V(1,1),      V(1,1),      V(-24,-24), V(40,4));

        var hdr = MkImg(_mainPanel, "Header", HDR, V(0,0.80f), V(1,1), V(0,0), V(0,0));
        MkImg(hdr, "LineB", ACCENT, V(0,0), V(1,0), V(0,1.5f), V(0,3));
        MkImg(hdr, "AccL",  ACCENT, V(0,0.15f), V(0,0.85f), V(3,0), V(6,0));

        var ttl = MkTxt(hdr, "Title", "MENU DE PAUSA", Color.white, 32,
                        V(0.04f, 0.1f), V(0.82f, 0.9f));
        ttl.fontStyle = FontStyles.Bold;
        ttl.alignment = TextAlignmentOptions.MidlineLeft;
        ttl.characterSpacing = 3f;

        var dotGO = new GameObject("PulseDot");
        dotGO.transform.SetParent(hdr, false);
        var dRT = dotGO.AddComponent<RectTransform>();
        dRT.anchorMin = dRT.anchorMax = new Vector2(0.86f, 0.5f);
        dRT.pivot = new Vector2(0.5f, 0.5f);
        dRT.sizeDelta = new Vector2(14, 14);
        dRT.anchoredPosition = Vector2.zero;
        dotGO.AddComponent<Image>().color = CRED;
        dotGO.AddComponent<PauseMenuPulse>();

        var closeRT = MkImg(hdr, "CloseBtn", C(0.12f, 0.06f, 0.12f, 0.80f),
                            V(0.92f, 0.10f), V(0.995f, 0.90f), V(0,0), V(0,0));
        var closeT = MkTxt(closeRT, "XT", "X", Color.white, 26, V(0,0), V(1,1));
        closeT.fontStyle = FontStyles.Bold;
        closeT.alignment = TextAlignmentOptions.Center;
        var closeBtn = closeRT.gameObject.AddComponent<Button>();
        closeBtn.targetGraphic = closeRT.GetComponent<Image>();
        SetBtnColors(closeBtn);
        closeBtn.onClick.AddListener(Resume);

        var slRow = MkImg(_mainPanel, "SlRow", Color.clear, V(0.05f, 0.52f), V(0.95f, 0.76f), V(0,0), V(0,0));

        var lblT = MkTxt(slRow, "Lbl", "Volumen Master", Color.white, 19,
                         V(0.02f, 0.52f), V(0.60f, 0.95f));
        lblT.alignment = TextAlignmentOptions.MidlineLeft;

        var valTxt = MkTxt(slRow, "Val", Mathf.RoundToInt(_volMaster * 100) + " %", ACCENT, 18,
                           V(0.82f, 0.52f), V(1f, 0.95f));
        valTxt.alignment  = TextAlignmentOptions.MidlineRight;
        valTxt.fontStyle  = FontStyles.Bold;

        var trackBG = MkImg(slRow, "Track", C(0.06f, 0.10f, 0.20f),
                            V(0.02f, 0.08f), V(1, 0.48f), V(0,0), V(0,0));
        var fillImg = MkImg(trackBG, "Fill", ACCENT, V(0,0), V(_volMaster, 1), V(0,0), V(0,0));

        var slGO = new GameObject("SliderCtrl");
        slGO.transform.SetParent(slRow, false);
        var slRT = slGO.AddComponent<RectTransform>();
        slRT.anchorMin = new Vector2(0.02f, 0f); slRT.anchorMax = new Vector2(1f, 1f);
        slRT.sizeDelta = Vector2.zero; slRT.anchoredPosition = Vector2.zero;
        slGO.AddComponent<Image>().color = Color.clear;

        var sl = slGO.AddComponent<Slider>();
        sl.minValue  = 0f; sl.maxValue = 1f;
        sl.value     = _volMaster;
        sl.direction = Slider.Direction.LeftToRight;

        var hGO = new GameObject("Handle"); hGO.transform.SetParent(slGO.transform, false);
        var hRT = hGO.AddComponent<RectTransform>();
        hRT.anchorMin = hRT.anchorMax = new Vector2(_volMaster, 0.5f);
        hRT.pivot = new Vector2(0.5f, 0.5f); hRT.sizeDelta = new Vector2(24, 24);
        var hImg = hGO.AddComponent<Image>(); hImg.color = Color.white;
        sl.handleRect = hRT;
        sl.targetGraphic = hImg;

        var fillRT  = fillImg.GetComponent<RectTransform>();
        sl.onValueChanged.AddListener(v =>
        {
            _volMaster = v;
            AudioListener.volume = v;
            fillRT.anchorMax = new Vector2(v, 1);
            valTxt.text = Mathf.RoundToInt(v * 100) + " %";
            PlayerPrefs.SetFloat(K_VOL_MASTER, v);
            PlayerPrefs.Save();
        });

        MkImg(_mainPanel, "Sep", C(1,1,1,0.08f), V(0.05f, 0.48f), V(0.95f, 0.48f), V(0,1), V(0,2));

        BuildBtn(_mainPanel, "Cerrar menu", ACCENT,
                 V(0.05f, 0.08f), V(0.47f, 0.42f),
                 Resume, PANEL);

        BuildBtn(_mainPanel, "Elegir dificultad", CDIFF,
                 V(0.53f, 0.08f), V(0.95f, 0.42f),
                 GoToDifficulty, PANEL);
    }

    void GoToDifficulty()
    {
        Time.timeScale = 1f;
        _isOpen = false;
        SetVisible(false, instant: true);
        UnityEngine.SceneManagement.SceneManager.LoadScene("DifficultySelector");
    }

    void SetVisible(bool v, bool instant = false)
    {
        _canvas.gameObject.SetActive(v);
        if (instant)
        {
            _cg.alpha = v ? 1f : 0f;
            if (_mainPanel) _mainPanel.localScale = Vector3.one;
        }
    }

    IEnumerator AnimateIn()
    {
        _animating = true;
        float t = 0f, dur = 0.20f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.SmoothStep(0f, 1f, t / dur);
            _cg.alpha            = p;
            _mainPanel.localScale = Vector3.Lerp(new Vector3(0.92f, 0.92f, 1f), Vector3.one, p);
            yield return null;
        }
        _cg.alpha             = 1f;
        _mainPanel.localScale = Vector3.one;
        _animating = false;
    }

    IEnumerator AnimateOut(Action onDone)
    {
        _animating = true;
        float t = 0f, dur = 0.16f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.SmoothStep(0f, 1f, t / dur);
            _cg.alpha            = 1f - p;
            _mainPanel.localScale = Vector3.Lerp(Vector3.one, new Vector3(0.94f, 0.94f, 1f), p);
            yield return null;
        }
        _animating = false;
        onDone?.Invoke();
    }

    void BuildBtn(RectTransform p, string label, Color accentLine,
                  Vector2 am, Vector2 aM, Action onClick, Color bg)
    {
        var rt = MkImg(p, "Btn_" + label, bg, am, aM, V(0,0), V(0,0));

        MkImg(rt, "LineT", accentLine, V(0,1), V(1,1), V(0,-2), V(0,4));
        MkImg(rt, "Sh",    C(1,1,1,0.06f), V(0,0.5f), V(1,1), V(0,0), V(0,0));

        var btn = rt.gameObject.AddComponent<Button>();
        btn.targetGraphic = rt.GetComponent<Image>();
        SetBtnColors(btn);
        btn.onClick.AddListener(() => onClick?.Invoke());

        var t = MkTxt(rt, "T", label, Color.white, 22, V(0,0), V(1,1));
        t.fontStyle = FontStyles.Bold;
        t.alignment = TextAlignmentOptions.Center;
    }

    void SetBtnColors(Button btn)
    {
        var cols = btn.colors;
        cols.normalColor      = Color.white;
        cols.highlightedColor = new Color(1, 1, 1, 0.82f);
        cols.pressedColor     = new Color(0.75f, 0.75f, 0.75f);
        btn.colors = cols;
    }

    RectTransform MkImg(RectTransform p, string n, Color col,
                        Vector2 am, Vector2 aM, Vector2 pos, Vector2 sd)
    {
        var go = new GameObject(n); go.transform.SetParent(p, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = am; rt.anchorMax = aM; rt.pivot = new Vector2(.5f, .5f);
        rt.anchoredPosition = pos; rt.sizeDelta = sd;
        go.AddComponent<Image>().color = col;
        return rt;
    }

    TextMeshProUGUI MkTxt(RectTransform p, string n, string txt,
                          Color col, float sz, Vector2 am, Vector2 aM)
    {
        var go = new GameObject(n); go.transform.SetParent(p, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = am; rt.anchorMax = aM; rt.pivot = new Vector2(.5f, .5f);
        rt.anchoredPosition = Vector2.zero; rt.sizeDelta = Vector2.zero;
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = txt; t.color = col; t.fontSize = sz;
        t.alignment = TextAlignmentOptions.Center;
        t.overflowMode = TextOverflowModes.Overflow;
        return t;
    }
}

public class PauseMenuPulse : MonoBehaviour
{
    Image _img;
    void Start() => _img = GetComponent<Image>();
    void Update()
    {
        if (_img == null) return;
        float a = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 4f);
        _img.color = new Color(0.90f, 0.22f, 0.28f, a);
    }
}
