using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// GameManager del minijuego "Palabras Fugaces".
/// Hereda MinigameBase → panel de introduccion automatico.
///
/// Mecanica:
///   1. Fase MEMORIZAR: se muestran N palabras durante [memorizeTime] segundos.
///   2. Fase ELEGIR: aparece una lista mayor (targets + distractoras) mezclada.
///      El jugador selecciona las palabras que recuerda haber visto.
///   3. El jugador pulsa CONFIRMAR (o [ESPACIO]).
///   4. Feedback visual: verde / naranja / rojo por palabra.
///   5. Se repite [totalRounds] rondas con mas palabras cada vez.
///
/// Puntuacion:
///   Cada palabra correctamente seleccionada  → +15 pts
///   Cada palabra incorrectamente seleccionada→  -8 pts
///   Ronda perfecta (0 errores, 0 omisiones)  → +25 pts bonus
///
/// Condicion de victoria:
///   Ganar [roundsToWin] de [totalRounds] rondas.
///   Una ronda es "ganada" si el jugador acierta al menos el 75% de las palabras objetivo
///   sin seleccionar mas de 1 distractor.
///
/// Dificultad:
///   Facil   → 3 rondas, 4/5/6 palabras objetivo, distractoras = objetivo * 1.5
///   Medio   → 3 rondas, 5/6/7 palabras, distractoras = objetivo * 1.5
///   Dificil → 4 rondas, 6/7/8/9 palabras, distractoras = objetivo * 2
/// </summary>
public class WordMemoryGameManager : MinigameBase
{
    // ------------------------------------------------------------------ //
    // Inspector
    // ------------------------------------------------------------------ //

    [Header("Rondas")]
    public int totalRounds = 3;
    public int roundsToWin = 2;

    [Header("Palabras objetivo por ronda")]
    public int[] targetWordsPerRound = new int[] { 2, 3, 4 };

    [Header("Distractoras = objetivo * este multiplicador (redondeado)")]
    public float distractorMultiplier = 1.5f;

    [Header("Tiempo de memorizar (s)")]
    public float memorizeTime = 3.5f;

    [Header("Tiempo de feedback tras confirmar (s)")]
    public float feedbackTime = 2.0f;

    // ------------------------------------------------------------------ //
    // Pool de palabras (espanol, sin acentos para evitar problemas de encoding)
    // ------------------------------------------------------------------ //

    static readonly string[] WORD_POOL = new string[]
    {
        "PERRO",    "GATO",     "CASA",     "ARBOL",    "LUNA",
        "SOL",      "MAR",      "PAN",      "LECHE",    "MESA",
        "SILLA",    "LIBRO",    "TREN",     "AVION",    "COCHE",
        "FLOR",     "NUBE",     "PIEDRA",   "AGUA",     "FUEGO",
        "TIERRA",   "VIENTO",   "ESTRELLA", "MONTANA",  "RIO",
        "PUENTE",   "CIUDAD",   "CAMPO",    "BOSQUE",   "PLAYA",
        "PAJARO",   "PEZ",      "CABALLO",  "LOBO",     "OSO",
        "TIGRE",    "CONEJO",   "RANA",     "SERPIENTE","AGUILA",
        "NARANJA",  "MANZANA",  "UVAS",     "LIMON",    "PERA",
        "ZAPATO",   "RELOJ",    "LAMPARA",  "VENTANA",  "PUERTA"
    };

    // ------------------------------------------------------------------ //
    // Estado interno
    // ------------------------------------------------------------------ //

    WordMemoryUIController _ui;

    int          _currentRound;
    int          _score;
    int          _roundsWon;
    bool         _confirmed;

    List<string> _currentTargets;    // palabras a memorizar esta ronda
    List<string> _allWordsShown;     // targets + distractoras mezcladas
    HashSet<int> _targetIndices;     // indices dentro de _allWordsShown que son objetivo

    // ════════════════════════════════════════════════════════════════════

    protected override string GetIntroDescription() =>
        "Apareceran varias palabras en pantalla durante unos segundos.\n" +
        "Memoriza cuales son, porque desapareceran.\n\n" +
        "Luego elige esas mismas palabras de una lista mayor.\n" +
        "Gana " + roundsToWin + " de " + totalRounds + " rondas para completar el juego.";

