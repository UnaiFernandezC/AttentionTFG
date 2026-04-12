using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controlador principal del minijuego "Parejas de Colores".
/// Construye toda la UI por código y orquesta el tablero.
///
/// Hereda de MinigameBase para integrarse con GameManager y SceneLoader.
///
/// Dificultad:
///   Easy   → 4 pares / 8  cartas / sin tiempo
///   Medium → 6 pares / 12 cartas / sin tiempo
///   Hard   → 8 pares / 16 cartas / 90 segundos
/// </summary>
public class ColorMatchGameController : MinigameBase
{
    // ─── Inspector ────────────────────────────────────────────────────────
    [Header("Pares por dificultad")]
    public int pairsEasy   = 4;
    public int pairsMedium = 6;
    public int pairsHard   = 8;

    [Header("Tiempo limite en Hard (0 = sin limite)")]
    public float timeLimitHard = 90f;

    // ─── Paleta UI ────────────────────────────────────────────────────────
    private static readonly Color C_BG_DARK   = new Color(0.07f, 0.08f, 0.18f);
    private static readonly Color C_BG_MID    = new Color(0.10f, 0.07f, 0.22f);
    private static readonly Color C_PANEL     = new Color(0.11f, 0.12f, 0.26f);
    private static readonly Color C_PANEL2    = new Color(0.14f, 0.16f, 0.32f);
    private static readonly Color C_ACCENT    = new Color(0.48f, 0.76f, 1.00f);
    private static readonly Color C_GREEN     = new Color(0.28f, 0.86f, 0.60f);
    private static readonly Color C_BTN_BLUE  = new Color(0.25f, 0.55f, 1.00f);
    private static readonly Color C_BTN_GREY  = new Color(0.20f, 0.22f, 0.38f);
    private static readonly Color C_BTN_RED   = new Color(0.75f, 0.20f, 0.28f);
    private static readonly Color C_WHITE     = Color.white;
    private static readonly Color C_WHITE_DIM = new Color(1f, 1f, 1f, 0.65f);
    private static readonly Color C_SEPARATOR = new Color(1f, 1f, 1f, 0.08f);

    // ─── Estado ───────────────────────────────────────────────────────────
    private int   _attempts    = 0;
    private float _elapsed     = 0f;
    private bool  _gameOver    = false;
    private bool  _useTimer    = false;
    private float _timeLimit   = 0f;
    private int   _targetPairs = 4;

    // ─── Referencias UI ───────────────────────────────────────────────────
    private BoardManager  _boardManager;
    private RectTransform _boardContainer;
    private TextMeshProUGUI _attemptsLabel;
    private TextMeshProUGUI _timerLabel;
    private GameObject    _winPanel;
    private TextMeshProUGUI _winTitle;
    private TextMeshProUGUI _statsPairs;
    private TextMeshProUGUI _statsAttempts;
    private TextMeshProUGUI _statsTime;
    private TextMeshProUGUI _statsScore;

    // ═══════════════════════════════════════════════════════════════════════
    // MinigameBase
    // ═══════════════════════════════════════════════════════════════════════

    protected override void OnMinigameStart()
    {
        var diff = GameManager.Instance != null
            ? GameManager.Instance.CurrentDifficulty
            : DifficultyLevel.Easy;

        switch (diff)
        {
            case DifficultyLevel.Easy:
                _targetPairs = pairsEasy;   _useTimer = false; break;
            case DifficultyLevel.Medium:
                _targetPairs = pairsMedium; _useTimer = false; break;
            case DifficultyLevel.Hard:
                _targetPairs = pairsHard;
                _useTimer    = timeLimitHard > 0f;
                _timeLimit   = timeLimitHard; break;
        }

        EnsureEventSystem();
        BuildUI();
        StartBoard();
    }

    protected override void OnMinigameComplete() { _gameOver = true; ShowResultPanel(won: true); }
    protected override void OnMinigameFailed()   { _gameOver = true; ShowResultPanel(won: false); }

    // ═══════════════════════════════════════════════════════════════════════
    // Update
    // ═══════════════════════════════════════════════════════════════════════

