// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Minijuego de atencion "Camino numerico" (Trail-Making infantil).
/// Burbujas numeradas dispersas sin solaparse; hay que tocarlas en orden 1-N.
/// Al acertar, la burbuja se colorea y se dibuja una linea desde la anterior.
/// Al fallar, la burbuja se sacude y el juego continua.
///
/// Progresion:
///  - Facil : numeros 1-8, burbujas grandes.
///  - Medio : numeros 1-12 + 4 burbujas distractoras con letras (no tocarlas).
///  - Dificil: alternancia numero-letra 1-A-2-B-... hasta 6-F (TMT-B real,
///             cambio de set atencional).
/// </summary>
public class NumberPathGameManager : MinigameBase
{
    class Bubble
    {
        public RectTransform rt;
        public Image         fill;
        public TextMeshProUGUI label;
        public Button        btn;
        public int           order;        // -1 = distractor
        public bool          done;
    }

    // --- Config (ApplyDifficulty) -------------------------------------------------
    string[] _sequence;          // etiquetas en orden a tocar
    int      _distractorCount;
    float    _bubbleSize = 150f;
    int      _baseScore  = 800;

    // --- Limites de la zona de juego (coords de canvas 1920x1080, centro) ----------
    const float AREA_X = 790f;
    const float AREA_Y_MIN = -400f;
    const float AREA_Y_MAX = 280f;

    // --- Estado ---------------------------------------------------------------------
    RectTransform   _canvasRT;
    RectTransform   _playRT;
    RectTransform   _linesRT;
    TextMeshProUGUI _timerLbl;
    TextMeshProUGUI _nextLbl;
    TextMeshProUGUI _errorsLbl;

    readonly List<Bubble> _bubbles = new List<Bubble>();
    int   _nextIndex;
    int   _errors;
    float _startTime;
    float _lastCorrectTime;
    bool  _running;

    static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

    static readonly Color BG       = new Color(0.08f, 0.10f, 0.16f);
    static readonly Color HDR      = new Color(0.05f, 0.08f, 0.15f);
    static readonly Color ACCENT   = new Color(0.98f, 0.80f, 0.10f);
    static readonly Color DIM      = new Color(0.45f, 0.58f, 0.75f);
    static readonly Color CGREEN   = new Color(0.25f, 0.90f, 0.52f);
    static readonly Color BUBBLE   = new Color(0.28f, 0.60f, 1.00f);
    static readonly Color BUBBLE_D = new Color(0.58f, 0.28f, 0.92f);   // distractor
    static readonly Color LINE_COL = new Color(0.25f, 0.90f, 0.52f, 0.85f);

    // --- MinigameBase -----------------------------------------------------------------
    protected override string GetIntroDescription()
    {
        var diff = GameManager.Instance != null
            ? GameManager.Instance.CurrentDifficulty
            : DifficultyLevel.Easy;

        switch (diff)
        {
            case DifficultyLevel.Medium:
                return "Toca las burbujas en orden: 1, 2, 3...\n" +
                       "¡Cuidado! Las burbujas con LETRAS son trampa, no las toques.\n\n" +
                       "Cuanto mas rapido y sin fallos, mas puntos.";
            case DifficultyLevel.Hard:
                return "Ahora el camino alterna numero y letra:\n" +
                       "1 - A - 2 - B - 3 - C... ¡hasta la F!\n\n" +
                       "Piensa bien cual toca antes de pulsar.";
            default:
                return "Toca las burbujas en orden: 1, 2, 3... ¡hasta el 8!\n" +
                       "Se dibujara un camino de colores detras de ti.\n\n" +
                       "Cuanto mas rapido, mas puntos.";
        }
    }

