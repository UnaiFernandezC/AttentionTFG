using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WordMemoryUIController : MonoBehaviour
{

    static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);
    static Vector2 V(float x, float y) => new Vector2(x, y);

    static readonly Color BG        = C(0.05f, 0.07f, 0.13f);
    static readonly Color HDR       = C(0.04f, 0.05f, 0.11f);
    static readonly Color PANEL     = C(0.08f, 0.11f, 0.20f);
    static readonly Color ACCENT    = C(0.58f, 0.28f, 0.92f);
    static readonly Color DIM       = C(0.40f, 0.48f, 0.68f);
    static readonly Color WORD_BG   = C(0.11f, 0.14f, 0.24f);
    static readonly Color BTN_IDLE  = C(0.13f, 0.17f, 0.28f);
    static readonly Color BTN_SEL   = C(0.40f, 0.20f, 0.72f);
    static readonly Color CGREEN    = C(0.25f, 0.90f, 0.52f);
    static readonly Color CRED      = C(0.90f, 0.28f, 0.30f);
    static readonly Color CORANGE   = C(0.96f, 0.62f, 0.18f);

    TextMeshProUGUI _phaseLbl;
    TextMeshProUGUI _infoLbl;
    TextMeshProUGUI _roundLbl;
    TextMeshProUGUI _scoreLbl;
    Image           _countdownFill;

    RectTransform   _memorizePanel;

    RectTransform   _choosePanel;
    List<Image>     _wordBtnsImg;
    List<Button>    _wordBtns;
    List<bool>      _wordSelected;
    List<string>    _wordLabels;

    GameObject      _confirmBtnGO;
    GameObject      _resultPanel;
    TextMeshProUGUI _resultTitle;
    TextMeshProUGUI _resultSub;

    Action<int>     _onWordToggled;
    Action          _onConfirm;

    public void BuildUI(Action<int> onWordToggled, Action onConfirm,
                        Action onRestart, Action onMenu)
    {
        _onWordToggled = onWordToggled;
        _onConfirm     = onConfirm;

        var cGO = new GameObject("Canvas_WordMemory");
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

        MkImg(R, "BG",    BG,                             V(0, 0),     V(1, 1),  V(0,0), V(0,0));
        MkImg(R, "GradT", C(0.16f, 0.06f, 0.28f, 0.18f), V(0, 0.55f), V(1, 1),  V(0,0), V(0,0));
        MkImg(R, "GradB", C(0.02f, 0.04f, 0.10f, 0.30f), V(0, 0),     V(1, 0.3f), V(0,0), V(0,0));

        var hdr = MkImg(R, "Hdr", HDR, V(0,1), V(1,1), V(0,-44), V(0,88));
        MkImg(hdr, "Line", ACCENT, V(0,0),     V(1,0),     V(0, 1.5f), V(0,3));
        MkImg(hdr, "AccL", ACCENT, V(0,0.18f), V(0,0.82f), V(3, 0),    V(6,0));

        var ttl = MkTxt(hdr, "T", "PALABRAS FUGACES", Color.white, 34,
                        V(0.03f, 0.12f), V(0.52f, 0.88f));
        ttl.fontStyle = FontStyles.Bold;
        ttl.alignment = TextAlignmentOptions.MidlineLeft;
        ttl.characterSpacing = 2f;

        MkTxt(hdr, "Cat", "MEMORIA", DIM, 16,
              V(0.52f, 0.12f), V(0.70f, 0.88f)).alignment = TextAlignmentOptions.MidlineRight;

        _roundLbl = MkTxt(hdr, "Round", "Ronda 1/3", Color.white, 22,
                          V(0.70f, 0.12f), V(0.86f, 0.88f));
        _roundLbl.fontStyle = FontStyles.Bold;
        _roundLbl.alignment = TextAlignmentOptions.MidlineRight;

        _scoreLbl = MkTxt(hdr, "Score", "0 pts", ACCENT, 26,
                          V(0.86f, 0.12f), V(0.99f, 0.88f));
        _scoreLbl.fontStyle = FontStyles.Bold;
        _scoreLbl.alignment = TextAlignmentOptions.MidlineRight;

        _phaseLbl = MkTxt(R, "Phase", "", ACCENT, 38, V(0.1f, 0.862f), V(0.9f, 0.930f));
        _phaseLbl.fontStyle = FontStyles.Bold;

        _infoLbl = MkTxt(R, "Info", "", DIM, 21, V(0.1f, 0.806f), V(0.9f, 0.862f));

        var cdBg = MkImg(R, "CdBg", C(0.04f, 0.06f, 0.12f),
                         V(0, 0.790f), V(1, 0.806f), V(0,0), V(0,0));
        MkImg(cdBg, "CdShine", C(1,1,1,0.04f), V(0,0.55f), V(1,1), V(0,0), V(0,0));

        var cfGO = new GameObject("CdFill");
        cfGO.transform.SetParent(cdBg, false);
        var cfRT = cfGO.AddComponent<RectTransform>();
        cfRT.anchorMin = Vector2.zero; cfRT.anchorMax = Vector2.one;
        cfRT.sizeDelta = Vector2.zero; cfRT.anchoredPosition = Vector2.zero;
        _countdownFill = cfGO.AddComponent<Image>();
        _countdownFill.color      = ACCENT;
        _countdownFill.type       = Image.Type.Filled;
        _countdownFill.fillMethod = Image.FillMethod.Horizontal;
        _countdownFill.fillAmount = 1f;

        var memGO = new GameObject("MemorizePanel");
        memGO.transform.SetParent(R, false);
        _memorizePanel = memGO.AddComponent<RectTransform>();
        _memorizePanel.anchorMin        = V(0.18f, 0.12f);
        _memorizePanel.anchorMax        = V(0.82f, 0.785f);
        _memorizePanel.sizeDelta        = Vector2.zero;
        _memorizePanel.anchoredPosition = Vector2.zero;
        memGO.AddComponent<Image>().color          = Color.clear;
        memGO.GetComponent<Image>().raycastTarget  = false;

        var choGO = new GameObject("ChoosePanel");
        choGO.transform.SetParent(R, false);
        _choosePanel = choGO.AddComponent<RectTransform>();
        _choosePanel.anchorMin        = V(0.08f, 0.12f);
        _choosePanel.anchorMax        = V(0.92f, 0.785f);
        _choosePanel.sizeDelta        = Vector2.zero;
        _choosePanel.anchoredPosition = Vector2.zero;
        choGO.AddComponent<Image>().color         = Color.clear;
        choGO.GetComponent<Image>().raycastTarget = false;

        _confirmBtnGO = BuildConfirmBtn(R);
        _confirmBtnGO.SetActive(false);

        var bot = MkImg(R, "Bot", HDR, V(0,0), V(1,0), V(0,40), V(0,80));
        MkImg(bot, "BotLine", ACCENT, V(0,1), V(1,1), V(0,-1.5f), V(0,3));
        MkTxt(bot, "Instr", "Memoriza las palabras · Luego identifica cuales viste",
              C(ACCENT.r + 0.12f, ACCENT.g + 0.12f, ACCENT.b + 0.12f, 1f),
              19, V(0.01f, 0), V(0.78f, 1)).alignment = TextAlignmentOptions.MidlineLeft;
        MkImg(bot, "Sep", C(1,1,1,0.10f), V(0.78f, 0.1f), V(0.782f, 0.9f), V(0,0), V(0,0));

        BuildResultPanel(R, onRestart, onMenu);

        _memorizePanel.gameObject.SetActive(false);
        _choosePanel.gameObject.SetActive(false);
    }

    public void ShowMemorizePhase(List<string> targetWords)
    {

        foreach (Transform ch in _memorizePanel) Destroy(ch.gameObject);
        _choosePanel.gameObject.SetActive(false);
        _memorizePanel.gameObject.SetActive(true);
        if (_confirmBtnGO != null) _confirmBtnGO.SetActive(false);

        int count     = targetWords.Count;
        float cardH   = Mathf.Min(78f, (_memorizePanel.rect.height - (count - 1) * 10f) / count);
        float gap     = 10f;
        float totalH  = count * cardH + (count - 1) * gap;
        float startY  = totalH * 0.5f - cardH * 0.5f;

        for (int i = 0; i < count; i++)
        {
            float y = startY - i * (cardH + gap);

            var cardGO = new GameObject("WCard_" + i);
            cardGO.transform.SetParent(_memorizePanel, false);
            var cardRT = cardGO.AddComponent<RectTransform>();
            cardRT.anchorMin        = V(0.5f, 0.5f);
            cardRT.anchorMax        = V(0.5f, 0.5f);
            cardRT.pivot            = V(0.5f, 0.5f);
            cardRT.sizeDelta        = new Vector2(_memorizePanel.rect.width, cardH);
            cardRT.anchoredPosition = new Vector2(0f, y);

            var cardImg = cardGO.AddComponent<Image>();
            cardImg.color = WORD_BG;

            var accGO = new GameObject("Acc");
            accGO.transform.SetParent(cardRT, false);
            var accRT = accGO.AddComponent<RectTransform>();
            accRT.anchorMin = V(0, 0.1f); accRT.anchorMax = V(0, 0.9f);
            accRT.sizeDelta = V(6, 0); accRT.anchoredPosition = V(3, 0);
            accGO.AddComponent<Image>().color = ACCENT;

            var shGO = new GameObject("Sh");
            shGO.transform.SetParent(cardRT, false);
            var shRT = shGO.AddComponent<RectTransform>();
            shRT.anchorMin = V(0, 0.55f); shRT.anchorMax = V(1, 1);
            shRT.sizeDelta = V(0, 0); shRT.anchoredPosition = V(0, 0);
            shGO.AddComponent<Image>().color = C(1, 1, 1, 0.05f);

            var txt = MkTxt(cardRT, "W", targetWords[i], Color.white, 36, V(0.04f, 0), V(0.96f, 1));
            txt.fontStyle = FontStyles.Bold;
            txt.alignment = TextAlignmentOptions.MidlineLeft;
        }

        SetCountdown(1f);
    }

    public void ShowChoosePhase(List<string> allWords)
    {
        foreach (Transform ch in _choosePanel) Destroy(ch.gameObject);
        _memorizePanel.gameObject.SetActive(false);
        _choosePanel.gameObject.SetActive(true);
        if (_confirmBtnGO != null) _confirmBtnGO.SetActive(true);

        _wordLabels   = new List<string>(allWords);
        _wordBtnsImg  = new List<Image>();
        _wordBtns     = new List<Button>();
        _wordSelected = new List<bool>();

        int total   = allWords.Count;
        int cols    = 3;
        int rows    = Mathf.CeilToInt((float)total / cols);
        float btnW  = (_choosePanel.rect.width - (cols - 1) * 12f) / cols;
        float btnH  = Mathf.Min(64f, (_choosePanel.rect.height - (rows - 1) * 10f) / rows);
        float gapX  = 12f;
        float gapY  = 10f;
        float totalW = cols * btnW + (cols - 1) * gapX;
        float totalH = rows * btnH + (rows - 1) * gapY;
        float startX = -totalW * 0.5f + btnW * 0.5f;
        float startY =  totalH * 0.5f - btnH * 0.5f;

        for (int i = 0; i < total; i++)
        {
            int col = i % cols;
            int row = i / cols;
            float x = startX + col * (btnW + gapX);
            float y = startY - row * (btnH + gapY);

            var btnGO = new GameObject("WBtn_" + i);
            btnGO.transform.SetParent(_choosePanel, false);
            var btnRT = btnGO.AddComponent<RectTransform>();
            btnRT.anchorMin        = V(0.5f, 0.5f);
            btnRT.anchorMax        = V(0.5f, 0.5f);
            btnRT.pivot            = V(0.5f, 0.5f);
            btnRT.sizeDelta        = new Vector2(btnW, btnH);
            btnRT.anchoredPosition = new Vector2(x, y);

            var img = btnGO.AddComponent<Image>();
            img.color = BTN_IDLE;
            _wordBtnsImg.Add(img);
            _wordSelected.Add(false);

            var shGO = new GameObject("Sh");
            shGO.transform.SetParent(btnRT, false);
            var shRT = shGO.AddComponent<RectTransform>();
            shRT.anchorMin = V(0, 0.5f); shRT.anchorMax = V(1, 1);
            shRT.sizeDelta = V(0, 0); shRT.anchoredPosition = V(0, 0);
            shGO.AddComponent<Image>().color = C(1, 1, 1, 0.06f);

            var txt = MkTxt(btnRT, "T", allWords[i], Color.white, 26, V(0.04f, 0), V(0.96f, 1));
            txt.fontStyle = FontStyles.Bold;

            int capturedIndex = i;
            var btn = btnGO.AddComponent<Button>();
            btn.targetGraphic = img;
            var cb = btn.colors;
            cb.normalColor      = Color.white;
            cb.highlightedColor = C(1, 1, 1, 0.88f);
            cb.pressedColor     = C(0.72f, 0.72f, 0.72f);
            btn.colors = cb;
            btn.onClick.AddListener(() => _onWordToggled?.Invoke(capturedIndex));
            _wordBtns.Add(btn);
        }

        SetCountdown(0f);
    }

    public void SetPhaseLabel(string text, Color col)
    {
        if (_phaseLbl != null) { _phaseLbl.text = text; _phaseLbl.color = col; }
    }

    public void SetInfoLabel(string text)
    {
        if (_infoLbl != null) _infoLbl.text = text;
    }

    public void UpdateRound(int current, int total)
    {
        if (_roundLbl != null) _roundLbl.text = "Ronda " + current + "/" + total;
    }

    public void UpdateScore(int score)
    {
        if (_scoreLbl != null) _scoreLbl.text = score + " pts";
    }

    public void SetCountdown(float t)
    {
        if (_countdownFill == null) return;
        t = Mathf.Clamp01(t);
        _countdownFill.fillAmount = t;
        _countdownFill.color = Color.Lerp(CRED, ACCENT, t);
    }

    public void ToggleWord(int idx)
    {
        if (idx < 0 || idx >= _wordSelected.Count) return;
        _wordSelected[idx]       = !_wordSelected[idx];
        _wordBtnsImg[idx].color  = _wordSelected[idx] ? BTN_SEL : BTN_IDLE;
    }

    public List<int> GetSelectedIndices()
    {
        var list = new List<int>();
        for (int i = 0; i < _wordSelected.Count; i++)
            if (_wordSelected[i]) list.Add(i);
        return list;
    }

    public void ShowWordResult(HashSet<int> targetIndices, List<int> playerSelected)
    {
        if (_confirmBtnGO != null) _confirmBtnGO.SetActive(false);
        var playerSet = new HashSet<int>(playerSelected);

        for (int i = 0; i < _wordBtnsImg.Count; i++)
        {
            bool inTarget = targetIndices.Contains(i);
            bool inPlayer = playerSet.Contains(i);
            _wordBtns[i].interactable = false;

            if      ( inTarget &&  inPlayer) _wordBtnsImg[i].color = CGREEN;
            else if (!inTarget &&  inPlayer) _wordBtnsImg[i].color = CRED;
            else if ( inTarget && !inPlayer) _wordBtnsImg[i].color = CORANGE;
            else                             _wordBtnsImg[i].color = C(0.09f, 0.11f, 0.18f);
        }
    }

    public void ShowFinalResult(bool win, string sub)
    {
        _resultTitle.text  = win ? "¡Memoria verbal!" : "Sigue practicando";
        _resultTitle.color = win ? CGREEN : CRED;
        _resultSub.text    = sub;
        _resultPanel.SetActive(true);
    }

    GameObject BuildConfirmBtn(RectTransform R)
    {
        var rt = MkImg(R, "ConfirmBtn", ACCENT,
                       V(0.35f, 0.135f), V(0.65f, 0.215f), V(0,0), V(0,0));
        MkImg(rt, "Sh", C(1,1,1,0.13f), V(0,0.5f), V(1,1), V(0,0), V(0,0));
        var btn = rt.gameObject.AddComponent<Button>();
        btn.targetGraphic = rt.GetComponent<Image>();
        var cb = btn.colors;
        cb.normalColor      = Color.white;
        cb.highlightedColor = C(1,1,1,0.85f);
        cb.pressedColor     = C(0.72f,0.72f,0.72f);
        btn.colors = cb;
        btn.onClick.AddListener(() => _onConfirm?.Invoke());
        var t = MkTxt(rt, "T", "CONFIRMAR", Color.white, 30, V(0,0), V(1,1));
        t.fontStyle = FontStyles.Bold;
        return rt.gameObject;
    }

    void BuildResultPanel(RectTransform R, Action onRestart, Action onMenu)
    {
        _resultPanel = new GameObject("ResultPanel");
        _resultPanel.transform.SetParent(R, false);
        var er = _resultPanel.AddComponent<RectTransform>();
        er.anchorMin = V(0,0); er.anchorMax = V(1,1);
        er.sizeDelta = V(0,0); er.anchoredPosition = V(0,0);
        _resultPanel.AddComponent<Image>().color = C(0,0,0,0.86f);

        var card = MkImg(er, "Card", PANEL, V(0.5f,0.5f), V(0.5f,0.5f), V(0,0), V(820f,420f));
        MkImg(card, "Sh",    C(1,1,1,0.03f), V(0,0.5f),  V(1,1),     V(0,0),  V(0,0));
        MkImg(card, "LineT", ACCENT,          V(0,1),     V(1,1),     V(0,-4), V(0,8));
        MkImg(card, "AccL",  ACCENT,          V(0,0.08f), V(0,0.92f), V(4,0),  V(8,0));

        _resultTitle = MkTxt(card, "RT", "", Color.white, 52, V(0.05f,0.74f), V(0.95f,0.97f));
        _resultTitle.fontStyle = FontStyles.Bold;
        _resultSub = MkTxt(card, "RS", "", C(0.48f,0.62f,0.80f), 23, V(0.05f,0.24f), V(0.95f,0.72f));
        _resultSub.overflowMode = TextOverflowModes.Overflow;

        MkBtn(card, "Jugar de nuevo",     ACCENT,                V(0.05f,0.20f), V(0.48f,0.34f), onRestart);
        MkBtn(card, "Volver a la seccion", C(0.18f,0.24f,0.38f), V(0.52f,0.20f), V(0.95f,0.34f), onMenu);
        MkBtn(card, "Menu principal",     C(0.10f,0.13f,0.22f),  V(0.05f,0.04f), V(0.95f,0.17f), () => SceneLoader.GoToMainMenu());

        _resultPanel.SetActive(false);
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
