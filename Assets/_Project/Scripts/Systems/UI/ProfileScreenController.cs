// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Pantalla de selección y creación de perfiles ("¿Quién juega hoy?").
/// Se muestra como overlay sobre la primera pantalla del juego, antes de jugar.
/// Pensada para niños que no leen: tarjetas grandes con avatar y color,
/// selección de edad con los tres robots (fija la dificultad recomendada).
/// El botón "Adulto" (discreto) abre el área de tutor protegida por PIN.
/// </summary>
public class ProfileScreenController : MonoBehaviour
{
    // Uso familiar: 5 perfiles + "Nuevo jugador" = 6 pods sin scroll.
    // Modo profesional (gabinetes): sin límite; con >5 se usa rejilla con scroll.
    static int MAX_PROFILES =>
        (ProfileManager.Instance != null && ProfileManager.Instance.ProfessionalMode) ? 500 : 5;

    GameObject _canvasGO;
    RectTransform _rootRT;
    GameObject _listView;
    GameObject _createView;

    // Estado de creación
    string _selNombre = "";
    string _selAvatar = KidUI.AVATAR_IDS[0];
    int _selEdad = -1;
    TMP_InputField _nameField;
    readonly List<Image> _avatarFrames = new List<Image>();
    readonly List<Image> _edadFrames = new List<Image>();

    static ProfileScreenController _current;

    /// <summary>True mientras la pantalla de perfiles está visible (bloquea el
    /// "Pulsa ENTER" de la pantalla inicial).</summary>
    public static bool IsOpen => _current != null;

    public static void Show()
    {
        if (_current != null) return;
        KidUI.EnsureEventSystem();
        var go = new GameObject("ProfileScreen");
        _current = go.AddComponent<ProfileScreenController>();
        _current.Build();
    }

    /// <summary>Reconstruye la lista de tarjetas si la pantalla está abierta
    /// (p. ej. tras borrar un perfil desde el área del tutor).</summary>
    public static void RefreshIfOpen()
    {
        if (_current == null) return;
        if (_current._listView != null && _current._listView.activeSelf)
            _current.BuildListView();
    }

    void OnDestroy()
    {
        if (_current == this) _current = null;
    }

    void Build()
    {
        var cv = KidUI.MakeCanvas("ProfileCanvas", 800, transform);
        _canvasGO = cv.gameObject;
        _rootRT = cv.GetComponent<RectTransform>();

        // Fondo espacial OPACO: cubre por completo la pantalla que haya detrás.
        KidUI.BuildSpaceBackground(_rootRT);

        BuildListView();
        // Sin fundido del canvas completo: el fondo opaco cubre la pantalla anterior
        // al instante y evita el "flash" de la PrimeraPantalla al abrirse.
    }

    // =================================================== VISTA: LISTA DE PERFILES

    void BuildListView()
    {
        if (_listView != null) Destroy(_listView);
        _listView = new GameObject("ListView");
        _listView.transform.SetParent(_rootRT, false);
        var rt = _listView.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;

        var title = KidUI.Txt(rt, "Title", "¿QUIÉN JUEGA HOY?", Color.white, 60,
                              new Vector2(0.05f, 0.87f), new Vector2(0.95f, 0.97f));
        title.fontStyle = FontStyles.Bold;
        title.characterSpacing = 5f;

        var line = KidUI.RoundImg(rt, "TitleLine", KidUI.ACCENT,
                                  new Vector2(0.44f, 0.855f), new Vector2(0.56f, 0.862f),
                                  Vector2.zero, Vector2.zero, 4f);
        line.GetComponent<Image>().raycastTarget = false;

        KidUI.Txt(rt, "Sub", "Toca tu planeta para despegar", KidUI.DIM, 24,
                  new Vector2(0.05f, 0.79f), new Vector2(0.95f, 0.85f));

        var profiles = ProfileManager.Instance != null
            ? ProfileManager.Instance.GetProfiles()
            : new List<ProfileData>();

        // Pods circulares: perfiles existentes + "Nuevo jugador"
        int total = Mathf.Min(profiles.Count, MAX_PROFILES) + 1;

        if (total <= 6)
        {
            // Disposición familiar: pods grandes en 1-2 filas
            int row1 = total <= 3 ? total : Mathf.CeilToInt(total / 2f);
            int row2 = total - row1;

            for (int i = 0; i < total; i++)
            {
                bool inRow1 = i < row1;
                int idxInRow = inRow1 ? i : i - row1;
                int rowCount = inRow1 ? row1 : row2;
                float spacing = 0.19f;
                float x = 0.5f + (idxInRow - (rowCount - 1) / 2f) * spacing;
                float y = total <= 3 ? 0.50f : (inRow1 ? 0.585f : 0.275f);

                if (i < total - 1)
                    BuildProfilePod(rt, profiles[i], i, new Vector2(x, y));
                else
                    BuildNewPlayerPod(rt, new Vector2(x, y), profiles.Count >= MAX_PROFILES);
            }
        }
        else
        {
            // Modo profesional con muchos niños: rejilla compacta con scroll
            BuildScrollGrid(rt, profiles);
        }

        // Botones inferiores
        var guestB = KidUI.Btn(rt, "Jugar sin guardar", new Color(0.10f, 0.14f, 0.26f, 0.85f),
                               new Vector2(0.38f, 0.045f), new Vector2(0.62f, 0.105f),
                               OnGuest, 19f);
        guestB.GetComponentInChildren<TextMeshProUGUI>().color = KidUI.DIM;

        KidUI.Btn(rt, "ADULTO", new Color(0.08f, 0.10f, 0.18f, 0.9f),
                  new Vector2(0.885f, 0.035f), new Vector2(0.975f, 0.095f),
                  OnAdult, 15f);
    }

