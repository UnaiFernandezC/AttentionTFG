using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Minijuego "Secuencia de acciones" — Planificacion.
///
/// 3 rondas secuenciales. En cada ronda el jugador pulsa las acciones
/// en el orden correcto para completar la tarea.
///   Ronda 1: easyActions   (Levantarte, Desayunar, Ir al colegio)
///   Ronda 2: mediumActions (Chutar, Marcar, Celebrar)
///   Ronda 3: hardActions   (Tumbarte, Cerrar los ojos, Dormir)
///
/// Correcto  -> boton verde + pulso, avanza al siguiente paso
/// Incorrecto -> parpadeo rojo, reinicia desde el paso 1 de esa ronda
///
/// Toda la UI se genera por codigo; no se necesitan assets externos.
/// </summary>
public class ActionSequenceController : MinigameBase
{
    // ─── Inspector ────────────────────────────────────────────────────────

    [Header("Ronda 1")]
    public string[] easyActions   = { "Levantarte", "Desayunar", "Ir al colegio" };

    [Header("Ronda 2")]
    public string[] mediumActions = { "Chutar", "Marcar", "Celebrar" };

    [Header("Ronda 3")]
    public string[] hardActions   = { "Tumbarte", "Cerrar los ojos", "Dormir" };

    [Header("Segundos de feedback de error antes de reiniciar")]
    public float errorDelay = 0.9f;

    // ─── Estado de sesion ─────────────────────────────────────────────────

    const int ROUNDS = 3;
    int _round;        // 0-based

    // ─── Estado de ronda ─────────────────────────────────────────────────

    string[] _sequence;   // orden correcto fijo
    string[] _shuffled;   // orden aleatorio en pantalla
    int      _progress;   // cuantas acciones correctas lleva en esta ronda
    bool     _locked;     // bloquea pulsaciones durante animacion

    // ─── UI raiz ─────────────────────────────────────────────────────────

    RectTransform _canvasRT;   // root del canvas

    // ─── UI de juego ─────────────────────────────────────────────────────

    TextMeshProUGUI _progressLbl;
    TextMeshProUGUI _roundLbl;
    TextMeshProUGUI _instructLbl;
    Image[]         _dots;

    // Contenedor de botones (se destruye y recrea en cada ronda)
    GameObject        _btnAreaGO;
    Button[]          _btns;
    Image[]           _btnBgs;
    TextMeshProUGUI[] _btnLbls;
    Color[]           _btnDefaultColors;

    // ─── Paneles ─────────────────────────────────────────────────────────

    GameObject      _transPanel;
    TextMeshProUGUI _transTitle;
    TextMeshProUGUI _transSub;

    GameObject      _endPanel;
    Image           _endBar;
    TextMeshProUGUI _endTitle;
    TextMeshProUGUI _endSub;

    // ─── Colores ──────────────────────────────────────────────────────────

    static readonly Color BG     = C(0.08f, 0.09f, 0.18f);
    static readonly Color PANEL  = C(0.12f, 0.13f, 0.24f);
    static readonly Color HDR    = C(0.10f, 0.11f, 0.22f);
    static readonly Color ACCENT = C(0.25f, 0.55f, 1.00f);
    static readonly Color GREEN  = C(0.20f, 0.78f, 0.48f);
    static readonly Color RED    = C(0.85f, 0.25f, 0.32f);
    static readonly Color YELLOW = C(1.00f, 0.84f, 0.22f);
    static readonly Color DIM    = C(0.55f, 0.58f, 0.75f);
    static readonly Color GREY   = C(0.28f, 0.30f, 0.42f);
    static readonly Color DOTOFF = C(0.25f, 0.27f, 0.45f);
    static readonly Color BTNC   = C(0.18f, 0.22f, 0.45f);

    static Color C(float r, float g, float b) { return new Color(r, g, b); }

    // ═════════════════════════════════════════════════════════════════════
    //  MINIGAME BASE
    // ═════════════════════════════════════════════════════════════════════

    protected override void OnMinigameStart()
    {
        EnsureES();
        BuildUI();
        StartRound(0);
    }

    protected override void OnMinigameComplete() { }
    protected override void OnMinigameFailed()   { }

    // ═════════════════════════════════════════════════════════════════════
    //  GESTION DE RONDAS
    // ═════════════════════════════════════════════════════════════════════

