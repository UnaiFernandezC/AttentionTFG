// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Estado del jugador durante la fase de reproduccion: se mueve libremente a
/// casillas ADYACENTES no bloqueadas desde el inicio hasta la meta. El manager
/// decide feedback, telemetria y fin de ronda a partir del resultado.
/// </summary>
public class PathMemoryPlayerInput : MonoBehaviour
{
    public enum MoveResult
    {
        Inactive,   // no es tu turno o click en la casilla actual
        Invalid,    // no adyacente o casilla bloqueada
        Moved,      // movimiento valido
        Goal        // movimiento valido que alcanza la meta
    }

    List<Vector2Int>    _route;
    HashSet<Vector2Int> _routeSet;
    HashSet<Vector2Int> _blocked;
    Vector2Int          _current;
    Vector2Int          _goal;
    bool                _active;
    int                 _moves;

    public Vector2Int Current => _current;
    public Vector2Int Goal    => _goal;
    public int        Moves   => _moves;

    public void Init(List<Vector2Int> route, IEnumerable<Vector2Int> blockedCells)
    {
        _route    = route;
        _routeSet = new HashSet<Vector2Int>(route);
        _blocked  = blockedCells != null
            ? new HashSet<Vector2Int>(blockedCells)
            : new HashSet<Vector2Int>();
        _current  = route[0];
        _goal     = route[route.Count - 1];
        _moves    = 0;
        _active   = false;
    }

    public void SetActive(bool active) => _active = active;

    public bool IsOnRoute(Vector2Int pos) => _routeSet != null && _routeSet.Contains(pos);
    public bool IsBlocked(Vector2Int pos) => _blocked  != null && _blocked.Contains(pos);

    public MoveResult TryMove(Vector2Int pos)
    {
        if (!_active || _route == null) return MoveResult.Inactive;
        if (pos == _current)            return MoveResult.Inactive;

        bool adjacent = Mathf.Abs(pos.x - _current.x) + Mathf.Abs(pos.y - _current.y) == 1;
        if (!adjacent || _blocked.Contains(pos)) return MoveResult.Invalid;

        _moves++;
        _current = pos;

        if (pos == _goal)
        {
            _active = false;
            return MoveResult.Goal;
        }
        return MoveResult.Moved;
    }
}
