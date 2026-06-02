using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonHoverScaler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [HideInInspector] public Vector3 hoverScale;
    [HideInInspector] public float scaleSpeed = 8f;

    Vector3 _originalScale;
    Vector3 _targetScale;

    void Start()
    {
        _originalScale = transform.localScale;
        hoverScale     = _originalScale * 1.07f;
        _targetScale   = _originalScale;
    }

    void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, _targetScale, Time.deltaTime * scaleSpeed);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _targetScale = hoverScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _targetScale = _originalScale;
    }
}
