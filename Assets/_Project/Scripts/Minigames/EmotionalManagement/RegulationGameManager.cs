// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
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
    int   _errors;
    int   _goodActions;
    float _turnStartedAt;

    protected override string GetIntroDescription() =>
        "La emocion esta muy alta y sube sola cada turno.\n" +
        "Elige acciones que calmen de verdad y baja el nivel a 10 o menos.";

    void ApplyDifficulty()
    {
        var diff = GameManager.Instance != null
            ? GameManager.Instance.CurrentDifficulty
            : DifficultyLevel.Easy;
        // Valores calibrados para que las tres dificultades sean GANABLES
        // rotando las acciones calmantes (con los valores antiguos, en medio y
        // dificil era matematicamente imposible bajar de 100 a 10).
        switch (diff)
        {
            case DifficultyLevel.Medium:
                startLevel          = 100f;
                regenerationPerTurn = 9f;
                maxSteps            = 10;
                break;
            case DifficultyLevel.Hard:
                startLevel          = 100f;
                regenerationPerTurn = 10f;
                maxSteps            = 11;
                break;
            default:
                startLevel          = 100f;
                regenerationPerTurn = 8f;
                maxSteps            = 10;
                break;
        }
    }

    protected override void OnMinigameStart()
    {
        ApplyDifficulty();
        EnsureEventSystem();

        _emotionMgr  = new RegulationEmotionManager(startLevel, regenerationPerTurn);
        _ui          = GetComponent<RegulationUIController>();
        _errors      = 0;
        _goodActions = 0;

        _ui.BuildUI(idx => HandleAction(idx));

        RefreshUI();
        _turnStartedAt = Time.realtimeSinceStartup;
    }

    protected override void OnMinigameComplete() { }
    protected override void OnMinigameFailed()   { }

    void HandleAction(int actionIndex)
    {
        if (!IsPlaying) return;
        if (!_emotionMgr.CanUseAction(actionIndex)) return;

        float levelBefore = _emotionMgr.CurrentLevel;
        var action = _emotionMgr.ApplyAction(actionIndex);
        if (action == null) return;

        float rtMs = (Time.realtimeSinceStartup - _turnStartedAt) * 1000f;
        int   net  = action.impact + Mathf.RoundToInt(regenerationPerTurn);
        bool  good = net < 0;

        // Acierto pedagogico = la accion tuvo impacto neto negativo (calmo mas
        // de lo que subio la tension automatica del turno).
        ReportEvent(good, rtMs);

        RefreshUI();
        _ui.ShowFeedback(action, _emotionMgr.CurrentLevel, regenerationPerTurn);

        if (good)
        {
            _goodActions++;
            GameFeel.PlaySuccess();
            GameFeel.FloatingText(net.ToString(), new Color(0.22f, 0.86f, 0.54f),
                                  new Vector2(0f, 150f));
        }
        else
        {
            _errors++;
            GameFeel.PlayError();
            GameFeel.FloatingText("+" + net, new Color(0.90f, 0.28f, 0.30f),
                                  new Vector2(0f, 150f));
            GameFeel.ScreenFlash(new Color(0.90f, 0.22f, 0.28f), 0.14f, 0.25f);
        }

        // La barra pulsa en rojo siempre que el nivel termina mas alto que antes.
        if (_emotionMgr.CurrentLevel > levelBefore)
            _ui.PulseBarRise();

        _turnStartedAt = Time.realtimeSinceStartup;

        if (_errors >= 3 && !_emotionMgr.IsWon)
        {
            EndGame(won: false);
            return;
        }

        if (_emotionMgr.IsWon)
            EndGame(won: true);
        else if (_emotionMgr.StepsTaken >= maxSteps)
            EndGame(won: false);
    }

    void EndGame(bool won)
    {
        int steps      = _emotionMgr.StepsTaken;
        int finalLevel = Mathf.RoundToInt(_emotionMgr.CurrentLevel);
        int score      = won ? _emotionMgr.CalculateScore() : 0;

        // Eficiencia = proporcion de acciones que realmente calmaron.
        float efficiency = steps > 0 ? (float)_goodActions / steps : 0f;

        if (won)
        {
            CompleteMinigame(score);
            GameFeel.Confetti();
        }
        else
        {
            FailMinigame();
        }

        ShowResults(
            won,
            GameFeel.StarsFromRatio(won, efficiency),
            score,
            new[]
            {
                "Nivel final: " + finalLevel + " (objetivo: 10 o menos)",
                "Acciones usadas: " + steps + " de " + maxSteps,
                "Eficiencia: " + Mathf.RoundToInt(efficiency * 100f) + "% de acciones calmantes"
            },
            won ? "¡Nivel emocional regulado!" : "La emocion gano esta vez",
            won ? "Respirar, contar hasta 10 o hablar calman de verdad."
                : "Recuerda: ignorar o estallar no calma; respirar y hablar si.");
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
