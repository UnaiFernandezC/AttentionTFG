// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Carita emocional construida 100% con Images (sin assets externos), como apoyo
/// no-lector para los minijuegos de Gestion emocional.
/// mood 0 = enfadado/agobiado (rojo) · 0.5 = neutral/preocupado (amarillo)
/// · 1 = feliz/en calma (verde).
/// Uso:
///   var face = EmotionFaceWidget.Build(parent, new Vector2(0.5f, 0.5f), 96f);
///   face.SetMood(0.8f);
/// </summary>
public class EmotionFaceWidget : MonoBehaviour
{
    public RectTransform Root { get; private set; }

    Image         _bg;
    Image         _browLImg, _browRImg;
    RectTransform _browL, _browR;
    RectTransform _mouthC, _mouthL, _mouthR;

    float _size;

    static readonly Color FACE_RED    = new Color(0.92f, 0.32f, 0.30f);
    static readonly Color FACE_YELLOW = new Color(0.97f, 0.80f, 0.25f);
    static readonly Color FACE_GREEN  = new Color(0.30f, 0.85f, 0.48f);
    static readonly Color FEATURE     = new Color(0.10f, 0.12f, 0.18f, 0.92f);

    public static EmotionFaceWidget Build(RectTransform parent, Vector2 anchor,
                                          float size, Vector2? offset = null)
    {
        var go = new GameObject("EmotionFace");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(size, size);
        rt.anchoredPosition = offset ?? Vector2.zero;

        var w  = go.AddComponent<EmotionFaceWidget>();
        w.Root  = rt;
        w._size = size;
        w.BuildFace();
        return w;
    }

    void BuildFace()
    {
        var circle = AttractionUIController.MakeCircleSprite(128);

        _bg = MkImg(Root, "Bg", FACE_YELLOW, Vector2.zero,
                    new Vector2(_size, _size), circle);

        float eyeS = _size * 0.14f;
        float eyeX = _size * 0.185f;
        float eyeY = _size * 0.13f;
        MkImg(Root, "EyeL", FEATURE, new Vector2(-eyeX, eyeY), new Vector2(eyeS, eyeS), circle);
        MkImg(Root, "EyeR", FEATURE, new Vector2( eyeX, eyeY), new Vector2(eyeS, eyeS), circle);

        float browW = _size * 0.24f, browH = _size * 0.055f;
        float browY = eyeY + _size * 0.155f;
        _browLImg = MkImg(Root, "BrowL", FEATURE, new Vector2(-eyeX, browY), new Vector2(browW, browH), null);
        _browRImg = MkImg(Root, "BrowR", FEATURE, new Vector2( eyeX, browY), new Vector2(browW, browH), null);
        _browL = _browLImg.rectTransform;
        _browR = _browRImg.rectTransform;

        float mouthY = -_size * 0.22f;
        _mouthC = MkImg(Root, "MouthC", FEATURE, new Vector2(0f, mouthY),
                        new Vector2(_size * 0.28f, _size * 0.06f), null).rectTransform;
        _mouthL = MkImg(Root, "MouthL", FEATURE, new Vector2(-_size * 0.20f, mouthY),
                        new Vector2(_size * 0.17f, _size * 0.06f), null).rectTransform;
        _mouthR = MkImg(Root, "MouthR", FEATURE, new Vector2( _size * 0.20f, mouthY),
                        new Vector2(_size * 0.17f, _size * 0.06f), null).rectTransform;

        SetMood(0.5f);
    }

    Image MkImg(RectTransform p, string n, Color col, Vector2 pos, Vector2 size, Sprite sp)
    {
        var go = new GameObject(n);
        go.transform.SetParent(p, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta        = size;
        var img = go.AddComponent<Image>();
        img.color         = col;
        img.sprite        = sp;
        img.raycastTarget = false;
        return img;
    }

    /// <summary>0 = enfadado/agobiado · 0.5 = neutral/preocupado · 1 = feliz.</summary>
    public void SetMood(float mood)
    {
        if (_bg == null) return;
        mood = Mathf.Clamp01(mood);

        _bg.color = mood < 0.5f
            ? Color.Lerp(FACE_RED,    FACE_YELLOW, mood * 2f)
            : Color.Lerp(FACE_YELLOW, FACE_GREEN, (mood - 0.5f) * 2f);

        float smile = (mood - 0.5f) * 2f;                 // -1 (ceño) .. +1 (sonrisa)

        // Cejas inclinadas hacia dentro solo cuando esta enfadado.
        float angry = Mathf.Clamp01(1f - mood * 2f);
        var bc = FEATURE; bc.a = angry * 0.92f;
        _browLImg.color = bc;
        _browRImg.color = bc;
        _browL.localEulerAngles = new Vector3(0f, 0f, -28f * angry);
        _browR.localEulerAngles = new Vector3(0f, 0f,  28f * angry);

        // Boca: las comisuras suben (sonrisa) o bajan (enfado).
        float mouthY = -_size * 0.22f;
        float lift   = smile * _size * 0.065f;
        float sideX  = _size * 0.20f;
        _mouthL.anchoredPosition = new Vector2(-sideX, mouthY + lift);
        _mouthR.anchoredPosition = new Vector2( sideX, mouthY + lift);
        _mouthC.anchoredPosition = new Vector2(0f, mouthY - smile * _size * 0.015f);
        _mouthL.localEulerAngles = new Vector3(0f, 0f, -32f * smile);
        _mouthR.localEulerAngles = new Vector3(0f, 0f,  32f * smile);
    }

    /// <summary>Pulso rapido para reforzar un cambio de estado.</summary>
    public void Pulse()
    {
        if (Root != null) UITween.PulseOnce(Root, 1.18f, 0.28f);
    }
}
