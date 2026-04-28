using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static InverseResponseStimulusManager;

/// <summary>
/// Construye toda la UI de "Respuesta Inversa" de forma procedural.
/// Sin prefabs ni assets externos.
///
/// LAYOUT (1920×1080, tres zonas bien separadas verticalmente):
///
///  ┌─────────────────────────────────────────────────┐  y=1080
///  │  HEADER  (88px)  Titulo │ Categoria │ Puntos    │
///  ├─────────────────────────────────────────────────┤  y=992
///  │                                                  │
///  │   ZONA A – FLECHA (y≈62%)                        │
///  │        Círculo 240px + flecha blanca              │
///  │                                                  │
///  │   ZONA B – REGLA (y≈43%)                         │
///  │      Panel "INVERSA / IGUAL" + descripcion       │
///  │                                                  │
///  │   ZONA C – BOTONES (y≈24%)                       │
///  │       Cruz de 4 botones compactos (80px cada uno)│
///  │                                                  │
///  ├─────────────────────────────────────────────────┤  y=80
///  │  FOOTER (80px)  Instruccion │ Menu               │
///  └─────────────────────────────────────────────────┘  y=0
///
///  Lateral izquierdo: barra de tiempo (y: 10%–90%)
///  La flecha se dibuja proceduralmente con 3 rectángulos rotados.
/// </summary>
public class InverseResponseUIController : MonoBehaviour
{
    // ── Helpers ───────────────────────────────────────────────────────────
    static Vector2 V(float x, float y) => new Vector2(x, y);
    static Color   C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

    // ── Paleta ────────────────────────────────────────────────────────────
    static readonly Color BG      = C(0.05f, 0.07f, 0.13f);
    static readonly Color HDR     = C(0.03f, 0.04f, 0.09f);
    static readonly Color PANEL   = C(0.07f, 0.10f, 0.20f);
    static readonly Color ACCENT  = C(0.18f, 0.80f, 0.58f);
    static readonly Color DIM     = C(0.38f, 0.52f, 0.63f);
    static readonly Color CRED    = C(0.88f, 0.22f, 0.28f);
    static readonly Color CGREEN  = C(0.20f, 0.86f, 0.52f);
    static readonly Color CYELLOW = C(0.96f, 0.80f, 0.15f);
    static readonly Color CARROW  = C(0.92f, 0.92f, 0.96f);

    // ── Refs publicas ─────────────────────────────────────────────────────
    public InverseResponseInputHandler InputHandler { get; private set; }

    // ── Refs internas ─────────────────────────────────────────────────────
    RectTransform    _arrowParent;
    Image[]          _arrowParts;
    Image            _arrowBg;

    TextMeshProUGUI  _ruleName;     // "INVERSA" / "IGUAL"
    TextMeshProUGUI  _ruleDesc;     // descripcion de la regla
    Image            _rulePanelBg;  // fondo del panel de regla (cambia de color)

    TextMeshProUGUI  _scoreText;
    TextMeshProUGUI  _feedbackText;
    Image            _timerBar;
    Image            _flashOverlay;

    GameObject       _resultPanel;
    TextMeshProUGUI  _resultTitle;
    TextMeshProUGUI  _resultSub;

    // ═════════════════════════════════════════════════════════════════════
    // Construccion principal
    // ═════════════════════════════════════════════════════════════════════

