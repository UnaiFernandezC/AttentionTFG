using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using static InverseResponseStimulusManager;

/// <summary>
/// GameManager del minijuego "Respuesta Inversa".
/// Hereda MinigameBase → panel de introduccion automatico.
///
/// FLUJO DE JUEGO:
///   1. IntroPanel (MinigameBase) → jugador pulsa Comenzar / ESPACIO
///   2. OnMinigameStart() → construye UI, configura componentes, lanza 1er estimulo
///   3. Por cada estimulo:
///        a. StimulusManager muestra una flecha + regla activa
///        b. InputHandler espera input del jugador (teclado o boton en pantalla)
///        c. Se compara el input con RequiredResponse
///        d. Correcto → _correct++  /  Incorrecto o timeout → _errors++
///        e. Pausa breve, luego siguiente estimulo
///   4. Tras [totalStimuli] estimulos:
///        _correct >= passCount → victoria
///        en caso contrario    → derrota
///
/// CAMBIO DE REGLA:
///   Cada [ruleChangeInterval] estimulos, la regla alterna entre Inverse y Same.
///   Easy: ruleChangeInterval=999 (regla fija: siempre Inverse).
///   Medium: cada 4 estimulos la regla puede cambiar.
///   Hard: cada 2 estimulos (mucho mas dificil, el jugador apenas se adapta).
///
/// AJUSTE DE VELOCIDAD (Inspector):
///   responseTime  → segundos disponibles por estimulo para responder
///   pauseAfterResponse → pausa entre respuesta y siguiente estimulo
///   Reducir estos valores aumenta la presion temporal.
///
/// AJUSTE DE DIFICULTAD (Inspector):
///   Easy   → responseTime=3.0, ruleChangeInterval=999, totalStimuli=10, passCount=7
///   Medium → responseTime=2.0, ruleChangeInterval=4,   totalStimuli=12, passCount=9
///   Hard   → responseTime=1.5, ruleChangeInterval=2,   totalStimuli=15, passCount=12
/// </summary>
public class InverseResponseGameManager : MinigameBase
{
    // ── Inspector ─────────────────────────────────────────────────────────
    [Header("Secuencia")]
    public int   totalStimuli = 10;
    public int   passCount    = 7;     // minimo de correctas para ganar

    [Header("Velocidad")]
    public float responseTime        = 3.0f;  // segundos por estimulo
    public float pauseAfterResponse  = 0.55f; // pausa (s) entre respuesta y siguiente

    [Header("Cambio de regla")]
    [Tooltip("Cada cuantos estimulos cambia la regla. 999 = nunca (Easy).")]
    public int ruleChangeInterval = 999;

    // ── Componentes ───────────────────────────────────────────────────────
    InverseResponseStimulusManager _stimulus;
    InverseResponseInputHandler    _input;
    InverseResponseUIController    _ui;

    // ── Estado ────────────────────────────────────────────────────────────
    int  _stimulusDone;
    int  _correct;
    int  _errors;
    bool _waitingForNext;

    // ═════════════════════════════════════════════════════════════════════

    protected override string GetIntroDescription()
    {
        string ruleHint = ruleChangeInterval < 999
            ? "La regla puede cambiar durante el juego: lee el banner."
            : "Regla siempre activa: INVERSA (pulsa lo contrario).";

        return "Aparece una flecha: pulsa la direccion CONTRARIA.\n" +
               "→ Derecha  →  pulsa ← Izquierda\n" +
               "↑ Arriba   →  pulsa ↓ Abajo\n\n" +
               ruleHint + "\n" +
               "Necesitas " + passCount + " de " + totalStimuli + " correctas para ganar.";
    }

    protected override void OnMinigameStart()
    {
        EnsureEventSystem();

        _stimulus = GetComponent<InverseResponseStimulusManager>();
        _input    = GetComponent<InverseResponseInputHandler>();
        _ui       = GetComponent<InverseResponseUIController>();

        // Configurar StimulusManager
        _stimulus.responseTime       = responseTime;
        _stimulus.ruleChangeInterval = ruleChangeInterval;

        // Construir UI y pasar referencia del InputHandler
        _ui.BuildUI(totalStimuli, () => RestartMinigame(), () => ReturnToGameSelector(), _input);

        // Suscribir eventos
        _stimulus.OnStimulusShown += HandleStimulus;
        _stimulus.OnTimeout       += HandleTimeout;
        _input.OnDirectionInput   += HandleInput;

        // Estado inicial
        _stimulusDone = 0;
        _correct      = 0;
        _errors       = 0;
        _waitingForNext = false;

        _ui.UpdateScore(0, 0, totalStimuli);

        StartCoroutine(LaunchFirstStimulus());
    }

