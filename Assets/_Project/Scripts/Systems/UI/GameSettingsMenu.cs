using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameSettingsMenu : MonoBehaviour
{

    public static GameSettingsMenu Instance { get; private set; }

    GameObject      _panel;
    Slider          _musicSlider;
    Slider          _clickSlider;
    Toggle          _musicToggle;
    TextMeshProUGUI _musicValLbl;
    TextMeshProUGUI _clickValLbl;

    bool _built = false;

    static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);
    static readonly Color BG      = C(0.04f, 0.06f, 0.12f, 0.97f);
    static readonly Color PANEL   = C(0.08f, 0.11f, 0.22f);
    static readonly Color HDR     = C(0.06f, 0.09f, 0.18f);
    static readonly Color ACCENT  = C(0.30f, 0.65f, 1.00f);
    static readonly Color DIM     = C(0.45f, 0.58f, 0.75f);
    static readonly Color BTNC    = C(0.12f, 0.18f, 0.32f);

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        BuildMenu();
        _panel.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
            Toggle();
        if (Input.GetKeyDown(KeyCode.Escape) && _panel.activeSelf)
            Close();
    }

    public void Toggle() { if (_panel.activeSelf) Close(); else Open(); }
    public void Open()   { _panel.SetActive(true);  RefreshFromManager(); }
    public void Close()  { _panel.SetActive(false); }

    void RefreshFromManager()
    {
        if (UIAudioManager.Instance == null) return;
        if (_musicSlider) _musicSlider.value = UIAudioManager.Instance.MusicVolume;
        if (_clickSlider) _clickSlider.value = UIAudioManager.Instance.ClickVolume;
        if (_musicToggle) _musicToggle.isOn  = UIAudioManager.Instance.MusicEnabled;
        UpdateLabels();
    }

    void UpdateLabels()
    {
        if (UIAudioManager.Instance == null) return;
        if (_musicValLbl) _musicValLbl.text = Mathf.RoundToInt(UIAudioManager.Instance.MusicVolume  * 100f) + "%";
        if (_clickValLbl) _clickValLbl.text = Mathf.RoundToInt(UIAudioManager.Instance.ClickVolume  * 100f) + "%";
    }

    void BuildMenu()
    {
        if (_built) return;
        _built = true;

        var cvGO = new GameObject("SettingsCanvas");
        cvGO.transform.SetParent(transform, false);
        var cv = cvGO.AddComponent<Canvas>();
        cv.renderMode   = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 200;
        var sc = cvGO.AddComponent<CanvasScaler>();
        sc.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920f, 1080f);
        sc.matchWidthOrHeight  = 0.5f;
        cvGO.AddComponent<GraphicRaycaster>();
        var R = cvGO.GetComponent<RectTransform>();

        _panel = new GameObject("SettingsPanel");
        _panel.transform.SetParent(R, false);
        var panelRT = _panel.AddComponent<RectTransform>();
        panelRT.anchorMin = Vector2.zero; panelRT.anchorMax = Vector2.one;
        panelRT.sizeDelta = Vector2.zero; panelRT.anchoredPosition = Vector2.zero;
        _panel.AddComponent<Image>().color = C(0, 0, 0, 0.75f);
        var pRT = panelRT;

        var card = MkImg(pRT, "Card", PANEL, new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f),
                         Vector2.zero, new Vector2(640f, 480f));

        var hdr = MkImg(card, "Hdr", HDR, new Vector2(0,1), new Vector2(1,1),
                        Vector2.zero, new Vector2(0, 72f));
        hdr.anchoredPosition = new Vector2(0, -36f);
        MkImg(card, "HdrLine", ACCENT, new Vector2(0,1), new Vector2(1,1),
              new Vector2(0,-72f), new Vector2(0,3f));

        var titleT = MkTxt(hdr, "Title", "AJUSTES", Color.white, 34,
                           new Vector2(0.04f,0), new Vector2(0.80f,1));
        titleT.fontStyle = FontStyles.Bold;
        titleT.alignment = TextAlignmentOptions.MidlineLeft;
        titleT.characterSpacing = 3f;

        var hintT = MkTxt(hdr, "Hint", "[M] para cerrar", DIM, 16,
                          new Vector2(0.72f,0.10f), new Vector2(0.98f,0.90f));
        hintT.alignment = TextAlignmentOptions.MidlineRight;

        MkSectionLabel(card, "Musica de fondo", ACCENT, 0.73f);

        _musicToggle = MkToggle(card, "Activar musica", 0.63f, isOn =>
        {
            if (UIAudioManager.Instance) UIAudioManager.Instance.MusicEnabled = isOn;
        });

        _musicValLbl = MkTxt(card, "MusicVal", "55%", DIM, 20,
                             new Vector2(0.78f, 0.52f), new Vector2(0.96f, 0.60f));
        _musicValLbl.alignment = TextAlignmentOptions.MidlineRight;

        _musicSlider = MkSlider(card, "MusicSlider", 0.52f, 0.55f, v =>
        {
            if (UIAudioManager.Instance) UIAudioManager.Instance.SetMusicVolume(v);
            UpdateLabels();
        });

        MkSectionLabel(card, "Sonido de clic", ACCENT, 0.40f);

        _clickValLbl = MkTxt(card, "ClickVal", "75%", DIM, 20,
                             new Vector2(0.78f, 0.29f), new Vector2(0.96f, 0.37f));
        _clickValLbl.alignment = TextAlignmentOptions.MidlineRight;

        _clickSlider = MkSlider(card, "ClickSlider", 0.29f, 0.75f, v =>
        {
            if (UIAudioManager.Instance) UIAudioManager.Instance.SetClickVolume(v);
            UpdateLabels();
        });

        MkBtn(card, "Cerrar", ACCENT,
              new Vector2(0.25f, 0.04f), new Vector2(0.75f, 0.15f), Close);

        UpdateLabels();
    }

    void MkSectionLabel(RectTransform p, string text, Color col, float anchorY)
    {
        var lbl = MkTxt(p, "Sec_" + text, text, col, 22,
                        new Vector2(0.05f, anchorY - 0.04f), new Vector2(0.75f, anchorY + 0.04f));
        lbl.fontStyle = FontStyles.Bold;
        lbl.alignment = TextAlignmentOptions.MidlineLeft;

        MkImg(p, "SecLine", C(ACCENT.r, ACCENT.g, ACCENT.b, 0.25f),
              new Vector2(0.05f, anchorY - 0.045f), new Vector2(0.95f, anchorY - 0.04f),
              Vector2.zero, Vector2.zero);
    }

    Toggle MkToggle(RectTransform p, string label, float anchorY, System.Action<bool> onChange)
    {
        var bg = MkImg(p, "Tog_" + label, C(0,0,0,0),
                       new Vector2(0.05f, anchorY - 0.04f), new Vector2(0.95f, anchorY + 0.04f),
                       Vector2.zero, Vector2.zero);

        var lbl = MkTxt(bg, "Lbl", label, DIM, 20, new Vector2(0.12f,0), new Vector2(0.85f,1));
        lbl.alignment = TextAlignmentOptions.MidlineLeft;

        var togGO = new GameObject("Toggle");
        togGO.transform.SetParent(bg, false);
        var togRT = togGO.AddComponent<RectTransform>();
        togRT.anchorMin = new Vector2(0.02f, 0.15f);
        togRT.anchorMax = new Vector2(0.10f, 0.85f);
        togRT.sizeDelta = Vector2.zero;
        togRT.anchoredPosition = Vector2.zero;

        var togBg = togGO.AddComponent<Image>();
        togBg.color = BTNC;

        var tog = togGO.AddComponent<Toggle>();
        tog.targetGraphic = togBg;
        tog.isOn = true;

        var checkGO = new GameObject("Check");
        checkGO.transform.SetParent(togGO.transform, false);
        var checkRT = checkGO.AddComponent<RectTransform>();
        checkRT.anchorMin = new Vector2(0.1f, 0.1f);
        checkRT.anchorMax = new Vector2(0.9f, 0.9f);
        checkRT.sizeDelta = Vector2.zero;
        var checkImg = checkGO.AddComponent<Image>();
        checkImg.color = ACCENT;
        tog.graphic = checkImg;

        tog.onValueChanged.AddListener(v => { togBg.color = v ? ACCENT : BTNC; onChange?.Invoke(v); });
        return tog;
    }

    Slider MkSlider(RectTransform p, string name, float anchorY, float defaultValue,
                    System.Action<float> onChange)
    {
        var bg = MkImg(p, name + "_Bg", C(0.06f, 0.08f, 0.16f),
                       new Vector2(0.05f, anchorY - 0.04f), new Vector2(0.77f, anchorY + 0.04f),
                       Vector2.zero, Vector2.zero);

        var sliderGO = new GameObject(name);
        sliderGO.transform.SetParent(bg, false);
        var sliderRT = sliderGO.AddComponent<RectTransform>();
        sliderRT.anchorMin = new Vector2(0.02f, 0.15f);
        sliderRT.anchorMax = new Vector2(0.98f, 0.85f);
        sliderRT.sizeDelta = Vector2.zero;
        sliderRT.anchoredPosition = Vector2.zero;

        var slider = sliderGO.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value    = defaultValue;

        var trackBg = new GameObject("TrackBg");
        trackBg.transform.SetParent(sliderGO.transform, false);
        var trackRT = trackBg.AddComponent<RectTransform>();
        trackRT.anchorMin = new Vector2(0, 0.35f); trackRT.anchorMax = new Vector2(1, 0.65f);
        trackRT.sizeDelta = Vector2.zero;
        var trackImg = trackBg.AddComponent<Image>();
        trackImg.color = C(0.15f, 0.18f, 0.30f);
        slider.targetGraphic = trackImg;

        var fillArea = new GameObject("FillArea");
        fillArea.transform.SetParent(sliderGO.transform, false);
        var fillAreaRT = fillArea.AddComponent<RectTransform>();
        fillAreaRT.anchorMin = new Vector2(0, 0.25f); fillAreaRT.anchorMax = new Vector2(1, 0.75f);
        fillAreaRT.offsetMin = new Vector2(5, 0); fillAreaRT.offsetMax = new Vector2(-15, 0);
        fillArea.AddComponent<Image>().color = Color.clear;

        var fillGO = new GameObject("Fill");
        fillGO.transform.SetParent(fillArea.transform, false);
        var fillRT = fillGO.AddComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero; fillRT.anchorMax = Vector2.one;
        fillRT.sizeDelta = new Vector2(10, 0);
        var fillImg = fillGO.AddComponent<Image>();
        fillImg.color = ACCENT;
        slider.fillRect = fillRT;

        var handleArea = new GameObject("HandleArea");
        handleArea.transform.SetParent(sliderGO.transform, false);
        var handleAreaRT = handleArea.AddComponent<RectTransform>();
        handleAreaRT.anchorMin = Vector2.zero; handleAreaRT.anchorMax = Vector2.one;
        handleAreaRT.offsetMin = new Vector2(10, 0); handleAreaRT.offsetMax = new Vector2(-10, 0);
        handleArea.AddComponent<Image>().color = Color.clear;

        var handleGO = new GameObject("Handle");
        handleGO.transform.SetParent(handleArea.transform, false);
        var handleRT = handleGO.AddComponent<RectTransform>();
        handleRT.anchorMin = handleRT.anchorMax = new Vector2(0.5f, 0.5f);
        handleRT.sizeDelta = new Vector2(22f, 22f);
        var handleImg = handleGO.AddComponent<Image>();
        handleImg.color = Color.white;
        slider.handleRect = handleRT;

        slider.onValueChanged.AddListener(v => onChange?.Invoke(v));
        return slider;
    }

    RectTransform MkImg(RectTransform p, string n, Color col,
                        Vector2 am, Vector2 aM, Vector2 pos, Vector2 sd)
    {
        var go = new GameObject(n); go.transform.SetParent(p, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = am; rt.anchorMax = aM; rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = sd;
        go.AddComponent<Image>().color = col;
        return rt;
    }

    TextMeshProUGUI MkTxt(RectTransform p, string n, string txt,
                          Color col, float sz, Vector2 am, Vector2 aM)
    {
        var go = new GameObject(n); go.transform.SetParent(p, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = am; rt.anchorMax = aM; rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero; rt.sizeDelta = Vector2.zero;
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = txt; t.color = col; t.fontSize = sz;
        t.alignment = TextAlignmentOptions.Center;
        t.overflowMode = TextOverflowModes.Overflow;
        return t;
    }

    void MkBtn(RectTransform p, string lbl, Color bg, Vector2 am, Vector2 aM,
               System.Action click)
    {
        var rt = MkImg(p, "Btn_" + lbl, bg, am, aM, Vector2.zero, Vector2.zero);
        MkImg(rt, "Sh", C(1,1,1,0.09f), new Vector2(0,0.5f), Vector2.one, Vector2.zero, Vector2.zero);
        var b = rt.gameObject.AddComponent<Button>();
        b.targetGraphic = rt.GetComponent<Image>();
        var cb = b.colors;
        cb.normalColor      = Color.white;
        cb.highlightedColor = new Color(1,1,1,0.82f);
        cb.pressedColor     = new Color(0.72f,0.72f,0.72f);
        b.colors = cb;
        b.onClick.AddListener(() => click?.Invoke());
        var t = MkTxt(rt, "T", lbl, Color.white, 26, Vector2.zero, Vector2.one);
        t.fontStyle = FontStyles.Bold;
    }
}
