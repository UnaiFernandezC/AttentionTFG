using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Minijuego "Gestión de recursos" (categoría: Planificación).
///
/// Pensado para niños. El jugador tiene "estrellas" (energía) y debe
/// llenar una barra de progreso al 100% eligiendo acciones.
/// Cada acción cuesta estrellas y da progreso.
///
/// Valores por defecto MUY fáciles. Desde el Inspector se puede:
///   - Reducir estrellas para mayor dificultad
///   - Cambiar coste/ganancia de cada acción
///   - Añadir o quitar acciones
/// </summary>
public class ResourceGameController : MinigameBase
{
    // ─── Datos de acción (configurable en Inspector) ──────────────────────

    [Serializable]
    public class ActionData
    {
        [Tooltip("Icono de texto que se muestra en el boton")]
        public string icon = "★";

        [Tooltip("Nombre de la accion")]
        public string actionName = "Accion";

        [Tooltip("Estrellas que cuesta")]
        public int cost = 1;

        [Tooltip("Progreso que da (sobre 100)")]
        public int progress = 10;

        [Tooltip("Color del boton (si es negro se usa el color automatico)")]
        public Color buttonColor = Color.black;
    }

    // ─── Inspector ────────────────────────────────────────────────────────

    [Header("=== ESTRELLAS (energia) ===")]
    [Tooltip("Estrellas en dificultad facil (para ninos: generoso)")]
    public int starsEasy = 20;

    [Tooltip("Estrellas en dificultad media")]
    public int starsMedium = 14;

    [Tooltip("Estrellas en dificultad dificil")]
    public int starsHard = 9;

    [Header("=== OBJETIVO ===")]
    [Tooltip("Progreso necesario para ganar")]
    public int goal = 100;

    [Header("=== ACCIONES ===")]
    [Tooltip("Lista de acciones. Por defecto 3 acciones faciles de entender.")]
    public List<ActionData> actions = new List<ActionData>();

    // ─── Estado ───────────────────────────────────────────────────────────

    private int   _maxStars;
    private int   _stars;
    private float _progress;
    private float _displayProg;
    private bool  _ended;

    // ─── UI refs ──────────────────────────────────────────────────────────

    private RectTransform   _starsFill;
    private Image           _starsFillImg;
    private TextMeshProUGUI _starsLbl;
    private TextMeshProUGUI _starsIcon;
    private RectTransform   _progFill;
    private TextMeshProUGUI _progLbl;
    private TextMeshProUGUI _progPct;
    private List<Button>         _btns     = new List<Button>();
    private List<Image>          _btnBgs   = new List<Image>();
    private List<CanvasGroup>    _btnGroups = new List<CanvasGroup>();
    private GameObject           _endPanel;
    private TextMeshProUGUI      _endTitle;
    private TextMeshProUGUI      _endSub;
    private Image                _endBar;

    // ─── Colores ──────────────────────────────────────────────────────────

    static readonly Color BG       = new Color(0.14f, 0.16f, 0.28f);
    static readonly Color PANEL    = new Color(0.18f, 0.20f, 0.35f);
    static readonly Color HEADER   = new Color(0.12f, 0.13f, 0.24f);
    static readonly Color ACCENT   = new Color(0.30f, 0.58f, 1.00f);
    static readonly Color GREEN    = new Color(0.20f, 0.80f, 0.48f);
    static readonly Color RED      = new Color(0.90f, 0.28f, 0.32f);
    static readonly Color YELLOW   = new Color(1.00f, 0.85f, 0.22f);
    static readonly Color ORANGE   = new Color(1.00f, 0.62f, 0.20f);
    static readonly Color DIM      = new Color(0.55f, 0.58f, 0.75f);
    static readonly Color DARK     = new Color(0.08f, 0.09f, 0.16f);
    static readonly Color BTN_OFF  = new Color(0.22f, 0.24f, 0.34f);

    static readonly Color[] AUTO_COLORS = {
        new Color(0.30f, 0.60f, 1.00f),  // azul
        new Color(0.92f, 0.45f, 0.20f),  // naranja
        new Color(0.60f, 0.32f, 0.95f),  // violeta
        new Color(0.20f, 0.75f, 0.55f),  // verde
        new Color(0.90f, 0.30f, 0.50f),  // rosa
    };

