using System;
using UnityEngine;

/// <summary>
/// Detecta la entrada del jugador (click izquierdo o barra espaciadora)
/// y la reenvía al ReactionManager únicamente cuando AcceptInput == true.
///
/// Se desacopla del GameManager para mantener los scripts pequeños y
/// reutilizables en variantes de dificultad.
/// </summary>
public class QuickReactionInputHandler : MonoBehaviour
{
    /// <summary>
    /// Mientras sea false, ningún input llega al ReactionManager.
    /// El GameManager lo activa al inicio de cada ronda y lo desactiva
    /// en cuanto se resuelve.
    /// </summary>
    public bool AcceptInput { get; set; } = false;

    /// <summary>
    /// Disparado cada vez que el jugador pulsa (independientemente de si
    /// el estado es válido). El GameManager decide qué hacer con él.
    /// </summary>
    public event Action OnInputDetected;

    // ═════════════════════════════════════════════════════════════════════

    void Update()
    {
        if (!AcceptInput) return;

        bool pressed = Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space);
        if (pressed)
            OnInputDetected?.Invoke();
    }
}
