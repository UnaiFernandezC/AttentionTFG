// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Minijuego de atencion "Algo no cuadra" (odd-one-out).
/// Aparece una rejilla de fichas construida 100% por codigo: todas iguales
/// excepto UNA. El nino debe tocar la diferente.
///
/// Progresion de dificultad:
///  - Facil : rejilla 2x3, diferencia de COLOR evidente, 6 rondas, sin tiempo.
///  - Medio : rejilla 3x4, diferencia de LETRA/NUMERO parecidos (E/F, O/Q, 6/8),
///            8 rondas, 12 s por ronda.
///  - Dificil: rejilla 4x4, LETRAS ESPEJO (b/d, p/q, u/n) - relevante para la
///            confusion lectora -, 10 rondas, 8 s por ronda.
/// </summary>
public class AlgoNoCuadraGameManager : MinigameBase
{
    enum Mode { Color, Letter, Mirror }

    // --- Config (se fija en ApplyDifficulty) -----------------------------------
    int   _rows        = 2;
    int   _cols        = 3;
    int   _totalRounds = 6;
    float _roundTime   = 0f;      // 0 = sin limite de tiempo
    Mode  _mode        = Mode.Color;

    // --- UI ---------------------------------------------------------------------
    RectTransform   _canvasRT;
    RectTransform   _gridRT;
    RectTransform   _timerBarBg;
    RectTransform   _timerFillRT;
    Image           _timerFillImg;
    TextMeshProUGUI _roundLbl;
    TextMeshProUGUI _scoreLbl;
    TextMeshProUGUI _statusLbl;

    // --- Estado -------------------------------------------------------------------
    RectTransform[] _tiles;
    int   _oddIndex = -1;
    int   _round;
    int   _correctRounds;
    int   _score;
    float _roundStart;
    float _timeLeft;
    bool  _inputOpen;
    float _rtSumMs;
    int   _rtCount;

    static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

    static readonly Color BG      = new Color(0.08f, 0.10f, 0.16f);
    static readonly Color HDR     = new Color(0.05f, 0.08f, 0.15f);
    static readonly Color ACCENT  = new Color(0.98f, 0.80f, 0.10f);   // amarillo Atencion
    static readonly Color DIM     = new Color(0.45f, 0.58f, 0.75f);
    static readonly Color CGREEN  = new Color(0.25f, 0.90f, 0.52f);
    static readonly Color CRED    = new Color(0.90f, 0.28f, 0.30f);

    // Colores alegres para las fichas (modo COLOR)
    static readonly Color[] PALETTE =
    {
        new Color(0.28f, 0.60f, 1.00f),
        new Color(0.18f, 0.80f, 0.58f),
        new Color(0.98f, 0.80f, 0.10f),
        new Color(0.95f, 0.55f, 0.12f),
        new Color(0.58f, 0.28f, 0.92f),
        new Color(0.92f, 0.35f, 0.55f)
    };

    // Pares parecidos (letra base / letra diferente)
    static readonly string[][] LETTER_PAIRS =
    {
        new[] { "E", "F" }, new[] { "F", "E" },
        new[] { "O", "Q" }, new[] { "Q", "O" },
        new[] { "6", "8" }, new[] { "8", "6" }
    };

    // Letras espejo (confusion lectora tipica)
    static readonly string[][] MIRROR_PAIRS =
    {
        new[] { "b", "d" }, new[] { "d", "b" },
        new[] { "p", "q" }, new[] { "q", "p" },
        new[] { "u", "n" }, new[] { "n", "u" }
    };

    // --- MinigameBase --------------------------------------------------------------
    protected override string GetIntroDescription()
    {
        var diff = GameManager.Instance != null
            ? GameManager.Instance.CurrentDifficulty
            : DifficultyLevel.Easy;

        switch (diff)
        {
            case DifficultyLevel.Medium:
                return "Todas las fichas son iguales... ¡menos UNA!\n" +
                       "Mira bien las letras y los numeros y toca la diferente.\n\n" +
                       "Tienes 12 segundos por ronda. ¡Ojos de lince!";
            case DifficultyLevel.Hard:
                return "Todas las fichas son iguales... ¡menos UNA!\n" +
                       "Las letras estan casi iguales (b y d se parecen mucho).\n\n" +
                       "Miralas despacio y toca la diferente. 8 segundos por ronda.";
            default:
                return "Todas las fichas son iguales... ¡menos UNA!\n" +
                       "Busca la ficha de otro color y tocala.\n\n" +
                       "Sin prisa: lo importante es fijarse bien.";
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
                _rows = 3; _cols = 4; _totalRounds = 8;
                _roundTime = 12f; _mode = Mode.Letter;
                break;
            case DifficultyLevel.Hard:
                _rows = 4; _cols = 4; _totalRounds = 10;
                _roundTime = 8f; _mode = Mode.Mirror;
                break;
            default:
                _rows = 2; _cols = 3; _totalRounds = 6;
                _roundTime = 0f; _mode = Mode.Color;
                break;
        }
    }

