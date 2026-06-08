using UnityEngine;

public class SilentCountdownTimerManager : MonoBehaviour
{

    public float ElapsedTime { get; private set; } = 0f;
    public bool  IsRunning   { get; private set; } = false;

    public void StartCounting()
    {
        ElapsedTime = 0f;
        IsRunning   = true;
    }

    public float StopCounting()
    {
        IsRunning = false;
        return ElapsedTime;
    }

    public void Reset()
    {
        ElapsedTime = 0f;
    }

    void Update()
    {
        if (IsRunning)
            ElapsedTime += Time.deltaTime;
    }
}
