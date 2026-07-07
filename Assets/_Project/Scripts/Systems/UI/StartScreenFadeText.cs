// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using UnityEngine;
using TMPro;

/// <summary>
/// Texto parpadeante "Pulsa ENTER" de la pantalla inicial.
/// Flujo simplificado:
///  - Si la pantalla de perfiles está abierta, ENTER no hace nada (se elige tocando).
///  - Con perfil activo → directo al selector de categorías de SU dificultad.
///  - Sin perfil (modo invitado) → selector de dificultad clásico.
/// </summary>
public class StartScreenFadeText : MonoBehaviour
{
    public TextMeshProUGUI pressEnterText;
    public float fadeSpeed = 2f;

    void Update()
    {
        float alpha = Mathf.PingPong(Time.time * fadeSpeed, 1f);
        Color color = pressEnterText.color;
        color.a = alpha;
        pressEnterText.color = color;

        if (!Input.GetKeyDown(KeyCode.Return)) return;
        if (ProfileScreenController.IsOpen) return;   // la pantalla de perfiles manda

        if (ProfileManager.Instance != null && ProfileManager.Instance.HasActiveProfile)
            ProgressMapScreen.Show();                 // con perfil, el hub es el menú principal
        else
            SceneLoader.LoadScene(SceneLoader.DIFFICULTY_SELECTOR); // invitado (sin datos de hub)
    }
}
