// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class MemoryQuestion
{
    public string questionTitle;
    public string[] options = new string[4];
    public int correctIndex;
}

/// <summary>
/// "Detective de emociones" (Gestion emocional): reconocimiento emocional.
/// Una gran cara de robot (EmotionFaceArt) muestra una emocion con una
/// mini-situacion de una linea; abajo, 3-4 botones grandes con nombres de
/// emociones y el nino toca la correcta. 8 rondas, sin fracaso duro:
/// exito si acierta al menos la mitad.
/// UI 100% por codigo sobre fondo espacial opaco (tapa la UI vieja de la escena).
/// </summary>
public class MemorySelector : MinigameBase
{
    // ---------- Campos serializados LEGACY (se conservan para no romper la escena) ----------
    [Header("Preguntas y opciones (legacy, sin uso)")]
    public List<MemoryQuestion> questions;

    public TextMeshProUGUI questionTitleText;
    public List<Button> optionButtons;
    public TextMeshProUGUI[] optionTexts;

    [Header("Controlador de salto (legacy, sin uso)")]
    public CharacterJumper characterJumper;

    // ---------- Configuracion por dificultad ----------
    const int ROUNDS = 8;
    int   _optionCount = 3;
    bool  _useMatices  = false;
    float _timeLimit   = 0f;      // 0 = sin tiempo (barra solo en dificil)

    // Paleta de Gestion emocional (verde)
    static readonly Color VERDE = new Color(0.18f, 0.80f, 0.58f);

    // ---------- UI ----------
    RectTransform   _root;
    RobotFace       _face;
    TextMeshProUGUI _situLbl, _feedLbl, _roundHdrLbl;
    RectTransform   _situChip;
    RectTransform   _optionsRow;
    Image[]         _roundDots;
    Image           _timerFill;
    RectTransform   _timerPanel;

    // ---------- Estado ----------
    int          _round;
    int          _correct;
    int          _score;
    bool         _busy;
    float        _shownAt;
    float        _rtSum;
    int          _rtCount;
    RobotEmotion _current;
    RobotEmotion _last = (RobotEmotion)(-1);
    Coroutine    _timerCo;
    readonly List<Button> _liveButtons = new List<Button>();

    protected override void Start()
    {
        // Debe coincidir EXACTAMENTE con GameCatalog.
        minigameName = "Detective de emociones";
        category     = MinigameCategory.EmotionalManagement;
        base.Start();
    }

    protected override string GetIntroDescription() =>
        "Robi el robot siente algo... ¡Sé su detective!\n" +
        "Mira su cara, lee la pista y toca la emoción correcta.";

    void ApplyDifficulty()
    {
        var diff = GameManager.Instance != null
            ? GameManager.Instance.CurrentDifficulty
            : DifficultyLevel.Easy;
        switch (diff)
        {
            case DifficultyLevel.Medium:
                _optionCount = 4; _useMatices = false; _timeLimit = 0f;
                break;
            case DifficultyLevel.Hard:
                _optionCount = 4; _useMatices = true;  _timeLimit = 9f;
                break;
            default:
                _optionCount = 3; _useMatices = false; _timeLimit = 0f;
                break;
        }
    }

    protected override void OnMinigameStart()
    {
        ApplyDifficulty();
        KidUI.EnsureEventSystem();

        _round   = 0;
        _correct = 0;
        _score   = 0;
        _rtSum   = 0f;
        _rtCount = 0;
        _busy    = false;

        BuildUI();
        NextRound();
    }

    protected override void OnMinigameComplete() { }
    protected override void OnMinigameFailed()   { }

    // ================================================================ UI

