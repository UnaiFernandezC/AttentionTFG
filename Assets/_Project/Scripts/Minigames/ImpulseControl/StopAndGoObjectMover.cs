using UnityEngine;

public class StopAndGoObjectMover : MonoBehaviour
{

    [Header("Movement")]
    public float degreesPerSecond = 80f;
    public float trackRadius      = 180f;

    public float CurrentAngle { get; private set; } = 0f;

    private bool _running = false;
    private RectTransform _markerRT;

    public void Init(RectTransform markerRT)
    {
        _markerRT = markerRT;
        CurrentAngle = 0f;
        _running = false;
        PlaceMarker();
    }

    public void StartMoving()
    {
        _running = true;
    }

    public void StopMoving()
    {
        _running = false;
    }

    public void ResumeMoving()
    {
        _running = true;
    }

    void Update()
    {
        if (!_running || _markerRT == null) return;

        CurrentAngle = (CurrentAngle + degreesPerSecond * Time.deltaTime) % 360f;
        PlaceMarker();
    }

    void PlaceMarker()
    {
        if (_markerRT == null) return;

        float rad = (90f - CurrentAngle) * Mathf.Deg2Rad;
        float x   = Mathf.Cos(rad) * trackRadius;
        float y   = Mathf.Sin(rad) * trackRadius;
        _markerRT.anchoredPosition = new Vector2(x, y);
    }
}
