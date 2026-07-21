// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class PatternCell : MonoBehaviour
{

    public enum CellState { Idle, PatternShow, Selected, Correct, Wrong, Missed, Decoy }

    public int       Index      { get; private set; }
    public CellState State      { get; private set; }
    public bool      IsSelected => State == CellState.Selected;

    public System.Action<PatternCell> OnClicked;

    private Image  _bg;
    private Image  _shine;
    private Image  _glow;    // halo exterior de la celda (glow morado al revelarse)
    private Button _button;

    private static readonly Color C_IDLE     = new Color(0.20f, 0.16f, 0.38f);   // morado oscuro (paleta Memoria)
    private static readonly Color C_PATTERN  = new Color(0.18f, 0.82f, 0.92f);
    private static readonly Color C_SELECTED = new Color(0.32f, 0.52f, 0.96f);
    private static readonly Color C_CORRECT  = new Color(0.18f, 0.76f, 0.46f);
    private static readonly Color C_WRONG    = new Color(0.86f, 0.24f, 0.32f);
    private static readonly Color C_MISSED   = new Color(1.00f, 0.60f, 0.18f);
    private static readonly Color C_DECOY    = new Color(0.95f, 0.35f, 0.75f);
    private static readonly Color SHINE_ON   = new Color(1f, 1f, 1f, 0.18f);
    private static readonly Color SHINE_OFF  = new Color(1f, 1f, 1f, 0.00f);

    // Colores del halo por estado (solo estetica; el morado es la paleta Memoria)
    private static readonly Color G_IDLE     = new Color(0.58f, 0.28f, 0.92f, 0.10f);
    private static readonly Color G_PATTERN  = new Color(0.58f, 0.28f, 0.92f, 0.50f);
    private static readonly Color G_SELECTED = new Color(0.32f, 0.52f, 0.96f, 0.40f);
    private static readonly Color G_CORRECT  = new Color(0.18f, 0.76f, 0.46f, 0.45f);
    private static readonly Color G_WRONG    = new Color(0.86f, 0.24f, 0.32f, 0.45f);
    private static readonly Color G_MISSED   = new Color(1.00f, 0.60f, 0.18f, 0.40f);
    private static readonly Color G_DECOY    = new Color(0.95f, 0.35f, 0.75f, 0.40f);

    public void Initialize(int index, Image bg, Image shine, Image glow = null)
    {
        Index   = index;
        _bg     = bg;
        _shine  = shine;
        _glow   = glow;
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

    public void SetState(CellState state)
    {
        State = state;
        StopAllCoroutines();
        transform.localScale = Vector3.one;

        switch (state)
        {
            case CellState.Idle:
                ApplyColor(C_IDLE, SHINE_OFF, G_IDLE);
                break;

            case CellState.PatternShow:
                // Revelado: glow morado encendido + pulso (mas legible)
                ApplyColor(C_PATTERN, SHINE_ON, G_PATTERN);
                StartCoroutine(Pulse(0.12f, 0.30f));
                break;

            case CellState.Selected:
                ApplyColor(C_SELECTED, SHINE_ON, G_SELECTED);
                StartCoroutine(Pulse(0.07f, 0.16f));
                break;

            case CellState.Correct:
                ApplyColor(C_CORRECT, SHINE_ON, G_CORRECT);
                StartCoroutine(Pulse(0.14f, 0.38f));
                break;

            case CellState.Wrong:
                ApplyColor(C_WRONG, new Color(1f, 1f, 1f, 0.10f), G_WRONG);
                StartCoroutine(Shake());
                break;

            case CellState.Missed:
                ApplyColor(C_MISSED, new Color(1f, 1f, 1f, 0.12f), G_MISSED);
                StartCoroutine(Pulse(0.10f, 0.28f));
                break;

            case CellState.Decoy:
                // Celda "trampa": parpadea distinto y NO forma parte del patrón.
                ApplyColor(C_DECOY, SHINE_ON, G_DECOY);
                StartCoroutine(Blink());
                break;
        }
    }

    public void EnableInput(bool enable) => _button.interactable = enable;

    private void HandleClick()
    {
        if (!_button.interactable) return;
        OnClicked?.Invoke(this);
    }

    private void ApplyColor(Color bg, Color shine, Color? glow = null)
    {
        _bg.color    = bg;
        _shine.color = shine;
        if (_glow != null && glow != null) _glow.color = glow.Value;
    }

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

    private IEnumerator Blink()
    {
        while (State == CellState.Decoy)
        {
            _bg.color = C_DECOY;
            yield return new WaitForSeconds(0.22f);
            if (State != CellState.Decoy) yield break;
            _bg.color = Color.Lerp(C_DECOY, C_IDLE, 0.65f);
            yield return new WaitForSeconds(0.22f);
        }
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
