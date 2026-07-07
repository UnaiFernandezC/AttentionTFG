// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using System;
using System.Collections;
using UnityEngine;

public class InverseResponseStimulusManager : MonoBehaviour
{

    public enum ArrowDirection { Left, Right, Up, Down }
    public enum GameRule       { Inverse, Same }

    [HideInInspector] public float responseTime       = 3.0f;
    [HideInInspector] public int   ruleChangeInterval = 999;

    public event Action<ArrowDirection, GameRule> OnStimulusShown;

    public event Action OnTimeout;

    public ArrowDirection CurrentArrow    { get; private set; }
    public GameRule       CurrentRule     { get; private set; } = GameRule.Inverse;
    public float          StimulusElapsed { get; private set; }
    public bool           IsWaitingInput  { get; private set; }

    public ArrowDirection RequiredResponse =>
        CurrentRule == GameRule.Same ? CurrentArrow : Opposite(CurrentArrow);

    Coroutine _stimulusCo;
    int       _stimulusCount;

    public void ShowNext()
    {
        if (_stimulusCo != null) StopCoroutine(_stimulusCo);
        _stimulusCo = StartCoroutine(StimulusRoutine());
    }

    public void RegisterResponse()
    {
        IsWaitingInput = false;
        if (_stimulusCo != null) { StopCoroutine(_stimulusCo); _stimulusCo = null; }
    }

    public void StopAll()
    {
        if (_stimulusCo != null) { StopCoroutine(_stimulusCo); _stimulusCo = null; }
        IsWaitingInput = false;
    }

    IEnumerator StimulusRoutine()
    {

        if (_stimulusCount > 0 && _stimulusCount % ruleChangeInterval == 0)
            CurrentRule = (CurrentRule == GameRule.Inverse) ? GameRule.Same : GameRule.Inverse;

        CurrentArrow = (ArrowDirection)UnityEngine.Random.Range(0, 4);

        StimulusElapsed = 0f;
        IsWaitingInput  = true;
        _stimulusCount++;

        OnStimulusShown?.Invoke(CurrentArrow, CurrentRule);

        while (StimulusElapsed < responseTime)
        {
            if (!IsWaitingInput) yield break;
            StimulusElapsed += Time.deltaTime;
            yield return null;
        }

        IsWaitingInput = false;
        OnTimeout?.Invoke();
    }

    public static ArrowDirection Opposite(ArrowDirection d)
    {
        switch (d)
        {
            case ArrowDirection.Left:  return ArrowDirection.Right;
            case ArrowDirection.Right: return ArrowDirection.Left;
            case ArrowDirection.Up:    return ArrowDirection.Down;
            default:                   return ArrowDirection.Up;
        }
    }

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
