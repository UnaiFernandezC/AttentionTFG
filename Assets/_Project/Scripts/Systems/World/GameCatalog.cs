// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine;

/// <summary>
/// Catálogo del universo ATTENTIA: los 5 distritos (uno por función ejecutiva)
/// con su narrativa, y los 25 minijuegos con su nombre visible, su nombre EXACTO
/// de telemetría (para saber si un sector ya "revivió") y su escena base.
/// Un sector se considera REVIVIDO si el perfil completó ese minijuego alguna
/// vez (en cualquier dificultad). Nada nuevo se persiste: todo deriva de la
/// base de datos existente.
/// </summary>
public static class GameCatalog
{
    public class GameInfo
    {
        public readonly string display;    // nombre que ve el niño
        public readonly string telemetry;  // nombre con el que se registró en la BD
        public readonly string sceneBase;  // + "_Easy" / "_Medium" / "_Hard"
        public readonly string logo;       // ruta en Resources del logo del minijuego
        public GameInfo(string d, string t, string s, string l = null)
        { display = d; telemetry = t; sceneBase = s; logo = l; }

        /// <summary>Sprite del logo (Resources/logos_minijuegos). Puede ser null.</summary>
        public Sprite LoadLogo() =>
            string.IsNullOrEmpty(logo) ? null : Resources.Load<Sprite>(logo);
    }

    public class DistrictInfo
    {
        public string nombre;       // nombre narrativo ("LA GRAN BIBLIOTECA")
        public string lema;         // subtítulo corto
        public string sectorTag;    // nombre de cada sector ("PLANTA", "TORRE"...)
        public string[] guia;       // frase del robot guía: [0] apagado, [1] en marcha, [2] restaurada
        public GameInfo[] games;    // los 5 minijuegos del distrito
    }

    static readonly DistrictInfo[] D = new DistrictInfo[5];

