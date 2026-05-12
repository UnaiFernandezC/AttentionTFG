using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Construye y gestiona toda la UI de "No sigas la mayoría".
/// Estética idéntica al resto de minijuegos del proyecto.
///
/// Layout:
///   HEADER   — título, categoría, puntos de ronda
///   HINT     — "¿Qué dirección tiene MENOS flechas?"
///   STIMULUS — contenedor donde StimulusGenerator coloca las flechas
///   TIMER    — barra que se vacía con el tiempo
///   FEEDBACK — texto verde/rojo de resultado
///   D-PAD    — 4 botones de dirección en cruz
///   FOOTER   — consejo + botón menú
///   FINAL    — overlay de resultado final
/// </summary>
public class DontFollowMajorityUIController : MonoBehaviour
{
    // ── Helpers ───────────────────────────────────────────────────────────
    static Vector2 V(float x, float y) => new Vector2(x, y);
    static Color   C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

    // ── Paleta ────────────────────────────────────────────────────────────
    static readonly Color BG     = C(0.05f, 0.08f, 0.14f);
    static readonly Color HDR    = C(0.03f, 0.05f, 0.10f);
    static readonly Color PANEL  = C(0.07f, 0.11f, 0.20f);
    static readonly Color ACCENT = C(0.18f, 0.80f, 0.58f);
    static readonly Color DIM    = C(0.40f, 0.55f, 0.65f);
    static readonly Color CRED   = C(0.90f, 0.22f, 0.28f);
    static readonly Color CGREEN = C(0.22f, 0.86f, 0.54f);

    // ── Refs ──────────────────────────────────────────────────────────────
    Image[]         _roundDots;
    TextMeshProUGUI _scoreText;
    TextMeshProUGUI _feedbackText;
    RectTransform   _timerFill;   // anchorMax.x se mueve de 0.90→0.10
    Image           _timerFillImg;
    Image           _flashOverlay;

    GameObject      _finalPanel;
    TextMeshProUGUI _finalTitle;
    TextMeshProUGUI _finalSub;

    Coroutine _feedbackRoutine;

    const float TIMER_LEFT  = 0.10f;
    const float TIMER_RIGHT = 0.90f;

    // ── Propiedad pública ─────────────────────────────────────────────────
    /// <summary>Contenedor donde StimulusGenerator crea las flechas.</summary>
    public RectTransform StimulusContainer { get; private set; }

