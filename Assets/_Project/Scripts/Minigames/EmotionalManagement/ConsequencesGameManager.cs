// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
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
    int                _neutralCount;
    float              _situationShownAt;
    EmotionalSituation _currentSituation;

    protected override string GetIntroDescription() =>
        "Se te presentaran situaciones del dia a dia.\n" +
        "La carita de cada opcion te da una pista de como acaba.\n\n" +
        "No hay prisa: piensa antes de responder.\n" +
        "Consigue " + roundsToWin + " de " + situationCount + " decisiones adecuadas para completar el juego.";

    protected override void Start()
    {
        ApplyDifficulty();
        base.Start();
    }

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
            default:
                situationCount = 5;
                roundsToWin    = 3;
                break;
        }
    }

    protected override void OnMinigameStart()
    {
        EnsureEventSystem();

        _sitManager = GetComponent<ConsequencesSituationManager>();
        _ui         = GetComponent<ConsequencesUIController>();

        _currentIndex  = 0;
        _score         = 0;
        _positiveCount = 0;
        _neutralCount  = 0;

        _sitManager.Initialize(situationCount);

        _ui.BuildUI(
            idx => HandleOptionChosen(idx),
            ()  => NextSituation());

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
        _situationShownAt = Time.realtimeSinceStartup;
    }

    void HandleOptionChosen(int optionIndex)
    {
        if (!IsPlaying) return;
        if (_currentSituation == null) return;
        if (optionIndex < 0 || optionIndex >= _currentSituation.options.Length) return;

        var chosen = _currentSituation.options[optionIndex];

        float rtMs = (Time.realtimeSinceStartup - _situationShownAt) * 1000f;
        ReportEvent(chosen.quality == AnswerQuality.Positive, rtMs);

        int delta = 0;
        switch (chosen.quality)
        {
            case AnswerQuality.Positive:
                delta = pointsPositive;
                _positiveCount++;
                GameFeel.PlaySuccess();
                GameFeel.FloatingText("+" + pointsPositive, new Color(0.22f, 0.86f, 0.54f),
                                      new Vector2(0f, 180f));
                break;
            case AnswerQuality.Neutral:
                delta = pointsNeutral;
                _neutralCount++;
                GameFeel.PlayPop();
                GameFeel.FloatingText("+" + pointsNeutral, new Color(0.96f, 0.82f, 0.20f),
                                      new Vector2(0f, 180f));
                break;
            case AnswerQuality.Negative:
                delta = pointsNegative;
                GameFeel.PlayError();
                GameFeel.ScreenFlash(new Color(0.90f, 0.22f, 0.28f), 0.15f, 0.25f);
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

        if (won) CompleteMinigame(_score);
        else     FailMinigame();

        float ratio = _sitManager.Total > 0
            ? (float)_positiveCount / _sitManager.Total
            : 0f;

        ShowResults(
            won,
            GameFeel.StarsFromRatio(won, ratio),
            _score,
            new[]
            {
                "Decisiones adecuadas: " + _positiveCount + " / " + _sitManager.Total,
                "Decisiones neutras: " + _neutralCount
            },
            won ? "¡Buen manejo emocional!" : "Hay margen de mejora",
            won ? "Hablar con calma resuelve mas que gritar."
                : "Respirar y dialogar siempre da mejores resultados.");
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