    static GameCatalog()
    {
        D[(int)MinigameCategory.Memory] = new DistrictInfo
        {
            nombre = "LA GRAN BIBLIOTECA",
            lema = "Distrito de la Memoria — aquí vivían todos los recuerdos de Attentia",
            sectorTag = "PLANTA",
            guia = new[]
            {
                "La Biblioteca ha olvidado sus propios libros... ¿me ayudas a recordar?",
                "¡Las estanterías vuelven a llenarse! Cada planta que revives lo cambia todo.",
                "¡La Fuente de la Memoria brilla de nuevo! Gracias, Reconstructor."
            },
            games = new[]
            {
                new GameInfo("Combina colores",     "Parejas de Colores",   "Memory_ColorMatch",    "logos_minijuegos/Memoria/ColorMatch"),
                new GameInfo("Descubre el patrón",  "Repite el dibujo",     "Memory_PatternRecall", "logos_minijuegos/Memoria/PatternRecall"),
                new GameInfo("¿Qué ha cambiado?",   "Cambios sutiles",      "Memory_FindChange",    "logos_minijuegos/Memoria/FindChange"),
                new GameInfo("Simón dice",          "Simón Dice",           "Memory_SimonSays",     "logos_minijuegos/Memoria/SimonSays"),
                new GameInfo("Desafío de palabras", "Palabras Fugaces",     "Memory_WordMemory",    "logos_minijuegos/Memoria/WordMemory")
            }
        };

        D[(int)MinigameCategory.ImpulseControl] = new DistrictInfo
        {
            nombre = "LA CENTRAL DE ENERGÍA",
            lema = "Distrito del Control de Impulsos — sus máquinas se activan solas",
            sectorTag = "MÓDULO",
            guia = new[]
            {
                "Las máquinas se encienden y apagan sin control... hay que sincronizarlas.",
                "¡La energía empieza a circular en orden! Cada módulo cuenta.",
                "¡La Central obedece de nuevo! Todo está bajo control."
            },
            games = new[]
            {
                new GameInfo("No sigas la mayoría",     "No sigas la mayoría",       "Impulse_DontFollowMajority", "logos_minijuegos/ControlImpulsos/DontFollowMajority"),
                new GameInfo("Respuesta inversa",       "Respuesta Inversa",         "Impulse_InverseResponse",    "logos_minijuegos/ControlImpulsos/InverseResponse"),
                new GameInfo("¡No pulses todavía!",     "No pulses todavia",         "Impulse_DontPressYet",       "logos_minijuegos/ControlImpulsos/DontPressYet"),
                new GameInfo("Cuenta atrás silenciosa", "Cuenta Atrás Silenciosa",   "Impulse_SilentCountdown",    "logos_minijuegos/ControlImpulsos/SilentCountdown"),
                new GameInfo("Stop & Go",               "Stop & Go",                 "Impulse_StopAndGo",          "logos_minijuegos/ControlImpulsos/StopAndGo")
            }
        };

        D[(int)MinigameCategory.EmotionalManagement] = new DistrictInfo
        {
            nombre = "LOS JARDINES DE LA CALMA",
            lema = "Distrito del Control Emocional — el lugar más tranquilo de Attentia",
            sectorTag = "JARDÍN",
            guia = new[]
            {
                "Los jardines están marchitos y el cielo no deja de cambiar...",
                "¡Mira! Las flores empiezan a abrirse otra vez.",
                "¡La armonía volvió a los Jardines! ¿Oyes las cascadas?"
            },
            games = new[]
            {
                new GameInfo("Balance perfecto",      "Manten el equilibrio",  "Emotional_Balance",               "logos_minijuegos/ControlEmocional/EmotionalBalance"),
                new GameInfo("Rescate emocional",     "Rescate emocional",     "Emotional_Consequences",          "logos_minijuegos/ControlEmocional/EmotionalConsequences"),
                new GameInfo("Vuelve a la calma",     "Vuelve a la calma",     "Emotional_ProgressiveRegulation", "logos_minijuegos/ControlEmocional/ProgressiveRegulation"),
                new GameInfo("Mantén el control",     "Atraccion Emocional",   "Emotional_AttractionControl",     "logos_minijuegos/ControlEmocional/AttractionControl"),
                new GameInfo("Detective de emociones","Detective de emociones","Emotional_AventuraEmocional",     "logos_minijuegos/ControlEmocional/AventuraEmocional")
            }
        };

        D[(int)MinigameCategory.Attention] = new DistrictInfo
        {
            nombre = "LAS TORRES DE OBSERVACIÓN",
            lema = "Distrito de la Atención — radares y drones vigilaban el planeta",
            sectorTag = "TORRE",
            guia = new[]
            {
                "Los radares no detectan nada y los drones olvidaron sus rutas...",
                "¡Un radar acaba de girar! Vamos a despertar los demás.",
                "¡Las Torres vuelven a ver todo el planeta!"
            },
            games = new[]
            {
                new GameInfo("Atrápalo",       "Seguimiento de objeto", "Attention_ObjectTracking", "logos_minijuegos/Atencion/ObjectTracking"),
                new GameInfo("Camino láser",   "Camino Laser",          "Attention_LaserPath",      "logos_minijuegos/Atencion/CaminoLaser"),
                new GameInfo("Algo no cuadra", "Algo no cuadra",        "Attention_AlgoNoCuadra",   "logos_minijuegos/Atencion/AlgoNoCuadra"),
                new GameInfo("Reacción turbo", "Reaccion rapida",       "Attention_QuickReaction",  "logos_minijuegos/Atencion/QuickReaction"),
                new GameInfo("Cambio loco",    "Cambio de regla",       "Attention_RuleSwitch",     "logos_minijuegos/Atencion/RuleSwitch")
            }
        };

        D[(int)MinigameCategory.Planning] = new DistrictInfo
        {
            nombre = "LA GRAN FÁBRICA",
            lema = "Distrito de la Planificación — puentes, trenes y talleres a medio hacer",
            sectorTag = "TALLER",
            guia = new[]
            {
                "Puentes a medias, trenes parados... aquí nada llega a terminarse.",
                "¡La Fábrica retoma su ritmo! Los primeros trenes ya circulan.",
                "¡Todo funciona en perfecta coordinación! Eres un gran Reconstructor."
            },
            games = new[]
            {
                new GameInfo("El tren de Attentia", "El tren de Attentia",   "Planning_ActionSequence",     "logos_minijuegos/Planificacion/ActionSequence"),
                new GameInfo("Sigue la ruta",       "Memoria de Ruta",       "Planning_PathMemory",         "logos_minijuegos/Planificacion/PathMemory"),
                new GameInfo("Ruta óptima",         "Ruta optima",           "Planning_OptimalPath",        "logos_minijuegos/Planificacion/OptimalPath"),
                new GameInfo("El orden perdido",    "Orden correcto",        "Planning_OrdenCorrecto",      "logos_minijuegos/Planificacion/OrdenCorrecto"),
                new GameInfo("Torres de energía",   "Torres de energía",     "Planning_ResourceManagement", "logos_minijuegos/Planificacion/ResourceManagement")
            }
        };
    }

