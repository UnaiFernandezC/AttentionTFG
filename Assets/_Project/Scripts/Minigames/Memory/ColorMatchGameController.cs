// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ColorMatchGameController : MinigameBase
{

    private static readonly Color C_BG_DARK   = new Color(0.07f, 0.08f, 0.18f);
    private static readonly Color C_BG_MID    = new Color(0.10f, 0.07f, 0.22f);
    private static readonly Color C_PANEL     = new Color(0.11f, 0.12f, 0.26f);
    private static readonly Color C_ACCENT    = new Color(0.48f, 0.76f, 1.00f);
    private static readonly Color C_GREEN     = new Color(0.28f, 0.86f, 0.60f);
    private static readonly Color C_WHITE     = Color.white;
    private static readonly Color C_SEPARATOR = new Color(1f, 1f, 1f, 0.08f);

    // ------------------------------------------------ dificultad (runtime)
    private int   _pairs        = 6;
    private bool  _previewCards = true;
    private float _previewTime  = 2.5f;
    private float _timeLimit    = 0f;      // 0 = sin límite

    private int   _totalAttempts = 0;
    private int   _matchedPairs  = 0;
    private float _elapsed       = 0f;
    private bool  _gameOver      = false;
    private bool  _boardActive   = false;

    private BoardManager     _boardManager;
    private RectTransform    _boardContainer;
    private TextMeshProUGUI  _attemptsLabel;
    private TextMeshProUGUI  _timerLabel;
    private TextMeshProUGUI  _hintLabel;

    protected override string GetIntroDescription() =>
        "Da la vuelta a las cartas de dos en dos.\n" +
        "¡Encuentra todas las parejas del mismo color!";

    private void ApplyDifficulty()
    {
        var diff = GameManager.Instance != null
            ? GameManager.Instance.CurrentDifficulty
            : DifficultyLevel.Easy;

        switch (diff)
        {
            case DifficultyLevel.Medium:
                _pairs        = 8;
                _previewCards = false;
                _timeLimit    = 0f;
                break;
            case DifficultyLevel.Hard:
                _pairs        = 12;
                _previewCards = false;
                _timeLimit    = 120f;
                break;
            default:
                _pairs        = 6;
                _previewCards = true;
                _previewTime  = 2.5f;
                _timeLimit    = 0f;
                break;
        }
    }

    protected override void OnMinigameStart()
    {
        ApplyDifficulty();
        EnsureEventSystem();
        BuildUI();
        StartCoroutine(StartBoard());
    }

    protected override void OnMinigameComplete() { }
    protected override void OnMinigameFailed()   { }

    private void Update()
    {
        if (_gameOver || !_boardActive) return;
        _elapsed += Time.deltaTime;

        if (_timeLimit > 0f)
        {
            float left = Mathf.Max(0f, _timeLimit - _elapsed);
            if (_timerLabel)
            {
                _timerLabel.text  = $"{Mathf.CeilToInt(left)}s";
                _timerLabel.color = left < 20f ? new Color(1f, 0.35f, 0.35f) : C_ACCENT;
            }
            if (left <= 0f) EndGame(false);
        }
        else if (_timerLabel)
        {
            _timerLabel.text = FormatTime(_elapsed);
        }
    }

    private IEnumerator StartBoard()
    {
        if (_attemptsLabel) _attemptsLabel.text = "Intentos: 0";

        var go = new GameObject("BoardManager");
        go.transform.SetParent(transform, false);
        _boardManager = go.AddComponent<BoardManager>();

        float cardSize = _pairs <= 6 ? 130f : _pairs <= 8 ? 112f : 92f;
        float spacing  = _pairs <= 6 ? 14f  : _pairs <= 8 ? 11f  : 9f;

        _boardManager.Initialize(_boardContainer, _pairs, cardSize, spacing);
        _boardManager.OnAttemptMade  += OnAttempt;
        _boardManager.OnPairResolved += OnPairResolved;
        _boardManager.OnAllMatched   += OnAllMatched;

        if (_previewCards)
        {
            if (_hintLabel) _hintLabel.text = "¡Mira bien las cartas!";
            yield return _boardManager.PreviewAll(_previewTime);
        }

        if (_hintLabel) _hintLabel.text = "Encuentra las parejas";
        _boardActive = true;
    }

    private void OnAttempt(int total)
    {
        _totalAttempts = total;
        if (_attemptsLabel) _attemptsLabel.text = $"Intentos: {total}";
    }

    private void OnPairResolved(bool match, float rtMs)
    {
        ReportEvent(match, rtMs);
        if (match)
        {
            _matchedPairs++;
            GameFeel.FloatingText("+100", C_GREEN,
                new Vector2(0f, -80f), 44f);
        }
    }

    private void OnAllMatched()
    {
        EndGame(true);
    }

    private void EndGame(bool won)
    {
        if (_gameOver) return;
        _gameOver    = true;
        _boardActive = false;

        int score = _matchedPairs * 100;
        if (won)
        {
            int bonus = Mathf.Max(0, (_pairs * 3 - _totalAttempts) * 10);
            score += bonus;
            CompleteMinigame(score);
            GameFeel.Confetti(60);
        }
        else
        {
            FailMinigame();
        }

        float ratio = _totalAttempts > 0 ? (float)_pairs / _totalAttempts : 0f;
        int   stars = GameFeel.StarsFromRatio(won, Mathf.Clamp01(ratio * 1.6f));

        ShowResults(won, stars, won ? score : 0,
            new string[]
            {
                "Parejas: " + _matchedPairs + "/" + _pairs,
                "Intentos: " + _totalAttempts,
                "Tiempo: " + FormatTime(_elapsed)
            },
            won ? "¡Todas las parejas!" : "¡Se acabó el tiempo!",
            won ? "Tienes muy buena memoria." : "Inténtalo de nuevo, ¡más rápido!");
    }

    private void BuildUI()
    {
        var cGO    = new GameObject("Canvas"); cGO.transform.SetParent(transform, false);
        var canvas = cGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = cGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight  = 0.5f;
        cGO.AddComponent<GraphicRaycaster>();

        var root = MakePanel(cGO.transform, "Root", C_BG_DARK, Stretch());

        var grad = MakePanel(root.transform, "Grad", C_BG_MID, AnchorRect(0, 0, 1, 0.45f));
        grad.color = new Color(C_BG_MID.r, C_BG_MID.g, C_BG_MID.b, 0.60f);

        BuildHeader(root.transform);

        var boardZone = new GameObject("BoardZone");
        boardZone.transform.SetParent(root.transform, false);
        var bzRT = boardZone.AddComponent<RectTransform>();
        bzRT.anchorMin = new Vector2(0f, 0.10f);
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
    }

    private void BuildHeader(Transform parent)
    {
        var hdr = MakePanel(parent, "Header", C_PANEL, AnchorRect(0, 1, 1, 1, 0, -190f, 0, 0));

        MakePanel(hdr.transform, "AccentLine", C_ACCENT, AnchorRect(0, 1, 1, 1, 0, -3f, 0, 0));

        var title = MakeLabel(hdr.transform, "Title", "Parejas de Colores", C_WHITE, 42f, FontStyles.Bold);
        PlaceRT(title.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -50f), new Vector2(800f, 56f));

        _hintLabel = MakeLabel(hdr.transform, "Hint", "", C_GREEN, 26f, FontStyles.Bold);
        PlaceRT(_hintLabel.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -108f), new Vector2(600f, 32f));

        MakePanel(hdr.transform, "Sep", C_SEPARATOR, AnchorRect(0.05f, 0, 0.95f, 0, 0, 1f, 0, 2f));

        _attemptsLabel = MakeLabel(hdr.transform, "Attempts", "Intentos: 0", C_ACCENT, 26f, FontStyles.Normal);
        PlaceRT(_attemptsLabel.gameObject, new Vector2(0.28f, 1f), new Vector2(0.28f, 1f),
                new Vector2(0f, -160f), new Vector2(320f, 32f));

        _timerLabel = MakeLabel(hdr.transform, "Timer", "00:00", C_ACCENT, 26f, FontStyles.Normal);
        PlaceRT(_timerLabel.gameObject, new Vector2(0.72f, 1f), new Vector2(0.72f, 1f),
                new Vector2(0f, -160f), new Vector2(200f, 32f));
    }

    private Image MakePanel(Transform parent, string name, Color color, RectTransformCfg cfg)
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false);
        cfg.Apply(go.AddComponent<RectTransform>());
        var img = go.AddComponent<Image>(); img.color = color;
        return img;
    }

    private TextMeshProUGUI MakeLabel(Transform parent, string name, string text,
                                      Color color, float size, FontStyles style)
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = text; t.color = color; t.fontSize = size;
        t.fontStyle = style; t.alignment = TextAlignmentOptions.Center;
        return t;
    }

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
