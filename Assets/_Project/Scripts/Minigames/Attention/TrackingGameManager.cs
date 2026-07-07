// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Minijuego de atencion: SEGUIMIENTO DE OBJETOS MULTIPLES (MOT clasico).
/// Aparecen N bolas iguales; algunas se iluminan como "amigas" durante 2 s;
/// despues todas se mueven y se mezclan rebotando por la pantalla; al pararse,
/// el nino debe tocar las que eran amigas.
///
///  - Facil : 4 bolas, 1 amiga, 6 s de movimiento lento, 4 rondas.
///  - Medio : 5 bolas, 2 amigas, 8 s, 5 rondas.
///  - Dificil: 7 bolas, 3 amigas, 10 s mas rapido, 6 rondas.
/// </summary>
public class TrackingGameManager : MinigameBase
{
    class Ball
    {
        public RectTransform  rt;
        public Image          halo;
        public Image          core;
        public Image          ring;
        public ObjectMover    mover;
        public bool           isFriend;
        public bool           resolved;
    }

    // --- Config (ApplyDifficulty) ------------------------------------------------
    int   _ballCount    = 4;
    int   _friendCount  = 1;
    int   _totalRounds  = 4;
    float _moveDuration = 6f;
    float _ballSpeed    = 200f;

    const float BALL_SIZE = 110f;
    const float BOUND_X   = 800f;
    const float BOUND_Y_MIN = -420f;
    const float BOUND_Y_MAX =  310f;

    // --- Estado ---------------------------------------------------------------------
    TrackingUIController _ui;
    readonly List<Ball> _balls = new List<Ball>();

    int   _round;
    int   _totalHits;
    int   _roundHits;
    int   _picksLeft;
    int   _score;
    bool  _picking;
    float _pickStart;
    float _rtSumMs;
    int   _rtCount;

    static readonly Color BALL_COL   = new Color(0.32f, 0.58f, 0.98f);
    static readonly Color FRIEND_COL = new Color(0.98f, 0.80f, 0.10f);
    static readonly Color HIT_COL    = new Color(0.25f, 0.90f, 0.52f);
    static readonly Color MISS_COL   = new Color(0.90f, 0.28f, 0.30f);
    static readonly Color PHASE_LOOK = new Color(0.95f, 0.65f, 0.10f);
    static readonly Color PHASE_MOVE = new Color(0.28f, 0.55f, 0.95f);
    static readonly Color PHASE_TAP  = new Color(0.18f, 0.78f, 0.45f);

    // --- MinigameBase ------------------------------------------------------------------
    protected override string GetIntroDescription() =>
        "Algunas bolas son tus AMIGAS y brillan un momento.\n" +
        "¡No las pierdas de vista cuando se muevan y se mezclen!\n\n" +
        "Cuando se paren, toca a las amigas.";

    void ApplyDifficulty()
    {
        var diff = GameManager.Instance != null
            ? GameManager.Instance.CurrentDifficulty
            : DifficultyLevel.Easy;

        switch (diff)
        {
            case DifficultyLevel.Medium:
                _ballCount = 5; _friendCount = 2; _totalRounds = 5;
                _moveDuration = 8f; _ballSpeed = 270f;
                break;
            case DifficultyLevel.Hard:
                _ballCount = 7; _friendCount = 3; _totalRounds = 6;
                _moveDuration = 10f; _ballSpeed = 350f;
                break;
            default:
                _ballCount = 4; _friendCount = 1; _totalRounds = 4;
                _moveDuration = 6f; _ballSpeed = 200f;
                break;
        }
    }

    protected override void OnMinigameStart()
    {
        ApplyDifficulty();
        KidUI.EnsureEventSystem();

        _ui = GetComponent<TrackingUIController>();

        _round     = 0;
        _totalHits = 0;
        _score     = 0;
        _rtSumMs   = 0f;
        _rtCount   = 0;

        _ui.BuildUI(_totalRounds);
        _ui.SetScore(0);

        StartCoroutine(RunRound());
    }

    protected override void OnMinigameComplete() { }
    protected override void OnMinigameFailed()   { }

