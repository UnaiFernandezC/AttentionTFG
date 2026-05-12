using System;
using UnityEngine;

/// <summary>
/// Captura la pulsación del jugador (ESPACIO o botón on-screen) y emite OnPlayerPressed.
/// El ratón se gestiona SOLO por botón (Button.onClick), nunca por GetMouseButtonDown,
/// para evitar que el clic que para el contador avance la pantalla de resultado
/// en el mismo frame.
/// </summary>
public class SilentCountdownInputHandler : MonoBehaviour
{
    // ------------------------------------------------------------------ //
    // Eventos
    // ------------------------------------------------------------------ //
    public event Action OnPlayerPressed;

    // ------------------------------------------------------------------ //
    // Estado
    // ------------------------------------------------------------------ //
    public bool AcceptInput { get; set; } = false;

    // ------------------------------------------------------------------ //
    // Update
    // ------------------------------------------------------------------ //
    void Update()
    {
        if (!AcceptInput) return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            Fire();
        }
    }

    // ------------------------------------------------------------------ //
    // API para el botón on-screen
    // ------------------------------------------------------------------ //
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
