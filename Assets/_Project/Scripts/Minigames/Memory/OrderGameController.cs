using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Controlador del minijuego "Orden correcto" (categoría: Planificación).
///
/// El juego se divide en 3 rondas secuenciales:
///   Ronda 1 → numbersRound1 números, timeLimitRound1 segundos
///   Ronda 2 → numbersRound2 números, timeLimitRound2 segundos
///   Ronda 3 → numbersRound3 números, timeLimitRound3 segundos
///
/// Si el jugador completa las 3 rondas → victoria.
/// Si se le acaba el tiempo en cualquier ronda → derrota.
/// </summary>
public class OrderGameController : MinigameBase
{
    // ─── Configuración por ronda (editable en el Inspector) ───────────────
    [Header("Ronda 1")]
    public int   numbersRound1   = 4;
    public float timeLimitRound1 = 15f;

    [Header("Ronda 2")]
    public int   numbersRound2   = 6;
    public float timeLimitRound2 = 20f;

    [Header("Ronda 3")]
    public int   numbersRound3   = 10;
    public float timeLimitRound3 = 30f;

    // ─── Componentes de juego ─────────────────────────────────────────────
    private OrderManager _orderManager;

    // ─── Estado de rondas ─────────────────────────────────────────────────
    private int   _currentRound   = 0;       // 0-based (0, 1, 2)
    private const int TOTAL_ROUNDS = 3;
    private int   _totalErrors     = 0;
    private float _totalTime       = 0f;
    private int   _totalScore      = 0;

    // Estado de la ronda actual
    private bool  _roundRunning    = false;
    private float _timeRemaining   = 0f;
    private float _roundElapsed    = 0f;
    private int   _roundErrors     = 0;

    // ─── UI: pantalla de juego ────────────────────────────────────────────
    private Canvas                    _canvas;
    private TMPro.TextMeshProUGUI     _errorsLabel;
    private TMPro.TextMeshProUGUI     _timerLabel;
    private TMPro.TextMeshProUGUI     _nextLabel;
    private GameObject                _nextPanel;
    private RectTransform             _gridContainer;

    // Indicador de ronda
    private TMPro.TextMeshProUGUI     _roundLabel;
    private Image[]                   _roundDots;

    // ─── UI: panel de transición entre rondas ─────────────────────────────
    private GameObject                _transPanel;
    private TMPro.TextMeshProUGUI     _transTitle;
    private TMPro.TextMeshProUGUI     _transSubtitle;

    // ─── UI: panel de fin ─────────────────────────────────────────────────
    private GameObject                _endPanel;
    private TMPro.TextMeshProUGUI     _endTitle;
    private TMPro.TextMeshProUGUI     _statRounds;
    private TMPro.TextMeshProUGUI     _statErrors;
    private TMPro.TextMeshProUGUI     _statTime;
    private TMPro.TextMeshProUGUI     _statScore;
    private Image                     _endAccentBar;

    // ─── Paleta de colores ────────────────────────────────────────────────
    private static readonly Color C_BG_DARK  = new Color(0.08f, 0.09f, 0.18f);
    private static readonly Color C_PANEL    = new Color(0.13f, 0.14f, 0.26f);
    private static readonly Color C_ACCENT   = new Color(0.25f, 0.55f, 1.00f);
    private static readonly Color C_GREEN    = new Color(0.20f, 0.78f, 0.48f);
    private static readonly Color C_RED      = new Color(0.85f, 0.25f, 0.32f);
    private static readonly Color C_BTN_BLUE = new Color(0.22f, 0.50f, 0.95f);
    private static readonly Color C_BTN_GREY = new Color(0.30f, 0.32f, 0.40f);
    private static readonly Color C_TEXT_DIM = new Color(0.65f, 0.68f, 0.80f);
    private static readonly Color C_DOT_OFF  = new Color(0.25f, 0.27f, 0.45f);

    // ─── MinigameBase ─────────────────────────────────────────────────────

