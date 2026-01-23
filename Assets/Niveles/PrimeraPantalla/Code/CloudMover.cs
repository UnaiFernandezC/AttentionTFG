using UnityEngine;

public class CloudMover : MonoBehaviour
{
    public float speed = 0.5f;
    public float screenLimitRight = 11f;
    public float screenLimitLeft = -11f;

    void Update()
    {
        // Mover hacia la derecha
        transform.Translate(Vector3.right * speed * Time.deltaTime);

        // Si sale por la derecha, vuelve por la izquierda, manteniendo la misma altura (Y)
        if (transform.position.x > screenLimitRight)
        {
            transform.position = new Vector3(screenLimitLeft, transform.position.y, transform.position.z);
        }
    }
}