    protected override void OnMinigameStart()
    {
        ApplyDifficulty();
        KidUI.EnsureEventSystem();

        _round         = 0;
        _correctRounds = 0;
        _score         = 0;
        _rtSumMs       = 0f;
        _rtCount       = 0;

        BuildUI();
        BuildRound();
    }

    protected override void OnMinigameComplete() { }
    protected override void OnMinigameFailed()   { }

    // --- Construccion de UI ----------------------------------------------------------
    void BuildUI()
    {
        var cv = KidUI.MakeCanvas("Canvas_AlgoNoCuadra", 5, transform);
        _canvasRT = cv.GetComponent<RectTransform>();

        KidUI.Img(_canvasRT, "BG", BG, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        KidUI.Img(_canvasRT, "GradT", C(0.24f, 0.20f, 0.06f, 0.25f),
                  new Vector2(0f, 0.70f), Vector2.one, Vector2.zero, Vector2.zero);

        // Cabecera
        var hdr = KidUI.Img(_canvasRT, "Hdr", HDR,
                            new Vector2(0f, 1f), new Vector2(1f, 1f),
                            new Vector2(0f, -44f), new Vector2(0f, 88f));
        KidUI.Img(hdr, "Line", ACCENT, new Vector2(0f, 0f), new Vector2(1f, 0f),
                  new Vector2(0f, 1.5f), new Vector2(0f, 3f));
        KidUI.Img(hdr, "AccL", ACCENT, new Vector2(0f, 0.18f), new Vector2(0f, 0.82f),
                  new Vector2(3f, 0f), new Vector2(6f, 0f));
        var ttl = KidUI.Txt(hdr, "T", "ALGO NO CUADRA", Color.white, 35,
                            new Vector2(0.03f, 0.12f), new Vector2(0.60f, 0.88f));
        ttl.fontStyle = FontStyles.Bold;
        ttl.alignment = TextAlignmentOptions.MidlineLeft;
        ttl.characterSpacing = 2f;
        var cat = KidUI.Txt(hdr, "Cat", "ATENCION", DIM, 16,
                            new Vector2(0.60f, 0.12f), new Vector2(0.97f, 0.88f));
        cat.alignment = TextAlignmentOptions.MidlineRight;

        // Barra de info: ronda + puntos
        var info = KidUI.Img(_canvasRT, "Info", C(0f, 0f, 0f, 0.15f),
                             new Vector2(0f, 0.855f), new Vector2(1f, 0.915f),
                             Vector2.zero, Vector2.zero);
        _roundLbl = KidUI.Txt(info, "Round", "Ronda 1 de " + _totalRounds, Color.white, 24,
                              new Vector2(0.02f, 0f), new Vector2(0.40f, 1f));
        _roundLbl.alignment = TextAlignmentOptions.MidlineLeft;
        _roundLbl.fontStyle = FontStyles.Bold;
        _scoreLbl = KidUI.Txt(info, "Score", "0 pts", ACCENT, 24,
                              new Vector2(0.60f, 0f), new Vector2(0.98f, 1f));
        _scoreLbl.alignment = TextAlignmentOptions.MidlineRight;
        _scoreLbl.fontStyle = FontStyles.Bold;

        // Barra de tiempo (solo Medio/Dificil)
        _timerBarBg = KidUI.Img(_canvasRT, "TimerBg", C(0.04f, 0.07f, 0.14f),
                                new Vector2(0f, 0.815f), new Vector2(1f, 0.852f),
                                Vector2.zero, Vector2.zero);
        var fillGO = new GameObject("TimerFill");
        fillGO.transform.SetParent(_timerBarBg, false);
        _timerFillRT = fillGO.AddComponent<RectTransform>();
        _timerFillRT.anchorMin = Vector2.zero;
        _timerFillRT.anchorMax = Vector2.one;
        _timerFillRT.sizeDelta = Vector2.zero;
        _timerFillRT.anchoredPosition = Vector2.zero;
        _timerFillImg = fillGO.AddComponent<Image>();
        _timerFillImg.color = ACCENT;
        _timerFillImg.raycastTarget = false;
        _timerBarBg.gameObject.SetActive(_roundTime > 0f);

        // Zona de estado (mensajes cortos)
        _statusLbl = KidUI.Txt(_canvasRT, "Status", "¿Cual es diferente?", DIM, 28,
                               new Vector2(0.10f, 0.03f), new Vector2(0.90f, 0.10f));
        _statusLbl.fontStyle = FontStyles.Bold;

        // Contenedor de la rejilla
        var gridGO = new GameObject("Grid");
        gridGO.transform.SetParent(_canvasRT, false);
        _gridRT = gridGO.AddComponent<RectTransform>();
        _gridRT.anchorMin = _gridRT.anchorMax = new Vector2(0.5f, 0.5f);
        _gridRT.pivot = new Vector2(0.5f, 0.5f);
        _gridRT.sizeDelta = Vector2.zero;
        _gridRT.anchoredPosition = new Vector2(0f, -50f);
    }

    // --- Rondas ------------------------------------------------------------------------
    void BuildRound()
    {
        // Limpiar fichas anteriores
        for (int i = _gridRT.childCount - 1; i >= 0; i--)
            Destroy(_gridRT.GetChild(i).gameObject);

        int count = _rows * _cols;
        _tiles    = new RectTransform[count];
        _oddIndex = Random.Range(0, count);

        // Contenido de la ronda
        Color  baseCol  = PALETTE[Random.Range(0, PALETTE.Length)];
        Color  oddCol   = baseCol;
        string baseChar = "";
        string oddChar  = "";

        if (_mode == Mode.Color)
        {
            int oddPick;
            do { oddPick = Random.Range(0, PALETTE.Length); }
            while (PALETTE[oddPick] == baseCol);
            oddCol = PALETTE[oddPick];
        }
        else
        {
            var pairs = _mode == Mode.Letter ? LETTER_PAIRS : MIRROR_PAIRS;
            var pair  = pairs[Random.Range(0, pairs.Length)];
            baseChar  = pair[0];
            oddChar   = pair[1];
            // Fondo comun oscuro para que destaque la letra
            baseCol   = new Color(0.13f, 0.18f, 0.32f);
            oddCol    = baseCol;
        }

        // Geometria de la rejilla
        float gap      = 18f;
        float maxW     = 1250f, maxH = 640f;
        float tileSize = Mathf.Min(230f,
                                   (maxW - (_cols - 1) * gap) / _cols,
                                   (maxH - (_rows - 1) * gap) / _rows);
        float totalW = _cols * (tileSize + gap) - gap;
        float totalH = _rows * (tileSize + gap) - gap;

        for (int i = 0; i < count; i++)
        {
            int r = i / _cols, c = i % _cols;
            float px = c * (tileSize + gap) - totalW * 0.5f + tileSize * 0.5f;
            float py = -(r * (tileSize + gap) - totalH * 0.5f + tileSize * 0.5f);

            bool  isOdd = i == _oddIndex;
            Color col   = isOdd ? oddCol : baseCol;

            var tileGO = new GameObject("Tile" + i);
            tileGO.transform.SetParent(_gridRT, false);
            var rt = tileGO.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(tileSize, tileSize);
            rt.anchoredPosition = new Vector2(px, py);
            var borderImg = tileGO.AddComponent<Image>();
            borderImg.color = C(1f, 1f, 1f, 0.10f);

            var fill = KidUI.Img(rt, "Fill", col, Vector2.zero, Vector2.one,
                                 Vector2.zero, new Vector2(-8f, -8f));
            fill.GetComponent<Image>().raycastTarget = false;

            // Brillo superior suave
            KidUI.Img(fill, "Shine", C(1f, 1f, 1f, 0.10f),
                      new Vector2(0f, 0.55f), Vector2.one, Vector2.zero, Vector2.zero)
                 .GetComponent<Image>().raycastTarget = false;

            if (_mode != Mode.Color)
            {
                var t = KidUI.Txt(fill, "Char", isOdd ? oddChar : baseChar,
                                  Color.white, tileSize * 0.58f, Vector2.zero, Vector2.one);
                t.fontStyle = FontStyles.Bold;
                t.raycastTarget = false;
            }

            int idx = i;
            var btn = tileGO.AddComponent<Button>();
            btn.targetGraphic = borderImg;
            btn.onClick.AddListener(() => HandleTileTap(idx));
            ButtonJuice.Attach(tileGO);

            UITween.PopIn(rt, 0.35f, 0.6f, i * 0.03f);
            _tiles[i] = rt;
        }

        _roundLbl.text = "Ronda " + (_round + 1) + " de " + _totalRounds;
        _statusLbl.text  = "¿Cual es diferente?";
        _statusLbl.color = DIM;

        _timeLeft   = _roundTime;
        _roundStart = Time.time;
        _inputOpen  = true;
        SetTimerFill(1f);
    }

    void Update()
    {
        if (!IsPlaying || !_inputOpen || _roundTime <= 0f) return;

        _timeLeft -= Time.deltaTime;
        SetTimerFill(_timeLeft / _roundTime);

        if (_timeLeft <= 0f)
        {
            _inputOpen = false;
            ReportEvent(false, -1f);
            _statusLbl.text  = "¡Se acabo el tiempo!";
            _statusLbl.color = CRED;
            GameFeel.PlayError();
            StartCoroutine(RevealAndNext(false));
        }
    }

    void SetTimerFill(float t)
    {
        t = Mathf.Clamp01(t);
        if (_timerFillRT == null) return;
        var am = _timerFillRT.anchorMax;
        am.x = t;
        _timerFillRT.anchorMax = am;
        _timerFillImg.color = Color.Lerp(CRED, ACCENT, t);
    }

    void HandleTileTap(int idx)
    {
        if (!IsPlaying || !_inputOpen) return;
        _inputOpen = false;

        float rtMs = (Time.time - _roundStart) * 1000f;
        bool  ok   = idx == _oddIndex;
        ReportEvent(ok, rtMs);

        if (ok)
        {
            _correctRounds++;
            _rtSumMs += rtMs;
            _rtCount++;

            int speedBonus = _roundTime > 0f
                ? Mathf.RoundToInt(50f * Mathf.Clamp01(_timeLeft / _roundTime))
                : Mathf.Clamp(Mathf.RoundToInt((4000f - rtMs) / 80f), 0, 50);
            int pts = 100 + speedBonus;
            _score += pts;
            _scoreLbl.text = _score + " pts";

            GameFeel.Success(_tiles[idx]);
            UITween.PulseOnce(_tiles[idx], 1.30f, 0.35f);
            GameFeel.FloatingText("+" + pts, CGREEN);
            _statusLbl.text  = "¡Muy bien!";
            _statusLbl.color = CGREEN;
            StartCoroutine(RevealAndNext(true));
        }
        else
        {
            GameFeel.Error(_tiles[idx]);
            _statusLbl.text  = "Esa no era... ¡mira la que brilla!";
            _statusLbl.color = CRED;
            StartCoroutine(RevealAndNext(false));
        }
    }

    IEnumerator RevealAndNext(bool wasCorrect)
    {
        if (!wasCorrect && _oddIndex >= 0 && _oddIndex < _tiles.Length && _tiles[_oddIndex] != null)
        {
            // Resaltar la correcta 1 segundo
            var border = _tiles[_oddIndex].GetComponent<Image>();
            if (border != null) border.color = ACCENT;
            UITween.PulseOnce(_tiles[_oddIndex], 1.25f, 0.5f);
            yield return new WaitForSeconds(1.0f);
        }
        else
        {
            yield return new WaitForSeconds(0.7f);
        }

        _round++;
        if (_round >= _totalRounds) EndGame();
        else                        BuildRound();
    }

    void EndGame()
    {
        float ratio   = (float)_correctRounds / _totalRounds;
        bool  success = ratio >= 0.6f;
        int   stars   = GameFeel.StarsFromRatio(success, ratio);

        if (success) CompleteMinigame(_score);
        else         FailMinigame();

        string rtStat = _rtCount > 0
            ? "Reaccion media: " + Mathf.RoundToInt(_rtSumMs / _rtCount) + " ms"
            : "Reaccion media: -";

        ShowResults(success, stars, _score,
            new[] { "Aciertos: " + _correctRounds + "/" + _totalRounds, rtStat },
            null,
            success ? "¡Tienes ojos de lince!" : "Fijate despacio en cada ficha");
    }
}