    void StartRound(int r)
    {
        _round = r;
        _locked = false;

        if (_transPanel != null) _transPanel.SetActive(false);
        if (_endPanel   != null) _endPanel.SetActive(false);

        // Elegir secuencia
        string[] src = SequenceFor(r);
        _sequence = src;
        _shuffled = (string[])src.Clone();
        Shuffle(_shuffled);

        // Reconstruir botones para esta ronda
        RebuildButtons();

        UpdateRoundUI();
        ResetRound();
    }

    string[] SequenceFor(int r)
    {
        if (r == 0 && easyActions   != null && easyActions.Length   > 0) return easyActions;
        if (r == 1 && mediumActions != null && mediumActions.Length > 0) return mediumActions;
        if (r == 2 && hardActions   != null && hardActions.Length   > 0) return hardActions;
        return new string[]{ "Paso 1", "Paso 2", "Paso 3" };
    }

    void ResetRound()
    {
        _progress = 0;
        _locked   = false;
        RefreshProgress();
        ResetButtonColors();
    }

    IEnumerator HandleRoundComplete()
    {
        yield return new WaitForSeconds(0.5f);

        if (_round >= ROUNDS - 1)
        {
            // Todas las rondas completadas
            CompleteMinigame(1000);
            ShowEnd();
        }
        else
        {
            StartCoroutine(Transition());
        }
    }

    IEnumerator Transition()
    {
        _transPanel.SetActive(true);
        _transTitle.text = "Ronda " + (_round + 1) + " completada!";
        _transSub.text   = "Preparate para la siguiente...";

        yield return new WaitForSeconds(1f);
        for (int i = 3; i >= 1; i--)
        {
            _transSub.text = "Siguiente ronda en " + i + "...";
            yield return new WaitForSeconds(1f);
        }

        _transPanel.SetActive(false);
        StartRound(_round + 1);
    }

    // ═════════════════════════════════════════════════════════════════════
    //  LOGICA DE PULSACION
    // ═════════════════════════════════════════════════════════════════════

    void OnActionPressed(int shuffleIdx)
    {
        if (_locked) return;

        string pressed  = _shuffled[shuffleIdx];
        string expected = _sequence[_progress];

        if (pressed == expected)
            StartCoroutine(CorrectFeedback(shuffleIdx));
        else
            StartCoroutine(WrongFeedback(shuffleIdx));
    }

    IEnumerator CorrectFeedback(int shuffleIdx)
    {
        _locked = true;
        _btnBgs[shuffleIdx].color = GREEN;
        yield return StartCoroutine(PulseBtn(shuffleIdx, 1.12f));

        _progress++;
        RefreshProgress();

        if (_progress >= _sequence.Length)
        {
            // Ronda completada
            UpdateDots();
            yield return StartCoroutine(HandleRoundComplete());
        }
        else
        {
            _locked = false;
        }
    }

    IEnumerator WrongFeedback(int shuffleIdx)
    {
        _locked = true;
        _btnBgs[shuffleIdx].color = RED;
        yield return StartCoroutine(PulseBtn(shuffleIdx, 1.08f));
        yield return new WaitForSeconds(errorDelay * 0.5f);
        _btnBgs[shuffleIdx].color = RED;
        yield return new WaitForSeconds(errorDelay * 0.5f);

        _progress = 0;
        RefreshProgress();
        ResetButtonColors();
        _locked = false;
    }

    void RefreshProgress()
    {
        if (_progressLbl != null)
            _progressLbl.text = "Paso " + _progress + " de " + _sequence.Length;

        if (_instructLbl != null)
        {
            if (_progress == 0)
                _instructLbl.text = "Selecciona las acciones en el orden correcto";
            else if (_progress < _sequence.Length)
                _instructLbl.text = "Bien! Sigue con el siguiente paso";
        }
    }

    void ResetButtonColors()
    {
        if (_btnBgs == null) return;
        for (int i = 0; i < _btnBgs.Length; i++)
            _btnBgs[i].color = _btnDefaultColors[i];
    }

    // ═════════════════════════════════════════════════════════════════════
    //  UI — ROUND HEADER
    // ═════════════════════════════════════════════════════════════════════

    void UpdateRoundUI()
    {
        if (_roundLbl != null)
            _roundLbl.text = "Ronda " + (_round + 1) + " / " + ROUNDS;
    }

    void UpdateDots()
    {
        if (_dots == null) return;
        for (int i = 0; i < _dots.Length; i++)
            _dots[i].color = i <= _round ? ACCENT : DOTOFF;
    }