    private void Update()
    {
        if (_gameOver) return;
        _elapsed += Time.deltaTime;

        if (_useTimer)
        {
            float left = Mathf.Max(0f, _timeLimit - _elapsed);
            if (_timerLabel) _timerLabel.text  = $"{Mathf.CeilToInt(left)}s";
            if (_timerLabel) _timerLabel.color = left < 15f
                ? new Color(1f, 0.35f, 0.35f) : C_ACCENT;
            if (left <= 0f) FailMinigame();
        }
        else
        {
            if (_timerLabel) _timerLabel.text = FormatTime(_elapsed);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Construcción de la UI
    // ═══════════════════════════════════════════════════════════════════════

    private void BuildUI()
    {
        // ── Canvas ────────────────────────────────────────────────────────
        var cGO = new GameObject("Canvas"); cGO.transform.SetParent(transform, false);
        var canvas  = cGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler  = cGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight  = 0.5f;
        cGO.AddComponent<GraphicRaycaster>();

        // ── Fondo ─────────────────────────────────────────────────────────
        var root  = MakePanel(cGO.transform, "Root", C_BG_DARK, Stretch());
        // gradiente inferior sutil
        var grad  = MakePanel(root.transform, "Grad", C_BG_MID, AnchorRect(0,0,1,0.45f));
        grad.color = new Color(C_BG_MID.r, C_BG_MID.g, C_BG_MID.b, 0.60f);

        // ── Header ────────────────────────────────────────────────────────
        BuildHeader(root.transform);

        // ── Zona del tablero ──────────────────────────────────────────────
        var boardZone = new GameObject("BoardZone");
        boardZone.transform.SetParent(root.transform, false);
        var bzRT = boardZone.AddComponent<RectTransform>();
        bzRT.anchorMin = new Vector2(0f, 0.12f);
        bzRT.anchorMax = new Vector2(1f, 0.84f);
        bzRT.offsetMin = new Vector2(24f, 0f);
        bzRT.offsetMax = new Vector2(-24f, 0f);

        var boardContGO = new GameObject("BoardContainer");
        boardContGO.transform.SetParent(bzRT, false);
        _boardContainer = boardContGO.AddComponent<RectTransform>();
        _boardContainer.anchorMin        = new Vector2(0.5f, 0.5f);
        _boardContainer.anchorMax        = new Vector2(0.5f, 0.5f);
        _boardContainer.pivot            = new Vector2(0.5f, 0.5f);
        _boardContainer.anchoredPosition = Vector2.zero;

        // ── Barra inferior con botones ─────────────────────────────────────
        BuildBottomBar(root.transform);

        // ── Panel de resultado (oculto) ────────────────────────────────────
        BuildResultPanel(root.transform);
    }

    // ─── Header ───────────────────────────────────────────────────────────

    private void BuildHeader(Transform parent)
    {
        var hdr = MakePanel(parent, "Header", C_PANEL, AnchorRect(0, 1, 1, 1, 0, -148f, 0, 0));

        // Línea de acento arriba del header
        var line = MakePanel(hdr.transform, "AccentLine", C_ACCENT, AnchorRect(0, 1, 1, 1, 0, -3f, 0, 0));

        // Título
        var title = MakeLabel(hdr.transform, "Title", "Parejas de Colores",
            C_WHITE, 50f, FontStyles.Bold);
        PlaceRT(title.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -52f), new Vector2(800f, 64f));

        // Línea separadora
        var sep = MakePanel(hdr.transform, "Sep", C_SEPARATOR, AnchorRect(0.05f, 0, 0.95f, 0, 0, 1f, 0, 2f));

        // Stats HUD
        _attemptsLabel = MakeLabel(hdr.transform, "Attempts", "Intentos: 0",
            C_ACCENT, 28f, FontStyles.Normal);
        PlaceRT(_attemptsLabel.gameObject, new Vector2(0.28f, 0.5f), new Vector2(0.28f, 0.5f),
                new Vector2(0f, -90f), new Vector2(320f, 36f));

        _timerLabel = MakeLabel(hdr.transform, "Timer", "00:00",
            C_ACCENT, 28f, FontStyles.Normal);
        PlaceRT(_timerLabel.gameObject, new Vector2(0.72f, 0.5f), new Vector2(0.72f, 0.5f),
                new Vector2(0f, -90f), new Vector2(200f, 36f));

        // Separador vertical central
        var vsep = MakePanel(hdr.transform, "VSep", C_SEPARATOR,
            AnchorRect(0.5f, 0.5f, 0.5f, 0.5f, -0.5f, -50f, 0.5f, -36f));
    }

    // ─── Barra inferior ───────────────────────────────────────────────────

    private void BuildBottomBar(Transform parent)
    {
        var bar = MakePanel(parent, "BottomBar", C_PANEL, AnchorRect(0, 0, 1, 0.11f));

        // Línea separadora arriba
        MakePanel(bar.transform, "TopLine", C_SEPARATOR, AnchorRect(0, 1, 1, 1, 0, -1f, 0, 0));

        MakeButton(bar.transform, "BtnRestart", "Reiniciar",
            C_BTN_BLUE, new Vector2(-170f, 0f), new Vector2(290f, 68f),
            () => RestartMinigame());

        MakeButton(bar.transform, "BtnMenu", "Volver al menu",
            C_BTN_GREY, new Vector2(170f, 0f), new Vector2(290f, 68f),
            () => ReturnToGameSelector());
    }

    // ─── Panel de resultado ───────────────────────────────────────────────

    private void BuildResultPanel(Transform parent)
    {
        // Overlay oscuro de pantalla completa
        _winPanel = MakePanel(parent, "ResultPanel",
            new Color(0f, 0f, 0f, 0.85f), Stretch()).gameObject;

        // Tarjeta central
        var card = MakePanel(_winPanel.transform, "Card", C_PANEL,
            CenterRect(760f, 600f)).gameObject;

        // Borde superior de color (cambia según resultado)
        var topBorder = MakePanel(card.transform, "TopBorder", C_GREEN,
            AnchorRect(0, 1, 1, 1, 0, -5f, 0, 0));

        // Título
        _winTitle = MakeLabel(card.transform, "Title", "Ganaste",
            C_GREEN, 64f, FontStyles.Bold);
        PlaceRT(_winTitle.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -80f), new Vector2(680f, 80f));

        // Separador
        MakePanel(card.transform, "Sep1", new Color(1f, 1f, 1f, 0.10f),
            AnchorRect(0.08f, 1f, 0.92f, 1f, 0, -118f, 0, -116f));

        // Bloque de estadísticas (4 filas)
        float sy = -148f;
        float sh = 46f;
        float sgap = 8f;

        _statsPairs = MakeStatRow(card.transform, "SP", "Parejas encontradas", "--",
            new Vector2(0f, sy)); sy -= sh + sgap;
        _statsAttempts = MakeStatRow(card.transform, "SA", "Intentos realizados", "--",
            new Vector2(0f, sy)); sy -= sh + sgap;
        _statsTime = MakeStatRow(card.transform, "ST", "Tiempo", "--",
            new Vector2(0f, sy)); sy -= sh + sgap;
        _statsScore = MakeStatRow(card.transform, "SS", "Puntuacion", "--",
            new Vector2(0f, sy));

        // Separador antes de botones
        MakePanel(card.transform, "Sep2", new Color(1f, 1f, 1f, 0.10f),
            AnchorRect(0.08f, 0f, 0.92f, 0f, 0, 116f, 0, 118f));

        // Botones
        MakeButton(card.transform, "BtnAgain", "Jugar de nuevo",
            C_BTN_BLUE, new Vector2(-190f, 70f), new Vector2(330f, 72f),
            () => RestartMinigame());

        MakeButton(card.transform, "BtnMenuW", "Menu principal",
            C_BTN_GREY, new Vector2(190f, 70f), new Vector2(330f, 72f),
            () => ReturnToGameSelector());

        _winPanel.SetActive(false);
    }