    // --- Ronda ---------------------------------------------------------------------------
    IEnumerator RunRound()
    {
        yield return new WaitForSeconds(0.4f);

        SpawnBalls();
        _roundHits = 0;

        // FASE 1: memorizar (las amigas brillan 2 s)
        _ui.SetPhase("¡MIRA!", PHASE_LOOK);
        foreach (var b in _balls)
            if (b.isFriend) SetHighlight(b, true);

        float t = 0f;
        while (t < 2.0f)
        {
            t += Time.deltaTime;
            // Parpadeo suave de las amigas para atraer la mirada
            float k = 0.75f + 0.25f * Mathf.Sin(t * 9f);
            foreach (var b in _balls)
                if (b.isFriend && b.rt != null)
                    b.rt.localScale = Vector3.one * k * 1.1f;
            yield return null;
        }
        foreach (var b in _balls)
        {
            if (b.rt != null) b.rt.localScale = Vector3.one;
            if (b.isFriend) SetHighlight(b, false);
        }

        yield return new WaitForSeconds(0.45f);

        // FASE 2: movimiento y mezcla
        _ui.SetPhase("SIGUE...", PHASE_MOVE);
        foreach (var b in _balls) b.mover.Launch();
        yield return new WaitForSeconds(_moveDuration);
        foreach (var b in _balls) b.mover.StopMoving();

        // FASE 3: eleccion
        _ui.SetPhase("¡TOCA!", PHASE_TAP);
        GameFeel.PlayPop();
        _picksLeft = _friendCount;
        _pickStart = Time.time;
        _picking   = true;

        while (_picksLeft > 0 && IsPlaying) yield return null;
        _picking = false;

        // Revelar amigas no encontradas
        bool perfect = _roundHits == _friendCount;
        foreach (var b in _balls)
            if (b.isFriend && !b.resolved) SetHighlight(b, true);

        if (perfect)
        {
            _score += 50;   // bonus de ronda perfecta
            _ui.SetScore(_score);
            GameFeel.FloatingText("¡Ronda perfecta! +50", HIT_COL);
        }
        _ui.SetRoundDot(_round, perfect ? HIT_COL
                        : _roundHits > 0 ? FRIEND_COL : MISS_COL);

        yield return new WaitForSeconds(1.2f);
        ClearBalls();

        _round++;
        if (_round >= _totalRounds) EndGame();
        else                        StartCoroutine(RunRound());
    }

