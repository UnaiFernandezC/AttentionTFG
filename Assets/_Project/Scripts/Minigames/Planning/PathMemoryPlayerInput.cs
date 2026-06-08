using System;
using System.Collections.Generic;
using UnityEngine;

public class PathMemoryPlayerInput : MonoBehaviour
{
    List<Vector2Int> _route;
    int              _nextIndex;
    bool             _active;

    public event Action<Vector2Int, int> OnCorrectStep;

    public event Action<Vector2Int>      OnWrongStep;

    public event Action                  OnRouteComplete;

    public int TotalSteps     => _route != null ? _route.Count - 1 : 0;
    public int CompletedSteps => Mathf.Max(0, _nextIndex - 1);

    public void Init(List<Vector2Int> route)
    {
        _route     = route;
        _nextIndex = 1;
        _active    = false;
    }

    public void SetActive(bool active) => _active = active;

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

    public void Reset()
    {
        _nextIndex = 1;
        _active    = false;
    }
}
