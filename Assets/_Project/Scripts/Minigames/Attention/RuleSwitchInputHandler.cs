// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using System;
using UnityEngine;

public class RuleSwitchInputHandler : MonoBehaviour
{

    public bool AcceptInput { get; set; } = false;

    public event Action OnPlayerChoose;

    void Update()
    {
        if (!AcceptInput) return;
        if (Input.GetKeyDown(KeyCode.Space))
            OnPlayerChoose?.Invoke();
    }
}
