// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using UnityEngine;

/// <summary>
/// Las historias de los personajes de Attentia (avatares del selector).
/// Cada avatar tiene un nombre propio y un cuento corto pensado para
/// fascinar: misterios, récords y secretos del planeta que invitan a jugar.
/// </summary>
public static class CharacterLore
{
    /// <summary>Nombre propio del personaje (el id es el del avatar).</summary>
    public static string Nombre(string avatarId)
    {
        switch (avatarId)
        {
            case "neo":      return "NEO";
            case "axel":     return "AXEL";
            case "titan":    return "TITAN";
            case "bombilla": return "LUMI";
            case "globo":    return "VELA";
            case "atencion": return "IRIS";
            case "ager":     return "AGER";
            case "dili":     return "DILI";
            case "jun":      return "JUN";
            case "marxa":    return "MARXA";
            case "orbo":     return "ORBO";
            case "oti":      return "OTI";
            default:         return avatarId != null ? avatarId.ToUpper() : "?";
        }
    }

    /// <summary>Título corto que acompaña al nombre en el modal.</summary>
    public static string Titulo(string avatarId)
    {
        switch (avatarId)
        {
            case "neo":      return "El guardián más joven";
            case "axel":     return "El guardián valiente";
            case "titan":    return "El guardián más sabio";
            case "bombilla": return "La primera luz";
            case "globo":    return "El globo mensajero";
            case "atencion": return "La lupa curiosa";
            case "ager":     return "El científico de Attentia";
            case "dili":     return "La cantante de las calles";
            case "jun":      return "El saltarín más feliz";
            case "marxa":    return "El más rápido del planeta";
            case "orbo":     return "El guardián de los cristales";
            case "oti":      return "La más generosa de Attentia";
            default:         return "Habitante de Attentia";
        }
    }