    protected override void OnMinigameStart()
    {
        EnsureEventSystem();

        _ui = GetComponent<WordMemoryUIController>();

        _currentRound = 0;
        _score        = 0;
        _roundsWon    = 0;
        _confirmed    = false;

        _ui.BuildUI(
            idx => OnWordToggled(idx),
            ()  => OnConfirm(),
            ()  => RestartMinigame(),
            ()  => ReturnToGameSelector());

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
            int targetCount = (_currentRound < targetWordsPerRound.Length)
                ? targetWordsPerRound[_currentRound]
                : targetWordsPerRound[targetWordsPerRound.Length - 1];

            int distractorCount = Mathf.Max(2, Mathf.RoundToInt(targetCount * distractorMultiplier));

            _ui.UpdateRound(_currentRound + 1, totalRounds);

            // ── Generar palabras ──────────────────────────────────────
            BuildWordLists(targetCount, distractorCount);

            // ── Fase MEMORIZAR ────────────────────────────────────────
            _ui.SetPhaseLabel("Memoriza", new Color(0.58f, 0.28f, 0.92f));
            _ui.SetInfoLabel(targetCount + " palabras · " + memorizeTime + " segundos");
            _ui.ShowMemorizePhase(_currentTargets);

            // Countdown visual
            float elapsed = 0f;
            while (elapsed < memorizeTime)
            {
                elapsed += Time.deltaTime;
                _ui.SetCountdown(1f - elapsed / memorizeTime);
                yield return null;
            }

            // ── Fase ELEGIR ───────────────────────────────────────────
            _confirmed = false;
            _ui.SetPhaseLabel("¿Cuales viste?", Color.white);
            _ui.SetInfoLabel("Selecciona " + targetCount + " palabras y confirma");
            _ui.ShowChoosePhase(_allWordsShown);

            // Esperar confirmacion
            float waitMax = 30f;
            float waited  = 0f;
            while (!_confirmed && waited < waitMax)
            {
                waited += Time.deltaTime;
                yield return null;
            }

            // ── Evaluar ───────────────────────────────────────────────
            var selected = _ui.GetSelectedIndices();
            int correct  = 0;
            int wrong    = 0;

            foreach (int idx in selected)
            {
                if (_targetIndices.Contains(idx)) correct++;
                else                              wrong++;
            }
            int missed = _currentTargets.Count - correct;

            // Ronda ganada: >= 75% aciertos y <= 1 distractor seleccionado
            bool roundWon = (correct >= Mathf.CeilToInt(targetCount * 0.75f)) && (wrong <= 1);
            if (roundWon) _roundsWon++;

            int roundScore = correct * 15 - wrong * 8;
            if (correct == targetCount && wrong == 0) roundScore += 25; // bonus perfecto
            roundScore = Mathf.Max(0, roundScore);
            _score    += roundScore;
            _ui.UpdateScore(_score);

            // ── Feedback visual ───────────────────────────────────────
            _ui.ShowWordResult(_targetIndices, selected);

            string msg; Color col;
            if (correct == targetCount && wrong == 0)
            {
                msg = "¡Perfecto! +" + roundScore + " pts";
                col = new Color(0.25f, 0.90f, 0.52f);
            }
            else if (roundWon)
            {
                msg = correct + "/" + targetCount + " palabras correctas";
                col = new Color(0.96f, 0.72f, 0.18f);
            }
            else
            {
                msg = correct + "/" + targetCount + " correctas · " + wrong + " errores";
                col = new Color(0.90f, 0.28f, 0.30f);
            }

            _ui.SetPhaseLabel(msg, col);
            _ui.SetInfoLabel("Verde = correcto · Naranja = te faltaba · Rojo = error");

            yield return new WaitForSeconds(feedbackTime);
        }

        // ── Fin de partida ────────────────────────────────────────────
        yield return new WaitForSeconds(0.3f);

        bool won = _roundsWon >= roundsToWin;
        CompleteMinigame(_score);

        string sub =
            "Rondas superadas: " + _roundsWon + " / " + totalRounds + "\n" +
            "Puntuacion total: " + _score + " pts";

        _ui.ShowFinalResult(won, sub);
    }

    // ════════════════════════════════════════════════════════════════════
    // Callbacks
    // ════════════════════════════════════════════════════════════════════

    void OnWordToggled(int idx)
    {
        if (!IsPlaying || _confirmed) return;
        _ui.ToggleWord(idx);
    }

    void OnConfirm()
    {
        if (!IsPlaying || _confirmed) return;
        _confirmed = true;
    }

    // ════════════════════════════════════════════════════════════════════
    // Generacion de palabras
    // ════════════════════════════════════════════════════════════════════

    void BuildWordLists(int targetCount, int distractorCount)
    {
        // Mezclar el pool
        var pool = new List<string>(WORD_POOL);
        Shuffle(pool);

        int needed = targetCount + distractorCount;
        if (needed > pool.Count) needed = pool.Count;

        _currentTargets = new List<string>();
        for (int i = 0; i < Mathf.Min(targetCount, pool.Count); i++)
            _currentTargets.Add(pool[i]);

        var distractors = new List<string>();
        for (int i = targetCount; i < Mathf.Min(needed, pool.Count); i++)
            distractors.Add(pool[i]);

        // Mezclar targets + distractoras
        _allWordsShown = new List<string>(_currentTargets);
        _allWordsShown.AddRange(distractors);
        Shuffle(_allWordsShown);

        // Calcular indices de los targets en la lista mezclada
        _targetIndices = new HashSet<int>();
        var targetSet  = new HashSet<string>(_currentTargets);
        for (int i = 0; i < _allWordsShown.Count; i++)
            if (targetSet.Contains(_allWordsShown[i]))
                _targetIndices.Add(i);
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