    // ═══════════════════════════════════════════════════════════════════════
    // BuildUI
    // ═══════════════════════════════════════════════════════════════════════
    public void BuildUI(int rounds,
                        Action<DFMDirection> onDirection,
                        Action onRestart,
                        Action onMenu)
    {
        // ── Canvas ────────────────────────────────────────────────────────
        var cGO = new GameObject("Canvas_DFM");
        cGO.transform.SetParent(transform, false);
        var cv = cGO.AddComponent<Canvas>();
        cv.renderMode   = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 5;
        var sc = cGO.AddComponent<CanvasScaler>();
        sc.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = V(1920f, 1080f);
        sc.matchWidthOrHeight  = 0.5f;
        cGO.AddComponent<GraphicRaycaster>();
        var R = cGO.GetComponent<RectTransform>();

        // ── Fondo + cuadrícula ────────────────────────────────────────────
        MkImg(R, "BG",   BG,                           V(0,0), V(1,1), V(0,0), V(0,0));
        MkImg(R, "Grad", C(0f,0.08f,0.18f,0.28f),     V(0,0), V(1,1), V(0,0), V(0,0));
        BuildGrid(R);

        // ── Header ────────────────────────────────────────────────────────
        var hdr = MkImg(R, "Hdr", HDR, V(0,1), V(1,1), V(0,-44f), V(0,88f));
        MkImg(hdr, "LineB", ACCENT, V(0,0),     V(1,0),     V(0,1.5f), V(0,3f));
        MkImg(hdr, "AccL",  ACCENT, V(0,0.18f), V(0,0.82f), V(3f,0),   V(6f,0));
        var ttl = MkTxt(hdr, "Title", "NO SIGAS LA MAYORÍA", Color.white, 24,
                        V(0.03f,0.12f), V(0.55f,0.88f));
        ttl.fontStyle = FontStyles.Bold;
        ttl.alignment = TextAlignmentOptions.MidlineLeft;
        ttl.characterSpacing = 1.5f;
        MkTxt(hdr, "Cat", "CONTROL DE IMPULSOS", DIM, 14,
              V(0.55f,0.12f), V(0.73f,0.88f)).alignment = TextAlignmentOptions.MidlineRight;
        _roundDots = BuildRoundDots(hdr, rounds);

        // ── Score ─────────────────────────────────────────────────────────
        _scoreText = MkTxt(R, "Score", "0 pts", DIM, 19,
                           V(0.01f,0.885f), V(0.16f,0.935f));
        _scoreText.alignment = TextAlignmentOptions.MidlineLeft;

        // ── Hint ──────────────────────────────────────────────────────────
        var hint = MkTxt(R, "Hint",
                         "¿Qué dirección tiene  MENOS  flechas?",
                         C(0.65f,0.80f,0.95f), 22,
                         V(0.10f,0.865f), V(0.90f,0.907f));
        hint.alignment = TextAlignmentOptions.Center;
        hint.fontStyle = FontStyles.Italic;

        // ── Contenedor de estímulos ────────────────────────────────────────
        var scGO = new GameObject("StimulusContainer");
        scGO.transform.SetParent(R, false);
        StimulusContainer = scGO.AddComponent<RectTransform>();
        StimulusContainer.anchorMin = V(0.08f, 0.245f);
        StimulusContainer.anchorMax = V(0.92f, 0.862f);
        StimulusContainer.offsetMin = StimulusContainer.offsetMax = Vector2.zero;

        // ── Timer bar ─────────────────────────────────────────────────────
        MkImg(R, "TimerBg", C(0.06f,0.10f,0.20f,0.80f),
              V(TIMER_LEFT,0.220f), V(TIMER_RIGHT,0.240f), V(0,0), V(0,0));
        var fillRT = MkImg(R, "TimerFill", ACCENT,
                           V(TIMER_LEFT,0.220f), V(TIMER_RIGHT,0.240f), V(0,0), V(0,0));
        _timerFill    = fillRT;
        _timerFillImg = fillRT.GetComponent<Image>();

        // ── Feedback ──────────────────────────────────────────────────────
        _feedbackText = MkTxt(R, "Feedback", "", Color.white, 32,
                              V(0.10f,0.165f), V(0.90f,0.218f));
        _feedbackText.fontStyle = FontStyles.Bold;
        _feedbackText.alignment = TextAlignmentOptions.Center;

        // ── D-pad (4 botones de dirección) ────────────────────────────────
        BuildDPad(R, onDirection);

        // ── Flash overlay ─────────────────────────────────────────────────
        var fGO = new GameObject("Flash");
        fGO.transform.SetParent(R, false);
        var fRT = fGO.AddComponent<RectTransform>();
        fRT.anchorMin = V(0,0); fRT.anchorMax = V(1,1);
        fRT.sizeDelta = fRT.anchoredPosition = Vector2.zero;
        _flashOverlay = fGO.AddComponent<Image>();
        _flashOverlay.color = C(0,0,0,0);
        _flashOverlay.raycastTarget = false;
        fGO.SetActive(false);

        // ── Footer ────────────────────────────────────────────────────────
        var footer = MkImg(R, "Footer", HDR, V(0,0), V(1,0), V(0,40f), V(0,80f));
        MkImg(footer, "LineT", ACCENT, V(0,1), V(1,1), V(0,-1.5f), V(0,3f));
        MkTxt(footer, "Tip",
              "Ignora el impulso de seguir a la mayoría  ·  Sé crítico",
              C(ACCENT.r,ACCENT.g-0.08f,ACCENT.b-0.05f), 15,
              V(0.01f,0), V(0.78f,1)).alignment = TextAlignmentOptions.MidlineLeft;
        MkImg(footer, "Sep", C(1,1,1,0.10f), V(0.78f,0.1f), V(0.782f,0.9f), V(0,0), V(0,0));
        MkBtn(footer, "Menú", C(0.14f,0.22f,0.38f), V(0.80f,0.08f), V(0.99f,0.92f), onMenu);

        // ── Panel resultado final ──────────────────────────────────────────
        BuildFinalPanel(R, onRestart, onMenu);

        // Estado inicial
        SetTimerBar(1f);
    }

