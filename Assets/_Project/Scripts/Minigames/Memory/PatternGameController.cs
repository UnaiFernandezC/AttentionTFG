using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Controlador principal del minijuego "Repite el dibujo" (categoría: Memoria).
///
/// 3 rondas secuenciales. Si el jugador falla cualquier ronda → derrota.
/// Si completa las 3 → victoria.
///
/// Layout diseñado para resolución landscape 1920×1080.
/// </summary>
public class PatternGameController : MinigameBase
{
    // ─── Inspector: Ronda 1 ───────────────────────────────────────────────
    [Header("Ronda 1 (Facil)")]
    public int   colsRound1    = 3;
    public int   rowsRound1    = 3;
    public int   patternRound1 = 4;
    public float timeRound1    = 4f;

    // ─── Inspector: Ronda 2 ───────────────────────────────────────────────
    [Header("Ronda 2 (Media)")]
    public int   colsRound2    = 4;
    public int   rowsRound2    = 4;
    public int   patternRound2 = 6;
    public float timeRound2    = 3f;

    // ─── Inspector: Ronda 3 ───────────────────────────────────────────────
    [Header("Ronda 3 (Dificil)")]
    public int   colsRound3    = 5;
    public int   rowsRound3    = 5;
    public int   patternRound3 = 9;
    public float timeRound3    = 2f;

    // ─── Estado de rondas ─────────────────────────────────────────────────
    private int       _currentRound = 0;
    private const int TOTAL_ROUNDS  = 3;
    private int       _totalScore   = 0;
    private int       _roundsWon    = 0;

    // ─── Fases de juego ───────────────────────────────────────────────────
    private enum Phase { Memorize, Recall, Result }
    private Phase _phase;

    // ─── Componentes ──────────────────────────────────────────────────────
    private PatternGridManager _grid;
    private RectTransform      _gridContainer;

    // ─── UI: pantalla de juego ────────────────────────────────────────────
    private TMPro.TextMeshProUGUI _instrLabel;
    private TMPro.TextMeshProUGUI _countdownLabel;
    private GameObject            _countdownPanel;
    private TMPro.TextMeshProUGUI _selectionLabel;
    private GameObject            _selectionPanel;
    private GameObject            _confirmBtn;
    private TMPro.TextMeshProUGUI _roundLabel;
    private Image[]               _roundDots;

    // ─── UI: paneles ──────────────────────────────────────────────────────
    private GameObject            _transPanel;
    private TMPro.TextMeshProUGUI _transTitle;
    private TMPro.TextMeshProUGUI _transSubtitle;
    private GameObject            _endPanel;
    private TMPro.TextMeshProUGUI _endTitle;
    private Image                 _endAccentBar;
    private TMPro.TextMeshProUGUI _statRounds;
    private TMPro.TextMeshProUGUI _statDetail;
    private TMPro.TextMeshProUGUI _statScore;

    // ─── Paleta de colores ────────────────────────────────────────────────
    private static readonly Color C_BG_DARK  = new Color(0.08f, 0.09f, 0.18f);
    private static readonly Color C_PANEL    = new Color(0.13f, 0.14f, 0.26f);
    private static readonly Color C_HEADER   = new Color(0.10f, 0.11f, 0.22f);
    private static readonly Color C_ACCENT   = new Color(0.25f, 0.55f, 1.00f);
    private static readonly Color C_GREEN    = new Color(0.20f, 0.78f, 0.48f);
    private static readonly Color C_RED      = new Color(0.85f, 0.25f, 0.32f);
    private static readonly Color C_ORANGE   = new Color(1.00f, 0.60f, 0.18f);
    private static readonly Color C_BTN_BLUE = new Color(0.22f, 0.50f, 0.95f);
    private static readonly Color C_BTN_GREY = new Color(0.30f, 0.32f, 0.40f);
    private static readonly Color C_TEXT_DIM = new Color(0.65f, 0.68f, 0.80f);
    private static readonly Color C_DOT_OFF  = new Color(0.25f, 0.27f, 0.45f);

    // ─── MinigameBase ─────────────────────────────────────────────────────

    protected override void OnMinigameStart()
    {
        EnsureEventSystem();
        BuildUI();
        ResetSession();
        StartRound(0);
    }

    protected override void OnMinigameComplete() { }
    protected override void OnMinigameFailed()   { }

    // ─── Configuración de ronda ───────────────────────────────────────────