    // --- Bolas ----------------------------------------------------------------------------
    void SpawnBalls()
    {
        ClearBalls();

        var positions = PlaceWithoutOverlap(_ballCount, BALL_SIZE * 1.35f);

        // Elegir amigas al azar
        var friendIdx = new HashSet<int>();
        while (friendIdx.Count < _friendCount)
            friendIdx.Add(Random.Range(0, _ballCount));

        for (int i = 0; i < _ballCount; i++)
        {
            var go = new GameObject("Ball" + i);
            go.transform.SetParent(_ui.PlayAreaRT, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(BALL_SIZE, BALL_SIZE);
            rt.anchoredPosition = positions[i];

            var halo = go.AddComponent<Image>();
            halo.color = new Color(BALL_COL.r, BALL_COL.g, BALL_COL.b, 0.20f);

            // Anillo de resaltado (oculto por defecto)
            var ringRT = KidUI.Img(rt, "Ring", Color.white,
                                   Vector2.zero, Vector2.one,
                                   Vector2.zero, new Vector2(4f, 4f));
            var ring = ringRT.GetComponent<Image>();
            ring.raycastTarget = false;
            ring.enabled = false;

            var coreRT = KidUI.Img(rt, "Core", BALL_COL,
                                   Vector2.zero, Vector2.one,
                                   Vector2.zero, new Vector2(-14f, -14f));
            var core = coreRT.GetComponent<Image>();
            core.raycastTarget = false;

            KidUI.Img(coreRT, "Shine", new Color(1f, 1f, 1f, 0.20f),
                      new Vector2(0.12f, 0.58f), new Vector2(0.52f, 0.88f),
                      Vector2.zero, Vector2.zero)
                 .GetComponent<Image>().raycastTarget = false;

            var mover = go.AddComponent<ObjectMover>();
            mover.ObjectRT   = rt;
            mover.Speed      = _ballSpeed * Random.Range(0.9f, 1.1f);
            mover.boundsXMin = -BOUND_X;
            mover.boundsXMax =  BOUND_X;
            mover.boundsYMin =  BOUND_Y_MIN;
            mover.boundsYMax =  BOUND_Y_MAX;

            var ball = new Ball
            {
                rt = rt, halo = halo, core = core, ring = ring,
                mover = mover, isFriend = friendIdx.Contains(i)
            };

            var det = go.AddComponent<TrackingDetector>();
            det.OnTapped = () => HandleBallTap(ball);
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = halo;
            btn.onClick.AddListener(det.Tap);

            UITween.PopIn(rt, 0.35f, 0.4f, i * 0.04f);
            _balls.Add(ball);
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
                    var p = new Vector2(Random.Range(-BOUND_X, BOUND_X),
                                        Random.Range(BOUND_Y_MIN, BOUND_Y_MAX));
                    bool ok = true;
                    for (int j = 0; j < result.Count; j++)
                        if (Vector2.Distance(p, result[j]) < dist) { ok = false; break; }
                    if (ok) { result.Add(p); placed = true; break; }
                }
                if (!placed) break;
            }
            if (result.Count < count) dist *= 0.92f;
            safety++;
        }
        while (result.Count < count)
            result.Add(new Vector2(Random.Range(-BOUND_X, BOUND_X),
                                   Random.Range(BOUND_Y_MIN, BOUND_Y_MAX)));
        return result;
    }

    void SetHighlight(Ball b, bool on)
    {
        if (b.core == null) return;
        b.core.color   = on ? FRIEND_COL : BALL_COL;
        b.ring.enabled = on;
        b.ring.color   = Color.white;
        b.halo.color   = on
            ? new Color(FRIEND_COL.r, FRIEND_COL.g, FRIEND_COL.b, 0.35f)
            : new Color(BALL_COL.r, BALL_COL.g, BALL_COL.b, 0.20f);
    }

    void ClearBalls()
    {
        foreach (var b in _balls)
            if (b.rt != null) Destroy(b.rt.gameObject);
        _balls.Clear();
    }

    // --- Interaccion ---------------------------------------------------------------------------
    void HandleBallTap(Ball b)
    {
        if (!IsPlaying || !_picking || b.resolved || _picksLeft <= 0) return;
        b.resolved = true;
        _picksLeft--;

        float rtMs = (Time.time - _pickStart) * 1000f;
        ReportEvent(b.isFriend, rtMs);

        if (b.isFriend)
        {
            _totalHits++;
            _roundHits++;
            _score += 100;
            _ui.SetScore(_score);
            _rtSumMs += rtMs;
            _rtCount++;

            b.core.color   = HIT_COL;
            b.ring.enabled = true;
            b.ring.color   = HIT_COL;
            b.halo.color   = new Color(HIT_COL.r, HIT_COL.g, HIT_COL.b, 0.35f);
            GameFeel.Success(b.rt);
        }
        else
        {
            b.core.color = MISS_COL;
            b.halo.color = new Color(MISS_COL.r, MISS_COL.g, MISS_COL.b, 0.30f);
            GameFeel.Error(b.rt);
        }
    }

    // --- Fin -------------------------------------------------------------------------------------
    void EndGame()
    {
        int   totalFriends = _totalRounds * _friendCount;
        float ratio        = totalFriends > 0 ? (float)_totalHits / totalFriends : 0f;
        bool  success      = ratio >= 0.6f;
        int   stars        = GameFeel.StarsFromRatio(success, ratio);

        if (success) CompleteMinigame(_score);
        else         FailMinigame();

        string rtStat = _rtCount > 0
            ? "Reaccion media: " + Mathf.RoundToInt(_rtSumMs / _rtCount) + " ms"
            : "Reaccion media: -";

        ShowResults(success, stars, _score,
            new[] { "Amigas encontradas: " + _totalHits + "/" + totalFriends, rtStat },
            null,
            success ? "¡No se te escapa ni una!" : "Sigue a las bolas con los ojos, sin parpadear");
    }
}
