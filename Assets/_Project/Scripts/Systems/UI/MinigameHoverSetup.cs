// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using UnityEngine;

/// <summary>
/// Aplica el efecto de hover a los botones de minijuego de la escena.
/// Unificado en el sistema nuevo (ButtonJuice, tiempo no escalado). Si encuentra
/// el sistema antiguo (ButtonHoverScaler) lo retira para evitar que dos efectos
/// peleen por la escala del mismo objeto.
/// </summary>
public class MinigameHoverSetup : MonoBehaviour
{
    void Start()
    {
        var allObjects = FindObjectsOfType<GameObject>(includeInactive: false);
        foreach (var go in allObjects)
        {
            if (go.name.StartsWith("Minigame"))
            {
                // Migrar al sistema unificado: quitar el hover antiguo si existe.
                var old = go.GetComponent<ButtonHoverScaler>();
                if (old != null) Destroy(old);

                // ButtonJuice.Attach es idempotente (no duplica el componente).
                ButtonJuice.Attach(go);
            }
        }
    }
}
