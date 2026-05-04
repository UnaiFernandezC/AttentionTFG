using System;
using UnityEngine;

public class TrackingDetector : MonoBehaviour
{

    [HideInInspector] public RectTransform CanvasRT;
    [HideInInspector] public RectTransform ObjectRT;
    [HideInInspector] public float         TrackRadius = 55f;

    public bool IsTracking { get; private set; }
    public bool Active     { get; set; } = false;

    public event Action OnTrackingGained;
    public event Action OnTrackingLost;

    bool _wasTracking;
    Camera _cam;

    void Update()
    {
        if (!Active || CanvasRT == null || ObjectRT == null) return;

        Vector2 local;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            CanvasRT, Input.mousePosition, _cam, out local);

        float dist = Vector2.Distance(local, ObjectRT.anchoredPosition);
        IsTracking = dist <= TrackRadius;

        if (IsTracking && !_wasTracking) OnTrackingGained?.Invoke();
        if (!IsTracking && _wasTracking)  OnTrackingLost?.Invoke();
        _wasTracking = IsTracking;
    }
}
