using UnityEngine;

public class PlataformaBalanza : MonoBehaviour
{
    public KeyCode teclaIzquierda = KeyCode.LeftArrow;
    public KeyCode teclaDerecha = KeyCode.RightArrow;
    public float velocidad = 40f;
    public float anguloMax = 15f;

    private float anguloActual = 0f;
    private Quaternion rotacionInicial;

    void Start()
    {
        // Guardamos la rotación inicial
        rotacionInicial = transform.localRotation;
    }

    void Update()
    {
        float rotacion = 0f;

        if (Input.GetKey(teclaIzquierda))
            rotacion += velocidad * Time.deltaTime;

        if (Input.GetKey(teclaDerecha))
            rotacion -= velocidad * Time.deltaTime;

        anguloActual = Mathf.Clamp(anguloActual + rotacion, -anguloMax, anguloMax);

        // ✅ Inclinación sobre el eje LOCAL, usando transform.right (hacia adelante y atrás)
        transform.localRotation = rotacionInicial * Quaternion.AngleAxis(anguloActual, transform.right);
    }
}
