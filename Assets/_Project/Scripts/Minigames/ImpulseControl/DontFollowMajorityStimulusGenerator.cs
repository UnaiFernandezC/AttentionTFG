// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using UnityEngine;

/// <summary>
/// Genera el plan de rondas del Flanker: que rondas son incongruentes
/// (el pez central mira al contrario que la mayoria) con proporcion exacta
/// y orden barajado, y sortea la direccion del pez central.
/// </summary>
public class DontFollowMajorityStimulusGenerator : MonoBehaviour
{
    bool[] _incongruentPlan;

    public void BuildPlan(int rounds, float incongruentRatio)
    {
        _incongruentPlan = new bool[Mathf.Max(1, rounds)];

        int n = Mathf.Clamp(Mathf.RoundToInt(rounds * incongruentRatio), 0, rounds);
        for (int i = 0; i < n; i++) _incongruentPlan[i] = true;

        // Fisher-Yates
        for (int i = _incongruentPlan.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            bool tmp = _incongruentPlan[i];
            _incongruentPlan[i] = _incongruentPlan[j];
            _incongruentPlan[j] = tmp;
        }

        // La primera ronda siempre congruente (arranque amable)
        if (_incongruentPlan[0])
        {
            for (int i = 1; i < _incongruentPlan.Length; i++)
            {
                if (!_incongruentPlan[i])
                {
                    _incongruentPlan[i] = true;
                    _incongruentPlan[0] = false;
                    break;
                }
            }
        }
    }

    public bool IsIncongruent(int round)
    {
        return _incongruentPlan != null
            && round >= 0
            && round < _incongruentPlan.Length
            && _incongruentPlan[round];
    }

    public bool RandomRight() => Random.value < 0.5f;
}
