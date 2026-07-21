// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

/// <summary>
/// Genera el informe de un perfil en tres formatos, todos offline:
///  - Excel (.xlsx)  → XlsxWriter propio (sin dependencias)
///  - CSV (;)        → respaldo universal
///  - HTML autónomo  → informe visual con gráficas (imprimible a PDF desde el navegador)
/// Se guarda en Documentos\AttentiON\Informes\ y se abre automáticamente.
/// Orientado a psicopedagogos/familiares: lenguaje claro y SIN diagnósticos.
/// </summary>
public static class ReportGenerator
{
    // ============================================================ MODELO AGREGADO

    class CategoryStats
    {
        public MinigameCategory Cat;
        public int Partidas, Completadas, Aciertos, Errores;
        public float Pct = -1f;          // % acierto (solo resultados con rondas)
        public float RtMs = -1f;         // tiempo reacción medio
        public float PctPrimeraMitad = -1f, PctSegundaMitad = -1f; // evolución
        public string Nombre => MinigameResultData.CategoryDisplayName(Cat);

        public string Tendencia
        {
            get
            {
                if (PctPrimeraMitad < 0 || PctSegundaMitad < 0) return "sin datos suficientes";
                float d = PctSegundaMitad - PctPrimeraMitad;
                if (d > 5f)  return $"mejora (+{d:0} pts)";
                if (d < -5f) return $"a la baja ({d:0} pts)";
                return "estable";
            }
        }
    }

    class MinigameStats
    {
        public string Nombre;
        public MinigameCategory Cat;
        public int Partidas, Completadas, Aciertos, Errores;
        public float Pct = -1f, RtMs = -1f;
        public int MejorPuntuacion;
        public long UltimaVezTicks;
    }

    class DayPoint
    {
        public string Fecha;
        public float Pct;
        public int Partidas;
    }

    class SectorRank
    {
        public string Juego;       // nombre visible
        public string Tel;         // nombre de telemetría (para casar con los resultados)
        public int Rango;          // 0-4
        public int MejorPuntos;
    }

    class DistrictRankInfo
    {
        public MinigameCategory Cat;
        public string Nombre => MinigameResultData.CategoryDisplayName(Cat);
        public List<SectorRank> Sectores = new List<SectorRank>();
        public int Suma;           // 0-20
    }

    class ReportModel
    {
        public ProfileData Profile;
        public List<SessionData> Sessions;
        public List<MinigameResultData> Results;
        public List<CategoryStats> Categorias = new List<CategoryStats>();
        public List<MinigameStats> Minijuegos = new List<MinigameStats>();
        public List<DayPoint> Evolucion = new List<DayPoint>();
        public List<DistrictRankInfo> Rangos = new List<DistrictRankInfo>();
        public int RangoTotal;     // 0-100 (suma de los 5 distritos)
        public int Diamantes, Oros;
        public double MinutosTotales;
        public float PctGlobal = -1f;
        public List<string> Interpretacion = new List<string>();
    }

    // ============================================================ API PÚBLICA

    public static bool GenerateAndOpen(ProfileData profile, out string folder) =>
        Generate(profile, out folder, openAfter: true);

    /// <summary>Genera el informe (XLSX+CSV+HTML). openAfter=false para exportación
    /// por lote (modo profesional): genera sin abrir el navegador por cada niño.</summary>
    public static bool Generate(ProfileData profile, out string folder, bool openAfter)
    {
        folder = "";
        try
        {
            var store = ProfileManager.Store;
            if (profile == null || store == null) return false;

            var model = BuildModel(profile, store);

            string docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            folder = Path.Combine(docs, "AttentiON", "Informes");
            Directory.CreateDirectory(folder);

            string safeName = Sanitize(profile.nombre);
            string stamp = DateTime.Now.ToString("yyyyMMdd_HHmm");
            string baseName = $"Informe_{safeName}_{stamp}";

            string xlsxPath = Path.Combine(folder, baseName + ".xlsx");
            string csvPath  = Path.Combine(folder, baseName + ".csv");
            string htmlPath = Path.Combine(folder, baseName + ".html");

            WriteXlsx(model, xlsxPath);
            WriteCsv(model, csvPath);
            WriteHtml(model, htmlPath);

            // Abre el informe visual (salvo en exportación por lote).
            if (openAfter)
                Application.OpenURL("file:///" + htmlPath.Replace("\\", "/"));
            Debug.Log($"[ReportGenerator] Informe generado en {folder}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[ReportGenerator] Error generando informe: {e}");
            return false;
        }
    }

