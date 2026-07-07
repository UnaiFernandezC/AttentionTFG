// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using UnityEngine;

/// <summary>
/// Componente ligero por bola en el minijuego de seguimiento multiple (MOT).
/// Reenvia el toque de la bola al TrackingGameManager mediante un callback.
/// La instancia colocada en la escena (GameController) permanece inactiva.
/// </summary>
public class TrackingDetector : MonoBehaviour
{
    public System.Action OnTapped;

    public void Tap() => OnTapped?.Invoke();
}