    /// <summary>Rejilla compacta con scroll para el modo profesional (decenas de niños):
    /// tarjetas horizontales con avatar circular pequeño, nombre y edad, 4 por fila.</summary>
    void BuildScrollGrid(RectTransform parent, List<ProfileData> profiles)
    {
        var viewGO = new GameObject("GridView");
        viewGO.transform.SetParent(parent, false);
        var viewRT = viewGO.AddComponent<RectTransform>();
        viewRT.anchorMin = new Vector2(0.07f, 0.145f);
        viewRT.anchorMax = new Vector2(0.93f, 0.775f);
        viewRT.sizeDelta = Vector2.zero;
        viewGO.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.22f);
        viewGO.AddComponent<RectMask2D>();

        var contentGO = new GameObject("GridContent");
        contentGO.transform.SetParent(viewRT, false);
        var content = contentGO.AddComponent<RectTransform>();
        content.anchorMin = new Vector2(0, 1);
        content.anchorMax = new Vector2(1, 1);
        content.pivot = new Vector2(0.5f, 1f);

        const int COLS = 4;
        const float CELL_W = 396f, CELL_H = 104f, GAP = 14f;
        int totalCells = profiles.Count + 1;                       // + "Nuevo jugador"
        int rows = Mathf.CeilToInt(totalCells / (float)COLS);
        content.sizeDelta = new Vector2(0, rows * (CELL_H + GAP) + GAP);

        for (int i = 0; i < totalCells; i++)
        {
            int r = i / COLS, c = i % COLS;
            float x = GAP + c * (CELL_W + GAP) + CELL_W / 2f - (COLS * (CELL_W + GAP) + GAP) / 2f;
            float y = -(GAP + r * (CELL_H + GAP) + CELL_H / 2f);
            var pos = new Vector2(x, y);

            if (i < profiles.Count) BuildCompactCard(content, profiles[i], i, pos, CELL_W, CELL_H);
            else                    BuildCompactNewCard(content, pos, CELL_W, CELL_H,
                                                        profiles.Count >= MAX_PROFILES);
        }

