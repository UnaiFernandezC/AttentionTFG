using UnityEngine;

/// <summary>
/// Componente de apoyo para el DifficultySelector.
/// Cada botón de dificultad llama a SelectDifficulty() y luego navega al GameSelector.
/// Adjunta este script al GameObject que gestione la pantalla de dificultad.
/// </summary>
public class DifficultyManager : MonoBehaviour
{
    [Header("Configuración de botones")]
    [Tooltip("Activa esto para navegar automáticamente al GameSelector tras seleccionar dificultad.")]
    [SerializeField] private bool autoNavigate = true;

    // ------------------------------------------------------------------ //
    // API pública — llama desde los botones de UI via Inspector
    // ------------------------------------------------------------------ //

    /// <summary>Selecciona dificultad FÁCIL (BosqueMagico, 6-8 años).</summary>
    public void SelectEasy()   => SelectDifficulty(DifficultyLevel.Easy);

    /// <summary>Selecciona dificultad MEDIA (CastilloVolador, 9-11 años).</summary>
    public void SelectMedium() => SelectDifficulty(DifficultyLevel.Medium);

    /// <summary>Selecciona dificultad DIFÍCIL (CuevaMisteriosa, 12+ años).</summary>
    public void SelectHard()   => SelectDifficulty(DifficultyLevel.Hard);

    // ------------------------------------------------------------------ //
    // Lógica interna
    // ------------------------------------------------------------------ //

    private void SelectDifficulty(DifficultyLevel level)
    {
        if (GameManager.Instance != null)
            GameManager.Instance.SetDifficulty(level);
        else
            Debug.LogWarning("[DifficultyManager] GameManager no encontrado. Asegúrate de que existe en la escena.");

        Debug.Log($"[DifficultyManager] Dificultad seleccionada: {level}");

        if (autoNavigate)
            SceneLoader.LoadGameSelector();
    }
}