    void BuildUI()
    {
        Canvas cv = KidUI.MakeCanvas("DetectiveCanvas", 50, transform);
        _root = cv.GetComponent<RectTransform>();
        KidUI.BuildSpaceBackground(_root);

        // ---- cabecera flotante redondeada ----
        var hdr = KidUI.RoundImg(_root, "Hdr", KidUI.PANEL,
            new Vector2(0.02f, 0.905f), new Vector2(0.98f, 0.985f),
            Vector2.zero, Vector2.zero, 1.4f);
        var hl = KidUI.RoundImg(hdr, "HL", VERDE,
            new Vector2(0.02f, 0f), new Vector2(0.98f, 0f),
            new Vector2(0f, 2f), new Vector2(0f, 4f), 4f);
        hl.GetComponent<Image>().raycastTarget = false;

        var ttl = KidUI.Txt(hdr, "T", "DETECTIVE DE EMOCIONES", Color.white, 34,
                            new Vector2(0.03f, 0f), new Vector2(0.58f, 1f));
        ttl.fontStyle = FontStyles.Bold;
        ttl.alignment = TextAlignmentOptions.MidlineLeft;

        var cat = KidUI.Txt(hdr, "Cat", "GESTION EMOCIONAL", VERDE, 18,
                            new Vector2(0.58f, 0f), new Vector2(0.82f, 1f));
        cat.alignment = TextAlignmentOptions.MidlineRight;

        _roundHdrLbl = KidUI.Txt(hdr, "R", "Ronda 1 de " + ROUNDS, KidUI.DIM, 22,
                                 new Vector2(0.82f, 0f), new Vector2(0.98f, 1f));
        _roundHdrLbl.fontStyle = FontStyles.Bold;
        _roundHdrLbl.alignment = TextAlignmentOptions.MidlineRight;
        UITween.PopIn(hdr, 0.45f, 0.90f);

        // ---- puntos de ronda ----
        _roundDots = new Image[ROUNDS];
        float spacing = 38f, startX = -(ROUNDS - 1) * 19f;
        var dotsGO = new GameObject("Dots");
        dotsGO.transform.SetParent(_root, false);
        var dotsRT = dotsGO.AddComponent<RectTransform>();
        dotsRT.anchorMin = dotsRT.anchorMax = new Vector2(0.5f, 0.878f);
        dotsRT.pivot = new Vector2(0.5f, 0.5f);
        dotsRT.sizeDelta = Vector2.zero;
        for (int i = 0; i < ROUNDS; i++)
        {
            var d = KidUI.CircleAt(dotsRT, "D" + i, new Color(1f, 1f, 1f, 0.18f),
                                   new Vector2(0.5f, 0.5f), 20f);
            d.anchoredPosition = new Vector2(startX + i * spacing, 0f);
            d.GetComponent<Image>().raycastTarget = false;
            _roundDots[i] = d.GetComponent<Image>();
        }

        // ---- barra de tiempo suave (solo dificil) ----
        if (_timeLimit > 0f)
        {
            _timerPanel = KidUI.RoundImg(_root, "TimerBG", new Color(0.02f, 0.05f, 0.10f, 0.9f),
                new Vector2(0.32f, 0.833f), new Vector2(0.68f, 0.852f),
                Vector2.zero, Vector2.zero, 3f);
            _timerPanel.GetComponent<Image>().raycastTarget = false;
            var fGO = new GameObject("Fill");
            fGO.transform.SetParent(_timerPanel, false);
            var fRT = fGO.AddComponent<RectTransform>();
            fRT.anchorMin = Vector2.zero; fRT.anchorMax = Vector2.one;
            fRT.sizeDelta = new Vector2(-4f, -4f); fRT.anchoredPosition = Vector2.zero;
            _timerFill = fGO.AddComponent<Image>();
            _timerFill.sprite        = KidUI.RoundedSprite;
            _timerFill.type          = Image.Type.Filled;
            _timerFill.fillMethod    = Image.FillMethod.Horizontal;
            _timerFill.fillOrigin    = 0;
            _timerFill.fillAmount    = 1f;
            _timerFill.color         = VERDE;
            _timerFill.raycastTarget = false;
        }

        // ---- gran cara de robot ----
        _face = EmotionFaceArt.Build(_root, new Vector2(0.5f, 0.60f), 300f);

        // ---- pregunta + mini-situacion ----
        var q = KidUI.Txt(_root, "Q", "¿Cómo se siente Robi?", Color.white, 34,
                          new Vector2(0.15f, 0.395f), new Vector2(0.85f, 0.445f));
        q.fontStyle = FontStyles.Bold;

        _situChip = KidUI.RoundImg(_root, "SituChip", KidUI.PANEL2,
            new Vector2(0.20f, 0.325f), new Vector2(0.80f, 0.385f),
            Vector2.zero, Vector2.zero, 1.6f);
        _situChip.GetComponent<Image>().raycastTarget = false;
        _situLbl = KidUI.Txt(_situChip, "Situ", "", KidUI.DIM, 25,
                             new Vector2(0.02f, 0f), new Vector2(0.98f, 1f));
        _situLbl.enableAutoSizing = true;
        _situLbl.fontSizeMin = 16f; _situLbl.fontSizeMax = 25f;

        // ---- fila de botones de emocion ----
        var rowGO = new GameObject("OptionsRow");
        rowGO.transform.SetParent(_root, false);
        _optionsRow = rowGO.AddComponent<RectTransform>();
        _optionsRow.anchorMin = new Vector2(0.04f, 0.115f);
        _optionsRow.anchorMax = new Vector2(0.96f, 0.295f);
        _optionsRow.sizeDelta = Vector2.zero;
        _optionsRow.anchoredPosition = Vector2.zero;

        // ---- feedback ----
        _feedLbl = KidUI.Txt(_root, "Feed", "", VERDE, 28,
                             new Vector2(0.10f, 0.03f), new Vector2(0.90f, 0.09f));
        _feedLbl.fontStyle = FontStyles.Bold;
    }

