using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Singleton central del juego. Persiste entre escenas.
/// Gestiona el estado global: jugador actual, dificultad, puntuación acumulada.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // Estado del juego (campos privados expuestos como propiedades de solo lectura)
    private DifficultyLevel _currentDifficulty = DifficultyLevel.Easy;
    private int _totalScore = 0;

    public DifficultyLevel CurrentDifficulty => _currentDifficulty;
    public int TotalScore => _totalScore;

    [Header("Escenas principales")]
    [SerializeField] private string mainMenuScene   = "PrimeraPantalla";
    [SerializeField] private string difficultyScene = "DifficultySelector";

    // ------------------------------------------------------------------ //
    // Lifecycle
    // ------------------------------------------------------------------ //

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ------------------------------------------------------------------ //
    // Dificultad
    // ------------------------------------------------------------------ //

    /// <summary>Establece la dificultad global desde el DifficultySelector.</summary>
    public void SetDifficulty(DifficultyLevel difficulty)
    {
        _currentDifficulty = difficulty;
        Debug.Log($"[GameManager] Dificultad establecida: {difficulty}");
    }

    // ------------------------------------------------------------------ //
    // Puntuación
    // ------------------------------------------------------------------ //

    public void AddScore(int amount)
    {
        _totalScore += amount;
        Debug.Log($"[GameManager] Puntuación total: {_totalScore}");
    }

    public void ResetScore()
    {
        _totalScore = 0;
    }

    // ------------------------------------------------------------------ //
    // Navegación global
    // ------------------------------------------------------------------ //

    public void GoToMainMenu()
    {
        SceneLoader.LoadScene(mainMenuScene);
    }

    public void GoToDifficultySelector()
    {
        SceneLoader.LoadScene(difficultyScene);
    }
}

/// <summary>Niveles de dificultad disponibles en la plataforma.</summary>
public enum DifficultyLevel
{
    Easy   = 0,   // BosqueMagico  (6-8 años)
    Medium = 1,   // CastilloVolador (9-11 años)
    Hard   = 2    // CuevaMisteriosa (12+ años)
}