    // ═════════════════════════════════════════════════════════════════════
    //  UI — BOTONES (se reconstruyen en cada ronda)
    // ═════════════════════════════════════════════════════════════════════

    void RebuildButtons()
    {
        // Destruir botones anteriores
        if (_btnAreaGO != null)
            DestroyImmediate(_btnAreaGO);

        int n = _shuffled.Length;
        int cols = (n <= 4) ? n : Mathf.CeilToInt(n / 2f);
        int rows = (n <= 4) ? 1 : 2;

        // Crear nuevo contenedor anclado en el canvas
        _btnAreaGO = new GameObject("BtnArea");
        _btnAreaGO.transform.SetParent(_canvasRT, false);
        RectTransform area = _btnAreaGO.AddComponent<RectTransform>();
        area.anchorMin = V2(0.05f, 0.22f);
        area.anchorMax = V2(0.95f, 0.79f);
        area.sizeDelta = Vector2.zero;
        area.anchoredPosition = Vector2.zero;
        _btnAreaGO.AddComponent<Image>().color = new Color(0, 0, 0, 0);

        _btns             = new Button[n];
        _btnBgs           = new Image[n];
        _btnLbls          = new TextMeshProUGUI[n];
        _btnDefaultColors = new Color[n];

        float btnW = 1f / cols;
        float btnH = 1f / rows;
        float pad  = 0.015f;

        for (int i = 0; i < n; i++)
        {
            int col = i % cols;
            int row = i / cols;

            float xMin = col * btnW + pad;
            float xMax = (col + 1) * btnW - pad;
            float yMin = (rows - 1 - row) * btnH + pad;
            float yMax = (rows - row) * btnH - pad;

            RectTransform bg = MkImg(area, "BtnBg" + i, BTNC,
                V2(xMin, yMin), V2(xMax, yMax), V2(0, 0), V2(0, 0));
            MkImg(bg, "Top", ACCENT, V2(0, 1), V2(1, 1), V2(0, -3), V2(0, 6));

            Button btn = bg.gameObject.AddComponent<Button>();
            btn.targetGraphic = bg.GetComponent<Image>();
            ColorBlock cb = btn.colors;
            cb.normalColor      = Color.white;
            cb.highlightedColor = new Color(1, 1, 1, 0.85f);
            cb.pressedColor     = new Color(0.7f, 0.7f, 0.7f);
            cb.disabledColor    = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            btn.colors = cb;

            int idx = i;
            btn.onClick.AddListener(() => OnActionPressed(idx));

            var lbl = MkTxt(bg, "Lbl", _shuffled[i], Color.white, 36, V2(0.05f, 0.1f), V2(0.95f, 0.9f));
            lbl.fontStyle = FontStyles.Bold;

            _btns[i]             = btn;
            _btnBgs[i]           = bg.GetComponent<Image>();
            _btnLbls[i]          = lbl;
            _btnDefaultColors[i] = BTNC;
        }

        // Garantizar que los paneles overlay siempre esten por encima de los botones
        if (_transPanel != null) _transPanel.transform.SetAsLastSibling();
        if (_endPanel   != null) _endPanel.transform.SetAsLastSibling();
    }

    // ═════════════════════════════════════════════════════════════════════
    //  ANIMACIONES
    // ═════════════════════════════════════════════════════════════════════

    IEnumerator PulseBtn(int idx, float peak)
    {
        if (_btns == null || idx >= _btns.Length) yield break;
        RectTransform rt = _btns[idx].GetComponent<RectTransform>();
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 12f;
            float s = 1f + (peak - 1f) * Mathf.Sin(t * Mathf.PI);
            rt.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
        rt.localScale = Vector3.one;
    }

    // ═════════════════════════════════════════════════════════════════════
    //  PANEL FIN
    // ═════════════════════════════════════════════════════════════════════

    void ShowEnd()
    {
        _endBar.color  = YELLOW;
        _endTitle.text = "Completado!";
        _endSub.text   = "Has superado las 3 rondas correctamente";
        _endPanel.SetActive(true);
    }

    // ═════════════════════════════════════════════════════════════════════
    //  CONSTRUCCION DE UI (solo una vez)
    // ═════════════════════════════════════════════════════════════════════

