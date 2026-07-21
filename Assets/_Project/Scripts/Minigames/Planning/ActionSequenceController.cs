// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// EL TREN DE ATTENTIA — minijuego de planificacion pura.
/// Narrativa: los trenes de la Gran Fabrica vuelven a circular.
/// El niño ve una red de vias con desvios (flechas que puede girar),
/// planifica TODO antes de pulsar "¡EN MARCHA!" y luego observa si el
/// tren llega a la estacion meta o acaba en una via muerta.
/// La clase conserva su nombre original (ActionSequenceController) para
/// no romper las referencias serializadas de las escenas.
/// </summary>
public class ActionSequenceController : MinigameBase
{
    [Header("Campo legacy (conservado para la escena, sin uso)")]
    public float errorDelay = 0.9f;

    // ================================================================ MODELO

    class RailNode
    {
        public Vector2 pos;                    // posicion en px dentro del tablero
        public int  next = -1;                 // salida unica (nodos normales)
        public int  outA = -1, outB = -1;      // salidas de un desvio
        public int  state;                     // 0 → outA, 1 → outB
        public bool isSwitch, isStation, isDeadEnd, isBroken;
        public bool hasStar, starTaken;
        public RectTransform rt;               // circulo del nodo
        public RectTransform arrow;            // flecha giratoria del desvio
        public GameObject    starGO;
        public Button        btn;
    }

    class RailEdge
    {
        public int   a, b;
        public Image stripe;                   // franja interior coloreable
    }

    enum Phase { Planning, Driving, Over }

    const int ROUNDS     = 3;
    const int MAX_ERRORS = 3;

    // ------- dificultad -------
    int   _numSwitches = 2;
    bool  _hasBroken   = false;
    float _halfWidth   = 520f;
    int   _starsPerRound = 2;

    // ------- estado de partida -------
    Phase _phase = Phase.Planning;
    int   _round;
    int   _errors;
    int   _launches;
    int   _score;
    int   _starsCollected;
    int   _starsTotalSeen;
    int   _roundStars;
    float _planStart;

    List<RailNode> _nodes = new List<RailNode>();
    List<RailEdge> _edges = new List<RailEdge>();
    int _startIdx;

    // ------- UI -------
    RectTransform _root, _board, _trainRT;
    Button        _goBtn;
    TextMeshProUGUI _roundLbl, _starLbl, _msgLbl;
    Image[] _tryDots;

    // Paleta (azul de Planificacion + carriles)
    static readonly Color ACCENT2   = new Color(0.28f, 0.60f, 1.00f);
    static readonly Color RAIL_DARK = new Color(0.15f, 0.19f, 0.34f);
    static readonly Color RAIL_DIM  = new Color(0.36f, 0.44f, 0.64f);
    static readonly Color STAR_YEL  = new Color(0.98f, 0.80f, 0.10f);

    // ================================================================ CICLO

    protected override void Start()
    {
        minigameName = "El tren de Attentia";
        category     = MinigameCategory.Planning;
        base.Start();
    }

    protected override string GetIntroDescription() =>
        "Los trenes de la Gran Fabrica vuelven a circular.\n" +
        "Toca los desvios naranjas para girar sus flechas y, cuando\n" +
        "tengas la ruta lista, pulsa ¡EN MARCHA! para llevar el tren\n" +
        "a la estacion. ¡Recoge las estrellas del camino!";