    // ───────────────────────────────────────────────────────────────────────
    void BuildGrid(RectTransform R)
    {
        for (int i = 1; i < 6; i++)
        {
            float t = i / 6f;
            MkImg(R, "GH"+i, C(1,1,1,0.018f), V(0,t-0.001f),  V(1,t+0.001f),  V(0,0), V(0,0));
            MkImg(R, "GV"+i, C(1,1,1,0.018f), V(t-0.0006f,0), V(t+0.0006f,1), V(0,0), V(0,0));
        }
    }

    Image[] BuildRoundDots(RectTransform hdr, int rounds)
    {
        var dots    = new Image[rounds];
        float start = 0.75f, gap = Mathf.Min(0.030f, 0.22f / Mathf.Max(rounds,1));
        for (int i = 0; i < rounds; i++)
        {
            var go  = new GameObject("Dot_"+i);
            go.transform.SetParent(hdr, false);
            var rt  = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = V(start + i * gap, 0.5f);
            rt.pivot     = V(0.5f,0.5f);
            rt.sizeDelta = V(18f,18f);
            rt.anchoredPosition = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.sprite = MakeCircleSprite(32);
            img.color  = C(0.25f,0.30f,0.40f);
            dots[i]    = img;
        }
        return dots;
    }

    // ── D-pad en cruz ──────────────────────────────────────────────────────
    void BuildDPad(RectTransform R, Action<DFMDirection> onDir)
    {
        // Contenedor centrado, zona inferior entre feedback y footer
        var dpGO = new GameObject("DPad");
        dpGO.transform.SetParent(R, false);
        var dpRT = dpGO.AddComponent<RectTransform>();
        dpRT.anchorMin = V(0.34f, 0.075f);
        dpRT.anchorMax = V(0.66f, 0.162f);
        dpRT.offsetMin = dpRT.offsetMax = Vector2.zero;

        // Fondo sutil
        var dpBg = dpGO.AddComponent<Image>();
        dpBg.color = C(0,0,0,0);

        // UP
        MkDirButton(dpRT, "UP",    "↑", DFMDirection.Up,
                    V(0.38f,0.52f), V(0.62f,0.98f), onDir);
        // DOWN
        MkDirButton(dpRT, "DOWN",  "↓", DFMDirection.Down,
                    V(0.38f,0.02f), V(0.62f,0.48f), onDir);
        // LEFT
        MkDirButton(dpRT, "LEFT",  "←", DFMDirection.Left,
                    V(0.02f,0.22f), V(0.36f,0.78f), onDir);
        // RIGHT
        MkDirButton(dpRT, "RIGHT", "→", DFMDirection.Right,
                    V(0.64f,0.22f), V(0.98f,0.78f), onDir);
    }

    void MkDirButton(RectTransform parent, string name, string symbol,
                     DFMDirection dir, Vector2 am, Vector2 aM,
                     Action<DFMDirection> onDir)
    {
        var rt = MkImg(parent, "DBtn_"+name, C(0.09f,0.14f,0.26f), am, aM, V(0,0), V(0,0));

        // Shine superior
        MkImg(rt, "Sh", C(1,1,1,0.08f), V(0,0.5f), V(1,1), V(0,0), V(0,0));
        // Línea accent inferior
        MkImg(rt, "Bord", C(ACCENT.r,ACCENT.g,ACCENT.b,0.35f),
              V(0,0), V(1,0), V(0,1.5f), V(0,3f));

        var btn = rt.gameObject.AddComponent<Button>();
        btn.targetGraphic = rt.GetComponent<Image>();
        var cb = btn.colors;
        cb.normalColor      = Color.white;
        cb.highlightedColor = C(1,1,1,0.80f);
        cb.pressedColor     = C(0.60f,0.60f,0.60f);
        btn.colors = cb;
        var captured = dir;
        btn.onClick.AddListener(() => onDir?.Invoke(captured));

        var t = MkTxt(rt, "T", symbol, Color.white, 36, V(0,0), V(1,1));
        t.fontStyle = FontStyles.Bold;
    }