    /// <summary>
    /// Devuelve la configuración de la ronda (índice 0-based).
    /// cellSize y spacing se ajustan automáticamente para que el grid
    /// quepa bien en pantalla landscape sin solaparse con el resto de la UI.
    ///
    ///   Ronda 0 → 3×3, celdas 120px → total grid 384×384
    ///   Ronda 1 → 4×4, celdas  96px → total grid 414×414
    ///   Ronda 2 → 5×5, celdas  78px → total grid 430×430
    /// </summary>
    private void GetRoundConfig(int idx,
                                out int cols, out int rows,
                                out int patternCount, out float displayTime,
                                out float cellSize,  out float spacing)
    {
        switch (idx)
        {
            case 0:
                cols = colsRound1; rows = rowsRound1;
                patternCount = patternRound1; displayTime = timeRound1;
                cellSize = 120f; spacing = 12f;
                break;
            case 1:
                cols = colsRound2; rows = rowsRound2;
                patternCount = patternRound2; displayTime = timeRound2;
                cellSize = 96f;  spacing = 10f;
                break;
            default:
                cols = colsRound3; rows = rowsRound3;
                patternCount = patternRound3; displayTime = timeRound3;
                cellSize = 78f;  spacing = 10f;
                break;
        }
    }

    // ─── Gestión de rondas ────────────────────────────────────────────────

    private void ResetSession()
    {
        _currentRound = 0;
        _totalScore   = 0;
        _roundsWon    = 0;
    }

    private void StartRound(int roundIndex)
    {
        _currentRound = roundIndex;
        _phase        = Phase.Memorize;

        int cols, rows, patternCount;
        float displayTime, cellSize, spacing;
        GetRoundConfig(roundIndex, out cols, out rows,
                       out patternCount, out displayTime,
                       out cellSize, out spacing);

        UpdateRoundIndicator(roundIndex);

        _endPanel.SetActive(false);
        _transPanel.SetActive(false);
        _selectionPanel.SetActive(false);
        _confirmBtn.SetActive(false);
        _countdownPanel.SetActive(false);

        // Destruir grid anterior
        for (int i = _gridContainer.childCount - 1; i >= 0; i--)
            DestroyImmediate(_gridContainer.GetChild(i).gameObject);

        if (_grid != null) { Destroy(_grid.gameObject); _grid = null; }

        var gridGO = new GameObject("PatternGridManager");
        gridGO.transform.SetParent(transform, false);
        _grid = gridGO.AddComponent<PatternGridManager>();
        _grid.OnSelectionChanged += (selected) =>
        {
            int c, r, pc; float dt, cs, sp;
            GetRoundConfig(_currentRound, out c, out r, out pc, out dt, out cs, out sp);
            UpdateSelectionLabel(selected, pc);
        };

        // Inicializar con los tamaños de celda de esta ronda
        _grid.Initialize(_gridContainer, cols, rows, cellSize, spacing);
        _grid.GeneratePattern(patternCount);
        _grid.EnableInput(false);

        SetInstructions("Memoriza el patron");
        StartCoroutine(MemorizePhase(patternCount, displayTime));
    }

    // ─── Fase 1: MEMORIZAR ────────────────────────────────────────────────

    private IEnumerator MemorizePhase(int patternCount, float displayTime)
    {
        yield return new WaitForSeconds(0.3f);
        _grid.ShowPattern();
        _countdownPanel.SetActive(true);

        float remaining = displayTime;
        while (remaining > 0f)
        {
            _countdownLabel.text  = Mathf.CeilToInt(remaining).ToString();
            _countdownLabel.color = remaining <= 1.5f ? C_RED : C_ACCENT;
            remaining -= Time.deltaTime;
            yield return null;
        }

        _countdownPanel.SetActive(false);
        StartRecallPhase(patternCount);
    }

    // ─── Fase 2: RESPONDER ────────────────────────────────────────────────

    private void StartRecallPhase(int patternCount)
    {
        _phase = Phase.Recall;
        _grid.HidePattern();
        SetInstructions("Reproduce el patron");
        UpdateSelectionLabel(0, patternCount);
        _selectionPanel.SetActive(true);
        _confirmBtn.SetActive(true);
        _grid.EnableInput(true);
    }

