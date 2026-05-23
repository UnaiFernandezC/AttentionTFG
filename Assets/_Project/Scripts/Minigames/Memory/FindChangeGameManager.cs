using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class FindChangeGameManager : MinigameBase
{

    [Header("Tiempo de observación (segundos)")]
    public float observeTime     = 5f;
    [Header("Duración del flash de transición")]
    public float transitionTime  = 0.6f;
    [Header("Rondas totales")]
    public int   rounds          = 3;
    [Header("Rondas necesarias para ganar")]
    public int   roundsToWin     = 2;

    SceneGenerator         _gen;
    ChangeManager          _change;
    FindChangeInputHandler _input;
    FindChangeUIController _ui;

    ElementData[]  _elements;
    RectTransform  _gameArea;
    int            _currentRound;
    int            _correctCount;
    bool           _roundOver;

    enum Phase { Observe, Transition, Find, Result }
    Phase _phase;

    protected override string GetIntroDescription() =>
        "Se mostrará una escena con formas de colores.\n" +
        "Memorízala bien durante " + (int)observeTime + " segundos.\n" +
        "Después, un elemento habrá cambiado sutilmente.\n" +
        "Haz clic en el elemento que creas que cambió.\n" +
        "Completa " + roundsToWin + " de " + rounds + " rondas para ganar.";

    void ApplyDifficulty()
    {
        var diff = GameManager.Instance != null
            ? GameManager.Instance.CurrentDifficulty
            : DifficultyLevel.Easy;
        switch (diff)
        {
            case DifficultyLevel.Medium:
                rounds       = 4;
                roundsToWin  = 3;
                observeTime  = 4f;
                break;
            case DifficultyLevel.Hard:
                rounds       = 5;
                roundsToWin  = 4;
                observeTime  = 3f;
                break;
        }
    }

    protected override void OnMinigameStart()
    {
        ApplyDifficulty();
        EnsureEventSystem();

        _gen    = GetComponent<SceneGenerator>();
        _change = GetComponent<ChangeManager>();
        _input  = GetComponent<FindChangeInputHandler>();
        _ui     = GetComponent<FindChangeUIController>();

        _currentRound = 0;
        _correctCount = 0;

        _gameArea = _ui.BuildUI(() => RestartMinigame(), () => ReturnToGameSelector());
        StartCoroutine(RunRound());
    }

    protected override void OnMinigameComplete() { }
    protected override void OnMinigameFailed()   { }

    IEnumerator RunRound()
    {
        _roundOver = false;
        _currentRound++;

        if (_elements != null)
            foreach (var e in _elements)
                if (e.Go != null) Destroy(e.Go);

        _elements = _gen.Generate(_gameArea);
        _input.RegisterElements(_elements);
        _input.AcceptInput = false;
        _input.SetElementsInteractable(_elements, false);
        _input.OnElementClicked -= OnElementClicked;
        _input.OnElementClicked += OnElementClicked;

        _phase = Phase.Observe;
        _ui.SetPhase("MEMORIZA", new Color(0.40f, 0.70f, 1.00f));
        _ui.SetFlash(0f);

        float t = observeTime;
        while (t > 0f)
        {
            _ui.SetTimer(t, observeTime);
            t -= Time.deltaTime;
            yield return null;
        }
        _ui.HideTimer();

        _phase = Phase.Transition;
        _ui.SetPhase("...", new Color(0.60f, 0.60f, 0.60f));

        float half = transitionTime * 0.5f;
        for (float ft = 0f; ft < half; ft += Time.deltaTime)
        {
            _ui.SetFlash(ft / half);
            yield return null;
        }

        _change.ApplyChange(_elements);

        yield return new WaitForSeconds(0.05f);

        for (float ft = 0f; ft < half; ft += Time.deltaTime)
        {
            _ui.SetFlash(1f - ft / half);
            yield return null;
        }
        _ui.SetFlash(0f);

        _phase = Phase.Find;
        _ui.SetPhase("¿QUÉ CAMBIÓ?", new Color(0.96f, 0.82f, 0.22f));
        _input.SetElementsInteractable(_elements, true);
        _input.AcceptInput = true;

        float findTimeout = 10f;
        while (!_roundOver && findTimeout > 0f)
        {
            findTimeout -= Time.deltaTime;
            yield return null;
        }

        if (!_roundOver) OnElementClicked(-1);
    }

    void OnElementClicked(int clickedId)
    {
        if (_phase != Phase.Find || _roundOver) return;
        _roundOver = true;
        _input.AcceptInput = false;
        _input.SetElementsInteractable(_elements, false);

        bool correct      = clickedId == _change.ChangedElementId;
        ElementData corr  = FindById(_change.ChangedElementId);
        ElementData wrong = correct ? null : FindById(clickedId);

        if (correct)
        {
            _correctCount++;
            _ui.HighlightCorrect(corr);
        }
        else
        {
            _ui.HighlightWrong(wrong, corr);
        }

        StartCoroutine(ShowResultAfterDelay(correct, 1.2f));
    }

    IEnumerator ShowResultAfterDelay(bool correct, float delay)
    {
        yield return new WaitForSeconds(delay);

        _phase = Phase.Result;
        bool moreRounds = _currentRound < rounds;

        if (!moreRounds || (!correct && _correctCount < roundsToWin && (rounds - _currentRound) < (roundsToWin - _correctCount)))
        {

            bool won = _correctCount >= roundsToWin;
            if (won)
            {
                CompleteMinigame(CalculateScore());
                _ui.SetPhase("¡Victoria!", new Color(0.28f, 0.88f, 0.52f));
                _ui.ShowResult(true,
                    "Encontraste " + _correctCount + " de " + rounds + " cambios.\n+" + CalculateScore() + " puntos");
            }
            else
            {
                FailMinigame();
                _ui.SetPhase("Fin", new Color(0.90f, 0.28f, 0.32f));
                _ui.ShowResult(false,
                    "Encontraste " + _correctCount + " de " + rounds + " cambios.\nEl elemento que cambió era: " + _change.ChangeDescription);
            }
        }
        else if (moreRounds)
        {

            string msg = correct
                ? "¡Bien! Ronda " + _currentRound + "/" + rounds
                : "Fallaste. Ronda " + _currentRound + "/" + rounds;
            _ui.SetPhase(msg, correct ? new Color(0.28f,0.88f,0.52f) : new Color(0.90f,0.28f,0.32f));
            yield return new WaitForSeconds(1.5f);
            StartCoroutine(RunRound());
        }
    }

    int CalculateScore()
    {
        int base_ = 500;
        int bonus = _correctCount * 150;
        return base_ + bonus;
    }

    ElementData FindById(int id)
    {
        if (_elements == null || id < 0) return null;
        foreach (var e in _elements) if (e.Id == id) return e;
        return null;
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
