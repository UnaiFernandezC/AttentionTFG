using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneLoader
{

    public const string MAIN_MENU          = "PrimeraPantalla";
    public const string DIFFICULTY_SELECTOR = "DifficultySelector";

    public const string GAME_SELECTOR_EASY   = "GameSelector";
    public const string GAME_SELECTOR_MEDIUM = "GameSelector 1";
    public const string GAME_SELECTOR_HARD   = "GameSelector 2";

    // ── EscenasEleccion (selector de minijuego por categoría) ────────────
    public const string ESCENAS_ATENCION_EASY     = "Atencion";
    public const string ESCENAS_IMPULSOS_EASY     = "ContoImpulsos";
    public const string ESCENAS_EMOCIONAL_EASY    = "ControlEmocional";
    public const string ESCENAS_MEMORIA_EASY      = "MemoriaTrabajo1 2";
    public const string ESCENAS_PLANIF_EASY       = "Planificacion";

    public const string ESCENAS_ATENCION_MEDIUM   = "Atencion_Medium";
    public const string ESCENAS_IMPULSOS_MEDIUM   = "ContoImpulsos_Medium";
    public const string ESCENAS_EMOCIONAL_MEDIUM  = "ControlEmocional_Medium";
    public const string ESCENAS_MEMORIA_MEDIUM    = "MemoriaTrabajo_Medium";
    public const string ESCENAS_PLANIF_MEDIUM     = "Planificacion_Medium";

    public const string ESCENAS_ATENCION_HARD     = "Atencion_Hard";
    public const string ESCENAS_IMPULSOS_HARD     = "ContoImpulsos_Hard";
    public const string ESCENAS_EMOCIONAL_HARD    = "ControlEmocional_Hard";
    public const string ESCENAS_MEMORIA_HARD      = "MemoriaTrabajo_Hard";
    public const string ESCENAS_PLANIF_HARD       = "Planificacion_Hard";

    public const string MEMORY_COLOR_MATCH    = "Memory_ColorMatch";
    public const string MEMORY_PATTERN_RECALL = "Memory_PatternRecall";
    public const string MEMORY_SIMON_SAYS      = "SimonSays";
    public const string MEMORY_SIMON_SAYS_EASY = "Memory_SimonSays_Easy";
    public const string MEMORY_ALGO_NO_CUADRA = "¡Algo no cuadra!";
    public const string MEMORY_FIND_CHANGE      = "Memory_FindChange_Easy";
    public const string MEMORY_POSITION_MEMORY  = "Memory_PositionMemory_Easy";
    public const string MEMORY_WORD_MEMORY      = "Memory_WordMemory_Easy";

    public const string EMOTION_AVENTURA      = "Aventura emocional";
    public const string EMOTION_BALANCE       = "Emotional_Balance";
    public const string EMOTION_CONSEQUENCES           = "Emotional_Consequences_Easy";
    public const string EMOTION_PROGRESSIVE_REGULATION = "Emotional_ProgressiveRegulation_Easy";
    public const string EMOTION_ATTRACTION_CONTROL     = "Emotional_AttractionControl_Easy";

    public const string IMPULSE_DONT_PRESS_YET   = "Impulse_DontPressYet_Easy";
    public const string IMPULSE_INVERSE_RESPONSE  = "Impulse_InverseResponse_Easy";
    public const string IMPULSE_STOP_AND_GO         = "Impulse_StopAndGo_Easy";
    public const string IMPULSE_SILENT_COUNTDOWN       = "Impulse_SilentCountdown_Easy";
    public const string IMPULSE_DONT_FOLLOW_MAJORITY   = "Impulse_DontFollowMajority_Easy";

    public const string ATTENTION_SCENE            = "Attention";
    public const string ATTENTION_OBJECT_TRACKING  = "Attention_ObjectTracking_Easy";
    public const string ATTENTION_QUICK_REACTION   = "Attention_QuickReaction_Easy";
    public const string ATTENTION_RULE_SWITCH      = "Attention_RuleSwitch_Easy";

    public const string PLANNING_ORDEN_CORRECTO      = "Planning_OrdenCorrecto";
    public const string PLANNING_RESOURCE_MANAGEMENT = "Planning_ResourceManagement";
    public const string PLANNING_OPTIMAL_PATH        = "Attention_OptimalPath";
    public const string PLANNING_ACTION_SEQUENCE     = "Planning_ActionSequence";
    public const string PLANNING_PATH_MEMORY_EASY = "Planning_PathMemory_Easy";

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

    public static void ReloadCurrentScene()
    {
        LoadScene(SceneManager.GetActiveScene().name);
    }

    public static void GoToMainMenu()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.GoToMainMenu();
        else
            LoadScene(MAIN_MENU);
    }
}
