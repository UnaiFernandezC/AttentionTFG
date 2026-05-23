using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using TMPro;

/// <summary>
/// Menú de pausa avanzado — se activa con ESC.
/// Añade este componente a cualquier GameObject en la escena.
/// Construye toda la UI por código, sin prefabs.
/// Persiste ajustes con PlayerPrefs.
/// </summary>
public class PauseMenuController : MonoBehaviour
{
    // ── Singleton ────────────────────────────────────────────────────────────
    public static PauseMenuController Instance { get; private set; }

    // ── PlayerPrefs keys ─────────────────────────────────────────────────────
    const string K_VOL_MASTER  = "pm_vol_master";
    const string K_VOL_MUSIC   = "pm_vol_music";
    const string K_VOL_SFX     = "pm_vol_sfx";
    const string K_MUTE        = "pm_mute";
    const string K_FULLSCREEN  = "pm_fullscreen";
    const string K_QUALITY     = "pm_quality";
    const string K_BRIGHTNESS  = "pm_brightness";
    const string K_TEXT_SIZE   = "pm_textsize";
    const string K_HIGH_CONT   = "pm_highcontrast";
    const string K_REDUCE_ANIM = "pm_reduceAnim";
    const string K_COLORBLIND  = "pm_colorblind";
    const string K_SHOW_FPS    = "pm_showfps";

