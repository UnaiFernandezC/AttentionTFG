// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SimonButtonController : MonoBehaviour
{

    public int   ColorIndex  { get; private set; }
    public bool  Interactive { get; private set; }

    public event Action<int> OnPressed;

    private Image  _mainImage;
    private Image  _glowImage;
    private Image  _shineImage;
    private Button _button;

    private Color  _normalColor;
    private Color  _brightColor;
    private Color  _glowColor;

    private Coroutine _flashCoroutine;

    private const float SCALE_FLASH   = 1.10f;
    private const float SCALE_PRESS   = 0.93f;
    private const float SCALE_NORMAL  = 1.00f;
    private const float ANIM_SPEED    = 12f;

    public void Init(int colorIndex, Image mainImg, Image glowImg, Image shineImg)
    {
        ColorIndex  = colorIndex;
        _mainImage  = mainImg;
        _glowImage  = glowImg;
        _shineImage = shineImg;
        _button     = GetComponent<Button>();

        Color col = mainImg.color;
        _normalColor = new Color(col.r * 0.40f, col.g * 0.40f, col.b * 0.40f, 1f);
        _brightColor = col;
        _glowColor   = new Color(col.r, col.g, col.b, 0.45f);

        _mainImage.color = _normalColor;
        SetGlow(0f);

        if (_button != null)
            _button.onClick.AddListener(HandleClick);
    }

    public void SetInteractive(bool value)
    {
        Interactive = value;
        if (_button != null) _button.interactable = value;
    }

    public IEnumerator Flash(float duration)
    {
        if (_flashCoroutine != null)
            StopCoroutine(_flashCoroutine);

        _flashCoroutine = StartCoroutine(DoFlash(duration));
        yield return _flashCoroutine;
    }

    private IEnumerator DoFlash(float duration)
    {

        LightUp(true);
        transform.localScale = Vector3.one * SCALE_FLASH;

        yield return new WaitForSeconds(duration);

        LightUp(false);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * ANIM_SPEED;
            transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one, t);
            yield return null;
        }
        transform.localScale = Vector3.one;
        _flashCoroutine = null;
    }

    public IEnumerator PlayerPress(float duration = 0.18f)
    {
        LightUp(true);
        transform.localScale = Vector3.one * SCALE_PRESS;

        yield return new WaitForSeconds(duration);

        LightUp(false);
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * ANIM_SPEED;
            transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one, t);
            yield return null;
        }
        transform.localScale = Vector3.one;
    }

    private void LightUp(bool on)
    {
        if (_mainImage  != null) _mainImage.color = on ? _brightColor : _normalColor;
        if (_shineImage != null) _shineImage.color = new Color(1f, 1f, 1f, on ? 0.30f : 0.08f);
        SetGlow(on ? 1f : 0f);
    }

    private void SetGlow(float intensity)
    {
        if (_glowImage == null) return;
        _glowImage.color = new Color(_glowColor.r, _glowColor.g, _glowColor.b,
                                     _glowColor.a * intensity);
    }

    private void HandleClick()
    {
        if (!Interactive) return;
        OnPressed?.Invoke(ColorIndex);
    }
}
