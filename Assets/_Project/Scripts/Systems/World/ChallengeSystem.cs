// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// SISTEMA DE RANGOS POR PUNTUACIÓN — cada sector (minijuego) tiene un RANGO 0-4:
/// 0 = en ruinas (nunca jugado), 1 = BRONCE, 2 = PLATA, 3 = ORO, 4 = DIAMANTE.
///
/// La DIFICULTAD del juego NO cambia. Para subir de rango hay que SUPERAR una diana
/// de puntos cada vez más alta (calculada por juego y dificultad como fracción de la
/// puntuación máxima teórica: PLATA 35%, ORO 60%, DIAMANTE 85%). Modelo MIXTO: además
/// de las dianas, hay un ATAJO por estrellas (2★ garantiza al menos PLATA, 3★ al menos
/// ORO), para que progresar nunca se atasque. El rango solo sube; nunca baja.
///
/// Todo se persiste en PlayerPrefs por perfil y juego (clave estable = sceneBase).
/// No toca la telemetría ni los informes.
/// </summary>
public static class ChallengeSystem
{
    public const int MAX_RANK = 4;

    // Fracciones de la puntuación máxima que marcan cada diana.
    const float FRAC_PLATA    = 0.35f;
    const float FRAC_ORO      = 0.60f;
    const float FRAC_DIAMANTE = 0.85f;

    // ================================================================ CLAVES

    static string Prof(string p) => string.IsNullOrEmpty(p) ? "guest" : p;
    static string KBest(string p, string sb)  => $"retobest_{Prof(p)}_{sb}";
    static string KStars(string p, string sb) => $"retostars_{Prof(p)}_{sb}";
    static string KDone(string p, string sb)  => $"retodone_{Prof(p)}_{sb}";
    static string KSeen(string p, string sb)  => $"reto_visto_{Prof(p)}_{sb}";

    // ================================================================ REGISTRO

    /// <summary>La llama MinigameBase al mostrar resultados de una partida GANADA:
    /// guarda la mejor puntuación y las mejores estrellas del sector actual.</summary>
    public static void RegistrarResultado(int score, int stars)
    {
        string sb = SceneBaseActual();
        if (sb == null) return;
        string p = GameCatalog.ActiveProfileId;

        int prevBest = PlayerPrefs.GetInt(KBest(p, sb), 0);
        if (score > prevBest) PlayerPrefs.SetInt(KBest(p, sb), score);

        int prevStars = PlayerPrefs.GetInt(KStars(p, sb), 0);
        if (stars > prevStars) PlayerPrefs.SetInt(KStars(p, sb), Mathf.Clamp(stars, 0, 3));

        PlayerPrefs.SetInt(KDone(p, sb), 1);
        PlayerPrefs.Save();
    }

    // ================================================================ RANGO

    public static int MejorPuntuacion(string p, string sb) => PlayerPrefs.GetInt(KBest(p, sb), 0);
    public static int MejoresEstrellas(string p, string sb) => PlayerPrefs.GetInt(KStars(p, sb), 0);

    /// <summary>Rango 0-4 del juego para el perfil. Combina la diana de puntos (vía la
    /// mejor puntuación) con el atajo por estrellas. MIGRACIÓN: si nunca se registró
    /// pero la telemetría dice que el perfil ya lo completó, cuenta como BRONCE.</summary>
    public static int Rank(string profileId, string sceneBase)
    {
        if (string.IsNullOrEmpty(sceneBase)) return 0;

        bool done = PlayerPrefs.GetInt(KDone(profileId, sceneBase), 0) == 1;
        if (!done && !string.IsNullOrEmpty(profileId))
        {
            var g = FindBySceneBase(sceneBase);
            if (g != null && GameCatalog.IsCompleted(profileId, g)) done = true;
        }
        if (!done) return 0;

        int best  = MejorPuntuacion(profileId, sceneBase);
        int stars = MejoresEstrellas(profileId, sceneBase);
        int r = RefMax(sceneBase);

        // Vía puntuación (dianas). Juego sin puntuación significativa (r<=0) → por estrellas.
        int scoreTier;
        if (r <= 0)
            scoreTier = 1;   // sin dianas: el rango lo decide el atajo por estrellas
        else
            scoreTier = best >= r * FRAC_DIAMANTE ? 4
                      : best >= r * FRAC_ORO      ? 3
                      : best >= r * FRAC_PLATA    ? 2
                      : 1;

        // Atajo por estrellas: 2★→PLATA, 3★→ORO (no da DIAMANTE por sí solo en juegos
        // con puntuación; en juegos sin puntuación, 3★ sí llega a DIAMANTE).
        int starTier = r <= 0
            ? (stars >= 3 ? 4 : stars >= 2 ? 3 : stars >= 1 ? 2 : 1)
            : (stars >= 3 ? 3 : stars >= 2 ? 2 : 1);

        return Mathf.Clamp(Mathf.Max(scoreTier, starTier), 1, MAX_RANK);
    }

