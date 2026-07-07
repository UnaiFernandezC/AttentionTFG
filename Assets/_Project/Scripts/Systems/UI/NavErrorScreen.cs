// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Pantalla de error AMABLE para referencias de escena rotas: en lugar de un
/// fallo silencioso (o una excepción), muestra "zona en construcción" con un
/// botón para seguir jugando. Se autodestruye; no rompe el flujo.
/// </summary>
public static class NavErrorScreen
{
    static GameObject _current;

    /// <summary>True mientras la pantalla de error está visible (bloquea ESC).</summary>
    public static bool IsOpen => _current != null;

    public static void Show(string sceneName)
    {
        if (_current != null) return;
        KidUI.EnsureEventSystem();
        GameFeel.PlayError();

        var go = new GameObject("NavErrorScreen");
        _current = go;
        var cv = KidUI.MakeCanvas("NavErrorCanvas", 990, go.transform);
        var R = cv.GetComponent<RectTransform>();

        KidUI.Img(R, "Dim", new Color(0f, 0f, 0f, 0.82f),
                  Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        var card = KidUI.RoundImg(R, "Card", new Color(0.055f, 0.075f, 0.15f, 0.98f),
                                  new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                                  Vector2.zero, new Vector2(680f, 420f), 0.9f);
        var pill = KidUI.RoundImg(card, "Top", KidUI.WARN,
                                  new Vector2(0.34f, 0.985f), new Vector2(0.66f, 0.993f),
                                  Vector2.zero, Vector2.zero, 4f);
        pill.GetComponent<Image>().raycastTarget = false;

        // Robot "en obras"
        var robot = KidUI.LoadAvatar("neo");
        if (robot != null)
        {
            var rrt = KidUI.Sprite(card, "Robot", robot,
                                   new Vector2(0.05f, 0.42f), new Vector2(0.33f, 0.92f));
            rrt.GetComponent<Image>().color = new Color(0.8f, 0.8f, 0.9f);
            UITween.PopIn(rrt, 0.4f, 0.6f);
        }

        var title = KidUI.Txt(card, "Title", "¡UPS!", KidUI.WARN, 52,
                              new Vector2(0.36f, 0.68f), new Vector2(0.95f, 0.92f));
        title.fontStyle = FontStyles.Bold;
        title.alignment = TextAlignmentOptions.MidlineLeft;

        var msg = KidUI.Txt(card, "Msg",
            "Esta zona está en construcción.\nLos robots la están reparando.",
            Color.white, 24,
            new Vector2(0.36f, 0.42f), new Vector2(0.95f, 0.66f));
        msg.alignment = TextAlignmentOptions.TopLeft;

        KidUI.Txt(card, "Detail", "(" + sceneName + ")", KidUI.DIM, 14,
                  new Vector2(0.36f, 0.33f), new Vector2(0.95f, 0.41f))
            .alignment = TextAlignmentOptions.MidlineLeft;

        KidUI.Btn(card, "VOLVER AL JUEGO", KidUI.ACCENT,
                  new Vector2(0.25f, 0.06f), new Vector2(0.75f, 0.20f),
                  Close, 24f);

        UITween.FadeIn(go, 0.25f);
        UITween.PopIn(card, 0.35f, 0.8f);
    }

    static void Close()
    {
        if (_current != null) Object.Destroy(_current);
        _current = null;
    }
}
