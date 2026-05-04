using UnityEngine;

public class RotatingCoin : MonoBehaviour
{
    public float rotationSpeed = 90f;

    void Update()
    {

        transform.Rotate(Vector3.forward * rotationSpeed * Time.deltaTime);

    }
}