    void BuildUI()
    {
        // Canvas
        GameObject cGO = new GameObject("Canvas");
        cGO.transform.SetParent(transform, false);
        Canvas cv = cGO.AddComponent<Canvas>();
        cv.renderMode   = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 10;
        CanvasScaler sc = cGO.AddComponent<CanvasScaler>();
        sc.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920f, 1080f);
        sc.matchWidthOrHeight  = 0.5f;
        cGO.AddComponent<GraphicRaycaster>();
        _canvasRT = cGO.GetComponent<RectTransform>();

        // Fondo
        MkImg(_canvasRT, "BG", BG, V2(0, 0), V2(1, 1), V2(0, 0), V2(0, 0));

        // Header
        RectTransform hdr = MkImg(_canvasRT, "Hdr", HDR, V2(0, 1), V2(1, 1), V2(0, -40), V2(0, 80));
        MkImg(hdr, "HL", ACCENT, V2(0, 0), V2(1, 0), V2(0, 1.5f), V2(0, 3));

        var ht = MkTxt(hdr, "T", "Secuencia de acciones", Color.white, 40, V2(0.03f, 0), V2(0.50f, 1));
        ht.fontStyle = FontStyles.Bold;
        ht.alignment = TextAlignmentOptions.MidlineLeft;

        _roundLbl = MkTxt(hdr, "RL", "Ronda 1 / 3", DIM, 26, V2(0.50f, 0), V2(0.68f, 1));
        _roundLbl.alignment = TextAlignmentOptions.MidlineRight;

        _progressLbl = MkTxt(hdr, "PL", "Paso 0 de 3", ACCENT, 26, V2(0.68f, 0), V2(0.86f, 1));
        _progressLbl.fontStyle = FontStyles.Bold;
        _progressLbl.alignment = TextAlignmentOptions.MidlineRight;

        // Dots
        _dots = new Image[ROUNDS];
        for (int i = 0; i < ROUNDS; i++)
        {
            GameObject dot = new GameObject("Dot" + i);
            dot.transform.SetParent(hdr, false);
            RectTransform drt = dot.AddComponent<RectTransform>();
            drt.anchorMin        = new Vector2(1f, 0.5f);
            drt.anchorMax        = new Vector2(1f, 0.5f);
            drt.pivot            = new Vector2(0.5f, 0.5f);
            drt.anchoredPosition = new Vector2(-45f - (ROUNDS - 1 - i) * 26f, 0f);
            drt.sizeDelta        = new Vector2(16f, 16f);
            _dots[i]             = dot.AddComponent<Image>();
            _dots[i].color       = DOTOFF;
        }

        // Instruccion
        RectTransform instrArea = MkImg(_canvasRT, "IA", new Color(0, 0, 0, 0),
            V2(0.05f, 0.80f), V2(0.95f, 0.91f), V2(0, 0), V2(0, 0));
        _instructLbl = MkTxt(instrArea, "IL",
            "Selecciona las acciones en el orden correcto",
            DIM, 30, V2(0, 0), V2(1, 1));
        _instructLbl.alignment = TextAlignmentOptions.Center;

        // Barra inferior
        RectTransform bot = MkImg(_canvasRT, "Bot", HDR, V2(0, 0), V2(1, 0), V2(0, 45), V2(0, 90));
        MkImg(bot, "BotL", ACCENT, V2(0, 1), V2(1, 1), V2(0, -1.5f), V2(0, 3));
        MkBtn(bot, "Reiniciar ronda", GREY,      V2(0.04f, 0.12f), V2(0.35f, 0.88f), () =>
        {
            StopAllCoroutines();
            if (_transPanel != null) _transPanel.SetActive(false);
            if (_endPanel   != null) _endPanel.SetActive(false);
            StartRound(_round);
        });
        MkBtn(bot, "Volver al menu", GREY, V2(0.65f, 0.12f), V2(0.96f, 0.88f), () => ReturnToGameSelector());

        // Panel transicion
        BuildTransPanel(_canvasRT);

