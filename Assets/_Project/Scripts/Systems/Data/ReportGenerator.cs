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

    class ReportModel
    {
        public ProfileData Profile;
        public List<SessionData> Sessions;
        public List<MinigameResultData> Results;
        public List<CategoryStats> Categorias = new List<CategoryStats>();
        public List<MinigameStats> Minijuegos = new List<MinigameStats>();
        public List<DayPoint> Evolucion = new List<DayPoint>();
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
        sheets.Add(res);

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

    static void WriteHtml(ReportModel m, string path)
    {
        var p = m.Profile;
        var sb = new StringBuilder();
        var inv = CultureInfo.InvariantCulture;

        sb.Append(@"<!DOCTYPE html><html lang=""es""><head><meta charset=""utf-8"">
<title>Informe AttentiON - ").Append(H(p.nombre)).Append(@"</title>
<style>
 :root{--bg:#0a0f1e;--panel:#141b30;--panel2:#1a2340;--acc:#4da6ff;--good:#2ecc8f;--warn:#f28c1e;--bad:#e6394a;--dim:#8fa3c0;--txt:#eef3fb}
 *{box-sizing:border-box;margin:0;padding:0}
 body{font-family:'Segoe UI',Arial,sans-serif;background:var(--bg);color:var(--txt);padding:32px;max-width:1100px;margin:0 auto}
 h1{font-size:26px;letter-spacing:2px} h2{font-size:18px;color:var(--acc);margin:28px 0 12px;letter-spacing:1px;border-bottom:2px solid var(--panel2);padding-bottom:6px}
 .head{display:flex;justify-content:space-between;align-items:center;border-bottom:3px solid var(--acc);padding-bottom:14px}
 .head .sub{color:var(--dim);font-size:13px;margin-top:4px}
 .cards{display:flex;gap:14px;margin-top:18px;flex-wrap:wrap}
 .card{background:var(--panel);border-radius:10px;padding:14px 20px;flex:1;min-width:150px;border-top:3px solid var(--acc)}
 .card .v{font-size:26px;font-weight:bold} .card .k{font-size:12px;color:var(--dim);margin-top:2px}
 table{width:100%;border-collapse:collapse;font-size:13px;margin-top:6px}
 th{background:var(--panel2);color:var(--acc);text-align:left;padding:8px 10px;font-size:12px}
 td{padding:7px 10px;border-bottom:1px solid var(--panel2)}
 tr:nth-child(even) td{background:rgba(255,255,255,.02)}
 .bar-row{display:flex;align-items:center;gap:10px;margin:8px 0}
 .bar-lbl{width:200px;font-size:13px;color:var(--dim)} .bar-track{flex:1;background:var(--panel2);border-radius:6px;height:22px;overflow:hidden}
 .bar-fill{height:100%;border-radius:6px;display:flex;align-items:center;justify-content:flex-end;padding-right:8px;font-size:12px;font-weight:bold;color:#04101f}
 .interp{background:var(--panel);border-left:4px solid var(--good);border-radius:8px;padding:16px 20px;margin-top:10px}
 .interp p{margin:6px 0;font-size:14px;line-height:1.5}
 .disclaimer{margin-top:26px;color:var(--dim);font-size:12px;border-top:1px solid var(--panel2);padding-top:12px}
 svg{background:var(--panel);border-radius:10px;margin-top:6px}
 @media print{body{background:#fff;color:#111} .card,.interp,svg{background:#f4f6fa;border-color:#ccc} th{background:#e6ecf5;color:#036} td{border-color:#ddd} :root{--dim:#555;--acc:#0366cc}}
</style></head><body>");

        // Cabecera
        sb.Append(@"<div class=""head""><div><h1>INFORME AttentiON</h1>
<div class=""sub"">Videojuego educativo para el entrenamiento de funciones ejecutivas &middot; Documento generado el ")
          .Append(DateTime.Now.ToString("dd/MM/yyyy 'a las' HH:mm")).Append(@"</div></div>
<div style=""text-align:right""><div style=""font-size:22px;font-weight:bold"">").Append(H(p.nombre)).Append(@"</div>
<div class=""sub"">Edad: ").Append(H(p.EdadTramoLabel))
          .Append(" &middot; Dificultad: ").Append(H(MinigameResultData.DifficultyDisplayName(p.DificultadRecomendada)))
          .Append(@"</div></div></div>");

        // Tarjetas resumen
        sb.Append(@"<div class=""cards"">");
        Card(sb, m.Sessions.Count.ToString(), "Sesiones");
        Card(sb, m.MinutosTotales.ToString("0", inv) + " min", "Tiempo total");
        Card(sb, m.Results.Count.ToString(), "Partidas");
        Card(sb, m.Results.Count(r => r.completado).ToString(), "Completadas");
        Card(sb, m.PctGlobal >= 0 ? m.PctGlobal.ToString("0", inv) + "%" : "-", "Acierto global");
        sb.Append("</div>");

        // Gráfica de barras por categoría
        sb.Append("<h2>RENDIMIENTO POR CATEGORIA (% de acierto)</h2>");
        string[] colors = { "#a855f7", "#f28c1e", "#2ecc8f", "#facc15", "#4da6ff" };
        if (m.Categorias.Any(c => c.Pct >= 0))
        {
            foreach (var c in m.Categorias)
            {
                string col = colors[(int)c.Cat % colors.Length];
                float pct = Mathf.Max(0f, c.Pct);
                string label = c.Pct >= 0 ? c.Pct.ToString("0", inv) + "%" : "sin datos";
                float width = c.Pct >= 0 ? Mathf.Clamp(pct, 6f, 100f) : 6f;
                sb.Append($@"<div class=""bar-row""><div class=""bar-lbl"">{H(c.Nombre)}</div>
<div class=""bar-track""><div class=""bar-fill"" style=""width:{width.ToString("0", inv)}%;background:{col}"">{label}</div></div></div>");
            }
        }
        else sb.Append(@"<p style=""color:var(--dim)"">Aun no hay datos de rondas (aciertos/errores) registrados.</p>");

        // Gráfica de evolución (SVG)
        sb.Append("<h2>EVOLUCION TEMPORAL (% de acierto por dia)</h2>");
        sb.Append(EvolutionSvg(m));

        // Tabla por categoría
        sb.Append("<h2>DETALLE POR CATEGORIA</h2><table><tr><th>Categoria</th><th>Partidas</th><th>Completadas</th><th>% acierto</th><th>T. reaccion medio</th><th>Tendencia</th></tr>");
        foreach (var c in m.Categorias)
            sb.Append($"<tr><td>{H(c.Nombre)}</td><td>{c.Partidas}</td><td>{c.Completadas}</td>" +
                      $"<td>{(c.Pct >= 0 ? c.Pct.ToString("0.0", inv) + "%" : "-")}</td>" +
                      $"<td>{(c.RtMs >= 0 ? c.RtMs.ToString("0", inv) + " ms" : "-")}</td>" +
                      $"<td>{H(c.Tendencia)}</td></tr>");
        sb.Append("</table>");

        // Tabla por minijuego
        sb.Append("<h2>DETALLE POR MINIJUEGO</h2><table><tr><th>Minijuego</th><th>Categoria</th><th>Partidas</th><th>Completadas</th><th>% acierto</th><th>T. reaccion</th><th>Mejor punt.</th><th>Ultima vez</th></tr>");
        foreach (var s in m.Minijuegos)
            sb.Append($"<tr><td>{H(s.Nombre)}</td><td>{H(MinigameResultData.CategoryDisplayName(s.Cat))}</td>" +
                      $"<td>{s.Partidas}</td><td>{s.Completadas}</td>" +
                      $"<td>{(s.Pct >= 0 ? s.Pct.ToString("0.0", inv) + "%" : "-")}</td>" +
                      $"<td>{(s.RtMs >= 0 ? s.RtMs.ToString("0", inv) + " ms" : "-")}</td>" +
                      $"<td>{s.MejorPuntuacion}</td><td>{DataUtils.TicksToLocalDate(s.UltimaVezTicks)}</td></tr>");
        sb.Append("</table>");

        // Sesiones
        sb.Append("<h2>HISTORICO DE SESIONES</h2><table><tr><th>Inicio</th><th>Duracion</th><th>Dificultad</th><th>Partidas</th></tr>");
        foreach (var s in m.Sessions)
            sb.Append($"<tr><td>{DataUtils.TicksToLocalDateTime(s.inicioUtcTicks)}</td>" +
                      $"<td>{s.DuracionMin.ToString("0.0", inv)} min</td>" +
                      $"<td>{H(MinigameResultData.DifficultyDisplayName((DifficultyLevel)s.dificultad))}</td>" +
                      $"<td>{m.Results.Count(r => r.sessionId == s.id)}</td></tr>");
        sb.Append("</table>");

        // Interpretación
        sb.Append(@"<h2>LECTURA DE LOS DATOS (automatica, no clinica)</h2><div class=""interp"">");
        foreach (string linea in m.Interpretacion) sb.Append("<p>").Append(H(linea)).Append("</p>");
        sb.Append("</div>");

        sb.Append(@"<div class=""disclaimer"">AttentiON &middot; Informe generado automaticamente y guardado unicamente en este equipo (sin conexion). " +
                  "Esta herramienta es un apoyo complementario para el seguimiento del entrenamiento de funciones ejecutivas y no sustituye " +
                  "la valoracion de un profesional. Para exportar a PDF: Imprimir &rarr; Guardar como PDF.</div>");
        sb.Append("</body></html>");

        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
    }

    static void Card(StringBuilder sb, string value, string label) =>
        sb.Append($@"<div class=""card""><div class=""v"">{H(value)}</div><div class=""k"">{H(label)}</div></div>");

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
        sb.Append($@"<svg width=""100%"" viewBox=""0 0 {W} {Ht}"" xmlns=""http://www.w3.org/2000/svg"">");

        // Rejilla horizontal 0/25/50/75/100
        for (int v = 0; v <= 100; v += 25)
        {
            float y = padT + plotH * (1f - v / 100f);
            sb.Append($@"<line x1=""{padL}"" y1=""{y.ToString("0.#", inv)}"" x2=""{W - padR}"" y2=""{y.ToString("0.#", inv)}"" stroke=""#243154"" stroke-width=""1""/>");
            sb.Append($@"<text x=""{padL - 8}"" y=""{(y + 4).ToString("0.#", inv)}"" fill=""#8fa3c0"" font-size=""11"" text-anchor=""end"">{v}%</text>");
        }

        // Polilínea
        var line = new StringBuilder();
        for (int i = 0; i < pts.Count; i++)
        {
            float x = padL + plotW * (pts.Count == 1 ? 0.5f : (float)i / (pts.Count - 1));
            float y = padT + plotH * (1f - Mathf.Clamp01(pts[i].Pct / 100f));
            line.Append(x.ToString("0.#", inv)).Append(',').Append(y.ToString("0.#", inv)).Append(' ');
        }
        sb.Append($@"<polyline points=""{line}"" fill=""none"" stroke=""#4da6ff"" stroke-width=""3"" stroke-linejoin=""round""/>");

        // Puntos + etiquetas de fecha
        for (int i = 0; i < pts.Count; i++)
        {
            float x = padL + plotW * (pts.Count == 1 ? 0.5f : (float)i / (pts.Count - 1));
            float y = padT + plotH * (1f - Mathf.Clamp01(pts[i].Pct / 100f));
            sb.Append($@"<circle cx=""{x.ToString("0.#", inv)}"" cy=""{y.ToString("0.#", inv)}"" r=""5"" fill=""#2ecc8f""/>");
            sb.Append($@"<text x=""{x.ToString("0.#", inv)}"" y=""{(y - 10).ToString("0.#", inv)}"" fill=""#eef3fb"" font-size=""11"" text-anchor=""middle"">{pts[i].Pct.ToString("0", inv)}%</text>");
            // Fechas: se muestran hasta ~10 etiquetas para no solaparse
            int step = Mathf.Max(1, pts.Count / 10);
            if (i % step == 0 || i == pts.Count - 1)
                sb.Append($@"<text x=""{x.ToString("0.#", inv)}"" y=""{Ht - 12}"" fill=""#8fa3c0"" font-size=""10"" text-anchor=""middle"">{H(pts[i].Fecha.Substring(0, 5))}</text>");
        }

        sb.Append("</svg>");
        return sb.ToString();
    }

    static string H(string s) =>
        string.IsNullOrEmpty(s) ? "" :
        s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
}