    // ── Color palette (matches project) ──────────────────────────────────────
    static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);
    static Vector2 V(float x, float y) => new Vector2(x, y);

    static readonly Color BG      = C(0.04f, 0.06f, 0.12f, 0.97f);
    static readonly Color PANEL   = C(0.07f, 0.10f, 0.20f);
    static readonly Color PANEL2  = C(0.09f, 0.14f, 0.26f);
    static readonly Color HDR     = C(0.05f, 0.08f, 0.16f);
    static readonly Color ACCENT  = C(0.18f, 0.80f, 0.58f);   // teal
    static readonly Color ACCENT2 = C(0.30f, 0.60f, 1.00f);   // blue
    static readonly Color DIM     = C(0.38f, 0.52f, 0.68f);
    static readonly Color CRED    = C(0.90f, 0.22f, 0.28f);
    static readonly Color CGREEN  = C(0.22f, 0.86f, 0.54f);
    static readonly Color CYELLOW = C(0.96f, 0.78f, 0.18f);
    static readonly Color SIDEBAR = C(0.05f, 0.08f, 0.17f);

    // ── Tab enum ─────────────────────────────────────────────────────────────
    enum Tab { Audio, Pantalla, Accesibilidad, Control, Partida }

    // ── Runtime state ────────────────────────────────────────────────────────
    bool _isOpen;
    Tab  _activeTab = Tab.Audio;
    bool _animating;

    // ── UI references ────────────────────────────────────────────────────────
    Canvas         _canvas;
    CanvasGroup    _canvasGroup;
    RectTransform  _mainPanel;
    RectTransform  _contentRoot;
    GameObject[]   _tabContents;
    Button[]       _tabButtons;
    Image[]        _tabHighlights;
    TextMeshProUGUI _sceneLbl;
    TextMeshProUGUI _diffLbl;
    TextMeshProUGUI _scoreLbl;
    TextMeshProUGUI _fpsTxt;
    GameObject      _fpsGO;

    // Settings values
    float _volMaster, _volMusic, _volSfx;
    bool  _muted, _fullscreen, _highContrast, _reduceAnim, _showFps;
    int   _quality, _textSize, _colorblind;
    float _brightness;

    // Slider / Toggle refs for live update
    Slider  _slMaster, _slMusic, _slSfx, _slBrightness, _slTextSize;
    Toggle  _tgMute, _tgFullscreen, _tgHighContrast, _tgReduceAnim, _tgShowFps;
    TMP_Dropdown _ddQuality, _ddColorblind;

    // FPS tracking
    float _fpsTimer;
    int   _frameCount;
    float _currentFps;

    // ── Tab metadata ─────────────────────────────────────────────────────────
    static readonly string[] TAB_ICONS  = { "♪",  "⊡",  "☉",  "⌘",  "▣" };
    static readonly string[] TAB_NAMES  = { "Audio", "Pantalla", "Accesibilidad", "Control", "Partida" };

    // ═════════════════════════════════════════════════════════════════════════
    // Unity lifecycle
    // ═════════════════════════════════════════════════════════════════════════

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadSettings();
    }

    void Start()
    {
        BuildUI();
        ApplyAllSettings();
        SetVisible(false, instant: true);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (_isOpen) Resume(); else Open();
        }

        // FPS counter
        if (_showFps)
        {
            _frameCount++;
            _fpsTimer += Time.unscaledDeltaTime;
            if (_fpsTimer >= 0.5f)
            {
                _currentFps = _frameCount / _fpsTimer;
                _frameCount = 0; _fpsTimer = 0f;
                if (_fpsTxt != null) _fpsTxt.text = $"FPS  {_currentFps:F0}";
            }
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Public API
    // ═════════════════════════════════════════════════════════════════════════

    public void Open()
    {
        if (_animating) return;
        _isOpen = true;
        Time.timeScale = 0f;
        RefreshSessionInfo();
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

    // ═════════════════════════════════════════════════════════════════════════
    // Build UI
    // ═════════════════════════════════════════════════════════════════════════

    void BuildUI()
    {
        // ── Canvas ──
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
        _canvasGroup = cGO.AddComponent<CanvasGroup>();
        var R = cGO.GetComponent<RectTransform>();

        // ── Full-screen darkening + scan lines ──
        MkImg(R, "Overlay", C(0.02f, 0.04f, 0.10f, 0.88f), V(0,0), V(1,1), V(0,0), V(0,0));
        BuildScanlineDecor(R);

        // ── Animated FPS badge (top-right) ──
        _fpsGO = new GameObject("FPS_Badge");
        _fpsGO.transform.SetParent(R, false);
        var fpsRT = _fpsGO.AddComponent<RectTransform>();
        fpsRT.anchorMin = fpsRT.anchorMax = new Vector2(1, 1);
        fpsRT.pivot     = new Vector2(1, 1);
        fpsRT.sizeDelta = new Vector2(130, 36);
        fpsRT.anchoredPosition = new Vector2(-14, -14);
        _fpsGO.AddComponent<Image>().color = C(0.05f,0.09f,0.18f,0.80f);
        _fpsTxt = MkTxt(fpsRT,"FpsT","FPS  --", ACCENT, 18, V(0,0), V(1,1));
        _fpsTxt.alignment = TextAlignmentOptions.Center;
        _fpsTxt.fontStyle = FontStyles.Bold;

        // ── Main panel ──
        var panGO = new GameObject("MainPanel");
        panGO.transform.SetParent(R, false);
        _mainPanel = panGO.AddComponent<RectTransform>();
        _mainPanel.anchorMin = _mainPanel.anchorMax = new Vector2(0.5f, 0.5f);
        _mainPanel.pivot     = new Vector2(0.5f, 0.5f);
        _mainPanel.sizeDelta = new Vector2(1260, 720);
        _mainPanel.anchoredPosition = Vector2.zero;
        var panImg = panGO.AddComponent<Image>(); panImg.color = BG;

        // Border glow
        BuildBorderGlow(_mainPanel);

        // ── Header bar ──
        BuildHeader(_mainPanel);

        // ── Left sidebar ──
        BuildSidebar(_mainPanel);

        // ── Content area ──
        var cntGO = new GameObject("ContentArea");
        cntGO.transform.SetParent(_mainPanel, false);
        _contentRoot = cntGO.AddComponent<RectTransform>();
        _contentRoot.anchorMin = new Vector2(0.175f, 0.09f);
        _contentRoot.anchorMax = new Vector2(0.98f,  0.87f);
        _contentRoot.sizeDelta = Vector2.zero;
        _contentRoot.anchoredPosition = Vector2.zero;
        cntGO.AddComponent<Image>().color = Color.clear;

        // ── Tab content panels ──
        _tabContents = new GameObject[5];
        _tabContents[0] = BuildTabAudio(_contentRoot);
        _tabContents[1] = BuildTabPantalla(_contentRoot);
        _tabContents[2] = BuildTabAccesibilidad(_contentRoot);
        _tabContents[3] = BuildTabControl(_contentRoot);
        _tabContents[4] = BuildTabPartida(_contentRoot);

        // ── Footer buttons ──
        BuildFooter(_mainPanel);

        // ── Activate first tab ──
        SwitchTab(Tab.Audio, instant: true);
    }

    // ── Decorative scanlines in background ───────────────────────────────────
    void BuildScanlineDecor(RectTransform R)
    {
        // Horizontal scan lines (very subtle)
        for (int i = 0; i < 9; i++)
        {
            float y = (i + 1) / 10f;
            MkImg(R, $"Scan{i}", C(0.20f, 0.45f, 0.80f, 0.025f), V(0,y), V(1,y), V(0,1), V(0,2));
        }
        // Corner accent triangles
        var corners = new[] { V(0,0), V(1,0), V(0,1), V(1,1) };
        // just use a small teal line in each corner
        MkImg(R, "CrnTL", ACCENT, V(0,1), V(0.02f,1), V(0,0), V(0,4));
        MkImg(R, "CrnBL", ACCENT, V(0,0), V(0.02f,0), V(0,0), V(0,4));
    }

    // ── Glowing border around main panel ─────────────────────────────────────
    void BuildBorderGlow(RectTransform p)
    {
        // Top accent line
        MkImg(p, "BdrT", ACCENT,  V(0,1), V(1,1),   V(0,-2),   V(0,4));
        // Bottom dim line
        MkImg(p, "BdrB", DIM,     V(0,0), V(1,0),   V(0,2),    V(0,2));
        // Left accent bar
        MkImg(p, "BdrL", ACCENT,  V(0,0.1f), V(0,0.9f), V(3,0), V(6,0));
        // Teal corner notch top-left
        MkImg(p, "NtchTL", ACCENT, V(0,1), V(0,1), V(24,-24), V(40,4));
        MkImg(p, "NtchTLv", ACCENT,V(0,1), V(0,1), V(2,-22),  V(4,44));
        // Top-right corner notch
        MkImg(p, "NtchTR", ACCENT, V(1,1), V(1,1), V(-24,-24), V(40,4));
        MkImg(p, "NtchTRv", ACCENT,V(1,1), V(1,1), V(-2,-22),  V(4,44));
    }

    // ── Header ───────────────────────────────────────────────────────────────
    void BuildHeader(RectTransform p)
    {
        var hdr = MkImg(p, "Header", HDR, V(0,0.87f), V(1,1), V(0,0), V(0,0));
        MkImg(hdr,"LineB", ACCENT, V(0,0),    V(1,0), V(0,1.5f), V(0,3));
        MkImg(hdr,"AccL",  ACCENT, V(0,0.15f),V(0,0.85f), V(3,0), V(6,0));

        // Animated "PAUSA" title
        var ttl = MkTxt(hdr,"Title","⏸  MENÚ DE PAUSA", Color.white, 38,
                        V(0.03f,0.1f), V(0.45f,0.9f));
        ttl.fontStyle = FontStyles.Bold;
        ttl.alignment = TextAlignmentOptions.MidlineLeft;
        ttl.characterSpacing = 3f;

        // Pulse dot
        var dotGO = new GameObject("PulseDot");
        dotGO.transform.SetParent(hdr, false);
        var dRT = dotGO.AddComponent<RectTransform>();
        dRT.anchorMin = dRT.anchorMax = new Vector2(0.44f, 0.5f);
        dRT.pivot     = new Vector2(0.5f, 0.5f);
        dRT.sizeDelta = new Vector2(14,14);
        dRT.anchoredPosition = Vector2.zero;
        dotGO.AddComponent<Image>().color = CRED;
        dotGO.AddComponent<PauseMenuPulse>();  // small helper below

        // Scene + difficulty info (right side of header)
        _sceneLbl = MkTxt(hdr,"Scene","", DIM, 20, V(0.52f,0.52f), V(0.85f,0.95f));
        _sceneLbl.alignment = TextAlignmentOptions.MidlineLeft;
        _diffLbl  = MkTxt(hdr,"Diff","",  DIM, 18, V(0.52f,0.05f), V(0.85f,0.50f));
        _diffLbl.alignment = TextAlignmentOptions.MidlineLeft;

        // Close [X] button
        var closeRT = MkImg(hdr, "CloseBtn", C(0.12f,0.06f,0.12f,0.80f),
                            V(0.93f,0.12f), V(0.995f,0.88f), V(0,0), V(0,0));
        MkImg(closeRT,"CHov", C(CRED.r,CRED.g,CRED.b,0.15f), V(0,0),V(1,1),V(0,0),V(0,0));
        var closeTxt = MkTxt(closeRT,"XT","✕", Color.white, 30, V(0,0), V(1,1));
        closeTxt.fontStyle = FontStyles.Bold; closeTxt.alignment = TextAlignmentOptions.Center;
        var closeBtn = closeRT.gameObject.AddComponent<Button>();
        closeBtn.targetGraphic = closeRT.GetComponent<Image>();
        closeBtn.onClick.AddListener(Resume);
    }

    // ── Left sidebar ─────────────────────────────────────────────────────────
    void BuildSidebar(RectTransform p)
    {
        var sb = MkImg(p, "Sidebar", SIDEBAR, V(0,0.09f), V(0.175f,0.87f), V(0,0), V(0,0));
        MkImg(sb,"LineR", C(0.18f,0.80f,0.58f,0.25f), V(1,0), V(1,1), V(-1,0), V(2,0));

        _tabButtons    = new Button[5];
        _tabHighlights = new Image[5];

        for (int i = 0; i < 5; i++)
        {
            float yMax = 1f - i * 0.19f;
            float yMin = yMax - 0.17f;

            var row = MkImg(sb, $"TabRow{i}", Color.clear, V(0,yMin), V(1,yMax), V(0,0), V(0,0));

            // Active highlight bar (left edge)
            var hlImg = MkImg(row,"HL", ACCENT, V(0,0.1f), V(0,0.9f), V(3,0), V(6,0));
            _tabHighlights[i] = hlImg.GetComponent<Image>();

            // Hover background
            var bgImg = MkImg(row, "RowBG", Color.clear, V(0,0), V(1,1), V(0,0), V(0,0));
            bgImg.GetComponent<Image>().color = C(0.18f,0.80f,0.58f,0f);

            // Icon
            var icon = MkTxt(row, "Icon", TAB_ICONS[i], Color.white, 28,
                             V(0.05f, 0.25f), V(0.38f, 0.75f));
            icon.alignment = TextAlignmentOptions.Center;
            icon.fontStyle  = FontStyles.Bold;

            // Name
            var name = MkTxt(row, "Name", TAB_NAMES[i], DIM, 17,
                             V(0.35f, 0.18f), V(0.98f, 0.82f));
            name.alignment = TextAlignmentOptions.MidlineLeft;

            int capturedIdx = i;
            var btn = row.gameObject.AddComponent<Button>();
            btn.targetGraphic = bgImg.GetComponent<Image>();
            var cols = btn.colors;
            cols.normalColor      = Color.white;
            cols.highlightedColor = new Color(1,1,1,0.85f);
            cols.pressedColor     = new Color(0.8f,0.8f,0.8f);
            btn.colors = cols;
            btn.onClick.AddListener(() => SwitchTab((Tab)capturedIdx));
            _tabButtons[i] = btn;

            // Store name ref to tint on switch
            int fi = i;
            btn.onClick.AddListener(() => {});
        }
    }

    // ── Footer ───────────────────────────────────────────────────────────────
    void BuildFooter(RectTransform p)
    {
        var ft = MkImg(p, "Footer", HDR, V(0,0), V(1,0.09f), V(0,0), V(0,0));
        MkImg(ft,"LineT", C(0.18f,0.80f,0.58f,0.30f), V(0,1), V(1,1), V(0,-1.5f), V(0,3));

        // Resume
        var resumeRT = MkImg(ft,"BtnResume", ACCENT, V(0.02f,0.12f), V(0.26f,0.88f), V(0,0), V(0,0));
        MkImg(resumeRT,"Sh", C(1,1,1,0.12f), V(0,0.5f), V(1,1), V(0,0), V(0,0));
        var rTxt = MkTxt(resumeRT,"T","▶  Reanudar", PANEL, 23, V(0,0), V(1,1));
        rTxt.fontStyle = FontStyles.Bold; rTxt.alignment = TextAlignmentOptions.Center;
        var rBtn = resumeRT.gameObject.AddComponent<Button>(); rBtn.targetGraphic = resumeRT.GetComponent<Image>();
        rBtn.onClick.AddListener(Resume);

        // Main menu
        var menuRT = MkImg(ft,"BtnMenu", PANEL2, V(0.28f,0.12f), V(0.52f,0.88f), V(0,0), V(0,0));
        MkImg(menuRT,"Sh", C(1,1,1,0.06f), V(0,0.5f), V(1,1), V(0,0), V(0,0));
        MkImg(menuRT,"LineT", ACCENT2, V(0,1), V(1,1), V(0,-2), V(0,4));
        var mTxt = MkTxt(menuRT,"T","⌂  Menú Principal", Color.white, 21, V(0,0), V(1,1));
        mTxt.alignment = TextAlignmentOptions.Center;
        var mBtn = menuRT.gameObject.AddComponent<Button>(); mBtn.targetGraphic = menuRT.GetComponent<Image>();
        mBtn.onClick.AddListener(GoToMenu);

        // Restart
        var restartRT = MkImg(ft,"BtnRestart", PANEL2, V(0.54f,0.12f), V(0.78f,0.88f), V(0,0), V(0,0));
        MkImg(restartRT,"Sh", C(1,1,1,0.06f), V(0,0.5f), V(1,1), V(0,0), V(0,0));
        MkImg(restartRT,"LineT", CYELLOW, V(0,1), V(1,1), V(0,-2), V(0,4));
        var rstTxt = MkTxt(restartRT,"T","↺  Reiniciar", Color.white, 21, V(0,0), V(1,1));
        rstTxt.alignment = TextAlignmentOptions.Center;
        var rstBtn = restartRT.gameObject.AddComponent<Button>(); rstBtn.targetGraphic = restartRT.GetComponent<Image>();
        rstBtn.onClick.AddListener(RestartScene);

        // Quit
        var quitRT = MkImg(ft,"BtnQuit", C(0.12f,0.05f,0.08f), V(0.80f,0.12f), V(0.98f,0.88f), V(0,0), V(0,0));
        MkImg(quitRT,"Sh", C(1,1,1,0.06f), V(0,0.5f), V(1,1), V(0,0), V(0,0));
        MkImg(quitRT,"LineT", CRED, V(0,1), V(1,1), V(0,-2), V(0,4));
        var qTxt = MkTxt(quitRT,"T","✕  Salir", C(CRED.r+0.1f,CRED.g,CRED.b), 21, V(0,0), V(1,1));
        qTxt.alignment = TextAlignmentOptions.Center;
        var qBtn = quitRT.gameObject.AddComponent<Button>(); qBtn.targetGraphic = quitRT.GetComponent<Image>();
        qBtn.onClick.AddListener(QuitGame);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Tab: Audio
    // ═════════════════════════════════════════════════════════════════════════

    GameObject BuildTabAudio(RectTransform parent)
    {
        var tab = NewTabContainer(parent, "Tab_Audio");
        var R   = tab.GetComponent<RectTransform>();

        SectionTitle(R, "AJUSTES DE SONIDO", ACCENT);

        // Master volume
        _slMaster  = BuildSlider(R, "Volumen Master",  "♪", ACCENT,  V(0,0.72f), V(1,0.90f),
                                  _volMaster, v => { _volMaster = v; ApplyAudio(); SaveSettings(); });
        // Music volume
        _slMusic   = BuildSlider(R, "Música",          "♫", ACCENT2, V(0,0.52f), V(1,0.70f),
                                  _volMusic, v => { _volMusic = v; ApplyAudio(); SaveSettings(); });
        // SFX volume
        _slSfx     = BuildSlider(R, "Efectos de Sonido","◈", CYELLOW, V(0,0.32f), V(1,0.50f),
                                  _volSfx, v => { _volSfx = v; ApplyAudio(); SaveSettings(); });

        // Separator
        MkImg(R, "Sep1", C(1,1,1,0.08f), V(0,0.28f), V(1,0.28f), V(0,1), V(0,2));

        // Mute all toggle
        _tgMute = BuildToggle(R, "Silenciar todo",
                              "Desactiva todo el audio del juego.", CRED,
                              V(0,0.12f), V(0.48f,0.26f), _muted,
                              v => { _muted = v; ApplyAudio(); SaveSettings(); });

        // Audio hint
        var hint = MkTxt(R,"AudioHint",
            "Los cambios de volumen se aplican en tiempo real.",
            C(DIM.r,DIM.g,DIM.b,0.7f), 16, V(0.50f,0.12f), V(1,0.26f));
        hint.alignment = TextAlignmentOptions.MidlineLeft;

        return tab;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Tab: Pantalla
    // ═════════════════════════════════════════════════════════════════════════

    GameObject BuildTabPantalla(RectTransform parent)
    {
        var tab = NewTabContainer(parent, "Tab_Pantalla");
        var R   = tab.GetComponent<RectTransform>();

        SectionTitle(R, "AJUSTES DE PANTALLA", ACCENT2);

        // Fullscreen toggle
        _tgFullscreen = BuildToggle(R, "Pantalla Completa",
                                    "Alterna entre modo ventana y pantalla completa.",
                                    ACCENT2, V(0,0.72f), V(0.55f,0.90f), _fullscreen,
                                    v => { _fullscreen = v; ApplyScreen(); SaveSettings(); });

        // Quality dropdown
        BuildLabel(R, "Calidad Gráfica", "⊞", ACCENT2, V(0,0.52f), V(0.48f,0.70f));
        _ddQuality = BuildDropdown(R, V(0.50f,0.52f), V(1,0.70f),
                                   new[]{"Baja","Media","Alta","Ultra"},
                                   _quality,
                                   v => { _quality = v; ApplyScreen(); SaveSettings(); });

        // Brightness slider
        _slBrightness = BuildSlider(R, "Brillo", "☀", CYELLOW, V(0,0.32f), V(1,0.50f),
                                    _brightness, v => { _brightness = v; ApplyScreen(); SaveSettings(); });

        MkImg(R, "Sep2", C(1,1,1,0.08f), V(0,0.28f), V(1,0.28f), V(0,1), V(0,2));

        // Show FPS toggle
        _tgShowFps = BuildToggle(R, "Mostrar FPS",
                                 "Muestra el contador de fotogramas por segundo.",
                                 CYELLOW, V(0,0.12f), V(0.48f,0.26f), _showFps,
                                 v => { _showFps = v; _fpsGO?.SetActive(v); SaveSettings(); });

        // Resolution info
        var resInfo = MkTxt(R,"ResInfo",
            $"Resolución actual: {Screen.width}×{Screen.height}",
            C(DIM.r,DIM.g,DIM.b,0.7f), 16, V(0.50f,0.12f), V(1,0.26f));
        resInfo.alignment = TextAlignmentOptions.MidlineLeft;

        return tab;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Tab: Accesibilidad
    // ═════════════════════════════════════════════════════════════════════════

    GameObject BuildTabAccesibilidad(RectTransform parent)
    {
        var tab = NewTabContainer(parent, "Tab_Acc");
        var R   = tab.GetComponent<RectTransform>();

        SectionTitle(R, "ACCESIBILIDAD", C(0.72f,0.38f,0.96f));

        // Text size slider
        _slTextSize = BuildSlider(R, "Tamaño de Texto", "A", C(0.72f,0.38f,0.96f), V(0,0.72f), V(1,0.90f),
                                  _textSize / 2f, v => { _textSize = Mathf.RoundToInt(v*2); SaveSettings(); });
        // Min/max labels
        var szHint = MkTxt(R,"SzHint","Pequeño ← → Grande", C(DIM.r,DIM.g,DIM.b,0.6f), 14,
                           V(0.02f,0.69f), V(0.98f,0.73f));
        szHint.alignment = TextAlignmentOptions.Center;

        // High contrast toggle
        _tgHighContrast = BuildToggle(R, "Alto Contraste",
                                      "Aumenta el contraste de colores para mejor legibilidad.",
                                      C(0.72f,0.38f,0.96f), V(0,0.52f), V(0.55f,0.68f), _highContrast,
                                      v => { _highContrast = v; SaveSettings(); });

        // Reduce animations toggle
        _tgReduceAnim = BuildToggle(R, "Reducir Animaciones",
                                    "Desactiva efectos visuales y animaciones complejas.",
                                    C(0.72f,0.38f,0.96f), V(0.60f,0.52f), V(1,0.68f), _reduceAnim,
                                    v => { _reduceAnim = v; SaveSettings(); });

        MkImg(R, "Sep3", C(1,1,1,0.08f), V(0,0.48f), V(1,0.48f), V(0,1), V(0,2));

        // Colorblind dropdown
        BuildLabel(R, "Modo Daltónico", "◑", C(0.72f,0.38f,0.96f), V(0,0.30f), V(0.48f,0.46f));
        _ddColorblind = BuildDropdown(R, V(0.50f,0.30f), V(1,0.46f),
                                      new[]{"Ninguno","Deuteranopia","Protanopia","Tritanopia"},
                                      _colorblind,
                                      v => { _colorblind = v; SaveSettings(); });

        // Info note
        var note = MkTxt(R,"AccNote",
            "Nota: Algunos ajustes de accesibilidad pueden\nrequirir reiniciar la escena para aplicarse.",
            C(DIM.r,DIM.g,DIM.b,0.65f), 15, V(0,0.10f), V(1,0.28f));
        note.alignment = TextAlignmentOptions.Center;

        return tab;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Tab: Control
    // ═════════════════════════════════════════════════════════════════════════

    GameObject BuildTabControl(RectTransform parent)
    {
        var tab = NewTabContainer(parent, "Tab_Control");
        var R   = tab.GetComponent<RectTransform>();

        SectionTitle(R, "CONTROLES", CYELLOW);

        // Key bindings (read-only display)
        string[,] binds = {
            { "ESC",         "Abrir / Cerrar Pausa"     },
            { "Clic / Espacio", "Acción Principal"       },
            { "Flechas / WASD", "Direcciones (cuando aplica)" },
            { "R",           "Reiniciar partida actual"  },
            { "M",           "Volver al menú principal"  },
        };

        for (int i = 0; i < binds.GetLength(0); i++)
        {
            float yMax = 0.89f - i * 0.16f;
            float yMin = yMax  - 0.14f;
            BuildKeyBind(R, binds[i,0], binds[i,1], V(0,yMin), V(1,yMax));
        }

        // Reset settings button
        var rstRT = MkImg(R,"BtnReset", C(0.10f,0.08f,0.05f), V(0,0.04f), V(0.46f,0.16f), V(0,0), V(0,0));
        MkImg(rstRT,"LineT", CYELLOW, V(0,1), V(1,1), V(0,-2), V(0,4));
        MkImg(rstRT,"Sh", C(1,1,1,0.05f), V(0,0.5f), V(1,1), V(0,0), V(0,0));
        var rTxt = MkTxt(rstRT,"T","↺  Resetear Ajustes", CYELLOW, 19, V(0,0), V(1,1));
        rTxt.alignment = TextAlignmentOptions.Center;
        var rBtn = rstRT.gameObject.AddComponent<Button>(); rBtn.targetGraphic = rstRT.GetComponent<Image>();
        rBtn.onClick.AddListener(ResetAllSettings);

        return tab;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Tab: Partida
    // ═════════════════════════════════════════════════════════════════════════

    GameObject BuildTabPartida(RectTransform parent)
    {
        var tab = NewTabContainer(parent, "Tab_Partida");
        var R   = tab.GetComponent<RectTransform>();

        SectionTitle(R, "INFORMACIÓN DE PARTIDA", CGREEN);

        // Session info card
        var card = MkImg(R,"InfoCard", PANEL2, V(0,0.38f), V(1,0.90f), V(0,0), V(0,0));
        MkImg(card,"LineL", CGREEN, V(0,0.05f), V(0,0.95f), V(3,0), V(6,0));
        MkImg(card,"LineT", C(CGREEN.r,CGREEN.g,CGREEN.b,0.3f), V(0,1), V(1,1), V(0,-2), V(0,4));
        MkImg(card,"Sh", C(1,1,1,0.03f), V(0,0.5f), V(1,1), V(0,0), V(0,0));

        _sceneLbl = MkTxt(card,"SceneVal","—", Color.white, 24, V(0.04f,0.60f), V(0.98f,0.90f));
        _sceneLbl.alignment = TextAlignmentOptions.MidlineLeft; _sceneLbl.fontStyle = FontStyles.Bold;

        _diffLbl = MkTxt(card,"DiffVal","—", DIM, 19, V(0.04f,0.32f), V(0.60f,0.60f));
        _diffLbl.alignment = TextAlignmentOptions.MidlineLeft;

        _scoreLbl = MkTxt(card,"ScoreVal","—", CYELLOW, 22, V(0.60f,0.32f), V(0.98f,0.60f));
        _scoreLbl.alignment = TextAlignmentOptions.MidlineRight; _scoreLbl.fontStyle = FontStyles.Bold;

        MkTxt(card,"DiffLbl","Dificultad", DIM, 14, V(0.04f,0.08f), V(0.30f,0.32f)).alignment = TextAlignmentOptions.MidlineLeft;
        MkTxt(card,"ScoreLbl","Puntuación Total", DIM, 14, V(0.60f,0.08f), V(0.98f,0.32f)).alignment = TextAlignmentOptions.MidlineRight;

        // Tips area
        var tipsCard = MkImg(R,"TipsCard", PANEL2, V(0,0.04f), V(1,0.36f), V(0,0), V(0,0));
        MkImg(tipsCard,"LineL", CYELLOW, V(0,0.05f), V(0,0.95f), V(3,0), V(6,0));
        MkImg(tipsCard,"Sh", C(1,1,1,0.03f), V(0,0.5f), V(1,1), V(0,0), V(0,0));
        var tipTitle = MkTxt(tipsCard,"TipTitle","💡  CONSEJO", CYELLOW, 17, V(0.03f,0.62f), V(0.98f,0.95f));
        tipTitle.fontStyle = FontStyles.Bold; tipTitle.alignment = TextAlignmentOptions.MidlineLeft;
        var tipBody = MkTxt(tipsCard,"TipBody", GetRandomTip(), C(DIM.r+0.1f,DIM.g+0.1f,DIM.b+0.1f),
                            16, V(0.03f,0.05f), V(0.98f,0.62f));
        tipBody.alignment = TextAlignmentOptions.MidlineLeft;

        return tab;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Helpers — UI primitives
    // ═════════════════════════════════════════════════════════════════════════

    GameObject NewTabContainer(RectTransform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero; rt.anchoredPosition = Vector2.zero;
        go.AddComponent<Image>().color = Color.clear;
        return go;
    }

    void SectionTitle(RectTransform p, string text, Color accent)
    {
        var ttl = MkTxt(p,"SecTitle", text, accent, 24, V(0,0.92f), V(1,1));
        ttl.fontStyle = FontStyles.Bold;
        ttl.alignment = TextAlignmentOptions.MidlineLeft;
        ttl.characterSpacing = 3f;
        MkImg(p,"SecLine", C(accent.r,accent.g,accent.b,0.35f), V(0,0.91f), V(1,0.91f), V(0,1), V(0,2));
    }

    Slider BuildSlider(RectTransform p, string label, string icon, Color accent,
                       Vector2 ancMin, Vector2 ancMax, float initVal, Action<float> onChange)
    {
        var row = MkImg(p, "SL_"+label, Color.clear, ancMin, ancMax, V(0,0), V(0,0));

        // Icon
        var icTxt = MkTxt(row,"Icon", icon, accent, 26, V(0,0.1f), V(0.06f,0.9f));
        icTxt.alignment = TextAlignmentOptions.Center; icTxt.fontStyle = FontStyles.Bold;

        // Label
        var lbl = MkTxt(row,"Lbl", label, Color.white, 19, V(0.07f,0.52f), V(0.55f,0.95f));
        lbl.alignment = TextAlignmentOptions.MidlineLeft;

        // Value %
        var valTxt = MkTxt(row,"Val", Mathf.RoundToInt(initVal*100)+" %", accent, 18,
                           V(0.82f,0.52f), V(1,0.95f));
        valTxt.alignment = TextAlignmentOptions.MidlineRight;
        valTxt.fontStyle  = FontStyles.Bold;

        // Slider track background
        var trackBG = MkImg(row,"Track", C(0.06f,0.10f,0.20f), V(0.07f,0.08f), V(1,0.50f), V(0,0), V(0,0));

        // Fill bar (custom)
        var fillImg = MkImg(trackBG,"Fill", accent, V(0,0), V(initVal,1), V(0,0), V(0,0));

        // Build Unity Slider component
        var slGO = new GameObject("SliderCtrl");
        slGO.transform.SetParent(row, false);
        var slRT = slGO.AddComponent<RectTransform>();
        slRT.anchorMin = new Vector2(0.07f, 0f); slRT.anchorMax = new Vector2(1f, 1f);
        slRT.sizeDelta = Vector2.zero; slRT.anchoredPosition = Vector2.zero;
        slGO.AddComponent<Image>().color = Color.clear;

        var sl = slGO.AddComponent<Slider>();
        sl.minValue  = 0f; sl.maxValue = 1f;
        sl.value     = initVal;
        sl.direction = Slider.Direction.LeftToRight;

        // Handle
        var hGO = new GameObject("Handle"); hGO.transform.SetParent(slGO.transform, false);
        var hRT = hGO.AddComponent<RectTransform>();
        hRT.anchorMin = hRT.anchorMax = new Vector2(initVal, 0.5f);
        hRT.pivot = new Vector2(0.5f, 0.5f); hRT.sizeDelta = new Vector2(24, 24);
        var hImg = hGO.AddComponent<Image>(); hImg.color = Color.white;
        sl.handleRect = hRT;
        sl.targetGraphic = hImg;

        sl.onValueChanged.AddListener(v =>
        {
            fillImg.GetComponent<RectTransform>().anchorMax = new Vector2(v,1);
            valTxt.text = Mathf.RoundToInt(v*100)+" %";
            onChange?.Invoke(v);
        });

        return sl;
    }

    Toggle BuildToggle(RectTransform p, string label, string desc, Color accent,
                       Vector2 ancMin, Vector2 ancMax, bool initVal, Action<bool> onChange)
    {
        var row = MkImg(p,"TG_"+label, Color.clear, ancMin, ancMax, V(0,0), V(0,0));

        var lbl = MkTxt(row,"Lbl", label, Color.white, 19, V(0.12f,0.52f), V(0.85f,0.95f));
        lbl.alignment = TextAlignmentOptions.MidlineLeft;
        var dsc = MkTxt(row,"Dsc", desc, C(DIM.r,DIM.g,DIM.b,0.75f), 13, V(0.12f,0.05f), V(0.85f,0.52f));
        dsc.alignment = TextAlignmentOptions.MidlineLeft;

        // Toggle pill background
        var pillBG = MkImg(row,"PillBG", initVal ? accent : C(0.12f,0.16f,0.28f),
                           V(0.85f,0.22f), V(0.99f,0.78f), V(0,0), V(0,0));

        // Thumb
        var thumbGO = new GameObject("Thumb"); thumbGO.transform.SetParent(pillBG, false);
        var tRT = thumbGO.AddComponent<RectTransform>();
        tRT.anchorMin = tRT.anchorMax = initVal ? new Vector2(0.68f,0.5f) : new Vector2(0.32f,0.5f);
        tRT.pivot = new Vector2(0.5f,0.5f); tRT.sizeDelta = new Vector2(20,20);
        var tImg = thumbGO.AddComponent<Image>(); tImg.color = Color.white;

        // Hidden Unity Toggle
        var tgGO = new GameObject("ToggleCtrl"); tgGO.transform.SetParent(row, false);
        var tgRT = tgGO.AddComponent<RectTransform>();
        tgRT.anchorMin = Vector2.zero; tgRT.anchorMax = Vector2.one;
        tgRT.sizeDelta = Vector2.zero;
        tgGO.AddComponent<Image>().color = Color.clear;
        var tg = tgGO.AddComponent<Toggle>();
        tg.isOn         = initVal;
        tg.targetGraphic = tgGO.GetComponent<Image>();
        tg.graphic       = tImg;

        tg.onValueChanged.AddListener(v =>
        {
            pillBG.GetComponent<Image>().color = v ? accent : C(0.12f,0.16f,0.28f);
            tRT.anchorMin = tRT.anchorMax = v ? new Vector2(0.68f,0.5f) : new Vector2(0.32f,0.5f);
            onChange?.Invoke(v);
        });

        return tg;
    }

    TMP_Dropdown BuildDropdown(RectTransform p, Vector2 ancMin, Vector2 ancMax,
                               string[] options, int initIdx, Action<int> onChange)
    {
        var go = new GameObject("Dropdown");
        go.transform.SetParent(p, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = ancMin; rt.anchorMax = ancMax;
        rt.sizeDelta = Vector2.zero; rt.anchoredPosition = Vector2.zero;
        var img = go.AddComponent<Image>(); img.color = C(0.06f,0.10f,0.20f);

        var dd = go.AddComponent<TMP_Dropdown>();
        dd.captionText = MkTxt(rt,"Caption","", Color.white, 18, V(0.04f,0.1f), V(0.85f,0.9f));
        dd.captionText.alignment = TextAlignmentOptions.MidlineLeft;

        // Arrow symbol
        var arrTxt = MkTxt(rt,"Arrow","▾", DIM, 20, V(0.86f,0f), V(1f,1f));
        arrTxt.alignment = TextAlignmentOptions.Center;

        foreach (var opt in options)
            dd.options.Add(new TMP_Dropdown.OptionData(opt));

        dd.value = initIdx;
        dd.RefreshShownValue();

        dd.onValueChanged.AddListener(v => { onChange?.Invoke(v); SaveSettings(); });

        return dd;
    }

    void BuildLabel(RectTransform p, string label, string icon, Color accent,
                    Vector2 ancMin, Vector2 ancMax)
    {
        var row = MkImg(p,"Lbl_"+label, Color.clear, ancMin, ancMax, V(0,0), V(0,0));
        var ic = MkTxt(row,"Icon", icon, accent, 22, V(0,0.1f), V(0.08f,0.9f));
        ic.alignment = TextAlignmentOptions.Center; ic.fontStyle = FontStyles.Bold;
        var txt = MkTxt(row,"T", label, Color.white, 19, V(0.09f,0.1f), V(1,0.9f));
        txt.alignment = TextAlignmentOptions.MidlineLeft;
    }

    void BuildKeyBind(RectTransform p, string key, string action, Vector2 ancMin, Vector2 ancMax)
    {
        var row = MkImg(p,"KB_"+key, C(0.07f,0.10f,0.20f,0.5f), ancMin, ancMax, V(0,0), V(0,0));
        MkImg(row,"LineL", CYELLOW, V(0,0.1f), V(0,0.9f), V(3,0), V(6,0));

        // Key badge
        var badge = MkImg(row,"Badge", C(0.14f,0.18f,0.32f), V(0.01f,0.12f), V(0.26f,0.88f), V(0,0), V(0,0));
        MkImg(badge,"LineT", CYELLOW, V(0,1), V(1,1), V(0,-2), V(0,3));
        var kTxt = MkTxt(badge,"K", key, CYELLOW, 16, V(0,0), V(1,1));
        kTxt.alignment = TextAlignmentOptions.Center; kTxt.fontStyle = FontStyles.Bold;

        var aTxt = MkTxt(row,"A", action, C(DIM.r+0.1f,DIM.g+0.1f,DIM.b+0.1f), 18,
                         V(0.28f,0.1f), V(1,0.9f));
        aTxt.alignment = TextAlignmentOptions.MidlineLeft;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Tab switching
    // ═════════════════════════════════════════════════════════════════════════

    void SwitchTab(Tab tab, bool instant = false)
    {
        _activeTab = tab;
        for (int i = 0; i < _tabContents.Length; i++)
        {
            bool active = i == (int)tab;
            _tabContents[i].SetActive(active);

            if (_tabHighlights != null && i < _tabHighlights.Length)
                _tabHighlights[i].color = active ? ACCENT : Color.clear;
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Animations
    // ═════════════════════════════════════════════════════════════════════════

    void SetVisible(bool v, bool instant = false)
    {
        _canvas.gameObject.SetActive(v);
        if (instant)
        {
            _canvasGroup.alpha = v ? 1f : 0f;
            if (_mainPanel) _mainPanel.localScale = Vector3.one;
        }
    }

    IEnumerator AnimateIn()
    {
        _animating = true;
        float t = 0f; float dur = 0.22f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.SmoothStep(0f, 1f, t / dur);
            _canvasGroup.alpha      = p;
            _mainPanel.localScale   = Vector3.Lerp(new Vector3(0.92f,0.92f,1f), Vector3.one, p);
            yield return null;
        }
        _canvasGroup.alpha    = 1f;
        _mainPanel.localScale = Vector3.one;
        _animating = false;
    }

    IEnumerator AnimateOut(Action onDone)
    {
        _animating = true;
        float t = 0f; float dur = 0.16f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.SmoothStep(0f, 1f, t / dur);
            _canvasGroup.alpha    = 1f - p;
            _mainPanel.localScale = Vector3.Lerp(Vector3.one, new Vector3(0.94f,0.94f,1f), p);
            yield return null;
        }
        _animating = false;
        onDone?.Invoke();
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Settings — Load / Save / Apply / Reset
    // ═════════════════════════════════════════════════════════════════════════

    void LoadSettings()
    {
        _volMaster   = PlayerPrefs.GetFloat(K_VOL_MASTER,  0.80f);
        _volMusic    = PlayerPrefs.GetFloat(K_VOL_MUSIC,   0.70f);
        _volSfx      = PlayerPrefs.GetFloat(K_VOL_SFX,     0.85f);
        _muted       = PlayerPrefs.GetInt(K_MUTE,          0) == 1;
        _fullscreen  = PlayerPrefs.GetInt(K_FULLSCREEN,    1) == 1;
        _quality     = PlayerPrefs.GetInt(K_QUALITY,        2);
        _brightness  = PlayerPrefs.GetFloat(K_BRIGHTNESS,  0.50f);
        _textSize    = PlayerPrefs.GetInt(K_TEXT_SIZE,      1);
        _highContrast= PlayerPrefs.GetInt(K_HIGH_CONT,      0) == 1;
        _reduceAnim  = PlayerPrefs.GetInt(K_REDUCE_ANIM,    0) == 1;
        _colorblind  = PlayerPrefs.GetInt(K_COLORBLIND,     0);
        _showFps     = PlayerPrefs.GetInt(K_SHOW_FPS,       0) == 1;
    }

    void SaveSettings()
    {
        PlayerPrefs.SetFloat(K_VOL_MASTER,  _volMaster);
        PlayerPrefs.SetFloat(K_VOL_MUSIC,   _volMusic);
        PlayerPrefs.SetFloat(K_VOL_SFX,     _volSfx);
        PlayerPrefs.SetInt(K_MUTE,          _muted       ? 1 : 0);
        PlayerPrefs.SetInt(K_FULLSCREEN,    _fullscreen  ? 1 : 0);
        PlayerPrefs.SetInt(K_QUALITY,        _quality);
        PlayerPrefs.SetFloat(K_BRIGHTNESS,  _brightness);
        PlayerPrefs.SetInt(K_TEXT_SIZE,      _textSize);
        PlayerPrefs.SetInt(K_HIGH_CONT,      _highContrast ? 1 : 0);
        PlayerPrefs.SetInt(K_REDUCE_ANIM,    _reduceAnim   ? 1 : 0);
        PlayerPrefs.SetInt(K_COLORBLIND,     _colorblind);
        PlayerPrefs.SetInt(K_SHOW_FPS,       _showFps      ? 1 : 0);
        PlayerPrefs.Save();
    }

    void ApplyAllSettings()
    {
        ApplyAudio();
        ApplyScreen();
        if (_fpsGO != null) _fpsGO.SetActive(_showFps);
    }

    void ApplyAudio()
    {
        AudioListener.volume = _muted ? 0f : _volMaster;
    }

    void ApplyScreen()
    {
        Screen.fullScreen = _fullscreen;
        QualitySettings.SetQualityLevel(_quality, true);
        // Brightness via camera (simple approach: no post-processing)
        float b = Mathf.Lerp(0.3f, 2.0f, _brightness);
        Camera cam = Camera.main;
        if (cam != null)
        {
            // If using URP/HDRP a different method would be needed
        }
    }

    void ResetAllSettings()
    {
        _volMaster = 0.80f; _volMusic = 0.70f; _volSfx = 0.85f;
        _muted = false; _fullscreen = true; _quality = 2;
        _brightness = 0.50f; _textSize = 1; _highContrast = false;
        _reduceAnim = false; _colorblind = 0; _showFps = false;
        SaveSettings();
        ApplyAllSettings();

        // Refresh slider/toggle UI values
        if (_slMaster   != null) _slMaster.value   = _volMaster;
        if (_slMusic    != null) _slMusic.value    = _volMusic;
        if (_slSfx      != null) _slSfx.value      = _volSfx;
        if (_slBrightness!=null) _slBrightness.value = _brightness;
        if (_slTextSize != null) _slTextSize.value  = _textSize / 2f;
        if (_tgMute     != null) _tgMute.isOn       = _muted;
        if (_tgFullscreen!=null) _tgFullscreen.isOn  = _fullscreen;
        if (_tgHighContrast!=null) _tgHighContrast.isOn = _highContrast;
        if (_tgReduceAnim != null) _tgReduceAnim.isOn   = _reduceAnim;
        if (_tgShowFps    != null) _tgShowFps.isOn      = _showFps;
        if (_ddQuality    != null) _ddQuality.value      = _quality;
        if (_ddColorblind != null) _ddColorblind.value   = _colorblind;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Actions
    // ═════════════════════════════════════════════════════════════════════════

    void GoToMenu()
    {
        Time.timeScale = 1f;
        _isOpen = false;
        SetVisible(false, instant: true);
        SceneLoader.GoToMainMenu();
    }

    void RestartScene()
    {
        Time.timeScale = 1f;
        _isOpen = false;
        SetVisible(false, instant: true);
        SceneLoader.ReloadCurrentScene();
    }

    void QuitGame()
    {
        Time.timeScale = 1f;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void RefreshSessionInfo()
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (_sceneLbl != null) _sceneLbl.text = "Escena:  " + sceneName;

        string diff = "—";
        if (GameManager.Instance != null)
            diff = GameManager.Instance.CurrentDifficulty switch
            {
                DifficultyLevel.Easy   => "Fácil (3-5 años)",
                DifficultyLevel.Medium => "Media (5-7 años)",
                DifficultyLevel.Hard   => "Difícil (7-10 años)",
                _ => "—"
            };
        if (_diffLbl  != null) _diffLbl.text  = "Dificultad:  " + diff;

        int score = GameManager.Instance != null ? GameManager.Instance.TotalScore : 0;
        if (_scoreLbl != null) _scoreLbl.text = score + " pts";
    }

    static string GetRandomTip()
    {
        string[] tips = {
            "Respira hondo antes de cada ronda. La calma mejora tu concentración.",
            "Si te equivocas, no pasa nada. Cada error es una oportunidad de aprender.",
            "Las pausas activas ayudan al cerebro a consolidar lo que ha aprendido.",
            "Intenta jugar en un lugar sin distracciones para obtener mejores resultados.",
            "El sueño es fundamental para la memoria. ¡Descansa bien antes de jugar!",
            "Practicar a diario, aunque sean 5 minutos, tiene más efecto que sesiones largas.",
        };
        return tips[UnityEngine.Random.Range(0, tips.Length)];
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Generic UI helpers (same style as project)
    // ═════════════════════════════════════════════════════════════════════════

    RectTransform MkImg(RectTransform p, string n, Color col, Vector2 am, Vector2 aM, Vector2 pos, Vector2 sd)
    {
        var go = new GameObject(n); go.transform.SetParent(p, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = am; rt.anchorMax = aM; rt.pivot = new Vector2(.5f,.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = sd;
        go.AddComponent<Image>().color = col;
        return rt;
    }

    TextMeshProUGUI MkTxt(RectTransform p, string n, string txt, Color col, float sz, Vector2 am, Vector2 aM)
    {
        var go = new GameObject(n); go.transform.SetParent(p, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = am; rt.anchorMax = aM; rt.pivot = new Vector2(.5f,.5f);
        rt.anchoredPosition = Vector2.zero; rt.sizeDelta = Vector2.zero;
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = txt; t.color = col; t.fontSize = sz;
        t.alignment = TextAlignmentOptions.Center;
        t.overflowMode = TextOverflowModes.Overflow;
        return t;
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Helper: pulsating red dot in header (indicates game is paused)
// ─────────────────────────────────────────────────────────────────────────────
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