    protected override void OnMinigameStart()
    {
        EnsureEventSystem();
        BuildUI();
        ResetTotals();
        StartRound(0);
    }

    protected override void OnMinigameComplete() { }
    protected override void OnMinigameFailed()   { }

    // ─── Lógica de rondas ─────────────────────────────────────────────────

    private void ResetTotals()
    {
        _currentRound = 0;
        _totalErrors  = 0;
        _totalTime    = 0f;
        _totalScore   = 0;
    }

    private void StartRound(int roundIndex)
    {
        _currentRound  = roundIndex;
        _roundErrors   = 0;
        _roundElapsed  = 0f;
        _roundRunning  = true;

        // Configurar tiempo y número de elementos de esta ronda
        int   count;
        float limit;
        GetRoundConfig(roundIndex, out count, out limit);
        _timeRemaining = limit;

        // UI: actualizar indicador de ronda
        UpdateRoundIndicator(roundIndex);

        // UI: indicador "Siguiente" (solo en rondas 1 y 2)
        bool showNext = roundIndex < 2;
        _nextPanel.SetActive(showNext);
        if (showNext && _nextLabel != null)
            _nextLabel.text = "Siguiente: 1";

        // UI: temporizador
        _errorsLabel.text = "Errores: 0";
        UpdateTimerUI();

        // Ocultar paneles de transición y fin
        _transPanel.SetActive(false);
        _endPanel.SetActive(false);

        // Destruir todos los botones del grid anterior de forma inmediata
        // (Destroy() es diferido; DestroyImmediate elimina antes de que se rendericen los nuevos)
        for (int i = _gridContainer.childCount - 1; i >= 0; i--)
            DestroyImmediate(_gridContainer.GetChild(i).gameObject);

        // Destruir el OrderManager anterior
        if (_orderManager != null)
        {
            Destroy(_orderManager.gameObject);
            _orderManager = null;
        }

        var omGO = new GameObject("OrderManager");
        omGO.transform.SetParent(transform, false);
        _orderManager = omGO.AddComponent<OrderManager>();

        _orderManager.OnCorrectPress += HandleCorrect;
        _orderManager.OnWrongPress   += HandleWrong;
        _orderManager.OnComplete     += HandleRoundComplete;

        _orderManager.Initialize(_gridContainer, count);
    }

    private void GetRoundConfig(int roundIndex, out int count, out float limit)
    {
        switch (roundIndex)
        {
            case 0:  count = numbersRound1; limit = timeLimitRound1; break;
            case 1:  count = numbersRound2; limit = timeLimitRound2; break;
            default: count = numbersRound3; limit = timeLimitRound3; break;
        }
    }

    // ─── Handlers de OrderManager ─────────────────────────────────────────

    private void HandleCorrect(int nextExpected)
    {
        bool showNext = _currentRound < 2;
        if (showNext && _nextLabel != null)
        {
            int count; float limit;
            GetRoundConfig(_currentRound, out count, out limit);
            if (nextExpected <= count)
                _nextLabel.text = $"Siguiente: {nextExpected}";
        }
    }

    private void HandleWrong(int totalWrong)
    {
        _roundErrors++;
        _totalErrors++;
        _errorsLabel.text = $"Errores: {_totalErrors}";
    }

    private void HandleRoundComplete()
    {
        _roundRunning = false;
        _totalTime   += _roundElapsed;

        // Puntuación de esta ronda
        int count; float limit;
        GetRoundConfig(_currentRound, out count, out limit);
        int roundScore = Mathf.Max(0, count * 100 - _roundErrors * 15 +
                                   Mathf.RoundToInt(_timeRemaining * 2f));
        _totalScore += roundScore;

        bool isLastRound = (_currentRound >= TOTAL_ROUNDS - 1);

        if (isLastRound)
        {
            // Todas las rondas completadas
            CompleteMinigame(_totalScore);
            ShowEndPanel(won: true);
        }
        else
        {
            // Mostrar transición y avanzar a la siguiente ronda
            StartCoroutine(RoundTransition(_currentRound));
        }
    }

