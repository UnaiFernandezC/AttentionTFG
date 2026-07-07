// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Área del tutor (tras PIN): resumen de datos por niño, generación de informes
/// (Excel + HTML + CSV), borrado de datos del menor y aviso de privacidad.
/// Todo offline y construido por código.
/// </summary>
public class TutorPanel : MonoBehaviour
{
    GameObject _canvasGO;
    RectTransform _card;
    RectTransform _content;
    ProfileData _selected;
    TextMeshProUGUI _statusT;

    // Modo profesional: buscador + lista con scroll
    string _searchQuery = "";
    RectTransform _listContent;

    static TutorPanel _current;

    /// <summary>True mientras el área del tutor está abierta (bloquea ESC).</summary>
    public static bool IsOpen => _current != null;

    public static void Show(ProfileData preselected = null)
    {
        if (_current != null) return;
        KidUI.EnsureEventSystem();
        var go = new GameObject("TutorPanel");
        _current = go.AddComponent<TutorPanel>();
        _current._selected = preselected;
        _current.Build();
    }

    static bool _appQuitting;
    void OnApplicationQuit() => _appQuitting = true;

    void OnDestroy()
    {
        if (_current == this) _current = null;
        // Si la pantalla de perfiles sigue abierta detrás, refresca sus tarjetas
        // (por si se borró o creó algún perfil desde aquí).
        if (!_appQuitting) ProfileScreenController.RefreshIfOpen();
    }

    void Build()
    {
        var cv = KidUI.MakeCanvas("TutorCanvas", 850, transform);
        _canvasGO = cv.gameObject;
        var R = cv.GetComponent<RectTransform>();

        // Fondo espacial opaco (no se ve la pantalla de detrás)
        KidUI.BuildSpaceBackground(R, withPlanet: false);

        _card = KidUI.RoundImg(R, "Card", new Color(0.055f, 0.075f, 0.15f, 0.97f),
                               new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                               Vector2.zero, new Vector2(1250f, 820f), 0.7f);
        var topEdge = KidUI.RoundImg(_card, "Top", KidUI.WARN,
                                     new Vector2(0.04f, 0.985f), new Vector2(0.22f, 0.994f),
                                     Vector2.zero, Vector2.zero, 4f);
        topEdge.GetComponent<Image>().raycastTarget = false;

        var title = KidUI.Txt(_card, "Title", "AREA DEL TUTOR", Color.white, 34,
                              new Vector2(0.04f, 0.91f), new Vector2(0.70f, 0.99f));
        title.fontStyle = FontStyles.Bold;
        title.alignment = TextAlignmentOptions.MidlineLeft;
        title.characterSpacing = 3f;

        KidUI.Btn(_card, "Cerrar", KidUI.BAD,
                  new Vector2(0.86f, 0.92f), new Vector2(0.97f, 0.98f),
                  () => Destroy(gameObject), 18f);

        // Banda inferior bien separada: privacidad arriba, mensajes de estado debajo
        // (antes se pisaban entre sí).
        var priv = KidUI.Txt(_card, "Privacidad",
            "Privacidad: todos los datos se guardan UNICAMENTE en este ordenador (sin nube ni internet). " +
            "Los informes son una herramienta complementaria de seguimiento y NO constituyen un diagnostico clinico. " +
            "Puede borrar todos los datos del menor en cualquier momento con el boton 'Borrar datos'.",
            KidUI.DIM, 13,
            new Vector2(0.04f, 0.062f), new Vector2(0.96f, 0.118f));
        priv.alignment = TextAlignmentOptions.TopLeft;
        priv.lineSpacing = 14f;          // interlineado más cómodo

        _statusT = KidUI.Txt(_card, "Status", "", KidUI.GOOD, 17,
                             new Vector2(0.04f, 0.006f), new Vector2(0.96f, 0.056f));

        var contentGO = new GameObject("Content");
        contentGO.transform.SetParent(_card, false);
        _content = contentGO.AddComponent<RectTransform>();
        _content.anchorMin = new Vector2(0, 0.125f);
        _content.anchorMax = new Vector2(1, 0.91f);
        _content.sizeDelta = Vector2.zero;

        RefreshContent();
        // Fondo opaco al instante (sin fundido de canvas) + entrada animada de la tarjeta.
        UITween.PopIn(_card, 0.3f, 0.9f);
    }

