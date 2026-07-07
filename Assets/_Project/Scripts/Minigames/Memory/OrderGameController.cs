// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Orden correcto — "Ordena la mision" (categoria Planificacion).
/// Easy:   1-6 ascendente (rondas de 4, 5 y 6 numeros).
/// Medium: 1-10 ascendente con algunos numeros ya colocados.
/// Hard:   descendente 20→11 o de 2 en 2 (variante aleatoria por ronda).
/// </summary>
public class OrderGameController : MinigameBase
{

    [Header("Tiempos por ronda (fallback, ApplyDifficulty manda)")]
    public int   numbersRound1   = 4;
    public float timeLimitRound1 = 20f;
    public int   numbersRound2   = 5;
    public float timeLimitRound2 = 25f;
    public int   numbersRound3   = 6;
    public float timeLimitRound3 = 30f;

    private class RoundCfg
    {
        public int[]  values;
        public bool   descending;
        public int    prePlaced;
        public float  timeLimit;
        public string instruction;
        public bool   showNext;
    }

    private const int TOTAL_ROUNDS = 3;

    private RoundCfg[]   _rounds;
    private OrderManager _orderManager;

    private int   _currentRound  = 0;
    private int   _totalErrors   = 0;
    private int   _totalCorrect  = 0;
    private float _totalTime     = 0f;
    private int   _totalScore    = 0;

    private bool  _roundRunning  = false;
    private float _timeRemaining = 0f;
    private float _roundElapsed  = 0f;
    private int   _roundErrors   = 0;
    private float _lastPressTime = 0f;

    private Canvas                _canvas;
    private TMPro.TextMeshProUGUI _errorsLabel;
    private TMPro.TextMeshProUGUI _timerLabel;
    private TMPro.TextMeshProUGUI _nextLabel;
    private TMPro.TextMeshProUGUI _instrLabel;
    private GameObject            _nextPanel;
    private RectTransform         _gridContainer;

    private TMPro.TextMeshProUGUI _roundLabel;
    private Image[]               _roundDots;

    private GameObject            _transPanel;
    private int                   _transNextRound;
    private TMPro.TextMeshProUGUI _transTitle;
    private TMPro.TextMeshProUGUI _transSubtitle;

    private static readonly Color C_BG_DARK  = new Color(0.08f, 0.09f, 0.18f);
    private static readonly Color C_PANEL    = new Color(0.13f, 0.14f, 0.26f);
    private static readonly Color C_ACCENT   = new Color(0.25f, 0.55f, 1.00f);
    private static readonly Color C_GREEN    = new Color(0.20f, 0.78f, 0.48f);
    private static readonly Color C_RED      = new Color(0.85f, 0.25f, 0.32f);
    private static readonly Color C_TEXT_DIM = new Color(0.65f, 0.68f, 0.80f);
    private static readonly Color C_DOT_OFF  = new Color(0.25f, 0.27f, 0.45f);

    protected override string GetIntroDescription() =>
        "Los numeros de la mision estan revueltos.\n" +
        "Pulsalos en el orden que se pide antes de que acabe el tiempo.";

    protected override void OnMinigameStart()
    {
        EnsureEventSystem();
        ApplyDifficulty();
        BuildUI();
        ResetTotals();
        StartRound(0);
    }

    protected override void OnMinigameComplete() { }
    protected override void OnMinigameFailed()   { }

    private void ApplyDifficulty()
    {
        var diff = GameManager.Instance != null
            ? GameManager.Instance.CurrentDifficulty
            : DifficultyLevel.Easy;

        _rounds = new RoundCfg[TOTAL_ROUNDS];

        switch (diff)
        {
            case DifficultyLevel.Medium:
            {
                int[]   counts = { 8, 9, 10 };
                int[]   placed = { 2, 2, 3 };
                float[] times  = { 30f, 35f, 40f };
                for (int r = 0; r < TOTAL_ROUNDS; r++)
                {
                    _rounds[r] = new RoundCfg
                    {
                        values      = Range(1, counts[r], 1),
                        descending  = false,
                        prePlaced   = placed[r],
                        timeLimit   = times[r],
                        instruction = "De MENOR a MAYOR. Los verdes ya estan colocados",
                        showNext    = true
                    };
                }
                break;
            }

            case DifficultyLevel.Hard:
            {
                float[] times = { 40f, 40f, 45f };
                for (int r = 0; r < TOTAL_ROUNDS; r++)
                {
                    bool countdown = Random.value < 0.5f;
                    _rounds[r] = countdown
                        ? new RoundCfg
                        {
                            values      = Range(11, 10, 1),
                            descending  = true,
                            prePlaced   = 0,
                            timeLimit   = times[r],
                            instruction = "¡Al reves! De MAYOR a MENOR: 20, 19, 18...",
                            showNext    = false
                        }
                        : new RoundCfg
                        {
                            values      = Range(2, 10, 2),
                            descending  = false,
                            prePlaced   = 0,
                            timeLimit   = times[r],
                            instruction = "¡De 2 en 2! 2, 4, 6, 8...",
                            showNext    = false
                        };
                }
                break;
            }

            default: // Easy
            {
                int[]   counts = { 4, 5, 6 };
                float[] times  = { timeLimitRound1, timeLimitRound2, timeLimitRound3 };
                for (int r = 0; r < TOTAL_ROUNDS; r++)
                {
                    _rounds[r] = new RoundCfg
                    {
                        values      = Range(1, counts[r], 1),
                        descending  = false,
                        prePlaced   = 0,
                        timeLimit   = times[r] > 1f ? times[r] : 20f + r * 5f,
                        instruction = "Pulsa los numeros de MENOR a MAYOR",
                        showNext    = true
                    };
                }
                break;
            }
        }
    }

