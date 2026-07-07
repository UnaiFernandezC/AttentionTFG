// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using UnityEngine;

/// <summary>
/// Mueve una bola (RectTransform de UI) rebotando dentro de unos limites,
/// con pequenos giros aleatorios para que las bolas se crucen y se mezclen.
/// Usado por el minijuego de seguimiento multiple (MOT).
/// Si no se le asigna ObjectRT permanece inactivo (instancia de escena).
/// </summary>
public class ObjectMover : MonoBehaviour
{
    [HideInInspector] public RectTransform ObjectRT;
    [HideInInspector] public float Speed = 240f;

    [HideInInspector] public float boundsXMin = -810f;
    [HideInInspector] public float boundsXMax =  810f;
    [HideInInspector] public float boundsYMin = -430f;
    [HideInInspector] public float boundsYMax =  320f;

    Vector2 _vel;
    bool    _active;
    float   _turnTimer;

    /// <summary>Lanza la bola en una direccion aleatoria.</summary>
    public void Launch()
    {
        float ang = Random.Range(0f, Mathf.PI * 2f);
        _vel = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * Speed;
        _turnTimer = Random.Range(0.7f, 1.5f);
        _active = true;
    }

    public void StopMoving() => _active = false;

    void Update()
    {
        if (!_active || ObjectRT == null) return;

        float dt = Time.deltaTime;

        // Giro suave aleatorio cada poco tiempo (obliga a cruces y mezclas)
        _turnTimer -= dt;
        if (_turnTimer <= 0f)
        {
            float turn = Random.Range(-40f, 40f) * Mathf.Deg2Rad;
            float cos = Mathf.Cos(turn), sin = Mathf.Sin(turn);
            _vel = new Vector2(_vel.x * cos - _vel.y * sin,
                               _vel.x * sin + _vel.y * cos);
            _turnTimer = Random.Range(0.7f, 1.5f);
        }

        Vector2 pos = ObjectRT.anchoredPosition + _vel * dt;

        // Rebotes en los bordes
        if (pos.x < boundsXMin) { pos.x = boundsXMin; _vel.x =  Mathf.Abs(_vel.x); }
        if (pos.x > boundsXMax) { pos.x = boundsXMax; _vel.x = -Mathf.Abs(_vel.x); }
        if (pos.y < boundsYMin) { pos.y = boundsYMin; _vel.y =  Mathf.Abs(_vel.y); }
        if (pos.y > boundsYMax) { pos.y = boundsYMax; _vel.y = -Mathf.Abs(_vel.y); }

        ObjectRT.anchoredPosition = pos;
    }
}
