using System;
using UnityEngine;

public class DontFollowMajorityInputHandler : MonoBehaviour
{

    public event Action<DFMDirection> OnDirectionInput;

    public bool AcceptInput { get; set; } = false;

    void Update()
    {
        if (!AcceptInput) return;

        if (Input.GetKeyDown(KeyCode.LeftArrow)  || Input.GetKeyDown(KeyCode.A))
            Fire(DFMDirection.Left);
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            Fire(DFMDirection.Right);
        else if (Input.GetKeyDown(KeyCode.UpArrow)    || Input.GetKeyDown(KeyCode.W))
            Fire(DFMDirection.Up);
        else if (Input.GetKeyDown(KeyCode.DownArrow)  || Input.GetKeyDown(KeyCode.S))
            Fire(DFMDirection.Down);
    }

    public void PressDirection(DFMDirection d)
    {
        if (!AcceptInput) return;
        Fire(d);
    }

    void Fire(DFMDirection d)
    {
        AcceptInput = false;
        OnDirectionInput?.Invoke(d);
    }
}
