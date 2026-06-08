using System;
using UnityEngine;

public class SilentCountdownInputHandler : MonoBehaviour
{

    public event Action OnPlayerPressed;

    public bool AcceptInput { get; set; } = false;

    void Update()
    {
        if (!AcceptInput) return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            Fire();
        }
    }

    public void PressButton()
    {
        if (!AcceptInput) return;
        Fire();
    }

    void Fire()
    {
        AcceptInput = false;
        OnPlayerPressed?.Invoke();
    }
}
