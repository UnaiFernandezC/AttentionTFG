// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
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

        new EmotionalSituation(
            "Estas jugando a un juego de mesa con tus amigos y pierdes la partida.",
            new SituationOption[]
            {
                new SituationOption(
                    "Tirar las fichas y decir que el juego es tonto",
                    "Tus amigos ya no quieren jugar contigo. Enfadarse al perder estropea la diversion de todos.",
                    AnswerQuality.Negative),
                new SituationOption(
                    "Respirar hondo y felicitar a quien ha ganado",
                    "Tus amigos disfrutan jugando contigo y querran repetir. Perder tambien es parte de jugar.",
                    AnswerQuality.Positive),
                new SituationOption(
                    "Decir que hicieron trampas sin ser verdad",
                    "Acusar sin razon hace daño y crea peleas. Y la proxima vez nadie te creera.",
                    AnswerQuality.Negative),
                new SituationOption(
                    "Quedarte callado y dejar de jugar un rato",
                    "No empeoras nada, pero te pierdes la diversion. Contar como te sientes ayuda mas.",
                    AnswerQuality.Neutral),
            }),

        new EmotionalSituation(
            "Se te cae sin querer el juguete favorito de tu amigo y se rompe.",
            new SituationOption[]
            {
                new SituationOption(
                    "Decirle la verdad, pedir perdon y ayudar a arreglarlo",
                    "Tu amigo se pone triste, pero valora tu honestidad. Decir la verdad cuida la amistad.",
                    AnswerQuality.Positive),
                new SituationOption(
                    "Esconderlo y no decir nada",
                    "Cuando lo descubra se sentira doblemente mal: por el juguete y por el engaño.",
                    AnswerQuality.Negative),
                new SituationOption(
                    "Decir que lo rompio otro niño",
                    "Mentir mete en problemas a alguien inocente, y la verdad casi siempre se descubre.",
                    AnswerQuality.Negative),
                new SituationOption(
                    "Pedir a un adulto que se lo cuente por ti",
                    "El problema se soluciona, aunque contarlo tu mismo habria sido aun mas valiente.",
                    AnswerQuality.Neutral),
            }),

        new EmotionalSituation(
            "En el parque hay mucha cola para el tobogan y te toca esperar.",
            new SituationOption[]
            {
                new SituationOption(
                    "Colarte por delante de los demas",
                    "Los demas se enfadan contigo, y con razon: a nadie le gusta que se cuelen.",
                    AnswerQuality.Negative),
                new SituationOption(
                    "Esperar tu turno entretenido mirando o contando",
                    "El tiempo pasa volando y disfrutas del tobogan sin pelearte. La paciencia tiene premio.",
                    AnswerQuality.Positive),
                new SituationOption(
                    "Gritar y quejarte de que la cola va lenta",
                    "Gritar no hace que la cola avance y molesta a los que esperan igual que tu.",
                    AnswerQuality.Negative),
                new SituationOption(
                    "Irte a otro juego sin esperar",
                    "Evitas la espera, pero te quedas sin probar el tobogan que tanto querias.",
                    AnswerQuality.Neutral),
            }),

        new EmotionalSituation(
            "Tu hermano pequeño coge tu juguete favorito sin pedirte permiso.",
            new SituationOption[]
            {
                new SituationOption(
                    "Quitarselo de un tiron y gritarle",
                    "Tu hermano llora y los dos acabais regañados. La fuerza no arregla las cosas.",
                    AnswerQuality.Negative),
                new SituationOption(
                    "Pedirselo con calma y ofrecerle otro juguete",
                    "Te lo devuelve y acabais jugando juntos. Hablar con calma funciona mejor que gritar.",
                    AnswerQuality.Positive),
                new SituationOption(
                    "Ir a contarselo a papa o mama",
                    "Un adulto lo soluciona, aunque muchas veces puedes intentar resolverlo tu primero.",
                    AnswerQuality.Neutral),
                new SituationOption(
                    "Coger tu algo suyo para vengarte",
                    "Ahora los dos estais enfadados y el problema es el doble de grande.",
                    AnswerQuality.Negative),
            }),

        new EmotionalSituation(
            "Mañana actuas en la funcion del colegio y sientes muchos nervios.",
            new SituationOption[]
            {
                new SituationOption(
                    "Respirar despacio y ensayar una vez mas",
                    "Respirar te calma y ensayar te da seguridad. Los nervios se hacen pequeñitos.",
                    AnswerQuality.Positive),
                new SituationOption(
                    "Decir que estas malito para no actuar",
                    "Te libras hoy, pero el miedo crecera y la proxima vez sera mas dificil.",
                    AnswerQuality.Negative),
                new SituationOption(
                    "No pensar en ello y ponerte a ver la tele",
                    "Distraerse ayuda un rato, pero preparar lo que te da miedo ayuda mucho mas.",
                    AnswerQuality.Neutral),
                new SituationOption(
                    "Enfadarte y decir que el teatro es una tonteria",
                    "El enfado esconde tu miedo, pero no lo quita. Y te pierdes algo divertido.",
                    AnswerQuality.Negative),
            }),

        new EmotionalSituation(
            "Tus padres pasan mucho rato cuidando a tu prima pequeña y sientes celos.",
            new SituationOption[]
            {
                new SituationOption(
                    "Contarles como te sientes y pedir un rato juntos",
                    "Tus padres te abrazan y buscan tiempo para ti. Decir lo que sientes hace que te entiendan.",
                    AnswerQuality.Positive),
                new SituationOption(
                    "Portarte mal para llamar la atencion",
                    "Consigues atencion, pero es atencion de regañina y te hace sentir peor.",
                    AnswerQuality.Negative),
                new SituationOption(
                    "Tratar mal a tu prima",
                    "Ella no tiene la culpa. Hacer daño a otros nunca hace que los celos se vayan.",
                    AnswerQuality.Negative),
                new SituationOption(
                    "Ponerte a jugar solo sin decir nada",
                    "Te distraes un poco, pero los celos siguen ahi porque nadie sabe como te sientes.",
                    AnswerQuality.Neutral),
            }),

        new EmotionalSituation(
            "Llevas un buen rato con un ejercicio de mates y no te sale.",
            new SituationOption[]
            {
                new SituationOption(
                    "Romper la hoja y tirar el lapiz",
                    "El ejercicio sigue sin salir y ademas tienes que copiarlo de nuevo. La rabia no resuelve problemas.",
                    AnswerQuality.Negative),
                new SituationOption(
                    "Descansar cinco minutos, respirar y volver a intentarlo",
                    "Con la mente descansada lo ves mas claro y lo consigues. Parar un momento no es rendirse.",
                    AnswerQuality.Positive),
                new SituationOption(
                    "Dejarlo y pasar al siguiente ejercicio",
                    "Avanzas con lo demas, pero ese ejercicio te seguira esperando.",
                    AnswerQuality.Neutral),
                new SituationOption(
                    "Copiar el resultado de un compañero",
                    "Parece que terminas antes, pero no aprendes y el proximo sera aun mas dificil.",
                    AnswerQuality.Negative),
            }),

        new EmotionalSituation(
            "Ves a tu amigo sentado solo en el recreo con cara triste.",
            new SituationOption[]
            {
                new SituationOption(
                    "Acercarte, preguntarle que le pasa y escucharle",
                    "Tu amigo se siente acompañado y se anima. Escuchar es una forma de dar un abrazo.",
                    AnswerQuality.Positive),
                new SituationOption(
                    "Reirte de el por estar triste",
                    "Le haces sentir peor y puede dejar de confiar en ti. Las burlas duelen mucho.",
                    AnswerQuality.Negative),
                new SituationOption(
                    "Dejarle solo pensando que ya se le pasara",
                    "A veces se pasa solo, pero un amigo cerca hace que la tristeza pese menos.",
                    AnswerQuality.Neutral),
                new SituationOption(
                    "Avisar a la profesora de que esta triste",
                    "Buscar ayuda esta bien, aunque tu compañia tambien es una gran ayuda.",
                    AnswerQuality.Neutral),
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
