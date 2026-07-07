// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class FindChangeGameManager : MinigameBase
{

    [Header("Duración del flash de transición")]
    public float transitionTime = 0.6f;

    // ------------------------------------------------ dificultad (runtime)
    float _observeTime = 5f;
    int   _rounds      = 3;
    int   _roundsToWin = 2;

    SceneGenerator         _gen;
    ChangeManager          _change;
    FindChangeInputHandler _input;
    FindChangeUIController _ui;

    ElementData[]  _elements;
    RectTransform  _gameArea;
    int            _currentRound;
    int            _correctCount;
    int            _errors;
    bool           _roundOver;
    float          _findStartTime;

    enum Phase { Observe, Transition, Find, Result }
    Phase _phase;

    protected override string GetIntroDescription() =>
        "Mira bien la escena y memorízala.\n" +
        "Después, ¡toca lo que haya cambiado!";

    void ApplyDifficulty()
    {
        var diff = GameManager.Instance != null
            ? GameManager.Instance.CurrentDifficulty
            : DifficultyLevel.Easy;

        switch (diff)
        {
            case DifficultyLevel.Medium:
                _rounds      = 4;
                _roundsToWin = 3;
                _observeTime = 4f;
                _gen.columns = 4; _gen.rows = 2; _gen.elemSize = 130f;
                _change.changeSubtlety = 1;
                _change.changeTypeMask = 1;   // color + tamaño
                break;
            case DifficultyLevel.Hard:
                _rounds      = 5;
                _roundsToWin = 4;
                _observeTime = 3f;
                _gen.columns = 4; _gen.rows = 3; _gen.elemSize = 110f;
                _change.changeSubtlety = 2;
                _change.changeTypeMask = 2;   // tamaño + intercambio de posición
                break;
            default:
                _rounds      = 3;
                _roundsToWin = 2;
                _observeTime = 5f;
                _gen.columns = 3; _gen.rows = 2; _gen.elemSize = 150f;
                _change.changeSubtlety = 0;
                _change.changeTypeMask = 0;   // solo color (obvio)
                break;
        }
    }

    protected override void OnMinigameStart()
    {
        EnsureEventSystem();

        _gen    = GetComponent<SceneGenerator>();
        _change = GetComponent<ChangeManager>();
        _input  = GetComponent<FindChangeInputHandler>();
        _ui     = GetComponent<FindChangeUIController>();

        ApplyDifficulty();

        _currentRound = 0;
        _correctCount = 0;
        _errors       = 0;

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

        // Elimina también las sombras de la ronda anterior
        for (int i = _gameArea.childCount - 1; i >= 0; i--)
        {
            var child = _gameArea.GetChild(i);
            if (child.name.StartsWith("Shadow")) Destroy(child.gameObject);
        }

        _elements = _gen.Generate(_gameArea);
        _input.RegisterElements(_elements);
        _input.AcceptInput = false;
        _input.SetElementsInteractable(_elements, false);
        _input.OnElementClicked -= OnElementClicked;
        _input.OnElementClicked += OnElementClicked;

        _phase = Phase.Observe;
        _ui.SetPhase("MEMORIZA  ·  Ronda " + _currentRound + "/" + _rounds,
                     new Color(0.40f, 0.70f, 1.00f));
        _ui.SetFlash(0f);

        float t = _observeTime;
        while (t > 0f)
        {
            _ui.SetTimer(t, _observeTime);
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
        _findStartTime = Time.time;

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

        float rtMs = (Time.time - _findStartTime) * 1000f;

        bool correct      = clickedId == _change.ChangedElementId;
        ElementData corr  = FindById(_change.ChangedElementId);
        ElementData wrong = correct ? null : FindById(clickedId);

        ReportEvent(correct, rtMs);

        if (correct)
        {
            _correctCount++;
            _ui.HighlightCorrect(corr);
            GameFeel.Success(corr != null ? corr.RT : null);
            GameFeel.FloatingText("¡Lo encontraste!", new Color(0.28f, 0.88f, 0.52f));
        }
        else
        {
            _errors++;
            _ui.HighlightWrong(wrong, corr);
            GameFeel.Error(wrong != null ? wrong.RT : null);
        }

        StartCoroutine(ShowResultAfterDelay(correct, 1.2f));
    }

    IEnumerator ShowResultAfterDelay(bool correct, float delay)
    {
        yield return new WaitForSeconds(delay);

        _phase = Phase.Result;

        if (_errors >= 3)
        {
            FailMinigame();
            _ui.SetPhase("Fin", new Color(0.90f, 0.28f, 0.32f));
            ShowFinal(false);
            yield break;
        }

        bool moreRounds  = _currentRound < _rounds;
        bool cantWinNow  = (_rounds - _currentRound) < (_roundsToWin - _correctCount);

        if (!moreRounds || cantWinNow)
        {
            bool won = _correctCount >= _roundsToWin;
            if (won)
            {
                CompleteMinigame(CalculateScore());
                _ui.SetPhase("¡Victoria!", new Color(0.28f, 0.88f, 0.52f));
                GameFeel.Confetti(60);
            }
            else
            {
                FailMinigame();
                _ui.SetPhase("Fin", new Color(0.90f, 0.28f, 0.32f));
            }
            ShowFinal(won);
        }
        else
        {
            string msg = correct
                ? "¡Bien! Ronda " + _currentRound + "/" + _rounds
                : "¡Uy! Ronda " + _currentRound + "/" + _rounds;
            _ui.SetPhase(msg, correct ? new Color(0.28f,0.88f,0.52f) : new Color(0.90f,0.28f,0.32f));
            yield return new WaitForSeconds(1.5f);
            StartCoroutine(RunRound());
        }
    }

    void ShowFinal(bool won)
    {
        float ratio = _currentRound > 0 ? (float)_correctCount / _currentRound : 0f;
        int   stars = GameFeel.StarsFromRatio(won, ratio);
        int   score = won ? CalculateScore() : 0;

        ShowResults(won, stars, score,
            new string[]
            {
                "Cambios encontrados: " + _correctCount + "/" + _currentRound,
                "Errores: " + _errors
            },
            won ? "¡Ojo de halcón!" : "¡Casi!",
            won ? "Encontraste los cambios escondidos."
                : "El cambio era de " + _change.ChangeDescription + ". ¡Otra vez!");
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
