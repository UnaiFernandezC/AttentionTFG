using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class PositionMemoryGameManager : MinigameBase
{

    [Header("Tamano de la cuadricula")]
    public int rows = 4;
    public int cols = 4;

    [Header("Rondas")]
    public int totalRounds  = 3;
    public int roundsToWin  = 2;

    [Header("Casillas a memorizar por ronda")]
    public int[] cellsPerRound = new int[] { 3, 4, 5 };

    [Header("Tiempos (segundos)")]
    public float memorizeTime  = 2.5f;
    public float feedbackTime  = 1.6f;
    public float recallTimeout = 20f;

    PositionMemoryUIController _ui;

    int          _currentRound;
    int          _score;
    int          _roundsWon;
    bool         _confirmed;
    HashSet<int> _currentTargets;

    protected override string GetIntroDescription() =>
        "Observa las casillas iluminadas en la cuadricula.\n" +
        "Cuando se apaguen, selecciona las que recuerdas.\n\n" +
        "Confirma tu eleccion con CONFIRMAR o [ESPACIO].\n" +
        "Gana " + roundsToWin + " de " + totalRounds + " rondas para completar el juego.";

    void ApplyDifficulty()
    {
        var diff = GameManager.Instance != null
            ? GameManager.Instance.CurrentDifficulty
            : DifficultyLevel.Easy;
        switch (diff)
        {
            case DifficultyLevel.Medium:
                totalRounds    = 4;
                roundsToWin    = 3;
                cellsPerRound  = new int[] { 4, 5, 6, 7 };
                memorizeTime   = 2.0f;
                break;
            case DifficultyLevel.Hard:
                totalRounds    = 5;
                roundsToWin    = 4;
                cellsPerRound  = new int[] { 4, 5, 6, 7, 8 };
                memorizeTime   = 1.5f;
                break;
        }
    }

    protected override void OnMinigameStart()
    {
        ApplyDifficulty();
        EnsureEventSystem();

        _ui = GetComponent<PositionMemoryUIController>();

        _currentRound   = 0;
        _score          = 0;
        _roundsWon      = 0;
        _confirmed      = false;
        _currentTargets = new HashSet<int>();

        _ui.BuildUI(rows, cols,
            idx  => OnCellToggled(idx),
            ()   => OnConfirm(),
            ()   => RestartMinigame(),
            ()   => ReturnToGameSelector());

        _ui.UpdateScore(0);
        _ui.UpdateRound(1, totalRounds);

        StartCoroutine(GameLoop());
    }

    protected override void OnMinigameComplete() { }
    protected override void OnMinigameFailed()   { }

    void Update()
    {
        if (IsPlaying && Input.GetKeyDown(KeyCode.Space))
            OnConfirm();
    }

    IEnumerator GameLoop()
    {
        yield return new WaitForSeconds(0.4f);

        for (_currentRound = 0; _currentRound < totalRounds; _currentRound++)
        {
            int cellCount = (_currentRound < cellsPerRound.Length)
                ? cellsPerRound[_currentRound]
                : cellsPerRound[cellsPerRound.Length - 1];

            _ui.UpdateRound(_currentRound + 1, totalRounds);

            _currentTargets = GenerateTargets(rows * cols, cellCount);
            _ui.SetPhaseLabel("Memoriza", new Color(0.65f, 0.35f, 1.00f));
            _ui.SetInfoLabel("Recuerda " + cellCount + " posiciones · " + memorizeTime + "s");
            _ui.ShowMemorizePhase(new List<int>(_currentTargets));

            yield return new WaitForSeconds(memorizeTime);

            _confirmed = false;
            _ui.SetPhaseLabel("¿Cuales eran?", Color.white);
            _ui.SetInfoLabel("Selecciona " + cellCount + " casillas y pulsa CONFIRMAR");
            _ui.ShowRecallPhase();

            float waited = 0f;
            while (!_confirmed && waited < recallTimeout)
            {
                waited += Time.deltaTime;
                yield return null;
            }

            var selected = _ui.GetSelectedIndices();

            int correct = 0;
            int wrong   = 0;
            foreach (int idx in selected)
            {
                if (_currentTargets.Contains(idx)) correct++;
                else                               wrong++;
            }
            int missed = _currentTargets.Count - correct;

            bool roundWon   = (correct == _currentTargets.Count && wrong == 0);
            if (roundWon) _roundsWon++;

            int roundScore  = correct * 10 + (roundWon ? 20 : 0);
            _score         += roundScore;
            _ui.UpdateScore(_score);

            _ui.ShowRoundResult(_currentTargets, selected);

            string msg; Color col;
            if (roundWon)
            {
                msg = "¡Perfecto! +" + roundScore + " pts";
                col = new Color(0.25f, 0.90f, 0.52f);
            }
            else if (correct > 0)
            {
                msg = correct + "/" + _currentTargets.Count + " correctas";
                col = new Color(0.96f, 0.72f, 0.18f);
            }
            else
            {
                msg = "Sin aciertos";
                col = new Color(0.90f, 0.28f, 0.30f);
            }

            _ui.SetPhaseLabel(msg, col);
            _ui.SetInfoLabel("Verde = correcta · Naranja = te faltaba · Rojo = error");

            yield return new WaitForSeconds(feedbackTime);
        }

        yield return new WaitForSeconds(0.3f);

        bool won   = _roundsWon >= roundsToWin;
        CompleteMinigame(_score);

        string sub =
            "Rondas superadas: " + _roundsWon + " / " + totalRounds + "\n" +
            "Puntuacion total: " + _score + " pts";

        _ui.ShowFinalResult(won, sub);
    }

    void OnCellToggled(int idx)
    {
        if (!IsPlaying || _confirmed) return;
        _ui.ToggleCell(idx);
    }

    void OnConfirm()
    {
        if (!IsPlaying || _confirmed) return;
        _confirmed = true;
    }

    HashSet<int> GenerateTargets(int total, int count)
    {
        count = Mathf.Min(count, total);
        var set = new HashSet<int>();
        int tries = 0;
        while (set.Count < count && tries < 2000)
        {
            set.Add(UnityEngine.Random.Range(0, total));
            tries++;
        }
        return set;
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
