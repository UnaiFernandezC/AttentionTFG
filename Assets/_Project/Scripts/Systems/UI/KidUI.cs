// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Helpers compartidos para construir UI por código (mismo estilo que
/// IntroPanel/GameSettingsMenu). Usado por la pantalla de perfiles,
/// el teclado de PIN y el panel del tutor.
/// </summary>
public static class KidUI
{
    public static readonly Color BG     = new Color(0.03f, 0.05f, 0.12f, 0.97f);
    public static readonly Color PANEL  = new Color(0.10f, 0.13f, 0.24f);
    public static readonly Color PANEL2 = new Color(0.08f, 0.11f, 0.22f);
    public static readonly Color ACCENT = new Color(0.30f, 0.65f, 1.00f);
    public static readonly Color GOOD   = new Color(0.18f, 0.80f, 0.58f);
    public static readonly Color WARN   = new Color(0.95f, 0.55f, 0.12f);
    public static readonly Color BAD    = new Color(0.90f, 0.22f, 0.28f);
    public static readonly Color DIM    = new Color(0.45f, 0.58f, 0.75f);
    public static readonly Color BTNC   = new Color(0.12f, 0.18f, 0.32f);

    /// <summary>Colores alegres para tarjetas de perfil, rotando por índice.</summary>
    public static readonly Color[] CARD_COLORS =
    {
        new Color(0.28f, 0.60f, 1.00f),
        new Color(0.18f, 0.80f, 0.58f),
        new Color(0.98f, 0.80f, 0.10f),
        new Color(0.95f, 0.55f, 0.12f),
        new Color(0.58f, 0.28f, 0.92f),
        new Color(0.92f, 0.35f, 0.55f)
    };

    // ================================================================
    //  SPRITES PROCEDURALES (esquinas redondeadas y círculos, sin assets)
    // ================================================================

    static UnityEngine.Sprite _roundedSprite, _circleSprite;

    /// <summary>Rectángulo redondeado 9-slice generado en runtime.</summary>
    public static UnityEngine.Sprite RoundedSprite
    {
        get { if (_roundedSprite == null) _roundedSprite = MakeRounded(64, 18); return _roundedSprite; }
    }

    /// <summary>Círculo antialiasado generado en runtime.</summary>
    public static UnityEngine.Sprite CircleSpr
    {
        get { if (_circleSprite == null) _circleSprite = MakeCircle(160); return _circleSprite; }
    }

    static UnityEngine.Sprite MakeRounded(int size, int radius)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Max(0f, Mathf.Max(radius - x, x - (size - 1 - radius)));
                float dy = Mathf.Max(0f, Mathf.Max(radius - y, y - (size - 1 - radius)));
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(radius - d + 0.75f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        tex.Apply();
        int b = radius + 4;
        return UnityEngine.Sprite.Create(tex, new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect,
            new Vector4(b, b, b, b));
    }

