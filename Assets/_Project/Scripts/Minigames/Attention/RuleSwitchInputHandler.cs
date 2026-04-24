using System;
using UnityEngine;

/// <summary>
/// Gestiona la entrada por teclado del minijuego "Cambio de regla".
///
/// El click en el estímulo se gestiona directamente a través del Button
/// que añade RuleSwitchStimulusManager. Este handler captura la barra
/// ESPACIADORA como alternativa de teclado.
///
/// AcceptInput actúa como puerta: si es false, ninguna entrada pasa al juego.
/// </summary>
public class RuleSwitchInputHandler : MonoBehaviour
{
    /// <summary>Solo se acepta input cuando está a true.</summary>
    public bool AcceptInput { get; set; } = false;

    /// <summary>Disparado cuando el jugador elige el estímulo (ESPACIO).</summary>
    public event Action OnPlayerChoose;

    void Update()
    {
        if (!AcceptInput) return;
        if (Input.GetKeyDown(KeyCode.Space))
            OnPlayerChoose?.Invoke();
    }
}