    private static int[] Range(int start, int count, int step)
    {
        var arr = new int[count];
        for (int i = 0; i < count; i++) arr[i] = start + i * step;
        return arr;
    }

    private void ResetTotals()
    {
        _currentRound = 0;
        _totalErrors  = 0;
        _totalCorrect = 0;
        _totalTime    = 0f;
        _totalScore   = 0;
    }

    private void StartRound(int roundIndex)
    {
        _currentRound  = roundIndex;
        _roundErrors   = 0;
        _roundElapsed  = 0f;
        _roundRunning  = true;
        _lastPressTime = Time.realtimeSinceStartup;

        RoundCfg cfg   = _rounds[roundIndex];
        _timeRemaining = cfg.timeLimit;

        UpdateRoundIndicator(roundIndex);

        if (_instrLabel != null) _instrLabel.text = cfg.instruction;

        _errorsLabel.text = "Errores: 0";
        UpdateTimerUI();

        _transPanel.SetActive(false);

        for (int i = _gridContainer.childCount - 1; i >= 0; i--)
            DestroyImmediate(_gridContainer.GetChild(i).gameObject);

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

        float btnSize = cfg.values.Length >= 8 ? 118f : 130f;
        _orderManager.Initialize(_gridContainer, cfg.values, cfg.descending,
                                 cfg.prePlaced, btnSize);

        _nextPanel.SetActive(cfg.showNext);
        if (cfg.showNext && _nextLabel != null)
            _nextLabel.text = $"Siguiente: {_orderManager.NextExpectedValue}";
    }

    private void HandleCorrect(int nextExpectedValue)
    {
        if (!IsPlaying) return;
        float rtMs = (Time.realtimeSinceStartup - _lastPressTime) * 1000f;
        _lastPressTime = Time.realtimeSinceStartup;

        _totalCorrect++;
        ReportEvent(true, rtMs);
        GameFeel.PlayPop();

        if (_nextPanel.activeSelf && _nextLabel != null)
            _nextLabel.text = nextExpectedValue > 0
                ? $"Siguiente: {nextExpectedValue}"
                : "¡Ultimo!";
    }

    private void HandleWrong(int totalWrong)
    {
        if (!IsPlaying) return;
        float rtMs = (Time.realtimeSinceStartup - _lastPressTime) * 1000f;
        _lastPressTime = Time.realtimeSinceStartup;

        _roundErrors++;
        _totalErrors++;
        ReportEvent(false, rtMs);
        GameFeel.PlayError();
        _errorsLabel.text = $"Errores: {_totalErrors}";
    }

    private void HandleRoundComplete()
    {
        if (!IsPlaying) return;
        _roundRunning = false;
        _totalTime   += _roundElapsed;

        RoundCfg cfg   = _rounds[_currentRound];
        int pressable  = cfg.values.Length - cfg.prePlaced;
        int roundScore = Mathf.Max(0, pressable * 100 - _roundErrors * 15 +
                                   Mathf.RoundToInt(_timeRemaining * 2f));
        _totalScore += roundScore;

        GameFeel.PlaySuccess();
        GameFeel.Confetti(20);

        bool isLastRound = (_currentRound >= TOTAL_ROUNDS - 1);

        if (isLastRound)
            FinishGame(won: true);
        else
            StartCoroutine(RoundTransition(_currentRound));
    }

    private IEnumerator RoundTransition(int completedRound)
    {
        yield return new WaitForSeconds(0.5f);
        _transPanel.SetActive(true);
        _transPanel.transform.SetAsLastSibling();
        _transTitle.text    = $"¡Ronda {completedRound + 1} completada!";
        _transSubtitle.text = "Pulsa Continuar para la siguiente ronda";
        _transNextRound     = completedRound + 1;
    }

    private void OnTransContinue()
    {
        _transPanel.SetActive(false);
        StartRound(_transNextRound);
    }

