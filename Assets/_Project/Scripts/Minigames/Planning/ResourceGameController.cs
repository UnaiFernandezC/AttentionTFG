// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// TORRES DE ENERGIA — Torre de Hanoi infantil (paradigma clasico de
/// planificacion). Hay 3 torres y N anillos de energia de colores apilados
/// en la torre izquierda. Toca una torre para levantar su anillo superior
/// y otra torre para soltarlo. Un anillo grande nunca puede ir encima de
/// uno pequeño. Objetivo: mover toda la pila a la torre derecha.
/// La clase conserva su nombre original (ResourceGameController) y sus
/// campos publicos para no romper las referencias serializadas de la escena.
/// </summary>
public class ResourceGameController : MinigameBase
{
    // ---------------------------------------------------------------
    // CAMPOS LEGACY: conservados solo para que las escenas serializadas
    // no pierdan referencias. El juego nuevo NO los usa.
    // ---------------------------------------------------------------
    [Serializable]
    public class ActionData
    {
        public string icon = "+";
        public string actionName = "Accion";
        public int cost = 1;
        public int progress = 10;
        public Color buttonColor = Color.black;
        public bool isTrap = false;
        public bool isRisky = false;
        public int  riskyMin = 25;
        public int  riskyMax = 55;
    }

    [Header("Campos legacy (conservados para la escena, sin uso)")]
    public int stars = 20;
    public int goal = 100;
    public List<ActionData> actions = new List<ActionData>();

    // ================================================================ CONFIG

    const int TOWERS = 3;
    static readonly float[] TOWER_X = { -520f, 0f, 520f };
    const float BASE_Y   = -215f;
    const float PILLAR_H = 330f;
    const float RING_H   = 44f;
    const float RING_STEP = 48f;

    // Colores de los anillos (del mas pequeño al mas grande)
    static readonly Color[] RING_COLORS =
    {
        new Color(0.98f, 0.80f, 0.10f),
        new Color(0.18f, 0.80f, 0.58f),
        new Color(0.28f, 0.60f, 1.00f),
        new Color(0.58f, 0.28f, 0.92f)
    };

    static readonly Color ACCENT2 = new Color(0.28f, 0.60f, 1.00f);

    // ------- dificultad -------
    int  _ringCount  = 3;
    int  _optimal    = 7;
    int  _target     = 14;   // 2x optimo
    bool _showTarget = false;

    // ------- estado -------
    List<int>[]     _piles;          // cada pila guarda tamaños (1 pequeño..N grande)
    RectTransform[] _ringRT;         // visual de cada anillo, indexado por tamaño
    int   _selected = -1;
    bool  _busy, _ended;
    int   _moves;
    int   _legalMoves, _illegalTries;
    float _lastMoveTime;

    // ------- UI -------
    RectTransform _root, _board;
    RectTransform[] _towerGlow;
    TextMeshProUGUI _movesLbl, _msgLbl;

    // ================================================================ CICLO

    protected override void Start()
    {
        minigameName = "Torres de energía";
        category     = MinigameCategory.Planning;
        base.Start();
    }

    protected override string GetIntroDescription() =>
        "Lleva todos los anillos de energia a la torre derecha.\n" +
        "Toca una torre para levantar su anillo de arriba y otra\n" +
        "torre para soltarlo. Recuerda: un anillo grande nunca\n" +
        "puede ir encima de uno pequeño. ¡Planifica tus movimientos!";

    protected override void OnMinigameStart()
    {
        KidUI.EnsureEventSystem();
        ApplyDifficulty();

        _moves        = 0;
        _legalMoves   = 0;
        _illegalTries = 0;
        _selected     = -1;
        _busy         = false;
        _ended        = false;
        _lastMoveTime = Time.realtimeSinceStartup;

        BuildUI();
        BuildRings();
        RefreshHUD();
    }

    protected override void OnMinigameComplete() { }
    protected override void OnMinigameFailed()   { }

    void ApplyDifficulty()
    {
        var diff = GameManager.Instance != null
            ? GameManager.Instance.CurrentDifficulty
            : DifficultyLevel.Easy;

        switch (diff)
        {
            case DifficultyLevel.Medium:
                _ringCount = 3; _showTarget = true;   // objetivo: <= 14 (2x de 7)
                break;
            case DifficultyLevel.Hard:
                _ringCount = 4; _showTarget = true;   // objetivo: <= 30 (2x de 15)
                break;
            default: // Easy: sin limite, contador solo informativo
                _ringCount = 3; _showTarget = false;
                break;
        }
        _optimal = (1 << _ringCount) - 1;
        _target  = _optimal * 2;
    }