    void RefreshContent()
    {
        foreach (Transform t in _content) Destroy(t.gameObject);

        var pm = ProfileManager.Instance;
        var profiles = pm != null ? pm.GetProfiles() : new List<ProfileData>();

        if (_selected != null && profiles.All(p => p.id != _selected.id))
            _selected = null;
        if (_selected == null && profiles.Count == 1)
            _selected = profiles[0];

        // ---- Columna izquierda: lista de perfiles
        bool pro = pm != null && pm.ProfessionalMode;

        var lblP = KidUI.Txt(_content, "LblPerfiles",
                             pro ? $"PERFILES ({profiles.Count})" : "PERFILES", KidUI.DIM, 20,
                             new Vector2(0.04f, 0.92f), new Vector2(0.30f, 0.99f));
        lblP.fontStyle = FontStyles.Bold;

        // Toggle modo profesional (gabinetes): perfiles ilimitados + búsqueda + export por lote
        var proBtn = KidUI.Btn(_content,
                               pro ? "Modo profesional: ACTIVADO" : "Modo profesional: apagado",
                               pro ? new Color(0.13f, 0.42f, 0.36f) : KidUI.BTNC,
                               new Vector2(0.02f, 0.845f), new Vector2(0.31f, 0.91f),
                               ToggleProfessional, 14f);
        if (!pro) proBtn.GetComponentInChildren<TextMeshProUGUI>().color = KidUI.DIM;

        // Buscador (modo profesional o listas largas)
        float listTop = 0.835f;
        if (pro || profiles.Count > 6)
        {
            var search = KidUI.InputField(_content, "Buscar nombre...",
                                          new Vector2(0.02f, 0.765f), new Vector2(0.31f, 0.83f), 20);
            search.text = _searchQuery;
            search.onValueChanged.AddListener(v => { _searchQuery = v ?? ""; RebuildProfileList(profiles); });
            listTop = 0.755f;
        }
        else _searchQuery = "";

        // Lista con scroll (soporta decenas de perfiles)
        float listBottom = pro ? 0.205f : 0.13f;
        var viewGO = new GameObject("ProfListView");
        viewGO.transform.SetParent(_content, false);
        var viewRT = viewGO.AddComponent<RectTransform>();
        viewRT.anchorMin = new Vector2(0.02f, listBottom);
        viewRT.anchorMax = new Vector2(0.31f, listTop);
        viewRT.sizeDelta = Vector2.zero;
        viewGO.AddComponent<Image>().color = new Color(0, 0, 0, 0.18f);
        viewGO.AddComponent<RectMask2D>();

        var listGO = new GameObject("ProfListContent");
        listGO.transform.SetParent(viewRT, false);
        _listContent = listGO.AddComponent<RectTransform>();
        _listContent.anchorMin = new Vector2(0, 1);
        _listContent.anchorMax = new Vector2(1, 1);
        _listContent.pivot = new Vector2(0.5f, 1f);

        var sr = viewGO.AddComponent<ScrollRect>();
        sr.content = _listContent;
        sr.viewport = viewRT;
        sr.horizontal = false;
        sr.vertical = true;
        sr.movementType = ScrollRect.MovementType.Clamped;
        sr.scrollSensitivity = 30f;

        RebuildProfileList(profiles);

        // Exportar TODOS los informes (solo modo profesional)
        if (pro && profiles.Count > 0)
        {
            KidUI.Btn(_content, "EXPORTAR TODOS LOS INFORMES", KidUI.GOOD,
                      new Vector2(0.02f, 0.125f), new Vector2(0.31f, 0.195f),
                      ExportAll, 13f);
        }

        // ---- Zona de peligro: borrar TODA la base de datos (re-pide el PIN)
        if (profiles.Count > 0)
        {
            KidUI.Btn(_content, "Borrar TODA la base de datos", KidUI.BAD,
                      new Vector2(0.02f, 0.03f), new Vector2(0.31f, 0.115f),
                      ConfirmDeleteAll, 14f);
        }

        // ---- Columna derecha: detalle del perfil seleccionado
        if (_selected == null)
        {
            KidUI.Txt(_content, "Pick", "Selecciona un perfil para ver sus datos\ny generar su informe.",
                      KidUI.DIM, 22, new Vector2(0.35f, 0.45f), new Vector2(0.95f, 0.65f));
        }
        else
        {
            BuildDetail(_selected);
        }

        // (El aviso de privacidad vive ahora a nivel de tarjeta, en Build(),
        //  para que no se pise con los mensajes de estado.)
    }