    private IEnumerator RoundTransition(int completedRound)
    {
        // Mostrar panel de transición
        _transPanel.SetActive(true);
        _transTitle.text    = $"Ronda {completedRound + 1} completada!";
        _transSubtitle.text = "Preparate para la siguiente...";

        yield return new WaitForSeconds(0.6f);

        // Cuenta atrás visual: 3 – 2 – 1
        for (int i = 3; i >= 1; i--)
        {
            _transSubtitle.text = $"Siguiente ronda en {i}...";
            yield return new WaitForSeconds(1f);
        }

        _transPanel.SetActive(false);
        StartRound(completedRound + 1);
    }

    // ─── Update (temporizador) ────────────────────────────────────────────

    private void Update()
    {
        if (!_roundRunning) return;

        _roundElapsed  += Time.deltaTime;
        _timeRemaining -= Time.deltaTime;
        UpdateTimerUI();

        if (_timeRemaining <= 0f)
        {
            _roundRunning = false;
            _totalTime   += _roundElapsed;
            FailMinigame();
            ShowEndPanel(won: false);
        }
    }

    // ─── Construcción de UI ───────────────────────────────────────────────

    private void BuildUI()
    {
        // ── Canvas ──
        var canvasGO = new GameObject("Canvas");
        canvasGO.transform.SetParent(transform, false);
        _canvas = canvasGO.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight  = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        var root = canvasGO.GetComponent<RectTransform>();
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.sizeDelta = Vector2.zero;

        // ── Fondo ──
        MakePanel(root, "BG", C_BG_DARK, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        // ── Barra superior ──
        var headerBar = MakePanel(root, "HeaderBar",
            new Color(0.10f, 0.11f, 0.22f),
            new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0f, -110f), new Vector2(0f, 110f));
        var headerRT = headerBar.GetComponent<RectTransform>();

        // Franja accent inferior del header
        MakePanel(headerRT, "Accent", C_ACCENT,
            new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(0f, 3f), new Vector2(0f, 3f));

        // Título
        var titleLbl = MakeLabel(headerRT, "Title", "Orden correcto",
            Color.white, 50f,
            new Vector2(0.12f, 0f), new Vector2(0.88f, 1f), Vector2.zero, Vector2.zero);
        titleLbl.fontStyle = TMPro.FontStyles.Bold;
        titleLbl.alignment = TMPro.TextAlignmentOptions.Center;

        // Errores (izquierda)
        _errorsLabel = MakeLabel(headerRT, "Errors", "Errores: 0",
            C_TEXT_DIM, 34f,
            new Vector2(0f, 0f), new Vector2(0.35f, 1f),
            new Vector2(24f, 0f), new Vector2(0f, 0f));
        _errorsLabel.alignment = TMPro.TextAlignmentOptions.MidlineLeft;

        // Temporizador (derecha)
        _timerLabel = MakeLabel(headerRT, "Timer", "0:15",
            C_ACCENT, 44f,
            new Vector2(0.65f, 0f), new Vector2(1f, 1f),
            new Vector2(0f, 0f), new Vector2(-24f, 0f));
        _timerLabel.fontStyle = TMPro.FontStyles.Bold;
        _timerLabel.alignment = TMPro.TextAlignmentOptions.MidlineRight;

        // ── Indicador de ronda ──
        BuildRoundIndicator(root);

        // ── Franja de instrucción ──
        var instrBar = MakePanel(root, "InstrBar",
            new Color(0.12f, 0.14f, 0.28f),
            new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0f, -200f), new Vector2(0f, 48f));
        var instrLbl = MakeLabel(instrBar.GetComponent<RectTransform>(), "Instr",
            "Pulsa los numeros en orden: del menor al mayor",
            new Color(0.72f, 0.76f, 0.92f), 31f,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        instrLbl.alignment = TMPro.TextAlignmentOptions.Center;

        // ── Panel "Siguiente: X" ──
        _nextPanel = new GameObject("NextPanel");
        _nextPanel.transform.SetParent(root, false);
        var nextRT = _nextPanel.AddComponent<RectTransform>();
        nextRT.anchorMin        = new Vector2(0.5f, 1f);
        nextRT.anchorMax        = new Vector2(0.5f, 1f);
        nextRT.pivot            = new Vector2(0.5f, 1f);
        nextRT.anchoredPosition = new Vector2(0f, -270f);
        nextRT.sizeDelta        = new Vector2(420f, 72f);
        var nextBg = _nextPanel.AddComponent<Image>();
        nextBg.color = new Color(0.16f, 0.18f, 0.34f);
        _nextLabel = MakeLabel(nextRT, "NextLbl", "Siguiente: 1",
            Color.white, 42f,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        _nextLabel.fontStyle = TMPro.FontStyles.Bold;
        _nextLabel.alignment = TMPro.TextAlignmentOptions.Center;
        _nextPanel.SetActive(true);

        // ── Contenedor del grid ──
        var gridGO = new GameObject("GridContainer");
        gridGO.transform.SetParent(root, false);
        _gridContainer = gridGO.AddComponent<RectTransform>();
        _gridContainer.anchorMin        = new Vector2(0.5f, 0.5f);
        _gridContainer.anchorMax        = new Vector2(0.5f, 0.5f);
        _gridContainer.pivot            = new Vector2(0.5f, 0.5f);
        _gridContainer.anchoredPosition = new Vector2(0f, 30f);
        _gridContainer.sizeDelta        = new Vector2(600f, 400f);

        // ── Barra inferior ──
        var botBar = MakePanel(root, "BotBar",
            new Color(0.10f, 0.11f, 0.22f),
            new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(0f, 120f), new Vector2(0f, 120f));
        var botRT = botBar.GetComponent<RectTransform>();

        MakeButton(botRT, "BtnRestart", "Reiniciar", C_BTN_GREY,
            new Vector2(0.08f, 0.12f), new Vector2(0.48f, 0.88f), Vector2.zero, Vector2.zero,
            () => { ResetTotals(); StartRound(0); });

        MakeButton(botRT, "BtnMenu", "Volver al menu", C_BTN_BLUE,
            new Vector2(0.52f, 0.12f), new Vector2(0.92f, 0.88f), Vector2.zero, Vector2.zero,
            ReturnToGameSelector);

        // ── Panel de transición entre rondas ──
        BuildTransitionPanel(root);

        // ── Panel de fin ──
        BuildEndPanel(root);
    }

    private void BuildRoundIndicator(RectTransform root)
    {
        // Franja contenedora
        var bar = MakePanel(root, "RoundBar",
            new Color(0.10f, 0.11f, 0.22f),
            new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0f, -148f), new Vector2(0f, 38f));
        var barRT = bar.GetComponent<RectTransform>();

        // Texto "Ronda X / 3"
        _roundLabel = MakeLabel(barRT, "RoundLbl", "Ronda 1 / 3",
            new Color(0.60f, 0.64f, 0.88f), 30f,
            new Vector2(0.05f, 0f), new Vector2(0.50f, 1f), Vector2.zero, Vector2.zero);
        _roundLabel.alignment = TMPro.TextAlignmentOptions.MidlineLeft;

        // Puntos de progreso
        _roundDots = new Image[TOTAL_ROUNDS];
        for (int i = 0; i < TOTAL_ROUNDS; i++)
        {
            var dot = new GameObject($"Dot{i}");
            dot.transform.SetParent(barRT, false);
            var dotRT = dot.AddComponent<RectTransform>();
            dotRT.anchorMin        = new Vector2(1f, 0.5f);
            dotRT.anchorMax        = new Vector2(1f, 0.5f);
            dotRT.pivot            = new Vector2(0.5f, 0.5f);
            dotRT.anchoredPosition = new Vector2(-60f - (TOTAL_ROUNDS - 1 - i) * 34f, 0f);
            dotRT.sizeDelta        = new Vector2(22f, 22f);
            _roundDots[i] = dot.AddComponent<Image>();
            _roundDots[i].color = C_DOT_OFF;
        }
    }