    // ─── Fila de estadística ──────────────────────────────────────────────

    private TextMeshProUGUI MakeStatRow(Transform parent, string id, string label, string value,
                                 Vector2 yOffset)
    {
        // Fondo de fila
        var row = MakePanel(parent, id + "_Row", C_PANEL2,
            new RectTransformCfg
            {
                anchorMin = new Vector2(0.07f, 0.5f),
                anchorMax = new Vector2(0.93f, 0.5f),
                offsetMin = new Vector2(0f, yOffset.y - 23f),
                offsetMax = new Vector2(0f, yOffset.y + 23f)
            });

        // Etiqueta izquierda
        var lbl = new GameObject(id + "_Lbl");
        lbl.transform.SetParent(row.transform, false);
        var lblRT = lbl.AddComponent<RectTransform>();
        lblRT.anchorMin = new Vector2(0f, 0f); lblRT.anchorMax = new Vector2(0.62f, 1f);
        lblRT.offsetMin = new Vector2(16f, 0f); lblRT.offsetMax = Vector2.zero;
        var lblTmp = lbl.AddComponent<TextMeshProUGUI>();
        lblTmp.text      = label;
        lblTmp.color     = C_WHITE_DIM;
        lblTmp.fontSize  = 27f;
        lblTmp.alignment = TextAlignmentOptions.MidlineLeft;

        // Valor derecho
        var val = new GameObject(id + "_Val");
        val.transform.SetParent(row.transform, false);
        var valRT = val.AddComponent<RectTransform>();
        valRT.anchorMin = new Vector2(0.62f, 0f); valRT.anchorMax = new Vector2(1f, 1f);
        valRT.offsetMin = Vector2.zero; valRT.offsetMax = new Vector2(-16f, 0f);
        var valTmp = val.AddComponent<TextMeshProUGUI>();
        valTmp.text      = value;
        valTmp.color     = C_WHITE;
        valTmp.fontSize  = 27f;
        valTmp.fontStyle = FontStyles.Bold;
        valTmp.alignment = TextAlignmentOptions.MidlineRight;

        return valTmp;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Lógica del juego
    // ═══════════════════════════════════════════════════════════════════════

    private void StartBoard()
    {
        var go = new GameObject("BoardManager");
        go.transform.SetParent(transform, false);
        _boardManager = go.AddComponent<BoardManager>();

        float cardSize = _targetPairs <= 4 ? 140f
                       : _targetPairs <= 6 ? 118f : 98f;
        float spacing  = _targetPairs <= 4 ? 14f
                       : _targetPairs <= 6 ? 11f  : 9f;

        _boardManager.Initialize(_boardContainer, _targetPairs, cardSize, spacing);
        _boardManager.OnAttemptMade += OnAttempt;
        _boardManager.OnAllMatched  += OnAllMatched;
    }

    private void OnAttempt(int total)
    {
        _attempts = total;
        if (_attemptsLabel) _attemptsLabel.text = $"Intentos: {_attempts}";
    }

    private void OnAllMatched() => CompleteMinigame(CalcScore());

    private int CalcScore()
    {
        int bonus = Mathf.Max(0, _targetPairs - (_attempts - _targetPairs));
        return _targetPairs * 100 + bonus * 10;
    }

    // ─── Panel de resultado ───────────────────────────────────────────────

    private void ShowResultPanel(bool won)
    {
        // Actualizar datos
        if (_statsPairs)    _statsPairs.text    = $"{_targetPairs}/{_targetPairs}";
        if (_statsAttempts) _statsAttempts.text = $"{_attempts}";
        if (_statsTime)     _statsTime.text     = FormatTime(_elapsed);
        if (_statsScore)    _statsScore.text    = won ? $"{Score} pts" : "---";

        // Cambiar título y color de borde según resultado
        if (won)
        {
            if (_winTitle) { _winTitle.text = "¡Ganaste!"; _winTitle.color = C_GREEN; }

            // Borde superior verde
            var border = _winPanel?.transform.Find("Card/TopBorder")?.GetComponent<Image>();
            if (border) border.color = C_GREEN;
        }
        else
        {
            if (_winTitle) { _winTitle.text = "Tiempo agotado"; _winTitle.color = C_BTN_RED; }
            if (_statsPairs) _statsPairs.text = "Tiempo agotado";

            var border = _winPanel?.transform.Find("Card/TopBorder")?.GetComponent<Image>();
            if (border) border.color = C_BTN_RED;
        }

        if (_winPanel) _winPanel.SetActive(true);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Helpers de construcción UI
    // ═══════════════════════════════════════════════════════════════════════

    private Image MakePanel(Transform parent, string name, Color color, RectTransformCfg cfg)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        cfg.Apply(go.AddComponent<RectTransform>());
        var img = go.AddComponent<Image>();
        img.color = color;
        return img;
    }

    private TextMeshProUGUI MakeLabel(Transform parent, string name, string text,
                                Color color, float size, FontStyles style)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = text; t.color = color; t.fontSize = size;
        t.fontStyle = style; t.alignment = TextAlignmentOptions.Center;
        return t;
    }