    public void BuildUI(int totalStimuli, Action onRestart, Action onMenu,
                        InverseResponseInputHandler inputHandler)
    {
        InputHandler = inputHandler;

        // ── Canvas ──────────────────────────────────────────────────────
        var cGO = new GameObject("Canvas_InverseResp");
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

        // ── Fondo ────────────────────────────────────────────────────────
        MkImg(R, "BG", BG, V(0,0), V(1,1), V(0,0), V(0,0));

        // ── Header ──────────────────────────────────────────────────────
        BuildHeader(R, totalStimuli);

        // ── Barra lateral de tiempo ──────────────────────────────────────
        BuildTimerBar(R);

        // ── ZONA A: Flecha grande ────────────────────────────────────────
        BuildArrowZone(R);

        // ── ZONA B: Banner de regla ──────────────────────────────────────
        BuildRuleBanner(R);

        // ── Texto de feedback (entre banner y botones) ────────────────────
        _feedbackText = MkTxt(R, "Feedback", "", CGREEN, 32,
                              V(0.15f, 0.40f), V(0.85f, 0.47f));
        _feedbackText.fontStyle = FontStyles.Bold;
        _feedbackText.alignment = TextAlignmentOptions.Center;

        // ── ZONA C: Botones de direccion ─────────────────────────────────
        BuildDirectionButtons(R);

        // ── Flash de impacto ─────────────────────────────────────────────
        var flashGO = new GameObject("Flash");
        flashGO.transform.SetParent(R, false);
        var fRT = flashGO.AddComponent<RectTransform>();
        fRT.anchorMin = V(0,0); fRT.anchorMax = V(1,1);
        fRT.sizeDelta = fRT.anchoredPosition = Vector2.zero;
        _flashOverlay = flashGO.AddComponent<Image>();
        _flashOverlay.color = C(0,0,0,0);
        flashGO.SetActive(false);

        // ── Footer ──────────────────────────────────────────────────────
        var bot = MkImg(R, "Bot", HDR, V(0,0), V(1,0), V(0,40), V(0,80));
        MkImg(bot, "LineT", ACCENT, V(0,1), V(1,1), V(0,-1.5f), V(0,3));
        MkTxt(bot, "Info",
              "Flechas del teclado o WASD  •  Pulsa segun la regla activa",
              C(ACCENT.r, ACCENT.g - 0.08f, ACCENT.b - 0.05f),
              16, V(0.01f,0), V(0.78f,1)).alignment = TextAlignmentOptions.MidlineLeft;
        MkImg(bot, "Sep", C(1,1,1,0.08f), V(0.78f,0.1f), V(0.782f,0.9f), V(0,0), V(0,0));
        MkBtn(bot, "Menu", C(0.12f,0.18f,0.32f), V(0.80f,0.08f), V(0.99f,0.92f), onMenu);

        // ── Panel de resultado ───────────────────────────────────────────
        BuildResultPanel(R, onRestart, onMenu);
    }

    // ─────────────────────────────────────────────────────────────────────
    // ZONA HEADER
    // ─────────────────────────────────────────────────────────────────────
    void BuildHeader(RectTransform R, int totalStimuli)
    {
        var hdr = MkImg(R, "Hdr", HDR, V(0,1), V(1,1), V(0,-44), V(0,88));
        MkImg(hdr, "LineB", ACCENT, V(0,0), V(1,0), V(0,1.5f), V(0,3));
        MkImg(hdr, "AccL",  ACCENT, V(0,0.18f), V(0,0.82f), V(3,0), V(6,0));

        var ttl = MkTxt(hdr, "T", "RESPUESTA INVERSA", Color.white, 28,
                        V(0.03f,0.12f), V(0.48f,0.88f));
        ttl.fontStyle = FontStyles.Bold;
        ttl.alignment = TextAlignmentOptions.MidlineLeft;
        ttl.characterSpacing = 1.5f;

        MkTxt(hdr, "Cat", "CONTROL DE IMPULSOS", DIM, 14,
              V(0.48f,0.12f), V(0.68f,0.88f)).alignment = TextAlignmentOptions.MidlineRight;

        _scoreText = MkTxt(hdr, "Score", "0 / " + totalStimuli, CGREEN, 24,
                           V(0.80f,0.12f), V(0.98f,0.88f));
        _scoreText.fontStyle = FontStyles.Bold;
        _scoreText.alignment = TextAlignmentOptions.MidlineRight;
    }