    // ================================================================ UI

    void BuildUI()
    {
        var cv = KidUI.MakeCanvas("HanoiCanvas", 50, transform);
        _root  = cv.GetComponent<RectTransform>();
        KidUI.BuildSpaceBackground(_root);

        // ---- cabecera ----
        var hdr = KidUI.RoundImg(_root, "Hdr", KidUI.PANEL,
            new Vector2(0.02f, 0.905f), new Vector2(0.98f, 0.985f),
            Vector2.zero, Vector2.zero, 1.4f);
        var line = KidUI.RoundImg(hdr, "Line", ACCENT2,
            new Vector2(0.02f, 0f), new Vector2(0.98f, 0f),
            new Vector2(0f, 2f), new Vector2(0f, 4f), 4f);
        line.GetComponent<Image>().raycastTarget = false;

        var title = KidUI.Txt(hdr, "T", "TORRES DE ENERGIA", Color.white, 36,
            new Vector2(0.02f, 0f), new Vector2(0.45f, 1f));
        title.fontStyle = FontStyles.Bold;
        title.alignment = TextAlignmentOptions.MidlineLeft;

        _movesLbl = KidUI.Txt(hdr, "Moves", "", ACCENT2, 27,
            new Vector2(0.45f, 0f), new Vector2(0.98f, 1f));
        _movesLbl.fontStyle = FontStyles.Bold;
        _movesLbl.alignment = TextAlignmentOptions.MidlineRight;
        UITween.PopIn(hdr, 0.4f, 0.9f);

        // ---- mensaje-guia ----
        _msgLbl = KidUI.Txt(_root, "Msg",
            "Toca una torre para levantar su anillo de arriba",
            KidUI.DIM, 28, new Vector2(0.05f, 0.83f), new Vector2(0.95f, 0.90f));
        _msgLbl.overflowMode = TextOverflowModes.Overflow;

        // ---- tablero ----
        var boardGO = new GameObject("Board");
        boardGO.transform.SetParent(_root, false);
        _board = boardGO.AddComponent<RectTransform>();
        _board.anchorMin = _board.anchorMax = new Vector2(0.5f, 0.45f);
        _board.pivot = new Vector2(0.5f, 0.5f);
        _board.anchoredPosition = Vector2.zero;
        _board.sizeDelta = Vector2.zero;

        _towerGlow = new RectTransform[TOWERS];
        for (int i = 0; i < TOWERS; i++)
            BuildTower(i);

        // Rotulos de ayuda bajo las torres
        MakeLabel("SALIDA",  TOWER_X[0], KidUI.DIM);
        MakeLabel("META",    TOWER_X[2], KidUI.GOOD);

        // ---- boton reiniciar ----
        KidUI.Btn(_root, "Volver a empezar", KidUI.BTNC,
            new Vector2(0.02f, 0.02f), new Vector2(0.20f, 0.095f),
            ResetPuzzle, 24f);
    }

