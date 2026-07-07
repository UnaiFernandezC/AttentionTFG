// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneLoader
{

    public const string MAIN_MENU          = "PrimeraPantalla";
    public const string DIFFICULTY_SELECTOR = "DifficultySelector";

    public const string GAME_SELECTOR_EASY   = "EasyMenu";
    public const string GAME_SELECTOR_MEDIUM = "MediumMenu";
    public const string GAME_SELECTOR_HARD   = "HardMenu";

    public const string ESCENAS_ATENCION_EASY     = "Atencion";
    public const string ESCENAS_IMPULSOS_EASY     = "ContoImpulsos";
    public const string ESCENAS_EMOCIONAL_EASY    = "ControlEmocional";
    public const string ESCENAS_MEMORIA_EASY      = "MemoriaTrabajo_Easy";
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

    public const string ATTENTION_QUICK_REACTION_EASY   = "Attention_QuickReaction_Easy";
    public const string ATTENTION_QUICK_REACTION_MEDIUM = "Attention_QuickReaction_Medium";
    public const string ATTENTION_QUICK_REACTION_HARD   = "Attention_QuickReaction_Hard";

    public const string ATTENTION_RULE_SWITCH_EASY   = "Attention_RuleSwitch_Easy";
    public const string ATTENTION_RULE_SWITCH_MEDIUM = "Attention_RuleSwitch_Medium";
    public const string ATTENTION_RULE_SWITCH_HARD   = "Attention_RuleSwitch_Hard";

    public const string ATTENTION_OBJECT_TRACKING_EASY   = "Attention_ObjectTracking_Easy";
    public const string ATTENTION_OBJECT_TRACKING_MEDIUM = "Attention_ObjectTracking_Medium";
    public const string ATTENTION_OBJECT_TRACKING_HARD   = "Attention_ObjectTracking_Hard";

    public const string ATTENTION_OPTIMAL_PATH_EASY   = "Attention_OptimalPath_Easy";
    public const string ATTENTION_OPTIMAL_PATH_MEDIUM = "Attention_OptimalPath_Medium";
    public const string ATTENTION_OPTIMAL_PATH_HARD   = "Attention_OptimalPath_Hard";

    public const string ATTENTION_ALGO_NO_CUADRA_EASY   = "Attention_AlgoNoCuadra_Easy";
    public const string ATTENTION_ALGO_NO_CUADRA_MEDIUM = "Attention_AlgoNoCuadra_Medium";
    public const string ATTENTION_ALGO_NO_CUADRA_HARD   = "Attention_AlgoNoCuadra_Hard";

    public const string ATTENTION_LASER_PATH_EASY   = "Attention_LaserPath_Easy";
    public const string ATTENTION_LASER_PATH_MEDIUM = "Attention_LaserPath_Medium";
    public const string ATTENTION_LASER_PATH_HARD   = "Attention_LaserPath_Hard";

    public const string EMOTION_ATTRACTION_CONTROL_EASY   = "Emotional_AttractionControl_Easy";
    public const string EMOTION_ATTRACTION_CONTROL_MEDIUM = "Emotional_AttractionControl_Medium";
    public const string EMOTION_ATTRACTION_CONTROL_HARD   = "Emotional_AttractionControl_Hard";

    public const string EMOTION_CONSEQUENCES_EASY   = "Emotional_Consequences_Easy";
    public const string EMOTION_CONSEQUENCES_MEDIUM = "Emotional_Consequences_Medium";
    public const string EMOTION_CONSEQUENCES_HARD   = "Emotional_Consequences_Hard";

    public const string EMOTION_PROGRESSIVE_REGULATION_EASY   = "Emotional_ProgressiveRegulation_Easy";
    public const string EMOTION_PROGRESSIVE_REGULATION_MEDIUM = "Emotional_ProgressiveRegulation_Medium";
    public const string EMOTION_PROGRESSIVE_REGULATION_HARD   = "Emotional_ProgressiveRegulation_Hard";

    public const string EMOTION_BALANCE_EASY   = "Emotional_Balance_Easy";
    public const string EMOTION_BALANCE_MEDIUM = "Emotional_Balance_Medium";
    public const string EMOTION_BALANCE_HARD   = "Emotional_Balance_Hard";

    public const string EMOTION_AVENTURA_EMOCIONAL_EASY   = "Emotional_AventuraEmocional_Easy";
    public const string EMOTION_AVENTURA_EMOCIONAL_MEDIUM = "Emotional_AventuraEmocional_Medium";
    public const string EMOTION_AVENTURA_EMOCIONAL_HARD   = "Emotional_AventuraEmocional_Hard";

    public const string IMPULSE_DONT_FOLLOW_MAJORITY_EASY   = "Impulse_DontFollowMajority_Easy";
    public const string IMPULSE_DONT_FOLLOW_MAJORITY_MEDIUM = "Impulse_DontFollowMajority_Medium";
    public const string IMPULSE_DONT_FOLLOW_MAJORITY_HARD   = "Impulse_DontFollowMajority_Hard";

    public const string IMPULSE_DONT_PRESS_YET_EASY   = "Impulse_DontPressYet_Easy";
    public const string IMPULSE_DONT_PRESS_YET_MEDIUM = "Impulse_DontPressYet_Medium";
    public const string IMPULSE_DONT_PRESS_YET_HARD   = "Impulse_DontPressYet_Hard";

    public const string IMPULSE_INVERSE_RESPONSE_EASY   = "Impulse_InverseResponse_Easy";
    public const string IMPULSE_INVERSE_RESPONSE_MEDIUM = "Impulse_InverseResponse_Medium";
    public const string IMPULSE_INVERSE_RESPONSE_HARD   = "Impulse_InverseResponse_Hard";

    public const string IMPULSE_SILENT_COUNTDOWN_EASY   = "Impulse_SilentCountdown_Easy";
    public const string IMPULSE_SILENT_COUNTDOWN_MEDIUM = "Impulse_SilentCountdown_Medium";
    public const string IMPULSE_SILENT_COUNTDOWN_HARD   = "Impulse_SilentCountdown_Hard";

    public const string IMPULSE_STOP_AND_GO_EASY   = "Impulse_StopAndGo_Easy";
    public const string IMPULSE_STOP_AND_GO_MEDIUM = "Impulse_StopAndGo_Medium";
    public const string IMPULSE_STOP_AND_GO_HARD   = "Impulse_StopAndGo_Hard";

    public const string MEMORY_FIND_CHANGE_EASY   = "Memory_FindChange_Easy";
    public const string MEMORY_FIND_CHANGE_MEDIUM = "Memory_FindChange_Medium";
    public const string MEMORY_FIND_CHANGE_HARD   = "Memory_FindChange_Hard";

    public const string MEMORY_SIMON_SAYS_EASY   = "Memory_SimonSays_Easy";
    public const string MEMORY_SIMON_SAYS_MEDIUM = "Memory_SimonSays_Medium";
    public const string MEMORY_SIMON_SAYS_HARD   = "Memory_SimonSays_Hard";

    public const string MEMORY_WORD_MEMORY_EASY   = "Memory_WordMemory_Easy";
    public const string MEMORY_WORD_MEMORY_MEDIUM = "Memory_WordMemory_Medium";
    public const string MEMORY_WORD_MEMORY_HARD   = "Memory_WordMemory_Hard";

    public const string MEMORY_COLOR_MATCH_EASY   = "Memory_ColorMatch_Easy";
    public const string MEMORY_COLOR_MATCH_MEDIUM = "Memory_ColorMatch_Medium";
    public const string MEMORY_COLOR_MATCH_HARD   = "Memory_ColorMatch_Hard";

    public const string MEMORY_PATTERN_RECALL_EASY   = "Memory_PatternRecall_Easy";
    public const string MEMORY_PATTERN_RECALL_MEDIUM = "Memory_PatternRecall_Medium";
    public const string MEMORY_PATTERN_RECALL_HARD   = "Memory_PatternRecall_Hard";

    public const string PLANNING_PATH_MEMORY_EASY   = "Planning_PathMemory_Easy";
    public const string PLANNING_PATH_MEMORY_MEDIUM = "Planning_PathMemory_Medium";
    public const string PLANNING_PATH_MEMORY_HARD   = "Planning_PathMemory_Hard";

    public const string PLANNING_ACTION_SEQUENCE_EASY   = "Planning_ActionSequence_Easy";
    public const string PLANNING_ACTION_SEQUENCE_MEDIUM = "Planning_ActionSequence_Medium";
    public const string PLANNING_ACTION_SEQUENCE_HARD   = "Planning_ActionSequence_Hard";

    public const string PLANNING_ORDEN_CORRECTO_EASY   = "Planning_OrdenCorrecto_Easy";
    public const string PLANNING_ORDEN_CORRECTO_MEDIUM = "Planning_OrdenCorrecto_Medium";
    public const string PLANNING_ORDEN_CORRECTO_HARD   = "Planning_OrdenCorrecto_Hard";

    public const string PLANNING_RESOURCE_MANAGEMENT_EASY   = "Planning_ResourceManagement_Easy";
    public const string PLANNING_RESOURCE_MANAGEMENT_MEDIUM = "Planning_ResourceManagement_Medium";
    public const string PLANNING_RESOURCE_MANAGEMENT_HARD   = "Planning_ResourceManagement_Hard";

    public const string PLANNING_OPTIMAL_PATH_EASY   = "Planning_OptimalPath_Easy";
    public const string PLANNING_OPTIMAL_PATH_MEDIUM = "Planning_OptimalPath_Medium";
    public const string PLANNING_OPTIMAL_PATH_HARD   = "Planning_OptimalPath_Hard";

    public static void LoadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("[SceneLoader] Nombre de escena vacío.");
            NavErrorScreen.Show("sin escena asignada");
            return;
        }
        // Validación central: si la escena no existe / no está en Build Settings,
        // pantalla amable en lugar de fallo silencioso.
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"[SceneLoader] La escena '{sceneName}' no existe o no está en Build Settings.");
            NavErrorScreen.Show(sceneName);
            return;
        }
        Debug.Log($"[SceneLoader] Cargando escena: {sceneName}");
        SceneTransition.LoadScene(sceneName);
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

    /// <summary>
    /// Vuelve a la pantalla de seleccion de minijuegos de la categoria y dificultad actuales.
    /// </summary>
    public static void LoadCategorySelector(MinigameCategory cat)
    {
        DifficultyLevel diff = GameManager.Instance != null
            ? GameManager.Instance.CurrentDifficulty
            : DifficultyLevel.Easy;

        string scene;
        switch (cat)
        {
            case MinigameCategory.Attention:
                scene = diff == DifficultyLevel.Easy   ? ESCENAS_ATENCION_EASY
                      : diff == DifficultyLevel.Medium  ? ESCENAS_ATENCION_MEDIUM
                      :                                   ESCENAS_ATENCION_HARD;
                break;
            case MinigameCategory.ImpulseControl:
                scene = diff == DifficultyLevel.Easy   ? ESCENAS_IMPULSOS_EASY
                      : diff == DifficultyLevel.Medium  ? ESCENAS_IMPULSOS_MEDIUM
                      :                                   ESCENAS_IMPULSOS_HARD;
                break;
            case MinigameCategory.EmotionalManagement:
                scene = diff == DifficultyLevel.Easy   ? ESCENAS_EMOCIONAL_EASY
                      : diff == DifficultyLevel.Medium  ? ESCENAS_EMOCIONAL_MEDIUM
                      :                                   ESCENAS_EMOCIONAL_HARD;
                break;
            case MinigameCategory.Memory:
                scene = diff == DifficultyLevel.Easy   ? ESCENAS_MEMORIA_EASY
                      : diff == DifficultyLevel.Medium  ? ESCENAS_MEMORIA_MEDIUM
                      :                                   ESCENAS_MEMORIA_HARD;
                break;
            case MinigameCategory.Planning:
                scene = diff == DifficultyLevel.Easy   ? ESCENAS_PLANIF_EASY
                      : diff == DifficultyLevel.Medium  ? ESCENAS_PLANIF_MEDIUM
                      :                                   ESCENAS_PLANIF_HARD;
                break;
            default:
                LoadGameSelector();
                return;
        }
        LoadScene(scene);
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
