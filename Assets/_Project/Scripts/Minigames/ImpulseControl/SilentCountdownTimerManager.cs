// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Temporizador del "semaforo escondido": cuenta atras de startCount a 0.
/// Emite OnTick(numero) mientras el numero es visible, OnHidden cuando el
/// numero llega a hideAt (a partir de ahi el niño cuenta en silencio) y
/// OnZeroTimeout si se agota el margen despues del 0 sin pulsacion.
/// </summary>
public class SilentCountdownTimerManager : MonoBehaviour
{
    public event Action<int> OnTick;
    public event Action      OnHidden;
    public event Action      OnZeroTimeout;

    public float Elapsed    { get; private set; }
    public float TargetTime { get; private set; }
    public bool  IsHidden   { get; private set; }
    public bool  Running    { get; private set; }

    int   _startCount;
    int   _hideAt;
    float _graceAfterZero;
    Coroutine _co;

    public void StartCountdown(int startCount, int hideAt, float graceAfterZero)
    {
        Stop();
        _startCount     = startCount;
        _hideAt         = hideAt;
        _graceAfterZero = graceAfterZero;

        Elapsed    = 0f;
        TargetTime = startCount;
        IsHidden   = false;
        Running    = true;

        _co = StartCoroutine(Routine());
    }

    public void Stop()
    {
        if (_co != null) { StopCoroutine(_co); _co = null; }
        Running = false;
    }

    IEnumerator Routine()
    {
        int current = _startCount;
        OnTick?.Invoke(current);

        while (Elapsed < TargetTime + _graceAfterZero)
        {
            Elapsed += Time.deltaTime;

            int newNum = Mathf.Max(0, _startCount - Mathf.FloorToInt(Elapsed));
            if (newNum != current)
            {
                current = newNum;

                if (!IsHidden && current <= _hideAt)
                {
                    IsHidden = true;
                    OnHidden?.Invoke();
                }
                else if (!IsHidden)
                {
                    OnTick?.Invoke(current);
                }
            }
            yield return null;
        }

        Running = false;
        _co = null;
        OnZeroTimeout?.Invoke();
    }
}