    void ApplyDifficulty()
    {
        var diff = GameManager.Instance != null
            ? GameManager.Instance.CurrentDifficulty
            : DifficultyLevel.Easy;

        switch (diff)
        {
            case DifficultyLevel.Medium:
                _sequence = new[] { "1","2","3","4","5","6","7","8","9","10","11","12" };
                _distractorCount = 4;
                _bubbleSize = 118f;
                _baseScore  = 1000;
                break;
            case DifficultyLevel.Hard:
                _sequence = new[] { "1","A","2","B","3","C","4","D","5","E","6","F" };
                _distractorCount = 0;
                _bubbleSize = 118f;
                _baseScore  = 1200;
                break;
            default:
                _sequence = new[] { "1","2","3","4","5","6","7","8" };
                _distractorCount = 0;
                _bubbleSize = 150f;
                _baseScore  = 800;
                break;
        }
    }

    protected override void OnMinigameStart()
    {
        ApplyDifficulty();
        KidUI.EnsureEventSystem();

        _nextIndex = 0;
        _errors    = 0;

        BuildUI();
        SpawnBubbles();

        _startTime       = Time.time;
        _lastCorrectTime = Time.time;
        _running         = true;
        UpdateNextLabel();
    }

    protected override void OnMinigameComplete() { }
    protected override void OnMinigameFailed()   { }