    void BuildDetail(ProfileData p)
    {
        var store = ProfileManager.Store;
        var sessions = store != null ? store.GetSessions(p.id) : new List<SessionData>();
        var results  = store != null ? store.GetResults(p.id)  : new List<MinigameResultData>();

        var head = KidUI.Txt(_content, "DetName", p.nombre.ToUpper(), Color.white, 30,
                             new Vector2(0.35f, 0.91f), new Vector2(0.78f, 0.99f));
        head.fontStyle = FontStyles.Bold;
        head.alignment = TextAlignmentOptions.MidlineLeft;

        // Chips de edad y dificultad
        var chipEdad = KidUI.RoundImg(_content, "ChipEdad",
                                      new Color(KidUI.ACCENT.r, KidUI.ACCENT.g, KidUI.ACCENT.b, 0.16f),
                                      new Vector2(0.35f, 0.845f), new Vector2(0.50f, 0.905f),
                                      Vector2.zero, Vector2.zero, 2.2f);
        KidUI.Txt(chipEdad, "T", p.EdadTramoLabel, KidUI.ACCENT, 17,
                  Vector2.zero, Vector2.one);
        var chipDif = KidUI.RoundImg(_content, "ChipDif",
                                     new Color(KidUI.WARN.r, KidUI.WARN.g, KidUI.WARN.b, 0.16f),
                                     new Vector2(0.52f, 0.845f), new Vector2(0.72f, 0.905f),
                                     Vector2.zero, Vector2.zero, 2.2f);
        KidUI.Txt(chipDif, "T", MinigameResultData.DifficultyDisplayName(p.DificultadActiva),
                  KidUI.WARN, 17, Vector2.zero, Vector2.one);

        // ---- Tabla de resumen general
        double minutos = sessions.Sum(s => s.DuracionMin);
        int completados = results.Count(r => r.completado);
        var conRondas = results.Where(r => r.Intentos > 0).ToList();
        string pctGlobal = conRondas.Count > 0
            ? conRondas.Average(r => r.PorcentajeAcierto).ToString("0") + "%"
            : "-";

        KidUI.Table(_content,
            new Vector2(0.35f, 0.685f), new Vector2(0.95f, 0.835f),
            new[] { "Sesiones", "Tiempo total", "Partidas", "Completadas", "Acierto" },
            new List<string[]>
            {
                new[]
                {
                    sessions.Count.ToString(),
                    minutos.ToString("0") + " min",
                    results.Count.ToString(),
                    completados.ToString(),
                    pctGlobal
                }
            },
            null, 18f);

        // ---- Tabla por categoría (siempre las 5 filas)
        var lbl = KidUI.Txt(_content, "LblCat", "RENDIMIENTO POR CATEGORÍA", KidUI.DIM, 16,
                            new Vector2(0.35f, 0.635f), new Vector2(0.95f, 0.675f));
        lbl.alignment = TextAlignmentOptions.MidlineLeft;
        lbl.fontStyle = FontStyles.Bold;
        lbl.characterSpacing = 2f;

        var catRows = new List<string[]>();
        for (int cat = 0; cat < 5; cat++)
        {
            var rs = results.Where(r => r.categoria == cat).ToList();
            var rr = rs.Where(r => r.Intentos > 0).ToList();
            var rts = rs.Where(r => r.tiempoReaccionMedioMs > 0).ToList();
            catRows.Add(new[]
            {
                MinigameResultData.CategoryDisplayName((MinigameCategory)cat),
                rs.Count > 0 ? rs.Count.ToString() : "-",
                rs.Count > 0 ? rs.Count(r => r.completado).ToString() : "-",
                rr.Count > 0 ? rr.Average(r => r.PorcentajeAcierto).ToString("0") + "%" : "-",
                rts.Count > 0 ? rts.Average(r => r.tiempoReaccionMedioMs).ToString("0") + " ms" : "-"
            });
        }
        KidUI.Table(_content,
            new Vector2(0.35f, 0.335f), new Vector2(0.95f, 0.630f),
            new[] { "Categoría", "Partidas", "Compl.", "% acierto", "T. reacción" },
            catRows,
            new float[] { 2.1f, 1f, 1f, 1.1f, 1.2f }, 17f);

        // ---- Acciones
        KidUI.Btn(_content, "DESCARGAR INFORME  (Excel + HTML + CSV)", KidUI.GOOD,
                  new Vector2(0.35f, 0.215f), new Vector2(0.95f, 0.305f),
                  () => GenerateReport(p), 21f);

        KidUI.Btn(_content, "Borrar datos del menor", KidUI.BAD,
                  new Vector2(0.35f, 0.10f), new Vector2(0.63f, 0.185f),
                  () => ConfirmDelete(p), 18f);

        KidUI.Btn(_content, "Cambiar PIN", KidUI.BTNC,
                  new Vector2(0.67f, 0.10f), new Vector2(0.95f, 0.185f),
                  ChangePin, 18f);
    }

