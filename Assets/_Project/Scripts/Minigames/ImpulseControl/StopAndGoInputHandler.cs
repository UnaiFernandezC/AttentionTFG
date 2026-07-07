// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using System;
using UnityEngine;

/// <summary>
/// Entrada del GO/NO-GO: ESPACIO, clic/tap en cualquier parte de la pantalla
/// o toque directo sobre el circulo (via Press desde la UI).
/// </summary>
public class StopAndGoInputHandler : MonoBehaviour
{
    public bool AcceptInput { get; set; } = false;

    public event Action OnPress;

    bool _firedThisFrame;

    void Update()
    {
        _firedThisFrame = false;
        if (!AcceptInput) return;

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
            Fire();
    }

    /// <summary>Llamado por el boton-circulo de la UI.</summary>
    public void Press()
    {
        if (!AcceptInput) return;
        Fire();
    }

    void Fire()
    {
        if (_firedThisFrame) return;   // evita doble disparo clic + boton
        _firedThisFrame = true;
        OnPress?.Invoke();
    }
}
