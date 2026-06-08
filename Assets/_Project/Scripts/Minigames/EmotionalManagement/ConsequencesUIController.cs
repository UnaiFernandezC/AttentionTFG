using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ConsequencesUIController : MonoBehaviour
{

    static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);
    static Vector2 V(float x, float y) => new Vector2(x, y);

    static readonly Color BG       = C(0.05f, 0.08f, 0.13f);
    static readonly Color HDR      = C(0.03f, 0.06f, 0.11f);
    static readonly Color PANEL    = C(0.07f, 0.11f, 0.20f);
    static readonly Color ACCENT   = C(0.18f, 0.80f, 0.58f);
    static readonly Color DIM      = C(0.38f, 0.54f, 0.62f);
    static readonly Color SIT_BG   = C(0.07f, 0.12f, 0.22f);
    static readonly Color BTN_IDLE = C(0.10f, 0.16f, 0.27f);
    static readonly Color CGREEN   = C(0.22f, 0.86f, 0.54f);
    static readonly Color CYELLOW  = C(0.96f, 0.82f, 0.20f);
    static readonly Color CRED     = C(0.90f, 0.28f, 0.30f);

    TextMeshProUGUI _roundLbl;
    TextMeshProUGUI _scoreLbl;
    TextMeshProUGUI _phaseLbl;
    TextMeshProUGUI _situationTxt;

    RectTransform   _optionsContainer;
    RectTransform   _consequencePanel;
    Image           _qualityBar;
    TextMeshProUGUI _qualityLbl;
    TextMeshProUGUI _consequenceTxt;
    TextMeshProUGUI _nextBtnTxt;

    GameObject      _resultPanel;
    TextMeshProUGUI _resultTitle;
    TextMeshProUGUI _resultSub;

    Action<int>     _onOptionChosen;
    Action          _onNext;

    public void BuildUI(Action<int> onOptionChosen, Action onNext,
                        Action onRestart, Action onMenu)
    {
        _onOptionChosen = onOptionChosen;
        _onNext         = onNext;

        var cGO = new GameObject("Canvas_Consequences");
        cGO.transform.SetParent(transform, false);
        var cv = cGO.AddComponent<Canvas>();
        cv.renderMode   = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 5;
        var sc = cGO.AddComponent<CanvasScaler>();
        sc.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920f, 1080f);
        sc.matchWidthOrHeight  = 0.5f;
        cGO.AddComponent<GraphicRaycaster>();
        var R = cGO.GetComponent<RectTransform>();

        MkImg(R, "BG",    BG,                             V(0, 0),     V(1, 1),    V(0,0), V(0,0));
        MkImg(R, "GradT", C(0.04f, 0.18f, 0.14f, 0.18f), V(0, 0.50f), V(1, 1),    V(0,0), V(0,0));
        MkImg(R, "GradB", C(0.02f, 0.04f, 0.08f, 0.28f), V(0, 0),     V(1, 0.30f),V(0,0), V(0,0));

        var hdr = MkImg(R, "Hdr", HDR, V(0,1), V(1,1), V(0,-44), V(0,88));
        MkImg(hdr, "Line", ACCENT, V(0,0),     V(1,0),     V(0, 1.5f), V(0,3));
        MkImg(hdr, "AccL", ACCENT, V(0,0.18f), V(0,0.82f), V(3, 0),    V(6,0));

        var ttl = MkTxt(hdr, "T", "CONSECUENCIAS EMOCIONALES", Color.white, 30,
                        V(0.03f, 0.12f), V(0.53f, 0.88f));
        ttl.fontStyle = FontStyles.Bold;
        ttl.alignment = TextAlignmentOptions.MidlineLeft;
        ttl.characterSpacing = 1.5f;

        MkTxt(hdr, "Cat", "GESTION EMOCIONAL", DIM, 15,
              V(0.53f, 0.12f), V(0.72f, 0.88f)).alignment = TextAlignmentOptions.MidlineRight;

        _roundLbl = MkTxt(hdr, "Round", "Situacion 1/5", Color.white, 21,
                          V(0.72f, 0.12f), V(0.87f, 0.88f));
        _roundLbl.fontStyle = FontStyles.Bold;
        _roundLbl.alignment = TextAlignmentOptions.MidlineRight;

        _scoreLbl = MkTxt(hdr, "Score", "0 pts", ACCENT, 25,
                          V(0.87f, 0.12f), V(0.99f, 0.88f));
        _scoreLbl.fontStyle = FontStyles.Bold;
        _scoreLbl.alignment = TextAlignmentOptions.MidlineRight;

        _phaseLbl = MkTxt(R, "Phase", "Elige la mejor reaccion posible",
                          DIM, 20, V(0.08f, 0.882f), V(0.92f, 0.918f));

        var sitCard = MkImg(R, "SitCard", SIT_BG,
                            V(0.06f, 0.668f), V(0.94f, 0.878f), V(0,0), V(0,0));
        MkImg(sitCard, "AccT", ACCENT, V(0,1),      V(1,1),      V(0,-2.5f), V(0,5f));
        MkImg(sitCard, "AccL", ACCENT, V(0,0.10f),  V(0,0.90f),  V(4, 0),    V(8,0));
        MkImg(sitCard, "Sh",   C(1,1,1,0.035f), V(0,0.5f), V(1,1), V(0,0),  V(0,0));

        _situationTxt = MkTxt(sitCard, "SitTxt", "", Color.white, 28,
                              V(0.04f, 0.08f), V(0.96f, 0.92f));
        _situationTxt.alignment      = TextAlignmentOptions.MidlineLeft;
        _situationTxt.enableAutoSizing = true;
        _situationTxt.fontSizeMin    = 18f;
        _situationTxt.fontSizeMax    = 30f;
        _situationTxt.overflowMode   = TextOverflowModes.Overflow;

        var optGO = new GameObject("OptionsContainer");
        optGO.transform.SetParent(R, false);
        _optionsContainer = optGO.AddComponent<RectTransform>();
        _optionsContainer.anchorMin        = V(0.06f, 0.095f);
        _optionsContainer.anchorMax        = V(0.94f, 0.660f);
        _optionsContainer.sizeDelta        = Vector2.zero;
        _optionsContainer.anchoredPosition = Vector2.zero;
        optGO.AddComponent<Image>().color          = Color.clear;
        optGO.GetComponent<Image>().raycastTarget  = false;

        BuildConsequencePanel(R);

        var bot = MkImg(R, "Bot", HDR, V(0,0), V(1,0), V(0,40), V(0,80));
        MkImg(bot, "BotLine", ACCENT, V(0,1), V(1,1), V(0,-1.5f), V(0,3));
        MkTxt(bot, "Instr", "Observa la situacion y selecciona la reaccion mas adecuada",
              C(ACCENT.r + 0.08f, ACCENT.g - 0.12f, ACCENT.b - 0.10f, 1f),
              18, V(0.01f, 0), V(0.78f, 1)).alignment = TextAlignmentOptions.MidlineLeft;
        MkImg(bot, "Sep", C(1,1,1,0.10f), V(0.78f, 0.1f), V(0.782f, 0.9f), V(0,0), V(0,0));

        BuildResultPanel(R, onRestart, onMenu);
    }

    void BuildConsequencePanel(RectTransform R)
    {

        var conGO = new GameObject("ConsequencePanel");
        conGO.transform.SetParent(R, false);
        var conRT = conGO.AddComponent<RectTransform>();
        conRT.anchorMin        = V(0.06f, 0.095f);
        conRT.anchorMax        = V(0.94f, 0.660f);
        conRT.sizeDelta        = Vector2.zero;
        conRT.anchoredPosition = Vector2.zero;
        conGO.AddComponent<Image>().color = PANEL;
        MkImg(conRT, "Sh", C(1,1,1,0.03f), V(0,0.5f), V(1,1), V(0,0), V(0,0));

        var qBarGO = new GameObject("QualityBar");
        qBarGO.transform.SetParent(conRT, false);
        var qBarRT = qBarGO.AddComponent<RectTransform>();
        qBarRT.anchorMin        = V(0, 1);
        qBarRT.anchorMax        = V(1, 1);
        qBarRT.sizeDelta        = V(0, 56);
        qBarRT.anchoredPosition = V(0, -28);
        _qualityBar             = qBarGO.AddComponent<Image>();
        _qualityBar.color       = ACCENT;

        _qualityLbl = MkTxt(qBarRT, "QL", "RESPUESTA ADECUADA",
                            Color.white, 24, V(0,0), V(1,1));
        _qualityLbl.fontStyle       = FontStyles.Bold;
        _qualityLbl.characterSpacing = 2f;

        _consequenceTxt = MkTxt(conRT, "CTxt", "",
            C(0.82f, 0.92f, 0.98f), 26,
            V(0.05f, 0.22f), V(0.95f, 0.88f));
        _consequenceTxt.alignment      = TextAlignmentOptions.Center;
        _consequenceTxt.enableAutoSizing = true;
        _consequenceTxt.fontSizeMin    = 18f;
        _consequenceTxt.fontSizeMax    = 28f;
        _consequenceTxt.overflowMode   = TextOverflowModes.Overflow;
        _consequenceTxt.lineSpacing    = 8f;

        var nextRT = MkImg(conRT, "NextBtn", ACCENT,
                           V(0.25f, 0.04f), V(0.75f, 0.18f), V(0,0), V(0,0));
        MkImg(nextRT, "Sh", C(1,1,1,0.13f), V(0,0.5f), V(1,1), V(0,0), V(0,0));
        var nextBtn = nextRT.gameObject.AddComponent<Button>();
        nextBtn.targetGraphic = nextRT.GetComponent<Image>();
        var cb = nextBtn.colors;
        cb.normalColor      = Color.white;
        cb.highlightedColor = C(1,1,1,0.85f);
        cb.pressedColor     = C(0.72f,0.72f,0.72f);
        nextBtn.colors = cb;
        nextBtn.onClick.AddListener(() => _onNext?.Invoke());
        _nextBtnTxt = MkTxt(nextRT, "T", "Siguiente situacion", Color.white, 26, V(0,0), V(1,1));
        _nextBtnTxt.fontStyle = FontStyles.Bold;

        conGO.SetActive(false);
        _consequencePanel = conRT;
    }

    void BuildResultPanel(RectTransform R, Action onRestart, Action onMenu)
    {
        _resultPanel = new GameObject("ResultPanel");
        _resultPanel.transform.SetParent(R, false);
        var er = _resultPanel.AddComponent<RectTransform>();
        er.anchorMin = V(0,0); er.anchorMax = V(1,1);
        er.sizeDelta = V(0,0); er.anchoredPosition = V(0,0);
        _resultPanel.AddComponent<Image>().color = C(0,0,0,0.88f);

        var card = MkImg(er, "Card", PANEL, V(0.5f,0.5f), V(0.5f,0.5f), V(0,0), V(880f,480f));
        MkImg(card, "Sh",    C(1,1,1,0.03f), V(0,0.5f),  V(1,1),     V(0,0),  V(0,0));
        MkImg(card, "LineT", ACCENT,          V(0,1),     V(1,1),     V(0,-4), V(0,8));
        MkImg(card, "AccL",  ACCENT,          V(0,0.08f), V(0,0.92f), V(4,0),  V(8,0));

        _resultTitle = MkTxt(card, "RT", "", Color.white, 46, V(0.05f,0.76f), V(0.95f,0.97f));
        _resultTitle.fontStyle = FontStyles.Bold;

        _resultSub = MkTxt(card, "RS", "", C(0.50f, 0.68f, 0.80f), 22,
                           V(0.05f, 0.22f), V(0.95f, 0.74f));
        _resultSub.overflowMode = TextOverflowModes.Overflow;
        _resultSub.alignment    = TextAlignmentOptions.Center;
        _resultSub.lineSpacing  = 10f;

        MkBtn(card, "Jugar de nuevo", ACCENT,               V(0.05f,0.04f), V(0.95f,0.17f), onRestart);

        _resultPanel.SetActive(false);
    }

    public void UpdateRound(int current, int total)
    {
        if (_roundLbl) _roundLbl.text = "Situacion " + current + "/" + total;
    }

    public void UpdateScore(int score)
    {
        if (_scoreLbl) _scoreLbl.text = score + " pts";
    }

    public void ShowSituation(EmotionalSituation sit, int current, int total)
    {
        _optionsContainer.gameObject.SetActive(true);
        _consequencePanel.gameObject.SetActive(false);

        _situationTxt.text = sit.situation;
        UpdateRound(current, total);
        _phaseLbl.text = "Elige la mejor reaccion posible";

        foreach (Transform ch in _optionsContainer)
            Destroy(ch.gameObject);

        int   count = sit.options.Length;
        float gap   = 12f;
        float btnH  = count <= 2 ? 160f : count == 3 ? 142f : 118f;
        float totalH = count * btnH + (count - 1) * gap;
        float startY = totalH * 0.5f - btnH * 0.5f;

        for (int i = 0; i < count; i++)
        {
            var opt          = sit.options[i];
            int capturedIdx  = i;
            float y          = startY - i * (btnH + gap);

            var btnGO = new GameObject("Opt_" + i);
            btnGO.transform.SetParent(_optionsContainer, false);
            var btnRT = btnGO.AddComponent<RectTransform>();
            btnRT.anchorMin        = V(0, 0.5f);
            btnRT.anchorMax        = V(1, 0.5f);
            btnRT.pivot            = V(0.5f, 0.5f);
            btnRT.sizeDelta        = new Vector2(0, btnH);
            btnRT.anchoredPosition = new Vector2(0, y);

            var img   = btnGO.AddComponent<Image>();
            img.color = BTN_IDLE;

            var accGO = new GameObject("Acc");
            accGO.transform.SetParent(btnRT, false);
            var accRT = accGO.AddComponent<RectTransform>();
            accRT.anchorMin        = V(0, 0.15f);
            accRT.anchorMax        = V(0, 0.85f);
            accRT.sizeDelta        = V(6, 0);
            accRT.anchoredPosition = V(3, 0);
            accGO.AddComponent<Image>().color = C(ACCENT.r, ACCENT.g, ACCENT.b, 0.55f);

            var shGO = new GameObject("Sh");
            shGO.transform.SetParent(btnRT, false);
            var shRT = shGO.AddComponent<RectTransform>();
            shRT.anchorMin = V(0, 0.5f); shRT.anchorMax = V(1, 1);
            shRT.sizeDelta = V(0, 0); shRT.anchoredPosition = V(0, 0);
            shGO.AddComponent<Image>().color = C(1, 1, 1, 0.05f);

            var txt = MkTxt(btnRT, "T", opt.text, Color.white, 24,
                            V(0.03f, 0), V(0.97f, 1));
            txt.alignment      = TextAlignmentOptions.MidlineLeft;
            txt.enableAutoSizing = true;
            txt.fontSizeMin    = 16f;
            txt.fontSizeMax    = 26f;

            var btn = btnGO.AddComponent<Button>();
            btn.targetGraphic = img;
            var cb = btn.colors;
            cb.normalColor      = Color.white;
            cb.highlightedColor = C(1, 1, 1, 0.85f);
            cb.pressedColor     = C(0.72f, 0.72f, 0.72f);
            btn.colors = cb;
            btn.onClick.AddListener(() => _onOptionChosen?.Invoke(capturedIdx));
        }
    }

    public void ShowConsequence(SituationOption chosen, bool hasNext)
    {
        _optionsContainer.gameObject.SetActive(false);
        _consequencePanel.gameObject.SetActive(true);

        Color  qualCol;
        string qualTxt;

        switch (chosen.quality)
        {
            case AnswerQuality.Positive:
                qualCol = CGREEN;
                qualTxt = "RESPUESTA ADECUADA  ·  +20 pts";
                break;
            case AnswerQuality.Neutral:
                qualCol = CYELLOW;
                qualTxt = "RESPUESTA NEUTRA  ·  +8 pts";
                break;
            default:
                qualCol = CRED;
                qualTxt = "RESPUESTA POCO ADECUADA  ·  0 pts";
                break;
        }

        _qualityBar.color  = qualCol;
        _qualityLbl.text   = qualTxt;
        _consequenceTxt.text = chosen.consequence;

        _nextBtnTxt.text = hasNext ? "Siguiente situacion  →" : "Ver resultado  →";
        _phaseLbl.text   = "Resultado de tu decision:";
    }

    public void ShowFinalResult(bool win, int positive, int total, int score)
    {
        string title    = win ? "¡Buen manejo emocional!" : "Hay margen de mejora";
        Color titleCol  = win ? CGREEN : CYELLOW;

        string advice = win
            ? "Sabes identificar respuestas constructivas. ¡Sigue cultivando esa inteligencia emocional!"
            : "Recuerda: responder con calma y dialogar siempre da mejores resultados a largo plazo.";

        string sub =
            "Decisiones adecuadas:   " + positive + " / " + total + "\n" +
            "Puntuacion total:   " + score + " pts\n\n" +
            advice;

        _resultTitle.text  = title;
        _resultTitle.color = titleCol;
        _resultSub.text    = sub;
        _resultPanel.SetActive(true);
    }

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
        t.text      = txt;
        t.color     = col;
        t.fontSize  = sz;
        t.alignment = TextAlignmentOptions.Center;
        t.overflowMode = TextOverflowModes.Overflow;
        return t;
    }

    void MkBtn(RectTransform p, string lbl, Color bg,
               Vector2 am, Vector2 aM, Action click)
    {
        var rt = MkImg(p, "Btn_" + lbl, bg, am, aM, V(0,0), V(0,0));
        MkImg(rt, "Sh", C(1,1,1,0.09f), V(0,0.5f), V(1,1), V(0,0), V(0,0));
        var b = rt.gameObject.AddComponent<Button>();
        b.targetGraphic = rt.GetComponent<Image>();
        var cb = b.colors;
        cb.normalColor      = Color.white;
        cb.highlightedColor = C(1,1,1,0.82f);
        cb.pressedColor     = C(0.72f,0.72f,0.72f);
        b.colors = cb;
        b.onClick.AddListener(() => click?.Invoke());
        var t = MkTxt(rt, "T", lbl, Color.white, 24, V(0,0), V(1,1));
        t.fontStyle = FontStyles.Bold;
    }
}
