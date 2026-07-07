// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using System;
using System.Collections.Generic;

/// <summary>
/// Modelos de datos serializables (JsonUtility) para perfiles, sesiones y resultados.
/// Las fechas se guardan como ticks UTC (long) + cadena legible ISO para robustez.
/// </summary>

[Serializable]
public class ProfileData
{
    public string id;
    public string nombre;
    public string avatarId;        // clave del sprite en Resources/Avatars
    public int    edadTramo;       // 0 = 3-5, 1 = 5-7, 2 = 7-10
    public int    dificultad = -1; // -1 = usar la recomendada por edad; 0/1/2 = elegida
    public long   fechaCreacionUtcTicks;
    public string fechaCreacionUtc;

    public static string[] EdadTramoLabels = { "3-5 años", "5-7 años", "7-10 años" };

    public string EdadTramoLabel =>
        (edadTramo >= 0 && edadTramo < EdadTramoLabels.Length) ? EdadTramoLabels[edadTramo] : "-";

    public DifficultyLevel DificultadRecomendada =>
        edadTramo == 0 ? DifficultyLevel.Easy :
        edadTramo == 1 ? DifficultyLevel.Medium : DifficultyLevel.Hard;

    /// <summary>Dificultad efectiva del perfil: la elegida, o la recomendada por edad.</summary>
    public DifficultyLevel DificultadActiva =>
        (dificultad >= 0 && dificultad <= 2) ? (DifficultyLevel)dificultad : DificultadRecomendada;
}

[Serializable]
public class SessionData
{
    public string id;
    public string profileId;
    public long   inicioUtcTicks;
    public string inicioUtc;
    public long   finUtcTicks;
    public string finUtc;
    public int    dificultad;      // DifficultyLevel al iniciar

    public double DuracionMin =>
        finUtcTicks > inicioUtcTicks
            ? TimeSpan.FromTicks(finUtcTicks - inicioUtcTicks).TotalMinutes
            : 0.0;
}

[Serializable]
public class MinigameResultData
{
    public string id;
    public string profileId;
    public string sessionId;
    public string minijuego;
    public int    categoria;               // MinigameCategory
    public int    dificultad;              // DifficultyLevel
    public long   fechaUtcTicks;
    public string fechaUtc;
    public float  duracionSeg;
    public int    aciertos;
    public int    errores;
    public int    puntuacion;
    public float  tiempoReaccionMedioMs;   // -1 si el minijuego no lo reporta
    public bool   completado;
    public int    monedas;

    public int   Intentos => aciertos + errores;
    public float PorcentajeAcierto => Intentos > 0 ? (100f * aciertos / Intentos) : -1f;

    public string CategoriaNombre => CategoryDisplayName((MinigameCategory)categoria);
    public string DificultadNombre => DifficultyDisplayName((DifficultyLevel)dificultad);

    public static string CategoryDisplayName(MinigameCategory cat)
    {
        switch (cat)
        {
            case MinigameCategory.Memory:              return "Memoria";
            case MinigameCategory.ImpulseControl:      return "Control de impulsos";
            case MinigameCategory.EmotionalManagement: return "Gestion emocional";
            case MinigameCategory.Attention:           return "Atencion";
            case MinigameCategory.Planning:            return "Planificacion";
            default:                                   return cat.ToString();
        }
    }

    public static string DifficultyDisplayName(DifficultyLevel d)
    {
        switch (d)
        {
            case DifficultyLevel.Easy:   return "Facil (NEO)";
            case DifficultyLevel.Medium: return "Medio (AXEL)";
            case DifficultyLevel.Hard:   return "Dificil (TITAN)";
            default:                     return d.ToString();
        }
    }
}

/// <summary>Contenedor por perfil: un fichero JSON por niño.</summary>
[Serializable]
public class ProfileDatabase
{
    public ProfileData profile = new ProfileData();
    public List<SessionData> sessions = new List<SessionData>();
    public List<MinigameResultData> results = new List<MinigameResultData>();
}

/// <summary>Ajustes globales del área de tutor (PIN compartido, modo profesional
/// y consentimiento parental).</summary>
[Serializable]
public class TutorSettings
{
    public string pinHash = "";              // SHA-256 hex del PIN de 4 dígitos
    public bool   modoProfesional = false;   // gabinetes: perfiles ilimitados, búsqueda, export por lote
    public string consentimientoVersion = ""; // versión de la política aceptada ("" = pendiente)
    public long   consentimientoUtcTicks;     // cuándo se aceptó
}

public static class DataUtils
{
    public static string NewId() => Guid.NewGuid().ToString("N");

    public static long NowTicks() => DateTime.UtcNow.Ticks;

    public static string TicksToIso(long ticks) =>
        new DateTime(ticks, DateTimeKind.Utc).ToString("yyyy-MM-dd HH:mm:ss");

    public static string TicksToLocalDate(long ticks) =>
        new DateTime(ticks, DateTimeKind.Utc).ToLocalTime().ToString("dd/MM/yyyy");

    public static string TicksToLocalDateTime(long ticks) =>
        new DateTime(ticks, DateTimeKind.Utc).ToLocalTime().ToString("dd/MM/yyyy HH:mm");

    public static string HashPin(string pin)
    {
        if (string.IsNullOrEmpty(pin)) return "";
        using (var sha = System.Security.Cryptography.SHA256.Create())
        {
            byte[] bytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes("AttentiON::" + pin));
            var sb = new System.Text.StringBuilder(bytes.Length * 2);
            foreach (byte b in bytes) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }
}
