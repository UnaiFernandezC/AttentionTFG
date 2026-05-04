using System;
using UnityEngine;

public class QuickReactionInputHandler : MonoBehaviour
{

    public bool AcceptInput { get; set; } = false;

    public event Action OnInputDetected;

    void Update()
    {
        if (!AcceptInput) return;

        bool pressed = Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space);
        if (pressed)
            OnInputDetected?.Invoke();
    }
}