    private void BuildTransitionPanel(RectTransform root)
    {
        _transPanel = new GameObject("TransPanel");
        _transPanel.transform.SetParent(root, false);
        var rt = _transPanel.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;

        var overlay = _transPanel.AddComponent<Image>();
        overlay.color = new Color(0f, 0f, 0f, 0.82f);

        var card = MakePanel(rt, "Card", C_PANEL,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(700f, 340f));
        var cardRT = card.GetComponent<RectTransform>();

        MakePanel(cardRT, "AccentTop", C_GREEN,
            new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0f, -12f), new Vector2(0f, 24f));

        _transTitle = MakeLabel(cardRT, "TransTitle", "Ronda 1 completada!",
            Color.white, 58f,
            new Vector2(0.05f, 0.45f), new Vector2(0.95f, 1f), Vector2.zero, Vector2.zero);
        _transTitle.fontStyle = TMPro.FontStyles.Bold;
        _transTitle.alignment = TMPro.TextAlignmentOptions.Center;

        _transSubtitle = MakeLabel(cardRT, "TransSub", "Siguiente ronda en 3...",
            C_TEXT_DIM, 38f,
            new Vector2(0.05f, 0f), new Vector2(0.95f, 0.45f), Vector2.zero, Vector2.zero);
        _transSubtitle.alignment = TMPro.TextAlignmentOptions.Center;

