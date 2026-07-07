// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// "¿Dónde estaba?" — memoria de asociación objeto-lugar:
/// se muestran varios objetos en una rejilla, se ocultan y el niño debe
/// tocar la casilla donde estaba el objeto por el que se pregunta.
/// </summary>
public class PositionMemoryGameManager : MinigameBase
{

    // ------------------------------------------------ dificultad (runtime)
    int   _rows          = 2;
    int   _cols          = 3;
    int   _objectCount   = 3;
    int   _questionCount = 2;
    float _memorizeTime  = 4f;

    const int   TOTAL_ROUNDS    = 2;
    const float ANSWER_TIMEOUT  = 15f;
    const int   POINTS_PER_HIT  = 50;

    PositionMemoryUIController _ui;

    int   _score;
    int   _totalCorrect;
    int   _totalQuestions;
    int   _clickedCell;
    bool  _answered;

    protected override string GetIntroDescription() =>
        "Mira dónde está cada objeto.\n" +
        "Luego, ¡toca la casilla donde estaba!";

    void ApplyDifficulty()
    {
        var diff = GameManager.Instance != null
            ? GameManager.Instance.CurrentDifficulty
            : DifficultyLevel.Easy;

        switch (diff)
        {
            case DifficultyLevel.Medium:
                _rows = 3; _cols = 3;
                _objectCount = 4; _questionCount = 3;
                _memorizeTime = 5f;
                break;
            case DifficultyLevel.Hard:
                _rows = 3; _cols = 4;
                _objectCount = 6; _questionCount = 4;
                _memorizeTime = 6f;
                break;
            default:
                _rows = 2; _cols = 3;
                _objectCount = 3; _questionCount = 2;
                _memorizeTime = 4f;
                break;
        }
    }

    protected override void OnMinigameStart()
    {
        ApplyDifficulty();
        EnsureEventSystem();

        _ui = GetComponent<PositionMemoryUIController>();
        if (_ui == null) _ui = gameObject.AddComponent<PositionMemoryUIController>();

        _score          = 0;
        _totalCorrect   = 0;
        _totalQuestions = 0;

        _ui.BuildUI(_rows, _cols, OnCellClicked);
        _ui.UpdateScore(0);
        _ui.UpdateRound(1, TOTAL_ROUNDS);

        StartCoroutine(GameLoop());
    }

    protected override void OnMinigameComplete() { }
    protected override void OnMinigameFailed()   { }

