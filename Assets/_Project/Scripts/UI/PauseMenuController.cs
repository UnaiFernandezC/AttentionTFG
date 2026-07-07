// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
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

    // Auto-arranque: el menú de pausa existe en TODAS las escenas (así ESC funciona
    // también en el hub del Planeta Attentia y demás pantallas), sin colocarlo a mano.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        if (Instance != null) return;
        var go = new GameObject("PauseMenuController");
        go.AddComponent<PauseMenuController>();
    }

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
        // No pausar con ningún overlay del sistema abierto (PIN, tutorial, perfiles,
        // privacidad, consentimiento, hub, área del tutor o pantalla de error).
        if (PinPrompt.IsOpen || TutorialScreen.IsOpen || ProfileScreenController.IsOpen ||
            PolicyViewer.IsOpen || ConsentScreen.IsOpen || ProgressMapScreen.IsOpen ||
            TutorPanel.IsOpen || NavErrorScreen.IsOpen) return;

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
        _canvas.sortingOrder = 870;   // por encima del hub (820) y otras pantallas de menú
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
        _mainPanel.sizeDelta = new Vector2(680f, 600f);
        _mainPanel.anchoredPosition = Vector2.zero;
        var panImg = panGO.AddComponent<Image>();
        panImg.color = BG;
        panImg.sprite = KidUI.RoundedSprite;
        panImg.type = Image.Type.Sliced;
        panImg.pixelsPerUnitMultiplier = 0.9f;

        // Acento superior redondeado (sustituye a los bordes/notches rectos)
        var accTop = MkImg(_mainPanel, "AccTop", ACCENT, V(0.36f, 0.985f), V(0.64f, 0.994f), V(0,0), V(0,0));
        MakeRounded(accTop, 4f);

        var hdr = MkImg(_mainPanel, "Header", HDR, V(0.025f, 0.80f), V(0.975f, 0.985f), V(0,0), V(0,0));
        MakeRounded(hdr, 1.4f);
        MkImg(hdr, "LineB", ACCENT, V(0.03f,0), V(0.97f,0), V(0,1.5f), V(0,3));

        var ttl = MkTxt(hdr, "Title", "MENU DE PAUSA", Color.white, 32,
                        V(0.04f, 0.1f), V(0.70f, 0.9f));
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

        // Botón de ayuda "?" → reabre el tutorial cuando se quiera.
        var helpRT = MkImg(hdr, "HelpBtn", C(0.10f, 0.16f, 0.30f, 0.90f),
                           V(0.72f, 0.10f), V(0.815f, 0.90f), V(0,0), V(0,0));
        MakeRounded(helpRT, 2f);
        var helpT = MkTxt(helpRT, "QT", "?", Color.white, 26, V(0,0), V(1,1));
        helpT.fontStyle = FontStyles.Bold;
        helpT.alignment = TextAlignmentOptions.Center;
        var helpBtn = helpRT.gameObject.AddComponent<Button>();
        helpBtn.targetGraphic = helpRT.GetComponent<Image>();
        SetBtnColors(helpBtn);
        helpBtn.onClick.AddListener(() => TutorialScreen.Show());

        var slRow = MkImg(_mainPanel, "SlRow", Color.clear, V(0.05f, 0.635f), V(0.95f, 0.79f), V(0,0), V(0,0));

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

        MkImg(_mainPanel, "Sep", C(1,1,1,0.08f), V(0.05f, 0.60f), V(0.95f, 0.60f), V(0,1), V(0,2));

        BuildBtn(_mainPanel, "Cerrar menu", ACCENT,
                 V(0.05f, 0.455f), V(0.47f, 0.575f),
                 Resume, PANEL);

        BuildBtn(_mainPanel, "Elegir dificultad", CDIFF,
                 V(0.53f, 0.455f), V(0.95f, 0.575f),
                 GoToDifficulty, PANEL);

        // --- Fila del sistema de perfiles/informes ---
        BuildBtn(_mainPanel, "Descargar informe", C(0.95f, 0.55f, 0.12f),
                 V(0.05f, 0.305f), V(0.47f, 0.425f),
                 OnDownloadReport, PANEL);

        BuildBtn(_mainPanel, "Cambiar jugador", C(0.58f, 0.28f, 0.92f),
                 V(0.53f, 0.305f), V(0.95f, 0.425f),
                 OnSwitchProfile, PANEL);

        // --- Volver al menú principal (hub Planeta Attentia: mapa, misiones, logros) ---
        BuildBtn(_mainPanel, "Menu principal", C(0.20f, 0.78f, 0.95f),
                 V(0.05f, 0.09f), V(0.66f, 0.25f),
                 GoToMissions, PANEL);

        // --- Releer la política de privacidad (se aceptó en el primer arranque) ---
        BuildBtn(_mainPanel, "Privacidad", C(0.45f, 0.52f, 0.68f),
                 V(0.70f, 0.09f), V(0.95f, 0.25f),
                 PolicyViewer.Show, PANEL);
    }

    /// <summary>
    /// Cierra la pausa y abre el hub "Planeta Attentia" (misión de hoy, progreso,
    /// logros). Solo si hay un perfil activo; en modo invitado no hay misiones.
    /// </summary>
    void GoToMissions()
    {
        var pm = ProfileManager.Instance;
        if (pm == null || !pm.HasActiveProfile)
        {
            GameFeel.FloatingText("Necesitas un perfil para ver tu planeta",
                                  new Color(0.95f, 0.55f, 0.12f), null, 34f);
            return;
        }
        Time.timeScale = 1f;
        _isOpen = false;
        SetVisible(false, instant: true);
        // Navega a la pantalla principal: el hub se abre solo al cargarla
        // (si mostrásemos el mapa aquí, el minijuego seguiría corriendo debajo).
        SceneLoader.GoToMainMenu();
    }

    /// <summary>
    /// Genera el informe del perfil activo. Protegido por el PIN del tutor
    /// (si aún no existe PIN, guía su creación). Requiere un perfil activo.
    /// </summary>
    void OnDownloadReport()
    {
        var pm = ProfileManager.Instance;
        if (pm == null || !pm.HasActiveProfile)
        {
            // Feedback visible (antes solo salía en la consola)
            GameFeel.FloatingText("Necesitas un perfil para el informe",
                                  new Color(0.95f, 0.55f, 0.12f), null, 34f);
            return;
        }
        var profile = pm.ActiveProfile;
        PinPrompt.Show(onSuccess: () =>
        {
            string folder;
            bool ok = ReportGenerator.GenerateAndOpen(profile, out folder);
            Debug.Log(ok ? $"[PauseMenu] Informe generado en {folder}"
                         : "[PauseMenu] Error al generar el informe.");
        });
    }

    /// <summary>Cierra la sesión actual y vuelve a la pantalla de selección de perfil.</summary>
    void OnSwitchProfile()
    {
        _isOpen = false;
        Time.timeScale = 1f;
        SetVisible(false, instant: true);
        if (ProfileManager.Instance != null)
            ProfileManager.Instance.SwitchProfile();
        else
            SceneTransition.LoadScene(SceneLoader.MAIN_MENU);
    }

    void GoToDifficulty()
    {
        Time.timeScale = 1f;
        _isOpen = false;
        SetVisible(false, instant: true);
        SceneTransition.LoadScene("DifficultySelector");
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

    /// <summary>Aplica esquinas redondeadas a una imagen ya creada.</summary>
    static void MakeRounded(RectTransform rt, float cornerScale = 1.2f)
    {
        var img = rt.GetComponent<Image>();
        if (img == null) return;
        img.sprite = KidUI.RoundedSprite;
        img.type = Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = cornerScale;
    }

    void BuildBtn(RectTransform p, string label, Color accentLine,
                  Vector2 am, Vector2 aM, Action onClick, Color bg)
    {
        var rt = MkImg(p, "Btn_" + label, bg, am, aM, V(0,0), V(0,0));
        MakeRounded(rt, 1.3f);

        var lineT = MkImg(rt, "LineT", accentLine, V(0.08f,1), V(0.92f,1), V(0,-3), V(0,4));
        MakeRounded(lineT, 4f);
        ButtonJuice.Attach(rt.gameObject);

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