    // --- UI -------------------------------------------------------------------------------
    void BuildUI()
    {
        var cv = KidUI.MakeCanvas("Canvas_NumberPath", 5, transform);
        _canvasRT = cv.GetComponent<RectTransform>();

        KidUI.Img(_canvasRT, "BG", BG, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        KidUI.Img(_canvasRT, "GradT", C(0.24f, 0.20f, 0.06f, 0.22f),
                  new Vector2(0f, 0.70f), Vector2.one, Vector2.zero, Vector2.zero);

        // Cabecera
        var hdr = KidUI.Img(_canvasRT, "Hdr", HDR,
                            new Vector2(0f, 1f), new Vector2(1f, 1f),
                            new Vector2(0f, -44f), new Vector2(0f, 88f));
        KidUI.Img(hdr, "Line", ACCENT, new Vector2(0f, 0f), new Vector2(1f, 0f),
                  new Vector2(0f, 1.5f), new Vector2(0f, 3f));
        KidUI.Img(hdr, "AccL", ACCENT, new Vector2(0f, 0.18f), new Vector2(0f, 0.82f),
                  new Vector2(3f, 0f), new Vector2(6f, 0f));
        var ttl = KidUI.Txt(hdr, "T", "CAMINO NUMERICO", Color.white, 35,
                            new Vector2(0.03f, 0.12f), new Vector2(0.55f, 0.88f));
        ttl.fontStyle = FontStyles.Bold;
        ttl.alignment = TextAlignmentOptions.MidlineLeft;
        ttl.characterSpacing = 2f;

        // Cronometro visible (en cabecera, derecha)
        _timerLbl = KidUI.Txt(hdr, "Timer", "0.0 s", ACCENT, 34,
                              new Vector2(0.72f, 0.10f), new Vector2(0.97f, 0.90f));
        _timerLbl.fontStyle = FontStyles.Bold;
        _timerLbl.alignment = TextAlignmentOptions.MidlineRight;

        // Barra de info: siguiente objetivo + errores
        var info = KidUI.Img(_canvasRT, "Info", C(0f, 0f, 0f, 0.15f),
                             new Vector2(0f, 0.855f), new Vector2(1f, 0.915f),
                             Vector2.zero, Vector2.zero);
        _nextLbl = KidUI.Txt(info, "Next", "Busca el 1", Color.white, 26,
                             new Vector2(0.02f, 0f), new Vector2(0.55f, 1f));
        _nextLbl.alignment = TextAlignmentOptions.MidlineLeft;
        _nextLbl.fontStyle = FontStyles.Bold;
        _errorsLbl = KidUI.Txt(info, "Err", "Errores: 0", DIM, 22,
                               new Vector2(0.60f, 0f), new Vector2(0.98f, 1f));
        _errorsLbl.alignment = TextAlignmentOptions.MidlineRight;

        // Zona de juego (las lineas van en un contenedor DEBAJO de las burbujas)
        var playGO = new GameObject("PlayArea");
        playGO.transform.SetParent(_canvasRT, false);
        _playRT = playGO.AddComponent<RectTransform>();
        _playRT.anchorMin = _playRT.anchorMax = new Vector2(0.5f, 0.5f);
        _playRT.pivot = new Vector2(0.5f, 0.5f);
        _playRT.sizeDelta = Vector2.zero;
        _playRT.anchoredPosition = new Vector2(0f, -40f);

        var linesGO = new GameObject("Lines");
        linesGO.transform.SetParent(_playRT, false);
        _linesRT = linesGO.AddComponent<RectTransform>();
        _linesRT.anchorMin = _linesRT.anchorMax = new Vector2(0.5f, 0.5f);
        _linesRT.pivot = new Vector2(0.5f, 0.5f);
        _linesRT.sizeDelta = Vector2.zero;
        _linesRT.anchoredPosition = Vector2.zero;
    }

    // --- Burbujas -----------------------------------------------------------------------------
    void SpawnBubbles()
    {
        _bubbles.Clear();

        int total = _sequence.Length + _distractorCount;
        var positions = PlaceWithoutOverlap(total, _bubbleSize * 1.25f);

        // Distractores: letras que NO estan en la secuencia
        string[] distractorChars = { "S", "L", "R", "T", "M", "P" };

        for (int i = 0; i < total; i++)
        {
            bool isDistractor = i >= _sequence.Length;
            string label = isDistractor
                ? distractorChars[(i - _sequence.Length) % distractorChars.Length]
                : _sequence[i];

            var b = MakeBubble(label, positions[i],
                               isDistractor ? BUBBLE_D : BUBBLE,
                               isDistractor ? -1 : i);
            _bubbles.Add(b);
        }
    }

    List<Vector2> PlaceWithoutOverlap(int count, float minDist)
    {
        var result = new List<Vector2>();
        float dist = minDist;
        int safety = 0;

        while (result.Count < count && safety < 40)
        {
            result.Clear();
            for (int i = 0; i < count; i++)
            {
                bool placed = false;
                for (int attempt = 0; attempt < 300; attempt++)
                {
                    var p = new Vector2(Random.Range(-AREA_X, AREA_X),
                                        Random.Range(AREA_Y_MIN, AREA_Y_MAX));
                    bool ok = true;
                    for (int j = 0; j < result.Count; j++)
                        if (Vector2.Distance(p, result[j]) < dist) { ok = false; break; }
                    if (ok) { result.Add(p); placed = true; break; }
                }
                if (!placed) break;
            }
            if (result.Count < count) dist *= 0.92f;   // relajar y reintentar
            safety++;
        }
        // Relleno de emergencia (no deberia ocurrir)
        while (result.Count < count)
            result.Add(new Vector2(Random.Range(-AREA_X, AREA_X),
                                   Random.Range(AREA_Y_MIN, AREA_Y_MAX)));
        return result;
    }

    Bubble MakeBubble(string label, Vector2 pos, Color col, int order)
    {
        var go = new GameObject("Bubble_" + label);
        go.transform.SetParent(_playRT, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(_bubbleSize, _bubbleSize);
        rt.anchoredPosition = pos;

        // Halo exterior
        var halo = go.AddComponent<Image>();
        halo.color = C(col.r, col.g, col.b, 0.22f);

        // Nucleo
        var core = KidUI.Img(rt, "Core", col, Vector2.zero, Vector2.one,
                             Vector2.zero, new Vector2(-16f, -16f));
        core.GetComponent<Image>().raycastTarget = false;

        // Brillo
        KidUI.Img(core, "Shine", C(1f, 1f, 1f, 0.18f),
                  new Vector2(0.10f, 0.58f), new Vector2(0.55f, 0.90f),
                  Vector2.zero, Vector2.zero)
             .GetComponent<Image>().raycastTarget = false;

        var t = KidUI.Txt(core, "Lbl", label, Color.white, _bubbleSize * 0.42f,
                          Vector2.zero, Vector2.one);
        t.fontStyle = FontStyles.Bold;
        t.raycastTarget = false;

        var bubble = new Bubble
        {
            rt = rt, fill = core.GetComponent<Image>(), label = t, order = order
        };

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = halo;
        btn.onClick.AddListener(() => HandleTap(bubble));
        bubble.btn = btn;
        ButtonJuice.Attach(go);

        UITween.PopIn(rt, 0.4f, 0.4f, Random.Range(0f, 0.25f));
        return bubble;
    }

    // --- Interaccion -----------------------------------------------------------------------------
    void HandleTap(Bubble b)
    {
        if (!IsPlaying || !_running || b.done) return;

        if (b.order == _nextIndex)
        {
            b.done = true;
            float rtMs = (Time.time - _lastCorrectTime) * 1000f;
            _lastCorrectTime = Time.time;
            ReportEvent(true, rtMs);

            b.fill.color = CGREEN;
            var haloImg = b.rt.GetComponent<Image>();
            if (haloImg != null) haloImg.color = C(CGREEN.r, CGREEN.g, CGREEN.b, 0.25f);
            GameFeel.PlayPop();
            UITween.PulseOnce(b.rt, 1.20f, 0.25f);

            // Linea desde la burbuja anterior
            if (_nextIndex > 0)
            {
                var prev = FindByOrder(_nextIndex - 1);
                if (prev != null) DrawLine(prev.rt.anchoredPosition, b.rt.anchoredPosition);
            }

            _nextIndex++;
            if (_nextIndex >= _sequence.Length) Finish();
            else                                UpdateNextLabel();
        }
        else
        {
            _errors++;
            ReportEvent(false, -1f);
            _errorsLbl.text  = "Errores: " + _errors;
            _errorsLbl.color = new Color(0.90f, 0.28f, 0.30f);
            GameFeel.Error(b.rt);
        }
    }

    Bubble FindByOrder(int order)
    {
        for (int i = 0; i < _bubbles.Count; i++)
            if (_bubbles[i].order == order) return _bubbles[i];
        return null;
    }

    void DrawLine(Vector2 from, Vector2 to)
    {
        var go = new GameObject("Line");
        go.transform.SetParent(_linesRT, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);

        Vector2 delta = to - from;
        rt.sizeDelta        = new Vector2(delta.magnitude, 10f);
        rt.anchoredPosition = (from + to) * 0.5f;
        rt.localRotation    = Quaternion.Euler(0f, 0f,
            Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);

        var img = go.AddComponent<Image>();
        img.color = LINE_COL;
        img.raycastTarget = false;

        UITween.PopIn(rt, 0.25f, 0.3f);
    }

    void UpdateNextLabel()
    {
        if (_nextIndex < _sequence.Length)
            _nextLbl.text = "Busca: " + _sequence[_nextIndex];
    }

    void Update()
    {
        if (!IsPlaying || !_running || _timerLbl == null) return;
        _timerLbl.text = (Time.time - _startTime).ToString("0.0") + " s";
    }

    // --- Fin ---------------------------------------------------------------------------------------
    void Finish()
    {
        _running = false;
        float elapsed = Time.time - _startTime;

        int score = _baseScore
                    - Mathf.RoundToInt(elapsed * 10f)
                    - _errors * 40;
        score = Mathf.Max(60, score);

        int   correct = _sequence.Length;
        float ratio   = (float)correct / (correct + _errors);
        int   stars   = GameFeel.StarsFromRatio(true, ratio);

        _nextLbl.text = "¡Camino completado!";
        GameFeel.Confetti(30);
        GameFeel.PlaySuccess();

        CompleteMinigame(score);
        ShowResults(true, stars, score,
            new[]
            {
                "Tiempo: " + elapsed.ToString("0.0") + " s",
                "Errores: " + _errors
            },
            null,
            _errors == 0 ? "¡Camino perfecto, sin fallos!" : "¡Camino completado!");
    }
}
