using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controla el comportamiento de una sola carta en el minijuego Parejas de Colores.
/// Gestiona su estado (oculta / revelada / emparejada) y la animación de giro.
/// </summary>
[RequireComponent(typeof(Button))]
public class CardController : MonoBehaviour
{
    // ─── Estado público ────────────────────────────────────────────────────
    public int     ColorId    { get; private set; }
    public bool    IsRevealed { get; private set; }
    public bool    IsMatched  { get; private set; }

    // ─── Evento ────────────────────────────────────────────────────────────
    /// <summary>El BoardManager se suscribe a este evento para gestionar la selección.</summary>
    public System.Action<CardController> OnCardClicked;

    // ─── Referencia al panel frontal (color) y trasero (fondo) ────────────
    private Image _backImage;   // panel siempre visible al inicio
    private Image _frontImage;  // panel con el color de la pareja
    private Button _button;

    // ─── Paleta visual ────────────────────────────────────────────────────
    private static readonly Color BACK_COLOR    = new Color(0.22f, 0.22f, 0.38f);  // azul oscuro neutro
    private static readonly Color MATCHED_TINT  = new Color(1f, 1f, 1f, 0.55f);    // overlay blanco suave
    private static readonly Color BACK_HOVER    = new Color(0.28f, 0.28f, 0.46f);  // hover del reverso

    // Duración de la animación de giro (segundos para cada mitad)
    private const float FLIP_HALF_DURATION = 0.12f;

    // ─── Inicialización ───────────────────────────────────────────────────

    /// <summary>
    /// Configura la carta con su ID de color y su color visual.
    /// Llamado por BoardManager al crear la carta.
    /// </summary>
    public void Initialize(int colorId, Color faceColor, Image backImg, Image frontImg)
    {
        ColorId     = colorId;
        _backImage  = backImg;
        _frontImage = frontImg;
        _button     = GetComponent<Button>();

        // Color del frente
        _frontImage.color = faceColor;

        // Estado inicial: boca abajo
        _frontImage.gameObject.SetActive(false);
        _backImage.color = BACK_COLOR;

        // Listeners de botón
        _button.onClick.AddListener(HandleClick);

        // Quitar colores de transición por defecto del Button para no interferir
        var cb = _button.colors;
        cb.normalColor      = Color.white;
        cb.highlightedColor = new Color(1f, 1f, 1f, 0.9f);
        cb.pressedColor     = new Color(0.85f, 0.85f, 0.85f);
        cb.selectedColor    = Color.white;
        cb.fadeDuration     = 0.05f;
        _button.colors = cb;
    }

    // ─── Interacción ──────────────────────────────────────────────────────

    private void HandleClick()
    {
        if (IsRevealed || IsMatched) return;
        OnCardClicked?.Invoke(this);
    }

    // ─── Animaciones ──────────────────────────────────────────────────────

    /// <summary>Revela la carta con animación de giro.</summary>
    public void FlipReveal()
    {
        if (IsRevealed || IsMatched) return;
        IsRevealed = true;
        _button.interactable = false;
        StartCoroutine(FlipAnimation(reveal: true));
    }

    /// <summary>Oculta la carta con animación de giro.</summary>
    public void FlipHide()
    {
        IsRevealed = false;
        _button.interactable = true;
        StartCoroutine(FlipAnimation(reveal: false));
    }

    /// <summary>Marca la carta como emparejada (queda revelada definitivamente).</summary>
    public void SetMatched()
    {
        IsMatched = true;
        _button.interactable = false;

        // Overlay de "emparejada": añade un tinte brillante al frente
        StartCoroutine(MatchedPulse());
    }

    // ─── Corrutinas de animación ──────────────────────────────────────────

    private IEnumerator FlipAnimation(bool reveal)
    {
        // Mitad 1: escalar X a 0 (carta "se cierra")
        yield return ScaleX(1f, 0f);

        // Cambiar qué cara se muestra
        _backImage.gameObject.SetActive(!reveal);
        _frontImage.gameObject.SetActive(reveal);

        // Mitad 2: escalar X de 0 a 1 (carta "se abre")
        yield return ScaleX(0f, 1f);

        if (reveal)
            _button.interactable = false;
        else
            _button.interactable = true;
    }

    private IEnumerator ScaleX(float from, float to)
    {
        float elapsed = 0f;
        Vector3 scale = transform.localScale;

        while (elapsed < FLIP_HALF_DURATION)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / FLIP_HALF_DURATION);
            // Ease in-out suave
            t = t * t * (3f - 2f * t);
            scale.x = Mathf.Lerp(from, to, t);
            transform.localScale = scale;
            yield return null;
        }

        scale.x = to;
        transform.localScale = scale;
    }

    private IEnumerator MatchedPulse()
    {
        // Pulso de escala: ligeramente más grande y luego normal
        float duration = 0.25f;
        float elapsed  = 0f;
        Vector3 baseScale = Vector3.one;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float s = 1f + 0.12f * Mathf.Sin(t * Mathf.PI);
            transform.localScale = baseScale * s;
            yield return null;
        }

        transform.localScale = baseScale;

        // Overlay blanco suave sobre el color
        if (_frontImage != null)
        {
            Color c = _frontImage.color;
            c.r = Mathf.Min(1f, c.r + 0.15f);
            c.g = Mathf.Min(1f, c.g + 0.15f);
            c.b = Mathf.Min(1f, c.b + 0.15f);
            _frontImage.color = c;
        }
    }
}
