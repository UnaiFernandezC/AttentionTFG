// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using UnityEngine;

/// <summary>
/// (Legado) El antiguo Stop & Go movia un marcador circular. El minijuego se
/// rediseño como GO/NO-GO y ya no necesita movimiento, pero la clase se
/// mantiene porque las escenas existentes la referencian por GUID.
/// </summary>
public class StopAndGoObjectMover : MonoBehaviour
{
    [HideInInspector] public float degreesPerSecond = 80f;
    [HideInInspector] public float trackRadius      = 180f;
}
