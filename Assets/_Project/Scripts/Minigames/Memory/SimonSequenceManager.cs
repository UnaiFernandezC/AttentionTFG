using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gestiona la secuencia de colores del juego Simón Dice.
/// Sin dependencias de Unity UI — lógica pura.
/// </summary>
public class SimonSequenceManager : MonoBehaviour
{
    // ── Constantes ────────────────────────────────────────────────────────────
    public const int COLOR_COUNT = 4;

    // ── Estado interno ────────────────────────────────────────────────────────
    private readonly List<int> _sequence = new List<int>();
    private int _playerIndex;

    // ── Propiedades públicas ──────────────────────────────────────────────────

    /// <summary>Número de ronda actual (= longitud de la secuencia).</summary>
    public int Round => _sequence.Count;

    /// <summary>True cuando el jugador ya ha introducido todos los pasos.</summary>
    public bool RoundComplete => _playerIndex >= _sequence.Count;

    /// <summary>Color que el jugador debe pulsar a continuación. -1 si no aplica.</summary>
    public int ExpectedColor => RoundComplete ? -1 : _sequence[_playerIndex];

    /// <summary>Índice actual del jugador dentro de la secuencia.</summary>
    public int PlayerProgress => _playerIndex;

    // ── API pública ───────────────────────────────────────────────────────────

    /// <summary>Reinicia completamente la secuencia.</summary>
    public void ResetSequence()
    {
        _sequence.Clear();
        _playerIndex = 0;
    }

    /// <summary>Añade un nuevo color aleatorio al final de la secuencia y resetea el índice del jugador.</summary>
    public void AddStep()
    {
        _sequence.Add(Random.Range(0, COLOR_COUNT));
        _playerIndex = 0;
    }

    /// <summary>
    /// Registra la pulsación del jugador.
    /// </summary>
    /// <param name="colorIndex">Color pulsado (0-3).</param>
    /// <param name="roundComplete">True si el jugador completó toda la ronda correctamente.</param>
    /// <returns>True si el color era correcto.</returns>
    public bool Submit(int colorIndex, out bool roundComplete)
    {
        roundComplete = false;

        if (RoundComplete || _sequence.Count == 0)
            return false;

        bool correct = _sequence[_playerIndex] == colorIndex;

        if (correct)
        {
            _playerIndex++;
            roundComplete = RoundComplete;
        }

        return correct;
    }

    /// <summary>Devuelve el color en la posición dada de la secuencia.</summary>
    public int GetStep(int index)
    {
        if (index < 0 || index >= _sequence.Count)
            return -1;
        return _sequence[index];
    }

    /// <summary>Copia de solo lectura de la secuencia completa (útil para debug).</summary>
    public IReadOnlyList<int> GetSequence() => _sequence.AsReadOnly();
}
