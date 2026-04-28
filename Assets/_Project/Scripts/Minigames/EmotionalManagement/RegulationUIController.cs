using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI de "Regulacion Progresiva".
///
/// Novedades respecto a la version anterior:
///   • Indicador de regeneracion (+8/turno) siempre visible junto a la barra
///   • Numero de nivel puede superar 100 (sin tope superior)
///   • Barra llena al 100% cuando nivel >= 100, con color rojo vivo
///   • Cooldown visible en cada boton (overlay oscuro + contador)
/// </summary>
public class RegulationUIController : MonoBehaviour
{
    static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);
    static Vector2 V(float x, float y) => new Vector2(x, y);

    static readonly Color BG      = C(0.05f, 0.08f, 0.13f);
    static readonly Color HDR     = C(0.03f, 0.06f, 0.11f);
    static readonly Color PANEL   = C(0.07f, 0.11f, 0.20f);
    static readonly Color ACCENT  = C(0.18f, 0.80f, 0.58f);
    static readonly Color DIM     = C(0.38f, 0.54f, 0.62f);
    static readonly Color BTN_N   = C(0.10f, 0.16f, 0.27f);
    static readonly Color BTN_CD  = C(0.06f, 0.09f, 0.16f);
    static readonly Color CGREEN  = C(0.22f, 0.86f, 0.54f);
    static readonly Color CYELLOW = C(0.96f, 0.82f, 0.20f);
    static readonly Color CRED    = C(0.90f, 0.28f, 0.30f);
    static readonly Color CDARK   = C(0.60f, 0.14f, 0.15f);  // rojo muy oscuro (sobrecarga)

    // ── Referencias ───────────────────────────────────────────────────────
    TextMeshProUGUI _stepsLbl;
    TextMeshProUGUI _scoreLbl;
    Image           _barFill;
    TextMeshProUGUI _levelLbl;
    TextMeshProUGUI _stateLbl;
    TextMeshProUGUI _regenLbl;      // "+8 / turno" siempre visible
    Image           _stepsFill;
    TextMeshProUGUI _stepsBarLbl;
    TextMeshProUGUI _feedbackLbl;

    Button[]          _actionBtns;
    Image[]           _actionBtnImgs;
    Image[]           _actionCDOverlay;
    TextMeshProUGUI[] _actionCDLbl;

    GameObject      _resultPanel;
    TextMeshProUGUI _resultTitle;
    TextMeshProUGUI _resultSub;

    Action<int> _onAction;

    // ═════════════════════════════════════════════════════════════════════
    public void BuildUI(Action<int> onAction, Action onRestart, Action onMenu)
    {
        _onAction = onAction;

        var cGO = new GameObject("Canvas_Regulation");
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

        MkImg(R, "BG",    BG,                          V(0,0),     V(1,1),    V(0,0), V(0,0));
        MkImg(R, "GradT", C(0.04f,0.18f,0.14f,0.18f), V(0,0.50f), V(1,1),   V(0,0), V(0,0));
        MkImg(R, "GradB", C(0.02f,0.04f,0.08f,0.28f), V(0,0),     V(1,0.3f),V(0,0), V(0,0));

        // ── Header ──────────────────────────────────────────────────────
        var hdr = MkImg(R, "Hdr", HDR, V(0,1), V(1,1), V(0,-44), V(0,88));
        MkImg(hdr, "Line", ACCENT, V(0,0),     V(1,0),     V(0,1.5f), V(0,3));
        MkImg(hdr, "AccL", ACCENT, V(0,0.18f), V(0,0.82f), V(3,0),    V(6,0));

        var ttl = MkTxt(hdr, "T", "REGULACION PROGRESIVA", Color.white, 30,
                        V(0.03f,0.12f), V(0.55f,0.88f));
        ttl.fontStyle = FontStyles.Bold; ttl.alignment = TextAlignmentOptions.MidlineLeft;
        ttl.characterSpacing = 1.5f;

        MkTxt(hdr, "Cat", "GESTION EMOCIONAL", DIM, 15,
              V(0.55f,0.12f), V(0.74f,0.88f)).alignment = TextAlignmentOptions.MidlineRight;

        _stepsLbl = MkTxt(hdr, "Steps", "10 acc.", Color.white, 21,
                          V(0.74f,0.12f), V(0.88f,0.88f));
        _stepsLbl.fontStyle = FontStyles.Bold;
        _stepsLbl.alignment = TextAlignmentOptions.MidlineRight;

        _scoreLbl = MkTxt(hdr, "Score", "0 pts", ACCENT, 25,
                          V(0.88f,0.12f), V(0.99f,0.88f));
        _scoreLbl.fontStyle = FontStyles.Bold;
        _scoreLbl.alignment = TextAlignmentOptions.MidlineRight;

        // ── Barra emocional ──────────────────────────────────────────────
        BuildEmotionBar(R);

        // ── Barra de pasos restantes ─────────────────────────────────────
        BuildStepsBar(R);

        // ── Feedback label ───────────────────────────────────────────────
        _feedbackLbl = MkTxt(R, "Feedback",
                             "El nivel sube +8 cada turno automaticamente. Elige bien.",
                             DIM, 19, V(0.06f,0.432f), V(0.94f,0.470f));
        _feedbackLbl.enableAutoSizing = true;
        _feedbackLbl.fontSizeMin = 13f; _feedbackLbl.fontSizeMax = 19f;
        _feedbackLbl.overflowMode = TextOverflowModes.Overflow;

        // ── Grid de acciones ─────────────────────────────────────────────
        BuildActionGrid(R);

        // ── Footer ──────────────────────────────────────────────────────
        var bot = MkImg(R, "Bot", HDR, V(0,0), V(1,0), V(0,40), V(0,80));
        MkImg(bot, "BotLine", ACCENT, V(0,1), V(1,1), V(0,-1.5f), V(0,3));
        MkTxt(bot, "Instr",
              "El nivel sube automaticamente cada turno  •  Las acciones tienen recarga de 2 turnos",
              C(ACCENT.r+0.08f, ACCENT.g-0.12f, ACCENT.b-0.10f, 1f),
              16, V(0.01f,0), V(0.78f,1)).alignment = TextAlignmentOptions.MidlineLeft;
        MkImg(bot, "Sep", C(1,1,1,0.10f), V(0.78f,0.1f), V(0.782f,0.9f), V(0,0), V(0,0));
        MkBtn(bot, "Menu", C(0.12f,0.20f,0.36f), V(0.80f,0.08f), V(0.99f,0.92f), onMenu);

        BuildResultPanel(R, onRestart, onMenu);
    }

    // ─────────────────────────────────────────────────────────────────────
    void BuildEmotionBar(RectTransform R)
    {
        MkTxt(R, "BarTitle", "NIVEL DE ACTIVACION EMOCIONAL", DIM, 17,
              V(0.06f,0.862f), V(0.75f,0.896f)).characterSpacing = 1.5f;

        // Indicador de regeneracion (siempre visible, esquina derecha del titulo)
        _regenLbl = MkTxt(R, "RegenLbl", "+8 / turno", CRED, 18,
                          V(0.75f,0.862f), V(0.94f,0.896f));
        _regenLbl.fontStyle = FontStyles.Bold;
        _regenLbl.alignment = TextAlignmentOptions.MidlineRight;

        var barBG = MkImg(R, "BarBG", C(0.04f,0.07f,0.14f),
                          V(0.06f,0.810f), V(0.94f,0.856f), V(0,0), V(0,0));
        MkImg(barBG, "BorderT", C(1,1,1,0.08f), V(0,1), V(1,1), V(0,-1), V(0,2));
        MkImg(barBG, "BorderB", C(0,0,0,0.25f), V(0,0), V(1,0), V(0, 1), V(0,2));

        var fillGO = new GameObject("BarFill");
        fillGO.transform.SetParent(barBG, false);
        var fRT = fillGO.AddComponent<RectTransform>();
        fRT.anchorMin = V(0,0); fRT.anchorMax = V(1,1);
        fRT.sizeDelta = V(0,0); fRT.anchoredPosition = V(0,0);
        _barFill            = fillGO.AddComponent<Image>();
        _barFill.color      = CRED;
        _barFill.type       = Image.Type.Filled;
        _barFill.fillMethod = Image.FillMethod.Horizontal;
        _barFill.fillOrigin = 0;
        _barFill.fillAmount = 1f;
        MkImg(barBG, "Shine", C(1,1,1,0.10f), V(0,0.5f), V(1,1), V(0,0), V(0,0));

        _levelLbl = MkTxt(R, "LevelNum", "100", Color.white, 76,
                          V(0.38f,0.720f), V(0.62f,0.808f));
        _levelLbl.fontStyle = FontStyles.Bold;
        _levelLbl.alignment = TextAlignmentOptions.Center;

        _stateLbl = MkTxt(R, "StateLabel", "MUY ALTERADO", CRED, 22,
                          V(0.06f,0.720f), V(0.38f,0.808f));
        _stateLbl.fontStyle = FontStyles.Bold;
        _stateLbl.alignment = TextAlignmentOptions.MidlineRight;

        // Marcadores de referencia en la barra (0, 25, 50, 75, 100)
        for (int i = 0; i <= 4; i++)
        {
            float frac = i / 4f;
            int   val  = Mathf.RoundToInt(frac * 100);
            MkTxt(R, "Tick_" + val, val.ToString(), DIM, 13,
                  V(0.06f + frac*0.88f - 0.02f, 0.795f),
                  V(0.06f + frac*0.88f + 0.02f, 0.808f)).alignment = TextAlignmentOptions.Center;
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    void BuildStepsBar(RectTransform R)
    {
        MkTxt(R, "StepsTitle", "ACCIONES RESTANTES", DIM, 14,
              V(0.06f,0.700f), V(0.42f,0.716f)).alignment = TextAlignmentOptions.MidlineLeft;

        var sbBG = MkImg(R, "StepsBG", C(0.04f,0.07f,0.14f),
                         V(0.06f,0.683f), V(0.94f,0.700f), V(0,0), V(0,0));

        var sfGO = new GameObject("StepsFill");
        sfGO.transform.SetParent(sbBG, false);
        var sfRT = sfGO.AddComponent<RectTransform>();
        sfRT.anchorMin = V(0,0); sfRT.anchorMax = V(1,1);
        sfRT.sizeDelta = V(0,0); sfRT.anchoredPosition = V(0,0);
        _stepsFill            = sfGO.AddComponent<Image>();
        _stepsFill.color      = ACCENT;
        _stepsFill.type       = Image.Type.Filled;
        _stepsFill.fillMethod = Image.FillMethod.Horizontal;
        _stepsFill.fillOrigin = 0;
        _stepsFill.fillAmount = 1f;
        MkImg(sbBG, "Shine", C(1,1,1,0.10f), V(0,0.5f), V(1,1), V(0,0), V(0,0));

        _stepsBarLbl = MkTxt(R, "StepsBarLbl", "10 / 10", ACCENT, 16,
                             V(0.62f,0.683f), V(0.94f,0.700f));
        _stepsBarLbl.fontStyle = FontStyles.Bold;
        _stepsBarLbl.alignment = TextAlignmentOptions.MidlineRight;
    }

    // ─────────────────────────────────────────────────────────────────────
    void BuildActionGrid(RectTransform R)
    {
        int n = RegulationEmotionManager.ACTIONS.Length;
        _actionBtns      = new Button[n];
        _actionBtnImgs   = new Image[n];
        _actionCDOverlay = new Image[n];
        _actionCDLbl     = new TextMeshProUGUI[n];

        MkTxt(R, "ActTitle", "ACCIONES  (recarga: 2 turnos)", DIM, 15,
              V(0.06f,0.414f), V(0.94f,0.430f)).characterSpacing = 0.8f;

        float colAMin = 0.06f, colAMax = 0.47f;
        float colBMin = 0.53f, colBMax = 0.94f;
        float[] rowMins = { 0.310f, 0.198f, 0.086f };
        float[] rowMaxs = { 0.408f, 0.296f, 0.184f };

        for (int i = 0; i < n; i++)
        {
            bool  colB = (i % 2) == 1;
            int   row  = i / 2;
            float xMin = colB ? colBMin : colAMin;
            float xMax = colB ? colBMax : colAMax;
            int capturedIdx = i;
            var action = RegulationEmotionManager.ACTIONS[i];

            var btnRT = MkImg(R, "Btn_" + i, BTN_N,
                              V(xMin, rowMins[row]), V(xMax, rowMaxs[row]), V(0,0), V(0,0));
            MkImg(btnRT, "Sh",   C(1,1,1,0.06f),                      V(0,0.5f),  V(1,1),     V(0,0), V(0,0));
            MkImg(btnRT, "AccL", C(ACCENT.r,ACCENT.g,ACCENT.b,0.55f), V(0,0.12f), V(0,0.88f), V(4,0), V(8,0));

            // Etiqueta de impacto (neto = impacto + regen)
            string impStr = (action.impact >= 0 ? "+" : "") + action.impact;
            Color  impCol = action.impact <= -16 ? CGREEN
                          : action.impact <= -12 ? CYELLOW
                          : CRED;
            var impLbl = MkTxt(btnRT, "Imp", impStr, impCol, 20,
                               V(0.68f,0.55f), V(0.98f,0.98f));
            impLbl.fontStyle = FontStyles.Bold;
            impLbl.alignment = TextAlignmentOptions.TopRight;

            var txt = MkTxt(btnRT, "T", action.name, Color.white, 21,
                            V(0.05f,0.08f), V(0.88f,0.92f));
            txt.alignment        = TextAlignmentOptions.MidlineLeft;
            txt.enableAutoSizing = true;
            txt.fontSizeMin = 13f; txt.fontSizeMax = 21f;

            // Overlay de cooldown
            var cdOvGO = new GameObject("CDOverlay");
            cdOvGO.transform.SetParent(btnRT, false);
            var cdRT = cdOvGO.AddComponent<RectTransform>();
            cdRT.anchorMin = V(0,0); cdRT.anchorMax = V(1,1);
            cdRT.sizeDelta = V(0,0); cdRT.anchoredPosition = V(0,0);
            var cdImg = cdOvGO.AddComponent<Image>();
            cdImg.color = C(0f,0f,0f,0.65f);
            cdOvGO.SetActive(false);
            _actionCDOverlay[i] = cdImg;

            var cdLbl = MkTxt(cdRT, "CDLbl", "2", C(1f,1f,1f,0.55f), 38,
                              V(0,0), V(1,1));
            cdLbl.fontStyle = FontStyles.Bold;
            _actionCDLbl[i] = cdLbl;

            _actionBtnImgs[i] = btnRT.GetComponent<Image>();
            var btn = btnRT.gameObject.AddComponent<Button>();
            btn.targetGraphic = _actionBtnImgs[i];
            var bc = btn.colors;
            bc.normalColor      = Color.white;
            bc.highlightedColor = C(1.18f,1.18f,1.18f,1f);
            bc.pressedColor     = C(0.72f,0.72f,0.72f);
            bc.disabledColor    = C(0.35f,0.35f,0.35f,0.55f);
            btn.colors = bc;
            btn.onClick.AddListener(() => _onAction?.Invoke(capturedIdx));
            _actionBtns[i] = btn;
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    void BuildResultPanel(RectTransform R, Action onRestart, Action onMenu)
    {
        _resultPanel = new GameObject("ResultPanel");
        _resultPanel.transform.SetParent(R, false);
        var er = _resultPanel.AddComponent<RectTransform>();
        er.anchorMin = V(0,0); er.anchorMax = V(1,1);
        er.sizeDelta = V(0,0); er.anchoredPosition = V(0,0);
        _resultPanel.AddComponent<Image>().color = C(0,0,0,0.88f);

        var card = MkImg(er, "Card", PANEL, V(0.5f,0.5f), V(0.5f,0.5f), V(0,0), V(900f,480f));
        MkImg(card, "Sh",    C(1,1,1,0.03f), V(0,0.5f),  V(1,1),     V(0,0),  V(0,0));
        MkImg(card, "LineT", ACCENT,          V(0,1),     V(1,1),     V(0,-4), V(0,8));
        MkImg(card, "AccL",  ACCENT,          V(0,0.08f), V(0,0.92f), V(4,0),  V(8,0));

        _resultTitle = MkTxt(card, "RT", "", Color.white, 42,
                             V(0.05f,0.76f), V(0.95f,0.97f));
        _resultTitle.fontStyle = FontStyles.Bold;
        _resultTitle.enableAutoSizing = true;
        _resultTitle.fontSizeMin = 24f; _resultTitle.fontSizeMax = 46f;

        _resultSub = MkTxt(card, "RS", "", C(0.50f,0.68f,0.80f), 21,
                           V(0.05f,0.22f), V(0.95f,0.74f));
        _resultSub.overflowMode = TextOverflowModes.Overflow;
        _resultSub.alignment    = TextAlignmentOptions.Center;
        _resultSub.lineSpacing  = 10f;

        MkBtn(card, "Jugar de nuevo", ACCENT,              V(0.05f,0.04f), V(0.46f,0.17f), onRestart);
        MkBtn(card, "Menu",          C(0.14f,0.22f,0.38f), V(0.54f,0.04f), V(0.95f,0.17f), onMenu);

        _resultPanel.SetActive(false);
    }

    // ═════════════════════════════════════════════════════════════════════
    // API publica
    // ═════════════════════════════════════════════════════════════════════

    public void UpdateBar(float level, int stepsTaken, int maxSteps, float regenPerTurn)
    {
        // La barra muestra 0-100 (llena si >= 100); el numero muestra el valor real
        float displayFrac = Mathf.Clamp01(level / 100f);
        _barFill.fillAmount = displayFrac;

        if (level > 100f)
        {
            // Sobrecarga: rojo muy oscuro para indicar que se ha pasado del limite
            _barFill.color = CDARK;
            _stateLbl.text  = "SOBRECARGADO"; _stateLbl.color = CDARK;
            _levelLbl.color = CRED;
        }
        else if (level > 65f)
        {
            _barFill.color = CRED;
            _stateLbl.text  = "MUY ALTERADO"; _stateLbl.color = CRED;
            _levelLbl.color = Color.white;
        }
        else if (level > 35f)
        {
            _barFill.color = CYELLOW;
            _stateLbl.text  = "PROGRESANDO";  _stateLbl.color = CYELLOW;
            _levelLbl.color = Color.white;
        }
        else
        {
            _barFill.color = CGREEN;
            _stateLbl.text  = level <= 10f ? "EN CALMA" : "CASI EN CALMA";
            _stateLbl.color = CGREEN;
            _levelLbl.color = CGREEN;
        }

        _levelLbl.text  = Mathf.RoundToInt(level).ToString();
        _regenLbl.text  = "+" + Mathf.RoundToInt(regenPerTurn) + " / turno";

        // Barra de pasos
        int   remaining = maxSteps - stepsTaken;
        float stepFrac  = maxSteps > 0 ? (float)remaining / maxSteps : 0f;
        _stepsFill.fillAmount = Mathf.Clamp01(stepFrac);

        Color stepCol = remaining <= 2 ? CRED : remaining <= 4 ? CYELLOW : ACCENT;
        _stepsFill.color    = stepCol;
        _stepsBarLbl.color  = stepCol;
        _stepsLbl.color     = remaining <= 2 ? CRED : remaining <= 4 ? CYELLOW : Color.white;
        _stepsBarLbl.text   = remaining + " / " + maxSteps;
        _stepsLbl.text      = remaining + " acc.";
    }

    public void UpdateScore(int score)
    {
        if (_scoreLbl) _scoreLbl.text = score + " pts";
    }

    public void UpdateButtonCooldowns(int[] cooldowns)
    {
        if (_actionBtns == null) return;
        for (int i = 0; i < _actionBtns.Length && i < cooldowns.Length; i++)
        {
            bool avail = cooldowns[i] <= 0;
            _actionBtns[i].interactable          = avail;
            _actionBtnImgs[i].color              = avail ? BTN_N : BTN_CD;
            _actionCDOverlay[i].gameObject.SetActive(!avail);
            if (!avail) _actionCDLbl[i].text = cooldowns[i].ToString();
        }
    }

    public void ShowFeedback(RegulationEmotionManager.EmotionAction action,
                             float newLevel, float regen)
    {
        int netEffect = action.impact + Mathf.RoundToInt(regen);
        string netStr = (netEffect >= 0 ? "+" : "") + netEffect;

        string text = (action.impact < 0 ? action.feedbackGood : action.feedbackBad)
                    + "\n[Efecto neto este turno: " + netStr + "]";

        Color col = action.impact <= -16 ? ACCENT
                  : action.impact <= -12 ? CYELLOW
                  : CRED;

        _feedbackLbl.text  = text;
        _feedbackLbl.color = col;
    }

    public void ShowResult(bool won, int steps, int score, int finalLevel)
    {
        if (won)
        {
            _resultTitle.text  = "Nivel emocional regulado";
            _resultTitle.color = CGREEN;
            _resultSub.text    =
                "Nivel final:   " + finalLevel + "\n" +
                "Acciones usadas:   " + steps + "\n" +
                "Puntuacion:   " + score + " pts\n\n" +
                (steps <= 9
                    ? "Excelente regulacion. Dominaste la tension automatica."
                    : "Lo conseguiste. Con practica podras hacerlo en menos pasos.");
        }
        else
        {
            _resultTitle.text  = "Se agotaron las acciones";
            _resultTitle.color = CRED;
            _resultSub.text    =
                "Nivel final:   " + finalLevel + "  (objetivo: 10 o menos)\n\n" +
                "Recuerda: el nivel sube automaticamente +8 cada turno.\n" +
                "Solo Respirar, Hablar y Caminar tienen efecto neto negativo.\n" +
                "Ignorar y Reaccionar con ira empeoran la situacion.";
        }
        _resultPanel.SetActive(true);
    }

    // ═════════════════════════════════════════════════════════════════════
    RectTransform MkImg(RectTransform p, string n, Color col,
                        Vector2 am, Vector2 aM, Vector2 pos, Vector2 sd)
    {
        var go = new GameObject(n);
        go.transform.SetParent(p, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = am; rt.anchorMax = aM;
        rt.pivot     = V(0.5f, 0.5f);
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
        rt.pivot     = V(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero; rt.sizeDelta = Vector2.zero;
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = txt; t.color = col; t.fontSize = sz;
        t.alignment = TextAlignmentOptions.Center;
        t.overflowMode = TextOverflowModes.Overflow;
        return t;
    }

    void MkBtn(RectTransform p, string lbl, Color bg, Vector2 am, Vector2 aM, Action click)
    {
        var rt = MkImg(p, "Btn_" + lbl, bg, am, aM, V(0,0), V(0,0));
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
