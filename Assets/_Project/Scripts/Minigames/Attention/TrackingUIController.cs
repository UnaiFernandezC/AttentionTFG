// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI del minijuego de seguimiento multiple (MOT): fondo, cabecera,
/// banner de fase (MIRA / SIGUE / TOCA), puntos de ronda y marcador.
/// Muy visual y con texto minimo durante el juego.
/// </summary>
public class TrackingUIController : MonoBehaviour
{
    public RectTransform CanvasRT   { get; private set; }
    public RectTransform PlayAreaRT { get; private set; }

    Image[]         _roundDots;
    Image           _phaseBg;
    TextMeshProUGUI _phaseLbl;
    TextMeshProUGUI _scoreLbl;

    static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

    static readonly Color BG     = new Color(0.08f, 0.10f, 0.16f);
    static readonly Color HDR    = new Color(0.05f, 0.08f, 0.15f);
    static readonly Color ACCENT = new Color(0.98f, 0.80f, 0.10f);
    static readonly Color DIM    = new Color(0.45f, 0.58f, 0.75f);

    public void BuildUI(int rounds)
    {
        var cv = KidUI.MakeCanvas("Canvas_Tracking", 5, transform);
        CanvasRT = cv.GetComponent<RectTransform>();

        KidUI.Img(CanvasRT, "BG", BG, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        KidUI.Img(CanvasRT, "GradT", C(0.24f, 0.20f, 0.06f, 0.20f),
                  new Vector2(0f, 0.70f), Vector2.one, Vector2.zero, Vector2.zero);

        // Cabecera
        var hdr = KidUI.Img(CanvasRT, "Hdr", HDR,
                            new Vector2(0f, 1f), new Vector2(1f, 1f),
                            new Vector2(0f, -44f), new Vector2(0f, 88f));
        KidUI.Img(hdr, "Line", ACCENT, new Vector2(0f, 0f), new Vector2(1f, 0f),
                  new Vector2(0f, 1.5f), new Vector2(0f, 3f));
        KidUI.Img(hdr, "AccL", ACCENT, new Vector2(0f, 0.18f), new Vector2(0f, 0.82f),
                  new Vector2(3f, 0f), new Vector2(6f, 0f));
        var ttl = KidUI.Txt(hdr, "T", "SIGUE A LAS AMIGAS", Color.white, 33,
                            new Vector2(0.03f, 0.10f), new Vector2(0.62f, 0.90f));
        ttl.fontStyle = FontStyles.Bold;
        ttl.alignment = TextAlignmentOptions.MidlineLeft;
        ttl.characterSpacing = 1.5f;
        ttl.enableAutoSizing = true;
        ttl.fontSizeMin = 18f;
        ttl.fontSizeMax = 33f;
        var cat = KidUI.Txt(hdr, "Cat", "ATENCION", DIM, 16,
                            new Vector2(0.63f, 0.12f), new Vector2(0.97f, 0.88f));
        cat.alignment = TextAlignmentOptions.MidlineRight;

        // Banner de fase (palabra corta + color, centro superior)
        _phaseBg = KidUI.Img(CanvasRT, "PhaseBg", C(0.98f, 0.80f, 0.10f, 0.90f),
                             new Vector2(0.36f, 0.855f), new Vector2(0.64f, 0.915f),
                             Vector2.zero, Vector2.zero).GetComponent<Image>();
        _phaseBg.raycastTarget = false;
        _phaseLbl = KidUI.Txt((RectTransform)_phaseBg.transform, "PhaseLbl", "¡MIRA!",
                              Color.white, 34, Vector2.zero, Vector2.one);
        _phaseLbl.fontStyle = FontStyles.Bold;
        _phaseLbl.characterSpacing = 3f;
        _phaseLbl.raycastTarget = false;

        // Marcador (izquierda) y puntos de ronda (derecha)
        _scoreLbl = KidUI.Txt(CanvasRT, "Score", "0 pts", ACCENT, 26,
                              new Vector2(0.02f, 0.855f), new Vector2(0.30f, 0.915f));
        _scoreLbl.fontStyle = FontStyles.Bold;
        _scoreLbl.alignment = TextAlignmentOptions.MidlineLeft;

        _roundDots = new Image[rounds];
        var dotsHolder = KidUI.Img(CanvasRT, "Dots", Color.clear,
                                   new Vector2(0.70f, 0.855f), new Vector2(0.98f, 0.915f),
                                   Vector2.zero, Vector2.zero);
        dotsHolder.GetComponent<Image>().raycastTarget = false;
        float dotW = 26f, dotGap = 12f;
        float totalW = rounds * dotW + (rounds - 1) * dotGap;
        for (int i = 0; i < rounds; i++)
        {
            var dGO = new GameObject("Dot" + i);
            dGO.transform.SetParent(dotsHolder, false);
            var dRT = dGO.AddComponent<RectTransform>();
            dRT.anchorMin = dRT.anchorMax = new Vector2(1f, 0.5f);
            dRT.pivot = new Vector2(0.5f, 0.5f);
            dRT.sizeDelta = new Vector2(dotW, dotW);
            dRT.anchoredPosition = new Vector2(-totalW + i * (dotW + dotGap) + dotW * 0.5f, 0f);
            var img = dGO.AddComponent<Image>();
            img.color = C(1f, 1f, 1f, 0.18f);
            img.raycastTarget = false;
            _roundDots[i] = img;
        }

        // Zona de juego (las bolas se cuelgan aqui)
        var playGO = new GameObject("PlayArea");
        playGO.transform.SetParent(CanvasRT, false);
        PlayAreaRT = playGO.AddComponent<RectTransform>();
        PlayAreaRT.anchorMin = PlayAreaRT.anchorMax = new Vector2(0.5f, 0.5f);
        PlayAreaRT.pivot = new Vector2(0.5f, 0.5f);
        PlayAreaRT.sizeDelta = Vector2.zero;
        PlayAreaRT.anchoredPosition = Vector2.zero;
    }

    public void SetPhase(string word, Color col)
    {
        if (_phaseBg == null) return;
        _phaseBg.color  = new Color(col.r, col.g, col.b, 0.90f);
        _phaseLbl.text  = word;
        UITween.PulseOnce((RectTransform)_phaseBg.transform, 1.08f, 0.25f);
    }

    public void SetScore(int score)
    {
        if (_scoreLbl != null) _scoreLbl.text = score + " pts";
    }

    public void SetRoundDot(int index, Color col)
    {
        if (_roundDots == null || index < 0 || index >= _roundDots.Length) return;
        _roundDots[index].color = col;
        UITween.PulseOnce(_roundDots[index].rectTransform, 1.4f, 0.3f);
    }
}