    private void UpdateSelectionLabel(int selected, int needed)
    {
        _selectionLabel.text  = $"{selected} / {needed} seleccionadas";
        _selectionLabel.color = (selected == needed) ? C_GREEN : C_TEXT_DIM;
    }

    // ─── Fase 3: RESULTADO ────────────────────────────────────────────────

    private void HandleConfirm()
    {
        if (_phase != Phase.Recall) return;
        _phase = Phase.Result;
        _confirmBtn.SetActive(false);
        _selectionPanel.SetActive(false);
        _grid.EnableInput(false);
        StartCoroutine(ShowResultSequence());
    }

    private IEnumerator ShowResultSequence()
    {
        var (correct, wrong, missed) = _grid.ShowResult();
        yield return new WaitForSeconds(1.6f);

        bool won = (wrong == 0 && missed == 0);
        int roundScore = Mathf.Max(0, correct * 100 - wrong * 30 - missed * 20);
        _totalScore += roundScore;

        if (!won)
        {
            FailMinigame();
            ShowEndPanel(false);
        }
        else
        {
            _roundsWon++;
            if (_currentRound >= TOTAL_ROUNDS - 1)
            {
                CompleteMinigame(_totalScore);
                ShowEndPanel(true);
            }
            else
            {
                StartCoroutine(RoundTransition(_currentRound));
            }
        }
    }

    private IEnumerator RoundTransition(int completedRound)
    {
        _transPanel.SetActive(true);
        _transTitle.text    = $"Ronda {completedRound + 1} completada!";
        _transSubtitle.text = "Preparate para la siguiente...";
        yield return new WaitForSeconds(0.6f);
        for (int i = 3; i >= 1; i--)
        {
            _transSubtitle.text = $"Siguiente ronda en {i}...";
            yield return new WaitForSeconds(1f);
        }
        _transPanel.SetActive(false);
        StartRound(completedRound + 1);
    }

    // ─── Construcción de UI (layout landscape 1920×1080) ─────────────────
    //
    // Distribución vertical (canvas effective height ≈ 1080):
    //   [0 – 90]    Bottom bar
    //   [90 – 162]  Confirm button area
    //   [162 – 650] Grid (center, y=-50 from canvas center)
    //   [818 – 880] Countdown / Selection panel (top anchor, y=-232)
    //   [880 – 924] Instrucción (top anchor, y=-156)
    //   [924 – 962] Round bar (top anchor, y=-118)
    //   [962 – 1080] Header bar (top anchor, y=-80)
    //
    // La separación garantiza que no haya solapamiento en ninguna de las 3 rondas.

    private void BuildUI()
    {
        // ── Canvas ──
        var canvasGO = new GameObject("Canvas");
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);  // landscape
        scaler.matchWidthOrHeight  = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        var root = canvasGO.GetComponent<RectTransform>();
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.sizeDelta = Vector2.zero;

        // ── Fondo ──
        MakePanel(root, "BG", C_BG_DARK, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        // ── Header (80px desde arriba) ──
        var header = MakePanel(root, "Header", C_HEADER,
            new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0f, -40f), new Vector2(0f, 80f));
        var headerRT = header.GetComponent<RectTransform>();

        MakePanel(headerRT, "Accent", C_ACCENT,
            new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(0f, 2f), new Vector2(0f, 3f));

        var title = MakeLabel(headerRT, "Title", "Repite el dibujo",
            Color.white, 42f,
            new Vector2(0.08f, 0f), new Vector2(0.92f, 1f), Vector2.zero, Vector2.zero);
        title.fontStyle = TMPro.FontStyles.Bold;
        title.alignment = TMPro.TextAlignmentOptions.Center;

