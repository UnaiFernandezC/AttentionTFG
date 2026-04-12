using UnityEngine;

public class CameraTransition : MonoBehaviour
{
    [Header("Transición")]
    public Transform puntoInicio;     // Posición y rotación inicial
    public Transform puntoDestino;    // Posición y rotación final
    public float duracion = 2f;

    private float tiempo;
    private bool moviendo = true;

    void Start()
    {
        if (puntoInicio != null)
        {
            transform.position = puntoInicio.position;
            transform.rotation = puntoInicio.rotation;
        }
        else
        {
            Debug.LogWarning("No se asignó un punto de inicio. Usando posición actual.");
        }

        tiempo = 0f;
    }

    void Update()
    {
        if (!moviendo || puntoDestino == null) return;

        tiempo += Time.deltaTime;
        float t = Mathf.Clamp01(tiempo / duracion);

        transform.position = Vector3.Lerp(
            puntoInicio != null ? puntoInicio.position : transform.position,
            puntoDestino.position,
            t
        );

        transform.rotation = Quaternion.Slerp(
            puntoInicio != null ? puntoInicio.rotation : transform.rotation,
            puntoDestino.rotation,
            t
        );

        if (t >= 1f)
        {
            moviendo = false;
        }
    }
}
