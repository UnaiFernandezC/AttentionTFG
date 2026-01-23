using UnityEngine;

public class RotatingCoin : MonoBehaviour
{
    public float rotationSpeed = 90f; // grados por segundo

    void Update()
    {
        // Rota en el eje X (de lado)
        transform.Rotate(Vector3.forward * rotationSpeed * Time.deltaTime);
        // Si quieres girar en Z cambia a: Vector3.forward
    }
}
