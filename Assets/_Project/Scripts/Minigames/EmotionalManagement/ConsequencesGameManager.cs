// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// RESCATE EMOCIONAL — Gestión emocional (sustituye al antiguo "Consecuencias").
/// Un robot amigo llega con una emoción muy intensa y una situación cotidiana.
/// El niño elige, entre varias tarjetas, la estrategia que DE VERDAD le ayuda
/// a calmarse (respirar hondo, pedir ayuda, contar hasta 10...). Si acierta,
/// ve al robot calmarse delante de él: la recompensa es la regulación misma.
/// Estrategias reales y aplicables fuera de la pantalla.
///
/// La clase conserva su nombre y sus campos serializados para no romper las
/// escenas; toda la UI se construye por código (la vieja queda tapada).
/// </summary>
public class ConsequencesGameManager : MinigameBase
{
    // ---- Campos legacy serializados en las escenas (se conservan; los usa
    // ---- también el juego nuevo con el mismo significado)
    [Header("Cuantas situaciones se juegan por partida")]
    public int situationCount = 5;

    [Header("Decisiones adecuadas necesarias para ganar")]
    public int roundsToWin = 3;

    [Header("Puntuacion por tipo de respuesta")]
    public int pointsPositive = 20;   // ayudó a la primera
    public int pointsNeutral  = 8;    // ayudó a la segunda
    public int pointsNegative = 0;

    // ------------------------------------------------ contenido

    class Situacion
    {
        public RobotEmotion emo;
        public string texto;       // qué le ha pasado al robot
        public string correcta;    // estrategia que ayuda
        public string[] malas;     // estrategias que no ayudan
        public bool matiz;         // solo aparece en Difícil

        public Situacion(RobotEmotion e, string t, string ok, string[] mal, bool m = false)
        { emo = e; texto = t; correcta = ok; malas = mal; matiz = m; }
    }

    static readonly Situacion[] BANCO =
    {
        new Situacion(RobotEmotion.Enfado,
            "¡A NEO se le ha roto su torre de bloques!",
            "Respirar hondo 3 veces",
            new[]{ "Tirar los bloques", "Gritar muy fuerte", "Culpar a AXEL" }),
        new Situacion(RobotEmotion.Tristeza,
            "TITAN ha perdido a su dron mascota.",
            "Decir cómo se siente",
            new[]{ "Esconderse y no hablar", "Enfadarse con todos", "Hacer como si nada" }),
        new Situacion(RobotEmotion.Miedo,
            "AXEL oye un ruido raro por la noche.",
            "Pedir ayuda a un mayor",
            new[]{ "Quedarse temblando solo", "Gritar sin parar", "No dormir nunca más" }),
        new Situacion(RobotEmotion.Enfado,
            "Un dron se ha colado en el sitio de TITAN.",
            "Decirlo con palabras tranquilas",
            new[]{ "Empujar al dron", "Dar un portazo", "Gritar muy fuerte" }),
        new Situacion(RobotEmotion.Tristeza,
            "NEO no puede ir a la excursión de hoy.",
            "Pedir un abrazo",
            new[]{ "Encerrarse en su cuarto", "Enfadarse con su familia", "Tirar su mochila" }),
        new Situacion(RobotEmotion.Miedo,
            "A AXEL le asusta la tormenta de esta noche.",
            "Pensar en algo bonito",
            new[]{ "Esconderse sin avisar", "Gritar toda la noche", "No salir nunca de casa" }),
        new Situacion(RobotEmotion.Sorpresa,
            "¡El robot cocinero ha quemado la merienda!",
            "Respirar y buscar otra merienda",
            new[]{ "Llorar toda la tarde", "Enfadarse con el cocinero", "No merendar nunca más" }),
        new Situacion(RobotEmotion.Frustracion,
            "A NEO no le sale el puzle de estrellas.",
            "Descansar y probar otra vez",
            new[]{ "Romper el puzle", "Rendirse para siempre", "Tirar las piezas" }, true),
        new Situacion(RobotEmotion.Nervios,
            "Mañana AXEL tiene una carrera importante.",
            "Respirar hondo y prepararse",
            new[]{ "Morderse las uñas sin parar", "No presentarse", "Enfadarse con todos" }, true),
        new Situacion(RobotEmotion.Verguenza,
            "TITAN se ha equivocado delante de todos.",
            "Recordar que todos se equivocan",
            new[]{ "No hablar nunca más", "Enfadarse con quien lo vio", "Esconderse siempre" }, true),
        new Situacion(RobotEmotion.Frustracion,
            "El robot pintor ha manchado su dibujo.",
            "Pedir ayuda para arreglarlo",
            new[]{ "Romper el dibujo", "Tirar las pinturas", "Rendirse para siempre" }, true)
    };