    // ─────────────────────────────────────────────────────────────────────
    // BARRA LATERAL DE TIEMPO
    // ─────────────────────────────────────────────────────────────────────
    void BuildTimerBar(RectTransform R)
    {
        // Contenedor estrecho pegado al borde izquierdo
        var panel = MkImg(R, "TimerPanel", C(0.04f,0.07f,0.14f,0.90f),
                          V(0,0.10f), V(0,0.90f), V(26f,0), V(20f,0));
        MkImg(panel, "R", ACCENT, V(1,0), V(1,1), V(-1f,0), V(2,0));

        // Fondo oscuro de la barra
        var barBG = MkImg(panel, "BarBG", C(0.02f,0.04f,0.09f),
                          V(0.15f,0.04f), V(0.85f,0.96f), V(0,0), V(0,0));

        var fillGO = new GameObject("Fill");
        fillGO.transform.SetParent(barBG, false);
        var fillRT = fillGO.AddComponent<RectTransform>();
        fillRT.anchorMin = V(0,0); fillRT.anchorMax = V(1,1);
        fillRT.sizeDelta = fillRT.anchoredPosition = Vector2.zero;
        _timerBar             = fillGO.AddComponent<Image>();
        _timerBar.color       = CGREEN;
        _timerBar.type        = Image.Type.Filled;
        _timerBar.fillMethod  = Image.FillMethod.Vertical;
        _timerBar.fillOrigin  = 1; // de arriba a abajo (se vacia)
        _timerBar.fillAmount  = 1f;

        MkTxt(panel, "Lbl", "T", DIM, 10, V(0,-0.04f), V(1,0.04f));
    }

    // ─────────────────────────────────────────────────────────────────────
    // ZONA A: FLECHA GRANDE
    // y≈62% de la pantalla (bien por encima del centro)
    // ─────────────────────────────────────────────────────────────────────
    void BuildArrowZone(RectTransform R)
    {
        // Circulo de fondo
        var bgGO = new GameObject("ArrowBG");
        bgGO.transform.SetParent(R, false);
        var bgRT = bgGO.AddComponent<RectTransform>();
        bgRT.anchorMin = bgRT.anchorMax = V(0.5f, 0.70f);
        bgRT.pivot     = V(0.5f, 0.5f);
        bgRT.sizeDelta = V(250f, 250f);
        bgRT.anchoredPosition = Vector2.zero;
        _arrowBg = bgGO.AddComponent<Image>();
        _arrowBg.sprite = MakeCircleSprite(128);
        _arrowBg.color  = C(0.10f, 0.14f, 0.26f);

        // Contenedor de la flecha (rotamos solo este)
        var arrowGO = new GameObject("Arrow");
        arrowGO.transform.SetParent(R, false);
        _arrowParent = arrowGO.AddComponent<RectTransform>();
        _arrowParent.anchorMin = _arrowParent.anchorMax = V(0.5f, 0.70f);
        _arrowParent.pivot     = V(0.5f, 0.5f);
        _arrowParent.sizeDelta = V(200f, 200f);
        _arrowParent.anchoredPosition = Vector2.zero;

        _arrowParts = BuildArrowShape(_arrowParent, CARROW);
    }

    // ─────────────────────────────────────────────────────────────────────
    // ZONA B: BANNER DE REGLA
    // y≈44% — claramente separado de la flecha (arriba) y los botones (abajo)
    // ─────────────────────────────────────────────────────────────────────
    void BuildRuleBanner(RectTransform R)
    {
        // Panel de fondo del banner
        var panelGO = new GameObject("RulePanel");
        panelGO.transform.SetParent(R, false);
        var panelRT = panelGO.AddComponent<RectTransform>();
        panelRT.anchorMin = panelRT.anchorMax = V(0.5f, 0.515f);
        panelRT.pivot     = V(0.5f, 0.5f);
        panelRT.sizeDelta = V(560f, 92f);
        panelRT.anchoredPosition = Vector2.zero;
        _rulePanelBg = panelGO.AddComponent<Image>();
        _rulePanelBg.color = C(0.08f, 0.12f, 0.24f);

        // Linea de acento a la izquierda
        var accL = new GameObject("AccL");
        accL.transform.SetParent(panelGO.transform, false);
        var aRT = accL.AddComponent<RectTransform>();
        aRT.anchorMin = V(0,0.1f); aRT.anchorMax = V(0,0.9f);
        aRT.sizeDelta = V(5,0); aRT.anchoredPosition = V(3,0);
        accL.AddComponent<Image>().color = CYELLOW;

        // Nombre de la regla (grande, a la izquierda)
        _ruleName = MkTxt(panelRT, "RuleName", "INVERSA", CYELLOW, 30,
                          V(0.04f, 0.45f), V(0.42f, 0.98f));
        _ruleName.fontStyle = FontStyles.Bold;
        _ruleName.alignment = TextAlignmentOptions.MidlineLeft;

        // Separador vertical
        var sepGO = new GameObject("Sep");
        sepGO.transform.SetParent(panelGO.transform, false);
        var sepRT = sepGO.AddComponent<RectTransform>();
        sepRT.anchorMin = V(0.42f, 0.12f); sepRT.anchorMax = V(0.422f, 0.88f);
        sepRT.sizeDelta = sepRT.anchoredPosition = Vector2.zero;
        sepGO.AddComponent<Image>().color = C(1,1,1,0.12f);

        // Descripcion de la regla (derecha del separador)
        _ruleDesc = MkTxt(panelRT, "RuleDesc", "Pulsa la direccion CONTRARIA",
                          C(0.75f, 0.85f, 0.90f), 20,
                          V(0.44f, 0.05f), V(0.98f, 0.95f));
        _ruleDesc.alignment  = TextAlignmentOptions.MidlineLeft;
        _ruleDesc.fontStyle  = FontStyles.Normal;
        _ruleDesc.enableWordWrapping = false;
    }

