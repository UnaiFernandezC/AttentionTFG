using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gestiona el input del jugador durante la fase de reproducción.
/// Compara cada click con la ruta esperada y dispara los eventos correspondientes.
///
/// Flujo:
///   1. Init(route) — carga la ruta a seguir
///   2. SetActive(true) — activa la escucha
///   3. HandleCellClick(pos) — llamado desde GridManager.CellClicked
///      → OnCorrectStep si la celda coincide con el siguiente paso esperado
///      → OnWrongStep   si la celda es incorrecta (desactiva input)
///      → OnRouteComplete si se completó la ruta entera
/// </summary>
public class PathMemoryPlayerInput : MonoBehaviour
{
    List<Vector2Int> _route;
    int              _nextIndex;  // índice del próximo paso esperado en _route (empieza en 1)
    bool             _active;

    // ── Eventos ──────────────────────────────────────────────────

    /// <summary>Paso correcto: posición clickada + índice en la ruta (0-based).</summary>
    public event Action<Vector2Int, int> OnCorrectStep;

    /// <summary>Paso incorrecto: posición clickada.</summary>
    public event Action<Vector2Int>      OnWrongStep;

    /// <summary>El jugador completó la ruta entera correctamente.</summary>
    public event Action                  OnRouteComplete;

    // ── Estado ───────────────────────────────────────────────────

    public int TotalSteps     => _route != null ? _route.Count - 1 : 0;
    public int CompletedSteps => Mathf.Max(0, _nextIndex - 1);

    // ── Inicialización ───────────────────────────────────────────

    public void Init(List<Vector2Int> route)
    {
        _route     = route;
        _nextIndex = 1;     // el start (route[0]) ya está dado; el jugador empieza desde route[1]
        _active    = false;
    }

    public void SetActive(bool active) => _active = active;

    // ── Procesamiento de clicks ──────────────────────────────────

    /// <summary>
    /// Llamado por el GameManager cuando el jugador hace click en una casilla.
    /// </summary>
    public void HandleCellClick(Vector2Int pos)
    {
        if (!_active || _route == null || _nextIndex >= _route.Count) return;

        if (pos == _route[_nextIndex])
        {
            int idx = _nextIndex;
            _nextIndex++;
            OnCorrectStep?.Invoke(pos, idx);

            if (_nextIndex >= _route.Count)
            {
                _active = false;
                OnRouteComplete?.Invoke();
            }
        }
        else
        {
            _active = false;
            OnWrongStep?.Invoke(pos);
        }
    }

    /// <summary>Reinicia el estado del input para jugar de nuevo.</summary>
    public void Reset()
    {
        _nextIndex = 1;
        _active    = false;
    }
}
