using UnityEngine;

public class ObjectMover : MonoBehaviour
{
    [Header("Velocidad (px/s)")]
    public float speed          = 160f;
    [Header("Segundos entre cambios de dirección")]
    public float dirChangeRate  = 2.5f;
    [Header("Amplitud de oscilación Perlin")]
    public float driftAmp       = 28f;

    [HideInInspector] public float boundsXMin = -840f;
    [HideInInspector] public float boundsXMax =  840f;
    [HideInInspector] public float boundsYMin = -380f;
    [HideInInspector] public float boundsYMax =  320f;

    [HideInInspector] public RectTransform ObjectRT;

    bool    _active;
    Vector2 _target;
    float   _dirTimer;
    float   _noiseOffX, _noiseOffY;

    public void StartMoving()
    {
        _active    = true;
        _noiseOffX = Random.value * 100f;
        _noiseOffY = Random.value * 100f;
        PickTarget();
    }

    public void StopMoving() => _active = false;

    void Update()
    {
        if (!_active || ObjectRT == null) return;

        _dirTimer -= Time.deltaTime;
        if (_dirTimer <= 0f) PickTarget();

        Vector2 pos = ObjectRT.anchoredPosition;

        Vector2 toTarget = _target - pos;
        if (toTarget.magnitude < 25f) PickTarget();
        pos += toTarget.normalized * speed * Time.deltaTime;

        float t = Time.time * 0.55f;
        pos.x += (Mathf.PerlinNoise(t + _noiseOffX, 0f) - 0.5f) * driftAmp * Time.deltaTime;
        pos.y += (Mathf.PerlinNoise(0f, t + _noiseOffY) - 0.5f) * driftAmp * Time.deltaTime;

        if (pos.x < boundsXMin) { pos.x = boundsXMin; _target.x = Random.Range(0f, boundsXMax); }
        if (pos.x > boundsXMax) { pos.x = boundsXMax; _target.x = Random.Range(boundsXMin, 0f); }
        if (pos.y < boundsYMin) { pos.y = boundsYMin; _target.y = Random.Range(0f, boundsYMax); }
        if (pos.y > boundsYMax) { pos.y = boundsYMax; _target.y = Random.Range(boundsYMin, 0f); }

        ObjectRT.anchoredPosition = pos;
    }

    void PickTarget()
    {
        _target   = new Vector2(
            Random.Range(boundsXMin * 0.85f, boundsXMax * 0.85f),
            Random.Range(boundsYMin * 0.85f, boundsYMax * 0.85f));
        _dirTimer = dirChangeRate + Random.Range(-0.4f, 0.6f);
    }
}
