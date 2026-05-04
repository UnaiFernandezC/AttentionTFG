using System.Collections.Generic;
using UnityEngine;

public enum AnswerQuality { Positive, Neutral, Negative }

public class SituationOption
{
    public string       text;
    public string       consequence;
    public AnswerQuality quality;

    public SituationOption(string text, string consequence, AnswerQuality quality)
    {
        this.text        = text;
        this.consequence = consequence;
        this.quality     = quality;
    }
}

public class EmotionalSituation
{
    public string            situation;
    public SituationOption[] options;

    public EmotionalSituation(string situation, SituationOption[] options)
    {
        this.situation = situation;
        this.options   = options;
    }
}

public class ConsequencesSituationManager : MonoBehaviour
{

    static readonly EmotionalSituation[] ALL_SITUATIONS = new EmotionalSituation[]
    {

        new EmotionalSituation(
            "Un compañero te empuja sin querer en el pasillo y no se disculpa.",
            new SituationOption[]
            {
                new SituationOption(
                    "Empujarle de vuelta con fuerza",
                    "El conflicto escala. Ambos acabais en problemas y la situacion empeora para los dos.",
                    AnswerQuality.Negative),
                new SituationOption(
                    "Decirle con calma que tenga mas cuidado",
                    "Tu compañero se disculpa y el problema se resuelve sin drama. Comunicarse funciona.",
                    AnswerQuality.Positive),
                new SituationOption(
                    "Ignorarlo y seguir andando",
                    "Evitas el conflicto inmediato, aunque el malestar puede quedarse dentro.",
                    AnswerQuality.Neutral),
                new SituationOption(
                    "Quejarte en voz alta para que todos lo oigan",
                    "Creas una escena innecesaria que incomoda a todos sin resolver nada.",
                    AnswerQuality.Negative),
            }),

        new EmotionalSituation(
            "Sacas una nota muy baja en un examen que preparaste durante dias.",
            new SituationOption[]
            {
                new SituationOption(
                    "Enfadarte con el profesor y discutir en clase",
                    "El profesor se pone a la defensiva. La relacion empeora y no mejora tu nota.",
                    AnswerQuality.Negative),
                new SituationOption(
                    "Pedir al profesor que te explique donde fallaste",
                    "Entiendes tus errores y puedes mejorar en el proximo examen. Actitud excelente.",
                    AnswerQuality.Positive),
                new SituationOption(
                    "Pensar que no sirves para estudiar y rendirte",
                    "Este pensamiento daña tu autoestima y hace que el esfuerzo disminuya con el tiempo.",
                    AnswerQuality.Negative),
                new SituationOption(
                    "Guardar la nota sin hacer nada al respecto",
                    "No empeoras la situacion, pero tampoco aprendes de los errores.",
                    AnswerQuality.Neutral),
            }),

        new EmotionalSituation(
            "Tu mejor amigo no te invita a su fiesta de cumpleaños y te enteras por otros.",
            new SituationOption[]
            {
                new SituationOption(
                    "Enfadarte y dejar de hablarle sin decirle nada",
                    "Pierdes un amigo por un malentendido sin aclarar. El silencio no resuelve nada.",
                    AnswerQuality.Negative),
                new SituationOption(
                    "Hablar con el con calma y preguntarle que paso",
                    "Puede haber sido un error. Aclarar las cosas directamente fortalece la amistad.",
                    AnswerQuality.Positive),
                new SituationOption(
                    "Contarselo a todos para que se pongan de tu lado",
                    "Creas drama en el grupo antes de haber hablado con tu amigo. Complica todo.",
                    AnswerQuality.Negative),
                new SituationOption(
                    "Hacerte el indiferente y esperar a que el se explique",
                    "Evitas el conflicto pero el problema sigue sin resolverse.",
                    AnswerQuality.Neutral),
            }),

        new EmotionalSituation(
            "Estas muy nervioso antes de hacer una presentacion en clase.",
            new SituationOption[]
            {
                new SituationOption(
                    "Negarte a salir y quedarte sentado",
                    "Evitar el miedo lo hace mas grande. La proxima vez sera igual o peor.",
                    AnswerQuality.Negative),
                new SituationOption(
                    "Respirar hondo, recordar que te preparaste y salir",
                    "Te calmas y lo haces mejor de lo esperado. Con practica, el nerviosismo disminuye.",
                    AnswerQuality.Positive),
                new SituationOption(
                    "Fingir que estas enfermo para librarte",
                    "Evitas el momento, pero la culpa y el miedo siguen ahi para la proxima vez.",
                    AnswerQuality.Negative),
                new SituationOption(
                    "Salir y hacerlo aunque estes muy nervioso",
                    "Superas el momento. Casi siempre el malestar es mayor en la imaginacion que en la realidad.",
                    AnswerQuality.Neutral),
            }),

        new EmotionalSituation(
            "Un familiar te critica duramente delante de otras personas.",
            new SituationOption[]
            {
                new SituationOption(
                    "Responderle con insultos en ese momento",
                    "La situacion escala y todos se incomodan. El conflicto empeora en publico.",
                    AnswerQuality.Negative),
                new SituationOption(
                    "Decirle en privado, con calma, como te ha hecho sentir",
                    "Expresar tus sentimientos con respeto genera comprension y mejora la relacion.",
                    AnswerQuality.Positive),
                new SituationOption(
                    "Aguantar en silencio aunque te duela mucho",
                    "Evitas la confrontacion, pero el malestar queda dentro y puede acumularse.",
                    AnswerQuality.Neutral),
                new SituationOption(
                    "Irte del lugar sin decir nada a nadie",
                    "Muestras que algo te molesto, pero el problema queda sin resolver.",
                    AnswerQuality.Neutral),
            }),

        new EmotionalSituation(
            "Tu equipo pierde un partido importante y estas muy frustrado.",
            new SituationOption[]
            {
                new SituationOption(
                    "Culpar a tus compañeros del resultado",
                    "El equipo se desanima y los conflictos internos empeoran el rendimiento futuro.",
                    AnswerQuality.Negative),
                new SituationOption(
                    "Reconocer el esfuerzo de todos y animaros para la proxima",
                    "El equipo mantiene la moral alta y aprende del error juntos. Eso es un buen lider.",
                    AnswerQuality.Positive),
                new SituationOption(
                    "No decir nada y marcharte solo",
                    "Evitas el conflicto pero el equipo no recibe apoyo en un momento dificil.",
                    AnswerQuality.Neutral),
                new SituationOption(
                    "Enfadarte tanto que decides abandonar el equipo",
                    "Tomar decisiones importantes cuando estas muy enfadado suele llevar al arrepentimiento.",
                    AnswerQuality.Negative),
            }),

        new EmotionalSituation(
            "Alguien en clase se burla de ti delante de todos tus compañeros.",
            new SituationOption[]
            {
                new SituationOption(
                    "Devolverle la burla con insultos",
                    "El intercambio escala y ambos quedais mal ante el grupo.",
                    AnswerQuality.Negative),
                new SituationOption(
                    "Responder con seguridad y no darle importancia",
                    "Mostrar seguridad ante las burlas les quita poder. El grupo lo respeta.",
                    AnswerQuality.Positive),
                new SituationOption(
                    "Ignorarle completamente",
                    "No alimentas el conflicto, aunque el comportamiento puede repetirse si no se pone limite.",
                    AnswerQuality.Neutral),
                new SituationOption(
                    "Pedir ayuda a un adulto de confianza si se repite",
                    "Buscar ayuda cuando la necesitas es una señal de madurez, no de debilidad.",
                    AnswerQuality.Positive),
            }),
    };

    List<EmotionalSituation> _activeSituations;

    public int Total => _activeSituations?.Count ?? 0;

    public void Initialize(int count)
    {
        var pool = new List<EmotionalSituation>(ALL_SITUATIONS);
        Shuffle(pool);
        _activeSituations = pool.GetRange(0, Mathf.Min(count, pool.Count));

        foreach (var sit in _activeSituations)
        {
            var opts = new List<SituationOption>(sit.options);
            Shuffle(opts);
            sit.options = opts.ToArray();
        }
    }

    public EmotionalSituation GetSituation(int index)
    {
        if (_activeSituations == null || index < 0 || index >= _activeSituations.Count)
            return null;
        return _activeSituations[index];
    }

    static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            T tmp = list[i]; list[i] = list[j]; list[j] = tmp;
        }
    }
}