    // ─────────────────────────────────────────────────────────────────────
    // ZONA C: BOTONES DE DIRECCION
    // Cruz de 4 botones en y≈22%, tamaño 80px, separación 100px
    // ─────────────────────────────────────────────────────────────────────
    void BuildDirectionButtons(RectTransform R)
    {
        // Centro de la cruz en coordenadas de referencia: (960, 230) → fraccion (0.5, 0.213)
        // Separación centro a centro: 100px → 100/1080=0.0926 vertical, 100/1920=0.0521 horizontal
        const float CY    = 0.285f;
        const float CX    = 0.500f;
        const float VSEP  = 100f / 1080f;  // separacion vertical
        const float HSEP  = 100f / 1920f;  // separacion horizontal
        const float BSIZE = 80f;           // tamaño del boton

        var layout = new (float ax, float ay, ArrowDirection dir)[]
        {
            (CX,        CY + VSEP, ArrowDirection.Up),
            (CX,        CY - VSEP, ArrowDirection.Down),
            (CX - HSEP, CY,        ArrowDirection.Left),
            (CX + HSEP, CY,        ArrowDirection.Right),
        };

        for (int i = 0; i < layout.Length; i++)
        {
            var (ax, ay, dir) = layout[i];
            BuildDirButton(R, ax, ay, BSIZE, dir);
        }
    }

    void BuildDirButton(RectTransform R, float ax, float ay,
                        float size, ArrowDirection dir)
    {
        // Fondo del boton (circulo)
        var btnGO = new GameObject("DirBtn_" + dir);
        btnGO.transform.SetParent(R, false);
        var btnRT = btnGO.AddComponent<RectTransform>();
        btnRT.anchorMin = btnRT.anchorMax = V(ax, ay);
        btnRT.pivot     = V(0.5f, 0.5f);
        btnRT.sizeDelta = V(size, size);
        btnRT.anchoredPosition = Vector2.zero;

        var btnImg = btnGO.AddComponent<Image>();
        btnImg.sprite = MakeCircleSprite(64);
        btnImg.color  = C(0.12f, 0.16f, 0.28f);

        // Componente Button
        var btn = btnGO.AddComponent<Button>();
        btn.targetGraphic = btnImg;
        var cb = btn.colors;
        cb.normalColor      = Color.white;
        cb.highlightedColor = C(1f, 1f, 1f, 0.78f);
        cb.pressedColor     = C(0.60f, 0.60f, 0.60f);
        btn.colors = cb;
        btn.onClick.AddListener(() => InputHandler?.PressDirection(dir));

        // Mini flecha dentro del boton
        var miniGO = new GameObject("Mini");
        miniGO.transform.SetParent(btnGO.transform, false);
        var miniRT = miniGO.AddComponent<RectTransform>();
        miniRT.anchorMin = miniRT.anchorMax = V(0.5f, 0.5f);
        miniRT.pivot     = V(0.5f, 0.5f);
        miniRT.sizeDelta = V(size * 0.72f, size * 0.72f);
        miniRT.anchoredPosition = Vector2.zero;
        miniRT.localRotation = Quaternion.Euler(0, 0, DirectionToDeg(dir));
        BuildArrowShape(miniRT, CARROW);

        // Etiqueta de tecla debajo del boton
        string key = dir == ArrowDirection.Up   ? "W / ↑"
                   : dir == ArrowDirection.Down  ? "S / ↓"
                   : dir == ArrowDirection.Left  ? "A / ←"
                   :                               "D / →";
        var lblGO = new GameObject("Key");
        lblGO.transform.SetParent(R, false);
        var lblRT = lblGO.AddComponent<RectTransform>();
        lblRT.anchorMin = lblRT.anchorMax = V(ax, ay);
        lblRT.pivot     = V(0.5f, 1.0f);                     // anclado por la parte superior
        lblRT.sizeDelta = V(120f, 28f);
        lblRT.anchoredPosition = V(0f, -(size * 0.5f + 6f)); // justo debajo del boton
        var lbl = lblGO.AddComponent<TextMeshProUGUI>();
        lbl.text      = key;
        lbl.color     = DIM;
        lbl.fontSize  = 14f;
        lbl.alignment = TextAlignmentOptions.Center;
        lbl.overflowMode = TextOverflowModes.Overflow;
    }