    // ================================================================ RONDAS

    void NextRound()
    {
        if (!IsPlaying) return;
        if (_round >= ROUNDS) { FinishGame(); return; }

        _busy = false;
        _roundHdrLbl.text = "Ronda " + (_round + 1) + " de " + ROUNDS;
        _feedLbl.text = "";

        // Emocion objetivo (sin repetir la anterior)
        var pool = BuildPool();
        do { _current = pool[Random.Range(0, pool.Count)]; }
        while (_current == _last && pool.Count > 1);
        _last = _current;

        _face.SetEmotion(_current, Random.Range(0.7f, 1f));
        _face.Pulse();

        _situLbl.text = Situacion(_current);
        UITween.PopIn(_situChip, 0.30f, 0.92f);

        BuildOptionButtons(pool);

        _shownAt = Time.realtimeSinceStartup;
        if (_timeLimit > 0f)
        {
            if (_timerCo != null) StopCoroutine(_timerCo);
            _timerCo = StartCoroutine(TimerRoutine());
        }
    }

    List<RobotEmotion> BuildPool()
    {
        var pool = new List<RobotEmotion>(EmotionFaceArt.BASICAS);
        if (_useMatices) pool.AddRange(EmotionFaceArt.MATICES);
        return pool;
    }

    void BuildOptionButtons(List<RobotEmotion> pool)
    {
        foreach (Transform ch in _optionsRow) Destroy(ch.gameObject);
        _liveButtons.Clear();

        // Opciones: la correcta + distractores distintos
        var opts = new List<RobotEmotion> { _current };
        var distract = new List<RobotEmotion>(pool);
        distract.Remove(_current);
        while (opts.Count < _optionCount && distract.Count > 0)
        {
            int r = Random.Range(0, distract.Count);
            opts.Add(distract[r]);
            distract.RemoveAt(r);
        }
        // Barajar
        for (int i = opts.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (opts[i], opts[j]) = (opts[j], opts[i]);
        }

        int   n     = opts.Count;
        float gap   = 0.025f;
        float w     = (1f - gap * (n - 1)) / n;
        for (int i = 0; i < n; i++)
        {
            var emo  = opts[i];
            float x0 = i * (w + gap);
            var btn  = KidUI.Btn(_optionsRow, EmotionFaceArt.Nombre(emo), KidUI.BTNC,
                                 new Vector2(x0, 0f), new Vector2(x0 + w, 1f),
                                 () => OnPick(emo), 32f);
            UITween.PopIn((RectTransform)btn.transform, 0.32f, 0.85f, i * 0.05f);
            _liveButtons.Add(btn);
        }
    }

    IEnumerator TimerRoutine()
    {
        float t = 0f;
        while (t < _timeLimit)
        {
            if (_busy || !IsPlaying) yield break;
            t += Time.deltaTime;
            float frac = 1f - Mathf.Clamp01(t / _timeLimit);
            if (_timerFill != null)
            {
                _timerFill.fillAmount = frac;
                _timerFill.color = Color.Lerp(KidUI.WARN, VERDE, frac);
            }
            yield return null;
        }
        if (!_busy && IsPlaying) OnTimeout();
    }

    void OnPick(RobotEmotion picked)
    {
        if (!IsPlaying || _busy) return;
        _busy = true;

        float rtMs = (Time.realtimeSinceStartup - _shownAt) * 1000f;
        bool  ok   = picked == _current;
        ReportEvent(ok, rtMs);

        RectTransform btnRT = null;
        foreach (var b in _liveButtons)
        {
            var lbl = b.GetComponentInChildren<TextMeshProUGUI>();
            if (lbl != null && lbl.text == EmotionFaceArt.Nombre(picked))
                btnRT = (RectTransform)b.transform;
            b.interactable = false;
        }

        if (ok)
        {
            _correct++;
            _score += 100;
            _rtSum += rtMs;
            _rtCount++;
            _roundDots[_round].color = VERDE;
            UITween.PulseOnce(_roundDots[_round].rectTransform, 1.4f, 0.28f);
            GameFeel.Success(btnRT);
            GameFeel.FloatingText("¡Detective genial! +100", VERDE, new Vector2(0f, 150f), 42f);
            _face.Pulse();
            _feedLbl.text  = "¡Sí! Robi siente " + EmotionFaceArt.Nombre(_current).ToLower() + ".";
            _feedLbl.color = VERDE;
        }
        else
        {
            _roundDots[_round].color = KidUI.WARN;
            GameFeel.PlayError();
            if (btnRT != null) GameFeel.Shake(btnRT, 10f, 0.3f);
            _feedLbl.text  = "Casi... era " + EmotionFaceArt.Nombre(_current).ToLower() +
                             ". ¡Fíjate en sus cejas y su boca!";
            _feedLbl.color = KidUI.WARN;
        }

        _round++;
        StartCoroutine(NextRoundDelayed(ok ? 1.0f : 1.5f));
    }

