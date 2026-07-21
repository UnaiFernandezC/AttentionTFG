// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Emociones que dibuja la cara de robot compartida.
/// Las 6 primeras son basicas; las 4 ultimas son matices (solo en dificil).
/// </summary>
public enum RobotEmotion
{
    Alegria     = 0,
    Tristeza    = 1,
    Enfado      = 2,
    Miedo       = 3,
    Calma       = 4,
    Sorpresa    = 5,
    Frustracion = 6,
    Nervios     = 7,
    Verguenza   = 8,
    Orgullo     = 9
}

/// <summary>
/// Dibujador compartido de caras de robot (100% primitivas KidUI, sin assets).
/// Lo usan "Detective de emociones" (reconocimiento) y "Ordena la emocion"
/// (granularidad). La intensidad 0-1 exagera rasgos y saturacion del glow.
/// Uso:
///   var cara = EmotionFaceArt.Build(parent, new Vector2(0.5f, 0.6f), 320f);
///   cara.SetEmotion(RobotEmotion.Alegria, 0.8f);
/// </summary>
public static class EmotionFaceArt
{
    /// <summary>Emociones basicas (Facil y Medio de "Detective de emociones").</summary>
    public static readonly RobotEmotion[] BASICAS =
    {
        RobotEmotion.Alegria, RobotEmotion.Tristeza, RobotEmotion.Enfado,
        RobotEmotion.Miedo,   RobotEmotion.Calma,    RobotEmotion.Sorpresa
    };

    /// <summary>Matices emocionales (se suman en Dificil).</summary>
    public static readonly RobotEmotion[] MATICES =
    {
        RobotEmotion.Frustracion, RobotEmotion.Nervios,
        RobotEmotion.Verguenza,   RobotEmotion.Orgullo
    };

    /// <summary>Color propio de cada emocion (glow, antena y botones).</summary>
    public static Color GlowColor(RobotEmotion e)
    {
        switch (e)
        {
            case RobotEmotion.Alegria:     return new Color(0.98f, 0.80f, 0.10f);
            case RobotEmotion.Tristeza:    return new Color(0.30f, 0.55f, 0.95f);
            case RobotEmotion.Enfado:      return new Color(0.90f, 0.25f, 0.28f);
            case RobotEmotion.Miedo:       return new Color(0.58f, 0.28f, 0.92f);
            case RobotEmotion.Calma:       return new Color(0.18f, 0.80f, 0.58f);
            case RobotEmotion.Sorpresa:    return new Color(0.95f, 0.55f, 0.12f);
            case RobotEmotion.Frustracion: return new Color(0.95f, 0.45f, 0.18f);
            case RobotEmotion.Nervios:     return new Color(0.45f, 0.80f, 0.95f);
            case RobotEmotion.Verguenza:   return new Color(0.95f, 0.45f, 0.65f);
            default:                       return new Color(0.98f, 0.70f, 0.20f); // Orgullo
        }
    }

    /// <summary>Nombre en espanol, listo para botones y textos.</summary>
    public static string Nombre(RobotEmotion e)
    {
        switch (e)
        {
            case RobotEmotion.Alegria:     return "Alegría";
            case RobotEmotion.Tristeza:    return "Tristeza";
            case RobotEmotion.Enfado:      return "Enfado";
            case RobotEmotion.Miedo:       return "Miedo";
            case RobotEmotion.Calma:       return "Calma";
            case RobotEmotion.Sorpresa:    return "Sorpresa";
            case RobotEmotion.Frustracion: return "Frustración";
            case RobotEmotion.Nervios:     return "Nervios";
            case RobotEmotion.Verguenza:   return "Vergüenza";
            default:                       return "Orgullo";
        }
    }

    /// <summary>
    /// Palabras de intensidad creciente (4 grados) para "Ordena la emocion".
    /// Indice 0 = muy suave ... 3 = muy fuerte.
    /// </summary>
    public static string[] PalabrasIntensidad(RobotEmotion e)
    {
        switch (e)
        {
            case RobotEmotion.Alegria:     return new[] { "Contento",        "Alegre",         "Muy feliz",        "¡Eufórico!" };
            case RobotEmotion.Tristeza:    return new[] { "Desanimado",      "Triste",         "Muy triste",       "Desconsolado" };
            case RobotEmotion.Enfado:      return new[] { "Un poco molesto", "Molesto",        "Enfadado",         "¡Furioso!" };
            case RobotEmotion.Miedo:       return new[] { "Inquieto",        "Nervioso",       "Asustado",         "¡Aterrado!" };
            case RobotEmotion.Calma:       return new[] { "Relajado",        "Tranquilo",      "Muy tranquilo",    "Calma total" };
            case RobotEmotion.Sorpresa:    return new[] { "Curioso",         "Sorprendido",    "Muy sorprendido",  "¡Alucinado!" };
            case RobotEmotion.Frustracion: return new[] { "Algo atascado",   "Atascado",       "Frustrado",        "¡Muy frustrado!" };
            case RobotEmotion.Nervios:     return new[] { "Algo inquieto",   "Nervioso",       "Muy nervioso",     "¡Temblando!" };
            case RobotEmotion.Verguenza:   return new[] { "Un poco cortado", "Tímido",         "Avergonzado",      "¡Como un tomate!" };
            default:                       return new[] { "Satisfecho",      "Contento de sí", "Orgulloso",        "¡Súper orgulloso!" };
        }
    }