    // ─────────────────────────────────────────────────────────────────────
    // PANEL DE RESULTADO
    // ─────────────────────────────────────────────────────────────────────
    void BuildResultPanel(RectTransform R, Action onRestart, Action onMenu)
    {
        _resultPanel = new GameObject("ResultPanel");
        _resultPanel.transform.SetParent(R, false);
        var er = _resultPanel.AddComponent<RectTransform>();
        er.anchorMin = V(0,0); er.anchorMax = V(1,1);
        er.sizeDelta = er.anchoredPosition = Vector2.zero;
        _resultPanel.AddComponent<Image>().color = C(0,0,0,0.88f);

        var card = MkImgFixed(er, "Card", PANEL, V(0.5f,0.5f), V(0,0), V(920f,500f));
        MkImg(card, "Sh",    C(1,1,1,0.03f), V(0,0.5f), V(1,1),      V(0,0),  V(0,0));
        MkImg(card, "LineT", ACCENT,          V(0,1),    V(1,1),      V(0,-4), V(0,8));
        MkImg(card, "AccL",  ACCENT,          V(0,0.08f), V(0,0.92f), V(4,0),  V(8,0));

        _resultTitle = MkTxt(card, "RT", "", Color.white, 44,
                             V(0.05f,0.78f), V(0.95f,0.97f));
        _resultTitle.fontStyle = FontStyles.Bold;
        _resultTitle.enableAutoSizing = true;
        _resultTitle.fontSizeMin = 26f; _resultTitle.fontSizeMax = 46f;

        _resultSub = MkTxt(card, "RS", "", C(0.50f,0.68f,0.80f), 22,
                           V(0.05f,0.22f), V(0.95f,0.76f));
        _resultSub.overflowMode = TextOverflowModes.Overflow;
        _resultSub.alignment    = TextAlignmentOptions.Center;
        _resultSub.lineSpacing  = 10f;

        MkBtn(card, "Jugar de nuevo", ACCENT,              V(0.05f,0.04f), V(0.46f,0.17f), onRestart);
        MkBtn(card, "Menu",          C(0.14f,0.22f,0.38f), V(0.54f,0.04f), V(0.95f,0.17f), onMenu);

        _resultPanel.SetActive(false);
    }

    // ═════════════════════════════════════════════════════════════════════
    // API PUBLICA
    // ═════════════════════════════════════════════════════════════════════

    public void ShowArrow(ArrowDirection dir, GameRule rule)
    {
        // Rotar la flecha
        _arrowParent.localRotation = Quaternion.Euler(0, 0, DirectionToDeg(dir));

        // Tinte del circulo segun regla
        bool inv = rule == GameRule.Inverse;
        _arrowBg.color = inv ? C(0.12f, 0.08f, 0.24f) : C(0.08f, 0.20f, 0.14f);

        // Banner de regla
        _rulePanelBg.color = inv ? C(0.10f, 0.08f, 0.22f) : C(0.06f, 0.16f, 0.10f);

        if (inv)
        {
            _ruleName.text  = "INVERSA";
            _ruleName.color = CYELLOW;
            _ruleDesc.text  = "Pulsa la direccion CONTRARIA";
        }
        else
        {
            _ruleName.text  = "IGUAL";
            _ruleName.color = CGREEN;
            _ruleDesc.text  = "Pulsa la misma direccion";
        }

        // Hacer visible la flecha
        foreach (var p in _arrowParts) if (p) p.color = CARROW;
    }

