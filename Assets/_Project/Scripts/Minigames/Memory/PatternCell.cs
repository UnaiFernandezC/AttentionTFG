using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Representa una casilla individual del grid en el minijuego "Repite el dibujo".
///
/// Estados:
///   Idle        → Casilla neutra (color de fondo)
///   PatternShow → Iluminada durante la fase de memorización (cian)
///   Selected    → Seleccionada por el jugador (azul)
///   Correct     → Seleccionada correctamente al confirmar (verde)
///   Wrong       → Seleccionada por error al confirmar (rojo)
///   Missed      → Formaba parte del patrón pero el jugador no la seleccionó (naranja; muestra la solución)
/// </summary>
[RequireComponent(typeof(Button))]
public class PatternCell : MonoBehaviour
{
    // ─── Estado ───────────────────────────────────────────────────────────
    public enum CellState { Idle, PatternShow, Selected, Correct, Wrong, Missed }

    public int       Index      { get; private set; }
    public CellState State      { get; private set; }
    public bool      IsSelected => State == CellState.Selected;

    // ─── Evento ───────────────────────────────────────────────────────────
    public System.Action<PatternCell> OnClicked;

    // ─── Referencias ──────────────────────────────────────────────────────
    private Image  _bg;
    private Image  _shine;
    private Button _button;

    // ─── Paleta de colores ────────────────────────────────────────────────
    private static readonly Color C_IDLE     = new Color(0.18f, 0.20f, 0.36f);
    private static readonly Color C_PATTERN  = new Color(0.18f, 0.82f, 0.92f);   // Cian
    private static readonly Color C_SELECTED = new Color(0.32f, 0.52f, 0.96f);   // Azul
    private static readonly Color C_CORRECT  = new Color(0.18f, 0.76f, 0.46f);   // Verde
    private static readonly Color C_WRONG    = new Color(0.86f, 0.24f, 0.32f);   // Rojo
    private static readonly Color C_MISSED   = new Color(1.00f, 0.60f, 0.18f);   // Naranja (solución)
    private static readonly Color SHINE_ON   = new Color(1f, 1f, 1f, 0.18f);
    private static readonly Color SHINE_OFF  = new Color(1f, 1f, 1f, 0.00f);

    // ─── Inicialización ───────────────────────────────────────────────────

    public void Initialize(int index, Image bg, Image shine)
    {
        Index   = index;
        _bg     = bg;
        _shine  = shine;
        _button = GetComponent<Button>();

        _button.targetGraphic = _bg;
        var cb = _button.colors;
        cb.normalColor      = Color.white;
        cb.highlightedColor = new Color(1f, 1f, 1f, 0.85f);
        cb.pressedColor     = new Color(0.75f, 0.75f, 0.75f);
        cb.fadeDuration     = 0.05f;
        _button.colors      = cb;

        _button.onClick.AddListener(HandleClick);
        ApplyColor(C_IDLE, SHINE_OFF);
        State = CellState.Idle;
    }

    // ─── API pública ──────────────────────────────────────────────────────

    public void SetState(CellState state)
    {
        State = state;
        StopAllCoroutines();
        transform.localScale = Vector3.one;

        switch (state)
        {
            case CellState.Idle:
                ApplyColor(C_IDLE, SHINE_OFF);
                break;

            case CellState.PatternShow:
                ApplyColor(C_PATTERN, SHINE_ON);
                StartCoroutine(Pulse(0.12f, 0.30f));
                break;

            case CellState.Selected:
                ApplyColor(C_SELECTED, SHINE_ON);
                StartCoroutine(Pulse(0.07f, 0.16f));
                break;

            case CellState.Correct:
                ApplyColor(C_CORRECT, SHINE_ON);
                StartCoroutine(Pulse(0.14f, 0.38f));
                break;

            case CellState.Wrong:
                ApplyColor(C_WRONG, new Color(1f, 1f, 1f, 0.10f));
                StartCoroutine(Shake());
                break;

            case CellState.Missed:
                ApplyColor(C_MISSED, new Color(1f, 1f, 1f, 0.12f));
                StartCoroutine(Pulse(0.10f, 0.28f));
                break;
        }
    }

    public void EnableInput(bool enable) => _button.interactable = enable;

    // ─── Helpers privados ─────────────────────────────────────────────────

    private void HandleClick()
    {
        if (!_button.interactable) return;
        OnClicked?.Invoke(this);
    }

    private void ApplyColor(Color bg, Color shine)
    {
        _bg.color    = bg;
        _shine.color = shine;
    }

    // ─── Animaciones ──────────────────────────────────────────────────────

    private IEnumerator Pulse(float strength, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            transform.localScale = Vector3.one * (1f + strength * Mathf.Sin(t * Mathf.PI));
            yield return null;
        }
        transform.localScale = Vector3.one;
    }

    private IEnumerator Shake()
    {
        float duration = 0.30f, elapsed = 0f;
        Vector3 origin = transform.localPosition;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float offset = Mathf.Sin(t * Mathf.PI * 6f) * 8f * (1f - t);
            transform.localPosition = origin + new Vector3(offset, 0f, 0f);
            yield return null;
        }
        transform.localPosition = origin;
    }
}
