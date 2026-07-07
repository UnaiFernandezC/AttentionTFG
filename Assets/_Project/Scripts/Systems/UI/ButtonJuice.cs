// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Squash & stretch para botones: crece al pasar el ratón, se "aplasta" al pulsar.
///
/// DISEÑO ANTI-TIRONES: en reposo NO escribe el transform (cero conflictos con
/// UITween.PopIn/PulseOnce y cero rebuilds de Canvas por frame). Solo toma el
/// control de la escala mientras hay interacción, capturando como base la escala
/// que tenga el elemento en ese momento, y la suelta al volver al reposo.
/// Funciona con Time.timeScale = 0.
/// </summary>
public class ButtonJuice : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] float hoverScale = 1.06f;
    [SerializeField] float pressScale = 0.92f;
    [SerializeField] float speed = 14f;

    float _mult = 1f;            // multiplicador propio, independiente de otras animaciones
    bool _hover, _down;
    Vector3 _baseScale = Vector3.one;
    bool _controlling;           // true solo mientras esta clase escribe el transform

    public static ButtonJuice Attach(GameObject go)
    {
        var j = go.GetComponent<ButtonJuice>();
        if (j == null) j = go.AddComponent<ButtonJuice>();
        return j;
    }

    void OnDisable()
    {
        // Si se desactiva a mitad de interacción, deja la escala limpia.
        if (_controlling) transform.localScale = _baseScale;
        _hover = _down = false;
        _mult = 1f;
        _controlling = false;
    }

    void Update()
    {
        float target = _down ? pressScale : (_hover ? hoverScale : 1f);

        // REPOSO: sin interacción y multiplicador ya en 1 → no tocar el transform.
        if (!_hover && !_down && Mathf.Abs(_mult - 1f) < 0.001f)
        {
            if (_controlling)
            {
                transform.localScale = _baseScale;   // restaura exacto y suelta el control
                _controlling = false;
            }
            _mult = 1f;
            return;
        }

        // Toma el control: captura como base la escala actual (respeta lo que
        // hayan dejado PopIn/PulseOnce ya terminados).
        if (!_controlling)
        {
            _baseScale = transform.localScale;
            if (_baseScale == Vector3.zero) _baseScale = Vector3.one;
            _controlling = true;
        }

        _mult = Mathf.Lerp(_mult, target,
                           1f - Mathf.Exp(-speed * Time.unscaledDeltaTime));
        if (Mathf.Abs(_mult - target) < 0.002f) _mult = target;
        transform.localScale = _baseScale * _mult;
    }

    public void OnPointerEnter(PointerEventData e) => _hover = true;
    public void OnPointerExit(PointerEventData e)  { _hover = false; _down = false; }
    public void OnPointerDown(PointerEventData e)  => _down = true;
    public void OnPointerUp(PointerEventData e)    => _down = false;
}
