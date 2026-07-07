// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AttractionCursorController : MonoBehaviour
{

    public Vector2 CursorCanvasPos { get; private set; }

    [HideInInspector] public float damping             = 2.2f;
    [HideInInspector] public float maxPullOffset       = 280f;
    [HideInInspector] public float cursorRadius        = 18f;

    [HideInInspector] public float instabilityStrength = 0f;

    RectTransform      _canvasRT;
    RectTransform      _cursorRT;
    Image              _cursorImg;
    AttractionController _attraction;

    Vector2 _pullOffset = Vector2.zero;

    static readonly Color COL_SAFE   = new Color(0.85f, 0.95f, 1.00f, 1f);
    static readonly Color COL_WARN   = new Color(1.00f, 0.88f, 0.20f, 1f);
    static readonly Color COL_DANGER = new Color(0.95f, 0.45f, 0.15f, 1f);
    static readonly Color COL_CRIT   = new Color(0.95f, 0.18f, 0.22f, 1f);

    public void Initialize(RectTransform canvasRT, RectTransform cursorRT,
                           AttractionController attraction)
    {
        _canvasRT   = canvasRT;
        _cursorRT   = cursorRT;
        _cursorImg  = cursorRT.GetComponent<Image>();
        _attraction = attraction;
        _pullOffset = Vector2.zero;

        CursorCanvasPos = Vector2.zero;
        _cursorRT.anchoredPosition = Vector2.zero;
    }

    public void Tick()
    {
        float dt = Time.deltaTime;

        Vector2 mouseCanvasPos = GetMouseCanvasPos();

        Vector2 force = _attraction.CalculateTotalForce(CursorCanvasPos);

        if (instabilityStrength > 0f)
        {
            float angle = Time.time * 0.70f;

            float angle2 = Time.time * 0.43f + 1.2f;
            var drift = new Vector2(
                Mathf.Cos(angle)  * 0.65f + Mathf.Cos(angle2) * 0.35f,
                Mathf.Sin(angle)  * 0.65f + Mathf.Sin(angle2) * 0.35f);
            force += drift.normalized * instabilityStrength;
        }

        _pullOffset += force * dt;

        _pullOffset = Vector2.Lerp(_pullOffset, Vector2.zero, damping * dt);

        if (_pullOffset.magnitude > maxPullOffset)
            _pullOffset = _pullOffset.normalized * maxPullOffset;

        CursorCanvasPos = mouseCanvasPos + _pullOffset;
        _cursorRT.anchoredPosition = CursorCanvasPos;

        float danger = _attraction.GetDangerLevel(CursorCanvasPos);
        UpdateCursorColor(danger);
    }

    public bool IsInSafeZone(float safeRadius)
    {
        return CursorCanvasPos.magnitude <= safeRadius;
    }

    public bool IsTouchingStimulus()
    {
        return _attraction.IsTouchingAny(CursorCanvasPos, cursorRadius);
    }

    public float DangerLevel => _attraction.GetDangerLevel(CursorCanvasPos);

    Vector2 GetMouseCanvasPos()
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRT, Input.mousePosition, null, out Vector2 localPos);
        return localPos;
    }

    void UpdateCursorColor(float danger)
    {
        if (_cursorImg == null) return;

        Color col;
        if      (danger < 0.25f) col = Color.Lerp(COL_SAFE,   COL_WARN,   danger / 0.25f);
        else if (danger < 0.55f) col = Color.Lerp(COL_WARN,   COL_DANGER, (danger - 0.25f) / 0.30f);
        else                     col = Color.Lerp(COL_DANGER,  COL_CRIT,   (danger - 0.55f) / 0.45f);

        _cursorImg.color = col;
    }
}