    // ------------------------------------------------ estado

    static readonly Color CAT = new Color(0.18f, 0.80f, 0.58f);   // verde emocional

    RectTransform _root;
    RobotFace     _face;
    TextMeshProUGUI _situText, _promptText, _roundText, _scoreText;
    RectTransform _heartsRow;
    readonly List<RectTransform> _cards = new List<RectTransform>();

    List<Situacion> _ronda;        // situaciones elegidas para esta partida
    int   _idx;                    // situación actual
    int   _logradas;               // rondas ayudadas (1er o 2º intento)
    int   _primeras;               // ayudadas a la primera
    int   _fallosRonda;
    int   _score;
    int   _opciones = 3;           // tarjetas por ronda (4 en difícil)
    float _shownAt;
    bool  _locked;

    // ------------------------------------------------ ciclo de vida

    protected override string GetIntroDescription() =>
        "Un robot amigo llega con una emoción muy fuerte.\n" +
        "Toca la tarjeta que DE VERDAD le ayudaría a calmarse.\n\n" +
        "Piensa despacio: no hay prisa.\n" +
        $"Ayuda en {roundsToWin} de {situationCount} situaciones para ganar.";

    protected override void Start()
    {
        minigameName = "Rescate emocional";
        category     = MinigameCategory.EmotionalManagement;
        ApplyDifficulty();
        base.Start();
    }

    void ApplyDifficulty()
    {
        var diff = GameManager.Instance != null
            ? GameManager.Instance.CurrentDifficulty
            : DifficultyLevel.Easy;
        switch (diff)
        {
            case DifficultyLevel.Medium: situationCount = 6; roundsToWin = 4; _opciones = 3; break;
            case DifficultyLevel.Hard:   situationCount = 8; roundsToWin = 6; _opciones = 4; break;
            default:                     situationCount = 5; roundsToWin = 3; _opciones = 3; break;
        }
    }

    protected override void OnMinigameStart()
    {
        KidUI.EnsureEventSystem();

        bool conMatices = GameManager.Instance != null &&
                          GameManager.Instance.CurrentDifficulty == DifficultyLevel.Hard;

        // Baraja el banco (sin matices fuera de Difícil) y toma las necesarias
        var pool = new List<Situacion>();
        foreach (var s in BANCO)
            if (conMatices || !s.matiz) pool.Add(s);
        for (int i = pool.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }
        _ronda = pool.GetRange(0, Mathf.Min(situationCount, pool.Count));
        situationCount = _ronda.Count;

        _idx = 0; _logradas = 0; _primeras = 0; _score = 0;

        BuildUI();
        ShowSituacion();
    }

    protected override void OnMinigameComplete() { }
    protected override void OnMinigameFailed()   { }

    // ------------------------------------------------ UI

