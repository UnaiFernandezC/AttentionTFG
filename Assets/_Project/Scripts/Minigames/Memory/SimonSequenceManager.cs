// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using System.Collections.Generic;
using UnityEngine;

public class SimonSequenceManager : MonoBehaviour
{

    private int _colorCount = 4;
    private readonly List<int> _sequence = new List<int>();
    private int _playerIndex;

    public int Round => _sequence.Count;

    public bool RoundComplete => _playerIndex >= _sequence.Count;

    public int ExpectedColor => RoundComplete ? -1 : _sequence[_playerIndex];

    public int PlayerProgress => _playerIndex;

    public void Initialize(int colorCount)
    {
        _colorCount = Mathf.Max(2, colorCount);
    }

    public void ResetSequence()
    {
        _sequence.Clear();
        _playerIndex = 0;
    }

    public void AddStep()
    {
        _sequence.Add(Random.Range(0, _colorCount));
        _playerIndex = 0;
    }

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

    public int GetStep(int index)
    {
        if (index < 0 || index >= _sequence.Count)
            return -1;
        return _sequence[index];
    }

    public IReadOnlyList<int> GetSequence() => _sequence.AsReadOnly();
}