    /// <summary>
    /// Construye la cara de robot centrada en anchorPoint del padre,
    /// de "size" pixeles de lado. Devuelve el controlador para cambiar
    /// de emocion e intensidad en cualquier momento.
    /// </summary>
    public static RobotFace Build(RectTransform parent, Vector2 anchorPoint,
                                  float size, Vector2? offset = null)
    {
        var go = new GameObject("RobotFace");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchorPoint;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(size, size);
        rt.anchoredPosition = offset ?? Vector2.zero;

        var face = go.AddComponent<RobotFace>();
        face.BuildParts(size);
        return face;
    }
}

/// <summary>
/// Controlador de la cara de robot dibujada por EmotionFaceArt.
/// Cejas, ojos, boca de pastillas, boca abierta, lagrima, mejillas y glow
/// cambian segun la emocion; la intensidad 0-1 exagera todo.
/// </summary>
public class RobotFace : MonoBehaviour
{
    public RectTransform Root { get; private set; }

    float _s;               // lado de la cara en pixeles

    Image         _glow, _screen, _antTip;
    RectTransform _eyeL, _eyeR;
    Image         _browLImg, _browRImg;
    RectTransform _browL, _browR;
    RectTransform _mouthL, _mouthC, _mouthR, _mouthO;
    Image         _mouthLImg, _mouthCImg, _mouthRImg, _mouthOImg;
    Image         _tear, _cheekL, _cheekR;

    static readonly Color HEAD    = new Color(0.78f, 0.84f, 0.96f);
    static readonly Color SCREEN  = new Color(0.09f, 0.12f, 0.22f);
    static readonly Color EYE     = new Color(0.65f, 0.95f, 1.00f);
    static readonly Color PUPIL   = new Color(0.05f, 0.08f, 0.15f);
    static readonly Color FEATURE = new Color(0.88f, 0.96f, 1.00f);

