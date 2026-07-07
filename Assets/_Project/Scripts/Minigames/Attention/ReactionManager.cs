// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using System;
using UnityEngine;

public class ReactionManager : MonoBehaviour
{

    [Header("Tiempo de espera antes del estímulo (segundos)")]
    public float waitMin = 2.5f;
    public float waitMax = 5.5f;

    public enum State { Idle, Waiting, Stimulus, Resolved }

    public State CurrentState   { get; private set; } = State.Idle;
    public long  LastReactionMs { get; private set; }
    public bool  WasTooEarly    { get; private set; }
    public bool  WasTimeout     { get; private set; }

    public float StimulusElapsed { get; private set; }

    public float ReactionTimeLimit { get; set; } = 3f;

    public event Action OnStimulusAppeared;
    public event Action OnReactionRegistered;

    float  _waitTarget;
    float  _waitElapsed;
    bool   _active;

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

    public void Cancel()
    {
        _active      = false;
        CurrentState = State.Idle;
    }

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

                WasTimeout   = true;
                WasTooEarly  = false;
                CurrentState = State.Resolved;
                _active      = false;
                OnReactionRegistered?.Invoke();
            }
        }
    }

    public static string EvaluateTime(long ms)
    {
        if (ms < 180)  return "¡Increíble!";
        if (ms < 280)  return "¡Excelente!";
        if (ms < 400)  return "¡Muy bien!";
        if (ms < 550)  return "Bien";
        if (ms < 750)  return "Puede mejorar";
        return "Lento";
    }

    public static int CalcRoundScore(long ms)
    {
        float t = Mathf.InverseLerp(1000f, 100f, (float)ms);
        return Mathf.RoundToInt(Mathf.Clamp01(t) * 500f);
    }
}
