using System;
using UnityEngine;

/// <summary>
/// Gestiona la lógica de tiempo del minijuego "Reacción rápida".
///
/// Máquina de estados:
///   Idle → Waiting (tiempo aleatorio) → Stimulus (aparece estímulo) → Resolved
///
/// Countdown por ronda:
///   Ronda 1 → ReactionTimeLimit=3 s
///   Ronda 2 → ReactionTimeLimit=2 s
///   Ronda 3 → ReactionTimeLimit=1 s
///   (asignado por el GameManager antes de cada StartRound)
///
/// Dificultad (ajustar en Inspector del GameManager):
///   Fácil  → waitMin=2.5, waitMax=5.5  (ventana amplia, predecible)
///   Medio  → waitMin=1.5, waitMax=4.5
///   Difícil→ waitMin=0.8, waitMax=6.0  (rango extremo, impredecible)
/// </summary>
public class ReactionManager : MonoBehaviour
{
    // ------------------------------------------------------------------ //
    // Inspector
    // ------------------------------------------------------------------ //

    [Header("Tiempo de espera antes del estímulo (segundos)")]
    public float waitMin = 2.5f;
    public float waitMax = 5.5f;

    // ------------------------------------------------------------------ //
    // Estado
    // ------------------------------------------------------------------ //

    public enum State { Idle, Waiting, Stimulus, Resolved }

    public State CurrentState   { get; private set; } = State.Idle;
    public long  LastReactionMs { get; private set; }   // ms de reacción
    public bool  WasTooEarly    { get; private set; }   // true si click prematuro
    public bool  WasTimeout     { get; private set; }   // true si se agotó el tiempo

    /// <summary>Segundos transcurridos desde que apareció el estímulo.</summary>
    public float StimulusElapsed { get; private set; }

    /// <summary>
    /// Límite de tiempo para reaccionar tras el estímulo.
    /// Asignar antes de llamar a StartRound().
    /// </summary>
    public float ReactionTimeLimit { get; set; } = 3f;

    // ------------------------------------------------------------------ //
    // Eventos
    // ------------------------------------------------------------------ //

    public event Action OnStimulusAppeared;   // estímulo visible → ¡ya!
    public event Action OnReactionRegistered; // jugador reaccionó o tiempo agotado

    // ------------------------------------------------------------------ //
    // Internos
    // ------------------------------------------------------------------ //

    float  _waitTarget;    // tiempo que hay que esperar
    float  _waitElapsed;   // tiempo transcurrido en fase Waiting
    bool   _active;

    // ═════════════════════════════════════════════════════════════════════

    /// <summary>Inicia una nueva ronda desde cero.</summary>
    public void StartRound()
    {
        WasTooEarly      = false;
        WasTimeout       = false;
        LastReactionMs   = 0;
        StimulusElapsed  = 0f;
        _waitElapsed     = 0f;
        _waitTarget      = UnityEngine.Random.Range(waitMin, waitMax);
        _active          = true;
        CurrentState     = State.Waiting;
        Debug.Log($"[ReactionManager] Ronda iniciada. Espera: {_waitTarget:F2}s | Límite reacción: {ReactionTimeLimit}s");
    }

    /// <summary>
    /// Llamado por el InputHandler cuando el jugador hace click / pulsa tecla.
    /// Devuelve true si la entrada se ha procesado.
    /// </summary>
    public bool RegisterInput()
    {
        if (!_active) return false;

        if (CurrentState == State.Waiting)
        {
            WasTooEarly  = true;
            CurrentState = State.Resolved;
            _active      = false;
            OnReactionRegistered?.Invoke();
            return true;
        }

        if (CurrentState == State.Stimulus)
        {
            LastReactionMs = Mathf.RoundToInt(StimulusElapsed * 1000f);
            WasTooEarly    = false;
            WasTimeout     = false;
            CurrentState   = State.Resolved;
            _active        = false;
            OnReactionRegistered?.Invoke();
            return true;
        }

        return false;
    }

    /// <summary>Detiene la ronda sin registrar resultado (reset).</summary>
    public void Cancel()
    {
        _active      = false;
        CurrentState = State.Idle;
    }

    // ═════════════════════════════════════════════════════════════════════

    void Update()
    {
        if (!_active) return;

        if (CurrentState == State.Waiting)
        {
            _waitElapsed += Time.deltaTime;
            if (_waitElapsed >= _waitTarget)
            {
                StimulusElapsed = 0f;
                CurrentState    = State.Stimulus;
                OnStimulusAppeared?.Invoke();
            }
            return;
        }

        if (CurrentState == State.Stimulus)
        {
            StimulusElapsed += Time.deltaTime;
            if (StimulusElapsed >= ReactionTimeLimit)
            {
                // Tiempo agotado → ronda fallida automáticamente
                WasTimeout   = true;
                WasTooEarly  = false;
                CurrentState = State.Resolved;
                _active      = false;
                OnReactionRegistered?.Invoke();
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // Helpers de evaluación
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>Devuelve un mensaje textual según el tiempo de reacción.</summary>
    public static string EvaluateTime(long ms)
    {
        if (ms < 180)  return "¡Increíble!";
        if (ms < 280)  return "¡Excelente!";
        if (ms < 400)  return "¡Muy bien!";
        if (ms < 550)  return "Bien";
        if (ms < 750)  return "Puede mejorar";
        return "Lento";
    }

    /// <summary>
    /// Puntuación por ronda basada en velocidad.
    /// Máximo 500 pts en 100 ms, decae linealmente hasta 0 pts en 1000 ms.
    /// </summary>
    public static int CalcRoundScore(long ms)
    {
        float t = Mathf.InverseLerp(1000f, 100f, (float)ms);
        return Mathf.RoundToInt(Mathf.Clamp01(t) * 500f);
    }
}