    /// <summary>Historia corta del personaje (para el modal del selector).</summary>
    public static string Historia(string avatarId)
    {
        switch (avatarId)
        {
            case "neo": return
                "NEO nació de una chispa de la Fuente de la Memoria, y por eso quiere aprenderlo TODO: " +
                "los caminos, los cuentos, los nombres de las estrellas.\n\n" +
                "Cuando la Tormenta del Caos apagó las Fuentes, NEO olvidó casi todos sus recuerdos... " +
                "menos uno: que los niños de la Tierra pueden devolver la energía al planeta.\n\n" +
                "Por eso te esperaba. Dice que jugar contigo es su forma favorita de recordar.";

            case "axel": return
                "AXEL es el guardián que nunca, jamás, se rinde. Una vez cruzó los tres desiertos de " +
                "Attentia saltando a la pata coja... ¡solo porque alguien le dijo que era imposible!\n\n" +
                "La Tormenta le robó su energía, pero no sus ganas: cada desafío que superas junto a él " +
                "le devuelve un poquito de su fuerza.\n\n" +
                "Su frase favorita: «Si te sale mal, ¡es que estás a punto de aprenderlo!»";

            case "titan": return
                "TITAN es tan antiguo que vio nacer las cinco Fuentes Cognitivas. Ha protegido Attentia " +
                "durante miles de años y conoce secretos que nadie más recuerda.\n\n" +
                "Cuando llegó la Tormenta, TITAN gastó casi toda su sabiduría en salvar a NEO y AXEL. " +
                "Ahora habla despacio y piensa mucho antes de moverse...\n\n" +
                "Pero dicen que cuando un niño consigue tres estrellas, a TITAN le brillan los ojos como antes.";

            case "bombilla": return
                "Cuando la Tormenta del Caos apagó el planeta entero, hubo una noche completamente oscura. " +
                "Y entonces, en medio del silencio... ¡clic! Se encendió LUMI.\n\n" +
                "Nadie sabe por qué fue la primera luz en volver. Ella dice que fue porque alguien, " +
                "en algún lugar, tuvo una GRAN idea.\n\n" +
                "Desde entonces LUMI persigue ideas brillantes por todo el planeta. Cuando tú aciertas, ella brilla más.";

            case "globo": return
                "VELA es el único globo de Attentia que vuela sin viento: se impulsa con la alegría " +
                "de los que viajan dentro.\n\n" +
                "Durante la Tormenta, VELA rescató a cientos de robots pequeños llevándolos por el cielo, " +
                "esquivando rayos morados. ¡Sin perder ni un pasajero!\n\n" +
                "Ahora lleva mensajes entre los cinco distritos. Si un día ves pasar una sombra de colores " +
                "por el cielo del planeta... salúdala.";

            case "atencion": return
                "IRIS es una lupa curiosa que trabajaba en las Torres de Observación encontrando lo que " +
                "nadie más veía: una tuerca perdida, una estrella nueva, un dron dormido.\n\n" +
                "Cuando la Tormenta escondió las cosas importantes del planeta, IRIS fue la única que " +
                "no se asustó: «Todo lo perdido puede encontrarse... si miras con atención».\n\n" +
                "Dicen que si juegas con ella, se te contagia su superpoder: ver lo que otros no ven.";

            case "ager": return
                "AGER es el gran científico de Attentia. En su laboratorio hay burbujas que flotan, " +
                "pociones que cambian de color y un telescopio ENORME apuntando a las estrellas.\n\n" +
                "Cuando la Tormenta del Caos apagó las Fuentes, todos se rindieron... menos él. " +
                "Investigó noche y día hasta descubrir algo increíble: en un pequeño planeta azul " +
                "llamado Tierra vivían niños capaces de reentrenar la energía perdida.\n\n" +
                "Así que, en realidad... ¡AGER fue quien te descubrió a TI!";

            case "dili": return
                "DILI va cantando por las calles de Attentia desde que amanece. Su voz es tan " +
                "especial que las farolas se encienden solas para escucharla, y los robots se " +
                "asoman a las ventanas cuando pasa.\n\n" +
                "Cuando la Tormenta apagó el planeta, las calles se quedaron en silencio... pero " +
                "DILI no dejó de cantar ni un solo día. Dice que las calles no están apagadas: " +
                "solo están esperando su canción favorita.\n\n" +
                "Cada vez que aciertas, su canción suena un poquito más fuerte. ¿La oyes?";

            case "jun": return
                "¡BOING! ¡BOING! Ese es JUN, que va botando por el pueblo desde por la mañana. " +
                "Bota tan alto que saluda a los drones, y es tan feliz que su sonrisa se contagia " +
                "a todo el que lo mira.\n\n" +
                "Cuando llegó la Tormenta y todos los robots se pusieron tristes, JUN siguió " +
                "botando entre las ruinas. Dicen que fue su risa la que evitó que el pueblo " +
                "se durmiera del todo.\n\n" +
                "Su secreto: cada bote deja una lucecita en el suelo. Por eso el pueblo nunca quedó a oscuras.";

            case "marxa": return
                "MARXA es el más rápido de todo el planeta. Tan rápido que casi nadie lo ha visto " +
                "entero: solo su estela de colores. En la Gran Carrera de Attentia dio TRES vueltas " +
                "al planeta... ¡antes de que los demás cruzaran la primera curva!\n\n" +
                "Desde la Tormenta usa su velocidad para algo más importante: llevar chispas de " +
                "energía de un distrito a otro antes de que se enfríen.\n\n" +
                "Dice que solo correrá contra alguien que consiga tres estrellas. ¿Aceptas el reto?";

            case "orbo": return
                "ORBO es el guardián de los cristales de Attentia: las gemas gigantes que guardan " +
                "la energía de las cinco Fuentes. Se sabe el nombre de cada cristal, y les da las " +
                "buenas noches uno por uno.\n\n" +
                "Cuando la Tormenta los apagó todos, ORBO hizo una promesa: no descansar hasta " +
                "verlos brillar de nuevo, todos a la vez.\n\n" +
                "Cada partida que superas enciende un cristal en algún lugar del planeta. ORBO " +
                "los cuenta cada noche... y sonríe.";

            case "oti": return
                "OTI es la robot más querida de todo Attentia, y no es por casualidad: comparte " +
                "TODO. Su merienda, sus juguetes, sus mejores ideas... ¡hasta su propia energía!\n\n" +
                "Cuentan que durante la Tormenta dio su última chispa para encender la lamparita " +
                "de un robot bebé que tenía miedo a la oscuridad. Desde entonces, cada mañana " +
                "aparecen regalitos en su puerta: es el planeta entero dándole las gracias.\n\n" +
                "Si la eliges, lo compartirá todo contigo. Hasta sus secretos.";

            default: return
                "Un habitante misterioso de Attentia. Nadie conoce todavía su historia... ¡quizá la escribas tú!";
        }
    }
}