    void MakeLabel(string txt, float x, Color col)
    {
        var lbl = KidUI.Txt(_board, "Lbl_" + txt, txt, col, 24,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        var rt = (RectTransform)lbl.transform;
        rt.anchoredPosition = new Vector2(x, BASE_Y - 58f);
        rt.sizeDelta = new Vector2(220f, 34f);
        lbl.fontStyle = FontStyles.Bold;
        lbl.raycastTarget = false;
    }

    void BuildTower(int i)
    {
        float x = TOWER_X[i];

        // Pilar
        var pillar = KidUI.RoundImg(_board, "Pillar" + i, new Color(0.20f, 0.26f, 0.46f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(x, BASE_Y + PILLAR_H * 0.5f), new Vector2(26f, PILLAR_H), 2f);
        pillar.GetComponent<Image>().raycastTarget = false;

        // Base
        var bse = KidUI.RoundImg(_board, "Base" + i, new Color(0.16f, 0.21f, 0.38f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(x, BASE_Y - 20f), new Vector2(420f, 36f), 1.2f);
        bse.GetComponent<Image>().raycastTarget = false;

        // Halo superior (se enciende cuando la torre esta seleccionada)
        var glow = KidUI.CircleAt(_board, "Glow" + i,
            new Color(ACCENT2.r, ACCENT2.g, ACCENT2.b, 0.30f),
            new Vector2(0.5f, 0.5f), 110f);
        glow.anchoredPosition = new Vector2(x, BASE_Y + PILLAR_H + 66f);
        glow.GetComponent<Image>().raycastTarget = false;
        glow.gameObject.SetActive(false);
        _towerGlow[i] = glow;

        // Zona tactil de toda la columna (imagen invisible que si recibe clics)
        var hit = KidUI.Img(_board, "Hit" + i, new Color(0f, 0f, 0f, 0f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(x, BASE_Y + PILLAR_H * 0.5f + 40f),
            new Vector2(460f, PILLAR_H + 260f));
        var btn = hit.gameObject.AddComponent<Button>();
        btn.transition = Selectable.Transition.None;
        int captured = i;
        btn.onClick.AddListener(() => OnTower(captured));

        UITween.PopIn(pillar, 0.4f, 0.6f, 0.05f * i);
        UITween.PopIn(bse, 0.4f, 0.6f, 0.05f * i);
    }

    void BuildRings()
    {
        _piles = new List<int>[TOWERS];
        for (int i = 0; i < TOWERS; i++) _piles[i] = new List<int>();
        _ringRT = new RectTransform[_ringCount + 1];

        // Pila inicial en la torre izquierda: grande abajo, pequeño arriba
        for (int size = _ringCount; size >= 1; size--)
        {
            _piles[0].Add(size);

            float w = 130f + size * 62f;
            var ring = KidUI.RoundImg(_board, "Ring" + size,
                RING_COLORS[(size - 1) % RING_COLORS.Length],
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                RingRestPos(0, _piles[0].Count - 1), new Vector2(w, RING_H), 0.85f);

            // Brillito superior para dar volumen
            var shine = KidUI.RoundImg(ring, "Shine", new Color(1f, 1f, 1f, 0.28f),
                new Vector2(0.08f, 0.58f), new Vector2(0.92f, 0.80f),
                Vector2.zero, Vector2.zero, 4f);
            shine.GetComponent<Image>().raycastTarget = false;
            ring.GetComponent<Image>().raycastTarget = false;

            _ringRT[size] = ring;
            UITween.PopIn(ring, 0.45f, 0.5f, 0.10f + 0.07f * (_ringCount - size));
        }
    }

    Vector2 RingRestPos(int tower, int stackIdx) =>
        new Vector2(TOWER_X[tower], BASE_Y + RING_H * 0.5f + stackIdx * RING_STEP);

    Vector2 LiftPos(int tower) =>
        new Vector2(TOWER_X[tower], BASE_Y + PILLAR_H + 66f);

    // ================================================================ JUEGO

    void OnTower(int i)
    {
        if (_ended || _busy || !IsPlaying) return;

        if (_selected < 0)
        {
            // --- elegir torre origen ---
            if (_piles[i].Count == 0)
            {
                GameFeel.PlayPop();
                _msgLbl.text  = "Esa torre esta vacia. ¡Elige una con anillos!";
                _msgLbl.color = KidUI.DIM;
                return;
            }
            _selected = i;
            int top = TopOf(i);
            GameFeel.PlayPop();
            _towerGlow[i].gameObject.SetActive(true);
            StartCoroutine(GlideRing(_ringRT[top], LiftPos(i), 0.22f, null));
            _msgLbl.text  = "¡Anillo levantado! Ahora toca la torre donde soltarlo";
            _msgLbl.color = ACCENT2;
        }
        else if (i == _selected)
        {
            // --- soltar en la misma torre (cancelar) ---
            int top = TopOf(i);
            _towerGlow[i].gameObject.SetActive(false);
            _selected = -1;
            GameFeel.PlayPop();
            StartCoroutine(GlideRing(_ringRT[top],
                RingRestPos(i, _piles[i].Count - 1), 0.22f, null));
            _msgLbl.text  = "Anillo devuelto. Toca una torre para empezar";
            _msgLbl.color = KidUI.DIM;
        }
        else
        {
            // --- intentar mover al destino ---
            int from = _selected;
            int size = TopOf(from);
            bool legal = _piles[i].Count == 0 || TopOf(i) > size;

            float rtMs = (Time.realtimeSinceStartup - _lastMoveTime) * 1000f;
            _lastMoveTime = Time.realtimeSinceStartup;
            ReportEvent(legal, rtMs);

            _towerGlow[from].gameObject.SetActive(false);
            _selected = -1;

            if (legal)
            {
                _piles[from].RemoveAt(_piles[from].Count - 1);
                _piles[i].Add(size);
                _moves++;
                _legalMoves++;
                GameFeel.PlayPop();
                StartCoroutine(MoveRingAcross(size, from, i));
                RefreshHUD();
            }
            else
            {
                // Feedback suave: no es un fallo grave, solo se recoloca
                _illegalTries++;
                GameFeel.PlayError();
                GameFeel.Shake(_ringRT[size], 10f, 0.3f);
                GameFeel.FloatingText("El grande no cabe encima", KidUI.WARN,
                    new Vector2(0f, 40f), 38f);
                StartCoroutine(GlideRing(_ringRT[size],
                    RingRestPos(from, _piles[from].Count - 1), 0.25f, null));
                _msgLbl.text  = "Los anillos grandes van siempre debajo";
                _msgLbl.color = KidUI.WARN;
            }
        }
    }

    int TopOf(int tower) => _piles[tower][_piles[tower].Count - 1];

    IEnumerator MoveRingAcross(int size, int from, int to)
    {
        _busy = true;
        var ring = _ringRT[size];
        // El anillo ya esta levantado sobre 'from': cruza y baja
        yield return GlideRing(ring, LiftPos(to), 0.26f, null);
        yield return GlideRing(ring, RingRestPos(to, _piles[to].Count - 1), 0.20f, null);
        UITween.PulseOnce(ring, 1.08f, 0.16f);
        _busy = false;

        if (_piles[2].Count == _ringCount)
        {
            _ended = true;
            StartCoroutine(Finish());
        }
        else if (_showTarget && _moves == _target + 1)
        {
            _msgLbl.text  = "Puedes seguir jugando, ¡pero intenta planear mejor!";
            _msgLbl.color = KidUI.WARN;
        }
        else
        {
            _msgLbl.text  = "¡Muy bien! Sigue asi";
            _msgLbl.color = KidUI.DIM;
        }
    }

    IEnumerator GlideRing(RectTransform ring, Vector2 target, float dur, Action onDone)
    {
        if (ring == null) yield break;
        Vector2 start = ring.anchoredPosition;
        float t = 0f;
        while (t < dur)
        {
            if (ring == null) yield break;
            t += Time.deltaTime;
            ring.anchoredPosition = Vector2.Lerp(start, target,
                Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / dur)));
            yield return null;
        }
        ring.anchoredPosition = target;
        onDone?.Invoke();
    }

    IEnumerator Finish()
    {
        GameFeel.PlaySuccess();
        GameFeel.Confetti(45);
        GameFeel.FloatingText("¡Torre completada!", KidUI.GOOD, new Vector2(0f, 80f));

        float ratio = _moves > 0 ? Mathf.Clamp01((float)_optimal / _moves) : 1f;
        int score = 400 + Mathf.RoundToInt(600f * ratio);

        CompleteMinigame(score);

        yield return new WaitForSeconds(1.2f);

        int pct = Mathf.RoundToInt(ratio * 100f);
        ShowResults(true, GameFeel.StarsFromRatio(true, ratio), score,
            new[]
            {
                "Movimientos: " + _moves,
                "Optimo: " + _optimal,
                "Eficiencia: " + pct + "%"
            },
            _moves == _optimal ? "¡Plan perfecto!" : "¡Energia restaurada!",
            _moves == _optimal
                ? "Lo resolviste con los movimientos justos"
                : "Toda la energia llego a la torre meta");
    }

    /// <summary>Recoloca los anillos en la torre de salida y reinicia el
    /// contador (borron y cuenta nueva, sin castigos).</summary>
    void ResetPuzzle()
    {
        if (_ended || !IsPlaying) return;
        StopAllCoroutines();
        _busy = false;
        _selected = -1;
        _moves = 0;
        _lastMoveTime = Time.realtimeSinceStartup;
        foreach (var g in _towerGlow) g.gameObject.SetActive(false);

        for (int i = 0; i < TOWERS; i++) _piles[i].Clear();
        for (int size = _ringCount; size >= 1; size--)
        {
            _piles[0].Add(size);
            _ringRT[size].anchoredPosition = RingRestPos(0, _piles[0].Count - 1);
            UITween.PopIn(_ringRT[size], 0.3f, 0.7f);
        }
        _msgLbl.text  = "Torres reiniciadas. ¡A planificar!";
        _msgLbl.color = KidUI.DIM;
        RefreshHUD();
    }

    void RefreshHUD()
    {
        if (_movesLbl == null) return;
        _movesLbl.text = _showTarget
            ? "Movimientos: " + _moves + "   (optimo " + _optimal + ", objetivo " + _target + ")"
            : "Movimientos: " + _moves + "   (optimo " + _optimal + ")";
        _movesLbl.color = (_showTarget && _moves > _target) ? KidUI.WARN : ACCENT2;
        UITween.PulseOnce((RectTransform)_movesLbl.transform, 1.06f, 0.14f);
    }
}