    public void HideArrow()
    {
        foreach (var p in _arrowParts) if (p) p.color = C(0,0,0,0);
        if (_arrowBg) _arrowBg.color = C(0,0,0,0);
    }

    public void ShowArrowVisible()
    {
        foreach (var p in _arrowParts) if (p) p.color = CARROW;
    }

    public void UpdateTimerBar(float elapsed, float total)
    {
        if (_timerBar == null) return;
        float f = 1f - Mathf.Clamp01(elapsed / total);
        _timerBar.fillAmount = f;
        _timerBar.color = Color.Lerp(CRED, CGREEN, f);
    }

    public void UpdateScore(int correct, int errors, int total)
    {
        if (_scoreText == null) return;
        _scoreText.text  = correct + " / " + total;
        _scoreText.color = errors > 2 ? CYELLOW : CGREEN;
    }

    public void ShowFeedback(bool correct, string msg)
    {
        if (_feedbackText != null)
        {
            _feedbackText.text  = msg;
            _feedbackText.color = correct ? CGREEN : CRED;
        }
        Flash(correct ? C(0.08f, 0.50f, 0.18f, 0.18f) : C(0.72f, 0.08f, 0.12f, 0.22f));
        StartCoroutine(ClearFeedback(0.70f));
    }

    public void ShowFinalResult(bool won, int correct, int errors, int total, int score)
    {
        string pct = total > 0
            ? " (" + Mathf.RoundToInt(correct * 100f / total) + "% precision)"
            : "";

        _resultTitle.text  = won ? "¡Control conseguido!" : "El impulso gano esta vez";
        _resultTitle.color = won ? CGREEN : CRED;
        _resultSub.text    = won
            ? "Respuestas correctas: " + correct + "/" + total + pct + "\n" +
              "Errores: " + errors + "   Puntuacion: " + score + " pts\n\n" +
              "Inhibiste el impulso y aplicaste la regla correctamente."
            : "Respuestas correctas: " + correct + "/" + total + pct + "\n" +
              "Errores: " + errors + "\n\n" +
              "Es normal seguir el impulso al principio.\n" +
              "Con practica el cerebro aprende a frenar la respuesta habitual.";
        _resultPanel.SetActive(true);
    }

    // ═════════════════════════════════════════════════════════════════════
    // HELPERS INTERNOS
    // ═════════════════════════════════════════════════════════════════════

    IEnumerator ClearFeedback(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (_feedbackText != null) _feedbackText.text = "";
    }

    void Flash(Color col)
    {
        if (_flashOverlay == null) return;
        _flashOverlay.gameObject.SetActive(true);
        _flashOverlay.color = col;
        StartCoroutine(FlashRoutine());
    }

