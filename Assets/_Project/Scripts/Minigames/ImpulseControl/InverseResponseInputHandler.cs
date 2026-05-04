using System;
using UnityEngine;
using static InverseResponseStimulusManager;

public class InverseResponseInputHandler : MonoBehaviour
{

    public bool AcceptInput { get; set; } = false;

    public event Action<ArrowDirection> OnDirectionInput;

    void Update()
    {
        if (!AcceptInput) return;

        if (Input.GetKeyDown(KeyCode.LeftArrow)  || Input.GetKeyDown(KeyCode.A))
            Fire(ArrowDirection.Left);
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            Fire(ArrowDirection.Right);
        else if (Input.GetKeyDown(KeyCode.UpArrow)   || Input.GetKeyDown(KeyCode.W))
            Fire(ArrowDirection.Up);
        else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
            Fire(ArrowDirection.Down);
    }

    public void PressDirection(ArrowDirection dir)
    {
        if (!AcceptInput) return;
        Fire(dir);
    }

    void Fire(ArrowDirection dir)
    {
        AcceptInput = false;
        OnDirectionInput?.Invoke(dir);
    }
}