    public static int RankEscenaActual()
    {
        string sb = SceneBaseActual();
        return sb == null ? 0 : Rank(GameCatalog.ActiveProfileId, sb);
    }

    /// <summary>Puntos que faltan para el SIGUIENTE rango por dianas y su nombre.
    /// Devuelve (faltan<=0, null) si ya está en DIAMANTE o el juego no puntúa.</summary>
    public static (int faltan, string siguiente) PuntosParaSiguiente(string profileId, string sceneBase)
    {
        int r = RefMax(sceneBase);
        if (r <= 0) return (0, null);
        int best = MejorPuntuacion(profileId, sceneBase);

        int plata = Mathf.RoundToInt(r * FRAC_PLATA);
        int oro   = Mathf.RoundToInt(r * FRAC_ORO);
        int dia   = Mathf.RoundToInt(r * FRAC_DIAMANTE);

        if (best < plata) return (plata - best, "PLATA");
        if (best < oro)   return (oro   - best, "ORO");
        if (best < dia)   return (dia   - best, "DIAMANTE");
        return (0, null);
    }

    /// <summary>Puntos que faltan (respecto a la mejor marca) para alcanzar la diana
    /// de un rango concreto (2=PLATA, 3=ORO, 4=DIAMANTE). 0 si el juego no puntúa.</summary>
    public static int PuntosParaRango(string profileId, string sceneBase, int targetRank)
    {
        int r = RefMax(sceneBase);
        if (r <= 0) return 0;
        float frac = targetRank >= 4 ? FRAC_DIAMANTE
                   : targetRank == 3 ? FRAC_ORO
                   : targetRank == 2 ? FRAC_PLATA : 0f;
        int target = Mathf.RoundToInt(r * frac);
        return Mathf.Max(0, target - MejorPuntuacion(profileId, sceneBase));
    }

    /// <summary>Puntos absolutos de la diana de un rango concreto (2=PLATA, 3=ORO,
    /// 4=DIAMANTE) para la dificultad activa. 0 si el juego no puntúa o es BRONCE.</summary>
    public static int DianaPuntos(string sceneBase, int rango)
    {
        int r = RefMax(sceneBase);
        if (r <= 0) return 0;
        float frac = rango >= 4 ? FRAC_DIAMANTE
                   : rango == 3 ? FRAC_ORO
                   : rango == 2 ? FRAC_PLATA : 0f;
        return Mathf.RoundToInt(r * frac);
    }

    // ================================================================ DISTRITO

    /// <summary>Suma de rangos de los 5 juegos del distrito (0-20).</summary>
    public static int SumaDistrito(string profileId, int categoria)
    {
        var d = GameCatalog.Get(categoria);
        int sum = 0;
        foreach (var g in d.games) sum += Rank(profileId, g.sceneBase);
        return sum;
    }

    // ================================================================ CELEBRACIÓN

    public static int RangoVisto(string profileId, string sceneBase) =>
        Mathf.Clamp(PlayerPrefs.GetInt(KSeen(profileId, sceneBase), 0), 0, MAX_RANK);

    public static void MarcarVisto(string profileId, string sceneBase, int rango)
    {
        PlayerPrefs.SetInt(KSeen(profileId, sceneBase), Mathf.Clamp(rango, 0, MAX_RANK));
        PlayerPrefs.Save();
    }

    // ================================================================ PRESENTACIÓN

    public static string NombreRango(int r)
    {
        switch (Mathf.Clamp(r, 0, MAX_RANK))
        {
            case 1:  return "BRONCE";
            case 2:  return "PLATA";
            case 3:  return "ORO";
            case 4:  return "DIAMANTE";
            default: return "EN RUINAS";
        }
    }

    public static Color ColorRango(int r)
    {
        switch (Mathf.Clamp(r, 0, MAX_RANK))
        {
            case 1:  return new Color(0.80f, 0.50f, 0.25f);   // bronce
            case 2:  return new Color(0.75f, 0.80f, 0.88f);   // plata
            case 3:  return new Color(1.00f, 0.82f, 0.12f);   // oro
            case 4:  return new Color(0.45f, 0.90f, 1.00f);   // diamante
            default: return KidUI.DIM;                        // en ruinas
        }
    }

