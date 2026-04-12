using UnityEngine;

/// <summary>
/// Clase base abstracta para todos los minijuegos de la plataforma.
/// Cada minijuego debe heredar de esta clase e implementar los métodos abstractos.
///
/// Uso:
///   public class SimonGame : MinigameBase { ... }
///   public class MemoryGameManager : MinigameBase { ... }
///
/// El ciclo de vida es:
///   OnMinigameStart() → jugando → OnMinigameComplete() o OnMinigameFailed()
/// </summary>
public abstract class MinigameBase : MonoBehaviour
{
    // ------------------------------------------------------------------ //
    // Propiedades
    // ------------------------------------------------------------------ //

    /// <summary>Nombre legible del minijuego (para logs y UI).</summary>
    [Header("Configuración del minijuego")]
    [SerializeField] protected string minigameName = "Minijuego";

    /// <summary>Categoría cognitiva de este minijuego.</summary>
    [SerializeField] protected MinigameCategory category = MinigameCategory.Memory;

    /// <summary>Puntuación obtenida al completar.</summary>
    public int Score { get; protected set; } = 0;

    /// <summary>Indica si el minijuego está en curso.</summary>
    public bool IsPlaying { get; private set; } = false;

    // ------------------------------------------------------------------ //
    // Unity lifecycle
    // ------------------------------------------------------------------ //

    protected virtual void Start()
    {
        IsPlaying = true;
        OnMinigameStart();
    }

    // ------------------------------------------------------------------ //
    // Métodos abstractos — implementar en cada minijuego
    // ------------------------------------------------------------------ //

    /// <summary>Llamado al iniciar el minijuego. Configura el estado inicial.</summary>
    protected abstract void OnMinigameStart();

    /// <summary>Llamado cuando el jugador completa el minijuego con éxito.</summary>
    protected abstract void OnMinigameComplete();

    /// <summary>Llamado si el jugador falla o agota el tiempo.</summary>
    protected abstract void OnMinigameFailed();

    // ------------------------------------------------------------------ //
    // Métodos de ayuda para las subclases
    // ------------------------------------------------------------------ //

    /// <summary>Marca el minijuego como completado y notifica al GameManager.</summary>
    protected void CompleteMinigame(int finalScore = 0)
    {
        if (!IsPlaying) return;
        IsPlaying = false;
        Score = finalScore;

        if (GameManager.Instance != null)
            GameManager.Instance.AddScore(finalScore);

        Debug.Log($"[{minigameName}] Completado. Puntuación: {finalScore}");
        OnMinigameComplete();
    }

    /// <summary>Marca el minijuego como fallado.</summary>
    protected void FailMinigame()
    {
        if (!IsPlaying) return;
        IsPlaying = false;
        Debug.Log($"[{minigameName}] Fallado.");
        OnMinigameFailed();
    }

    /// <summary>Vuelve al selector de minijuegos de la dificultad activa.</summary>
    protected void ReturnToGameSelector()
    {
        SceneLoader.LoadGameSelector();
    }

    /// <summary>Recarga este mismo minijuego.</summary>
    protected void RestartMinigame()
    {
        SceneLoader.ReloadCurrentScene();
    }
}

/// <summary>Categorías cognitivas de la plataforma.</summary>
public enum MinigameCategory
{
    Memory           = 0,   // Memoria de trabajo
    ImpulseControl   = 1,   // Control de impulsos
    EmotionalManagement = 2,// Gestión emocional
    Attention        = 3,   // Atención
    Planning         = 4    // Planificación y organización
}