    private void MakeButton(Transform parent, string name, string label,
                            Color bg, Vector2 pos, Vector2 size, System.Action cb)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        var img = go.AddComponent<Image>();
        img.color = bg;

        var btn = go.AddComponent<Button>();
        var bc  = btn.colors;
        bc.normalColor      = Color.white;
        bc.highlightedColor = new Color(1.12f, 1.12f, 1.12f);
        bc.pressedColor     = new Color(0.85f, 0.85f, 0.85f);
        bc.fadeDuration     = 0.06f;
        btn.colors = bc;
        btn.onClick.AddListener(() => cb?.Invoke());

        // Texto interior
        var tGO = new GameObject("Label");
        tGO.transform.SetParent(go.transform, false);
        var tRT = tGO.AddComponent<RectTransform>();
        tRT.anchorMin = Vector2.zero; tRT.anchorMax = Vector2.one;
        tRT.sizeDelta = Vector2.zero; tRT.anchoredPosition = Vector2.zero;
        var tmp = tGO.AddComponent<TextMeshProUGUI>();
        tmp.text = label; tmp.color = C_WHITE;
        tmp.fontSize = 29f; tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
    }

    // ─── Helpers de layout ────────────────────────────────────────────────

    private static void PlaceRT(GameObject go, Vector2 anchorMin, Vector2 anchorMax,
                                Vector2 pos, Vector2 size)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = size;
    }

    private static RectTransformCfg Stretch() =>
        new RectTransformCfg(Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

    private static RectTransformCfg AnchorRect(float x0, float y0, float x1, float y1,
                                               float ox0 = 0, float oy0 = 0,
                                               float ox1 = 0, float oy1 = 0) =>
        new RectTransformCfg(new Vector2(x0, y0), new Vector2(x1, y1),
                             new Vector2(ox0, oy0), new Vector2(ox1, oy1));

    private static RectTransformCfg CenterRect(float w, float h) =>
        new RectTransformCfg(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                             new Vector2(-w / 2f, -h / 2f), new Vector2(w / 2f, h / 2f));

    private static string FormatTime(float s)
    {
        int m = (int)(s / 60f), sec = (int)(s % 60f);
        return $"{m:00}:{sec:00}";
    }

    private static void EnsureEventSystem()
    {
        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var go = new GameObject("EventSystem");
            go.AddComponent<UnityEngine.EventSystems.EventSystem>();
            go.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
    }

    // ─── Struct auxiliar ──────────────────────────────────────────────────

    private struct RectTransformCfg
    {
        public Vector2 anchorMin, anchorMax, offsetMin, offsetMax;
        public RectTransformCfg(Vector2 mn, Vector2 mx, Vector2 oMin, Vector2 oMax)
        { anchorMin = mn; anchorMax = mx; offsetMin = oMin; offsetMax = oMax; }
        public void Apply(RectTransform rt)
        {
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin; rt.offsetMax = offsetMax;
        }
    }
}