    public static DistrictInfo Get(int cat) => D[Mathf.Clamp(cat, 0, 4)];

    /// <summary>Color de la categoría (mismo sistema de color de toda la app).</summary>
    public static Color CatColor(int cat) =>
        IntroPanel.CategoryColor(MinigameResultData.CategoryDisplayName((MinigameCategory)cat));

    /// <summary>Perfil activo o null (invitado → todo aparece por revivir).</summary>
    public static string ActiveProfileId =>
        ProfileManager.Instance != null && ProfileManager.Instance.HasActiveProfile
            ? ProfileManager.Instance.ActiveProfile.id : null;

    /// <summary>Escena del minijuego en la dificultad ACTUAL del perfil.</summary>
    public static string SceneFor(GameInfo g)
    {
        var diff = GameManager.Instance != null
            ? GameManager.Instance.CurrentDifficulty : DifficultyLevel.Easy;
        string suf = diff == DifficultyLevel.Medium ? "_Medium"
                   : diff == DifficultyLevel.Hard   ? "_Hard" : "_Easy";
        return g.sceneBase + suf;
    }

    /// <summary>¿El perfil completó este minijuego alguna vez? (comparación sin
    /// tildes ni mayúsculas para tolerar variaciones del nombre registrado).</summary>
    public static bool IsCompleted(string profileId, GameInfo g)
    {
        var store = ProfileManager.Store;
        if (store == null || string.IsNullOrEmpty(profileId)) return false;
        string t = Norm(g.telemetry);
        return store.GetResults(profileId).Any(r => r.completado && Norm(r.minijuego) == t);
    }

    public static int CompletedCount(string profileId, int cat) =>
        Get(cat).games.Count(g => IsCompleted(profileId, g));

    /// <summary>¿El perfil completó este minijuego HOY? (para las misiones diarias).
    /// today en formato "dd/MM/yyyy" (DataUtils.TicksToLocalDate).</summary>
    public static bool CompletedToday(string profileId, GameInfo g, string today)
    {
        var store = ProfileManager.Store;
        if (store == null || string.IsNullOrEmpty(profileId)) return false;
        string t = Norm(g.telemetry);
        return store.GetResults(profileId).Any(r => r.completado && Norm(r.minijuego) == t
            && DataUtils.TicksToLocalDate(r.fechaUtcTicks) == today);
    }

    /// <summary>Robot guía según la dificultad activa del perfil.</summary>
    public static (string avatarId, string nombre, Color color) Guide()
    {
        var diff = GameManager.Instance != null
            ? GameManager.Instance.CurrentDifficulty : DifficultyLevel.Easy;
        switch (diff)
        {
            case DifficultyLevel.Medium: return ("axel",  "AXEL",  new Color(0.95f, 0.65f, 0.12f));
            case DifficultyLevel.Hard:   return ("titan", "TITAN", new Color(0.63f, 0.42f, 1f));
            default:                     return ("neo",   "NEO",   new Color(0.18f, 0.80f, 0.58f));
        }
    }

    /// <summary>minúsculas + sin tildes + sin espacios sobrantes.</summary>
    static string Norm(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        var formD = s.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(formD.Length);
        foreach (char ch in formD)
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                sb.Append(ch);
        return sb.ToString().Normalize(NormalizationForm.FormC).Trim().ToLowerInvariant();
    }
}
