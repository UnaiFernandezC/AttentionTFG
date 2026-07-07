// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using UnityEngine;

public class FloatingLogo : MonoBehaviour
{
    public float floatAmplitude = 10f;
    public float floatFrequency = 1f;

    private Vector3 startPos;

    void Start()
    {
        startPos = transform.localPosition;
    }

    void Update()
    {
        float yOffset = Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
        transform.localPosition = startPos + new Vector3(0f, yOffset, 0f);
    }
}