        // Panel fin
        BuildEndPanel(_canvasRT);
    }

    void BuildTransPanel(RectTransform R)
    {
        _transPanel = new GameObject("Trans");
        _transPanel.transform.SetParent(R, false);
        RectTransform tr = _transPanel.AddComponent<RectTransform>();
        tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one;
        tr.sizeDelta = Vector2.zero; tr.anchoredPosition = Vector2.zero;
        _transPanel.AddComponent<Image>().color = new Color(0, 0, 0, 0.82f);

        RectTransform card = MkImg(tr, "Card", PANEL, V2(0.5f, 0.5f), V2(0.5f, 0.5f), V2(0, 0), V2(680, 300));
        MkImg(card, "Bar", GREEN, V2(0, 1), V2(1, 1), V2(0, -12), V2(0, 24));

        _transTitle = MkTxt(card, "Ti", "", Color.white, 52, V2(0.05f, 0.50f), V2(0.95f, 0.90f));
        _transTitle.fontStyle = FontStyles.Bold;
        _transSub   = MkTxt(card, "Su", "", DIM, 30, V2(0.05f, 0.08f), V2(0.95f, 0.50f));

        _transPanel.SetActive(false);
    }

    void BuildEndPanel(RectTransform R)
    {
        _endPanel = new GameObject("EndPanel");
        _endPanel.transform.SetParent(R, false);
        RectTransform er = _endPanel.AddComponent<RectTransform>();
        er.anchorMin = Vector2.zero; er.anchorMax = Vector2.one;
        er.sizeDelta = Vector2.zero; er.anchoredPosition = Vector2.zero;
        _endPanel.AddComponent<Image>().color = new Color(0, 0, 0, 0.82f);

        RectTransform card = MkImg(er, "Card", PANEL, V2(0.5f, 0.5f), V2(0.5f, 0.5f), V2(0, 0), V2(700, 380));
        _endBar   = MkImg(card, "Bar", GREEN, V2(0, 1), V2(1, 1), V2(0, -13), V2(0, 26)).GetComponent<Image>();
        _endTitle = MkTxt(card, "Ti", "", Color.white, 58, V2(0.05f, 0.55f), V2(0.95f, 0.92f));
        _endTitle.fontStyle = FontStyles.Bold;
        _endSub   = MkTxt(card, "Su", "", DIM, 28, V2(0.05f, 0.28f), V2(0.95f, 0.55f));

        MkBtn(card, "Jugar de nuevo", ACCENT, V2(0.06f, 0.04f), V2(0.46f, 0.22f), () =>
        {
            StopAllCoroutines();
            _endPanel.SetActive(false);
            StartRound(0);
        });
        MkBtn(card, "Menu", GREY, V2(0.54f, 0.04f), V2(0.94f, 0.22f), () => ReturnToGameSelector());

        _endPanel.SetActive(false);
    }

    // ═════════════════════════════════════════════════════════════════════
    //  HELPERS
    // ═════════════════════════════════════════════════════════════════════

    static void Shuffle(string[] arr)
    {
        for (int i = arr.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            string tmp = arr[i]; arr[i] = arr[j]; arr[j] = tmp;
        }
    }

    static Vector2 V2(float x, float y) { return new Vector2(x, y); }

    RectTransform MkImg(RectTransform p, string n, Color col,
                        Vector2 amin, Vector2 amax, Vector2 pos, Vector2 sd)
    {
        GameObject go = new GameObject(n);
        go.transform.SetParent(p, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = amin; rt.anchorMax = amax;
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = sd;
        go.AddComponent<Image>().color = col;
        return rt;
    }

    TextMeshProUGUI MkTxt(RectTransform p, string n, string text,
                          Color col, float size, Vector2 amin, Vector2 amax)
    {
        GameObject go = new GameObject(n);
        go.transform.SetParent(p, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = amin; rt.anchorMax = amax;
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero; rt.sizeDelta = Vector2.zero;
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text         = text;
        tmp.color        = col;
        tmp.fontSize     = size;
        tmp.alignment    = TextAlignmentOptions.Center;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        return tmp;
    }

    void MkBtn(RectTransform p, string label, Color bgC,
               Vector2 amin, Vector2 amax,
               UnityEngine.Events.UnityAction click)
    {
        RectTransform bg = MkImg(p, "B" + label, bgC, amin, amax, V2(0, 0), V2(0, 0));
        Button b = bg.gameObject.AddComponent<Button>();
        b.targetGraphic = bg.GetComponent<Image>();
        ColorBlock cb   = b.colors;
        cb.normalColor      = Color.white;
        cb.highlightedColor = new Color(1, 1, 1, 0.85f);
        cb.pressedColor     = new Color(0.7f, 0.7f, 0.7f);
        b.colors = cb;
        b.onClick.AddListener(click);
        var t = MkTxt(bg, "T", label, Color.white, 28, V2(0, 0), V2(1, 1));
        t.fontStyle = FontStyles.Bold;
    }

    static void EnsureES()
    {
        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }
    }
}