    private void FinishGame(bool won)
    {
        int total  = _totalCorrect + _totalErrors;
        float ratio = total > 0 ? (float)_totalCorrect / total : 0f;
        int roundsDone = won ? TOTAL_ROUNDS : _currentRound;

        var stats = new[]
        {
            $"Rondas: {roundsDone} / {TOTAL_ROUNDS}",
            $"Errores: {_totalErrors}",
            $"Tiempo: {FormatTime(_totalTime)}"
        };

        if (won)
        {
            CompleteMinigame(_totalScore);
            ShowResults(true, GameFeel.StarsFromRatio(true, ratio), _totalScore, stats,
                _totalErrors == 0 ? "¡Orden perfecto!" : null,
                _totalErrors == 0 ? "Ni un solo fallo" : null);
        }
        else
        {
            GameFeel.PlayError();
            GameFeel.ScreenFlash(C_RED, 0.18f, 0.3f);
            FailMinigame();
            ShowResults(false, 0, 0, stats,
                "¡Tiempo agotado!",
                "Piensa el orden y ve numero a numero");
        }
    }

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
            FinishGame(won: false);
        }
    }

    private void BuildUI()
    {

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

        MakePanel(root, "BG", C_BG_DARK, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        var headerBar = MakePanel(root, "HeaderBar",
            new Color(0.10f, 0.11f, 0.22f),
            new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0f, -110f), new Vector2(0f, 110f));
        var headerRT = headerBar.GetComponent<RectTransform>();

        MakePanel(headerRT, "Accent", C_ACCENT,
            new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(0f, 3f), new Vector2(0f, 3f));

        var titleLbl = MakeLabel(headerRT, "Title", "Ordena la mision",
            Color.white, 50f,
            new Vector2(0.12f, 0f), new Vector2(0.88f, 1f), Vector2.zero, Vector2.zero);
        titleLbl.fontStyle = TMPro.FontStyles.Bold;
        titleLbl.alignment = TMPro.TextAlignmentOptions.Center;

        _errorsLabel = MakeLabel(headerRT, "Errors", "Errores: 0",
            C_TEXT_DIM, 34f,
            new Vector2(0f, 0f), new Vector2(0.35f, 1f),
            new Vector2(24f, 0f), new Vector2(0f, 0f));
        _errorsLabel.alignment = TMPro.TextAlignmentOptions.MidlineLeft;

        _timerLabel = MakeLabel(headerRT, "Timer", "0:20",
            C_ACCENT, 44f,
            new Vector2(0.65f, 0f), new Vector2(1f, 1f),
            new Vector2(0f, 0f), new Vector2(-24f, 0f));
        _timerLabel.fontStyle = TMPro.FontStyles.Bold;
        _timerLabel.alignment = TMPro.TextAlignmentOptions.MidlineRight;

        BuildRoundIndicator(root);

        var instrBar = MakePanel(root, "InstrBar",
            new Color(0.12f, 0.14f, 0.28f),
            new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0f, -200f), new Vector2(0f, 48f));
        _instrLabel = MakeLabel(instrBar.GetComponent<RectTransform>(), "Instr",
            "Pulsa los numeros de MENOR a MAYOR",
            new Color(0.72f, 0.76f, 0.92f), 31f,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        _instrLabel.alignment = TMPro.TextAlignmentOptions.Center;

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

        var gridGO = new GameObject("GridContainer");
        gridGO.transform.SetParent(root, false);
        _gridContainer = gridGO.AddComponent<RectTransform>();
        _gridContainer.anchorMin        = new Vector2(0.5f, 0.5f);
        _gridContainer.anchorMax        = new Vector2(0.5f, 0.5f);
        _gridContainer.pivot            = new Vector2(0.5f, 0.5f);
        _gridContainer.anchoredPosition = new Vector2(0f, 30f);
        _gridContainer.sizeDelta        = new Vector2(600f, 400f);

        BuildTransitionPanel(root);
    }

    private void BuildRoundIndicator(RectTransform root)
    {

        var bar = MakePanel(root, "RoundBar",
            new Color(0.10f, 0.11f, 0.22f),
            new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0f, -148f), new Vector2(0f, 38f));
        var barRT = bar.GetComponent<RectTransform>();

        _roundLabel = MakeLabel(barRT, "RoundLbl", "Ronda 1 / 3",
            new Color(0.60f, 0.64f, 0.88f), 30f,
            new Vector2(0.05f, 0f), new Vector2(0.50f, 1f), Vector2.zero, Vector2.zero);
        _roundLabel.alignment = TMPro.TextAlignmentOptions.MidlineLeft;

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

        _transSubtitle = MakeLabel(cardRT, "TransSub", "",
            C_TEXT_DIM, 34f,
            new Vector2(0.05f, 0.30f), new Vector2(0.95f, 0.55f), Vector2.zero, Vector2.zero);
        _transSubtitle.alignment = TMPro.TextAlignmentOptions.Center;

        MakeButton(cardRT, "BtnContinue", "Continuar", C_GREEN,
            new Vector2(0.30f, 0f), new Vector2(0.70f, 0f),
            new Vector2(0f, 56f), new Vector2(0f, 72f),
            () => OnTransContinue());

        _transPanel.SetActive(false);
    }

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
        ButtonJuice.Attach(go);

        var txtGO = new GameObject("Text");
        txtGO.transform.SetParent(go.transform, false);
        ApplyRT(txtGO.AddComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var tmp = txtGO.AddComponent<TMPro.TextMeshProUGUI>();
        tmp.text = label; tmp.color = Color.white;
        tmp.fontSize = 38f; tmp.fontStyle = TMPro.FontStyles.Bold;
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
    }

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