    protected override void OnMinigameComplete() { }
    protected override void OnMinigameFailed()   { }

    // ═════════════════════════════════════════════════════════════════════
    // Update
    // ═════════════════════════════════════════════════════════════════════

    void Update()
    {
        if (!IsPlaying) return;

        _stimulus.Tick();

        if (_stimulus.IsWaitingInput)
            _ui.UpdateTimerBar(_stimulus.StimulusElapsed, responseTime);
    }

    // ═════════════════════════════════════════════════════════════════════
    // Flujo de estimulos
    // ═════════════════════════════════════════════════════════════════════

    IEnumerator LaunchFirstStimulus()
    {
        yield return new WaitForSeconds(0.4f);
        NextStimulus();
    }

    void NextStimulus()
    {
        if (!IsPlaying) return;
        _input.AcceptInput = true;
        _ui.ShowArrowVisible();
        _stimulus.ShowNext();
    }

    // ── El StimulusManager acaba de generar un nuevo estimulo ─────────────
    void HandleStimulus(ArrowDirection dir, GameRule rule)
    {
        _ui.ShowArrow(dir, rule);
        _ui.UpdateTimerBar(0f, responseTime);
    }

    // ── El jugador pulso una direccion ───────────────────────────────────
    void HandleInput(ArrowDirection pressed)
    {
        if (!IsPlaying) return;

        _stimulus.RegisterResponse();
        _input.AcceptInput = false;

        bool correct = (pressed == _stimulus.RequiredResponse);

        if (correct)
        {
            _correct++;
            _ui.ShowFeedback(true, "¡Correcto!");
        }
        else
        {
            _errors++;
            string expected = InverseResponseStimulusManager.DirName(_stimulus.RequiredResponse);
            _ui.ShowFeedback(false, "Error — era " + expected);
        }

        _ui.UpdateScore(_correct, _errors, totalStimuli);
        AdvanceOrEnd();
    }

    // ── El jugador no respondio a tiempo ─────────────────────────────────
    void HandleTimeout()
    {
        if (!IsPlaying) return;

        _input.AcceptInput = false;
        _errors++;
        _ui.ShowFeedback(false, "Tiempo agotado");
        _ui.UpdateScore(_correct, _errors, totalStimuli);
        AdvanceOrEnd();
    }

    // ─────────────────────────────────────────────────────────────────────
    void AdvanceOrEnd()
    {
        _stimulusDone++;

        // Fin anticipado: ya no puede ganar aunque acierte todo lo que queda
        int remaining   = totalStimuli - _stimulusDone;
        bool alreadyWon = _correct >= passCount;
        bool canWin     = (_correct + remaining) >= passCount;

        if (alreadyWon || _stimulusDone >= totalStimuli || !canWin)
            StartCoroutine(EndGame(alreadyWon));
        else
            StartCoroutine(NextStimulusDelayed());
    }

    IEnumerator NextStimulusDelayed()
    {
        _ui.HideArrow();
        yield return new WaitForSeconds(pauseAfterResponse);
        NextStimulus();
    }

    // ─────────────────────────────────────────────────────────────────────
    IEnumerator EndGame(bool won)
    {
        yield return new WaitForSeconds(0.9f);
        int score = CalculateScore(won);
        CompleteMinigame(score);
        _ui.ShowFinalResult(won, _correct, _errors, totalStimuli, score);
    }

    int CalculateScore(bool won)
    {
        if (!won) return 0;
        int  baseS    = 300;
        int  accuracy = _correct * 60;
        int  penalty  = _errors  * 20;
        return Mathf.Max(0, baseS + accuracy - penalty);
    }

    // ═════════════════════════════════════════════════════════════════════
    // Helper
    // ═════════════════════════════════════════════════════════════════════

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