    void BuildFinalPanel(RectTransform R, Action onRestart, Action onMenu)
    {
        _finalPanel = new GameObject("FinalPanel");
        _finalPanel.transform.SetParent(R, false);
        var er = _finalPanel.AddComponent<RectTransform>();
        er.anchorMin = V(0,0); er.anchorMax = V(1,1);
        er.sizeDelta = er.anchoredPosition = Vector2.zero;
        _finalPanel.AddComponent<Image>().color = C(0,0,0,0.88f);

        var card = MkImg(er, "Card", PANEL,
                         V(0.5f,0.5f), V(0.5f,0.5f), V(0,0), V(900f,490f));
        MkImg(card, "Sh",    C(1,1,1,0.03f), V(0,0.5f),   V(1,1),     V(0,0),   V(0,0));
        MkImg(card, "LineT", ACCENT,          V(0,1),      V(1,1),     V(0,-4),  V(0,8));
        MkImg(card, "AccL",  ACCENT,          V(0,0.08f),  V(0,0.92f), V(4,0),   V(8,0));

        _finalTitle = MkTxt(card, "FT", "", Color.white, 44, V(0.05f,0.76f), V(0.95f,0.97f));
        _finalTitle.fontStyle = FontStyles.Bold;
        _finalTitle.enableAutoSizing = true;
        _finalTitle.fontSizeMin = 26f; _finalTitle.fontSizeMax = 46f;

        _finalSub = MkTxt(card, "FS", "", C(0.50f,0.68f,0.80f), 22, V(0.05f,0.22f), V(0.95f,0.74f));
        _finalSub.overflowMode = TextOverflowModes.Overflow;
        _finalSub.alignment    = TextAlignmentOptions.Center;
        _finalSub.lineSpacing  = 10f;

        MkBtn(card, "Jugar de nuevo", ACCENT,               V(0.05f,0.04f), V(0.46f,0.17f), onRestart);
        MkBtn(card, "Menú",           C(0.14f,0.22f,0.38f), V(0.54f,0.04f), V(0.95f,0.17f), onMenu);

        _finalPanel.SetActive(false);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // API pública
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Actualiza la barra de tiempo restante. t en [0,1].</summary>
    public void SetTimerBar(float t)
    {
        if (_timerFill == null) return;
        t = Mathf.Clamp01(t);
        _timerFill.anchorMax = new Vector2(TIMER_LEFT + (TIMER_RIGHT - TIMER_LEFT) * t,
                                           _timerFill.anchorMax.y);
        // Color: verde→amarillo→rojo según tiempo restante
        Color col = t > 0.5f
            ? Color.Lerp(new Color(0.95f,0.80f,0.15f), ACCENT,     (t - 0.5f) * 2f)
            : Color.Lerp(CRED,                           new Color(0.95f,0.80f,0.15f), t * 2f);
        _timerFillImg.color = col;
    }

    /// <summary>Muestra el feedback de resultado de la ronda.</summary>
    public void ShowFeedback(bool correct, string correctDirName)
    {
        if (_feedbackRoutine != null) StopCoroutine(_feedbackRoutine);
        _feedbackRoutine = StartCoroutine(FeedbackRoutine(correct, correctDirName));
    }

    /// <summary>Oculta el feedback inmediatamente.</summary>
    public void HideFeedback()
    {
        if (_feedbackRoutine != null) { StopCoroutine(_feedbackRoutine); _feedbackRoutine = null; }
        if (_feedbackText)   _feedbackText.text = "";
        if (_flashOverlay)   _flashOverlay.gameObject.SetActive(false);
    }

    IEnumerator FeedbackRoutine(bool correct, string correctDirName)
    {
        Color flashCol = correct ? C(0.22f,0.86f,0.54f,0.24f) : C(0.90f,0.22f,0.28f,0.24f);
        Color textCol  = correct ? CGREEN : CRED;
        string txt     = correct
            ? "✓  CORRECTO  —  " + correctDirName
            : "✗  INCORRECTO  —  era " + correctDirName;

        if (_feedbackText) { _feedbackText.text = txt; _feedbackText.color = textCol; }

        if (_flashOverlay != null)
        {
            _flashOverlay.gameObject.SetActive(true);
            _flashOverlay.color = flashCol;
            float t = 0f;
            while (t < 0.38f)
            {
                t += Time.deltaTime;
                _flashOverlay.color = Color.Lerp(flashCol, C(0,0,0,0), t / 0.38f);
                yield return null;
            }
            _flashOverlay.gameObject.SetActive(false);
        }
        _feedbackRoutine = null;
    }

    public void SetRoundDot(int index, bool? correct)
    {
        if (_roundDots == null || index >= _roundDots.Length) return;
        _roundDots[index].color = correct == null  ? C(0.25f,0.30f,0.40f)
                                : correct == true  ? CGREEN : CRED;
    }

    public void SetScore(int score)
    {
        if (_scoreText) _scoreText.text = score + " pts";
    }

    public void ShowFinalResult(bool won, int correct, int total, int score)
    {
        _finalPanel.SetActive(true);

        _finalTitle.text  = won ? "¡Resististe a la mayoría!" : "La mayoría te venció";
        _finalTitle.color = won ? CGREEN : CRED;

        string msg = won
            ? correct + " de " + total + " rondas correctas.\n" +
              "Puntuación: " + score + " pts\n\n" +
              "Tu cerebro supo ver más allá del instinto.\n" +
              "Excelente control de impulsos."
            : "Aciertos: " + correct + " de " + total + "\n\n" +
              "Seguir a la mayoría es una respuesta automática.\n" +
              "¡Entrena tu mirada crítica y vuelve a intentarlo!";

        _finalSub.text = msg;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Helpers UI
    // ═══════════════════════════════════════════════════════════════════════

    RectTransform MkImg(RectTransform p, string n, Color col,
                        Vector2 am, Vector2 aM, Vector2 pos, Vector2 sd)
    {
        var go = new GameObject(n);
        go.transform.SetParent(p, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = am; rt.anchorMax = aM;
        rt.pivot = V(0.5f,0.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = sd;
        go.AddComponent<Image>().color = col;
        return rt;
    }

    TextMeshProUGUI MkTxt(RectTransform p, string n, string txt,
                           Color col, float sz, Vector2 am, Vector2 aM)
    {
        var go = new GameObject(n);
        go.transform.SetParent(p, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = am; rt.anchorMax = aM;
        rt.pivot = V(0.5f,0.5f);
        rt.anchoredPosition = Vector2.zero; rt.sizeDelta = Vector2.zero;
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = txt; t.color = col; t.fontSize = sz;
        t.alignment    = TextAlignmentOptions.Center;
        t.overflowMode = TextOverflowModes.Overflow;
        return t;
    }

    void MkBtn(RectTransform p, string lbl, Color bg, Vector2 am, Vector2 aM, Action click)
    {
        var rt = MkImg(p, "Btn_"+lbl, bg, am, aM, V(0,0), V(0,0));
        MkImg(rt, "Sh", C(1,1,1,0.09f), V(0,0.5f), V(1,1), V(0,0), V(0,0));
        var b = rt.gameObject.AddComponent<Button>();
        b.targetGraphic = rt.GetComponent<Image>();
        var cb = b.colors;
        cb.normalColor = Color.white; cb.highlightedColor = C(1,1,1,0.82f);
        cb.pressedColor = C(0.72f,0.72f,0.72f); b.colors = cb;
        b.onClick.AddListener(() => click?.Invoke());
        var t = MkTxt(rt, "T", lbl, Color.white, 22, V(0,0), V(1,1));
        t.fontStyle = FontStyles.Bold;
    }

    static Sprite MakeCircleSprite(int res = 64)
    {
        var tex = new Texture2D(res, res, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float r = res * 0.5f;
        var px = new Color[res * res];
        for (int y = 0; y < res; y++)
        for (int x = 0; x < res; x++)
        {
            float d = Vector2.Distance(new Vector2(x+0.5f,y+0.5f), new Vector2(r,r));
            float a = Mathf.Clamp01(1f - (d - r + 1.5f) / 2f);
            px[y*res+x] = new Color(1,1,1,a);
        }
        tex.SetPixels(px); tex.Apply();
        return Sprite.Create(tex, new Rect(0,0,res,res), V(0.5f,0.5f));
    }
}