    static UnityEngine.Sprite MakeCircle(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        float c = (size - 1) / 2f;
        float r = size / 2f - 1.5f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(r - d + 0.75f)));
            }
        tex.Apply();
        return UnityEngine.Sprite.Create(tex, new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f), 100f);
    }

    /// <summary>Imagen con esquinas redondeadas (9-slice). cornerScale &gt;1 = radio menor.</summary>
    public static RectTransform RoundImg(RectTransform p, string n, Color col,
                                         Vector2 am, Vector2 aM, Vector2 pos, Vector2 sd,
                                         float cornerScale = 1f)
    {
        var rt = Img(p, n, col, am, aM, pos, sd);
        var img = rt.GetComponent<Image>();
        img.sprite = RoundedSprite;
        img.type = Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = cornerScale;
        return rt;
    }

    /// <summary>Círculo de color colocado por punto central (anclas) y tamaño en píxeles.</summary>
    public static RectTransform CircleAt(RectTransform p, string n, Color col,
                                         Vector2 anchorPoint, float sizePx)
    {
        var go = new GameObject(n);
        go.transform.SetParent(p, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchorPoint;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(sizePx, sizePx);
        var img = go.AddComponent<Image>();
        img.color = col;
        img.sprite = CircleSpr;
        return rt;
    }

    // ================================================================
    //  FONDO ESPACIAL (opaco): gradiente + nebulosas + estrellas + planeta
    // ================================================================

    public static RectTransform BuildSpaceBackground(RectTransform parent, bool withPlanet = true)
    {
        var bg = Img(parent, "SpaceBG", new Color(0.015f, 0.022f, 0.065f, 1f),
                     Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        bg.GetComponent<Image>().raycastTarget = true; // bloquea lo de detrás

        // Canvas anidado: aísla el parpadeo de estrellas del canvas de la UI
        // (el twinkle deja de provocar rebuilds del canvas principal cada frame).
        bg.gameObject.AddComponent<Canvas>();
        bg.gameObject.AddComponent<GraphicRaycaster>(); // conserva el bloqueo de clics

        // Nebulosas suaves
        CircleAt(bg, "Neb1", new Color(0.22f, 0.13f, 0.48f, 0.16f), new Vector2(0.16f, 0.78f), 950f);
        CircleAt(bg, "Neb2", new Color(0.06f, 0.26f, 0.52f, 0.14f), new Vector2(0.86f, 0.22f), 1150f);
        CircleAt(bg, "Neb3", new Color(0.45f, 0.16f, 0.42f, 0.09f), new Vector2(0.68f, 0.88f), 700f);
        CircleAt(bg, "Neb4", new Color(0.10f, 0.35f, 0.45f, 0.08f), new Vector2(0.35f, 0.15f), 800f);

        // Estrellas con parpadeo
        for (int i = 0; i < 55; i++)
        {
            var pos = new Vector2(Random.value, Random.value);
            float s = Random.Range(2.5f, 6.5f);
            float a = Random.Range(0.35f, 0.95f);
            var star = CircleAt(bg, "Star" + i, new Color(1f, 1f, 1f, a), pos, s);
            star.GetComponent<Image>().raycastTarget = false;
            star.gameObject.AddComponent<StarTwinkle>();
        }

        if (withPlanet)
        {
            // Planeta grande asomando por la esquina inferior izquierda
            var planet = CircleAt(bg, "Planet", new Color(0.13f, 0.22f, 0.42f, 1f),
                                  new Vector2(0.02f, -0.06f), 560f);
            planet.GetComponent<Image>().raycastTarget = false;
            var glow = CircleAt(planet, "Glow", new Color(0.35f, 0.55f, 0.95f, 0.20f),
                                new Vector2(0.68f, 0.72f), 300f);
            glow.GetComponent<Image>().raycastTarget = false;
            // Anillo
            var ring = RoundImg(planet, "Ring", new Color(0.55f, 0.70f, 1f, 0.22f),
                                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                                Vector2.zero, new Vector2(900f, 26f), 0.5f);
            ring.localRotation = Quaternion.Euler(0, 0, -16f);
            ring.GetComponent<Image>().raycastTarget = false;

            // Luna pequeña arriba a la derecha
            var moon = CircleAt(bg, "Moon", new Color(0.55f, 0.58f, 0.70f, 0.85f),
                                new Vector2(0.91f, 0.86f), 70f);
            moon.GetComponent<Image>().raycastTarget = false;
            moon.gameObject.AddComponent<FloatBob>().Configure(6f, 0.8f);
        }
        return bg;
    }

    // ================================================================
    //  TABLA SIMPLE (cabecera + filas alternadas)
    // ================================================================

    public static RectTransform Table(RectTransform p, Vector2 am, Vector2 aM,
                                      string[] headers,
                                      System.Collections.Generic.IList<string[]> rows,
                                      float[] weights = null, float fontSize = 18f)
    {
        var box = RoundImg(p, "Table", new Color(1f, 1f, 1f, 0.035f), am, aM,
                           Vector2.zero, Vector2.zero, 1.6f);
        int cols = headers.Length;
        if (weights == null)
        {
            weights = new float[cols];
            for (int i = 0; i < cols; i++) weights[i] = 1f;
        }
        float total = 0f;
        foreach (float w in weights) total += w;

        int nRows = rows.Count + 1;
        float rowH = 1f / nRows;

        // Cabecera
        var hdrBg = RoundImg(box, "HdrBg", new Color(ACCENT.r, ACCENT.g, ACCENT.b, 0.14f),
                             new Vector2(0f, 1f - rowH), Vector2.one, Vector2.zero, Vector2.zero, 1.6f);
        hdrBg.GetComponent<Image>().raycastTarget = false;
        TableRow(box, headers, 0, rowH, weights, total, true, fontSize);

        for (int r = 0; r < rows.Count; r++)
        {
            if (r % 2 == 1)
            {
                float y1 = 1f - (r + 1) * rowH;
                var alt = Img(box, "Alt" + r, new Color(1f, 1f, 1f, 0.03f),
                              new Vector2(0f, y1 - rowH), new Vector2(1f, y1),
                              Vector2.zero, Vector2.zero);
                alt.GetComponent<Image>().raycastTarget = false;
            }
            TableRow(box, rows[r], r + 1, rowH, weights, total, false, fontSize);
        }
        return box;
    }

    static void TableRow(RectTransform box, string[] cells, int rowIdx, float rowH,
                         float[] w, float total, bool header, float fs)
    {
        float y1 = 1f - rowIdx * rowH;
        float y0 = y1 - rowH;
        float x = 0f;
        for (int c = 0; c < cells.Length && c < w.Length; c++)
        {
            float frac = w[c] / total;
            var t = Txt(box, $"r{rowIdx}c{c}", cells[c] ?? "",
                        header ? ACCENT : (c == 0 ? DIM : Color.white), fs,
                        new Vector2(x + 0.02f, y0), new Vector2(x + frac - 0.01f, y1));
            t.alignment = c == 0 ? TextAlignmentOptions.MidlineLeft : TextAlignmentOptions.Center;
            if (header) t.fontStyle = FontStyles.Bold;
            x += frac;
        }
    }

    public static void EnsureEventSystem()
    {
        if (Object.FindObjectOfType<EventSystem>() != null) return;
        var esGO = new GameObject("EventSystem");
        esGO.AddComponent<EventSystem>();
        esGO.AddComponent<StandaloneInputModule>();
    }

    public static Canvas MakeCanvas(string name, int sortingOrder, Transform parent = null)
    {
        var cvGO = new GameObject(name);
        if (parent != null) cvGO.transform.SetParent(parent, false);
        var cv = cvGO.AddComponent<Canvas>();
        cv.renderMode   = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = sortingOrder;
        var sc = cvGO.AddComponent<CanvasScaler>();
        sc.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920f, 1080f);
        sc.matchWidthOrHeight  = 0.5f;
        cvGO.AddComponent<GraphicRaycaster>();
        return cv;
    }

    public static RectTransform Img(RectTransform p, string n, Color col,
                                    Vector2 am, Vector2 aM, Vector2 pos, Vector2 sd)
    {
        var go = new GameObject(n);
        go.transform.SetParent(p, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = am; rt.anchorMax = aM; rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = sd;
        go.AddComponent<Image>().color = col;
        return rt;
    }

    public static RectTransform Sprite(RectTransform p, string n, UnityEngine.Sprite sprite,
                                       Vector2 am, Vector2 aM)
    {
        var rt = Img(p, n, Color.white, am, aM, Vector2.zero, Vector2.zero);
        var img = rt.GetComponent<Image>();
        img.sprite = sprite;
        img.preserveAspect = true;
        return rt;
    }

    public static TextMeshProUGUI Txt(RectTransform p, string n, string txt,
                                      Color col, float sz, Vector2 am, Vector2 aM)
    {
        var go = new GameObject(n);
        go.transform.SetParent(p, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = am; rt.anchorMax = aM; rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero; rt.sizeDelta = Vector2.zero;
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = txt; t.color = col; t.fontSize = sz;
        t.alignment = TextAlignmentOptions.Center;
        t.overflowMode = TextOverflowModes.Overflow;
        return t;
    }

    public static Button Btn(RectTransform p, string label, Color bg,
                             Vector2 am, Vector2 aM, System.Action click,
                             float fontSize = 26f)
    {
        var rt = RoundImg(p, "Btn_" + label, bg, am, aM, Vector2.zero, Vector2.zero, 1.2f);
        var b = rt.gameObject.AddComponent<Button>();
        b.targetGraphic = rt.GetComponent<Image>();
        var cb = b.colors;
        cb.normalColor      = Color.white;
        cb.highlightedColor = new Color(1, 1, 1, 0.82f);
        cb.pressedColor     = new Color(0.72f, 0.72f, 0.72f);
        b.colors = cb;
        if (click != null) b.onClick.AddListener(() =>
        {
            if (UIAudioManager.Instance != null) UIAudioManager.Instance.PlayClick();
            click();
        });
        var t = Txt(rt, "T", label, Color.white, fontSize, Vector2.zero, Vector2.one);
        t.fontStyle = FontStyles.Bold;
        ButtonJuice.Attach(rt.gameObject);
        return b;
    }

    /// <summary>Carga un avatar de Resources/Avatars. Puede devolver null.</summary>
    public static UnityEngine.Sprite LoadAvatar(string avatarId)
    {
        if (string.IsNullOrEmpty(avatarId)) return null;
        return Resources.Load<UnityEngine.Sprite>("Avatars/" + avatarId);
    }

    /// <summary>Ids de avatar disponibles (deben existir en Resources/Avatars).</summary>
    public static readonly string[] AVATAR_IDS =
        { "neo", "axel", "titan", "bombilla", "globo", "atencion" };

    /// <summary>
    /// Dibuja un avatar: sprite si existe; si no, círculo de color con la inicial.
    /// </summary>
    public static void Avatar(RectTransform parent, string avatarId, string nombre,
                              Color fallbackColor, Vector2 am, Vector2 aM)
    {
        var sp = LoadAvatar(avatarId);
        if (sp != null)
        {
            Sprite(parent, "Avatar", sp, am, aM);
        }
        else
        {
            var circle = Img(parent, "AvatarFallback", fallbackColor, am, aM,
                             Vector2.zero, Vector2.zero);
            string ini = string.IsNullOrEmpty(nombre) ? "?" : nombre.Substring(0, 1).ToUpper();
            var t = Txt(circle, "Ini", ini, Color.white, 64, Vector2.zero, Vector2.one);
            t.fontStyle = FontStyles.Bold;
        }
    }

    /// <summary>Campo de texto TMP construido por código.</summary>
    public static TMP_InputField InputField(RectTransform p, string placeholder,
                                            Vector2 am, Vector2 aM, int charLimit = 14)
    {
        var bg = RoundImg(p, "Input", new Color(0.05f, 0.08f, 0.16f), am, aM,
                          Vector2.zero, Vector2.zero, 1.4f);
        var line = RoundImg(bg, "Line", ACCENT, new Vector2(0.06f, 0f), new Vector2(0.94f, 0f),
                            new Vector2(0, 3f), new Vector2(0, 3f), 4f);
        line.GetComponent<Image>().raycastTarget = false;

        var field = bg.gameObject.AddComponent<TMP_InputField>();

        var areaGO = new GameObject("TextArea");
        areaGO.transform.SetParent(bg, false);
        var areaRT = areaGO.AddComponent<RectTransform>();
        areaRT.anchorMin = new Vector2(0.03f, 0.05f);
        areaRT.anchorMax = new Vector2(0.97f, 0.95f);
        areaRT.sizeDelta = Vector2.zero;
        areaGO.AddComponent<RectMask2D>();

        var phT = Txt(areaRT, "Placeholder", placeholder, DIM, 28, Vector2.zero, Vector2.one);
        phT.alignment = TextAlignmentOptions.MidlineLeft;
        phT.fontStyle = FontStyles.Italic;

        var txtT = Txt(areaRT, "Text", "", Color.white, 28, Vector2.zero, Vector2.one);
        txtT.alignment = TextAlignmentOptions.MidlineLeft;

        field.targetGraphic  = bg.GetComponent<Image>();
        field.textViewport   = areaRT;
        field.textComponent  = txtT;
        field.placeholder    = phT;
        field.characterLimit = charLimit;
        field.caretColor     = ACCENT;
        field.customCaretColor = true;
        field.selectionColor = new Color(ACCENT.r, ACCENT.g, ACCENT.b, 0.35f);
        return field;
    }
}

/// <summary>Parpadeo suave de estrellas del fondo espacial (tiempo no escalado).</summary>
public class StarTwinkle : MonoBehaviour
{
    Image _img;
    float _phase, _speed, _baseAlpha;

    void Start()
    {
        _img = GetComponent<Image>();
        _phase = Random.value * 6.2832f;
        _speed = Random.Range(0.6f, 2.0f);
        if (_img != null) _baseAlpha = _img.color.a;
    }

    void Update()
    {
        if (_img == null) return;
        var c = _img.color;
        c.a = _baseAlpha * (0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * _speed + _phase));
        _img.color = c;
    }
}

/// <summary>Balanceo vertical suave (flotar en el espacio). Tiempo no escalado.</summary>
public class FloatBob : MonoBehaviour
{
    RectTransform _rt;
    Vector2 _basePos;
    float _phase;
    float _amp = 9f, _speed = 1.4f;
    bool _ready;

    public FloatBob Configure(float amplitude, float speed)
    {
        _amp = amplitude; _speed = speed;
        return this;
    }

    void Start()
    {
        _rt = transform as RectTransform;
        if (_rt == null) { enabled = false; return; }
        _basePos = _rt.anchoredPosition;
        _phase = Random.value * 6.2832f;
        _ready = true;
    }

    void Update()
    {
        if (!_ready || _rt == null) return;
        _rt.anchoredPosition = _basePos +
            new Vector2(0f, Mathf.Sin(Time.unscaledTime * _speed + _phase) * _amp);
    }
}