    /// <summary>Construye todas las piezas (lo llama EmotionFaceArt.Build).</summary>
    public void BuildParts(float size)
    {
        Root = transform as RectTransform;
        _s = size;
        float S = size;
        var mid = new Vector2(0.5f, 0.5f);

        // Glow de emocion detras de la cabeza
        _glow = Circle("Glow", new Color(1f, 1f, 1f, 0.25f), S * 1.30f, Vector2.zero);

        // Antena (tallo + punta que se tine con la emocion)
        Pill("AntStem", new Color(0.62f, 0.70f, 0.86f), new Vector2(S * 0.055f, S * 0.16f),
             new Vector2(0f, S * 0.50f), 3f);
        _antTip = Circle("AntTip", KidUI.ACCENT, S * 0.12f, new Vector2(0f, S * 0.585f));

        // Orejas laterales
        Pill("EarL", new Color(0.62f, 0.70f, 0.86f), new Vector2(S * 0.10f, S * 0.26f),
             new Vector2(-S * 0.52f, 0f), 2.2f);
        Pill("EarR", new Color(0.62f, 0.70f, 0.86f), new Vector2(S * 0.10f, S * 0.26f),
             new Vector2( S * 0.52f, 0f), 2.2f);

        // Cabeza redondeada + pantalla oscura donde viven ojos y boca
        var head = KidUI.RoundImg(Root, "Head", HEAD, mid, mid,
                                  Vector2.zero, new Vector2(S, S * 0.88f), 0.55f);
        head.GetComponent<Image>().raycastTarget = false;
        var screenRT = KidUI.RoundImg(Root, "Screen", SCREEN, mid, mid,
                                      new Vector2(0f, -S * 0.02f),
                                      new Vector2(S * 0.80f, S * 0.66f), 0.75f);
        _screen = screenRT.GetComponent<Image>();
        _screen.raycastTarget = false;

        // Ojos (blanco cian + pupila)
        float eyeX = S * 0.18f, eyeY = S * 0.095f, eyeD = S * 0.21f;
        _eyeL = Circle("EyeL", EYE, eyeD, new Vector2(-eyeX, eyeY)).rectTransform;
        _eyeR = Circle("EyeR", EYE, eyeD, new Vector2( eyeX, eyeY)).rectTransform;
        CircleIn(_eyeL, "PupL", PUPIL, eyeD * 0.45f);
        CircleIn(_eyeR, "PupR", PUPIL, eyeD * 0.45f);

        // Lagrima (solo tristeza intensa)
        _tear = Circle("Tear", new Color(0.45f, 0.75f, 1f, 0f), S * 0.075f,
                       new Vector2(-eyeX, -S * 0.035f));

        // Mejillas (solo alegria)
        _cheekL = Circle("CheekL", new Color(0.98f, 0.55f, 0.55f, 0f), S * 0.095f,
                         new Vector2(-S * 0.30f, -S * 0.05f));
        _cheekR = Circle("CheekR", new Color(0.98f, 0.55f, 0.55f, 0f), S * 0.095f,
                         new Vector2( S * 0.30f, -S * 0.05f));

        // Cejas (pastillas que rotan)
        float browY = S * 0.245f, browW = S * 0.20f, browH = S * 0.048f;
        _browL = Pill("BrowL", FEATURE, new Vector2(browW, browH), new Vector2(-eyeX, browY), 2.6f);
        _browR = Pill("BrowR", FEATURE, new Vector2(browW, browH), new Vector2( eyeX, browY), 2.6f);
        _browLImg = _browL.GetComponent<Image>();
        _browRImg = _browR.GetComponent<Image>();

        // Boca de 3 pastillas (curvatura) + boca "O" (abierta)
        float mouthY = -S * 0.17f, mh = S * 0.048f;
        _mouthL = Pill("MouthL", FEATURE, new Vector2(S * 0.115f, mh), new Vector2(-S * 0.115f, mouthY), 2.6f);
        _mouthC = Pill("MouthC", FEATURE, new Vector2(S * 0.15f,  mh), new Vector2(0f,          mouthY), 2.6f);
        _mouthR = Pill("MouthR", FEATURE, new Vector2(S * 0.115f, mh), new Vector2( S * 0.115f, mouthY), 2.6f);
        _mouthLImg = _mouthL.GetComponent<Image>();
        _mouthCImg = _mouthC.GetComponent<Image>();
        _mouthRImg = _mouthR.GetComponent<Image>();
        _mouthO = Circle("MouthO", FEATURE, S * 0.17f, new Vector2(0f, mouthY)).rectTransform;
        _mouthOImg = _mouthO.GetComponent<Image>();
        _mouthO.gameObject.SetActive(false);

        SetEmotion(RobotEmotion.Calma, 0.5f);
    }

