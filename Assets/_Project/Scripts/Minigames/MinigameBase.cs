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

    bool        _gameStarted;
    GameObject  _introCvGO;

    protected virtual void Start()
    {
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
        OnMinigameStart();
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

    static string CategoryName(MinigameCategory cat)
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
        Debug.Log($"[{minigameName}] Completado. Puntuacion: {finalScore}");
        OnMinigameComplete();
    }

    protected void FailMinigame()
    {
        if (!IsPlaying) return;
        IsPlaying = false;
        Debug.Log($"[{minigameName}] Fallado.");
        OnMinigameFailed();
    }

    protected void ReturnToGameSelector()
    {
        SceneLoader.LoadGameSelector();
    }

    protected void RestartMinigame()
    {
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
