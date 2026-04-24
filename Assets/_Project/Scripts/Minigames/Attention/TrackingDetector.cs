using System;
using UnityEngine;

/// <summary>
/// Detecta si el cursor está sobre el objeto en movimiento.
/// Usa distancia en coordenadas canvas, no eventos UI (más fiable con objetos que se mueven).
/// </summary>
public class TrackingDetector : MonoBehaviour
{
    // Asignados por GameManager tras BuildUI
    [HideInInspector] public RectTransform CanvasRT;
    [HideInInspector] public RectTransform ObjectRT;
    [HideInInspector] public float         TrackRadius = 55f;   // radio de detección (px canvas)

    public bool IsTracking { get; private set; }
    public bool Active     { get; set; } = false;

    public event Action OnTrackingGained;
    public event Action OnTrackingLost;

    bool _wasTracking;
    Camera _cam;   // null para ScreenSpaceOverlay

    // ═════════════════════════════════════════════════════════════════════

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