    /// <summary>Rellena la lista scrolleable de perfiles (filtrada por el buscador).</summary>
    void RebuildProfileList(List<ProfileData> profiles)
    {
        if (_listContent == null) return;
        foreach (Transform t in _listContent) Destroy(t.gameObject);

        string q = Normalize(_searchQuery);
        var filtered = string.IsNullOrEmpty(q)
            ? profiles
            : profiles.Where(p => Normalize(p.nombre).Contains(q)).ToList();

        const float ROW_H = 56f, GAP = 6f;
        _listContent.sizeDelta = new Vector2(0, filtered.Count * (ROW_H + GAP) + GAP);

        if (filtered.Count == 0)
        {
            var none = new GameObject("None");
            none.transform.SetParent(_listContent, false);
            var nrt = none.AddComponent<RectTransform>();
            nrt.anchorMin = new Vector2(0, 1); nrt.anchorMax = new Vector2(1, 1);
            nrt.pivot = new Vector2(0.5f, 1f);
            nrt.anchoredPosition = new Vector2(0, -GAP);
            nrt.sizeDelta = new Vector2(0, ROW_H);
            var t = none.AddComponent<TextMeshProUGUI>();
            t.text = profiles.Count == 0 ? "No hay perfiles todavia." : "Sin resultados.";
            t.color = KidUI.DIM; t.fontSize = 16;
            t.alignment = TextAlignmentOptions.Center;
            return;
        }

        for (int i = 0; i < filtered.Count; i++)
        {
            var p = filtered[i];
            bool sel = _selected != null && _selected.id == p.id;

            var row = new GameObject("Row_" + p.nombre);
            row.transform.SetParent(_listContent, false);
            var rt = row.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0, -GAP - i * (ROW_H + GAP));
            rt.sizeDelta = new Vector2(-10f, ROW_H);

            var img = row.AddComponent<Image>();
            img.color = sel ? KidUI.ACCENT : KidUI.BTNC;
            img.sprite = KidUI.RoundedSprite;
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 1.6f;

            var label = KidUI.Txt(rt, "T", p.nombre + "  (" + p.EdadTramoLabel + ")",
                                  Color.white, 16, new Vector2(0.05f, 0f), new Vector2(0.95f, 1f));
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.fontStyle = FontStyles.Bold;

            var btn = row.AddComponent<Button>();
            btn.targetGraphic = img;
            var captured = p;
            btn.onClick.AddListener(() =>
            {
                if (UIAudioManager.Instance != null) UIAudioManager.Instance.PlayClick();
                _selected = captured;
                _pendingDelete = null;
                RefreshContent();
            });
            ButtonJuice.Attach(row);
        }
    }

    static string Normalize(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.ToLowerInvariant()
                .Replace("á", "a").Replace("é", "e").Replace("í", "i")
                .Replace("ó", "o").Replace("ú", "u").Replace("ñ", "n").Trim();
    }

    void ToggleProfessional()
    {
        var pm = ProfileManager.Instance;
        if (pm == null) return;
        bool nuevo = !pm.ProfessionalMode;
        pm.SetProfessionalMode(nuevo);
        _statusT.color = nuevo ? KidUI.GOOD : KidUI.DIM;
        _statusT.text = nuevo
            ? "Modo profesional ACTIVADO: perfiles ilimitados, buscador y exportación por lote."
            : "Modo profesional apagado (uso familiar: máximo 5 perfiles).";
        RefreshContent();
    }

    /// <summary>Exportación por lote (gabinetes): genera el informe de TODOS los
    /// perfiles sin abrir cada uno, y al terminar abre la carpeta de informes.</summary>
    void ExportAll()
    {
        var pm = ProfileManager.Instance;
        if (pm == null) return;
        var profiles = pm.GetProfiles();
        if (profiles.Count == 0) return;

        _statusT.color = KidUI.ACCENT;
        _statusT.text = "Generando " + profiles.Count + " informes...";

        int ok = 0;
        string folder = "";
        foreach (var p in profiles)
        {
            string f;
            if (ReportGenerator.Generate(p, out f, openAfter: false))
            {
                ok++;
                folder = f;
            }
        }

        if (ok > 0)
        {
            _statusT.color = KidUI.GOOD;
            _statusT.text = $"{ok}/{profiles.Count} informes generados en: {folder}";
            Application.OpenURL("file:///" + folder.Replace("\\", "/"));
            GameFeel.PlaySuccess();
        }
        else
        {
            _statusT.color = KidUI.BAD;
            _statusT.text = "No se pudo generar ningún informe (ver consola/log).";
        }
    }

    void GenerateReport(ProfileData p)
    {
        _statusT.color = KidUI.ACCENT;
        _statusT.text = "Generando informe...";
        string folder;
        bool ok = ReportGenerator.GenerateAndOpen(p, out folder);
        if (ok)
        {
            _statusT.color = KidUI.GOOD;
            _statusT.text = "Informe guardado en: " + folder;
        }
        else
        {
            _statusT.color = KidUI.BAD;
            _statusT.text = "No se pudo generar el informe (ver consola/log).";
        }
    }

    string _pendingDelete;

    void ConfirmDelete(ProfileData p)
    {
        // Confirmación en dos pasos dentro del propio panel.
        if (_pendingDelete == p.id)
        {
            _pendingDelete = null;
            if (ProfileManager.Instance != null)
                ProfileManager.Instance.DeleteProfileData(p.id);
            _selected = null;
            _statusT.color = KidUI.GOOD;
            _statusT.text = $"Datos de {p.nombre} eliminados.";
            RefreshContent();
            return;
        }
        _pendingDelete = p.id;
        _statusT.color = KidUI.BAD;
        _statusT.text = $"¿Seguro? Se borraran TODOS los datos de {p.nombre}. " +
                        "Pulsa otra vez 'Borrar datos' para confirmar.";
    }

    /// <summary>Borra TODA la base de datos (todos los perfiles). Re-pide el PIN
    /// establecido antes de ejecutar el borrado, por ser una acción irreversible.</summary>
    void ConfirmDeleteAll()
    {
        PinPrompt.Show(onSuccess: () =>
        {
            if (ProfileManager.Instance != null)
                ProfileManager.Instance.DeleteAllData();
            _selected = null;
            _pendingDelete = null;
            RefreshContent();
            if (_statusT != null)
            {
                _statusT.color = KidUI.GOOD;
                _statusT.text = "Base de datos borrada. No queda ningun perfil.";
            }
        });
    }

    void ChangePin()
    {
        // Verifica el PIN actual y luego lanza el flujo de creación de uno nuevo.
        // El PIN antiguo solo se sustituye cuando el nuevo queda confirmado
        // (si el usuario cancela, el antiguo sigue vigente).
        PinPrompt.Show(onSuccess: () =>
        {
            PinPrompt.ShowCreate(onSuccess: () =>
            {
                if (_statusT != null)
                {
                    _statusT.color = KidUI.GOOD;
                    _statusT.text = "PIN actualizado.";
                }
            });
        });
    }
}