    IEnumerator FlashRoutine()
    {
        Color start = _flashOverlay.color;
        float t = 0f;
        while (t < 0.40f)
        {
            t += Time.deltaTime;
            _flashOverlay.color = Color.Lerp(start, C(0,0,0,0), t / 0.40f);
            yield return null;
        }
        _flashOverlay.gameObject.SetActive(false);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Angulo de rotacion: la flecha base apunta a la DERECHA (→)
    //   Derecha → 0°    Arriba → 90°    Izquierda → 180°    Abajo → 270°
    // ─────────────────────────────────────────────────────────────────────
    static float DirectionToDeg(ArrowDirection d)
    {
        switch (d)
        {
            case ArrowDirection.Right: return   0f;
            case ArrowDirection.Up:    return  90f;
            case ArrowDirection.Left:  return 180f;
            default:                   return 270f; // Down
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // Flecha procedural: tallo + 2 brazos de punta, apuntando a la derecha.
    // El padre se rota para las otras direcciones.
    // ─────────────────────────────────────────────────────────────────────
    Image[] BuildArrowShape(RectTransform parent, Color col)
    {
        // Factor de escala relativo al tamaño del contenedor
        float w = parent.sizeDelta.x;
        float scale = w / 200f;   // 200 = tamaño de referencia

        var parts = new Image[3];

        // Tallo
        var sGO = new GameObject("S");
        sGO.transform.SetParent(parent, false);
        var sRT = sGO.AddComponent<RectTransform>();
        sRT.anchoredPosition = V(-12f * scale, 0f);
        sRT.sizeDelta        = V(110f * scale, 22f * scale);
        sRT.pivot            = V(0.5f, 0.5f);
        parts[0] = sGO.AddComponent<Image>();
        parts[0].color = col;

        // Brazo superior
        var tGO = new GameObject("T");
        tGO.transform.SetParent(parent, false);
        var tRT = tGO.AddComponent<RectTransform>();
        tRT.anchoredPosition = V(38f * scale, 24f * scale);
        tRT.sizeDelta        = V(64f * scale, 22f * scale);
        tRT.pivot            = V(0.5f, 0.5f);
        tRT.localRotation    = Quaternion.Euler(0, 0, -40f);
        parts[1] = tGO.AddComponent<Image>();
        parts[1].color = col;

        // Brazo inferior
        var bGO = new GameObject("B");
        bGO.transform.SetParent(parent, false);
        var bRT = bGO.AddComponent<RectTransform>();
        bRT.anchoredPosition = V(38f * scale, -24f * scale);
        bRT.sizeDelta        = V(64f * scale, 22f * scale);
        bRT.pivot            = V(0.5f, 0.5f);
        bRT.localRotation    = Quaternion.Euler(0, 0, 40f);
        parts[2] = bGO.AddComponent<Image>();
        parts[2].color = col;

        return parts;
    }

    // ─────────────────────────────────────────────────────────────────────
    // Sprite circular procedural
    // ─────────────────────────────────────────────────────────────────────
    public static Sprite MakeCircleSprite(int res = 128)
    {
        var tex = new Texture2D(res, res, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        var center = new Vector2(res * 0.5f, res * 0.5f);
        float r    = res * 0.5f;
        var px     = new Color[res * res];
        for (int y = 0; y < res; y++)
            for (int x = 0; x < res; x++)
            {
                float d = Vector2.Distance(new Vector2(x+.5f, y+.5f), center);
                float a = Mathf.Clamp01(1f - (d - r + 1.5f) / 2f);
                px[y*res+x] = new Color(1,1,1,a);
            }
        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0,0,res,res), V(0.5f,0.5f));
    }

    // ─────────────────────────────────────────────────────────────────────
    // Constructores de UI
    // ─────────────────────────────────────────────────────────────────────

    // Imagen con anclas estiradas (anchor min/max distintos)
    RectTransform MkImg(RectTransform p, string n, Color col,
                        Vector2 am, Vector2 aM, Vector2 pos, Vector2 sd)
    {
        var go = new GameObject(n);
        go.transform.SetParent(p, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = am; rt.anchorMax = aM;
        rt.pivot = V(0.5f, 0.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = sd;
        go.AddComponent<Image>().color = col;
        return rt;
    }

    // Imagen con ancla en punto fijo + tamaño absoluto
    RectTransform MkImgFixed(RectTransform p, string n, Color col,
                             Vector2 anchor, Vector2 pos, Vector2 sd)
    {
        var go = new GameObject(n);
        go.transform.SetParent(p, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = V(0.5f, 0.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = sd;
        go.AddComponent<Image>().color = col;
        return rt;
    }

    TextMeshProUGUI MkTxt(RectTransform p, string n, string txt, Color col,
                           float sz, Vector2 am, Vector2 aM)
    {
        var go = new GameObject(n);
        go.transform.SetParent(p, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = am; rt.anchorMax = aM;
        rt.pivot = V(0.5f, 0.5f);
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
        cb.pressedColor = C(0.72f,0.72f,0.72f);
        b.colors = cb;
        b.onClick.AddListener(() => click?.Invoke());
        var t = MkTxt(rt, "T", lbl, Color.white, 24, V(0,0), V(1,1));
        t.fontStyle = FontStyles.Bold;
    }
}
