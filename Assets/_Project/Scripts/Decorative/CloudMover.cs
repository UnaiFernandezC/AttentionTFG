using UnityEngine;

public class CloudMover : MonoBehaviour
{
    public float speed = 0.5f;
    public float screenLimitRight = 11f;
    public float screenLimitLeft = -11f;

    void Update()
    {

        transform.Translate(Vector3.right * speed * Time.deltaTime);

        if (transform.position.x > screenLimitRight)
        {
            transform.position = new Vector3(screenLimitLeft, transform.position.y, transform.position.z);
        }
    }
}