        var sr = viewGO.AddComponent<ScrollRect>();
        sr.content = content;
        sr.viewport = viewRT;
        sr.horizontal = false;
        sr.vertical = true;
        sr.movementType = ScrollRect.MovementType.Clamped;
        sr.scrollSensitivity = 34f;
    }

    void BuildCompactCard(RectTransform content, ProfileData p, int index,
                          Vector2 pos, float w, float h)
    {
        Color col = KidUI.CARD_COLORS[index % KidUI.CARD_COLORS.Length];
        var rt = MakeCell(content, "Card_" + p.nombre, pos, w, h,
                          new Color(0.09f, 0.12f, 0.23f, 0.97f));

        var ring = KidUI.CircleAt(rt, "Ring", col, new Vector2(0.115f, 0.5f), 76f);
        ring.GetComponent<Image>().raycastTarget = false;
        var inner = KidUI.CircleAt(rt, "Inner", new Color(0.06f, 0.09f, 0.17f, 1f),
                                   new Vector2(0.115f, 0.5f), 64f);
        inner.GetComponent<Image>().raycastTarget = false;
        KidUI.Avatar(inner, p.avatarId, p.nombre, col,
                     new Vector2(0.14f, 0.14f), new Vector2(0.86f, 0.86f));

        var nameT = KidUI.Txt(rt, "Name", p.nombre, Color.white, 22,
                              new Vector2(0.24f, 0.44f), new Vector2(0.96f, 0.92f));
        nameT.fontStyle = FontStyles.Bold;
        nameT.alignment = TextAlignmentOptions.MidlineLeft;
        var edadT = KidUI.Txt(rt, "Edad", p.EdadTramoLabel, new Color(col.r, col.g, col.b, 0.95f),
                              15, new Vector2(0.24f, 0.08f), new Vector2(0.96f, 0.44f));
        edadT.alignment = TextAlignmentOptions.MidlineLeft;

        var btn = rt.gameObject.AddComponent<Button>();
        btn.targetGraphic = rt.GetComponent<Image>();
        btn.onClick.AddListener(() =>
        {
            if (UIAudioManager.Instance != null) UIAudioManager.Instance.PlayClick();
            GameFeel.PlayPop();
            SelectAndClose(p);
        });
        ButtonJuice.Attach(rt.gameObject);
    }

    void BuildCompactNewCard(RectTransform content, Vector2 pos, float w, float h, bool full)
    {
        Color g = full ? KidUI.DIM : KidUI.GOOD;
        var rt = MakeCell(content, "Card_New", pos, w, h, new Color(0.07f, 0.10f, 0.19f, 0.95f));

        var ring = KidUI.CircleAt(rt, "Ring", new Color(g.r, g.g, g.b, 0.55f),
                                  new Vector2(0.115f, 0.5f), 76f);
        ring.GetComponent<Image>().raycastTarget = false;
        var plus = KidUI.Txt(rt, "Plus", "+", g, 40,
                             new Vector2(0.03f, 0.1f), new Vector2(0.20f, 0.9f));
        plus.fontStyle = FontStyles.Bold;

        var lbl = KidUI.Txt(rt, "Lbl", full ? "Límite alcanzado" : "Nuevo jugador",
                            full ? KidUI.DIM : Color.white, 20,
                            new Vector2(0.24f, 0.1f), new Vector2(0.96f, 0.9f));
        lbl.fontStyle = FontStyles.Bold;
        lbl.alignment = TextAlignmentOptions.MidlineLeft;

        if (!full)
        {
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = rt.GetComponent<Image>();
            btn.onClick.AddListener(() =>
            {
                if (UIAudioManager.Instance != null) UIAudioManager.Instance.PlayClick();
                ShowCreateView();
            });
            ButtonJuice.Attach(rt.gameObject);
        }
    }

    RectTransform MakeCell(RectTransform content, string name, Vector2 pos, float w, float h, Color bg)
    {
        var go = new GameObject(name);
        go.transform.SetParent(content, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(w, h);
        var img = go.AddComponent<Image>();
        img.color = bg;
        img.sprite = KidUI.RoundedSprite;
        img.type = Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = 1.2f;
        return rt;
    }

    /// <summary>Pod circular estilo planeta: anillo de color + avatar + chip con nombre.</summary>
    void BuildProfilePod(RectTransform parent, ProfileData p, int index, Vector2 center)
    {
        Color c = KidUI.CARD_COLORS[index % KidUI.CARD_COLORS.Length];

        var holder = new GameObject("Pod_" + p.nombre);
        holder.transform.SetParent(parent, false);
        var hrt = holder.AddComponent<RectTransform>();
        hrt.anchorMin = hrt.anchorMax = center;
        hrt.pivot = new Vector2(0.5f, 0.5f);
        hrt.anchoredPosition = Vector2.zero;
        hrt.sizeDelta = new Vector2(280f, 330f);

        // Zona clicable (transparente, cubre todo el pod)
        var hit = holder.AddComponent<Image>();
        hit.color = new Color(0, 0, 0, 0.001f);

        // Halo exterior suave + anillo de color + interior oscuro
        var halo = KidUI.CircleAt(hrt, "Halo", new Color(c.r, c.g, c.b, 0.18f),
                                  new Vector2(0.5f, 0.66f), 236f);
        halo.GetComponent<Image>().raycastTarget = false;
        var ring = KidUI.CircleAt(hrt, "Ring", c, new Vector2(0.5f, 0.66f), 204f);
        ring.GetComponent<Image>().raycastTarget = false;
        var inner = KidUI.CircleAt(hrt, "Inner", new Color(0.07f, 0.10f, 0.20f, 1f),
                                   new Vector2(0.5f, 0.66f), 180f);
        inner.GetComponent<Image>().raycastTarget = false;

        KidUI.Avatar(inner, p.avatarId, p.nombre, c,
                     new Vector2(0.13f, 0.13f), new Vector2(0.87f, 0.87f));

        // Chip con nombre y edad
        var chip = KidUI.RoundImg(hrt, "Chip", new Color(0.09f, 0.12f, 0.23f, 0.95f),
                                  new Vector2(0.06f, 0.02f), new Vector2(0.94f, 0.30f),
                                  Vector2.zero, Vector2.zero, 1.3f);
        chip.GetComponent<Image>().raycastTarget = false;
        var nameT = KidUI.Txt(chip, "Name", p.nombre, Color.white, 28,
                              new Vector2(0.05f, 0.42f), new Vector2(0.95f, 0.96f));
        nameT.fontStyle = FontStyles.Bold;
        KidUI.Txt(chip, "Edad", p.EdadTramoLabel, new Color(c.r, c.g, c.b, 0.95f), 16,
                  new Vector2(0.05f, 0.04f), new Vector2(0.95f, 0.42f));

        var btn = holder.AddComponent<Button>();
        btn.targetGraphic = hit;
        btn.onClick.AddListener(() =>
        {
            if (UIAudioManager.Instance != null) UIAudioManager.Instance.PlayClick();
            GameFeel.PlayPop();
            SelectAndClose(p);
        });
        ButtonJuice.Attach(holder);

        UITween.PopIn(hrt, 0.4f, 0.7f, 0.06f * index);
    }

    void BuildNewPlayerPod(RectTransform parent, Vector2 center, bool full)
    {
        var holder = new GameObject("Pod_New");
        holder.transform.SetParent(parent, false);
        var hrt = holder.AddComponent<RectTransform>();
        hrt.anchorMin = hrt.anchorMax = center;
        hrt.pivot = new Vector2(0.5f, 0.5f);
        hrt.anchoredPosition = Vector2.zero;
        hrt.sizeDelta = new Vector2(280f, 330f);

        var hit = holder.AddComponent<Image>();
        hit.color = new Color(0, 0, 0, 0.001f);

        Color g = full ? KidUI.DIM : KidUI.GOOD;
        var ring = KidUI.CircleAt(hrt, "Ring", new Color(g.r, g.g, g.b, 0.55f),
                                  new Vector2(0.5f, 0.66f), 204f);
        ring.GetComponent<Image>().raycastTarget = false;
        var inner = KidUI.CircleAt(hrt, "Inner", new Color(0.06f, 0.09f, 0.17f, 1f),
                                   new Vector2(0.5f, 0.66f), 184f);
        inner.GetComponent<Image>().raycastTarget = false;

        var plus = KidUI.Txt(inner, "Plus", "+", g, 100,
                             new Vector2(0.1f, 0.1f), new Vector2(0.9f, 0.9f));
        plus.fontStyle = FontStyles.Bold;

        var chip = KidUI.RoundImg(hrt, "Chip", new Color(0.09f, 0.12f, 0.23f, 0.95f),
                                  new Vector2(0.06f, 0.02f), new Vector2(0.94f, 0.30f),
                                  Vector2.zero, Vector2.zero, 1.3f);
        chip.GetComponent<Image>().raycastTarget = false;
        var lbl = KidUI.Txt(chip, "Lbl", full ? "Lista llena" : "Nuevo jugador",
                            full ? KidUI.DIM : Color.white, 24,
                            new Vector2(0.05f, 0.1f), new Vector2(0.95f, 0.9f));
        lbl.fontStyle = FontStyles.Bold;

        if (!full)
        {
            var btn = holder.AddComponent<Button>();
            btn.targetGraphic = hit;
            btn.onClick.AddListener(() =>
            {
                if (UIAudioManager.Instance != null) UIAudioManager.Instance.PlayClick();
                ShowCreateView();
            });
            ButtonJuice.Attach(holder);
        }

        UITween.PopIn(hrt, 0.4f, 0.7f, 0.3f);
    }

    // =================================================== VISTA: CREAR PERFIL

    void ShowCreateView()
    {
        if (_listView != null) _listView.SetActive(false);
        if (_createView != null) Destroy(_createView);

        _selNombre = "";
        _selAvatar = KidUI.AVATAR_IDS[0];
        _selEdad = -1;
        _avatarFrames.Clear();
        _edadFrames.Clear();

        _createView = new GameObject("CreateView");
        _createView.transform.SetParent(_rootRT, false);
        var rt = _createView.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;

        // Tarjeta central redondeada sobre el fondo espacial
        var card = KidUI.RoundImg(rt, "Card", new Color(0.055f, 0.075f, 0.15f, 0.93f),
                                  new Vector2(0.15f, 0.02f), new Vector2(0.85f, 0.99f),
                                  Vector2.zero, Vector2.zero, 0.7f);
        card.GetComponent<Image>().raycastTarget = false;
        var edge = KidUI.RoundImg(rt, "CardEdge", KidUI.GOOD,
                                  new Vector2(0.42f, 0.975f), new Vector2(0.58f, 0.983f),
                                  Vector2.zero, Vector2.zero, 4f);
        edge.GetComponent<Image>().raycastTarget = false;

        var title = KidUI.Txt(rt, "Title", "NUEVO JUGADOR", Color.white, 48,
                              new Vector2(0.05f, 0.90f), new Vector2(0.95f, 0.98f));
        title.fontStyle = FontStyles.Bold;

        // --- Nombre (lo puede escribir un adulto)
        KidUI.Txt(rt, "LblNombre", "Tu nombre (puede escribirlo un adulto):",
                  KidUI.DIM, 22, new Vector2(0.25f, 0.83f), new Vector2(0.75f, 0.88f));
        _nameField = KidUI.InputField(rt, "Escribe aqui...",
                                      new Vector2(0.33f, 0.76f), new Vector2(0.67f, 0.83f));

        // --- Avatar: 12 personajes en dos filas de 6, cada uno con su "?" que
        // abre un modal con la historia del personaje en Attentia.
        KidUI.Txt(rt, "LblAvatar", "Elige tu personaje (toca el ? para conocer su historia):",
                  KidUI.DIM, 20, new Vector2(0.15f, 0.68f), new Vector2(0.85f, 0.73f));
        int n = KidUI.AVATAR_IDS.Length;
        float aw = 0.105f, gap = 0.014f;
        float ax0 = 0.5f - (n * aw + (n - 1) * gap) / 2f;
        for (int i = 0; i < n; i++)
        {
            string id = KidUI.AVATAR_IDS[i];
            float x = ax0 + i * (aw + gap);
            float y = 0.585f;

            var frame = KidUI.CircleAt(rt, "AvFrame_" + id, KidUI.PANEL2,
                                       new Vector2(x + aw / 2f, y), 168f);
            _avatarFrames.Add(frame.GetComponent<Image>());
            var frameInner = KidUI.CircleAt(frame, "Inner", new Color(0.05f, 0.07f, 0.14f, 1f),
                                            new Vector2(0.5f, 0.5f), 150f);
            frameInner.GetComponent<Image>().raycastTarget = false;
            KidUI.Avatar(frameInner, id, CharacterLore.Nombre(id),
                         KidUI.CARD_COLORS[i % KidUI.CARD_COLORS.Length],
                         new Vector2(0.06f, 0.06f), new Vector2(0.94f, 0.94f));
            var b = frame.gameObject.AddComponent<Button>();
            b.targetGraphic = frame.GetComponent<Image>();
            string captured = id;
            b.onClick.AddListener(() =>
            {
                if (UIAudioManager.Instance != null) UIAudioManager.Instance.PlayClick();
                _selAvatar = captured;
                RefreshCreateSelection();
            });

            // Botón "?" — la historia del personaje (el hijo captura el clic
            // antes que el marco, así no cambia la selección).
            var ask = KidUI.CircleAt(frame, "Ask", new Color(0.30f, 0.65f, 1f, 0.95f),
                                     new Vector2(0.88f, 0.88f), 40f);
            var askT = KidUI.Txt(ask, "T", "?", Color.white, 22, Vector2.zero, Vector2.one);
            askT.fontStyle = FontStyles.Bold;
            askT.raycastTarget = false;
            var askBtn = ask.gameObject.AddComponent<Button>();
            askBtn.targetGraphic = ask.GetComponent<Image>();
            askBtn.onClick.AddListener(() =>
            {
                if (UIAudioManager.Instance != null) UIAudioManager.Instance.PlayClick();
                ShowLoreModal(captured);
            });
        }

        // --- Edad con los tres robots
        KidUI.Txt(rt, "LblEdad", "¿Cuántos años tienes?", KidUI.DIM, 22,
                  new Vector2(0.25f, 0.44f), new Vector2(0.75f, 0.49f));
        string[] robots = { "neo", "axel", "titan" };
        string[] edades = { "3 - 5", "5 - 7", "7 - 10" };
        string[] nombresRobot = { "NEO", "AXEL", "TITAN" };
        float ew = 0.14f, egap = 0.03f;
        float ex0 = 0.5f - (3 * ew + 2 * egap) / 2f;
        for (int i = 0; i < 3; i++)
        {
            float x = ex0 + i * (ew + egap);
            var frame = KidUI.RoundImg(rt, "EdadFrame_" + i, KidUI.PANEL2,
                                       new Vector2(x, 0.20f), new Vector2(x + ew, 0.43f),
                                       Vector2.zero, Vector2.zero, 1.1f);
            _edadFrames.Add(frame.GetComponent<Image>());

            KidUI.Avatar(frame, robots[i], nombresRobot[i],
                         KidUI.CARD_COLORS[i], new Vector2(0.15f, 0.40f), new Vector2(0.85f, 0.95f));
            var eT = KidUI.Txt(frame, "E", edades[i] + " años", Color.white, 24,
                               new Vector2(0.05f, 0.20f), new Vector2(0.95f, 0.38f));
            eT.fontStyle = FontStyles.Bold;
            KidUI.Txt(frame, "R", "con " + nombresRobot[i], KidUI.DIM, 16,
                      new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.19f));

            var b = frame.gameObject.AddComponent<Button>();
            b.targetGraphic = frame.GetComponent<Image>();
            int captured = i;
            b.onClick.AddListener(() =>
            {
                if (UIAudioManager.Instance != null) UIAudioManager.Instance.PlayClick();
                _selEdad = captured;
                RefreshCreateSelection();
            });
        }

        // --- Acciones
        KidUI.Btn(rt, "¡LISTO!", KidUI.GOOD,
                  new Vector2(0.38f, 0.07f), new Vector2(0.62f, 0.15f),
                  OnCreateConfirm, 30f);
        KidUI.Btn(rt, "Volver", KidUI.BTNC,
                  new Vector2(0.05f, 0.07f), new Vector2(0.18f, 0.13f),
                  () =>
                  {
                      Destroy(_createView);
                      _createView = null;
                      BuildListView();
                  }, 20f);

        RefreshCreateSelection();
        UITween.FadeIn(_createView, 0.25f);
    }

    void RefreshCreateSelection()
    {
        for (int i = 0; i < _avatarFrames.Count; i++)
            _avatarFrames[i].color = KidUI.AVATAR_IDS[i] == _selAvatar
                ? KidUI.ACCENT : KidUI.PANEL2;
        for (int i = 0; i < _edadFrames.Count; i++)
            _edadFrames[i].color = i == _selEdad
                ? KidUI.CARD_COLORS[i] * 0.6f + KidUI.PANEL2 * 0.4f
                : KidUI.PANEL2;
    }

    // =================================================== HISTORIA DEL PERSONAJE

    GameObject _loreModal;

    /// <summary>Modal con la historia del personaje en Attentia: retrato grande,
    /// nombre, título y su cuento. Se cierra tocando el fondo o el botón.</summary>
    void ShowLoreModal(string avatarId)
    {
        if (_loreModal != null) Destroy(_loreModal);

        _loreModal = new GameObject("LoreModal");
        _loreModal.transform.SetParent(_rootRT, false);
        var ort = _loreModal.AddComponent<RectTransform>();
        ort.anchorMin = Vector2.zero; ort.anchorMax = Vector2.one;
        ort.sizeDelta = Vector2.zero;

        // Fondo oscuro: también cierra al tocarlo
        var dim = KidUI.Img(ort, "Dim", new Color(0f, 0f, 0f, 0.72f),
                            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var dimBtn = dim.gameObject.AddComponent<Button>();
        dimBtn.transition = Selectable.Transition.None;
        dimBtn.onClick.AddListener(CloseLoreModal);

        var card = KidUI.RoundImg(ort, "Card", new Color(0.055f, 0.075f, 0.15f, 0.98f),
                                  new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                                  Vector2.zero, new Vector2(940f, 640f), 0.8f);

        // Retrato con halo
        var halo = KidUI.CircleAt(card, "Halo", new Color(0.30f, 0.65f, 1f, 0.15f),
                                  new Vector2(0.20f, 0.60f), 340f);
        halo.GetComponent<Image>().raycastTarget = false;
        var portrait = KidUI.Img(card, "Portrait", Color.clear,
                                 new Vector2(0.05f, 0.32f), new Vector2(0.36f, 0.88f),
                                 Vector2.zero, Vector2.zero);
        portrait.GetComponent<Image>().raycastTarget = false;
        KidUI.Avatar(portrait, avatarId, CharacterLore.Nombre(avatarId), KidUI.ACCENT,
                     Vector2.zero, Vector2.one);
        portrait.gameObject.AddComponent<FloatBob>().Configure(7f, 1.0f);

        // Nombre + título
        var nm = KidUI.Txt(card, "N", CharacterLore.Nombre(avatarId), Color.white, 42,
                           new Vector2(0.40f, 0.80f), new Vector2(0.95f, 0.94f));
        nm.fontStyle = FontStyles.Bold;
        nm.characterSpacing = 4f;
        nm.alignment = TextAlignmentOptions.MidlineLeft;
        var tt = KidUI.Txt(card, "TT", CharacterLore.Titulo(avatarId),
                           new Color(0.30f, 0.65f, 1f), 21,
                           new Vector2(0.40f, 0.73f), new Vector2(0.95f, 0.80f));
        tt.fontStyle = FontStyles.Bold;
        tt.alignment = TextAlignmentOptions.MidlineLeft;

        // La historia
        var story = KidUI.Txt(card, "Story", CharacterLore.Historia(avatarId),
                              new Color(0.88f, 0.92f, 1f), 20,
                              new Vector2(0.40f, 0.18f), new Vector2(0.95f, 0.72f));
        story.alignment = TextAlignmentOptions.TopLeft;
        story.enableWordWrapping = true;

        KidUI.Btn(card, "¡Me encanta!", KidUI.GOOD,
                  new Vector2(0.36f, 0.045f), new Vector2(0.64f, 0.145f),
                  CloseLoreModal, 22f);

        UITween.PopIn(card, 0.35f, 0.85f);
        GameFeel.PlayPop();
    }

    void CloseLoreModal()
    {
        if (_loreModal != null) Destroy(_loreModal);
        _loreModal = null;
    }

    void OnCreateConfirm()
    {
        _selNombre = _nameField != null ? _nameField.text : "";
        if (_selEdad < 0)
        {
            // Falta la edad: resalta la fila (feedback simple sin texto).
            foreach (var f in _edadFrames) f.color = KidUI.BAD;
            return;
        }
        var pm = ProfileManager.Instance;
        if (pm == null) return;
        var p = pm.CreateProfile(_selNombre, _selAvatar, _selEdad);
        SelectAndClose(p);
    }

    // =================================================== ACCIONES

    void SelectAndClose(ProfileData p)
    {
        if (ProfileManager.Instance != null)
            ProfileManager.Instance.SelectProfile(p);
        // Hub del jugador: mapa del planeta Attentia con progreso, misión del día
        // y logros. Desde ahí se despega a las categorías (dificultad ya fijada).
        ProgressMapScreen.Show();
        CloseAnimated();
    }

    void OnGuest()
    {
        if (ProfileManager.Instance != null)
            ProfileManager.Instance.PlayAsGuest();
        CloseAnimated();
    }

    void OnAdult()
    {
        PinPrompt.Show(onSuccess: () => TutorPanel.Show());
    }

    void CloseAnimated()
    {
        UITween.FadeOut(_canvasGO, 0.25f, () => { if (this != null) Destroy(gameObject); });
    }
}
