// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using System;
using UnityEngine;

public enum DFMDirection { Left, Right, Up, Down }

/// <summary>
/// Entrada del Flanker: flechas del teclado (o A/D) y los botones ◄ ► de la UI.
/// OnAnswer(true) = derecha, OnAnswer(false) = izquierda.
/// </summary>
public class DontFollowMajorityInputHandler : MonoBehaviour
{
    public bool AcceptInput { get; set; } = false;

    public event Action<bool> OnAnswer;

    void Update()
    {
        if (!AcceptInput) return;

        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            Fire(false);
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            Fire(true);
    }

    /// <summary>Llamado por los botones ◄ ► de la UI.</summary>
    public void Press(bool right)
    {
        if (!AcceptInput) return;
        Fire(right);
    }

    void Fire(bool right)
    {
        AcceptInput = false;
        OnAnswer?.Invoke(right);
    }
}