    /// <summary>
    /// Cambia la expresion. intensidad 0 = apenas se nota, 1 = exagerada
    /// (rasgos mas marcados y glow mas saturado).
    /// </summary>
    public void SetEmotion(RobotEmotion emo, float intensidad = 0.7f)
    {
        if (Root == null) return;
        float i = Mathf.Clamp01(intensidad);
        float S = _s;

        float smile = 0f, open = 0f, browAng = 0f, browLift = 0f;
        float eyeSY = 1f, eyeS = 1f, tearA = 0f, cheekA = 0f;

        switch (emo)
        {
            case RobotEmotion.Alegria:
                smile = 0.45f + 0.55f * i; browLift = 0.02f + 0.03f * i;
                cheekA = 0.30f + 0.45f * i;
                break;
            case RobotEmotion.Tristeza:
                smile = -(0.35f + 0.65f * i); browAng = -(14f + 18f * i);
                eyeSY = 0.90f; tearA = Mathf.Clamp01((i - 0.35f) / 0.65f);
                break;
            case RobotEmotion.Enfado:
                smile = -(0.25f + 0.45f * i); browAng = 16f + 26f * i;
                browLift = -0.02f - 0.02f * i; eyeSY = 0.85f - 0.30f * i;
                break;
            case RobotEmotion.Miedo:
                smile = -0.15f - 0.10f * i; open = 0.25f + 0.45f * i;
                browAng = -(10f + 14f * i); browLift = 0.03f + 0.04f * i;
                eyeS = 1.05f + 0.25f * i;
                break;
            case RobotEmotion.Calma:
                smile = 0.20f + 0.25f * i; eyeSY = 0.55f - 0.15f * i;
                break;
            case RobotEmotion.Sorpresa:
                open = 0.45f + 0.55f * i; browLift = 0.05f + 0.05f * i;
                eyeS = 1.15f + 0.35f * i;
                break;
            case RobotEmotion.Frustracion:
                // Como el enfado pero con la boca mas plana y ojos entornados.
                smile = -(0.30f + 0.35f * i); browAng = 12f + 20f * i;
                browLift = -0.01f - 0.02f * i; eyeSY = 0.75f - 0.15f * i;
                break;
            case RobotEmotion.Nervios:
                // Boca pequena entreabierta, cejas de preocupacion y gotita.
                smile = -0.10f - 0.10f * i; open = 0.18f + 0.20f * i;
                browAng = -(8f + 10f * i); browLift = 0.02f + 0.03f * i;
                eyeS = 1.05f + 0.15f * i; tearA = 0.35f * i;
                break;
            case RobotEmotion.Verguenza:
                // Mejillas muy encendidas, mirada bajita y sonrisa timida invertida.
                smile = -0.05f - 0.15f * i; browAng = -(6f + 8f * i);
                browLift = 0.02f; eyeSY = 0.60f - 0.10f * i;
                cheekA = 0.55f + 0.45f * i;
                break;
            case RobotEmotion.Orgullo:
                // Sonrisa amplia y serena con ojos medio cerrados de gustito.
                smile = 0.35f + 0.45f * i; browLift = 0.04f + 0.03f * i;
                eyeSY = 0.65f - 0.10f * i; cheekA = 0.20f + 0.25f * i;
                break;
        }

        // Glow, antena y tinte de pantalla con el color de la emocion
        Color gc = EmotionFaceArt.GlowColor(emo);
        _glow.color   = new Color(gc.r, gc.g, gc.b, 0.20f + 0.30f * i);
        _antTip.color = gc;
        _screen.color = Color.Lerp(SCREEN, gc, 0.10f + 0.22f * i);

        // Ojos
        _eyeL.localScale = new Vector3(eyeS, eyeS * eyeSY, 1f);
        _eyeR.localScale = new Vector3(eyeS, eyeS * eyeSY, 1f);

        // Cejas: browAng>0 = puntas internas hacia abajo (enfado);
        // browAng<0 = puntas internas hacia arriba (pena / miedo).
        float browY = S * (0.245f + browLift);
        _browL.anchoredPosition = new Vector2(-S * 0.18f, browY);
        _browR.anchoredPosition = new Vector2( S * 0.18f, browY);
        _browL.localEulerAngles = new Vector3(0f, 0f, -browAng);
        _browR.localEulerAngles = new Vector3(0f, 0f,  browAng);

        // Boca: cerrada (3 pastillas curvadas) o abierta (circulo escalado)
        bool abierta = open > 0.15f;
        _mouthLImg.enabled = _mouthCImg.enabled = _mouthRImg.enabled = !abierta;
        _mouthO.gameObject.SetActive(abierta);
        if (abierta)
        {
            _mouthO.localScale = new Vector3(0.70f + 0.40f * open, 0.50f + 0.90f * open, 1f);
            _mouthOImg.color = FEATURE;
        }
        else
        {
            float mouthY = -S * 0.17f;
            float lift   = smile * S * 0.060f;
            float sideX  = S * 0.115f;
            _mouthL.anchoredPosition = new Vector2(-sideX, mouthY + lift);
            _mouthR.anchoredPosition = new Vector2( sideX, mouthY + lift);
            _mouthC.anchoredPosition = new Vector2(0f, mouthY - smile * S * 0.012f);
            _mouthL.localEulerAngles = new Vector3(0f, 0f, -34f * smile);
            _mouthR.localEulerAngles = new Vector3(0f, 0f,  34f * smile);
        }

        // Detalles: lagrima y mejillas
        var tc = _tear.color;   tc.a = tearA * 0.9f;  _tear.color   = tc;
        var ck = _cheekL.color; ck.a = cheekA * 0.6f; _cheekL.color = ck; _cheekR.color = ck;
    }

    /// <summary>Pulso rapido para reforzar un cambio o un acierto.</summary>
    public void Pulse()
    {
        if (Root != null) UITween.PulseOnce(Root, 1.12f, 0.26f);
    }

    // ------------------------------------------------ helpers de construccion

    Image Circle(string n, Color c, float size, Vector2 pos)
    {
        var rt = KidUI.CircleAt(Root, n, c, new Vector2(0.5f, 0.5f), size);
        rt.anchoredPosition = pos;
        var img = rt.GetComponent<Image>();
        img.raycastTarget = false;
        return img;
    }

    void CircleIn(RectTransform parent, string n, Color c, float size)
    {
        var rt = KidUI.CircleAt(parent, n, c, new Vector2(0.5f, 0.5f), size);
        rt.GetComponent<Image>().raycastTarget = false;
    }

    RectTransform Pill(string n, Color c, Vector2 size, Vector2 pos, float corner)
    {
        var mid = new Vector2(0.5f, 0.5f);
        var rt = KidUI.RoundImg(Root, n, c, mid, mid, pos, size, corner);
        rt.GetComponent<Image>().raycastTarget = false;
        return rt;
    }
}
