// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class ActionSequenceController : MinigameBase
{

    class Routine
    {
        public string   title;
        public string[] steps;
        public Routine(string t, params string[] s) { title = t; steps = s; }
    }

    // ---- Banco de rutinas por dificultad (orden = causa-efecto) ----

    static readonly Routine[] EASY_POOL =
    {
        new Routine("La mañana",
            "Despertarte", "Vestirte", "Desayunar", "Coger la mochila"),
        new Routine("A dormir",
            "Ponerte el pijama", "Lavarte los dientes", "Meterte en la cama", "Apagar la luz"),
        new Routine("El bocadillo",
            "Lavarte las manos", "Coger el pan", "Poner el queso", "Comer el bocadillo"),
        new Routine("La mascota",
            "Coger la correa", "Salir a pasear", "Volver a casa", "Darle de comer"),
    };

    static readonly Routine[] MEDIUM_POOL =
    {
        new Routine("El colegio",
            "Llegar al colegio", "Sacar los libros", "Escuchar al profesor",
            "Hacer los ejercicios", "Guardar las cosas"),
        new Routine("La excursion",
            "Preparar la mochila", "Ponerte las botas", "Subir al autobus",
            "Caminar por el bosque", "Hacer un picnic"),
        new Routine("El bizcocho",
            "Lavarte las manos", "Sacar los ingredientes", "Mezclar la masa",
            "Hornear el bizcocho", "Probar un trozo"),
        new Routine("A dormir",
            "Cenar", "Ponerte el pijama", "Lavarte los dientes",
            "Leer un cuento", "Apagar la luz"),
    };

    static readonly Routine[] HARD_POOL =
    {
        new Routine("La mañana",
            "Despertarte", "Ponerte los calcetines", "Ponerte los zapatos",
            "Desayunar", "Lavarte los dientes", "Coger la mochila"),
        new Routine("El bizcocho",
            "Lavarte las manos", "Encender el horno", "Mezclar la masa",
            "Meter la masa al horno", "Sacar la masa del horno", "Probar el bizcocho"),
        new Routine("La excursion",
            "Preparar la mochila", "Subir al autobus", "Bajar del autobus",
            "Caminar hasta el rio", "Hacer un picnic", "Volver a casa"),
        new Routine("La mascota",
            "Coger la correa", "Ponerle la correa", "Salir a pasear",
            "Quitarle la correa", "Llenar su comedero", "Dejarle descansar"),
    };

    [Header("Segundos de feedback de error antes de reiniciar")]
    public float errorDelay = 0.9f;

    const int ROUNDS = 3;
    int _round;
    int _maxErrors = 3;
    int _errors;
    int _correctPresses;

    Routine[] _pool;
    int[]     _routineOrder;
    Routine   _routine;

    string[] _sequence;
    string[] _shuffled;
    int      _progress;
    bool     _locked;
    float    _lastPressTime;

    RectTransform _canvasRT;

    TextMeshProUGUI _progressLbl;
    TextMeshProUGUI _roundLbl;
    TextMeshProUGUI _instructLbl;
    Image[]         _dots;

    GameObject        _btnAreaGO;
    Button[]          _btns;
    Image[]           _btnBgs;
    TextMeshProUGUI[] _btnLbls;
    Color[]           _btnDefaultColors;

    GameObject      _transPanel;
    TextMeshProUGUI _transTitle;
    TextMeshProUGUI _transSub;

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

    protected override string GetIntroDescription() =>
        "Los pasos de cada mision estan desordenados.\n" +
        "Piensa que va primero y pulsalos en el orden logico.";

    protected override void OnMinigameStart()
    {
        EnsureES();
        ApplyDifficulty();
        BuildUI();
        StartRound(0);
    }

    void ApplyDifficulty()
    {
        var diff = GameManager.Instance != null
            ? GameManager.Instance.CurrentDifficulty
            : DifficultyLevel.Easy;
        switch (diff)
        {
            case DifficultyLevel.Medium: _maxErrors = 3; _pool = MEDIUM_POOL; break;
            case DifficultyLevel.Hard:   _maxErrors = 1; _pool = HARD_POOL;   break;
            default:                     _maxErrors = 5; _pool = EASY_POOL;   break;
        }
        _errors         = 0;
        _correctPresses = 0;

        // Elige 3 rutinas distintas al azar → rejugabilidad
        _routineOrder = new int[_pool.Length];
        for (int i = 0; i < _routineOrder.Length; i++) _routineOrder[i] = i;
        for (int i = _routineOrder.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int tmp = _routineOrder[i]; _routineOrder[i] = _routineOrder[j]; _routineOrder[j] = tmp;
        }
    }

    protected override void OnMinigameComplete() { }
    protected override void OnMinigameFailed()   { }

    void StartRound(int r)
    {
        _round = r;
        _locked = false;

        if (_transPanel != null) _transPanel.SetActive(false);

        _routine  = _pool[_routineOrder[r % _routineOrder.Length]];
        _sequence = _routine.steps;
        _shuffled = (string[])_sequence.Clone();
        do { Shuffle(_shuffled); } while (SameOrder(_shuffled, _sequence));

        RebuildButtons();

        UpdateRoundUI();
        ResetRound();
        _lastPressTime = Time.realtimeSinceStartup;
    }

    static bool SameOrder(string[] a, string[] b)
    {
        for (int i = 0; i < a.Length; i++)
            if (a[i] != b[i]) return false;
        return true;
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
        GameFeel.PlaySuccess();
        GameFeel.Confetti(20);
        yield return new WaitForSeconds(0.5f);

        if (_round >= ROUNDS - 1)
        {
            int score = Mathf.Max(200, 1000 - _errors * 100);
            float ratio = _correctPresses + _errors > 0
                ? (float)_correctPresses / (_correctPresses + _errors)
                : 1f;

            CompleteMinigame(score);
            ShowResults(true, GameFeel.StarsFromRatio(true, ratio), score,
                new[]
                {
                    "Misiones: " + ROUNDS + " / " + ROUNDS,
                    "Pasos correctos: " + _correctPresses,
                    "Errores: " + _errors
                });
        }
        else
        {
            StartCoroutine(Transition());
        }
    }

    IEnumerator Transition()
    {
        _transPanel.SetActive(true);
        _transPanel.transform.SetAsLastSibling();
        _transTitle.text = "\"" + _routine.title + "\" completada!";
        _transSub.text   = "Pulsa Continuar para la siguiente mision";
        yield break;
    }

    void OnActionPressed(int shuffleIdx)
    {
        if (_locked) return;

        float rtMs = (Time.realtimeSinceStartup - _lastPressTime) * 1000f;
        _lastPressTime = Time.realtimeSinceStartup;

        string pressed  = _shuffled[shuffleIdx];
        string expected = _sequence[_progress];

        if (pressed == expected)
        {
            _correctPresses++;
            ReportEvent(true, rtMs);
            StartCoroutine(CorrectFeedback(shuffleIdx));
        }
        else
        {
            ReportEvent(false, rtMs);
            StartCoroutine(WrongFeedback(shuffleIdx));
        }
    }

    IEnumerator CorrectFeedback(int shuffleIdx)
    {
        _locked = true;
        GameFeel.PlayPop();
        _btnBgs[shuffleIdx].color = GREEN;
        yield return StartCoroutine(PulseBtn(shuffleIdx, 1.12f));

        _progress++;
        RefreshProgress();

        if (_progress >= _sequence.Length)
        {

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
        GameFeel.PlayError();
        _btnBgs[shuffleIdx].color = RED;
        GameFeel.Shake(_btns[shuffleIdx].GetComponent<RectTransform>(), 10f, 0.3f);
        yield return StartCoroutine(PulseBtn(shuffleIdx, 1.08f));
        yield return new WaitForSeconds(errorDelay * 0.5f);
        _btnBgs[shuffleIdx].color = RED;
        yield return new WaitForSeconds(errorDelay * 0.5f);

        _progress = 0;
        RefreshProgress();
        ResetButtonColors();

        _errors++;
        if (_errors > _maxErrors)
        {
            _locked = true;
            FailMinigame();
            ShowResults(false, 0, 0,
                new[]
                {
                    "Misiones superadas: " + _round + " / " + ROUNDS,
                    "Errores: " + _errors
                },
                "¡Casi!",
                "Lee cada paso y piensa que va primero");
            yield break;
        }

        if (_instructLbl != null)
        {
            int left = _maxErrors - _errors + 1;
            _instructLbl.text = "Ese paso no toca aun. Te quedan " + left +
                                (left == 1 ? " intento" : " intentos");
        }

        _locked = false;
    }

    void RefreshProgress()
    {
        if (_progressLbl != null)
            _progressLbl.text = "Paso " + _progress + " de " + _sequence.Length;

        if (_instructLbl != null)
        {
            if (_progress == 0)
                _instructLbl.text = "Mision: " + _routine.title + " — ¿que haces primero?";
            else if (_progress < _sequence.Length)
                _instructLbl.text = "Mision: " + _routine.title + " — ¡bien! ¿y despues?";
        }
    }

    void ResetButtonColors()
    {
        if (_btnBgs == null) return;
        for (int i = 0; i < _btnBgs.Length; i++)
            _btnBgs[i].color = _btnDefaultColors[i];
    }

    void UpdateRoundUI()
    {
        if (_roundLbl != null)
            _roundLbl.text = "Mision " + (_round + 1) + " / " + ROUNDS;
    }

    void UpdateDots()
    {
        if (_dots == null) return;
        for (int i = 0; i < _dots.Length; i++)
            _dots[i].color = i <= _round ? ACCENT : DOTOFF;
    }

    void RebuildButtons()
    {

        if (_btnAreaGO != null)
            DestroyImmediate(_btnAreaGO);

        int n = _shuffled.Length;
        int cols = (n <= 4) ? n : Mathf.CeilToInt(n / 2f);
        int rows = (n <= 4) ? 1 : 2;

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
            ButtonJuice.Attach(bg.gameObject);

            float fSize = n >= 6 ? 30f : 36f;
            var lbl = MkTxt(bg, "Lbl", _shuffled[i], Color.white, fSize, V2(0.05f, 0.1f), V2(0.95f, 0.9f));
            lbl.fontStyle = FontStyles.Bold;

            _btns[i]             = btn;
            _btnBgs[i]           = bg.GetComponent<Image>();
            _btnLbls[i]          = lbl;
            _btnDefaultColors[i] = BTNC;
        }

        if (_transPanel != null) _transPanel.transform.SetAsLastSibling();
    }

    IEnumerator PulseBtn(int idx, float peak)
    {
        if (_btns == null || idx >= _btns.Length) yield break;
        RectTransform rt = _btns[idx].GetComponent<RectTransform>();
        float t = 0f;
        while (t < 1f)
        {
            if (rt == null) yield break;
            t += Time.deltaTime * 12f;
            float s = 1f + (peak - 1f) * Mathf.Sin(t * Mathf.PI);
            rt.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
        if (rt != null) rt.localScale = Vector3.one;
    }

    void BuildUI()
    {

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

        MkImg(_canvasRT, "BG", BG, V2(0, 0), V2(1, 1), V2(0, 0), V2(0, 0));

        RectTransform hdr = MkImg(_canvasRT, "Hdr", HDR, V2(0, 1), V2(1, 1), V2(0, -40), V2(0, 80));
        MkImg(hdr, "HL", ACCENT, V2(0, 0), V2(1, 0), V2(0, 1.5f), V2(0, 3));

        var ht = MkTxt(hdr, "T", "Secuencia de acciones", Color.white, 40, V2(0.03f, 0), V2(0.50f, 1));
        ht.fontStyle = FontStyles.Bold;
        ht.alignment = TextAlignmentOptions.MidlineLeft;

        _roundLbl = MkTxt(hdr, "RL", "Mision 1 / 3", DIM, 26, V2(0.50f, 0), V2(0.68f, 1));
        _roundLbl.alignment = TextAlignmentOptions.MidlineRight;

        _progressLbl = MkTxt(hdr, "PL", "Paso 0 de 4", ACCENT, 26, V2(0.68f, 0), V2(0.86f, 1));
        _progressLbl.fontStyle = FontStyles.Bold;
        _progressLbl.alignment = TextAlignmentOptions.MidlineRight;

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

        RectTransform instrArea = MkImg(_canvasRT, "IA", new Color(0, 0, 0, 0),
            V2(0.05f, 0.80f), V2(0.95f, 0.91f), V2(0, 0), V2(0, 0));
        _instructLbl = MkTxt(instrArea, "IL",
            "Pulsa los pasos en el orden logico",
            DIM, 30, V2(0, 0), V2(1, 1));
        _instructLbl.alignment = TextAlignmentOptions.Center;
        _instructLbl.overflowMode = TextOverflowModes.Overflow;

        RectTransform bot = MkImg(_canvasRT, "Bot", HDR, V2(0, 0), V2(1, 0), V2(0, 45), V2(0, 90));
        MkImg(bot, "BotL", ACCENT, V2(0, 1), V2(1, 1), V2(0, -1.5f), V2(0, 3));
        MkBtn(bot, "Reiniciar mision", GREY,      V2(0.04f, 0.12f), V2(0.35f, 0.88f), () =>
        {
            if (!IsPlaying) return;
            StopAllCoroutines();
            if (_transPanel != null) _transPanel.SetActive(false);
            StartRound(_round);
        });

        BuildTransPanel(_canvasRT);
    }

    void BuildTransPanel(RectTransform R)
    {
        _transPanel = new GameObject("Trans");
        _transPanel.transform.SetParent(R, false);
        RectTransform tr = _transPanel.AddComponent<RectTransform>();
        tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one;
        tr.sizeDelta = Vector2.zero; tr.anchoredPosition = Vector2.zero;
        _transPanel.AddComponent<Image>().color = new Color(0, 0, 0, 0.82f);

        RectTransform card = MkImg(tr, "Card", PANEL, V2(0.5f, 0.5f), V2(0.5f, 0.5f), V2(0, 0), V2(680, 380));
        MkImg(card, "Bar", GREEN, V2(0, 1), V2(1, 1), V2(0, -12), V2(0, 24));

        _transTitle = MkTxt(card, "Ti", "", Color.white, 46, V2(0.05f, 0.60f), V2(0.95f, 0.90f));
        _transTitle.fontStyle = FontStyles.Bold;
        _transSub   = MkTxt(card, "Su", "", DIM, 30, V2(0.05f, 0.32f), V2(0.95f, 0.58f));

        MkBtn(card, "Continuar", GREEN, V2(0.30f, 0.06f), V2(0.70f, 0.24f), () =>
        {
            _transPanel.SetActive(false);
            StartRound(_round + 1);
        });

        _transPanel.SetActive(false);
    }

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
        ButtonJuice.Attach(bg.gameObject);
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