    static string Sanitize(string s)
    {
        if (string.IsNullOrEmpty(s)) return "Jugador";
        foreach (char c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
        return s.Replace(' ', '_');
    }

    // ============================================================ AGREGACIÓN

    static ReportModel BuildModel(ProfileData p, IDataStore store)
    {
        var m = new ReportModel
        {
            Profile = p,
            Sessions = store.GetSessions(p.id),
            Results = store.GetResults(p.id)
        };

        m.MinutosTotales = m.Sessions.Sum(s => s.DuracionMin);

        var conRondas = m.Results.Where(r => r.Intentos > 0).ToList();
        if (conRondas.Count > 0)
            m.PctGlobal = conRondas.Average(r => r.PorcentajeAcierto);

        // --- Por categoría
        foreach (var g in m.Results.GroupBy(r => (MinigameCategory)r.categoria).OrderBy(g => g.Key))
        {
            var list = g.OrderBy(r => r.fechaUtcTicks).ToList();
            var cs = new CategoryStats
            {
                Cat = g.Key,
                Partidas = list.Count,
                Completadas = list.Count(r => r.completado),
                Aciertos = list.Sum(r => r.aciertos),
                Errores = list.Sum(r => r.errores)
            };
            var rondas = list.Where(r => r.Intentos > 0).ToList();
            if (rondas.Count > 0) cs.Pct = rondas.Average(r => r.PorcentajeAcierto);
            var rts = list.Where(r => r.tiempoReaccionMedioMs > 0).ToList();
            if (rts.Count > 0) cs.RtMs = rts.Average(r => r.tiempoReaccionMedioMs);

            if (rondas.Count >= 4)
            {
                int half = rondas.Count / 2;
                cs.PctPrimeraMitad = rondas.Take(half).Average(r => r.PorcentajeAcierto);
                cs.PctSegundaMitad = rondas.Skip(half).Average(r => r.PorcentajeAcierto);
            }
            m.Categorias.Add(cs);
        }

        // --- Por minijuego
        foreach (var g in m.Results.GroupBy(r => r.minijuego).OrderBy(g => g.Key))
        {
            var list = g.ToList();
            var ms = new MinigameStats
            {
                Nombre = g.Key,
                Cat = (MinigameCategory)list[0].categoria,
                Partidas = list.Count,
                Completadas = list.Count(r => r.completado),
                Aciertos = list.Sum(r => r.aciertos),
                Errores = list.Sum(r => r.errores),
                MejorPuntuacion = list.Max(r => r.puntuacion),
                UltimaVezTicks = list.Max(r => r.fechaUtcTicks)
            };
            var rondas = list.Where(r => r.Intentos > 0).ToList();
            if (rondas.Count > 0) ms.Pct = rondas.Average(r => r.PorcentajeAcierto);
            var rts = list.Where(r => r.tiempoReaccionMedioMs > 0).ToList();
            if (rts.Count > 0) ms.RtMs = rts.Average(r => r.tiempoReaccionMedioMs);
            m.Minijuegos.Add(ms);
        }

        // --- Evolución por día (solo días con rondas registradas)
        foreach (var g in m.Results.Where(r => r.Intentos > 0)
                          .GroupBy(r => DataUtils.TicksToLocalDate(r.fechaUtcTicks))
                          .OrderBy(g => g.Min(r => r.fechaUtcTicks)))
        {
            m.Evolucion.Add(new DayPoint
            {
                Fecha = g.Key,
                Pct = g.Average(r => r.PorcentajeAcierto),
                Partidas = g.Count()
            });
        }

        // --- Rangos por distrito (sistema de reto por puntuación)
        for (int c = 0; c < 5; c++)
        {
            var d = GameCatalog.Get(c);
            var dr = new DistrictRankInfo { Cat = (MinigameCategory)c };
            foreach (var g in d.games)
            {
                int rk = ChallengeSystem.Rank(p.id, g.sceneBase);
                dr.Sectores.Add(new SectorRank
                {
                    Juego = g.display,
                    Tel = g.telemetry,
                    Rango = rk,
                    MejorPuntos = ChallengeSystem.MejorPuntuacion(p.id, g.sceneBase)
                });
                dr.Suma += rk;
                if (rk >= 4) m.Diamantes++;
                else if (rk == 3) m.Oros++;
            }
            m.RangoTotal += dr.Suma;
            m.Rangos.Add(dr);
        }

        BuildInterpretation(m);
        return m;
    }

    /// <summary>Lectura automática prudente de los datos: fortalezas, áreas a reforzar
    /// y recomendaciones simples. Nunca diagnostica.</summary>
    static void BuildInterpretation(ReportModel m)
    {
        var I = m.Interpretacion;

        if (m.Results.Count == 0)
        {
            I.Add("Todavia no hay partidas registradas. Juegue algunas sesiones para obtener un informe con datos.");
            return;
        }

        I.Add($"Durante el periodo registrado, {m.Profile.nombre} ha jugado {m.Results.Count} partidas " +
              $"en {m.Sessions.Count} sesiones (aprox. {m.MinutosTotales:0} minutos en total).");

        if (m.RangoTotal > 0)
        {
            string logro = m.Diamantes > 0 ? $" (incluyendo {m.Diamantes} de DIAMANTE)"
                         : m.Oros > 0 ? $" (incluyendo {m.Oros} de ORO)" : "";
            I.Add($"Progreso de maestria: {m.RangoTotal} de 100 puntos de rango acumulados entre los 25 sectores" +
                  logro + ". El rango sube al SUPERAR marcas de puntuacion cada vez mas altas, sin cambiar la " +
                  "dificultad del juego; refleja constancia y mejora, no solo completar.");
        }

        var conPct = m.Categorias.Where(c => c.Pct >= 0).ToList();
        if (conPct.Count >= 2)
        {
            var mejor = conPct.OrderByDescending(c => c.Pct).First();
            var peor = conPct.OrderBy(c => c.Pct).First();
            if (mejor != peor)
            {
                I.Add($"El mayor porcentaje de acierto se observa en {mejor.Nombre} ({mejor.Pct:0}%), " +
                      $"lo que sugiere un punto fuerte en esta area.");
                I.Add($"El area con mas dificultad es {peor.Nombre} ({peor.Pct:0}% de acierto). " +
                      $"Puede ser util reforzar los minijuegos de esta categoria en proximas sesiones.");
            }
        }

        var mejoran = m.Categorias.Where(c => c.Tendencia.StartsWith("mejora")).Select(c => c.Nombre).ToList();
        var bajan = m.Categorias.Where(c => c.Tendencia.StartsWith("a la baja")).Select(c => c.Nombre).ToList();
        if (mejoran.Count > 0)
            I.Add("Se aprecia una evolucion positiva con el tiempo en: " + string.Join(", ", mejoran) + ".");
        if (bajan.Count > 0)
            I.Add("Conviene observar la evolucion en: " + string.Join(", ", bajan) +
                  " (el rendimiento reciente es algo menor; puede deberse a mayor dificultad, cansancio u otros factores).");

        var rts = m.Categorias.Where(c => c.RtMs > 0).ToList();
        if (rts.Count > 0)
        {
            var rapida = rts.OrderBy(c => c.RtMs).First();
            I.Add($"El tiempo de reaccion medio mas rapido se registra en {rapida.Nombre} ({rapida.RtMs:0} ms).");
        }

        I.Add("Recuerde: estos datos proceden de un videojuego educativo y son una herramienta complementaria " +
              "de observacion. NO constituyen una evaluacion clinica ni un diagnostico. Ante cualquier duda, " +
              "consulte con un profesional.");
    }

    // ============================================================ EXCEL

    static void WriteXlsx(ReportModel m, string path)
    {
        var sheets = new List<XlsxWriter.Sheet>();
        var p = m.Profile;

        // --- Hoja Resumen
        var res = new XlsxWriter.Sheet("Resumen");
        res.AddRow("INFORME AttentiON", "");
        res.AddRow("Nombre", p.nombre);
        res.AddRow("Tramo de edad", p.EdadTramoLabel);
        res.AddRow("Dificultad recomendada", MinigameResultData.DifficultyDisplayName(p.DificultadRecomendada));
        res.AddRow("Perfil creado", DataUtils.TicksToLocalDate(p.fechaCreacionUtcTicks));
        res.AddRow("Fecha del informe", DateTime.Now.ToString("dd/MM/yyyy HH:mm"));
        res.AddRow("");
        res.AddRow("Numero de sesiones", m.Sessions.Count);
        res.AddRow("Tiempo total (min)", Math.Round(m.MinutosTotales, 1));
        res.AddRow("Partidas jugadas", m.Results.Count);
        res.AddRow("Partidas completadas", m.Results.Count(r => r.completado));
        if (m.PctGlobal >= 0) res.AddRow("% de acierto global", Math.Round(m.PctGlobal, 1));
        res.AddRow("");
        res.AddRow("Interpretacion automatica (no clinica):");
        foreach (string linea in m.Interpretacion) res.AddRow(linea);
        res.ColWidths = new[] { 30f, 95f };
        sheets.Add(res);

        // --- Hoja Rangos (sistema de reto por puntuación)
        var rk = new XlsxWriter.Sheet("Rangos");
        rk.AddRow("Distrito", "Minijuego", "Rango", "Mejor puntuacion");
        foreach (var d in m.Rangos)
            foreach (var s in d.Sectores)
                rk.AddRow(d.Nombre, s.Juego, ChallengeSystem.NombreRango(s.Rango), s.MejorPuntos);
        rk.AddRow("");
        rk.AddRow("Maestria total (0-100)", m.RangoTotal);
        rk.AddRow("Sectores en ORO", m.Oros);
        rk.AddRow("Sectores en DIAMANTE", m.Diamantes);
        rk.ColWidths = new[] { 24f, 28f, 14f, 18f };
        sheets.Add(rk);

        // --- Hoja Por categoría
        var cat = new XlsxWriter.Sheet("Por categoria");
        cat.AddRow("Categoria", "Partidas", "Completadas", "Aciertos", "Errores",
                   "% acierto", "T. reaccion medio (ms)", "% 1a mitad", "% 2a mitad", "Tendencia");
        foreach (var c in m.Categorias)
            cat.AddRow(c.Nombre, c.Partidas, c.Completadas, c.Aciertos, c.Errores,
                       c.Pct >= 0 ? (object)Math.Round(c.Pct, 1) : "-",
                       c.RtMs >= 0 ? (object)Math.Round(c.RtMs, 0) : "-",
                       c.PctPrimeraMitad >= 0 ? (object)Math.Round(c.PctPrimeraMitad, 1) : "-",
                       c.PctSegundaMitad >= 0 ? (object)Math.Round(c.PctSegundaMitad, 1) : "-",
                       c.Tendencia);
        cat.ColWidths = new[] { 22f, 10f, 12f, 10f, 9f, 10f, 22f, 11f, 11f, 18f };
        sheets.Add(cat);

        // --- Hoja Por minijuego
        var mg = new XlsxWriter.Sheet("Por minijuego");
        mg.AddRow("Minijuego", "Categoria", "Partidas", "Completadas", "Aciertos", "Errores",
                  "% acierto", "T. reaccion (ms)", "Mejor puntuacion", "Ultima vez");
        foreach (var s in m.Minijuegos)
            mg.AddRow(s.Nombre, MinigameResultData.CategoryDisplayName(s.Cat), s.Partidas,
                      s.Completadas, s.Aciertos, s.Errores,
                      s.Pct >= 0 ? (object)Math.Round(s.Pct, 1) : "-",
                      s.RtMs >= 0 ? (object)Math.Round(s.RtMs, 0) : "-",
                      s.MejorPuntuacion, DataUtils.TicksToLocalDate(s.UltimaVezTicks));
        mg.ColWidths = new[] { 26f, 20f, 10f, 12f, 10f, 9f, 10f, 14f, 16f, 14f };
        sheets.Add(mg);

        // --- Hoja Historico de sesiones
        var ses = new XlsxWriter.Sheet("Sesiones");
        ses.AddRow("Inicio", "Fin", "Duracion (min)", "Dificultad", "Partidas en la sesion");
        foreach (var s in m.Sessions)
            ses.AddRow(DataUtils.TicksToLocalDateTime(s.inicioUtcTicks),
                       DataUtils.TicksToLocalDateTime(s.finUtcTicks),
                       Math.Round(s.DuracionMin, 1),
                       MinigameResultData.DifficultyDisplayName((DifficultyLevel)s.dificultad),
                       m.Results.Count(r => r.sessionId == s.id));
        ses.ColWidths = new[] { 20f, 20f, 15f, 14f, 22f };
        sheets.Add(ses);

        // --- Hoja Detalle (todas las partidas)
        var det = new XlsxWriter.Sheet("Detalle partidas");
        det.AddRow("Fecha", "Minijuego", "Categoria", "Dificultad", "Duracion (s)",
                   "Aciertos", "Errores", "% acierto", "T. reaccion (ms)", "Puntuacion", "Completado", "Monedas");
        foreach (var r in m.Results)
            det.AddRow(DataUtils.TicksToLocalDateTime(r.fechaUtcTicks), r.minijuego,
                       r.CategoriaNombre, r.DificultadNombre, Math.Round(r.duracionSeg, 1),
                       r.aciertos, r.errores,
                       r.Intentos > 0 ? (object)Math.Round(r.PorcentajeAcierto, 1) : "-",
                       r.tiempoReaccionMedioMs > 0 ? (object)Math.Round(r.tiempoReaccionMedioMs, 0) : "-",
                       r.puntuacion, r.completado ? "Si" : "No", r.monedas);
        det.ColWidths = new[] { 20f, 26f, 20f, 13f, 13f, 10f, 9f, 10f, 14f, 12f, 12f, 10f };
        sheets.Add(det);

        XlsxWriter.Write(path, sheets);
    }

    // ============================================================ CSV

    static void WriteCsv(ReportModel m, string path)
    {
        var sb = new StringBuilder();
        var inv = CultureInfo.InvariantCulture;
        // Separador ';' (Excel en espanol) y BOM UTF-8 para acentos.
        sb.AppendLine("Fecha;Minijuego;Categoria;Dificultad;DuracionSeg;Aciertos;Errores;PctAcierto;TReaccionMs;Puntuacion;Completado;Monedas;SesionId");
        foreach (var r in m.Results)
        {
            sb.Append(DataUtils.TicksToLocalDateTime(r.fechaUtcTicks)).Append(';')
              .Append(Csv(r.minijuego)).Append(';')
              .Append(Csv(r.CategoriaNombre)).Append(';')
              .Append(Csv(r.DificultadNombre)).Append(';')
              .Append(r.duracionSeg.ToString("0.0", inv)).Append(';')
              .Append(r.aciertos).Append(';')
              .Append(r.errores).Append(';')
              .Append(r.Intentos > 0 ? r.PorcentajeAcierto.ToString("0.0", inv) : "").Append(';')
              .Append(r.tiempoReaccionMedioMs > 0 ? r.tiempoReaccionMedioMs.ToString("0", inv) : "").Append(';')
              .Append(r.puntuacion).Append(';')
              .Append(r.completado ? "Si" : "No").Append(';')
              .Append(r.monedas).Append(';')
              .Append(r.sessionId).AppendLine();
        }
        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));
    }

    static string Csv(string s) =>
        string.IsNullOrEmpty(s) ? "" :
        (s.Contains(";") || s.Contains("\"")) ? "\"" + s.Replace("\"", "\"\"") + "\"" : s;

    // ============================================================ HTML VISUAL

    // Colores de las 5 funciones ejecutivas (mismo sistema que la app)
    static readonly string[] FUNC_HEX = { "#9B6BF0", "#F2934A", "#35D6A0", "#FFD21F", "#5AA9FF" };

    static void WriteHtml(ReportModel m, string path)
    {
        var p = m.Profile;
        var sb = new StringBuilder();
        var inv = CultureInfo.InvariantCulture;
        int completadas = m.Results.Count(r => r.completado);

        sb.Append(@"<!DOCTYPE html><html lang=""es""><head><meta charset=""utf-8"">
<meta name=""viewport"" content=""width=device-width,initial-scale=1"">
<title>Informe AttentiON - ").Append(H(p.nombre)).Append(@"</title>
<style>
 :root{
   --ink:#0B1020;--panel:#141C33;--panel2:#1C2647;--line:#2A3560;--txt:#EAF0FB;--dim:#94A5C6;
   --acc:#5AA9FF;--good:#35D6A0;--warn:#F2934A;--bad:#E6485C;
   --bronze:#CC8A4E;--silver:#C3CEDE;--gold:#FFD21F;--diamond:#6FE4FF;--ruins:#3A466A;
   --serif:Georgia,'Iowan Old Style','Times New Roman',serif;
   --sans:system-ui,'Segoe UI',Roboto,Helvetica,Arial,sans-serif;
 }
 *{box-sizing:border-box;margin:0;padding:0}
 body{font-family:var(--sans);background:
      radial-gradient(1200px 600px at 12% -10%,#16204a 0%,transparent 55%),
      radial-gradient(900px 500px at 100% 0%,#122040 0%,transparent 50%),var(--ink);
      color:var(--txt);line-height:1.55;padding:40px 26px 60px;max-width:1120px;margin:0 auto}
 a{color:inherit}
 .wordmark{display:flex;justify-content:space-between;align-items:baseline;
   border-bottom:1px solid var(--line);padding-bottom:12px}
 .wordmark .brand{font-family:var(--serif);font-size:15px;letter-spacing:.42em;text-transform:uppercase;color:var(--acc)}
 .wordmark .doc{font-size:12px;letter-spacing:.18em;text-transform:uppercase;color:var(--dim)}
 .eyebrow{font-family:var(--serif);font-style:italic;font-size:13px;color:var(--acc);letter-spacing:.04em;margin:38px 0 4px}
 h2{font-size:20px;font-weight:700;letter-spacing:-.01em;margin-bottom:12px}
 /* HERO */
 .hero{display:grid;grid-template-columns:1.35fr .95fr;gap:26px;align-items:center;margin-top:26px}
 .hero .name{font-family:var(--serif);font-size:44px;line-height:1.02;letter-spacing:-.02em}
 .hero .chips{margin:14px 0 10px;display:flex;gap:8px;flex-wrap:wrap}
 .chip{font-size:12px;color:var(--dim);border:1px solid var(--line);border-radius:999px;padding:5px 12px}
 .hero .lead{color:var(--dim);font-size:14px;max-width:46ch}
 .gauge{display:flex;flex-direction:column;align-items:center;justify-content:center;
   background:var(--panel);border:1px solid var(--line);border-radius:18px;padding:20px}
 .gauge .cap{font-size:11px;letter-spacing:.16em;text-transform:uppercase;color:var(--dim);margin-top:6px}
 /* STAT STRIP */
 .strip{display:grid;grid-template-columns:repeat(5,1fr);gap:12px;margin-top:22px}
 .stat{background:var(--panel);border:1px solid var(--line);border-radius:14px;padding:14px 16px}
 .stat .v{font-family:var(--serif);font-size:27px} .stat .k{font-size:11px;color:var(--dim);letter-spacing:.08em;text-transform:uppercase;margin-top:3px}
 /* DISTRICT / RANK CARDS */
 .districts{display:grid;grid-template-columns:repeat(5,1fr);gap:12px}
 .dcard{background:var(--panel);border:1px solid var(--line);border-radius:14px;padding:14px;border-top:3px solid var(--acc)}
 .dcard .dn{font-size:13px;font-weight:700;letter-spacing:.01em;min-height:32px}
 .medals{display:flex;gap:6px;margin:10px 0 8px}
 .medal{width:26px;height:26px;border-radius:50%;display:flex;align-items:center;justify-content:center;
   font-family:var(--serif);font-size:12px;font-weight:700;color:#0B1020;border:1px solid rgba(255,255,255,.18)}
 .medal.r0{background:var(--ruins);color:var(--dim);border-color:transparent}
 .fuente{font-size:11px;color:var(--dim);letter-spacing:.05em}
 .fbar{height:7px;border-radius:5px;background:var(--panel2);overflow:hidden;margin-top:5px}
 .fbar > span{display:block;height:100%;border-radius:5px}
 /* BARS */
 .bar-row{display:flex;align-items:center;gap:12px;margin:9px 0}
 .bar-lbl{width:190px;font-size:13px;color:var(--dim)}
 .bar-track{flex:1;background:var(--panel2);border-radius:7px;height:24px;overflow:hidden}
 .bar-fill{height:100%;border-radius:7px;display:flex;align-items:center;justify-content:flex-end;
   padding-right:9px;font-size:12px;font-weight:700;color:#08101f}
 /* TABLES */
 table{width:100%;border-collapse:collapse;font-size:13px;margin-top:4px;
   background:var(--panel);border:1px solid var(--line);border-radius:12px;overflow:hidden}
 th{background:var(--panel2);color:var(--acc);text-align:left;padding:10px 12px;font-size:11px;letter-spacing:.06em;text-transform:uppercase}
 td{padding:9px 12px;border-bottom:1px solid var(--line)}
 tr:last-child td{border-bottom:none}
 tr:nth-child(even) td{background:rgba(255,255,255,.018)}
 .pill{display:inline-block;padding:2px 9px;border-radius:999px;font-size:11px;font-weight:700;color:#0B1020}
 .interp{background:var(--panel);border:1px solid var(--line);border-left:4px solid var(--good);
   border-radius:12px;padding:18px 22px;margin-top:8px}
 .interp p{margin:8px 0;font-size:14px}
 .disclaimer{margin-top:30px;color:var(--dim);font-size:12px;border-top:1px solid var(--line);padding-top:14px}
 svg{display:block;background:var(--panel);border:1px solid var(--line);border-radius:14px;margin-top:4px}
 /* Texto de las gráficas SVG: por clase para que se adapte a pantalla e impresión */
 .chart .grid{stroke:var(--line)} .chart .axis{fill:var(--dim)} .chart .lbl{fill:var(--txt)} .chart .pt{fill:var(--good)} .chart .line{stroke:var(--acc)}
 .gnum{fill:var(--txt)} .gcap{fill:var(--dim)} .gtrack{stroke:var(--panel2)} .garc{stroke:var(--acc)}
 .bar-empty{height:100%;display:flex;align-items:center;padding-left:10px;font-size:12px;color:var(--dim)}
 @media (max-width:820px){.hero,.strip,.districts{grid-template-columns:1fr 1fr}.hero .name{font-size:34px}}
 @media print{
   body{background:#fff;color:#0B1020;padding:0}
   .panel,.stat,.dcard,.gauge,.interp,svg,table{background:#fff !important;border-color:#D4DBEA !important}
   th{background:#EEF2FA !important;color:#0A3E86 !important}
   td{border-color:#E2E7F1 !important}
   .wordmark .brand,.eyebrow{color:#0A3E86}
   :root{--txt:#0B1020;--dim:#5A6684}
   /* En papel: texto de las gráficas oscuro y rejilla clara (si no, quedaba invisible) */
   .chart .axis{fill:#5A6684} .chart .lbl,.gnum{fill:#0B1020} .gcap,.chart .axis{fill:#5A6684}
   .chart .grid{stroke:#D4DBEA} .gtrack{stroke:#E2E7F1}
   .dcard,.stat,.interp,table,svg{break-inside:avoid}
 }
</style></head><body>");

        // ---------- Wordmark
        sb.Append(@"<div class=""wordmark""><div class=""brand"">Attention</div>")
          .Append(@"<div class=""doc"">Informe de progreso &middot; ")
          .Append(DateTime.Now.ToString("dd/MM/yyyy · HH:mm")).Append("</div></div>");

        // ---------- HERO (nombre + gauge de acierto global)
        sb.Append(@"<div class=""hero""><div>")
          .Append(@"<div class=""name"">").Append(H(p.nombre)).Append("</div>")
          .Append(@"<div class=""chips"">")
          .Append(@"<span class=""chip"">Edad ").Append(H(p.EdadTramoLabel)).Append("</span>")
          .Append(@"<span class=""chip"">Nivel ").Append(H(MinigameResultData.DifficultyDisplayName(p.DificultadRecomendada))).Append("</span>")
          .Append(@"<span class=""chip"">Perfil desde ").Append(DataUtils.TicksToLocalDate(p.fechaCreacionUtcTicks)).Append("</span>")
          .Append("</div>")
          .Append(@"<div class=""lead"">Entrenamiento de funciones ejecutivas a través de 25 minijuegos en el planeta Attentia. Este documento resume la actividad registrada y es un apoyo de observación, no una evaluación clínica.</div>")
          .Append("</div>");
        // Gauge
        sb.Append(@"<div class=""gauge"">").Append(Gauge(m.PctGlobal))
          .Append(@"<div class=""cap"">Acierto global</div></div></div>");

        // ---------- Stat strip
        sb.Append(@"<div class=""strip"">");
        Stat(sb, m.Sessions.Count.ToString(), "Sesiones");
        Stat(sb, m.MinutosTotales.ToString("0", inv), "Minutos");
        Stat(sb, m.Results.Count.ToString(), "Partidas");
        Stat(sb, completadas.ToString(), "Completadas");
        Stat(sb, m.RangoTotal + "/100", "Maestría");
        sb.Append("</div>");

        // ---------- SIGNATURE: Fuentes Cognitivas (rangos por distrito)
        sb.Append(@"<div class=""eyebrow"">Fuentes cognitivas</div><h2>Mapa de maestría</h2>");
        sb.Append(@"<p style=""color:var(--dim);font-size:13px;margin-bottom:14px"">Cada sector sube de rango al superar marcas de puntuación cada vez más altas — la dificultad del juego no cambia. Bronce &rarr; Plata &rarr; Oro &rarr; Diamante.</p>");
        sb.Append(@"<div class=""districts"">");
        foreach (var d in m.Rangos)
        {
            string col = FUNC_HEX[(int)d.Cat % FUNC_HEX.Length];
            sb.Append($@"<div class=""dcard"" style=""border-top-color:{col}"">")
              .Append($@"<div class=""dn"" style=""color:{col}"">{H(d.Nombre)}</div>")
              .Append(@"<div class=""medals"">");
            foreach (var s in d.Sectores)
                sb.Append($@"<div class=""medal r{s.Rango}"" style=""{(s.Rango > 0 ? "background:" + RankHex(s.Rango) : "")}"" title=""{H(s.Juego)}: {H(ChallengeSystem.NombreRango(s.Rango))}"">{RankInitial(s.Rango)}</div>");
            float fw = Mathf.Clamp01(d.Suma / 20f) * 100f;
            sb.Append("</div>")
              .Append($@"<div class=""fuente"">Fuente {d.Suma}/20</div>")
              .Append($@"<div class=""fbar""><span style=""width:{fw.ToString("0", inv)}%;background:{col}""></span></div>")
              .Append("</div>");
        }
        sb.Append("</div>");

        // ---------- Rendimiento por área (barras)
        sb.Append(@"<div class=""eyebrow"">Precisión</div><h2>Rendimiento por área</h2>");
        if (m.Categorias.Any(c => c.Pct >= 0))
        {
            foreach (var c in m.Categorias)
            {
                string col = FUNC_HEX[(int)c.Cat % FUNC_HEX.Length];
                sb.Append($@"<div class=""bar-row""><div class=""bar-lbl"">{H(c.Nombre)}</div><div class=""bar-track"">");
                if (c.Pct >= 0)
                {
                    float width = Mathf.Clamp(c.Pct, 9f, 100f);
                    sb.Append($@"<div class=""bar-fill"" style=""width:{width.ToString("0", inv)}%;background:{col}"">{c.Pct.ToString("0", inv)}%</div>");
                }
                else sb.Append(@"<div class=""bar-empty"">sin datos</div>");
                sb.Append("</div></div>");
            }
        }
        else sb.Append(@"<p style=""color:var(--dim)"">Aún no hay datos de rondas (aciertos/errores) registrados.</p>");

        // ---------- Evolución (SVG)
        sb.Append(@"<div class=""eyebrow"">Constancia</div><h2>Evolución del acierto por día</h2>");
        sb.Append(EvolutionSvg(m));

        // ---------- Tabla por categoría
        sb.Append(@"<div class=""eyebrow"">Detalle</div><h2>Por categoría</h2>");
        sb.Append("<table><tr><th>Categoría</th><th>Partidas</th><th>Completadas</th><th>% acierto</th><th>T. reacción medio</th><th>Tendencia</th></tr>");
        foreach (var c in m.Categorias)
            sb.Append($"<tr><td>{H(c.Nombre)}</td><td>{c.Partidas}</td><td>{c.Completadas}</td>" +
                      $"<td>{(c.Pct >= 0 ? c.Pct.ToString("0.0", inv) + "%" : "-")}</td>" +
                      $"<td>{(c.RtMs >= 0 ? c.RtMs.ToString("0", inv) + " ms" : "-")}</td>" +
                      $"<td>{H(c.Tendencia)}</td></tr>");
        sb.Append("</table>");

        // ---------- Tabla por minijuego (con rango)
        sb.Append(@"<h2 style=""margin-top:26px"">Por minijuego</h2>");
        sb.Append("<table><tr><th>Minijuego</th><th>Categoría</th><th>Rango</th><th>Partidas</th><th>% acierto</th><th>T. reacción</th><th>Mejor punt.</th><th>Última vez</th></tr>");
        foreach (var s in m.Minijuegos)
        {
            int rk = RankForMinigame(m, s);
            string rkPill = rk > 0
                ? $@"<span class=""pill"" style=""background:{RankHex(rk)}"">{H(ChallengeSystem.NombreRango(rk))}</span>"
                : @"<span style=""color:var(--dim)"">—</span>";
            sb.Append($"<tr><td>{H(s.Nombre)}</td><td>{H(MinigameResultData.CategoryDisplayName(s.Cat))}</td>" +
                      $"<td>{rkPill}</td><td>{s.Partidas}</td>" +
                      $"<td>{(s.Pct >= 0 ? s.Pct.ToString("0.0", inv) + "%" : "-")}</td>" +
                      $"<td>{(s.RtMs >= 0 ? s.RtMs.ToString("0", inv) + " ms" : "-")}</td>" +
                      $"<td>{s.MejorPuntuacion}</td><td>{DataUtils.TicksToLocalDate(s.UltimaVezTicks)}</td></tr>");
        }
        sb.Append("</table>");

        // ---------- Sesiones
        sb.Append(@"<h2 style=""margin-top:26px"">Histórico de sesiones</h2>");
        sb.Append("<table><tr><th>Inicio</th><th>Duración</th><th>Dificultad</th><th>Partidas</th></tr>");
        foreach (var s in m.Sessions)
            sb.Append($"<tr><td>{DataUtils.TicksToLocalDateTime(s.inicioUtcTicks)}</td>" +
                      $"<td>{s.DuracionMin.ToString("0.0", inv)} min</td>" +
                      $"<td>{H(MinigameResultData.DifficultyDisplayName((DifficultyLevel)s.dificultad))}</td>" +
                      $"<td>{m.Results.Count(r => r.sessionId == s.id)}</td></tr>");
        sb.Append("</table>");

        // ---------- Interpretación
        sb.Append(@"<div class=""eyebrow"">Lectura de los datos</div><h2>Resumen orientativo (no clínico)</h2><div class=""interp"">");
        foreach (string linea in m.Interpretacion) sb.Append("<p>").Append(H(linea)).Append("</p>");
        sb.Append("</div>");

        sb.Append(@"<div class=""disclaimer"">AttentiON &middot; Informe generado automáticamente y guardado únicamente en este equipo (sin conexión). " +
                  "Es un apoyo complementario para el seguimiento del entrenamiento de funciones ejecutivas y no sustituye la valoración de un profesional. " +
                  "Para guardar en PDF: Imprimir &rarr; Guardar como PDF.</div>");
        sb.Append("</body></html>");

        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
    }

    static void Stat(StringBuilder sb, string value, string label) =>
        sb.Append($@"<div class=""stat""><div class=""v"">{H(value)}</div><div class=""k"">{H(label)}</div></div>");

    static string RankHex(int r)
    {
        switch (Mathf.Clamp(r, 0, 4))
        {
            case 1:  return "#CC8A4E";
            case 2:  return "#C3CEDE";
            case 3:  return "#FFD21F";
            case 4:  return "#6FE4FF";
            default: return "#3A466A";
        }
    }

    static string RankInitial(int r)
    {
        switch (Mathf.Clamp(r, 0, 4))
        {
            case 1:  return "B";
            case 2:  return "P";
            case 3:  return "O";
            case 4:  return "D";
            default: return "·";
        }
    }

    /// <summary>Rango de un minijuego del informe casando su categoría y nombre con el
    /// catálogo (para mostrar la insignia en la tabla de detalle).</summary>
    static int RankForMinigame(ReportModel m, MinigameStats s)
    {
        string want = NormName(s.Nombre);
        foreach (var dr in m.Rangos)
            if (dr.Cat == s.Cat)
                foreach (var sec in dr.Sectores)
                    if (NormName(sec.Tel) == want || NormName(sec.Juego) == want)
                        return sec.Rango;
        return 0;
    }

    static string NormName(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        s = s.Trim().ToLowerInvariant();
        return s.Replace("á", "a").Replace("é", "e").Replace("í", "i")
                .Replace("ó", "o").Replace("ú", "u").Replace("ñ", "n");
    }

    /// <summary>Renderiza una "medalla" circular con el color del rango (dibujo SVG donut
    /// para el acierto global).</summary>
    static string Gauge(float pct)
    {
        var inv = CultureInfo.InvariantCulture;
        float v = pct >= 0 ? Mathf.Clamp(pct, 0f, 100f) : 0f;
        const float R = 62f, C = 80f;
        float circ = 2f * Mathf.PI * R;
        float dash = circ * (v / 100f);
        string center = pct >= 0 ? v.ToString("0", inv) + "%" : "—";
        return
$@"<svg width=""176"" height=""176"" viewBox=""0 0 160 160"" style=""background:none;border:none"">
<circle class=""gtrack"" cx=""{C}"" cy=""{C}"" r=""{R}"" fill=""none"" stroke-width=""14""/>
<circle class=""garc"" cx=""{C}"" cy=""{C}"" r=""{R}"" fill=""none"" stroke-width=""14"" stroke-linecap=""round""
  stroke-dasharray=""{dash.ToString("0.#", inv)} {circ.ToString("0.#", inv)}"" transform=""rotate(-90 {C} {C})""/>
<text class=""gnum"" x=""{C}"" y=""{C + 2}"" font-family=""Georgia,serif"" font-size=""34"" font-weight=""700"" text-anchor=""middle"">{center}</text>
<text class=""gcap"" x=""{C}"" y=""{C + 26}"" font-size=""11"" text-anchor=""middle"">de acierto</text>
</svg>";
    }

    /// <summary>Gráfica de línea SVG con la evolución del % de acierto por día.</summary>
    static string EvolutionSvg(ReportModel m)
    {
        var pts = m.Evolucion;
        if (pts.Count < 2)
            return @"<p style=""color:var(--dim)"">Se necesitan al menos dos dias con partidas para dibujar la evolucion.</p>";

        var inv = CultureInfo.InvariantCulture;
        const int W = 1020, Ht = 300, padL = 50, padR = 20, padT = 20, padB = 40;
        float plotW = W - padL - padR, plotH = Ht - padT - padB;

        var sb = new StringBuilder();
        sb.Append($@"<svg class=""chart"" width=""100%"" viewBox=""0 0 {W} {Ht}"" xmlns=""http://www.w3.org/2000/svg"">");

        // Rejilla horizontal 0/25/50/75/100
        for (int v = 0; v <= 100; v += 25)
        {
            float y = padT + plotH * (1f - v / 100f);
            sb.Append($@"<line class=""grid"" x1=""{padL}"" y1=""{y.ToString("0.#", inv)}"" x2=""{W - padR}"" y2=""{y.ToString("0.#", inv)}"" stroke-width=""1""/>");
            sb.Append($@"<text class=""axis"" x=""{padL - 8}"" y=""{(y + 4).ToString("0.#", inv)}"" font-size=""11"" text-anchor=""end"">{v}%</text>");
        }

        // Polilínea
        var line = new StringBuilder();
        for (int i = 0; i < pts.Count; i++)
        {
            float x = padL + plotW * (pts.Count == 1 ? 0.5f : (float)i / (pts.Count - 1));
            float y = padT + plotH * (1f - Mathf.Clamp01(pts[i].Pct / 100f));
            line.Append(x.ToString("0.#", inv)).Append(',').Append(y.ToString("0.#", inv)).Append(' ');
        }
        sb.Append($@"<polyline class=""line"" points=""{line}"" fill=""none"" stroke-width=""3"" stroke-linejoin=""round""/>");

        // Puntos + etiquetas de fecha
        for (int i = 0; i < pts.Count; i++)
        {
            float x = padL + plotW * (pts.Count == 1 ? 0.5f : (float)i / (pts.Count - 1));
            float y = padT + plotH * (1f - Mathf.Clamp01(pts[i].Pct / 100f));
            sb.Append($@"<circle class=""pt"" cx=""{x.ToString("0.#", inv)}"" cy=""{y.ToString("0.#", inv)}"" r=""5""/>");
            sb.Append($@"<text class=""lbl"" x=""{x.ToString("0.#", inv)}"" y=""{(y - 10).ToString("0.#", inv)}"" font-size=""11"" text-anchor=""middle"">{pts[i].Pct.ToString("0", inv)}%</text>");
            // Fechas: se muestran hasta ~10 etiquetas para no solaparse
            int step = Mathf.Max(1, pts.Count / 10);
            if (i % step == 0 || i == pts.Count - 1)
                sb.Append($@"<text class=""axis"" x=""{x.ToString("0.#", inv)}"" y=""{Ht - 12}"" font-size=""10"" text-anchor=""middle"">{H(pts[i].Fecha.Substring(0, 5))}</text>");
        }

        sb.Append("</svg>");
        return sb.ToString();
    }

    static string H(string s) =>
        string.IsNullOrEmpty(s) ? "" :
        s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
}
