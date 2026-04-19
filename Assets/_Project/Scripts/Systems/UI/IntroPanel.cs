using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Utilidad estatica que construye el panel de introduccion de cualquier minijuego.
/// Devuelve el GameObject del canvas creado; destruyelo cuando el juego comience.
///
/// Uso desde un MonoBehaviour cualquiera:
///
///   bool _started = false;
///   var cv = IntroPanel.Build("Mi juego", "Memoria", ..., "Descripcion...", () => _started = true);
///   while (!_started) {
///       if (Input.GetKeyDown(KeyCode.Space)) _started = true;
///       yield return null;
///   }
///   Destroy(cv);
/// </summary>
public static class IntroPanel
{
    // ── Paleta de colores por categoria ──────────────────────────────────
    public static Color CategoryColor(string cat)
    {
        switch (cat)
        {
            case "Memoria":               return new Color(0.58f, 0.28f, 0.92f);
            case "Control de impulsos":   return new Color(0.95f, 0.55f, 0.12f);
            case "Gestion emocional":     return new Color(0.18f, 0.80f, 0.58f);
            case "Atencion":              return new Color(0.98f, 0.80f, 0.10f);
            case "Planificacion":         return new Color(0.28f, 0.60f, 1.00f);
            default:                      return new Color(0.28f, 0.60f, 1.00f);
        }
    }

    // ── Metodo principal ─────────────────────────────────────────────────

    /// <summary>
    /// Construye el panel de introduccion y lo devuelve.
    /// onStart se llama al pulsar el boton "Comenzar".
    /// </summary>
    public static GameObject Build(string title, string categoryName,
                                   string description, System.Action onStart)
    {
        // EventSystem
        if (Object.FindObjectOfType<EventSystem>() == null)
        {
            var esGO = new GameObject("EventSystem");
            esGO.AddComponent<EventSystem>();
            esGO.AddComponent<StandaloneInputModule>();
        }

        Color catCol = CategoryColor(categoryName);

        // Canvas raiz
        var cvGO = new GameObject("IntroCanvas");
        var cv = cvGO.AddComponent<Canvas>();
        cv.renderMode   = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 100;
        var sc = cvGO.AddComponent<CanvasScaler>();
        sc.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920f, 1080f);
        sc.matchWidthOrHeight  = 0.5f;
        cvGO.AddComponent<GraphicRaycaster>();
        var R = cvGO.GetComponent<RectTransform>();

        // ── Fondo oscuro ──
        Img(R, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            new Color(0.03f, 0.05f, 0.12f, 0.97f));

        // ── Tarjeta central ──
        var card = Img(R,
            new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f),
            Vector2.zero, new Vector2(1060f, 590f),
            new Color(0.10f, 0.13f, 0.24f));

        // Borde lateral izquierdo (color de categoria)
        Img(card, new Vector2(0,0), new Vector2(0,1),
            new Vector2(4,0), new Vector2(8,0), catCol);

        // Barra superior del color de categoria
        Img(card, new Vector2(0,1), new Vector2(1,1),
            Vector2.zero, new Vector2(0,5), catCol);

        // Sombra interna sutil arriba
        Img(card, new Vector2(0,1), new Vector2(1,1),
            new Vector2(0,-18), new Vector2(0,36),
            new Color(0,0,0,0.18f));

        // ── Badge de categoria (arriba derecha) ──
        var badge = Img(card,
            new Vector2(1f,1f), new Vector2(1f,1f),
            new Vector2(-80f,-22f), new Vector2(140f, 30f),
            new Color(catCol.r*0.5f, catCol.g*0.5f, catCol.b*0.5f, 0.9f));
        var badgeTxt = Txt(badge, categoryName.ToUpper(),
            catCol, 15, Vector2.zero, Vector2.one);
        badgeTxt.fontStyle = FontStyles.Bold;
        badgeTxt.characterSpacing = 2f;

        // ── Titulo ──
        var titleT = Txt(card, title, Color.white, 60,
            new Vector2(0.05f, 0.72f), new Vector2(0.95f, 0.96f));
        titleT.fontStyle  = FontStyles.Bold;
        titleT.alignment  = TextAlignmentOptions.MidlineLeft;
        titleT.overflowMode = TextOverflowModes.Overflow;

        // ── Linea separadora ──
        Img(card, new Vector2(0.05f, 0.68f), new Vector2(0.95f, 0.68f),
            Vector2.zero, new Vector2(0, 2),
            new Color(1,1,1,0.10f));

        // ── Descripcion ──
        var descT = Txt(card, description,
            new Color(0.72f, 0.80f, 0.96f), 28,
            new Vector2(0.06f, 0.34f), new Vector2(0.94f, 0.67f));
        descT.alignment     = TextAlignmentOptions.TopLeft;
        descT.overflowMode  = TextOverflowModes.Overflow;
        descT.lineSpacing   = 6f;

        // ── Icono de teclado + hint ──
        var hintBg = Img(card,
            new Vector2(0.06f,0.18f), new Vector2(0.94f,0.32f),
            Vector2.zero, Vector2.zero,
            new Color(catCol.r*0.2f, catCol.g*0.2f, catCol.b*0.2f, 0.6f));
        var hintT = Txt(hintBg,
            "Pulsa  [ESPACIO]  o el boton  COMENZAR  para jugar",
            new Color(catCol.r+0.3f, catCol.g+0.3f, catCol.b+0.3f, 1f), 22,
            Vector2.zero, Vector2.one);
        hintT.overflowMode = TextOverflowModes.Overflow;

        // ── Boton COMENZAR ──
        var btnBg = Img(card,
            new Vector2(0.28f, 0.04f), new Vector2(0.72f, 0.16f),
            Vector2.zero, Vector2.zero, catCol);

        // Capa brillante sobre el boton
        Img(btnBg, new Vector2(0,0.5f), new Vector2(1,1),
            Vector2.zero, Vector2.zero,
            new Color(1,1,1,0.12f));

        var btn = btnBg.gameObject.AddComponent<Button>();
        btn.targetGraphic = btnBg.GetComponent<Image>();
        var cb = btn.colors;
        cb.normalColor      = Color.white;
        cb.highlightedColor = new Color(1,1,1,0.85f);
        cb.pressedColor     = new Color(0.7f,0.7f,0.7f);
        btn.colors = cb;
        btn.onClick.AddListener(() => onStart?.Invoke());

        var btnT = Txt(btnBg, "COMENZAR", Color.white, 36, Vector2.zero, Vector2.one);
        btnT.fontStyle = FontStyles.Bold;
        btnT.characterSpacing = 3f;

        return cvGO;
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    static RectTransform Img(RectTransform p, Vector2 amin, Vector2 amax,
                              Vector2 pos, Vector2 sd, Color col)
    {
        var go = new GameObject("i");
        go.transform.SetParent(p, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = amin; rt.anchorMax = amax;
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = sd;
        go.AddComponent<Image>().color = col;
        return rt;
    }

    static TextMeshProUGUI Txt(RectTransform p, string text, Color col,
                                float size, Vector2 amin, Vector2 amax)
    {
        var go = new GameObject("t");
        go.transform.SetParent(p, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = amin; rt.anchorMax = amax;
        rt.pivot     = new Vector2(0.5f,0.5f);
        rt.anchoredPosition = Vector2.zero; rt.sizeDelta = Vector2.zero;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text         = text;
        tmp.color        = col;
        tmp.fontSize     = size;
        tmp.alignment    = TextAlignmentOptions.Center;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        return tmp;
    }
}
