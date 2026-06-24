using UnityEngine;
using UnityEngine.EventSystems;

public class RegulationGameManager : MinigameBase
{
    [Header("Nivel emocional inicial")]
    public float startLevel = 100f;

    [Header("Tension automatica por turno (antes de actuar)")]
    public float regenerationPerTurn = 8f;

    [Header("Maximo de acciones antes de perder")]
    public int maxSteps = 10;

    RegulationEmotionManager _emotionMgr;
    RegulationUIController   _ui;
    int _errors;

    protected override string GetIntroDescription() =>
        "El nivel de emocion esta muy alto. Tienes que calmarlo.\n\n" +
        "Cada turno el nivel sube solo. Elige la accion correcta para bajarlo.\n" +
        "No puedes repetir la misma accion dos veces seguidas.\n\n" +
        "Baja el nivel a 10 o menos antes de quedarte sin acciones!";

    void ApplyDifficulty()
    {
        var diff = GameManager.Instance != null
            ? GameManager.Instance.CurrentDifficulty
            : DifficultyLevel.Easy;
        switch (diff)
        {
            case DifficultyLevel.Medium:
                startLevel          = 100f;
                regenerationPerTurn = 10f;
                maxSteps            = 9;
                break;
            case DifficultyLevel.Hard:
                startLevel          = 100f;
                regenerationPerTurn = 13f;
                maxSteps            = 8;
                break;
        }
    }

    protected override void OnMinigameStart()
    {
        ApplyDifficulty();
        EnsureEventSystem();

        _emotionMgr = new RegulationEmotionManager(startLevel, regenerationPerTurn);
        _ui         = GetComponent<RegulationUIController>();
        _errors     = 0;

        _ui.BuildUI(
            idx => HandleAction(idx),
            ()  => RestartMinigame(),
            ()  => ReturnToGameSelector());

        RefreshUI();
    }

    protected override void OnMinigameComplete() { }
    protected override void OnMinigameFailed()   { }

    void HandleAction(int actionIndex)
    {
        if (!IsPlaying) return;
        if (!_emotionMgr.CanUseAction(actionIndex)) return;

        var action = _emotionMgr.ApplyAction(actionIndex);
        if (action == null) return;

        RefreshUI();
        _ui.ShowFeedback(action, _emotionMgr.CurrentLevel, regenerationPerTurn);

        if (action.impact + regenerationPerTurn > 0f)
            _errors++;

        if (_errors >= 3 && !_emotionMgr.IsWon)
        {
            FailMinigame();
            _ui.ShowResult(won: false,
                           steps: _emotionMgr.StepsTaken,
                           score: 0,
                           finalLevel: Mathf.RoundToInt(_emotionMgr.CurrentLevel));
            return;
        }

        if (_emotionMgr.IsWon)
        {
            int score = _emotionMgr.CalculateScore();
            CompleteMinigame(score);
            _ui.ShowResult(won: true,
                           steps: _emotionMgr.StepsTaken,
                           score: score,
                           finalLevel: Mathf.RoundToInt(_emotionMgr.CurrentLevel));
        }
        else if (_emotionMgr.StepsTaken >= maxSteps)
        {
            CompleteMinigame(0);
            _ui.ShowResult(won: false,
                           steps: _emotionMgr.StepsTaken,
                           score: 0,
                           finalLevel: Mathf.RoundToInt(_emotionMgr.CurrentLevel));
        }
    }

    void RefreshUI()
    {
        _ui.UpdateBar(_emotionMgr.CurrentLevel, _emotionMgr.StepsTaken,
                      maxSteps, regenerationPerTurn);
        _ui.UpdateScore(_emotionMgr.CalculateScore());

        int n   = RegulationEmotionManager.ACTIONS.Length;
        var cds = new int[n];
        for (int i = 0; i < n; i++)
            cds[i] = _emotionMgr.GetCooldown(i);
        _ui.UpdateButtonCooldowns(cds);
    }

    static void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() == null)
        {
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }
    }
}