    void BuildUI()
    {
        var cv = KidUI.MakeCanvas("RescateCanvas", 50, transform);
        _root = cv.GetComponent<RectTransform>();
        KidUI.BuildSpaceBackground(_root, withPlanet: false);

        // Cabecera
        var title = KidUI.Txt(_root, "T", "RESCATE EMOCIONAL", CAT, 30,
                              new Vector2(0.30f, 0.925f), new Vector2(0.70f, 0.985f));
        title.fontStyle = FontStyles.Bold;
        title.characterSpacing = 4f;

        _roundText = KidUI.Txt(_root, "Round", "", KidUI.DIM, 20,
                               new Vector2(0.03f, 0.925f), new Vector2(0.20f, 0.985f));
        _roundText.alignment = TextAlignmentOptions.MidlineLeft;

        _scoreText = KidUI.Txt(_root, "Score", "0 pts", Color.white, 20,
                               new Vector2(0.80f, 0.925f), new Vector2(0.97f, 0.985f));
        _scoreText.alignment = TextAlignmentOptions.MidlineRight;
        _scoreText.fontStyle = FontStyles.Bold;

        // Corazones de rescates logrados
        _heartsRow = KidUI.Img(_root, "Hearts", Color.clear,
                               new Vector2(0.35f, 0.865f), new Vector2(0.65f, 0.92f),
                               Vector2.zero, Vector2.zero);
        _heartsRow.GetComponent<Image>().raycastTarget = false;
        RefreshHearts();

        // Panel de situación
        var situ = KidUI.RoundImg(_root, "Situ", new Color(0.05f, 0.08f, 0.16f, 0.95f),
                                  new Vector2(0.22f, 0.74f), new Vector2(0.78f, 0.845f),
                                  Vector2.zero, Vector2.zero, 1.6f);
        situ.GetComponent<Image>().raycastTarget = false;
        _situText = KidUI.Txt(situ, "T", "", Color.white, 24,
                              new Vector2(0.03f, 0.05f), new Vector2(0.97f, 0.95f));
        _situText.enableWordWrapping = true;

        // Cara del robot (protagonista)
        _face = EmotionFaceArt.Build(_root, new Vector2(0.5f, 0.545f), 300f);

        // Pregunta
        _promptText = KidUI.Txt(_root, "Prompt", "¿Qué le ayudaría a calmarse?",
                                new Color(1f, 1f, 1f, 0.85f), 24,
                                new Vector2(0.25f, 0.335f), new Vector2(0.75f, 0.395f));
        _promptText.fontStyle = FontStyles.Bold;
    }

    void RefreshHearts()
    {
        foreach (Transform c in _heartsRow) Destroy(c.gameObject);
        for (int i = 0; i < roundsToWin; i++)
        {
            bool on = i < _logradas;
            var h = KidUI.CircleAt(_heartsRow, "H" + i,
                on ? new Color(0.95f, 0.35f, 0.50f, 1f) : new Color(1f, 1f, 1f, 0.12f),
                new Vector2(0.5f + (i - (roundsToWin - 1) * 0.5f) * 0.12f, 0.5f), 26f);
            h.GetComponent<Image>().raycastTarget = false;
            if (on) UITween.PopIn(h, 0.3f, 0.4f);
        }
    }

    // ------------------------------------------------ rondas

    void ShowSituacion()
    {
        var s = _ronda[_idx];
        _fallosRonda = 0;
        _locked = false;
        _shownAt = Time.time;

        _roundText.text = $"Robot {_idx + 1} de {situationCount}";
        _situText.text = s.texto;
        _face.SetEmotion(s.emo, 0.9f);
        _face.Pulse();
        _promptText.text = "¿Qué le ayudaría a calmarse?";
        _promptText.color = new Color(1f, 1f, 1f, 0.85f);

        // Baraja las opciones (correcta + malas)
        var opciones = new List<string> { s.correcta };
        for (int i = 0; i < Mathf.Min(_opciones - 1, s.malas.Length); i++)
            opciones.Add(s.malas[i]);
        for (int i = opciones.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (opciones[i], opciones[j]) = (opciones[j], opciones[i]);
        }

        // Tarjetas
        foreach (var c in _cards) if (c != null) Destroy(c.gameObject);
        _cards.Clear();

        int n = opciones.Count;
        float w = n == 4 ? 0.215f : 0.26f;
        float gap = n == 4 ? 0.24f : 0.30f;
        float x0 = 0.5f - gap * (n - 1) * 0.5f;
        for (int i = 0; i < n; i++)
        {
            string texto = opciones[i];
            bool esCorrecta = texto == s.correcta;
            float cx = x0 + gap * i;

            var card = KidUI.RoundImg(_root, "Card" + i, new Color(0.08f, 0.12f, 0.24f, 0.97f),
                                      new Vector2(cx - w / 2f, 0.08f), new Vector2(cx + w / 2f, 0.30f),
                                      Vector2.zero, Vector2.zero, 1.2f);
            var acc = KidUI.RoundImg(card, "Acc", CAT,
                                     new Vector2(0f, 0.90f), Vector2.one, Vector2.zero, Vector2.zero, 1.2f);
            acc.GetComponent<Image>().raycastTarget = false;
            var t = KidUI.Txt(card, "T", texto, Color.white, 21,
                              new Vector2(0.06f, 0.08f), new Vector2(0.94f, 0.82f));
            t.enableWordWrapping = true;
            t.fontStyle = FontStyles.Bold;

            var btn = card.gameObject.AddComponent<Button>();
            btn.targetGraphic = card.GetComponent<Image>();
            btn.onClick.AddListener(() => OnCardChosen(card, esCorrecta, s));
            ButtonJuice.Attach(card.gameObject);
            _cards.Add(card);

            UITween.PopIn(card, 0.35f, 0.8f, 0.07f * i);
        }
    }

