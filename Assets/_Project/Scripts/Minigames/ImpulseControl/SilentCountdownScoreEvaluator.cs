using UnityEngine;

/// <summary>
/// Evalúa la precisión del jugador comparando el tiempo objetivo con el real.
///
/// Márgenes configurables por dificultad:
///   Perfecto  → |diff| ≤ perfectMargin  → 100 pts, verde
///   Bien      → |diff| ≤ goodMargin     →  60 pts, amarillo
///   Fallo     → |diff|  > goodMargin    →   0 pts, rojo
/// </summary>
public class SilentCountdownScoreEvaluator : MonoBehaviour
{
    // ------------------------------------------------------------------ //
    // Inspector (fácil por defecto)
    // ------------------------------------------------------------------ //
    [Header("Márgenes de precisión (segundos)")]
    public float perfectMargin = 0.40f;   // ±0.4 s → perfecto
    public float goodMargin    = 1.00f;   // ±1.0 s → bien

    // ------------------------------------------------------------------ //
    // Resultado de evaluación
    // ------------------------------------------------------------------ //
    public enum Rating { Perfect, Good, Miss }

    public struct EvalResult
    {
        public Rating Rating;
        public int    Points;
        public float  Difference;    // valor absoluto
        public float  SignedDiff;    // positivo = se pasó, negativo = fue corto
    }

    // ------------------------------------------------------------------ //
    // API pública
    // ------------------------------------------------------------------ //

    /// <summary>
    /// Evalúa la diferencia entre el tiempo objetivo y el real del jugador.
    /// </summary>
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
            // Interpolación: más cerca del perfecto → más puntos (60–99)
            float t = 1f - (abs - perfectMargin) / (goodMargin - perfectMargin);
            points  = Mathf.RoundToInt(Mathf.Lerp(60f, 99f, t));
            rating  = Rating.Good;
        }
        else
        {
            // Puntos parciales incluso en fallo (máximo 30)
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

    /// <summary>
    /// True si la evaluación es Perfecto o Bien (cuenta como ronda correcta).
    /// </summary>
    public bool IsCorrect(EvalResult result) =>
        result.Rating == Rating.Perfect || result.Rating == Rating.Good;
}
