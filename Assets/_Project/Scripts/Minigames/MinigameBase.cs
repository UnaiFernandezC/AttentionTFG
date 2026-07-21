// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public abstract class MinigameBase : MonoBehaviour
{

    [Header("Configuracion del minijuego")]
    [SerializeField] protected string minigameName = "Minijuego";
    [SerializeField] protected MinigameCategory category = MinigameCategory.Memory;

    public int Score { get; protected set; } = 0;
    public bool IsPlaying { get; protected set; } = false;

    public static MinigameCategory? ActiveCategory { get; private set; }

    bool        _gameStarted;
    GameObject  _introCvGO;

    protected virtual void Start()
    {
        GameSelectorMusicManager.StopMusic();
        if (UIAudioManager.Instance != null) UIAudioManager.Instance.StopMusic();
        BuildIntroPanel();
        StartCoroutine(WaitForSpaceKey());
    }

    System.Collections.IEnumerator WaitForSpaceKey()
    {
        while (!_gameStarted)
        {
            if (Input.GetKeyDown(KeyCode.Space))
                LaunchGame();
            yield return null;
        }
    }

    void LaunchGame()
    {
        if (_gameStarted) return;
        _gameStarted = true;
        if (_introCvGO != null) Destroy(_introCvGO);
        IsPlaying = true;
        ActiveCategory = category;
        TelemetryManager.NotifyMinigameStarted(minigameName, category);
        OnMinigameStart();
        ShowChallengeChip();   // chip "RETO: ORO" si el sector ya tiene rango
    }

    /// <summary>Chip central discreto que indica el reto actual del sector (rango
    /// del ChallengeSystem). Solo se muestra si el sector ya está revivido
    /// (rango >= 1). Canvas propio, no bloquea clics.</summary>
    void ShowChallengeChip()
    {
        int r = ChallengeSystem.RankEscenaActual();
        if (r < 1) return;

        string pid = GameCatalog.ActiveProfileId;
        string sb  = ChallengeSystem.SceneBaseActual();
        var (faltan, siguiente) = ChallengeSystem.PuntosParaSiguiente(pid, sb);
        int metaSiguiente = siguiente != null
            ? ChallengeSystem.MejorPuntuacion(pid, sb) + faltan : 0;

        Color col = ChallengeSystem.ColorRango(r);
        var cv = KidUI.MakeCanvas("ChallengeChip", 60);
        var root = cv.GetComponent<RectTransform>();

        var chip = KidUI.RoundImg(root, "Chip", new Color(0.05f, 0.07f, 0.14f, 0.92f),
            new Vector2(0.36f, 0.935f), new Vector2(0.64f, 0.995f),
            Vector2.zero, Vector2.zero, 2.2f);
        chip.GetComponent<Image>().raycastTarget = false;

        var line = KidUI.RoundImg(chip, "Line", col,
            new Vector2(0.10f, 0f), new Vector2(0.90f, 0f),
            new Vector2(0f, 3f), new Vector2(0f, 4f), 4f);
        line.GetComponent<Image>().raycastTarget = false;

        // Rango actual del sector
        var t = KidUI.Txt(chip, "T", "RETO: " + ChallengeSystem.NombreRango(r),
                          col, 16, new Vector2(0.04f, siguiente != null ? 0.46f : 0f),
                          new Vector2(0.96f, 1f));
        t.fontStyle = FontStyles.Bold;
        t.characterSpacing = 2f;
        t.raycastTarget = false;

        // Meta de puntos para el siguiente rango (si no es ya DIAMANTE)
        if (siguiente != null)
        {
            var meta = KidUI.Txt(chip, "Meta", $"Meta {siguiente}: {metaSiguiente} pts",
                                 new Color(0.80f, 0.86f, 0.96f), 12,
                                 new Vector2(0.04f, 0.06f), new Vector2(0.96f, 0.48f));
            meta.raycastTarget = false;
        }
    }

    void BuildIntroPanel()
    {
        _introCvGO = IntroPanel.Build(
            minigameName,
            CategoryName(category),
            GetIntroDescription(),
            LaunchGame);
    }

    protected virtual string GetIntroDescription()
    {
        return "Sigue las instrucciones para completar el minijuego.";
    }

    /// <summary>
    /// Muestra la pantalla de resultados unificada (estrellas, robot, confeti,
    /// contador de puntos). Los botones ya quedan conectados a reintentar/salir.
    /// stars: usa GameFeel.StarsFromRatio(success, ratio) para calcularlas.
    /// </summary>
    protected void ShowResults(bool success, int stars, int score,
                               string[] stats = null, string title = null,
                               string subtitle = null)
    {
        // Registra la puntuación de la partida ganada para el sistema de rangos
        // (la mejor marca decide si el sector sube de BRONCE a DIAMANTE).
        if (success) ChallengeSystem.RegistrarResultado(score, stars);

        ResultsPanel.Show(new ResultsPanel.Config
        {
            success = success,
            stars = stars,
            score = score,
            stats = stats,
            title = title,
            subtitle = subtitle,
            categoryName = CategoryName(category),
            onReplay = RestartMinigame,
            onExit = ReturnToGameSelector
        });
    }

    protected static string CategoryName(MinigameCategory cat)
    {
        switch (cat)
        {
            case MinigameCategory.Memory:              return "Memoria";
            case MinigameCategory.ImpulseControl:      return "Control de impulsos";
            case MinigameCategory.EmotionalManagement: return "Gestion emocional";
            case MinigameCategory.Attention:           return "Atencion";
            case MinigameCategory.Planning:            return "Planificacion";
            default:                                    return "Memoria";
        }
    }

    protected abstract void OnMinigameStart();
    protected abstract void OnMinigameComplete();
    protected abstract void OnMinigameFailed();

    protected void CompleteMinigame(int finalScore = 0)
    {
        if (!IsPlaying) return;
        IsPlaying = false;
        Score = finalScore;
        if (GameManager.Instance != null)
            GameManager.Instance.AddScore(finalScore);
        TelemetryManager.NotifyMinigameEnded(minigameName, category, finalScore, true);
        Debug.Log($"[{minigameName}] Completado. Puntuacion: {finalScore}");
        OnMinigameComplete();
    }

    protected void FailMinigame()
    {
        if (!IsPlaying) return;
        IsPlaying = false;
        TelemetryManager.NotifyMinigameEnded(minigameName, category, 0, false);
        Debug.Log($"[{minigameName}] Fallado.");
        OnMinigameFailed();
    }

    /// <summary>
    /// API opcional de telemetría por ronda. Los minijuegos pueden llamarla en cada
    /// acierto/fallo para enriquecer los informes (aciertos, errores y tiempo de
    /// reacción medio). Si un minijuego no la usa, todo sigue funcionando igual.
    /// </summary>
    protected void ReportEvent(bool acierto, float tiempoReaccionMs = -1f)
    {
        TelemetryManager.NotifyRound(acierto, tiempoReaccionMs);
    }

    protected void ReturnToGameSelector()
    {
        ActiveCategory = null;
        SceneLoader.LoadCategorySelector(category);
    }

    protected void RestartMinigame()
    {
        ActiveCategory = null;
        SceneLoader.ReloadCurrentScene();
    }
}

public enum MinigameCategory
{
    Memory              = 0,
    ImpulseControl      = 1,
    EmotionalManagement = 2,
    Attention           = 3,
    Planning            = 4
}
