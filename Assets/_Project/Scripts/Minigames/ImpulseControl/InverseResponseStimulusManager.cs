using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Gestiona la secuencia de estimulos de "Respuesta Inversa".
///
/// MODELO DE ESTIMULO:
///   Cada estimulo tiene una ArrowDirection (Left/Right/Up/Down) y una GameRule
///   (Inverse = pulsa contraria, Same = pulsa igual).
///   La respuesta CORRECTA se calcula con RequiredResponse().
///
/// CALCULO DE DIRECCION CONTRARIA:
///   Left ↔ Right (eje horizontal)
///   Up   ↔ Down  (eje vertical)
///   Si la regla es "Same", la respuesta correcta ES la propia flecha.
///
/// CAMBIO DE REGLA:
///   La regla cambia cada [ruleChangeInterval] estimulos.
///   ruleChangeInterval = 999 → regla fija durante todo el juego (modo Easy).
///   La regla alterna: Inverse → Same → Inverse → …
///   Esto obliga al jugador a relajar la inhibicion aprendida y reactivarla,
///   el mecanismo cognitivo central del paradigma Go/No-Go invertido.
///
/// TEMPORIZADOR:
///   Cada estimulo dispone de [responseTime] segundos para que el jugador pulse.
///   Si no hay respuesta, se dispara OnTimeout (cuenta como error).
///   El tiempo transcurrido se expone en StimulusElapsed para la UI.
/// </summary>
public class InverseResponseStimulusManager : MonoBehaviour
{
    // ── Enum publicos ────────────────────────────────────────────────────
    public enum ArrowDirection { Left, Right, Up, Down }
    public enum GameRule       { Inverse, Same }

    // ── Config (asignada por GameManager) ────────────────────────────────
    [HideInInspector] public float responseTime       = 3.0f;  // segundos por estimulo
    [HideInInspector] public int   ruleChangeInterval = 999;   // estimulos entre cambios de regla

    // ── Eventos ───────────────────────────────────────────────────────────
    /// <summary>Nuevo estimulo listo: direccion + regla activa.</summary>
    public event Action<ArrowDirection, GameRule> OnStimulusShown;

    /// <summary>El jugador no respondio a tiempo.</summary>
    public event Action OnTimeout;

    // ── Estado publico ────────────────────────────────────────────────────
    public ArrowDirection CurrentArrow    { get; private set; }
    public GameRule       CurrentRule     { get; private set; } = GameRule.Inverse;
    public float          StimulusElapsed { get; private set; }
    public bool           IsWaitingInput  { get; private set; }

    /// <summary>Direccion que el jugador DEBE pulsar para acertar.</summary>
    public ArrowDirection RequiredResponse =>
        CurrentRule == GameRule.Same ? CurrentArrow : Opposite(CurrentArrow);

    // ── Privado ───────────────────────────────────────────────────────────
    Coroutine _stimulusCo;
    int       _stimulusCount; // cuantos estimulos se han mostrado desde el ultimo cambio de regla

    // ═════════════════════════════════════════════════════════════════════
    // API publica
    // ═════════════════════════════════════════════════════════════════════

    /// <summary>Lanza un nuevo estimulo. Cancela el anterior si estaba activo.</summary>
    public void ShowNext()
    {
        if (_stimulusCo != null) StopCoroutine(_stimulusCo);
        _stimulusCo = StartCoroutine(StimulusRoutine());
    }

    /// <summary>
    /// Registra que el jugador respondio (correcto o no).
    /// Detiene el temporizador del estimulo actual.
    /// </summary>
    public void RegisterResponse()
    {
        IsWaitingInput = false;
        if (_stimulusCo != null) { StopCoroutine(_stimulusCo); _stimulusCo = null; }
    }

    /// <summary>Detiene todo (llamado al finalizar el juego).</summary>
    public void StopAll()
    {
        if (_stimulusCo != null) { StopCoroutine(_stimulusCo); _stimulusCo = null; }
        IsWaitingInput = false;
    }

    // ── Tick (llamado desde GameManager.Update) ────────────────────────
    public void Tick()
    {
        if (IsWaitingInput)
            StimulusElapsed += Time.deltaTime;
    }

    // ═════════════════════════════════════════════════════════════════════
    // Rutina de estimulo
    // ═════════════════════════════════════════════════════════════════════

    IEnumerator StimulusRoutine()
    {
        // Comprobar si es momento de cambiar la regla
        if (_stimulusCount > 0 && _stimulusCount % ruleChangeInterval == 0)
            CurrentRule = (CurrentRule == GameRule.Inverse) ? GameRule.Same : GameRule.Inverse;

        // Generar direccion aleatoria
        CurrentArrow = (ArrowDirection)UnityEngine.Random.Range(0, 4);

        StimulusElapsed = 0f;
        IsWaitingInput  = true;
        _stimulusCount++;

        OnStimulusShown?.Invoke(CurrentArrow, CurrentRule);

        // Contar tiempo de respuesta
        while (StimulusElapsed < responseTime)
        {
            if (!IsWaitingInput) yield break; // respuesta registrada
            StimulusElapsed += Time.deltaTime;
            yield return null;
        }

        // Timeout
        IsWaitingInput = false;
        OnTimeout?.Invoke();
    }

    // ═════════════════════════════════════════════════════════════════════
    // Helpers estaticos
    // ═════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Calcula la direccion opuesta.
    /// Left ↔ Right | Up ↔ Down
    /// </summary>
    public static ArrowDirection Opposite(ArrowDirection d)
    {
        switch (d)
        {
            case ArrowDirection.Left:  return ArrowDirection.Right;
            case ArrowDirection.Right: return ArrowDirection.Left;
            case ArrowDirection.Up:    return ArrowDirection.Down;
            default:                   return ArrowDirection.Up;   // Down → Up
        }
    }

    /// <summary>Nombre legible de una direccion (para logs y UI).</summary>
    public static string DirName(ArrowDirection d)
    {
        switch (d)
        {
            case ArrowDirection.Left:  return "Izquierda";
            case ArrowDirection.Right: return "Derecha";
            case ArrowDirection.Up:    return "Arriba";
            default:                   return "Abajo";
        }
    }
}
