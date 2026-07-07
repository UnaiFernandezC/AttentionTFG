// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
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

    // Auto-arranque: el GameManager DEBE existir siempre (mantiene la dificultad
    // activa y la puntuación). Antes no estaba en ninguna escena, así que la
    // dificultad no se aplicaba y todo caía a Fácil. Ahora se crea solo.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        if (Instance != null) return;
        var go = new GameObject("GameManager");
        go.AddComponent<GameManager>();
    }

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

        // La dificultad queda ligada al perfil activo (se elige una sola vez;
        // los cambios manuales desde el selector también se recuerdan).
        if (ProfileManager.Instance != null)
            ProfileManager.Instance.PersistDifficulty(difficulty);
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
