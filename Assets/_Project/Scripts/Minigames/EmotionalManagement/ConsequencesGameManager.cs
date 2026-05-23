using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class ConsequencesGameManager : MinigameBase
{

    [Header("Cuantas situaciones se juegan por partida")]
    public int situationCount = 5;

    [Header("Decisiones adecuadas necesarias para ganar")]
    public int roundsToWin = 3;

    [Header("Puntuacion por tipo de respuesta")]
    public int pointsPositive = 20;
    public int pointsNeutral  = 8;
    public int pointsNegative = 0;

    ConsequencesSituationManager _sitManager;
    ConsequencesUIController     _ui;

    int                _currentIndex;
    int                _score;
    int                _positiveCount;
    EmotionalSituation _currentSituation;

    protected override string GetIntroDescription() =>
        "Se te presentaran situaciones del dia a dia.\n" +
        "Lee cada una y elige la reaccion que consideres mas adecuada.\n\n" +
        "No hay prisa: piensa antes de responder.\n" +
        "Gana " + roundsToWin + " de " + situationCount + " decisiones adecuadas para completar el juego.";

    void ApplyDifficulty()
    {
        var diff = GameManager.Instance != null
            ? GameManager.Instance.CurrentDifficulty
            : DifficultyLevel.Easy;
        switch (diff)
        {
            case DifficultyLevel.Medium:
                situationCount = 6;
                roundsToWin    = 4;
                break;
            case DifficultyLevel.Hard:
                situationCount = 8;
                roundsToWin    = 6;
                break;
        }
    }

    protected override void OnMinigameStart()
    {
        ApplyDifficulty();
        EnsureEventSystem();

        _sitManager = GetComponent<ConsequencesSituationManager>();
        _ui         = GetComponent<ConsequencesUIController>();

        _currentIndex  = 0;
        _score         = 0;
        _positiveCount = 0;

        _sitManager.Initialize(situationCount);

        _ui.BuildUI(
            idx => HandleOptionChosen(idx),
            ()  => NextSituation(),
            ()  => RestartMinigame(),
            ()  => ReturnToGameSelector());

        _ui.UpdateScore(0);

        StartCoroutine(StartAfterLayout());
    }

    protected override void OnMinigameComplete() { }
    protected override void OnMinigameFailed()   { }

    IEnumerator StartAfterLayout()
    {
        yield return null;
        ShowCurrentSituation();
    }

    void ShowCurrentSituation()
    {
        _currentSituation = _sitManager.GetSituation(_currentIndex);
        if (_currentSituation == null)
        {
            EndGame();
            return;
        }
        _ui.ShowSituation(_currentSituation, _currentIndex + 1, _sitManager.Total);
    }

    void HandleOptionChosen(int optionIndex)
    {
        if (!IsPlaying) return;
        if (_currentSituation == null) return;
        if (optionIndex < 0 || optionIndex >= _currentSituation.options.Length) return;

        var chosen = _currentSituation.options[optionIndex];

        int delta = 0;
        switch (chosen.quality)
        {
            case AnswerQuality.Positive:
                delta = pointsPositive;
                _positiveCount++;
                break;
            case AnswerQuality.Neutral:
                delta = pointsNeutral;
                break;
            case AnswerQuality.Negative:
                delta = pointsNegative;
                break;
        }
        _score += delta;
        _ui.UpdateScore(_score);

        bool hasNext = _currentIndex < _sitManager.Total - 1;
        _ui.ShowConsequence(chosen, hasNext);
    }

    void NextSituation()
    {
        if (!IsPlaying) return;
        _currentIndex++;
        if (_currentIndex >= _sitManager.Total)
            EndGame();
        else
            ShowCurrentSituation();
    }

    void EndGame()
    {
        bool won = _positiveCount >= roundsToWin;
        CompleteMinigame(_score);
        _ui.ShowFinalResult(won, _positiveCount, _sitManager.Total, _score);
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
