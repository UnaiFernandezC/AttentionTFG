// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Logros y rachas calculados directamente sobre la telemetría existente
/// (sin almacenamiento nuevo: solo se persiste qué logros ya se celebraron,
/// en PlayerPrefs por perfil). Todo derivado de sesiones y resultados.
/// </summary>
public static class AchievementSystem
{
    public class Badge
    {
        public string id;
        public string nombre;
        public string desc;
        public string symbol;    // texto corto que se dibuja en la medalla
        public Color color;
        public bool unlocked;
    }

    const string PREFS_PREFIX = "attention_ach_";

    // ------------------------------------------------ Racha de días

    /// <summary>Días consecutivos (terminando hoy o ayer) con al menos una partida.</summary>
    public static int GetStreakDays(string profileId)
    {
        if (string.IsNullOrEmpty(profileId)) return 0;
        var store = ProfileManager.Store;
        if (store == null) return 0;
        var days = new HashSet<string>(
            store.GetResults(profileId).Select(r => DataUtils.TicksToLocalDate(r.fechaUtcTicks)));
        if (days.Count == 0) return 0;

        DateTime cursor = DateTime.Now.Date;
        // La racha sigue viva si se jugó hoy o ayer.
        if (!days.Contains(cursor.ToString("dd/MM/yyyy")))
        {
            cursor = cursor.AddDays(-1);
            if (!days.Contains(cursor.ToString("dd/MM/yyyy"))) return 0;
        }
        int streak = 0;
        while (days.Contains(cursor.ToString("dd/MM/yyyy")))
        {
            streak++;
            cursor = cursor.AddDays(-1);
        }
        return streak;
    }

    // ------------------------------------------------ Evaluación de logros

    public static List<Badge> Evaluate(string profileId)
    {
        var store = ProfileManager.Store;
        // Sin perfil válido no hay logros: nunca se comparten entre jugadores.
        var results = (!string.IsNullOrEmpty(profileId) && store != null)
            ? store.GetResults(profileId)
            : new List<MinigameResultData>();

        int partidas = results.Count;
        int distintos = results.Select(r => r.minijuego).Distinct().Count();
        int streak = GetStreakDays(profileId);
        bool todasCategorias = Enumerable.Range(0, 5)
            .All(c => results.Any(r => r.categoria == c && r.completado));

        var list = new List<Badge>
        {
            Make("primera",  "Primer despegue",   "Juega tu primera partida",        "1",   0, partidas >= 1),
            Make("diez",     "En órbita",         "Juega 10 partidas",               "10",  1, partidas >= 10),
            Make("cincuenta","Piloto estelar",    "Juega 50 partidas",               "50",  2, partidas >= 50),
            Make("cien",     "Leyenda espacial",  "Juega 100 partidas",              "100", 3, partidas >= 100),
            Make("explora5", "Explorador",        "Prueba 5 juegos distintos",       "5",   4, distintos >= 5),
            Make("explora15","Gran explorador",   "Prueba 15 juegos distintos",      "15",  5, distintos >= 15),
            Make("planeta",  "Héroe de Attentia", "Completa un juego de cada zona",  "P",   1, todasCategorias),
            Make("racha3",   "Constante",         "Juega 3 días seguidos",           "3d",  2, streak >= 3),
            Make("racha7",   "Imparable",         "Juega 7 días seguidos",           "7d",  3, streak >= 7)
        };

        // Maestro de cada categoría: 10 partidas completadas en ella
        for (int c = 0; c < 5; c++)
        {
            string catName = MinigameResultData.CategoryDisplayName((MinigameCategory)c);
            int completadas = results.Count(r => r.categoria == c && r.completado);
            var b = Make("maestro" + c, "Maestro: " + catName,
                         "Completa 10 juegos de " + catName, "M", 0, completadas >= 10);
            b.color = IntroPanel.CategoryColor(catName);
            list.Add(b);
        }
        return list;
    }

    static Badge Make(string id, string nombre, string desc, string symbol,
                      int colorIdx, bool unlocked)
    {
        return new Badge
        {
            id = id, nombre = nombre, desc = desc, symbol = symbol,
            color = KidUI.CARD_COLORS[colorIdx % KidUI.CARD_COLORS.Length],
            unlocked = unlocked
        };
    }

    // ------------------------------------------------ Novedades (para celebrar)

    /// <summary>Devuelve los logros recién desbloqueados desde la última vez y los
    /// marca como vistos.</summary>
    public static List<Badge> TakeNewlyUnlocked(string profileId)
    {
        // Guarda de seguridad: sin id de perfil no se celebra nada ni se toca
        // ninguna clave global (evita cualquier fuga entre jugadores).
        if (string.IsNullOrEmpty(profileId)) return new List<Badge>();
        var all = Evaluate(profileId);
        string key = PREFS_PREFIX + profileId;
        var seen = new HashSet<string>(
            (PlayerPrefs.GetString(key, "") ?? "").Split(new[] { ',' },
                StringSplitOptions.RemoveEmptyEntries));

        var newly = all.Where(b => b.unlocked && !seen.Contains(b.id)).ToList();
        if (newly.Count > 0)
        {
            foreach (var b in newly) seen.Add(b.id);
            PlayerPrefs.SetString(key, string.Join(",", seen));
            PlayerPrefs.Save();
        }
        return newly;
    }
}
