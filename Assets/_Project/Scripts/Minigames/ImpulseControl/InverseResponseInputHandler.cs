using System;
using UnityEngine;
using static InverseResponseStimulusManager;

/// <summary>
/// Detecta el input del jugador en "Respuesta Inversa".
///
/// FUENTES DE INPUT:
///   1. Teclado: flechas del cursor (← ↑ → ↓) o WASD
///   2. Botones en pantalla: 4 botones de direccion (asignados via RegisterButton)
///
/// USO:
///   - GameManager establece AcceptInput = true cuando hay un estimulo activo.
///   - Suscribirse a OnDirectionInput(ArrowDirection) para recibir el input.
///   - El handler dispara el evento y desactiva AcceptInput automaticamente
///     para evitar multiples registros por estimulo.
/// </summary>
public class InverseResponseInputHandler : MonoBehaviour
{
    // ── Estado ────────────────────────────────────────────────────────────
    public bool AcceptInput { get; set; } = false;

    // ── Evento ────────────────────────────────────────────────────────────
    /// <summary>
    /// Disparado cuando el jugador pulsa una direccion valida.
    /// Solo se dispara si AcceptInput == true.
    /// </summary>
    public event Action<ArrowDirection> OnDirectionInput;

    // ═════════════════════════════════════════════════════════════════════
    // Unity Update — teclado
    // ═════════════════════════════════════════════════════════════════════

    void Update()
    {
        if (!AcceptInput) return;

        // Flechas del cursor
        if (Input.GetKeyDown(KeyCode.LeftArrow)  || Input.GetKeyDown(KeyCode.A))
            Fire(ArrowDirection.Left);
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            Fire(ArrowDirection.Right);
        else if (Input.GetKeyDown(KeyCode.UpArrow)   || Input.GetKeyDown(KeyCode.W))
            Fire(ArrowDirection.Up);
        else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
            Fire(ArrowDirection.Down);
    }

    // ═════════════════════════════════════════════════════════════════════
    // API publica — botones en pantalla
    // ═════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Llamado por el onClick de cada boton de direccion en pantalla.
    /// </summary>
    public void PressDirection(ArrowDirection dir)
    {
        if (!AcceptInput) return;
        Fire(dir);
    }

    // ═════════════════════════════════════════════════════════════════════
    // Privado
    // ═════════════════════════════════════════════════════════════════════

    void Fire(ArrowDirection dir)
    {
        AcceptInput = false;               // un solo disparo por estimulo
        OnDirectionInput?.Invoke(dir);
    }
}