    void OnCardChosen(RectTransform card, bool esCorrecta, Situacion s)
    {
        if (_locked) return;

        float rtMs = (Time.time - _shownAt) * 1000f;
        ReportEvent(esCorrecta, rtMs);

        if (esCorrecta)
        {
            _locked = true;
            _logradas++;
            bool primera = _fallosRonda == 0;
            if (primera) _primeras++;
            int pts = primera ? pointsPositive : pointsNeutral;
            _score += pts;
            _scoreText.text = _score + " pts";
            RefreshHearts();

            GameFeel.PlaySuccess();
            GameFeel.Success(card, floatText: false);
            GameFeel.FloatingText(primera ? "+" + pts + "  ¡Le has ayudado!" : "+" + pts,
                                  CAT, new Vector2(0.5f, 0.42f));
            StartCoroutine(CalmAndNext(s));
        }
        else
        {
            _fallosRonda++;
            GameFeel.PlayError();
            GameFeel.Shake(card, 10f, 0.3f);
            var img = card.GetComponent<Image>();
            img.color = new Color(0.10f, 0.10f, 0.16f, 0.85f);
            var b = card.GetComponent<Button>();
            if (b != null) b.interactable = false;
            _face.Pulse();

            if (_fallosRonda >= 2)
            {
                // Se enseña la buena con cariño y la ronda no puntúa
                _locked = true;
                _promptText.text = "Esta era la que ayudaba:";
                _promptText.color = KidUI.WARN;
                StartCoroutine(RevealAndNext(s));
            }
            else
            {
                _promptText.text = "Mmm... esa no le ayuda. ¡Prueba otra!";
                _promptText.color = KidUI.WARN;
            }
        }
    }

    IEnumerator CalmAndNext(Situacion s)
    {
        // El robot se calma delante del niño: ESTA es la recompensa
        yield return new WaitForSeconds(0.35f);
        _face.SetEmotion(s.emo, 0.35f);
        yield return new WaitForSeconds(0.45f);
        _face.SetEmotion(RobotEmotion.Calma, 0.7f);
        _face.Pulse();
        GameFeel.PlayStar();
        _situText.text = "¡Gracias! Ya me encuentro mucho mejor.";
        yield return new WaitForSeconds(1.1f);
        NextRound();
    }

    IEnumerator RevealAndNext(Situacion s)
    {
        // Resalta la tarjeta correcta
        foreach (var c in _cards)
        {
            if (c == null) continue;
            var txt = c.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null && txt.text == s.correcta)
            {
                c.GetComponent<Image>().color = new Color(CAT.r, CAT.g, CAT.b, 0.55f);
                UITween.PulseOnce(c, 1.10f, 0.3f);
            }
        }
        yield return new WaitForSeconds(1.6f);
        NextRound();
    }

    void NextRound()
    {
        _idx++;
        if (_idx < situationCount) { ShowSituacion(); return; }
        StartCoroutine(Finish());
    }

    IEnumerator Finish()
    {
        bool  success = _logradas >= roundsToWin;
        float ratio   = situationCount > 0 ? (float)_logradas / situationCount : 0f;

        if (success) { CompleteMinigame(_score); GameFeel.Confetti(60); }
        else         { FailMinigame(); }

        yield return new WaitForSeconds(0.8f);

        ShowResults(success,
            GameFeel.StarsFromRatio(success, ratio),
            _score,
            new[]
            {
                "Robots ayudados: " + _logradas + " / " + situationCount,
                "A la primera: " + _primeras,
                "Meta: " + roundsToWin
            },
            success ? "¡Rescate completado!" : "¡Casi lo consigues!",
            success ? "Sabes muy bien qué ayuda a calmarse."
                    : "Recuerda: respirar hondo y pedir ayuda siempre funcionan.");
    }
}
