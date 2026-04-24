using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// GameManager del minijuego "Memoria de Posiciones".
/// Hereda MinigameBase → panel de introduccion automatico.
///
/// Mecanica:
///   1. Fase MEMORIZAR: se iluminan N casillas durante [memorizeTime] segundos.
///   2. Fase RECORDAR: las casillas se apagan; el jugador selecciona las que recuerda.
///   3. El jugador pulsa CONFIRMAR (o se agota el tiempo de respuesta).
///   4. Se muestran verde / naranja / rojo segun aciertos.
///   5. Se repite [totalRounds] rondas con mas casillas cada vez.
///
/// Puntuacion:
///   Cada casilla correcta → +10 pts
///   Ronda perfecta (sin errores) → +20 pts bonus
///
/// Condicion de victoria:
///   Ganar [roundsToWin] de [totalRounds] rondas (ronda ganada = cero errores).
///
/// Dificultad en Inspector:
///   Facil   → 4x4, 3 rondas, 3/4/5 casillas, 2.5s memorizar
///   Medio   → 4x4, 3 rondas, 4/5/6 casillas, 2.0s memorizar
///   Dificil → 5x5, 4 rondas, 5/6/7/8 casillas, 1.5s memorizar
/// </summary>
public class PositionMemoryGameManager : MinigameBase
{
    // ------------------------------------------------------------------ //
    // Inspector
    // ------------------------------------------------------------------ //

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
    public float recallTimeout = 20f;    // tiempo maximo para responder

    // ------------------------------------------------------------------ //
    // Estado
    // ------------------------------------------------------------------ //

    PositionMemoryUIController _ui;

    int          _currentRound;
    int          _score;
    int          _roundsWon;
    bool         _confirmed;
    HashSet<int> _currentTargets;

    // ════════════════════════════════════════════════════════════════════

    protected override string GetIntroDescription() =>
        "Observa las casillas iluminadas en la cuadricula.\n" +
        "Cuando se apaguen, selecciona las que recuerdas.\n\n" +
        "Confirma tu eleccion con CONFIRMAR o [ESPACIO].\n" +
        "Gana " + roundsToWin + " de " + totalRounds + " rondas para completar el juego.";

    protected override void OnMinigameStart()
    {
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

    // ════════════════════════════════════════════════════════════════════
    // Update — confirmacion por teclado
    // ════════════════════════════════════════════════════════════════════

    void Update()
    {
        if (IsPlaying && Input.GetKeyDown(KeyCode.Space))
            OnConfirm();
    }

    // ════════════════════════════════════════════════════════════════════
    // Game Loop
    // ════════════════════════════════════════════════════════════════════

    IEnumerator GameLoop()
    {
        yield return new WaitForSeconds(0.4f);

        for (_currentRound = 0; _currentRound < totalRounds; _currentRound++)
        {
            int cellCount = (_currentRound < cellsPerRound.Length)
                ? cellsPerRound[_currentRound]
                : cellsPerRound[cellsPerRound.Length - 1];

            _ui.UpdateRound(_currentRound + 1, totalRounds);

            // ── Fase MEMORIZAR ────────────────────────────────────────
            _currentTargets = GenerateTargets(rows * cols, cellCount);
            _ui.SetPhaseLabel("Memoriza", new Color(0.65f, 0.35f, 1.00f));
            _ui.SetInfoLabel("Recuerda " + cellCount + " posiciones · " + memorizeTime + "s");
            _ui.ShowMemorizePhase(new List<int>(_currentTargets));

            yield return new WaitForSeconds(memorizeTime);

            // ── Fase RECORDAR ─────────────────────────────────────────
            _confirmed = false;
            _ui.SetPhaseLabel("¿Cuales eran?", Color.white);
            _ui.SetInfoLabel("Selecciona " + cellCount + " casillas y pulsa CONFIRMAR");
            _ui.ShowRecallPhase();

            // Esperar confirmacion o timeout
            float waited = 0f;
            while (!_confirmed && waited < recallTimeout)
            {
                waited += Time.deltaTime;
                yield return null;
            }

            // ── Evaluar ───────────────────────────────────────────────
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

            // ── Feedback visual ───────────────────────────────────────
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

        // ── Fin de partida ────────────────────────────────────────────
        yield return new WaitForSeconds(0.3f);

        bool won   = _roundsWon >= roundsToWin;
        CompleteMinigame(_score);

        string sub =
            "Rondas superadas: " + _roundsWon + " / " + totalRounds + "\n" +
            "Puntuacion total: " + _score + " pts";

        _ui.ShowFinalResult(won, sub);
    }

    // ════════════════════════════════════════════════════════════════════
    // Callbacks
    // ════════════════════════════════════════════════════════════════════

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

    // ════════════════════════════════════════════════════════════════════
    // Helpers
    // ════════════════════════════════════════════════════════════════════

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
