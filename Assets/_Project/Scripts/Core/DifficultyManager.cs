using UnityEngine;

public class DifficultyManager : MonoBehaviour
{
    [Header("Configuración de botones")]
    [Tooltip("Activa esto para navegar automáticamente al GameSelector tras seleccionar dificultad.")]
    [SerializeField] private bool autoNavigate = true;

    public void SelectEasy()   => SelectDifficulty(DifficultyLevel.Easy);

    public void SelectMedium() => SelectDifficulty(DifficultyLevel.Medium);

    public void SelectHard()   => SelectDifficulty(DifficultyLevel.Hard);

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
