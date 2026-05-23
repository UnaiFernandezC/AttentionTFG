using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controla la apariencia y comportamiento de cada botón de color en Simón Dice.
/// Gestiona: flash de iluminación, glow, escala y eventos de click.
/// Se añade dinámicamente por el UIController.
/// </summary>
public class SimonButtonController : MonoBehaviour
{
    // ── Propiedades ───────────────────────────────────────────────────────────
    public int   ColorIndex  { get; private set; }
    public bool  Interactive { get; private set; }

    // ── Eventos ───────────────────────────────────────────────────────────────
    public event Action<int> OnPressed;

    // ── Referencias internas ──────────────────────────────────────────────────
    private Image  _mainImage;
    private Image  _glowImage;
    private Image  _shineImage;
    private Button _button;

    private Color  _normalColor;
    private Color  _brightColor;
    private Color  _glowColor;

    private Coroutine _flashCoroutine;

    // ── Constantes de animación ───────────────────────────────────────────────
    private const float SCALE_FLASH   = 1.10f;
    private const float SCALE_PRESS   = 0.93f;
    private const float SCALE_NORMAL  = 1.00f;
    private const float ANIM_SPEED    = 12f;

    // ═════════════════════════════════════════════════════════════════════════
    // Inicialización
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Inicializa el botón con su índice de color y referencias visuales.
    /// Debe llamarse justo después de AddComponent.
    /// </summary>
    public void Init(int colorIndex, Image mainImg, Image glowImg, Image shineImg)
    {
        ColorIndex  = colorIndex;
        _mainImage  = mainImg;
        _glowImage  = glowImg;
        _shineImage = shineImg;
        _button     = GetComponent<Button>();

        // Precalcular colores
        Color col = mainImg.color;
        _normalColor = new Color(col.r * 0.40f, col.g * 0.40f, col.b * 0.40f, 1f);
        _brightColor = col;
        _glowColor   = new Color(col.r, col.g, col.b, 0.45f);

        // Aplicar estado inicial (apagado)
        _mainImage.color = _normalColor;
        SetGlow(0f);

        // Registrar click
        if (_button != null)
            _button.onClick.AddListener(HandleClick);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Interactividad
    // ═════════════════════════════════════════════════════════════════════════

    public void SetInteractive(bool value)
    {
        Interactive = value;
        if (_button != null) _button.interactable = value;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Flash (reproducción de secuencia)
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Ilumina el botón durante <paramref name="duration"/> segundos.
    /// Usado por el GameManager al mostrar la secuencia.
    /// </summary>
    public IEnumerator Flash(float duration)
    {
        if (_flashCoroutine != null)
            StopCoroutine(_flashCoroutine);

        _flashCoroutine = StartCoroutine(DoFlash(duration));
        yield return _flashCoroutine;
    }

    private IEnumerator DoFlash(float duration)
    {
        // Encender
        LightUp(true);
        transform.localScale = Vector3.one * SCALE_FLASH;

        yield return new WaitForSeconds(duration);

        // Apagar
        LightUp(false);

        // Suavizar la vuelta a escala normal
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

    // ═════════════════════════════════════════════════════════════════════════
    // Flash de feedback (correcto / incorrecto)
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>Breve destello de click cuando el jugador pulsa el botón.</summary>
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

    // ═════════════════════════════════════════════════════════════════════════
    // Helpers visuales
    // ═════════════════════════════════════════════════════════════════════════

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

    // ═════════════════════════════════════════════════════════════════════════
    // Input handler
    // ═════════════════════════════════════════════════════════════════════════

    private void HandleClick()
    {
        if (!Interactive) return;
        OnPressed?.Invoke(ColorIndex);
    }
}
