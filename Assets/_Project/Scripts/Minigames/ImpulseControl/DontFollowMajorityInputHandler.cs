using System;
using UnityEngine;

/// <summary>
/// Captura la dirección elegida por el jugador:
///   - Flechas del teclado o WASD
///   - Botones on-screen a través de PressDirection()
///
/// AcceptInput debe activarse desde el GameManager cuando proceda.
/// Los botones de UI llaman PressDirection() → también pasa por AcceptInput,
/// evitando doble disparo entre teclado y botón en el mismo frame.
/// </summary>
public class DontFollowMajorityInputHandler : MonoBehaviour
{
    // ------------------------------------------------------------------ //
    // Eventos
    // ------------------------------------------------------------------ //
    public event Action<DFMDirection> OnDirectionInput;

    // ------------------------------------------------------------------ //
    // Estado
    // ------------------------------------------------------------------ //
    public bool AcceptInput { get; set; } = false;

    // ------------------------------------------------------------------ //
    // Update — teclado
    // ------------------------------------------------------------------ //
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

    // ------------------------------------------------------------------ //
    // API para botones on-screen
    // ------------------------------------------------------------------ //
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
