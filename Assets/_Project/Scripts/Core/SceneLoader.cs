using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Sistema centralizado de carga de escenas.
/// Soporta carga directa y carga con fade (si se conecta a UIManager).
/// Todas las navegaciones del proyecto deben pasar por aquí.
/// </summary>
public static class SceneLoader
{
    // ------------------------------------------------------------------ //
    // Constantes de nombres de escena
    // Actualiza aquí si renombras escenas en el futuro.
    // ------------------------------------------------------------------ //

    // --- Menús principales ---
    public const string MAIN_MENU          = "PrimeraPantalla";
    public const string DIFFICULTY_SELECTOR = "DifficultySelector";

    // --- Selectores de juego por dificultad ---
    public const string GAME_SELECTOR_EASY   = "GameSelector";
    public const string GAME_SELECTOR_MEDIUM = "GameSelector2";
    public const string GAME_SELECTOR_HARD   = "GameSelector3";

    // --- Minijuegos: Memoria ---
    public const string MEMORY_COLOR_MATCH    = "Memory_ColorMatch";          // ← Parejas de Colores
    public const string MEMORY_PATTERN_RECALL = "Memory_PatternRecall";       // ← Repite el dibujo
    public const string MEMORY_SIMON_SAYS     = "SimonSays";
    public const string MEMORY_ALGO_NO_CUADRA = "¡Algo no cuadra!";

    // --- Minijuegos: Gestión Emocional ---
    public const string EMOTION_AVENTURA = "Aventura emocional";

    // --- Minijuegos: Atención ---
    public const string ATTENTION_SCENE = "Attention";

    // --- Minijuegos: Planificación ---
    public const string PLANNING_ORDEN_CORRECTO      = "Planning_OrdenCorrecto";       // ← Orden correcto
    public const string PLANNING_RESOURCE_MANAGEMENT = "Planning_ResourceManagement";  // ← Gestión de recursos

    // --- Slots vacíos para los 25 minijuegos futuros ---
    // Añade aquí las constantes de cada nuevo minijuego

    // ------------------------------------------------------------------ //
    // API pública
    // ------------------------------------------------------------------ //

    /// <summary>Carga una escena por su nombre de forma inmediata.</summary>
    public static void LoadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("[SceneLoader] Nombre de escena vacío.");
            return;
        }
        Debug.Log($"[SceneLoader] Cargando escena: {sceneName}");
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>Carga el selector de juego apropiado según la dificultad activa.</summary>
    public static void LoadGameSelector()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[SceneLoader] GameManager no encontrado. Cargando selector Easy por defecto.");
            LoadScene(GAME_SELECTOR_EASY);
            return;
        }

        switch (GameManager.Instance.CurrentDifficulty)
        {
            case DifficultyLevel.Easy:   LoadScene(GAME_SELECTOR_EASY);   break;
            case DifficultyLevel.Medium: LoadScene(GAME_SELECTOR_MEDIUM); break;
            case DifficultyLevel.Hard:   LoadScene(GAME_SELECTOR_HARD);   break;
            default:                     LoadScene(GAME_SELECTOR_EASY);   break;
        }
    }

    /// <summary>Recarga la escena actualmente activa.</summary>
    public static void ReloadCurrentScene()
    {
        LoadScene(SceneManager.GetActiveScene().name);
    }

    /// <summary>Vuelve al menú principal.</summary>
    public static void GoToMainMenu()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.GoToMainMenu();
        else
            LoadScene(MAIN_MENU);
    }
}
