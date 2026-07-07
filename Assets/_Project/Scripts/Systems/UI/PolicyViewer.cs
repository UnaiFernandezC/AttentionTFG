// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Visor reutilizable de la política de privacidad (solo lectura, con scroll).
/// Se usa desde la pantalla de consentimiento (primer arranque) y desde el
/// menú ESC para poder releerla en cualquier momento.
/// </summary>
public static class PolicyViewer
{
    static GameObject _current;

    /// <summary>True mientras el visor está abierto (el menú ESC lo consulta
    /// para no reaccionar a la tecla ESC por debajo).</summary>
    public static bool IsOpen => _current != null;

    public static void Show()
    {
        if (_current != null) return;
        KidUI.EnsureEventSystem();

        var go = new GameObject("PolicyViewer");
        _current = go;
        var cv = KidUI.MakeCanvas("PolicyCanvas", 995, go.transform);
        var R = cv.GetComponent<RectTransform>();

        KidUI.Img(R, "Dim", new Color(0f, 0f, 0f, 0.88f),
                  Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        var card = KidUI.RoundImg(R, "Card", new Color(0.055f, 0.075f, 0.15f, 0.99f),
                                  new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                                  Vector2.zero, new Vector2(1100f, 900f), 0.7f);
        var pill = KidUI.RoundImg(card, "Top", KidUI.ACCENT,
                                  new Vector2(0.36f, 0.988f), new Vector2(0.64f, 0.995f),
                                  Vector2.zero, Vector2.zero, 4f);
        pill.GetComponent<Image>().raycastTarget = false;

        var title = KidUI.Txt(card, "T", "POLÍTICA DE PRIVACIDAD", Color.white, 30,
                              new Vector2(0.05f, 0.93f), new Vector2(0.75f, 0.99f));
        title.fontStyle = FontStyles.Bold;
        title.alignment = TextAlignmentOptions.MidlineLeft;

        KidUI.Btn(card, "Cerrar", KidUI.BAD,
                  new Vector2(0.82f, 0.935f), new Vector2(0.96f, 0.985f),
                  Close, 16f);

        // Viewport con scroll
        var viewGO = new GameObject("Viewport");
        viewGO.transform.SetParent(card, false);
        var viewRT = viewGO.AddComponent<RectTransform>();
        viewRT.anchorMin = new Vector2(0.04f, 0.03f);
        viewRT.anchorMax = new Vector2(0.96f, 0.92f);
        viewRT.sizeDelta = Vector2.zero;
        viewGO.AddComponent<Image>().color = new Color(0, 0, 0, 0.25f);
        viewGO.AddComponent<RectMask2D>();

        var contentGO = new GameObject("Content");
        contentGO.transform.SetParent(viewRT, false);
        var contentRT = contentGO.AddComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1f);

        var txt = contentGO.AddComponent<TextMeshProUGUI>();
        txt.text = ConsentScreen.PolicyText();
        txt.fontSize = 18;
        txt.color = new Color(0.85f, 0.90f, 1f);
        txt.alignment = TextAlignmentOptions.TopLeft;
        txt.lineSpacing = 22f;          // interlineado cómodo de leer
        txt.paragraphSpacing = 14f;
        txt.margin = new Vector4(18, 14, 18, 14);
        txt.ForceMeshUpdate();
        contentRT.sizeDelta = new Vector2(0, txt.preferredHeight + 50f);

        var sr = viewGO.AddComponent<ScrollRect>();
        sr.content = contentRT;
        sr.viewport = viewRT;
        sr.horizontal = false;
        sr.vertical = true;
        sr.movementType = ScrollRect.MovementType.Clamped;
        sr.scrollSensitivity = 30f;

        UITween.FadeIn(go, 0.2f);
        UITween.PopIn(card, 0.3f, 0.9f);
    }

    static void Close()
    {
        if (_current != null) Object.Destroy(_current);
        _current = null;
    }
}