    IEnumerator GameLoop()
    {
        yield return new WaitForSeconds(0.5f);

        for (int round = 0; round < TOTAL_ROUNDS; round++)
        {
            _ui.UpdateRound(round + 1, TOTAL_ROUNDS);
            _ui.HideQuestion();
            _ui.EnableInput(false);

            // -------- colocar objetos
            var placements = GeneratePlacements();

            _ui.SetPhaseLabel("¡Memoriza!", new Color(0.65f, 0.35f, 1.00f));
            _ui.SetInfoLabel("Fíjate en dónde está cada objeto");
            _ui.PlaceObjects(placements);
            GameFeel.PlayPop();

            float elapsed = 0f;
            while (elapsed < _memorizeTime)
            {
                elapsed += Time.deltaTime;
                _ui.SetCountdown(1f - elapsed / _memorizeTime);
                yield return null;
            }
            _ui.SetCountdown(0f);

            // -------- tapar y preguntar
            _ui.CoverAllCells();
            GameFeel.PlayPop();
            yield return new WaitForSeconds(0.5f);

            var askOrder = new List<int>(placements.Keys);
            Shuffle(askOrder);
            int questions = Mathf.Min(_questionCount, askOrder.Count);

            for (int q = 0; q < questions; q++)
            {
                int cellTarget = askOrder[q];
                var obj        = placements[cellTarget];

                _totalQuestions++;
                _ui.SetPhaseLabel("Pregunta " + (q + 1) + " de " + questions, Color.white);
                _ui.SetInfoLabel("Toca la casilla correcta");
                _ui.ShowQuestion(obj);
                _ui.EnableInput(true);

                _answered    = false;
                _clickedCell = -1;
                float askStart = Time.time;
                float waited   = 0f;
                while (!_answered && waited < ANSWER_TIMEOUT)
                {
                    waited += Time.deltaTime;
                    yield return null;
                }
                _ui.EnableInput(false);

                float rtMs   = (Time.time - askStart) * 1000f;
                bool correct = _answered && _clickedCell == cellTarget;
                ReportEvent(correct, rtMs);

                if (correct)
                {
                    _totalCorrect++;
                    _score += POINTS_PER_HIT;
                    _ui.UpdateScore(_score);
                    _ui.RevealCell(cellTarget, obj, true);
                    GameFeel.Success(_ui.GetCellRT(cellTarget));
                    GameFeel.FloatingText("+" + POINTS_PER_HIT, new Color(0.25f, 0.90f, 0.52f));
                    _ui.SetPhaseLabel("¡Muy bien!", new Color(0.25f, 0.90f, 0.52f));
                }
                else
                {
                    if (_answered && _clickedCell >= 0)
                    {
                        placements.TryGetValue(_clickedCell, out var wrongObj);
                        _ui.RevealCell(_clickedCell,
                            placements.ContainsKey(_clickedCell) ? (PositionMemoryUIController.ObjDef?)wrongObj : null,
                            false);
                        GameFeel.Error(_ui.GetCellRT(_clickedCell));
                    }
                    else
                    {
                        GameFeel.PlayError();
                    }
                    _ui.RevealCell(cellTarget, obj, true);
                    _ui.SetPhaseLabel("Estaba aquí", new Color(0.96f, 0.62f, 0.18f));
                }

                yield return new WaitForSeconds(1.5f);

                // Volvemos a tapar para la siguiente pregunta
                _ui.ResetCellVisual(cellTarget);
                if (_clickedCell >= 0 && _clickedCell != cellTarget)
                    _ui.ResetCellVisual(_clickedCell);
                _ui.HideQuestion();
            }

            yield return new WaitForSeconds(0.4f);
        }

        // -------- final
        bool won = _totalCorrect >= Mathf.CeilToInt(_totalQuestions * 0.5f);
        if (won) { CompleteMinigame(_score); GameFeel.Confetti(60); }
        else       FailMinigame();

        float ratio = _totalQuestions > 0 ? (float)_totalCorrect / _totalQuestions : 0f;
        int   stars = GameFeel.StarsFromRatio(won, ratio);

        ShowResults(won, stars, won ? _score : 0,
            new string[]
            {
                "Aciertos: " + _totalCorrect + "/" + _totalQuestions,
                "Objetos por ronda: " + _objectCount
            },
            won ? "¡Detective de objetos!" : "¡Casi lo tienes!",
            won ? "Recordaste dónde estaba cada cosa."
                : "Fíjate bien en los lugares. ¡Otra vez!");
    }

    void OnCellClicked(int idx)
    {
        if (!IsPlaying || _answered) return;
        _clickedCell = idx;
        _answered    = true;
    }

    Dictionary<int, PositionMemoryUIController.ObjDef> GeneratePlacements()
    {
        int totalCells = _rows * _cols;
        int count      = Mathf.Min(_objectCount, Mathf.Min(totalCells,
                                   PositionMemoryUIController.OBJECTS.Length));

        var cellPool = new List<int>();
        for (int i = 0; i < totalCells; i++) cellPool.Add(i);
        Shuffle(cellPool);

        var objPool = new List<int>();
        for (int i = 0; i < PositionMemoryUIController.OBJECTS.Length; i++) objPool.Add(i);
        Shuffle(objPool);

        var result = new Dictionary<int, PositionMemoryUIController.ObjDef>();
        for (int i = 0; i < count; i++)
            result[cellPool[i]] = PositionMemoryUIController.OBJECTS[objPool[i]];

        return result;
    }

    static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            T tmp = list[i]; list[i] = list[j]; list[j] = tmp;
        }
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
