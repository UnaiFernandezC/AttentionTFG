using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Gestiona el tiempo aleatorio de cada ronda en "No pulses todavia".
///
/// FLUJO DE UNA RONDA:
///   1. StartRound()
///   2. Espera un tiempo ALEATORIO entre WaitMin y WaitMax segundos.
///      (Opcional) Durante la espera, puede lanzar señales FALSAS (FakeOutCount)
///      para dificultar el control de impulsos en modos mas duros.
///   3. Dispara OnActivated → el boton se pone verde.
///   4. El jugador tiene ActiveWindow segundos para pulsar.
///   5a. Jugador pulsa antes de OnActivated → el GameManager llama HandleEarlyPress()
///       y la ronda termina inmediatamente como fallo.
///   5b. Jugador pulsa tras OnActivated → el GameManager llama HandleCorrectPress()
///       y la ronda termina como exito.
///   5c. Nadie pulsa en ActiveWindow → dispara OnTimeout → fallo por tiempo.
///
/// TIEMPO ALEATORIO:
///   Se genera con Random.Range(WaitMin, WaitMax).
///   Al variar el rango (no solo el valor) se evita que el jugador aprenda
///   un patron: en Easy el rango es [2.0, 5.0] → diferencia de 3s; en Hard
///   el rango es [0.8, 6.0] → diferencia de 5.2s, mucho mas impredecible.
///
/// SEÑALES FALSAS (FakeOutCount > 0):
///   El temporizador lanza OnFakeOut a intervalos dentro del periodo de espera.
///   El boton se pone amarillo 0.4s y vuelve a rojo → el jugador NO debe pulsar.
///   Esto entrena la inhibicion de respuesta ante señales engañosas.
/// </summary>
public class DontPressTimerManager : MonoBehaviour
{
    // ── Config (asignada por GameManager segun dificultad) ────────────────
    [HideInInspector] public float WaitMin      = 2.0f;  // espera minima (s)
    [HideInInspector] public float WaitMax      = 5.0f;  // espera maxima (s)
    [HideInInspector] public float ActiveWindow = 2.5f;  // ventana de pulsacion (s)
    [HideInInspector] public int   FakeOutCount = 0;     // señales falsas (0 = desactivado)

    // ── Eventos ───────────────────────────────────────────────────────────
    /// <summary>Boton activado: el jugador DEBE pulsar ahora.</summary>
    public event Action OnActivated;

    /// <summary>El jugador no pulso a tiempo.</summary>
    public event Action OnTimeout;

    /// <summary>Señal falsa (flash amarillo): el jugador NO debe pulsar.</summary>
    public event Action OnFakeOut;

    // ── Estado publico ────────────────────────────────────────────────────
    /// <summary>True mientras el boton esta activo (verde) y esperando pulsacion.</summary>
    public bool  IsActive       { get; private set; }

    /// <summary>Tiempo transcurrido desde que el boton se activo.</summary>
    public float ActiveElapsed  { get; private set; }

    // ── Privado ───────────────────────────────────────────────────────────
    Coroutine _roundCo;

    // ═════════════════════════════════════════════════════════════════════
    // API publica
    // ═════════════════════════════════════════════════════════════════════

    /// <summary>Inicia una nueva ronda. Detiene cualquier ronda anterior.</summary>
    public void StartRound()
    {
        StopRound();
        IsActive      = false;
        ActiveElapsed = 0f;
        _roundCo      = StartCoroutine(RoundRoutine());
    }

    /// <summary>Detiene la ronda en curso (llamado tras una pulsacion o fin de juego).</summary>
    public void StopRound()
    {
        if (_roundCo != null) { StopCoroutine(_roundCo); _roundCo = null; }
        IsActive = false;
    }

    /// <summary>
    /// Registra pulsacion correcta (durante ventana activa).
    /// Desactiva el temporizador y devuelve true si la pulsacion era valida.
    /// </summary>
    public bool RegisterCorrectPress()
    {
        if (!IsActive) return false;
        IsActive = false;
        StopRound();
        return true;
    }

    // ─── Tick (llamado desde GameManager.Update) ──────────────────────────
    public void Tick()
    {
        if (IsActive)
            ActiveElapsed += Time.deltaTime;
    }

    // ═════════════════════════════════════════════════════════════════════
    // Corutina principal
    // ═════════════════════════════════════════════════════════════════════

    IEnumerator RoundRoutine()
    {
        float totalWait = UnityEngine.Random.Range(WaitMin, WaitMax);

        // ── Señales falsas (esparcidas uniformemente durante la espera) ───
        if (FakeOutCount > 0)
        {
            float interval = totalWait / (FakeOutCount + 1f);
            float elapsed  = 0f;

            for (int i = 0; i < FakeOutCount; i++)
            {
                float target = interval * (i + 1);
                while (elapsed < target)
                {
                    elapsed += Time.deltaTime;
                    yield return null;
                }
                OnFakeOut?.Invoke();
                yield return new WaitForSeconds(0.38f); // duracion del flash falso
                totalWait += 0.38f; // compensar el tiempo gastado en el flash
            }

            // Esperar el resto del tiempo
            while (elapsed < totalWait - 0.38f * FakeOutCount)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }
        else
        {
            // Sin señales falsas: espera directa
            yield return new WaitForSeconds(totalWait);
        }

        // ── Activar boton (verde) ─────────────────────────────────────────
        IsActive      = true;
        ActiveElapsed = 0f;
        OnActivated?.Invoke();

        // ── Ventana de pulsacion ──────────────────────────────────────────
        while (ActiveElapsed < ActiveWindow)
        {
            if (!IsActive) yield break; // pulsacion registrada: salir
            yield return null;
        }

        // ── Timeout: el jugador no pulso ──────────────────────────────────
        if (IsActive)
        {
            IsActive = false;
            OnTimeout?.Invoke();
        }
    }
}
