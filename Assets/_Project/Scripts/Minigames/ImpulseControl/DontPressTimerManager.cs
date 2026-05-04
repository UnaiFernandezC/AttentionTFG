using System;
using System.Collections;
using UnityEngine;

public class DontPressTimerManager : MonoBehaviour
{

    [HideInInspector] public float WaitMin      = 2.0f;
    [HideInInspector] public float WaitMax      = 5.0f;
    [HideInInspector] public float ActiveWindow = 2.5f;
    [HideInInspector] public int   FakeOutCount = 0;

    public event Action OnActivated;

    public event Action OnTimeout;

    public event Action OnFakeOut;

    public bool  IsActive       { get; private set; }

    public float ActiveElapsed  { get; private set; }

    Coroutine _roundCo;

    public void StartRound()
    {
        StopRound();
        IsActive      = false;
        ActiveElapsed = 0f;
        _roundCo      = StartCoroutine(RoundRoutine());
    }

    public void StopRound()
    {
        if (_roundCo != null) { StopCoroutine(_roundCo); _roundCo = null; }
        IsActive = false;
    }

    public bool RegisterCorrectPress()
    {
        if (!IsActive) return false;
        IsActive = false;
        StopRound();
        return true;
    }

    public void Tick()
    {
        if (IsActive)
            ActiveElapsed += Time.deltaTime;
    }

    IEnumerator RoundRoutine()
    {
        float totalWait = UnityEngine.Random.Range(WaitMin, WaitMax);

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
                yield return new WaitForSeconds(0.38f);
                totalWait += 0.38f;
            }

            while (elapsed < totalWait - 0.38f * FakeOutCount)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }
        else
        {

            yield return new WaitForSeconds(totalWait);
        }

        IsActive      = true;
        ActiveElapsed = 0f;
        OnActivated?.Invoke();

        while (ActiveElapsed < ActiveWindow)
        {
            if (!IsActive) yield break;
            yield return null;
        }

        if (IsActive)
        {
            IsActive = false;
            OnTimeout?.Invoke();
        }
    }
}
