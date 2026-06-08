using UnityEngine;

public class SilentCountdownScoreEvaluator : MonoBehaviour
{

    [Header("Márgenes de precisión (segundos)")]
    public float perfectMargin = 0.40f;
    public float goodMargin    = 1.00f;

    public enum Rating { Perfect, Good, Miss }

    public struct EvalResult
    {
        public Rating Rating;
        public int    Points;
        public float  Difference;
        public float  SignedDiff;
    }

    public EvalResult Evaluate(float targetTime, float actualTime)
    {
        float signed = actualTime - targetTime;
        float abs    = Mathf.Abs(signed);

        Rating rating;
        int    points;

        if (abs <= perfectMargin)
        {
            rating = Rating.Perfect;
            points = 100;
        }
        else if (abs <= goodMargin)
        {

            float t = 1f - (abs - perfectMargin) / (goodMargin - perfectMargin);
            points  = Mathf.RoundToInt(Mathf.Lerp(60f, 99f, t));
            rating  = Rating.Good;
        }
        else
        {

            float t = Mathf.Clamp01(1f - (abs - goodMargin) / 3f);
            points  = Mathf.RoundToInt(Mathf.Lerp(0f, 30f, t));
            rating  = Rating.Miss;
        }

        return new EvalResult
        {
            Rating     = rating,
            Points     = points,
            Difference = abs,
            SignedDiff = signed
        };
    }

    public bool IsCorrect(EvalResult result) =>
        result.Rating == Rating.Perfect || result.Rating == Rating.Good;
}
