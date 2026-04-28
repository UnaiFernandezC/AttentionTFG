using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// GameManager del minijuego "Consecuencias Emocionales".
/// Hereda MinigameBase → panel de introduccion automatico.
///
/// Flujo de juego:
///   1. IntroPanel (MinigameBase) → jugador pulsa Comenzar
///   2. OnMinigameStart() → inicializa situaciones y muestra la primera
///   3. ShowCurrentSituation() → UI muestra situacion + opciones
///   4. HandleOptionChosen() → calcula puntos, muestra consecuencia en UI
///   5. NextSituation() → avanza o llama EndGame()
///   6. EndGame() → muestra panel de resultado final
///
/// Puntuacion:
///   Respuesta adecuada (Positive) → +[pointsPositive] pts  (por defecto 20)
///   Respuesta neutra   (Neutral)  → +[pointsNeutral]  pts  (por defecto 8)
///   Respuesta poco adecuada (Negative) → 0 pts
///
/// Condicion de victoria:
///   [positiveCount] >= [roundsToWin]  (por defecto 3 de 5)
///
/// Para ajustar dificultad desde el Inspector:
///   Facil   → situationCount=5, roundsToWin=3 (situaciones mas claras en SituationManager)
///   Medio   → situationCount=5, roundsToWin=3 (opciones mas parecidas)
///   Dificil → situationCount=6, roundsToWin=4 (mas situaciones, mas ambiguas)
/// </summary>
public class ConsequencesGameManager : MinigameBase
{
    // ------------------------------------------------------------------ //
    // Inspector
    // ------------------------------------------------------------------ //

    [Header("Cuantas situaciones se juegan por partida")]
    public int situationCount = 5;

    [Header("Decisiones adecuadas necesarias para ganar")]
    public int roundsToWin = 3;

    [Header("Puntuacion por tipo de respuesta")]
    public int pointsPositive = 20;
    public int pointsNeutral  = 8;
    public int pointsNegative = 0;

    // ------------------------------------------------------------------ //
    // Componentes
    // ------------------------------------------------------------------ //

    ConsequencesSituationManager _sitManager;
    ConsequencesUIController     _ui;

    // ------------------------------------------------------------------ //
    // Estado
    // ------------------------------------------------------------------ //

    int                _currentIndex;
    int                _score;
    int                _positiveCount;
    EmotionalSituation _currentSituation;

    // ════════════════════════════════════════════════════════════════════

    protected override string GetIntroDescription() =>
        "Se te presentaran situaciones del dia a dia.\n" +
        "Lee cada una y elige la reaccion que consideres mas adecuada.\n\n" +
        "No hay prisa: piensa antes de responder.\n" +
        "Gana " + roundsToWin + " de " + situationCount + " decisiones adecuadas para completar el juego.";

    protected override void OnMinigameStart()
    {
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

        // Esperar un frame para que el layout de Canvas se calcule
        // antes de construir los botones de opciones
        StartCoroutine(StartAfterLayout());
    }

    protected override void OnMinigameComplete() { }
    protected override void OnMinigameFailed()   { }

    // ════════════════════════════════════════════════════════════════════
    // Arranque diferido un frame
    // ════════════════════════════════════════════════════════════════════

    IEnumerator StartAfterLayout()
    {
        yield return null;   // esperar un frame de layout
        ShowCurrentSituation();
    }

    // ════════════════════════════════════════════════════════════════════
    // Logica de juego
    // ════════════════════════════════════════════════════════════════════

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

        // Calcular puntos
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

    // ════════════════════════════════════════════════════════════════════
    // Helper
    // ════════════════════════════════════════════════════════════════════

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