        _transPanel.SetActive(false);
    }

    private void BuildEndPanel(RectTransform root)
    {
        _endPanel = new GameObject("EndPanel");
        _endPanel.transform.SetParent(root, false);
        var overlayRT = _endPanel.AddComponent<RectTransform>();
        overlayRT.anchorMin = Vector2.zero;
        overlayRT.anchorMax = Vector2.one;
        overlayRT.sizeDelta = Vector2.zero;

        var overlay = _endPanel.AddComponent<Image>();
        overlay.color = new Color(0f, 0f, 0f, 0.80f);

        var card = MakePanel(overlayRT, "Card", C_PANEL,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(760f, 640f));
        var cardRT = card.GetComponent<RectTransform>();

        _endAccentBar = MakePanel(cardRT, "AccentBar", C_GREEN,
            new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0f, -14f), new Vector2(0f, 28f)).GetComponent<Image>();

        _endTitle = MakeLabel(cardRT, "EndTitle", "Completado",
            Color.white, 64f,
            new Vector2(0.05f, 1f), new Vector2(0.95f, 1f),
            new Vector2(0f, -90f), new Vector2(0f, -28f));
        _endTitle.fontStyle = TMPro.FontStyles.Bold;
        _endTitle.alignment = TMPro.TextAlignmentOptions.Center;

        MakePanel(cardRT, "Divider", new Color(1f, 1f, 1f, 0.08f),
            new Vector2(0.08f, 1f), new Vector2(0.92f, 1f),
            new Vector2(0f, -130f), new Vector2(0f, 2f));

        float rowTop = -148f;
        float rowH   = 74f;
        _statRounds = BuildStatRow(cardRT, "Rondas completadas", "0 / 3", rowTop);
        _statErrors = BuildStatRow(cardRT, "Errores totales",    "0",    rowTop - rowH);
        _statTime   = BuildStatRow(cardRT, "Tiempo total",       "0:00", rowTop - rowH * 2f);
        _statScore  = BuildStatRow(cardRT, "Puntuacion",         "0",    rowTop - rowH * 3f);

        MakeButton(cardRT, "BtnReplay", "Jugar de nuevo", C_BTN_BLUE,
            new Vector2(0.06f, 0f), new Vector2(0.94f, 0f),
            new Vector2(0f, 90f), new Vector2(0f, 72f),
            () => { _endPanel.SetActive(false); ResetTotals(); StartRound(0); });

        MakeButton(cardRT, "BtnMenu", "Menu principal", C_BTN_GREY,
            new Vector2(0.06f, 0f), new Vector2(0.94f, 0f),
            new Vector2(0f, 14f), new Vector2(0f, 66f),
            ReturnToGameSelector);

        _endPanel.SetActive(false);
    }

    // ─── Panel de fin ─────────────────────────────────────────────────────

    private void ShowEndPanel(bool won)
    {
        _endPanel.SetActive(true);
        _endAccentBar.color = won ? C_GREEN : C_RED;
        _endTitle.text      = won ? "Completado!" : "Tiempo agotado";

        int roundsDone = won ? TOTAL_ROUNDS : _currentRound;
        _statRounds.text = $"{roundsDone} / {TOTAL_ROUNDS}";
        _statErrors.text = $"{_totalErrors}";
        _statTime.text   = FormatTime(_totalTime);
        _statScore.text  = $"{_totalScore}";
    }

    // ─── UI helpers ───────────────────────────────────────────────────────

    private void UpdateRoundIndicator(int roundIndex)
    {
        _roundLabel.text = $"Ronda {roundIndex + 1} / {TOTAL_ROUNDS}";
        for (int i = 0; i < _roundDots.Length; i++)
            _roundDots[i].color = i <= roundIndex ? C_ACCENT : C_DOT_OFF;
    }

    private void UpdateTimerUI()
    {
        _timerLabel.text  = FormatTime(_timeRemaining);
        _timerLabel.color = _timeRemaining < 5f ? C_RED : C_ACCENT;
    }

    private TMPro.TextMeshProUGUI BuildStatRow(RectTransform parent,
                                               string labelText, string valueText,
                                               float anchoredY)
    {
        var rowGO = new GameObject($"Row_{labelText}");
        rowGO.transform.SetParent(parent, false);
        var rowRT = rowGO.AddComponent<RectTransform>();
        rowRT.anchorMin        = new Vector2(0.08f, 1f);
        rowRT.anchorMax        = new Vector2(0.92f, 1f);
        rowRT.pivot            = new Vector2(0.5f, 1f);
        rowRT.anchoredPosition = new Vector2(0f, anchoredY);
        rowRT.sizeDelta        = new Vector2(0f, 64f);

        var lblGO = new GameObject("Lbl");
        lblGO.transform.SetParent(rowGO.transform, false);
        var lblRT = lblGO.AddComponent<RectTransform>();
        lblRT.anchorMin = new Vector2(0f, 0f); lblRT.anchorMax = new Vector2(0.60f, 1f);
        lblRT.sizeDelta = Vector2.zero;
        var lblTmp = lblGO.AddComponent<TMPro.TextMeshProUGUI>();
        lblTmp.text = labelText; lblTmp.color = C_TEXT_DIM;
        lblTmp.fontSize = 33f; lblTmp.alignment = TMPro.TextAlignmentOptions.MidlineLeft;

        var valGO = new GameObject("Val");
        valGO.transform.SetParent(rowGO.transform, false);
        var valRT = valGO.AddComponent<RectTransform>();
        valRT.anchorMin = new Vector2(0.60f, 0f); valRT.anchorMax = new Vector2(1f, 1f);
        valRT.sizeDelta = Vector2.zero;
        var valTmp = valGO.AddComponent<TMPro.TextMeshProUGUI>();
        valTmp.text = valueText; valTmp.color = Color.white;
        valTmp.fontSize = 38f; valTmp.fontStyle = TMPro.FontStyles.Bold;
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
        ApplyRT(txtGO.AddComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var tmp = txtGO.AddComponent<TMPro.TextMeshProUGUI>();
        tmp.text = label; tmp.color = Color.white;
        tmp.fontSize = 38f; tmp.fontStyle = TMPro.FontStyles.Bold;
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
    }

    // ─── Utilidades ───────────────────────────────────────────────────────

    private static string FormatTime(float seconds)
    {
        int total = Mathf.Max(0, Mathf.CeilToInt(seconds));
        return $"{total / 60}:{(total % 60):D2}";
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