    // ================================================================ DIANAS (R)

    /// <summary>Puntuación máxima teórica (juego perfecto) por sceneBase y dificultad
    /// {Easy, Medium, Hard}. Calculada a partir del sistema de puntuación de cada
    /// minijuego. 0 = el juego no tiene puntuación significativa (rango por estrellas).</summary>
    static readonly Dictionary<string, int[]> MAX_SCORE = new Dictionary<string, int[]>
    {
        // ---- Memoria
        { "Memory_ColorMatch",   new[] {  720,  960, 1440 } },
        { "Memory_PatternRecall",new[] { 1200, 1800, 2400 } },
        { "Memory_FindChange",   new[] {  950, 1100, 1250 } },
        { "Memory_SimonSays",    new[] { 1400, 2500, 3300 } },
        { "Memory_WordMemory",   new[] {  210,  300,  390 } },
        // ---- Control de impulsos (dianas sobre la puntuación de precisión, sin bonus de velocidad)
        { "Impulse_DontFollowMajority", new[] {  930, 1210, 1530 } },
        { "Impulse_InverseResponse",    new[] {  900, 1140, 1380 } },
        { "Impulse_DontPressYet",       new[] {  640,  720,  800 } },
        { "Impulse_SilentCountdown",    new[] { 1250, 1250, 1250 } },
        { "Impulse_StopAndGo",          new[] {  920, 1180, 1350 } },
        // ---- Gestión emocional
        { "Emotional_Balance",               new[] { 1000, 1000, 1000 } },
        { "Emotional_Consequences",          new[] {  100,  120,  160 } },
        { "Emotional_ProgressiveRegulation", new[] { 1000, 1000, 1000 } },  // sincronía 0-1000
        { "Emotional_AttractionControl",     new[] {  200,  200,  260 } },
        { "Emotional_AventuraEmocional",     new[] {  800,  800,  800 } },
        // ---- Atención (QuickReaction/DontFollow: diana realista, el bonus de velocidad es un extra)
        { "Attention_ObjectTracking", new[] {  600, 1250, 2100 } },
        { "Attention_LaserPath",      new[] { 1640, 1460, 1520 } },
        { "Attention_AlgoNoCuadra",   new[] {  900, 1200, 1500 } },
        { "Attention_QuickReaction",  new[] {  800,  950, 1100 } },
        { "Attention_RuleSwitch",     new[] {  150,  200,  250 } },
        // ---- Planificación
        { "Planning_ActionSequence",    new[] {  750,  900,  900 } },
        { "Planning_OptimalPath",       new[] { 1000, 1000, 1000 } },
        { "Planning_PathMemory",        new[] { 1000, 1000, 1000 } },
        { "Planning_OrdenCorrecto",     new[] { 1650, 2210, 3250 } },
        { "Planning_ResourceManagement",new[] { 1000, 1000, 1000 } },
    };

    /// <summary>Diana máxima del juego para la dificultad activa del perfil.</summary>
    static int RefMax(string sceneBase)
    {
        if (string.IsNullOrEmpty(sceneBase) || !MAX_SCORE.TryGetValue(sceneBase, out var arr))
            return 1000;   // fallback razonable si un juego no está tabulado
        int d = GameManager.Instance != null ? (int)GameManager.Instance.CurrentDifficulty : 0;
        return arr[Mathf.Clamp(d, 0, 2)];
    }

    // ================================================================ INTERNOS

    /// <summary>sceneBase del minijuego de la escena activa (quita _Easy/_Medium/_Hard
    /// y busca en el catálogo). null si la escena no es un minijuego.</summary>
    public static string SceneBaseActual()
    {
        string s = SceneManager.GetActiveScene().name;
        if (string.IsNullOrEmpty(s)) return null;
        foreach (string suf in Sufijos)
            if (s.EndsWith(suf)) { s = s.Substring(0, s.Length - suf.Length); break; }
        var g = FindBySceneBase(s);
        return g != null ? g.sceneBase : null;
    }

    static readonly string[] Sufijos = { "_Easy", "_Medium", "_Hard" };

    static GameCatalog.GameInfo FindBySceneBase(string sceneBase)
    {
        if (string.IsNullOrEmpty(sceneBase)) return null;
        for (int c = 0; c < 5; c++)
        {
            var d = GameCatalog.Get(c);
            foreach (var g in d.games)
                if (g.sceneBase == sceneBase) return g;
        }
        return null;
    }
}
