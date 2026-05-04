using System;
using UnityEngine;

public class StopAndGoInputHandler : MonoBehaviour
{

    public event Action OnStopPressed;

    public bool AcceptInput { get; set; } = false;

    void Update()
    {
        if (!AcceptInput) return;

        if (Input.GetKeyDown(KeyCode.Space) ||
            Input.GetMouseButtonDown(0))
        {
            AcceptInput = false;
            OnStopPressed?.Invoke();
        }
    }

    public void PressStop()
    {
        if (!AcceptInput) return;
        AcceptInput = false;
        OnStopPressed?.Invoke();
    }
}
