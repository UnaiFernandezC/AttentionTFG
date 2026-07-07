// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using UnityEngine;

/// <summary>
/// Evalua la precision de una pulsacion respecto al 0 de la cuenta atras.
/// La ventana de acierto la marca la dificultad (±0.8 / ±0.5 / ±0.35 s).
/// </summary>
public class SilentCountdownScoreEvaluator : MonoBehaviour
{
    public struct EvalResult
    {
        public bool   Acierto;
        public int    Points;
        public string Label;
    }

    /// <param name="deviationSec">Desvio respecto al 0 (positivo = tarde).</param>
    /// <param name="windowSec">Ventana de acierto (± segundos).</param>
    public EvalResult Evaluate(float deviationSec, float windowSec)
    {
        var r = new EvalResult();
        float d = Mathf.Abs(deviationSec);

        if (d <= windowSec * 0.4f)
        {
            r.Acierto = true;
            r.Points  = 200;
            r.Label   = "¡CLAVADO!";
        }
        else if (d <= windowSec)
        {
            r.Acierto = true;
            r.Points  = 120;
            r.Label   = "¡Muy cerca!";
        }
        else
        {
            r.Acierto = false;
            r.Points  = 0;
            r.Label   = deviationSec < 0f ? "Un poco pronto" : "Un poco tarde";
        }
        return r;
    }
}
