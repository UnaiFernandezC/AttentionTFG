// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using System.Collections;
using UnityEngine;

public class UITweenRunner : MonoBehaviour
{
    static UITweenRunner _instance;

    public static UITweenRunner Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("UITweenRunner");
                _instance = go.AddComponent<UITweenRunner>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }
}

public static class UITween
{
    public static Coroutine Run(IEnumerator routine)
    {
        return UITweenRunner.Instance.StartCoroutine(routine);
    }

    public static CanvasGroup EnsureGroup(GameObject go)
    {
        if (go == null) return null;
        var cg = go.GetComponent<CanvasGroup>();
        if (cg == null) cg = go.AddComponent<CanvasGroup>();
        return cg;
    }

    public static void FadeIn(GameObject go, float duration = 0.32f, float delay = 0f)
    {
        var cg = EnsureGroup(go);
        if (cg == null) return;
        cg.alpha = 0f;
        Run(FadeRoutine(cg, 0f, 1f, duration, delay, null));
    }

    public static void FadeOut(GameObject go, float duration = 0.28f, System.Action onDone = null)
    {
        var cg = EnsureGroup(go);
        if (cg == null) { onDone?.Invoke(); return; }
        Run(FadeRoutine(cg, cg.alpha, 0f, duration, 0f, onDone));
    }

    public static void PopIn(RectTransform rt, float duration = 0.42f, float fromScale = 0.82f, float delay = 0f)
    {
        if (rt == null) return;
        Run(PopRoutine(rt, fromScale, duration, delay));
    }

    public static void PulseOnce(RectTransform rt, float peak = 1.10f, float duration = 0.22f)
    {
        if (rt == null) return;
        Run(PulseRoutine(rt, peak, duration));
    }

    static IEnumerator FadeRoutine(CanvasGroup cg, float a, float b, float duration, float delay, System.Action onDone)
    {
        if (delay > 0f) yield return new WaitForSecondsRealtime(delay);
        float t = 0f;
        while (t < duration)
        {
            if (cg == null) yield break;
            t += Time.unscaledDeltaTime;
            // SmoothStep: arranque y frenada suaves (sin cortes lineales)
            cg.alpha = Mathf.Lerp(a, b, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duration)));
            yield return null;
        }
        if (cg != null) cg.alpha = b;
        onDone?.Invoke();
    }

    static IEnumerator PopRoutine(RectTransform rt, float fromScale, float duration, float delay)
    {
        Vector3 target = rt.localScale;
        if (target == Vector3.zero) target = Vector3.one;
        rt.localScale = target * fromScale;
        if (delay > 0f) yield return new WaitForSecondsRealtime(delay);
        float t = 0f;
        while (t < duration)
        {
            if (rt == null) yield break;
            t += Time.unscaledDeltaTime;
            float e = EaseOutBack(Mathf.Clamp01(t / duration));
            rt.localScale = Vector3.LerpUnclamped(target * fromScale, target, e);
            yield return null;
        }
        if (rt != null) rt.localScale = target;
    }

    static IEnumerator PulseRoutine(RectTransform rt, float peak, float duration)
    {
        Vector3 baseScale = rt.localScale;
        float t = 0f;
        while (t < duration)
        {
            if (rt == null) yield break;
            t += Time.unscaledDeltaTime;
            float p = t / duration;
            float s = 1f + (peak - 1f) * Mathf.Sin(p * Mathf.PI);
            rt.localScale = baseScale * s;
            yield return null;
        }
        if (rt != null) rt.localScale = baseScale;
    }

    static float EaseOutBack(float x)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }
}
