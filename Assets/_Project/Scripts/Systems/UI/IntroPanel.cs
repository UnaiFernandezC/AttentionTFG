// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public static class IntroPanel
{

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

    public static GameObject Build(string title, string categoryName,
                                   string description, System.Action onStart)
    {

        if (Object.FindObjectOfType<EventSystem>() == null)
        {
            var esGO = new GameObject("EventSystem");
            esGO.AddComponent<EventSystem>();
            esGO.AddComponent<StandaloneInputModule>();
        }

        Color catCol = CategoryColor(categoryName);

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

        // Fondo espacial (sustituye al fondo plano)
        KidUI.BuildSpaceBackground(R, withPlanet: false);

        var card = Img(R,
            new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f),
            Vector2.zero, new Vector2(1060f, 680f),
            new Color(0.055f, 0.075f, 0.15f, 0.97f));
        Round(card, 0.75f);

        // Barra lateral de color de la categoría (pill redondeado)
        var sideBar = Img(card, new Vector2(0,0.10f), new Vector2(0,0.90f),
            new Vector2(14,0), new Vector2(8,0), catCol);
        Round(sideBar, 4f);

        // Acento superior (pill centrado)
        var topPill = Img(card, new Vector2(0.36f,1f), new Vector2(0.64f,1f),
            new Vector2(0,-4f), new Vector2(0,7f), catCol);
        Round(topPill, 4f);

        var badge = Img(card,
            new Vector2(1f,1f), new Vector2(1f,1f),
            new Vector2(-92f,-30f), new Vector2(150f, 34f),
            new Color(catCol.r*0.35f, catCol.g*0.35f, catCol.b*0.35f, 0.95f));
        Round(badge, 2.2f);
        var badgeTxt = Txt(badge, categoryName.ToUpper(),
            catCol, 15, Vector2.zero, Vector2.one);
        badgeTxt.fontStyle = FontStyles.Bold;
        badgeTxt.characterSpacing = 2f;

        var titleT = Txt(card, title, Color.white, 60,
            new Vector2(0.05f, 0.72f), new Vector2(0.95f, 0.96f));
        titleT.fontStyle  = FontStyles.Bold;
        titleT.alignment  = TextAlignmentOptions.MidlineLeft;
        titleT.overflowMode = TextOverflowModes.Overflow;

        Img(card, new Vector2(0.05f, 0.68f), new Vector2(0.95f, 0.68f),
            Vector2.zero, new Vector2(0, 2),
            new Color(1,1,1,0.10f));

        var descT = Txt(card, description,
            new Color(0.72f, 0.80f, 0.96f), 23,
            new Vector2(0.06f, 0.34f), new Vector2(0.55f, 0.67f));

        // Demo animada "así se juega" ESPECÍFICA del minijuego (a la derecha de la
        // descripción). Si el minijuego no tiene demo propia, usa la genérica.
        MinigameDemos.Attach(card, title, categoryName);
        descT.alignment     = TextAlignmentOptions.TopLeft;
        descT.overflowMode  = TextOverflowModes.Truncate;
        descT.lineSpacing   = 6f;
        descT.enableAutoSizing = true;
        descT.fontSizeMin = 14f;
        descT.fontSizeMax = 23f;

        var hintBg = Img(card,
            new Vector2(0.06f,0.18f), new Vector2(0.94f,0.32f),
            Vector2.zero, Vector2.zero,
            new Color(catCol.r*0.2f, catCol.g*0.2f, catCol.b*0.2f, 0.6f));
        Round(hintBg, 1.6f);
        var hintT = Txt(hintBg,
            "Pulsa  [ESPACIO]  o el boton  COMENZAR  para jugar",
            new Color(catCol.r+0.3f, catCol.g+0.3f, catCol.b+0.3f, 1f), 22,
            Vector2.zero, Vector2.one);
        hintT.overflowMode = TextOverflowModes.Overflow;

        var btnBg = Img(card,
            new Vector2(0.28f, 0.04f), new Vector2(0.72f, 0.16f),
            Vector2.zero, Vector2.zero, catCol);
        Round(btnBg, 1.1f);
        ButtonJuice.Attach(btnBg.gameObject);

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

        UITween.FadeIn(cvGO, 0.30f);
        UITween.PopIn(card, 0.45f, 0.85f, 0.05f);
        UITween.PulseOnce(btnBg, 1.06f, 0.5f);

        return cvGO;
    }

    /// <summary>Aplica esquinas redondeadas a una imagen ya creada.</summary>
    static void Round(RectTransform rt, float cornerScale)
    {
        var img = rt.GetComponent<Image>();
        if (img == null) return;
        img.sprite = KidUI.RoundedSprite;
        img.type = Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = cornerScale;
    }

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