    void OnTimeout()
    {
        if (!IsPlaying || _busy) return;
        _busy = true;

        ReportEvent(false, _timeLimit * 1000f);
        foreach (var b in _liveButtons) b.interactable = false;

        _roundDots[_round].color = KidUI.WARN;
        GameFeel.PlayPop();
        _feedLbl.text  = "Se acabó el tiempo. Era " +
                         EmotionFaceArt.Nombre(_current).ToLower() + ". ¡A por la siguiente!";
        _feedLbl.color = KidUI.WARN;

        _round++;
        StartCoroutine(NextRoundDelayed(1.5f));
    }

    IEnumerator NextRoundDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);
        NextRound();
    }

    // ================================================================ FINAL

    void FinishGame()
    {
        bool won = _correct >= ROUNDS / 2;   // exito si acierta al menos la mitad
        float ratio = (float)_correct / ROUNDS;
        int   avgMs = _rtCount > 0 ? Mathf.RoundToInt(_rtSum / _rtCount) : 0;

        if (won)
        {
            CompleteMinigame(_score);
            GameFeel.Confetti();
        }
        else
        {
            FailMinigame();
        }

        string rtStat = _rtCount > 0
            ? "Rapidez media: " + (avgMs / 1000f).ToString("0.0") + " s"
            : "Rapidez media: -";

        ShowResults(
            won,
            GameFeel.StarsFromRatio(won, ratio),
            _score,
            new[]
            {
                "Emociones descubiertas: " + _correct + " de " + ROUNDS,
                rtStat
            },
            won ? "¡Caso resuelto, detective!" : "¡Casi lo resuelves!",
            won ? "Leer las caras te ayuda a entender a los demás."
                : "Cada cara da pistas: mira los ojos, las cejas y la boca.");
    }

    // ================================================================ DATOS

    /// <summary>Mini-situacion de una linea para dar contexto a la emocion.</summary>
    static string Situacion(RobotEmotion e)
    {
        string[] opts;
        switch (e)
        {
            case RobotEmotion.Alegria:
                opts = new[] { "¡Le han regalado un cohete nuevo!",
                               "Hoy juega con sus mejores amigos." }; break;
            case RobotEmotion.Tristeza:
                opts = new[] { "Se le ha perdido su peluche favorito.",
                               "Su mejor amigo se muda a otro planeta." }; break;
            case RobotEmotion.Enfado:
                opts = new[] { "Le han quitado su turno en el juego.",
                               "Alguien rompió su torre sin pedir perdón." }; break;
            case RobotEmotion.Miedo:
                opts = new[] { "Ha oído un ruido raro en la oscuridad.",
                               "Se ha perdido en un sitio nuevo." }; break;
            case RobotEmotion.Calma:
                opts = new[] { "Está descansando después de jugar.",
                               "Escucha su música tranquila favorita." }; break;
            case RobotEmotion.Sorpresa:
                opts = new[] { "¡Ha encontrado un regalo inesperado!",
                               "Sus amigos le han preparado una fiesta." }; break;
            case RobotEmotion.Frustracion:
                opts = new[] { "Lleva mil intentos y el puzle no le sale.",
                               "Su dibujo no queda como él quería." }; break;
            case RobotEmotion.Nervios:
                opts = new[] { "Mañana actúa delante de toda la clase.",
                               "Está esperando una noticia importante." }; break;
            case RobotEmotion.Verguenza:
                opts = new[] { "Se ha tropezado delante de todos.",
                               "Le han pedido cantar en público." }; break;
            default: // Orgullo
                opts = new[] { "¡Ha montado en bici sin ayuda por primera vez!",
                               "Terminó su primer libro él solito." }; break;
        }
        return opts[Random.Range(0, opts.Length)];
    }
}
