// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using System;
using UnityEngine;

/// <summary>
/// Entrada del "semaforo escondido": ESPACIO o el boton grande de la UI.
/// </summary>
public class SilentCountdownInputHandler : MonoBehaviour
{
    public bool AcceptInput { get; set; } = false;

    public event Action OnPress;

    void Update()
    {
        if (!AcceptInput) return;
        if (Input.GetKeyDown(KeyCode.Space))
            OnPress?.Invoke();
    }

    /// <summary>Llamado por el boton "¡AHORA!" de la UI.</summary>
    public void Press()
    {
        if (!AcceptInput) return;
        OnPress?.Invoke();
    }
}
