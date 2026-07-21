// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;

/// <summary>
/// Escritor mínimo de ficheros .xlsx (Office Open XML) sin dependencias externas:
/// solo System.IO.Compression (incluido en Unity 2022.3 / .NET Standard 2.1).
/// Soporta varias hojas, cadenas (inline), números y una fila de cabecera en negrita.
/// Los valores string se escriben como texto; int/float/double como número.
/// </summary>
public static class XlsxWriter
{
    public class Sheet
    {
        public string Name;
        public List<List<object>> Rows = new List<List<object>>();
        /// <summary>Anchos de columna (unidades Excel). Opcional.</summary>
        public float[] ColWidths;
        /// <summary>Congela la primera fila (cabecera) al desplazarse. Por defecto sí.</summary>
        public bool FreezeHeader = true;

        public Sheet(string name) { Name = name; }
        public void AddRow(params object[] cells) => Rows.Add(new List<object>(cells));
    }

    public static void Write(string path, List<Sheet> sheets)
    {
        using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write))
        using (var zip = new ZipArchive(fs, ZipArchiveMode.Create))
        {
            AddEntry(zip, "[Content_Types].xml", ContentTypesXml(sheets.Count));
            AddEntry(zip, "_rels/.rels", RelsXml());
            AddEntry(zip, "xl/workbook.xml", WorkbookXml(sheets));
            AddEntry(zip, "xl/_rels/workbook.xml.rels", WorkbookRelsXml(sheets.Count));
            AddEntry(zip, "xl/styles.xml", StylesXml());
            for (int i = 0; i < sheets.Count; i++)
                AddEntry(zip, $"xl/worksheets/sheet{i + 1}.xml", SheetXml(sheets[i]));
        }
    }

    static void AddEntry(ZipArchive zip, string name, string content)
    {
        var entry = zip.CreateEntry(name, System.IO.Compression.CompressionLevel.Optimal);
        using (var w = new StreamWriter(entry.Open(), new UTF8Encoding(false)))
            w.Write(content);
    }

    // ------------------------------------------------ Partes fijas

    static string ContentTypesXml(int sheetCount)
    {
        var sb = new StringBuilder();
        sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
        sb.Append("<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">");
        sb.Append("<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>");
        sb.Append("<Default Extension=\"xml\" ContentType=\"application/xml\"/>");
        sb.Append("<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>");
        sb.Append("<Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/>");
        for (int i = 1; i <= sheetCount; i++)
            sb.Append($"<Override PartName=\"/xl/worksheets/sheet{i}.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>");
        sb.Append("</Types>");
        return sb.ToString();
    }

    static string RelsXml() =>
        "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
        "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
        "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/>" +
        "</Relationships>";

    static string WorkbookXml(List<Sheet> sheets)
    {
        var sb = new StringBuilder();
        sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
        sb.Append("<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" ");
        sb.Append("xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets>");
        for (int i = 0; i < sheets.Count; i++)
            sb.Append($"<sheet name=\"{XmlEscape(SafeSheetName(sheets[i].Name))}\" sheetId=\"{i + 1}\" r:id=\"rId{i + 1}\"/>");
        sb.Append("</sheets></workbook>");
        return sb.ToString();
    }

    static string WorkbookRelsXml(int sheetCount)
    {
        var sb = new StringBuilder();
        sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
        sb.Append("<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">");
        for (int i = 1; i <= sheetCount; i++)
            sb.Append($"<Relationship Id=\"rId{i}\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet{i}.xml\"/>");
        sb.Append($"<Relationship Id=\"rId{sheetCount + 1}\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/>");
        sb.Append("</Relationships>");
        return sb.ToString();
    }

    /// <summary>Estilos: normal (s=0) y cabecera con banda de color + texto blanco (s=1).</summary>
    static string StylesXml() =>
        "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
        "<styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">" +
        "<fonts count=\"2\">" +
        "<font><sz val=\"11\"/><name val=\"Calibri\"/></font>" +
        "<font><b/><sz val=\"11\"/><color rgb=\"FFFFFFFF\"/><name val=\"Calibri\"/></font>" +
        "</fonts>" +
        "<fills count=\"3\">" +
        "<fill><patternFill patternType=\"none\"/></fill>" +
        "<fill><patternFill patternType=\"gray125\"/></fill>" +
        "<fill><patternFill patternType=\"solid\"><fgColor rgb=\"FF2A3560\"/><bgColor indexed=\"64\"/></patternFill></fill>" +
        "</fills>" +
        "<borders count=\"1\"><border/></borders>" +
        "<cellStyleXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/></cellStyleXfs>" +
        "<cellXfs count=\"2\">" +
        "<xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\"/>" +
        "<xf numFmtId=\"0\" fontId=\"1\" fillId=\"2\" borderId=\"0\" xfId=\"0\" applyFont=\"1\" applyFill=\"1\" applyAlignment=\"1\"><alignment vertical=\"center\"/></xf>" +
        "</cellXfs>" +
        "<cellStyles count=\"1\"><cellStyle name=\"Normal\" xfId=\"0\" builtinId=\"0\"/></cellStyles>" +
        "</styleSheet>";

    // ------------------------------------------------ Hojas

    static string SheetXml(Sheet sheet)
    {
        var sb = new StringBuilder();
        sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
        sb.Append("<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">");

        // Congelar la fila de cabecera
        if (sheet.FreezeHeader && sheet.Rows.Count > 1)
            sb.Append("<sheetViews><sheetView workbookViewId=\"0\">" +
                      "<pane ySplit=\"1\" topLeftCell=\"A2\" activePane=\"bottomLeft\" state=\"frozen\"/>" +
                      "<selection pane=\"bottomLeft\" activeCell=\"A2\" sqref=\"A2\"/>" +
                      "</sheetView></sheetViews>");

        // Anchos de columna
        if (sheet.ColWidths != null && sheet.ColWidths.Length > 0)
        {
            sb.Append("<cols>");
            for (int i = 0; i < sheet.ColWidths.Length; i++)
                sb.Append($"<col min=\"{i + 1}\" max=\"{i + 1}\" width=\"" +
                          $"{sheet.ColWidths[i].ToString("0.##", CultureInfo.InvariantCulture)}\" customWidth=\"1\"/>");
            sb.Append("</cols>");
        }

        sb.Append("<sheetData>");

        for (int r = 0; r < sheet.Rows.Count; r++)
        {
            var row = sheet.Rows[r];
            sb.Append($"<row r=\"{r + 1}\">");
            for (int c = 0; c < row.Count; c++)
            {
                object v = row[c];
                if (v == null) continue;
                string cellRef = ColName(c) + (r + 1);
                string style = r == 0 ? " s=\"1\"" : "";   // primera fila en negrita

                if (v is int || v is long || v is float || v is double || v is decimal)
                {
                    string num = Convert.ToDouble(v).ToString("0.####", CultureInfo.InvariantCulture);
                    sb.Append($"<c r=\"{cellRef}\"{style}><v>{num}</v></c>");
                }
                else
                {
                    sb.Append($"<c r=\"{cellRef}\"{style} t=\"inlineStr\"><is><t xml:space=\"preserve\">{XmlEscape(v.ToString())}</t></is></c>");
                }
            }
            sb.Append("</row>");
        }

        sb.Append("</sheetData></worksheet>");
        return sb.ToString();
    }

    static string ColName(int index)
    {
        string name = "";
        index++;
        while (index > 0)
        {
            int rem = (index - 1) % 26;
            name = (char)('A' + rem) + name;
            index = (index - 1) / 26;
        }
        return name;
    }

    static string SafeSheetName(string name)
    {
        if (string.IsNullOrEmpty(name)) return "Hoja";
        foreach (char bad in new[] { '[', ']', ':', '*', '?', '/', '\\' })
            name = name.Replace(bad, ' ');
        return name.Length > 31 ? name.Substring(0, 31) : name;
    }

    static string XmlEscape(string s) =>
        s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;")
         .Replace("\"", "&quot;").Replace("'", "&apos;");
}
