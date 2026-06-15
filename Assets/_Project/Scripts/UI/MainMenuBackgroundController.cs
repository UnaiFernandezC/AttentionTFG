using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuBackgroundController : MonoBehaviour
{

    static Color   C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);
    static Vector2 V(float x, float y) => new Vector2(x, y);

    static readonly Color BG      = C(0.05f, 0.08f, 0.14f);
    static readonly Color ACCENT  = C(0.18f, 0.80f, 0.58f);
    static readonly Color ACCENT2 = C(0.12f, 0.45f, 0.85f);

    [Header("Partículas")]
    public int   particleCount    = 32;
    public float particleMinSpeed = 28f;
    public float particleMaxSpeed = 68f;

    [Header("Nodos")]
    public int  nodeCount           = 11;
    public float nodeConnectionDist = 0.32f;

    [Header("Escáner")]
    public float scanPeriod = 10f;

    [Header("Estrellas fugaces")]
    public float shootingStarInterval = 4.5f;

    struct Particle
    {
        public RectTransform rt;
        public Image         img;
        public float         speed, phase, homeX;
    }

    struct Node
    {
        public RectTransform rt;
        public Image         img;
        public float         phase;
        public Color         baseCol;
    }

    struct Connection
    {
        public RectTransform rt;
        public Image         img;
    }

    RectTransform _root;
    Particle[]    _particles;
    Node[]        _nodes;
    Connection[]  _connections;
    Image[]       _hLines, _vLines;
    Image         _centerGlow;
    RectTransform _scanLine;

    const float RW = 1920f, RH = 1080f;

    float _nextStar;

    void Awake()
    {
        Build();
        _nextStar = Time.time + Random.Range(1f, shootingStarInterval);
    }

    void Build()
    {

        var cGO = new GameObject("BG_Canvas");
        cGO.transform.SetParent(transform, false);
        var cv = cGO.AddComponent<Canvas>();
        cv.renderMode   = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = -10;
        var sc = cGO.AddComponent<CanvasScaler>();
        sc.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = V(RW, RH);
        sc.matchWidthOrHeight  = 0.5f;
        _root = cGO.GetComponent<RectTransform>();

        MkImg(_root, "Base",  BG,                          V(0,0),    V(1,1),    V(0,0), V(0,0));
        MkImg(_root, "GradT", C(0f, 0.04f, 0.16f, 0.45f), V(0,0.55f),V(1,1),   V(0,0), V(0,0));
        MkImg(_root, "GradB", C(0f, 0.02f, 0.08f, 0.38f), V(0,0),    V(1,0.4f),V(0,0), V(0,0));

        MkImg(_root, "VigL",  C(0,0,0,0.22f), V(0,0),    V(0.15f,1), V(0,0), V(0,0));
        MkImg(_root, "VigR",  C(0,0,0,0.22f), V(0.85f,0),V(1,1),     V(0,0), V(0,0));

        BuildGrid();

        var gGO = new GameObject("CtrGlow");
        gGO.transform.SetParent(_root, false);
        var gRT = gGO.AddComponent<RectTransform>();
        gRT.anchorMin = V(0.22f, 0.12f);
        gRT.anchorMax = V(0.78f, 0.88f);
        gRT.offsetMin = gRT.offsetMax = Vector2.zero;
        _centerGlow = gGO.AddComponent<Image>();
        _centerGlow.color = C(0.08f, 0.28f, 0.55f, 0);
        _centerGlow.raycastTarget = false;

        BuildParticles();

        BuildNodes();
        BuildConnections();

        var sGO = new GameObject("ScanLine");
        sGO.transform.SetParent(_root, false);
        _scanLine = sGO.AddComponent<RectTransform>();
        _scanLine.anchorMin = _scanLine.anchorMax = V(0.5f, 1f);
        _scanLine.pivot     = V(0.5f, 0.5f);
        _scanLine.sizeDelta = V(0, 2.5f);
        _scanLine.anchorMin = V(0, 1f);
        _scanLine.anchorMax = V(1, 1f);
        _scanLine.anchoredPosition = Vector2.zero;
        var sImg = sGO.AddComponent<Image>();
        sImg.color = C(ACCENT.r, ACCENT.g, ACCENT.b, 0.07f);
        sImg.raycastTarget = false;
    }

    void BuildGrid()
    {
        int h = 8, v = 8;
        _hLines = new Image[h];
        _vLines = new Image[v];
        for (int i = 0; i < h; i++)
        {
            float t = (i + 1f) / (h + 1f);
            _hLines[i] = MkImg(_root, "GH"+i, C(1,1,1,0.020f),
                               V(0, t-0.0007f), V(1, t+0.0007f), V(0,0), V(0,0))
                         .GetComponent<Image>();
        }
        for (int i = 0; i < v; i++)
        {
            float t = (i + 1f) / (v + 1f);
            _vLines[i] = MkImg(_root, "GV"+i, C(1,1,1,0.020f),
                               V(t-0.0004f, 0), V(t+0.0004f, 1), V(0,0), V(0,0))
                         .GetComponent<Image>();
        }
    }

    void BuildParticles()
    {
        _particles = new Particle[particleCount];
        for (int i = 0; i < particleCount; i++)
        {
            float x   = Random.Range(0.02f, 0.98f);
            float y   = Random.Range(0f, 1f);
            float sz  = Random.Range(3f, 10f);
            float spd = Random.Range(particleMinSpeed, particleMaxSpeed);

            var go  = new GameObject("Pt_"+i);
            go.transform.SetParent(_root, false);
            var rt  = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = V(x, y);
            rt.pivot     = V(0.5f, 0.5f);
            rt.sizeDelta = V(sz, sz);
            rt.anchoredPosition = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.sprite = CircleSprite(16);

            Color col = Random.value > 0.55f ? ACCENT : ACCENT2;
            img.color = new Color(col.r, col.g, col.b, 0f);
            img.raycastTarget = false;

            _particles[i] = new Particle
            {
                rt = rt, img = img,
                speed = spd,
                phase = Random.Range(0f, Mathf.PI * 2f),
                homeX = x
            };
        }
    }

    static readonly float[] NX = { 0.10f, 0.22f, 0.40f, 0.60f, 0.78f, 0.90f,
                                    0.15f, 0.50f, 0.72f, 0.32f, 0.55f };
    static readonly float[] NY = { 0.72f, 0.28f, 0.60f, 0.18f, 0.52f, 0.78f,
                                    0.42f, 0.88f, 0.32f, 0.12f, 0.45f };

    void BuildNodes()
    {
        _nodes = new Node[nodeCount];
        for (int i = 0; i < nodeCount; i++)
        {
            float x  = i < NX.Length ? NX[i] : Random.Range(0.05f, 0.95f);
            float y  = i < NY.Length ? NY[i] : Random.Range(0.05f, 0.95f);
            float sz = Random.Range(7f, 16f);

            var go  = new GameObject("Nd_"+i);
            go.transform.SetParent(_root, false);
            var rt  = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = V(x, y);
            rt.pivot     = V(0.5f, 0.5f);
            rt.sizeDelta = V(sz, sz);
            rt.anchoredPosition = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.sprite = CircleSprite(32);
            Color col = Random.value > 0.45f ? ACCENT : ACCENT2;
            img.color = new Color(col.r, col.g, col.b, 0f);
            img.raycastTarget = false;

            _nodes[i] = new Node
            {
                rt = rt, img = img,
                phase   = Random.Range(0f, Mathf.PI * 2f),
                baseCol = col
            };
        }
    }

    void BuildConnections()
    {
        var list = new List<Connection>();
        for (int a = 0; a < nodeCount; a++)
        for (int b = a + 1; b < nodeCount; b++)
        {
            Vector2 pa = new Vector2(NX[a < NX.Length ? a : 0],
                                     NY[a < NY.Length ? a : 0]);
            Vector2 pb = new Vector2(NX[b < NX.Length ? b : 0],
                                     NY[b < NY.Length ? b : 0]);
            if (Vector2.Distance(pa, pb) > nodeConnectionDist) continue;

            var conn = DrawLine(pa, pb, C(ACCENT.r,ACCENT.g,ACCENT.b,0.06f));
            list.Add(new Connection { rt = conn, img = conn.GetComponent<Image>() });
        }
        _connections = list.ToArray();
    }

    void Update()
    {
        float t = Time.time;

        for (int i = 0; i < _hLines.Length; i++)
        {
            float a = 0.012f + 0.014f * Mathf.Sin(t * 0.45f + i * 0.72f);
            _hLines[i].color = new Color(1,1,1,a);
        }
        for (int i = 0; i < _vLines.Length; i++)
        {
            float a = 0.012f + 0.014f * Mathf.Sin(t * 0.45f + i * 0.72f + 0.5f);
            _vLines[i].color = new Color(1,1,1,a);
        }

        for (int i = 0; i < _particles.Length; i++)
        {
            ref Particle p = ref _particles[i];
            float y = p.rt.anchorMin.y + p.speed * Time.deltaTime / RH;

            if (y > 1.06f)
            {

                y = -0.04f;
                p.homeX = Random.Range(0.03f, 0.97f);
                p.phase = Random.Range(0f, Mathf.PI * 2f);
            }

            float x = p.homeX + Mathf.Sin(t * 0.35f + p.phase) * 0.014f;
            p.rt.anchorMin = V(x, y);
            p.rt.anchorMax = V(x, y);

            float fade = y < 0.12f ? y / 0.12f :
                         y > 0.82f ? (1.06f - y) / 0.24f : 1f;
            float twinkle = 0.55f + 0.20f * Mathf.Sin(t * 2.1f + p.phase * 1.3f);
            float alpha = Mathf.Clamp01(fade * twinkle) * 0.42f;

            var c = p.img.color;
            p.img.color = new Color(c.r, c.g, c.b, alpha);
        }

        for (int i = 0; i < _nodes.Length; i++)
        {
            ref Node n = ref _nodes[i];
            float pulse = 0.5f + 0.5f * Mathf.Sin(t * 0.85f + n.phase);
            float alpha = Mathf.Lerp(0.08f, 0.65f, pulse);
            var   bc    = n.baseCol;
            n.img.color = new Color(bc.r, bc.g, bc.b, alpha);
            float sc    = Mathf.Lerp(0.82f, 1.20f, pulse);
            n.rt.localScale = Vector3.one * sc;
        }

        for (int i = 0; i < _connections.Length; i++)
        {
            ref Connection cn = ref _connections[i];
            float a = 0.04f + 0.04f * Mathf.Sin(t * 0.6f + i * 0.9f);
            var c = cn.img.color;
            cn.img.color = new Color(c.r, c.g, c.b, a);
        }

        if (_centerGlow)
        {
            float breath = 0.5f + 0.5f * Mathf.Sin(t * 0.38f);
            _centerGlow.color = new Color(0.06f, 0.22f, 0.52f, breath * 0.09f);
        }

        if (_scanLine)
        {
            float yPos = 1f - (t % scanPeriod) / scanPeriod;
            _scanLine.anchorMin = V(0, yPos);
            _scanLine.anchorMax = V(1, yPos);

            float cycleT = (t % scanPeriod) / scanPeriod;
            float scanAlpha = cycleT < 0.05f
                ? cycleT / 0.05f * 0.07f
                : cycleT > 0.90f
                    ? (1f - cycleT) / 0.10f * 0.07f
                    : 0.07f;
            _scanLine.GetComponent<Image>().color =
                new Color(ACCENT.r, ACCENT.g, ACCENT.b, scanAlpha);
        }

        if (t >= _nextStar)
        {
            SpawnShootingStar();
            _nextStar = t + Random.Range(shootingStarInterval * 0.6f,
                                         shootingStarInterval * 1.6f);
        }
    }

    void SpawnShootingStar()
    {

        float startX = Random.Range(0.1f, 0.9f);
        float startY = Random.Range(0.6f, 1.0f);
        float angle  = Random.Range(-38f, -22f);
        float length = Random.Range(80f, 160f);
        float duration = Random.Range(0.5f, 0.9f);

        var go = new GameObject("Star");
        go.transform.SetParent(_root, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = V(startX, startY);
        rt.pivot     = V(0f, 0.5f);
        rt.sizeDelta = V(length, 2f);
        rt.anchoredPosition = Vector2.zero;
        rt.localRotation = Quaternion.Euler(0, 0, angle);
        var img = go.AddComponent<Image>();
        img.color = new Color(ACCENT.r, ACCENT.g, ACCENT.b, 0.85f);
        img.raycastTarget = false;

        StartCoroutine(AnimateStar(rt, img, duration,
                                   V(startX, startY), angle));
    }

    System.Collections.IEnumerator AnimateStar(
        RectTransform rt, Image img, float duration,
        Vector2 start, float angle)
    {
        float speed = 380f;
        float elapsed = 0f;

        float rad     = angle * Mathf.Deg2Rad;
        float dx      = Mathf.Cos(rad) / RW;
        float dy      = Mathf.Sin(rad) / RH;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float norm = elapsed / duration;

            float alpha = norm < 0.15f
                ? norm / 0.15f
                : 1f - (norm - 0.15f) / 0.85f;
            img.color = new Color(ACCENT.r, ACCENT.g, ACCENT.b, alpha * 0.80f);

            float dist = speed * elapsed;
            float nx = start.x + dx * dist;
            float ny = start.y + dy * dist;
            rt.anchorMin = rt.anchorMax = V(nx, ny);

            yield return null;
        }

        Destroy(rt.gameObject);
    }

    RectTransform DrawLine(Vector2 fromN, Vector2 toN, Color col)
    {
        Vector2 p1 = new Vector2(fromN.x * RW, fromN.y * RH);
        Vector2 p2 = new Vector2(toN.x   * RW, toN.y   * RH);
        Vector2 diff = p2 - p1;
        float   len  = diff.magnitude;
        float   ang  = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
        Vector2 mid  = (p1 + p2) * 0.5f;

        var go  = new GameObject("Conn");
        go.transform.SetParent(_root, false);
        var rt  = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = V(mid.x / RW, mid.y / RH);
        rt.pivot     = V(0.5f, 0.5f);
        rt.sizeDelta = V(len, 1.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.localRotation    = Quaternion.Euler(0, 0, ang);
        var img = go.AddComponent<Image>();
        img.color = col;
        img.raycastTarget = false;
        return rt;
    }

    RectTransform MkImg(RectTransform p, string n, Color col,
                        Vector2 am, Vector2 aM, Vector2 pos, Vector2 sd)
    {
        var go = new GameObject(n);
        go.transform.SetParent(p, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = am; rt.anchorMax = aM;
        rt.pivot = V(0.5f, 0.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = sd;
        var img = go.AddComponent<Image>();
        img.color = col;
        img.raycastTarget = false;
        return rt;
    }

    static Sprite CircleSprite(int res)
    {
        var tex = new Texture2D(res, res, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float r = res * 0.5f;
        var px = new Color[res * res];
        for (int y = 0; y < res; y++)
        for (int x = 0; x < res; x++)
        {
            float d = Vector2.Distance(new Vector2(x+0.5f,y+0.5f), new Vector2(r,r));
            float a = Mathf.Clamp01(1f - (d - r + 1.5f) / 2f);
            px[y*res+x] = new Color(1,1,1,a);
        }
        tex.SetPixels(px); tex.Apply();
        return Sprite.Create(tex, new Rect(0,0,res,res), V(0.5f,0.5f));
    }
}