    protected override void OnMinigameStart()
    {
        KidUI.EnsureEventSystem();
        ApplyDifficulty();

        _errors         = 0;
        _launches       = 0;
        _score          = 0;
        _starsCollected = 0;
        _starsTotalSeen = 0;

        BuildBaseUI();
        BuildRound(0);
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
                _numSwitches = 3; _hasBroken = false;
                _halfWidth = 640f; _starsPerRound = 3;
                break;
            case DifficultyLevel.Hard:
                _numSwitches = 4; _hasBroken = true;
                _halfWidth = 700f; _starsPerRound = 2; // + 1 estrella en el rodeo
                break;
            default: // Easy: red pequeña
                _numSwitches = 2; _hasBroken = false;
                _halfWidth = 520f; _starsPerRound = 2;
                break;
        }
    }

    // ================================================================ UI BASE

    void BuildBaseUI()
    {
        var cv = KidUI.MakeCanvas("TrainCanvas", 50, transform);
        _root  = cv.GetComponent<RectTransform>();
        KidUI.BuildSpaceBackground(_root);

        // ---- cabecera redondeada ----
        var hdr = KidUI.RoundImg(_root, "Hdr", KidUI.PANEL,
            new Vector2(0.02f, 0.905f), new Vector2(0.98f, 0.985f),
            Vector2.zero, Vector2.zero, 1.4f);
        var line = KidUI.RoundImg(hdr, "Line", ACCENT2,
            new Vector2(0.02f, 0f), new Vector2(0.98f, 0f),
            new Vector2(0f, 2f), new Vector2(0f, 4f), 4f);
        line.GetComponent<Image>().raycastTarget = false;

        var title = KidUI.Txt(hdr, "T", "EL TREN DE ATTENTIA", Color.white, 36,
            new Vector2(0.02f, 0f), new Vector2(0.42f, 1f));
        title.fontStyle = FontStyles.Bold;
        title.alignment = TextAlignmentOptions.MidlineLeft;

        _roundLbl = KidUI.Txt(hdr, "Round", "Viaje 1 / " + ROUNDS, KidUI.DIM, 26,
            new Vector2(0.42f, 0f), new Vector2(0.62f, 1f));

        _starLbl = KidUI.Txt(hdr, "Stars", "Estrellas: 0", STAR_YEL, 26,
            new Vector2(0.62f, 0f), new Vector2(0.80f, 1f));
        _starLbl.fontStyle = FontStyles.Bold;

        // Intentos restantes: 3 circulitos
        _tryDots = new Image[MAX_ERRORS];
        for (int i = 0; i < MAX_ERRORS; i++)
        {
            var d = KidUI.CircleAt(hdr, "Try" + i, KidUI.GOOD,
                new Vector2(0.86f + i * 0.04f, 0.5f), 22f);
            _tryDots[i] = d.GetComponent<Image>();
            _tryDots[i].raycastTarget = false;
        }
        UITween.PopIn(hdr, 0.4f, 0.9f);

        // ---- mensaje-guia ----
        _msgLbl = KidUI.Txt(_root, "Msg",
            "Gira los desvios y planifica la ruta hasta la estacion",
            KidUI.DIM, 28, new Vector2(0.05f, 0.83f), new Vector2(0.95f, 0.90f));
        _msgLbl.overflowMode = TextOverflowModes.Overflow;

        // ---- tablero de vias ----
        var boardGO = new GameObject("Board");
        boardGO.transform.SetParent(_root, false);
        _board = boardGO.AddComponent<RectTransform>();
        _board.anchorMin = _board.anchorMax = new Vector2(0.5f, 0.47f);
        _board.pivot = new Vector2(0.5f, 0.5f);
        _board.anchoredPosition = Vector2.zero;
        _board.sizeDelta = Vector2.zero;

        // ---- boton EN MARCHA ----
        _goBtn = KidUI.Btn(_root, "¡EN MARCHA!", KidUI.GOOD,
            new Vector2(0.38f, 0.02f), new Vector2(0.62f, 0.105f),
            OnGoPressed, 34f);
        UITween.PopIn((RectTransform)_goBtn.transform, 0.45f, 0.7f, 0.15f);
    }

    // ================================================================ RONDA

    void BuildRound(int r)
    {
        _round      = r;
        _roundStars = 0;
        _phase      = Phase.Planning;

        // Limpia el tablero anterior
        for (int i = _board.childCount - 1; i >= 0; i--)
            Destroy(_board.GetChild(i).gameObject);
        _nodes.Clear();
        _edges.Clear();

        GenerateNetwork();
        DrawNetwork();
        BuildTrain();
        RefreshRouteHighlight();
        UpdateHUD();

        _msgLbl.text  = "Gira los desvios y planifica la ruta hasta la estacion";
        _msgLbl.color = KidUI.DIM;
        _goBtn.interactable = true;
        _planStart = Time.realtimeSinceStartup;
    }

    /// <summary>Genera la red: linea principal con desvios, vias muertas,
    /// estrellas y (en dificil) un tramo roto con rodeo obligatorio.</summary>
    void GenerateNetwork()
    {
        int n = _numSwitches;
        float x0 = -_halfWidth, x1 = _halfWidth;
        float step = (x1 - x0) / (n + 1);

        // --- puntos principales: inicio, desvios W1..Wn, estacion ---
        int[] mains = new int[n + 2];
        mains[0] = AddNode(new Vector2(x0, 0f));
        for (int i = 1; i <= n; i++)
        {
            float wy = (i % 2 == 0) ? 42f : -42f;
            int w = AddNode(new Vector2(x0 + step * i, wy));
            _nodes[w].isSwitch = true;
            mains[i] = w;
        }
        mains[n + 1] = AddNode(new Vector2(x1, 0f));
        _nodes[mains[n + 1]].isStation = true;

        // --- nodos intermedios en cada tramo principal (para estrellas) ---
        int[] mids = new int[n + 1];
        for (int j = 0; j <= n; j++)
        {
            Vector2 pa = _nodes[mains[j]].pos, pb = _nodes[mains[j + 1]].pos;
            mids[j] = AddNode(Vector2.Lerp(pa, pb, 0.5f));
        }

        // --- desvio con rodeo (solo dificil): tramo roto que hay que esquivar ---
        int detour = _hasBroken ? Mathf.Clamp(n / 2, 1, n) : -1;   // indice 1..n

        // --- cableado de la red ---
        _nodes[mains[0]].next = mids[0];
        for (int j = 0; j <= n; j++)
        {
            int endOfMid = mains[j + 1];
            _nodes[mids[j]].next = endOfMid;
        }

        for (int i = 1; i <= n; i++)
        {
            var w = _nodes[mains[i]];
            int continueTo = mids[i];

            if (i == detour)
            {
                // El tramo recto esta ROTO: el nodo intermedio se marca roto y
                // el desvio ofrece un rodeo por arriba que reengancha despues.
                _nodes[mids[i]].isBroken = true;

                float dy   = 190f;
                Vector2 wp = w.pos;
                int t1 = AddNode(new Vector2(wp.x + step * 0.33f, wp.y + dy));
                int t2 = AddNode(new Vector2(wp.x + step * 0.66f, wp.y + dy));
                _nodes[t1].next = t2;
                _nodes[t2].next = mains[i + 1];
                _nodes[t2].hasStar = true;     // premio por esquivar el tramo roto
                _starsTotalSeen++;

                bool swap = Random.value < 0.5f;
                w.outA = swap ? t1 : mids[i];
                w.outB = swap ? mids[i] : t1;
                AddEdgeVisualPair(mains[i], t1);
                AddEdgeVisualPair(t1, t2);
                AddEdgeVisualPair(t2, mains[i + 1]);
            }
            else
            {
                // Via muerta: pequeño ramal que no lleva a ningun sitio
                float dir = (i % 2 == 0) ? 1f : -1f;
                if (Random.value < 0.4f) dir = -dir;
                Vector2 wp = w.pos;
                int stub = AddNode(new Vector2(wp.x + step * 0.45f, wp.y + dir * 175f));
                _nodes[stub].isDeadEnd = true;

                bool swap = Random.value < 0.5f;
                w.outA = swap ? stub : continueTo;
                w.outB = swap ? continueTo : stub;
                AddEdgeVisualPair(mains[i], stub);
            }
            w.state = Random.Range(0, 2);
        }

        // --- aristas de la linea principal ---
        AddEdgeVisualPair(mains[0], mids[0]);
        for (int j = 0; j <= n; j++)
            AddEdgeVisualPair(mids[j], mains[j + 1]);
        for (int i = 1; i <= n; i++)
            AddEdgeVisualPair(mains[i], mids[i]);

        _startIdx = mains[0];

        // --- estrellas sobre nodos intermedios transitables ---
        var candidates = new List<int>();
        for (int j = 0; j <= n; j++)
            if (!_nodes[mids[j]].isBroken) candidates.Add(mids[j]);
        for (int s = 0; s < _starsPerRound && candidates.Count > 0; s++)
        {
            int pick = Random.Range(0, candidates.Count);
            _nodes[candidates[pick]].hasStar = true;
            _starsTotalSeen++;
            candidates.RemoveAt(pick);
        }

        // --- garantiza que la ruta inicial NO este ya resuelta ---
        var route = ComputeRoute();
        if (_nodes[route[route.Count - 1]].isStation)
        {
            var switches = new List<RailNode>();
            foreach (var nd in _nodes) if (nd.isSwitch) switches.Add(nd);
            var flip = switches[Random.Range(0, switches.Count)];
            flip.state = 1 - flip.state;
        }
    }

    int AddNode(Vector2 pos)
    {
        _nodes.Add(new RailNode { pos = pos });
        return _nodes.Count - 1;
    }

    // Solo registra el par logico; el dibujo real se hace en DrawNetwork
    List<Vector2Int> _edgePairs = new List<Vector2Int>();
    void AddEdgeVisualPair(int a, int b) { _edgePairs.Add(new Vector2Int(a, b)); }

    // ================================================================ DIBUJO

    void DrawNetwork()
    {
        // ---- vias (debajo de todo) ----
        foreach (var pr in _edgePairs)
            DrawEdge(pr.x, pr.y);
        _edgePairs.Clear();

        // ---- nodos ----
        float delay = 0f;
        for (int i = 0; i < _nodes.Count; i++)
        {
            var nd = _nodes[i];

            if (nd.isStation)      DrawStation(nd);
            else if (nd.isDeadEnd) DrawDeadEnd(nd, i);
            else if (nd.isBroken)  DrawBroken(nd);
            else if (nd.isSwitch)  DrawSwitch(nd, i);
            else                   DrawPlainNode(nd);

            if (nd.hasStar) DrawStar(nd);

            if (nd.rt != null)
            {
                UITween.PopIn(nd.rt, 0.32f, 0.5f, delay);
                delay += 0.04f;
            }
        }
    }

    RectTransform Pill(RectTransform p, string n, Color col, Vector2 mid,
                       float len, float th, float angleDeg, float corner = 1f)
    {
        var rt = KidUI.RoundImg(p, n, col,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            mid, new Vector2(len, th), corner);
        rt.localRotation = Quaternion.Euler(0f, 0f, angleDeg);
        rt.GetComponent<Image>().raycastTarget = false;
        return rt;
    }

    RectTransform Circle(RectTransform p, string n, Color col, Vector2 pos, float size)
    {
        var rt = KidUI.CircleAt(p, n, col, new Vector2(0.5f, 0.5f), size);
        rt.anchoredPosition = pos;
        rt.GetComponent<Image>().raycastTarget = false;
        return rt;
    }

    void DrawEdge(int a, int b)
    {
        Vector2 pa = _nodes[a].pos, pb = _nodes[b].pos;
        Vector2 mid = (pa + pb) * 0.5f;
        float len = Vector2.Distance(pa, pb);
        float ang = Mathf.Atan2(pb.y - pa.y, pb.x - pa.x) * Mathf.Rad2Deg;

        Pill(_board, "EdgeBase", RAIL_DARK, mid, len + 10f, 24f, ang, 0.9f);
        var stripe = Pill(_board, "EdgeStripe", RAIL_DIM, mid, len - 18f, 8f, ang, 4f);

        _edges.Add(new RailEdge { a = a, b = b, stripe = stripe.GetComponent<Image>() });
    }

    void DrawPlainNode(RailNode nd)
    {
        nd.rt = Circle(_board, "Node", new Color(0.50f, 0.60f, 0.85f), nd.pos, 18f);
    }

    void DrawStation(RailNode nd)
    {
        nd.rt = Circle(_board, "Station", KidUI.GOOD, nd.pos, 52f);
        Circle(nd.rt, "Inner", new Color(1f, 1f, 1f, 0.85f), Vector2.zero, 24f);
        var lbl = KidUI.Txt(_board, "MetaLbl", "META", KidUI.GOOD, 24,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        var lrt = (RectTransform)lbl.transform;
        lrt.anchoredPosition = nd.pos + new Vector2(0f, -48f);
        lrt.sizeDelta = new Vector2(140f, 34f);
        lbl.fontStyle = FontStyles.Bold;
        nd.rt.gameObject.AddComponent<FloatBob>().Configure(4f, 1.2f);
    }

    void DrawDeadEnd(RailNode nd, int idx)
    {
        nd.rt = Circle(_board, "DeadEnd", new Color(0.45f, 0.30f, 0.35f), nd.pos, 22f);
        // Tope de via perpendicular a su carril de entrada
        float ang = 0f;
        foreach (var e in _edges)
            if (e.b == idx)
            {
                Vector2 d = nd.pos - _nodes[e.a].pos;
                ang = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
            }
        Pill(_board, "Buffer", KidUI.BAD, nd.pos + new Vector2(0f, 0f), 10f, 38f, ang, 2f);
    }

    void DrawBroken(RailNode nd)
    {
        // Cruz roja: tramo roto
        nd.rt = Circle(_board, "Broken", new Color(0.30f, 0.10f, 0.14f), nd.pos, 26f);
        Pill(_board, "BrkX1", KidUI.BAD, nd.pos, 40f, 9f, 45f, 3f);
        Pill(_board, "BrkX2", KidUI.BAD, nd.pos, 40f, 9f, -45f, 3f);
    }

    void DrawSwitch(RailNode nd, int idx)
    {
        nd.rt = Circle(_board, "Switch", KidUI.WARN, nd.pos, 58f);
        var img = nd.rt.GetComponent<Image>();
        img.raycastTarget = true;

        Circle(nd.rt, "SwInner", new Color(1f, 1f, 1f, 0.16f), Vector2.zero, 44f);

        // Flecha giratoria: eje + punta
        var arrowGO = new GameObject("Arrow");
        arrowGO.transform.SetParent(nd.rt, false);
        nd.arrow = arrowGO.AddComponent<RectTransform>();
        nd.arrow.anchorMin = nd.arrow.anchorMax = new Vector2(0.5f, 0.5f);
        nd.arrow.pivot = new Vector2(0.5f, 0.5f);
        nd.arrow.sizeDelta = Vector2.zero;
        Pill(nd.arrow, "Shaft", Color.white, new Vector2(12f, 0f), 26f, 8f, 0f, 4f);
        Pill(nd.arrow, "Head1", Color.white, new Vector2(26f, 5f), 15f, 7f, -42f, 4f);
        Pill(nd.arrow, "Head2", Color.white, new Vector2(26f, -5f), 15f, 7f, 42f, 4f);

        nd.btn = nd.rt.gameObject.AddComponent<Button>();
        nd.btn.targetGraphic = img;
        int captured = idx;
        nd.btn.onClick.AddListener(() => ToggleSwitch(captured));
        ButtonJuice.Attach(nd.rt.gameObject);

        AimArrow(nd, true);
    }

    void DrawStar(RailNode nd)
    {
        // Estrella dibujada con formas (sin depender de glifos de la fuente):
        // halo + dos cuadrados redondeados girados 45 grados entre si.
        var holder = new GameObject("Star");
        holder.transform.SetParent(_board, false);
        var srt = holder.AddComponent<RectTransform>();
        srt.anchorMin = srt.anchorMax = new Vector2(0.5f, 0.5f);
        srt.pivot = new Vector2(0.5f, 0.5f);
        srt.anchoredPosition = nd.pos + new Vector2(0f, 42f);
        srt.sizeDelta = Vector2.zero;

        Circle(srt, "Halo", new Color(STAR_YEL.r, STAR_YEL.g, STAR_YEL.b, 0.22f),
               Vector2.zero, 48f);
        Pill(srt, "Sq1", STAR_YEL, Vector2.zero, 24f, 24f, 0f, 1.6f);
        Pill(srt, "Sq2", STAR_YEL, Vector2.zero, 24f, 24f, 45f, 1.6f);

        holder.AddComponent<FloatBob>().Configure(6f, 1.6f);
        nd.starGO = holder;
    }

    void BuildTrain()
    {
        var trainGO = new GameObject("Train");
        trainGO.transform.SetParent(_board, false);
        _trainRT = trainGO.AddComponent<RectTransform>();
        _trainRT.anchorMin = _trainRT.anchorMax = new Vector2(0.5f, 0.5f);
        _trainRT.pivot = new Vector2(0.5f, 0.5f);
        _trainRT.sizeDelta = Vector2.zero;
        _trainRT.anchoredPosition = _nodes[_startIdx].pos;

        Circle(_trainRT, "WheelL", new Color(0.08f, 0.10f, 0.18f), new Vector2(-13f, -20f), 16f);
        Circle(_trainRT, "WheelR", new Color(0.08f, 0.10f, 0.18f), new Vector2(13f, -20f), 16f);
        var body = Circle(_trainRT, "Body", ACCENT2, Vector2.zero, 54f);
        Circle(body, "Win1", Color.white, new Vector2(-10f, 5f), 14f);
        Circle(body, "Win2", Color.white, new Vector2(10f, 5f), 14f);
        Circle(body, "Light", STAR_YEL, new Vector2(21f, -6f), 10f);

        _trainRT.SetAsLastSibling();
        UITween.PopIn(_trainRT, 0.4f, 0.4f, 0.2f);
    }

    // ================================================================ LOGICA

    List<int> ComputeRoute()
    {
        var list = new List<int>();
        int cur = _startIdx, guard = 0;
        while (cur >= 0 && guard++ < 80)
        {
            list.Add(cur);
            var nd = _nodes[cur];
            if (nd.isStation || nd.isDeadEnd || nd.isBroken) break;
            cur = nd.isSwitch ? (nd.state == 0 ? nd.outA : nd.outB) : nd.next;
        }
        return list;
    }

    void ToggleSwitch(int idx)
    {
        if (_phase != Phase.Planning) return;
        var nd = _nodes[idx];
        nd.state = 1 - nd.state;
        GameFeel.PlayPop();
        UITween.PulseOnce(nd.rt, 1.18f, 0.20f);
        AimArrow(nd, false);
        RefreshRouteHighlight();
    }

    void AimArrow(RailNode nd, bool instant)
    {
        int target = nd.state == 0 ? nd.outA : nd.outB;
        if (target < 0 || nd.arrow == null) return;
        Vector2 d = _nodes[target].pos - nd.pos;
        float ang = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
        if (instant) nd.arrow.localRotation = Quaternion.Euler(0f, 0f, ang);
        else StartCoroutine(RotateArrow(nd.arrow, ang));
    }

    IEnumerator RotateArrow(RectTransform arrow, float targetAng)
    {
        Quaternion from = arrow.localRotation;
        Quaternion to   = Quaternion.Euler(0f, 0f, targetAng);
        float t = 0f;
        while (t < 0.18f)
        {
            if (arrow == null) yield break;
            t += Time.unscaledDeltaTime;
            arrow.localRotation = Quaternion.Slerp(from, to,
                Mathf.SmoothStep(0f, 1f, t / 0.18f));
            yield return null;
        }
        if (arrow != null) arrow.localRotation = to;
    }

    void RefreshRouteHighlight()
    {
        var route = ComputeRoute();
        var onSet = new HashSet<long>();
        for (int i = 1; i < route.Count; i++)
            onSet.Add((long)route[i - 1] * 1000 + route[i]);

        foreach (var e in _edges)
        {
            bool on = onSet.Contains((long)e.a * 1000 + e.b);
            if (e.stripe != null)
                e.stripe.color = on ? ACCENT2 : RAIL_DIM;
        }
    }

    void OnGoPressed()
    {
        if (_phase != Phase.Planning || !IsPlaying) return;
        float planMs = (Time.realtimeSinceStartup - _planStart) * 1000f;
        _launches++;
        StartCoroutine(DriveTrain(ComputeRoute(), planMs));
    }

    IEnumerator DriveTrain(List<int> route, float planMs)
    {
        _phase = Phase.Driving;
        _goBtn.interactable = false;
        SetSwitchesInteractable(false);
        _msgLbl.text  = "¡Chu-chuu! El tren esta en camino…";
        _msgLbl.color = ACCENT2;

        for (int i = 1; i < route.Count; i++)
        {
            Vector2 a = _nodes[route[i - 1]].pos;
            Vector2 b = _nodes[route[i]].pos;
            float dur = Vector2.Distance(a, b) / 380f;
            float t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                _trainRT.anchoredPosition =
                    Vector2.Lerp(a, b, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / dur)));
                yield return null;
            }

            var nd = _nodes[route[i]];
            if (nd.hasStar && !nd.starTaken)
            {
                nd.starTaken = true;
                _roundStars++;
                GameFeel.PlayStar();
                GameFeel.FloatingText("+50", STAR_YEL,
                    BoardToScreen(nd.pos + new Vector2(0f, 60f)), 44f);
                if (nd.starGO != null) nd.starGO.SetActive(false);
                UpdateHUD();
            }
        }

        var last = _nodes[route[route.Count - 1]];
        if (last.isStation) yield return RoundSuccess(planMs);
        else                yield return RoundFail(planMs, last.isBroken);
    }

    IEnumerator RoundSuccess(float planMs)
    {
        ReportEvent(true, planMs);
        _starsCollected += _roundStars;
        int roundScore = 150 + _roundStars * 50;
        _score += roundScore;

        GameFeel.PlaySuccess();
        GameFeel.Confetti(30);
        UITween.PulseOnce(_trainRT, 1.3f, 0.3f);
        GameFeel.FloatingText("¡Ha llegado! +" + roundScore, KidUI.GOOD,
            new Vector2(0f, 60f));
        _msgLbl.text  = "¡Viaje " + (_round + 1) + " completado!";
        _msgLbl.color = KidUI.GOOD;
        UpdateHUD();

        yield return new WaitForSeconds(1.4f);

        if (_round >= ROUNDS - 1) FinishGame();
        else                      BuildRound(_round + 1);
    }

    IEnumerator RoundFail(float planMs, bool broken)
    {
        ReportEvent(false, planMs);
        _errors++;
        GameFeel.Error(_trainRT);
        _msgLbl.text = broken
            ? "¡Oh no, la via esta rota! Busca otro camino"
            : "¡Via muerta! Gira los desvios y prueba otra vez";
        _msgLbl.color = KidUI.WARN;
        UpdateHUD();

        // Las estrellas recogidas en un viaje fallido vuelven a su sitio
        foreach (var nd in _nodes)
            if (nd.starTaken)
            {
                nd.starTaken = false;
                if (nd.starGO != null) nd.starGO.SetActive(true);
            }
        _roundStars = 0;

        if (_errors >= MAX_ERRORS)
        {
            _phase = Phase.Over;
            yield return new WaitForSeconds(0.9f);
            FailMinigame();
            ShowResults(false, 0, _score,
                new[]
                {
                    "Viajes completados: " + _round + " / " + ROUNDS,
                    "Estrellas recogidas: " + _starsCollected,
                    "Intentos gastados: " + _launches
                },
                "¡Casi!",
                "Mira bien las flechas de los desvios antes de salir");
            yield break;
        }

        yield return new WaitForSeconds(1.0f);

        // El tren vuelve al inicio y se replanifica LA MISMA ronda
        _trainRT.anchoredPosition = _nodes[_startIdx].pos;
        UITween.PopIn(_trainRT, 0.35f, 0.4f);
        _phase = Phase.Planning;
        _planStart = Time.realtimeSinceStartup;
        SetSwitchesInteractable(true);
        _goBtn.interactable = true;
        UpdateHUD();
    }

    void FinishGame()
    {
        _phase = Phase.Over;
        float ratio = _launches > 0 ? Mathf.Clamp01((float)ROUNDS / _launches) : 1f;

        CompleteMinigame(_score);
        ShowResults(true, GameFeel.StarsFromRatio(true, ratio), _score,
            new[]
            {
                "Viajes completados: " + ROUNDS + " / " + ROUNDS,
                "Estrellas recogidas: " + _starsCollected + " / " + _starsTotalSeen,
                "Salidas del tren: " + _launches
            },
            _launches == ROUNDS ? "¡Maquinista perfecto!" : "¡Buen viaje!",
            "Los trenes de Attentia vuelven a circular");
    }

    // ================================================================ AUX

    void SetSwitchesInteractable(bool value)
    {
        foreach (var nd in _nodes)
            if (nd.btn != null) nd.btn.interactable = value;
    }

    void UpdateHUD()
    {
        if (_roundLbl != null)
            _roundLbl.text = "Viaje " + (_round + 1) + " / " + ROUNDS;
        if (_starLbl != null)
            _starLbl.text = "Estrellas: " + (_starsCollected + _roundStars);
        if (_tryDots != null)
            for (int i = 0; i < _tryDots.Length; i++)
                _tryDots[i].color = i < MAX_ERRORS - _errors
                    ? KidUI.GOOD
                    : new Color(1f, 1f, 1f, 0.15f);
    }

    /// <summary>Convierte coordenadas del tablero a coords de pantalla
    /// (canvas 1920x1080, respecto al centro) para FloatingText.</summary>
    Vector2 BoardToScreen(Vector2 boardPos)
    {
        return boardPos + new Vector2(0f, (0.47f - 0.5f) * 1080f);
    }
}
