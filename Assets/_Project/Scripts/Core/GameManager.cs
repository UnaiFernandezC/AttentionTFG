using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private DifficultyLevel _currentDifficulty = DifficultyLevel.Easy;
    private int _totalScore = 0;

    public DifficultyLevel CurrentDifficulty => _currentDifficulty;
    public int TotalScore => _totalScore;

    [Header("Escenas principales")]
    [SerializeField] private string mainMenuScene   = "PrimeraPantalla";
    [SerializeField] private string difficultyScene = "DifficultySelector";

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

    public void SetDifficulty(DifficultyLevel difficulty)
    {
        _currentDifficulty = difficulty;
        Debug.Log($"[GameManager] Dificultad establecida: {difficulty}");
    }

    public void AddScore(int amount)
    {
        _totalScore += amount;
        Debug.Log($"[GameManager] Puntuación total: {_totalScore}");
    }

    public void ResetScore()
    {
        _totalScore = 0;
    }

    public void GoToMainMenu()
    {
        SceneLoader.LoadScene(mainMenuScene);
    }

    public void GoToDifficultySelector()
    {
        SceneLoader.LoadScene(difficultyScene);
    }
}

public enum DifficultyLevel
{
    Easy   = 0,
    Medium = 1,
    Hard   = 2
}