    // ═══════════════════════════════════════════════════════════════════════
    //  MINIGAME BASE
    // ═══════════════════════════════════════════════════════════════════════

    protected override void OnMinigameStart()
    {
        if (actions == null || actions.Count == 0)
        {
            actions = new List<ActionData>
            {
                new ActionData { icon = "✿",  actionName = "Pasito corto",  cost = 1, progress = 8  },
                new ActionData { icon = "✦",  actionName = "Buen avance",   cost = 2, progress = 18 },
                new ActionData { icon = "⚡", actionName = "Super impulso", cost = 4, progress = 40 },
            };
        }

        EnsureES();
        _maxStars    = GetStars();
        _stars       = _maxStars;
        _progress    = 0f;
        _displayProg = 0f;
        _ended       = false;

        BuildUI();
        Refresh();
    }

    protected override void OnMinigameComplete() { }
    protected override void OnMinigameFailed()   { }

    private void Update()
    {
        if (Mathf.Abs(_displayProg - _progress) > 0.15f)
        {
            _displayProg = Mathf.Lerp(_displayProg, _progress, Time.deltaTime * 8f);
            UpdateProgVisual();
        }
    }

    private int GetStars()
    {
        if (GameManager.Instance == null) return starsEasy;
        DifficultyLevel d = GameManager.Instance.CurrentDifficulty;
        if (d == DifficultyLevel.Medium) return starsMedium;
        if (d == DifficultyLevel.Hard)   return starsHard;
        return starsEasy;
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  LÓGICA
    // ═══════════════════════════════════════════════════════════════════════

    private void DoAction(int i)
    {
        if (_ended || i < 0 || i >= actions.Count) return;
        ActionData a = actions[i];
        if (_stars < a.cost) return;

        _stars    -= a.cost;
        _progress  = Mathf.Min(_progress + a.progress, goal);

        Refresh();
        StartCoroutine(Pulse(i));

        if (_progress >= goal)
        {
            _ended = true;
            StartCoroutine(Finish(true));
        }
        else if (!CanDoAny())
        {
            _ended = true;
            StartCoroutine(Finish(false));
        }
    }

    private bool CanDoAny()
    {
        for (int i = 0; i < actions.Count; i++)
            if (_stars >= actions[i].cost) return true;
        return false;
    }

    private IEnumerator Finish(bool won)
    {
        yield return new WaitForSeconds(0.6f);
        if (won)
        {
            int sc = 500 + Mathf.RoundToInt(((float)_stars / _maxStars) * 500f);
            CompleteMinigame(sc);
        }
        else FailMinigame();
        ShowEnd(won);
    }

    private void Reset()
    {
        StopAllCoroutines();
        _stars       = _maxStars;
        _progress    = 0f;
        _displayProg = 0f;
        _ended       = false;
        if (_endPanel != null) _endPanel.SetActive(false);
        Refresh();
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  REFRESH UI
    // ═══════════════════════════════════════════════════════════════════════

    private void Refresh()
    {
        float r = Mathf.Clamp01((float)_stars / _maxStars);
        _starsFill.anchorMax = new Vector2(Mathf.Max(r, 0.005f), 1f);
        _starsLbl.text = _stars + " / " + _maxStars;

        if (r > 0.5f)      _starsFillImg.color = YELLOW;
        else if (r > 0.25f) _starsFillImg.color = ORANGE;
        else                _starsFillImg.color = RED;

        UpdateProgVisual();

        for (int i = 0; i < _btns.Count; i++)
        {
            bool ok = _stars >= actions[i].cost && !_ended;
            _btns[i].interactable = ok;
            Color c = GetBtnColor(i);
            _btnBgs[i].color = ok ? c : BTN_OFF;
            _btnGroups[i].alpha = ok ? 1f : 0.45f;
        }
    }

    private void UpdateProgVisual()
    {
        float r = Mathf.Clamp01(_displayProg / goal);
        _progFill.anchorMax = new Vector2(Mathf.Max(r, 0.005f), 1f);
        int pct = Mathf.RoundToInt(_displayProg);
        _progLbl.text = pct + "%";
        _progPct.text = pct + "%";
        _progPct.color = r >= 1f ? GREEN : Color.white;
    }

    private IEnumerator Pulse(int i)
    {
        if (i >= _btns.Count) yield break;
        RectTransform rt = _btns[i].GetComponent<RectTransform>();
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 10f;
            float s = 1f + 0.06f * Mathf.Sin(t * Mathf.PI);
            rt.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
        rt.localScale = Vector3.one;
    }

    private Color GetBtnColor(int i)
    {
        if (actions[i].buttonColor != Color.black)
            return actions[i].buttonColor;
        return AUTO_COLORS[i % AUTO_COLORS.Length];
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  BUILD UI
    // ═══════════════════════════════════════════════════════════════════════

    private void BuildUI()
    {
        // Canvas
        GameObject cGO = new GameObject("Canvas");
        cGO.transform.SetParent(transform, false);
        Canvas cv = cGO.AddComponent<Canvas>();
        cv.renderMode = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 10;
        CanvasScaler cs = cGO.AddComponent<CanvasScaler>();
        cs.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920f, 1080f);
        cs.matchWidthOrHeight  = 0.5f;
        cGO.AddComponent<GraphicRaycaster>();
        RectTransform R = cGO.GetComponent<RectTransform>();

        // Fondo
        Img(R, "BG", BG, V(0,0), V(1,1), V(0,0), V(0,0));

        // ── HEADER ──
        RectTransform hdr = Img(R, "Hdr", HEADER, V(0,1), V(1,1), V(0,-44), V(0,88));
        Img(hdr, "Line", ACCENT, V(0,0), V(1,0), V(0,1.5f), V(0,3));

        TextMeshProUGUI t1 = Txt(hdr, "T", "Gestion de recursos", Color.white, 42, V(0.03f,0), V(0.70f,1));
        t1.fontStyle = FontStyles.Bold;
        t1.alignment = TextAlignmentOptions.MidlineLeft;

        TextMeshProUGUI t2 = Txt(hdr, "I", "Usa tus estrellas para llegar al 100%", DIM, 24, V(0.45f,0), V(0.98f,1));
        t2.alignment = TextAlignmentOptions.MidlineRight;

        // ── ZONA SUPERIOR: barras (y: 0.60 – 0.88) ──
        RectTransform topP = Img(R, "TopP", PANEL, V(0.03f,0.58f), V(0.97f,0.88f), V(0,0), V(0,0));
        BuildBars(topP);

        // ── ZONA INFERIOR: botones de acción (y: 0.12 – 0.55) ──
        RectTransform botP = Img(R, "BotP", PANEL, V(0.03f,0.12f), V(0.97f,0.55f), V(0,0), V(0,0));
        BuildActions(botP);

        // ── BARRA INFERIOR ──
        RectTransform bar = Img(R, "Bar", HEADER, V(0,0), V(1,0), V(0,45), V(0,90));
        MkBtn(bar, "Reiniciar", new Color(0.32f,0.34f,0.44f), V(0.06f,0.12f), V(0.46f,0.88f), () => Reset());
        MkBtn(bar, "Volver al menu", ACCENT, V(0.54f,0.12f), V(0.94f,0.88f), () => ReturnToGameSelector());

        // ── PANEL FIN ──
        BuildEnd(R);
    }

    // ── Barras de estrellas y progreso ─────────────────────────────────────

    private void BuildBars(RectTransform p)
    {
        // ─ Estrellas (mitad izquierda) ─
        TextMeshProUGUI sT = Txt(p, "ST", "ESTRELLAS", YELLOW, 28, V(0.03f,0.70f), V(0.30f,0.95f));
        sT.fontStyle = FontStyles.Bold; sT.alignment = TextAlignmentOptions.MidlineLeft;

        _starsLbl = Txt(p, "SV", "", Color.white, 26, V(0.30f,0.70f), V(0.48f,0.95f));
        _starsLbl.alignment = TextAlignmentOptions.MidlineRight;

        RectTransform sBg = Img(p, "SBg", DARK, V(0.03f,0.35f), V(0.48f,0.70f), V(0,0), V(0,0));
        GameObject sf = new GameObject("SF");
        sf.transform.SetParent(sBg, false);
        _starsFill = sf.AddComponent<RectTransform>();
        _starsFill.anchorMin = V(0,0); _starsFill.anchorMax = V(1,1);
        _starsFill.offsetMin = V(0,0); _starsFill.offsetMax = V(0,0);
        _starsFillImg = sf.AddComponent<Image>();
        _starsFillImg.color = YELLOW;

        // Texto indicador "Cuantas estrellas te quedan"
        TextMeshProUGUI sH = Txt(p, "SH", "Cuantas estrellas te quedan", DIM, 19, V(0.03f,0.06f), V(0.48f,0.34f));
        sH.alignment = TextAlignmentOptions.Center;

        // ─ Progreso (mitad derecha) ─
        TextMeshProUGUI pT = Txt(p, "PT", "PROGRESO", GREEN, 28, V(0.53f,0.70f), V(0.76f,0.95f));
        pT.fontStyle = FontStyles.Bold; pT.alignment = TextAlignmentOptions.MidlineLeft;

        _progLbl = Txt(p, "PV", "0%", Color.white, 26, V(0.76f,0.70f), V(0.97f,0.95f));
        _progLbl.alignment = TextAlignmentOptions.MidlineRight;

        RectTransform pBg = Img(p, "PBg", DARK, V(0.53f,0.35f), V(0.97f,0.70f), V(0,0), V(0,0));
        GameObject pf = new GameObject("PF");
        pf.transform.SetParent(pBg, false);
        _progFill = pf.AddComponent<RectTransform>();
        _progFill.anchorMin = V(0,0); _progFill.anchorMax = V(0,1);
        _progFill.offsetMin = V(0,0); _progFill.offsetMax = V(0,0);
        pf.AddComponent<Image>().color = GREEN;

        _progPct = Txt(p, "PP", "0%", Color.white, 42, V(0.53f,0.06f), V(0.97f,0.34f));
        _progPct.fontStyle = FontStyles.Bold;
        _progPct.alignment = TextAlignmentOptions.Center;
    }

    // ── Botones de acción ─────────────────────────────────────────────────

    private void BuildActions(RectTransform p)
    {
        TextMeshProUGUI at = Txt(p, "AT", "Elige una accion:", Color.white, 28, V(0.03f,0.88f), V(0.97f,1f));
        at.fontStyle = FontStyles.Bold; at.alignment = TextAlignmentOptions.Center;

        int n = Mathf.Min(actions.Count, 5);
        _btns.Clear(); _btnBgs.Clear(); _btnGroups.Clear();

        // Botones distribuidos en horizontal
        float margin = 0.03f;
        float gap = 0.02f;
        float total = 1f - margin * 2f;
        float w = (total - gap * (n - 1)) / n;

        for (int i = 0; i < n; i++)
        {
            int idx = i;
            ActionData a = actions[i];
            Color col = GetBtnColor(i);

            float xL = margin + (w + gap) * i;
            float xR = xL + w;

            // Contenedor del botón
            RectTransform brt = Img(p, "A" + i, col, V(xL, 0.04f), V(xR, 0.85f), V(0,0), V(0,0));
            CanvasGroup cg = brt.gameObject.AddComponent<CanvasGroup>();

            Button btn = brt.gameObject.AddComponent<Button>();
            Image bg = brt.GetComponent<Image>();
            btn.targetGraphic = bg;
            ColorBlock cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1,1,1,0.85f);
            cb.pressedColor = new Color(0.7f,0.7f,0.7f);
            cb.disabledColor = new Color(0.5f,0.5f,0.5f,0.4f);
            btn.colors = cb;
            btn.onClick.AddListener(() => DoAction(idx));

            // Nombre grande centrado
            TextMeshProUGUI nm = Txt(brt, "Nm", a.actionName, Color.white, 34, V(0.04f,0.38f), V(0.96f,0.95f));
            nm.fontStyle = FontStyles.Bold;
            nm.alignment = TextAlignmentOptions.Center;

            // Coste y ganancia
            RectTransform infoBg = Img(brt, "Info", new Color(0,0,0,0.25f), V(0.06f,0.04f), V(0.94f,0.34f), V(0,0), V(0,0));

            TextMeshProUGUI costT = Txt(infoBg, "C", "-" + a.cost + " E", YELLOW, 22, V(0,0.5f), V(1,1));
            costT.fontStyle = FontStyles.Bold; costT.alignment = TextAlignmentOptions.Center;

            TextMeshProUGUI gainT = Txt(infoBg, "G", "+" + a.progress + "%", GREEN, 22, V(0,0), V(1,0.5f));
            gainT.fontStyle = FontStyles.Bold; gainT.alignment = TextAlignmentOptions.Center;

            _btns.Add(btn);
            _btnBgs.Add(bg);
            _btnGroups.Add(cg);
        }
    }

    // ── Panel fin ─────────────────────────────────────────────────────────

    private void BuildEnd(RectTransform R)
    {
        _endPanel = new GameObject("End");
        _endPanel.transform.SetParent(R, false);
        RectTransform oR = _endPanel.AddComponent<RectTransform>();
        oR.anchorMin = V(0,0); oR.anchorMax = V(1,1);
        oR.sizeDelta = V(0,0); oR.anchoredPosition = V(0,0);
        _endPanel.AddComponent<Image>().color = new Color(0,0,0,0.82f);

        RectTransform card = Img(oR, "Card", PANEL, V(0.5f,0.5f), V(0.5f,0.5f), V(0,0), V(720,440));

        _endBar = Img(card, "Bar", GREEN, V(0,1), V(1,1), V(0,-14), V(0,28)).GetComponent<Image>();

        _endTitle = Txt(card, "Ti", "", Color.white, 52, V(0.05f,0.55f), V(0.95f,0.92f));
        _endTitle.fontStyle = FontStyles.Bold;
        _endTitle.alignment = TextAlignmentOptions.Center;

        _endSub = Txt(card, "Su", "", DIM, 26, V(0.05f,0.28f), V(0.95f,0.50f));
        _endSub.alignment = TextAlignmentOptions.Center;

        MkBtn(card, "Jugar de nuevo", ACCENT, V(0.08f,0.04f), V(0.48f,0.24f), () => Reset());
        MkBtn(card, "Menu", new Color(0.32f,0.34f,0.44f), V(0.52f,0.04f), V(0.92f,0.24f), () => ReturnToGameSelector());

        _endPanel.SetActive(false);
    }

    private void ShowEnd(bool won)
    {
        _endBar.color   = won ? GREEN : RED;
        _endTitle.text  = won ? "Objetivo completado!" : "Te quedaste sin estrellas!";
        _endSub.text    = won
            ? "Estrellas sobrantes: " + _stars + " / " + _maxStars
            : "Necesitas planificar mejor.\nIntentalo de nuevo!";
        _endPanel.SetActive(true);
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  HELPERS
    // ═══════════════════════════════════════════════════════════════════════

    private static Vector2 V(float x, float y) { return new Vector2(x, y); }

    private RectTransform Img(RectTransform parent, string name, Color c,
                              Vector2 amin, Vector2 amax, Vector2 pos, Vector2 sd)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = amin; rt.anchorMax = amax;
        rt.pivot = V(0.5f, 0.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = sd;
        go.AddComponent<Image>().color = c;
        return rt;
    }

    private TextMeshProUGUI Txt(RectTransform parent, string name, string text,
                                Color c, float size, Vector2 amin, Vector2 amax)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = amin; rt.anchorMax = amax;
        rt.pivot = V(0.5f, 0.5f);
        rt.anchoredPosition = V(0,0); rt.sizeDelta = V(0,0);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.color = c; tmp.fontSize = size;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        return tmp;
    }

    private void MkBtn(RectTransform parent, string label, Color bgC,
                       Vector2 amin, Vector2 amax,
                       UnityEngine.Events.UnityAction click)
    {
        RectTransform bg = Img(parent, "B_" + label, bgC, amin, amax, V(0,0), V(0,0));
        Button b = bg.gameObject.AddComponent<Button>();
        b.targetGraphic = bg.GetComponent<Image>();
        ColorBlock cb = b.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1,1,1,0.85f);
        cb.pressedColor = new Color(0.7f,0.7f,0.7f);
        b.colors = cb;
        b.onClick.AddListener(click);
        TextMeshProUGUI t = Txt(bg, "T", label, Color.white, 28, V(0,0), V(1,1));
        t.fontStyle = FontStyles.Bold;
    }

    private static void EnsureES()
    {
        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }
    }
}