        // ── Barra de ronda (38px, debajo del header) ──
        var roundBar = MakePanel(root, "RoundBar", C_HEADER,
            new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0f, -99f), new Vector2(0f, 38f));
        var roundBarRT = roundBar.GetComponent<RectTransform>();

        _roundLabel = MakeLabel(roundBarRT, "RoundLbl", "Ronda 1 / 3",
            new Color(0.60f, 0.64f, 0.88f), 26f,
            new Vector2(0.04f, 0f), new Vector2(0.55f, 1f), Vector2.zero, Vector2.zero);
        _roundLabel.alignment = TMPro.TextAlignmentOptions.MidlineLeft;

        _roundDots = new Image[TOTAL_ROUNDS];
        for (int i = 0; i < TOTAL_ROUNDS; i++)
        {
            var dot = new GameObject($"Dot{i}");
            dot.transform.SetParent(roundBarRT, false);
            var dotRT = dot.AddComponent<RectTransform>();
            dotRT.anchorMin        = new Vector2(1f, 0.5f);
            dotRT.anchorMax        = new Vector2(1f, 0.5f);
            dotRT.pivot            = new Vector2(0.5f, 0.5f);
            dotRT.anchoredPosition = new Vector2(-50f - (TOTAL_ROUNDS - 1 - i) * 28f, 0f);
            dotRT.sizeDelta        = new Vector2(18f, 18f);
            _roundDots[i] = dot.AddComponent<Image>();
            _roundDots[i].color = C_DOT_OFF;
        }

        // ── Barra de instrucción (40px, debajo de ronda) ──
        var instrBar = MakePanel(root, "InstrBar",
            new Color(0.12f, 0.14f, 0.28f),
            new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0f, -138f), new Vector2(0f, 40f));
        _instrLabel = MakeLabel(instrBar.GetComponent<RectTransform>(), "Instr",
            "Memoriza el patron",
            new Color(0.72f, 0.76f, 0.92f), 28f,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        _instrLabel.alignment = TMPro.TextAlignmentOptions.Center;

        // ── Panel de cuenta atrás (debajo de la instrucción, SIN solapar el grid) ──
        // Posicionado en top anchor a y=-200 → queda entre la instrucción y el grid
        _countdownPanel = new GameObject("CountdownPanel");
        _countdownPanel.transform.SetParent(root, false);
        var cdRT = _countdownPanel.AddComponent<RectTransform>();
        cdRT.anchorMin        = new Vector2(0.5f, 1f);
        cdRT.anchorMax        = new Vector2(0.5f, 1f);
        cdRT.pivot            = new Vector2(0.5f, 1f);
        cdRT.anchoredPosition = new Vector2(0f, -180f);
        cdRT.sizeDelta        = new Vector2(160f, 56f);
        _countdownPanel.AddComponent<Image>().color = new Color(0.14f, 0.16f, 0.30f);

        _countdownLabel = MakeLabel(cdRT, "CdLabel", "4",
            C_ACCENT, 50f, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        _countdownLabel.fontStyle = TMPro.FontStyles.Bold;
        _countdownLabel.alignment = TMPro.TextAlignmentOptions.Center;
        _countdownPanel.SetActive(false);

        // ── Panel de selección (misma posición que countdown, solo en recall) ──
        _selectionPanel = new GameObject("SelectionPanel");
        _selectionPanel.transform.SetParent(root, false);
        var spRT = _selectionPanel.AddComponent<RectTransform>();
        spRT.anchorMin        = new Vector2(0.5f, 1f);
        spRT.anchorMax        = new Vector2(0.5f, 1f);
        spRT.pivot            = new Vector2(0.5f, 1f);
        spRT.anchoredPosition = new Vector2(0f, -180f);
        spRT.sizeDelta        = new Vector2(420f, 52f);
        _selectionPanel.AddComponent<Image>().color = new Color(0.14f, 0.16f, 0.30f);

        _selectionLabel = MakeLabel(spRT, "SelLabel", "0 / 0 seleccionadas",
            C_TEXT_DIM, 30f, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        _selectionLabel.alignment = TMPro.TextAlignmentOptions.Center;
        _selectionPanel.SetActive(false);

        // ── Contenedor del grid (centrado en pantalla con offset hacia arriba) ──
        // Offset y=+30 empuja el grid ligeramente por encima del centro,
        // dejando espacio en la zona inferior para el botón Confirmar.
        var gridGO = new GameObject("GridContainer");
        gridGO.transform.SetParent(root, false);
        _gridContainer = gridGO.AddComponent<RectTransform>();
        _gridContainer.anchorMin        = new Vector2(0.5f, 0.5f);
        _gridContainer.anchorMax        = new Vector2(0.5f, 0.5f);
        _gridContainer.pivot            = new Vector2(0.5f, 0.5f);
        _gridContainer.anchoredPosition = new Vector2(0f, 30f);
        _gridContainer.sizeDelta        = new Vector2(600f, 600f);

        // ── Botón Confirmar (centrado, por encima de la barra inferior) ──
        _confirmBtn = new GameObject("BtnConfirm");
        _confirmBtn.transform.SetParent(root, false);
        var confirmRT = _confirmBtn.AddComponent<RectTransform>();
        confirmRT.anchorMin        = new Vector2(0.5f, 0f);
        confirmRT.anchorMax        = new Vector2(0.5f, 0f);
        confirmRT.pivot            = new Vector2(0.5f, 0f);
        confirmRT.anchoredPosition = new Vector2(0f, 106f);
        confirmRT.sizeDelta        = new Vector2(560f, 68f);

        var confirmImg = _confirmBtn.AddComponent<Image>();
        confirmImg.color = C_GREEN;
        var confirmBtn = _confirmBtn.AddComponent<Button>();
        confirmBtn.targetGraphic = confirmImg;
        var cbc = confirmBtn.colors;
        cbc.normalColor = Color.white; cbc.highlightedColor = new Color(1f, 1f, 1f, 0.85f);
        cbc.pressedColor = new Color(0.75f, 0.75f, 0.75f); confirmBtn.colors = cbc;
        confirmBtn.onClick.AddListener(HandleConfirm);

        var cTxtGO = new GameObject("Text");
        cTxtGO.transform.SetParent(_confirmBtn.transform, false);
        ApplyRT(cTxtGO.AddComponent<RectTransform>(),
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var cTmp = cTxtGO.AddComponent<TMPro.TextMeshProUGUI>();
        cTmp.text = "Confirmar respuesta"; cTmp.color = Color.white;
        cTmp.fontSize = 36f; cTmp.fontStyle = TMPro.FontStyles.Bold;
        cTmp.alignment = TMPro.TextAlignmentOptions.Center;
        _confirmBtn.SetActive(false);

        // ── Barra inferior (90px) ──
        var botBar = MakePanel(root, "BotBar", C_HEADER,
            new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(0f, 45f), new Vector2(0f, 90f));
        var botRT = botBar.GetComponent<RectTransform>();

        MakeButton(botRT, "BtnRestart", "Reiniciar", C_BTN_GREY,
            new Vector2(0.06f, 0.12f), new Vector2(0.46f, 0.88f), Vector2.zero, Vector2.zero,
            () => { StopAllCoroutines(); ResetSession(); StartRound(0); });

        MakeButton(botRT, "BtnMenu", "Volver al menu", C_BTN_BLUE,
            new Vector2(0.54f, 0.12f), new Vector2(0.94f, 0.88f), Vector2.zero, Vector2.zero,
            ReturnToGameSelector);

        // ── Panel de transición ──
        BuildTransitionPanel(root);

        // ── Panel de fin ──
        BuildEndPanel(root);
    }

    // ─── Indicador de ronda ───────────────────────────────────────────────

    private void UpdateRoundIndicator(int roundIndex)
    {
        _roundLabel.text = $"Ronda {roundIndex + 1} / {TOTAL_ROUNDS}";
        for (int i = 0; i < _roundDots.Length; i++)
            _roundDots[i].color = i <= roundIndex ? C_ACCENT : C_DOT_OFF;
    }

    // ─── Panel de transición ─────────────────────────────────────────────

    private void BuildTransitionPanel(RectTransform root)
    {
        _transPanel = new GameObject("TransPanel");
        _transPanel.transform.SetParent(root, false);
        var rt = _transPanel.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero;
        _transPanel.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.82f);

        var card = MakePanel(rt, "Card", C_PANEL,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(680f, 300f));
        var cardRT = card.GetComponent<RectTransform>();

        MakePanel(cardRT, "AccentTop", C_GREEN,
            new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0f, -11f), new Vector2(0f, 22f));

        _transTitle = MakeLabel(cardRT, "TransTitle", "Ronda 1 completada!",
            Color.white, 52f,
            new Vector2(0.05f, 0.42f), new Vector2(0.95f, 1f), Vector2.zero, Vector2.zero);
        _transTitle.fontStyle = TMPro.FontStyles.Bold;
        _transTitle.alignment = TMPro.TextAlignmentOptions.Center;

        _transSubtitle = MakeLabel(cardRT, "TransSub", "Siguiente ronda en 3...",
            C_TEXT_DIM, 34f,
            new Vector2(0.05f, 0f), new Vector2(0.95f, 0.42f), Vector2.zero, Vector2.zero);
        _transSubtitle.alignment = TMPro.TextAlignmentOptions.Center;

        _transPanel.SetActive(false);
    }

    // ─── Panel de fin ─────────────────────────────────────────────────────

    private void BuildEndPanel(RectTransform root)
    {
        _endPanel = new GameObject("EndPanel");
        _endPanel.transform.SetParent(root, false);
        var overlayRT = _endPanel.AddComponent<RectTransform>();
        overlayRT.anchorMin = Vector2.zero; overlayRT.anchorMax = Vector2.one;
        overlayRT.sizeDelta = Vector2.zero;
        _endPanel.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.80f);

        var card = MakePanel(overlayRT, "Card", C_PANEL,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(720f, 520f));
        var cardRT = card.GetComponent<RectTransform>();

        _endAccentBar = MakePanel(cardRT, "AccentBar", C_GREEN,
            new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0f, -13f), new Vector2(0f, 26f)).GetComponent<Image>();

        _endTitle = MakeLabel(cardRT, "EndTitle", "Completado!",
            Color.white, 58f,
            new Vector2(0.05f, 1f), new Vector2(0.95f, 1f),
            new Vector2(0f, -80f), new Vector2(0f, -26f));
        _endTitle.fontStyle = TMPro.FontStyles.Bold;
        _endTitle.alignment = TMPro.TextAlignmentOptions.Center;

        MakePanel(cardRT, "Divider", new Color(1f, 1f, 1f, 0.08f),
            new Vector2(0.08f, 1f), new Vector2(0.92f, 1f),
            new Vector2(0f, -118f), new Vector2(0f, 2f));

        float rowTop = -132f, rowH = 76f;
        _statRounds = BuildStatRow(cardRT, "Rondas completadas", "0 / 3", rowTop);
        _statDetail = BuildStatRow(cardRT, "Estado",             "-",    rowTop - rowH);
        _statScore  = BuildStatRow(cardRT, "Puntuacion total",   "0",    rowTop - rowH * 2f);

        MakeButton(cardRT, "BtnReplay", "Jugar de nuevo", C_BTN_BLUE,
            new Vector2(0.06f, 0f), new Vector2(0.94f, 0f),
            new Vector2(0f, 84f), new Vector2(0f, 66f),
            () => { _endPanel.SetActive(false); StopAllCoroutines(); ResetSession(); StartRound(0); });

        MakeButton(cardRT, "BtnMenu", "Menu principal", C_BTN_GREY,
            new Vector2(0.06f, 0f), new Vector2(0.94f, 0f),
            new Vector2(0f, 14f), new Vector2(0f, 62f),
            ReturnToGameSelector);

        _endPanel.SetActive(false);
    }

    private void ShowEndPanel(bool won)
    {
        _endAccentBar.color = won ? C_GREEN : C_RED;
        _endTitle.text      = won ? "Completado!" : "Incorrecto";
        _statRounds.text    = $"{_roundsWon} / {TOTAL_ROUNDS}";
        _statScore.text     = _totalScore.ToString();
        _statDetail.text    = won ? "Todas superadas!" : $"Ronda {_currentRound + 1} fallada";
        _endPanel.SetActive(true);
    }

    private void BuildLegendDot(RectTransform parent, Color dotColor,
                                string label, float xOffset)
    {
        var go = new GameObject($"L_{label}");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(xOffset, 0f); rt.sizeDelta = new Vector2(160f, 28f);

        var dot = new GameObject("D");
        dot.transform.SetParent(go.transform, false);
        var dRT = dot.AddComponent<RectTransform>();
        dRT.anchorMin = new Vector2(0f, 0.15f); dRT.anchorMax = new Vector2(0f, 0.85f);
        dRT.anchoredPosition = new Vector2(12f, 0f); dRT.sizeDelta = new Vector2(18f, 0f);
        dot.AddComponent<Image>().color = dotColor;

        var txt = new GameObject("T");
        txt.transform.SetParent(go.transform, false);
        var tRT = txt.AddComponent<RectTransform>();
        tRT.anchorMin = new Vector2(0f, 0f); tRT.anchorMax = new Vector2(1f, 1f);
        tRT.anchoredPosition = new Vector2(16f, 0f); tRT.sizeDelta = Vector2.zero;
        var tmp = txt.AddComponent<TMPro.TextMeshProUGUI>();
        tmp.text = label; tmp.color = C_TEXT_DIM;
        tmp.fontSize = 22f; tmp.alignment = TMPro.TextAlignmentOptions.MidlineLeft;
    }

    // ─── UI helpers ───────────────────────────────────────────────────────

    private void SetInstructions(string text) => _instrLabel.text = text;

    private TMPro.TextMeshProUGUI BuildStatRow(RectTransform parent,
                                               string label, string value, float anchoredY)
    {
        var rowGO = new GameObject($"Row_{label}");
        rowGO.transform.SetParent(parent, false);
        var rowRT = rowGO.AddComponent<RectTransform>();
        rowRT.anchorMin        = new Vector2(0.08f, 1f);
        rowRT.anchorMax        = new Vector2(0.92f, 1f);
        rowRT.pivot            = new Vector2(0.5f, 1f);
        rowRT.anchoredPosition = new Vector2(0f, anchoredY);
        rowRT.sizeDelta        = new Vector2(0f, 66f);

        var lblGO = new GameObject("Lbl");
        lblGO.transform.SetParent(rowGO.transform, false);
        ApplyRT(lblGO.AddComponent<RectTransform>(),
            new Vector2(0f, 0f), new Vector2(0.60f, 1f), Vector2.zero, Vector2.zero);
        var lblTmp = lblGO.AddComponent<TMPro.TextMeshProUGUI>();
        lblTmp.text = label; lblTmp.color = C_TEXT_DIM;
        lblTmp.fontSize = 30f; lblTmp.alignment = TMPro.TextAlignmentOptions.MidlineLeft;

        var valGO = new GameObject("Val");
        valGO.transform.SetParent(rowGO.transform, false);
        ApplyRT(valGO.AddComponent<RectTransform>(),
            new Vector2(0.60f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        var valTmp = valGO.AddComponent<TMPro.TextMeshProUGUI>();
        valTmp.text = value; valTmp.color = Color.white;
        valTmp.fontSize = 36f; valTmp.fontStyle = TMPro.FontStyles.Bold;
        valTmp.alignment = TMPro.TextAlignmentOptions.MidlineRight;
        return valTmp;
    }

    private static void ApplyRT(RectTransform rt, Vector2 amin, Vector2 amax,
                                Vector2 pos, Vector2 sd)
    {
        rt.anchorMin = amin; rt.anchorMax = amax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = sd;
    }

    private GameObject MakePanel(RectTransform parent, string name, Color color,
                                  Vector2 amin, Vector2 amax, Vector2 pos, Vector2 sd)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        ApplyRT(go.AddComponent<RectTransform>(), amin, amax, pos, sd);
        go.AddComponent<Image>().color = color;
        return go;
    }

    private TMPro.TextMeshProUGUI MakeLabel(RectTransform parent, string name, string text,
                                            Color color, float fontSize,
                                            Vector2 amin, Vector2 amax, Vector2 pos, Vector2 sd)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        ApplyRT(go.AddComponent<RectTransform>(), amin, amax, pos, sd);
        var tmp = go.AddComponent<TMPro.TextMeshProUGUI>();
        tmp.text = text; tmp.color = color; tmp.fontSize = fontSize;
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        return tmp;
    }

    private void MakeButton(RectTransform parent, string name, string label,
                            Color bgColor, Vector2 amin, Vector2 amax,
                            Vector2 pos, Vector2 sd,
                            UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        ApplyRT(go.AddComponent<RectTransform>(), amin, amax, pos, sd);
        var img = go.AddComponent<Image>(); img.color = bgColor;
        var btn = go.AddComponent<Button>(); btn.targetGraphic = img;
        var cb = btn.colors;
        cb.normalColor = Color.white; cb.highlightedColor = new Color(1f,1f,1f,0.85f);
        cb.pressedColor = new Color(0.75f,0.75f,0.75f); btn.colors = cb;
        btn.onClick.AddListener(onClick);

        var txtGO = new GameObject("Text");
        txtGO.transform.SetParent(go.transform, false);
        ApplyRT(txtGO.AddComponent<RectTransform>(),
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var tmp = txtGO.AddComponent<TMPro.TextMeshProUGUI>();
        tmp.text = label; tmp.color = Color.white;
        tmp.fontSize = 34f; tmp.fontStyle = TMPro.FontStyles.Bold;
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
    }

    private static void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() == null)
        {
            var esGO = new GameObject("EventSystem");
            esGO.AddComponent<EventSystem>();
            esGO.AddComponent<StandaloneInputModule>();
        }
    }
}
