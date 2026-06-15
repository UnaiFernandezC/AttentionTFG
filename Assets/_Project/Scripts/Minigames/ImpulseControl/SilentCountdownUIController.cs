using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SilentCountdownUIController : MonoBehaviour
{

    static Vector2 V(float x, float y) => new Vector2(x, y);
    static Color   C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

    static readonly Color BG      = C(0.05f, 0.08f, 0.14f);
    static readonly Color HDR     = C(0.03f, 0.05f, 0.10f);
    static readonly Color PANEL   = C(0.07f, 0.11f, 0.20f);
    static readonly Color ACCENT  = C(0.18f, 0.80f, 0.58f);
    static readonly Color DIM     = C(0.40f, 0.55f, 0.65f);
    static readonly Color CRED    = C(0.90f, 0.22f, 0.28f);
    static readonly Color CGREEN  = C(0.22f, 0.86f, 0.54f);
    static readonly Color CYELLOW = C(0.95f, 0.80f, 0.15f);

    Image[]          _roundDots;
    TextMeshProUGUI  _scoreText;

    GameObject       _readyPanel;
    TextMeshProUGUI  _targetLabel;
    TextMeshProUGUI  _targetSeconds;
    TextMeshProUGUI  _readyHint;

    GameObject       _countingPanel;
    Image            _pulseRing;
    TextMeshProUGUI  _countingHint;

    GameObject       _resultPanel;
    TextMeshProUGUI  _resultTarget;
    TextMeshProUGUI  _resultActual;
    TextMeshProUGUI  _resultDiff;
    TextMeshProUGUI  _resultRating;

    Image            _mainBtnImg;
    TextMeshProUGUI  _mainBtnTxt;

    GameObject       _finalPanel;
    TextMeshProUGUI  _finalTitle;
    TextMeshProUGUI  _finalSub;

    Image _flashOverlay;

    Coroutine _pulseCoroutine;

    public void BuildUI(int rounds, Action onMainButton, Action onRestart, Action onMenu)
    {

        var cGO = new GameObject("Canvas_SilentCountdown");
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

        MkImg(R, "BG",   BG,                          V(0,0), V(1,1), V(0,0), V(0,0));
        MkImg(R, "Grad", C(0.00f,0.08f,0.18f,0.28f), V(0,0), V(1,1), V(0,0), V(0,0));
        BuildGrid(R);

        var hdr = MkImg(R, "Hdr", HDR, V(0,1), V(1,1), V(0,-44f), V(0,88f));
        MkImg(hdr, "LineB", ACCENT, V(0,0),     V(1,0),     V(0,1.5f), V(0,3f));
        MkImg(hdr, "AccL",  ACCENT, V(0,0.18f), V(0,0.82f), V(3f,0),   V(6f,0));

        var ttl = MkTxt(hdr, "Title", "CUENTA ATRÁS SILENCIOSA", Color.white, 26,
                        V(0.03f,0.12f), V(0.58f,0.88f));
        ttl.fontStyle = FontStyles.Bold;
        ttl.alignment = TextAlignmentOptions.MidlineLeft;
        ttl.characterSpacing = 1.5f;

        MkTxt(hdr, "Cat", "CONTROL DE IMPULSOS", DIM, 15,
              V(0.58f,0.12f), V(0.74f,0.88f)).alignment = TextAlignmentOptions.MidlineRight;

        _roundDots = BuildRoundDots(hdr, rounds);

        _scoreText = MkTxt(R, "Score", "0 pts", DIM, 20,
                           V(0.01f, 0.885f), V(0.16f, 0.935f));
        _scoreText.alignment = TextAlignmentOptions.MidlineLeft;

        BuildInstructionPanel(R);

        BuildReadyPanel(R);
        BuildCountingPanel(R);
        BuildResultPanel(R);

        BuildMainButton(R, onMainButton);

        var footer = MkImg(R, "Footer", HDR, V(0,0), V(1,0), V(0,40f), V(0,80f));
        MkImg(footer, "LineT", ACCENT, V(0,1), V(1,1), V(0,-1.5f), V(0,3f));
        MkTxt(footer, "Hint",
              "Confía en tu percepción del tiempo  ·  No hay trampas",
              C(ACCENT.r, ACCENT.g-0.08f, ACCENT.b-0.05f), 16,
              V(0.01f,0), V(0.78f,1)).alignment = TextAlignmentOptions.MidlineLeft;
        MkImg(footer, "Sep", C(1,1,1,0.10f), V(0.78f,0.1f), V(0.782f,0.9f), V(0,0), V(0,0));

        var fGO = new GameObject("Flash");
        fGO.transform.SetParent(R, false);
        var fRT = fGO.AddComponent<RectTransform>();
        fRT.anchorMin = V(0,0); fRT.anchorMax = V(1,1);
        fRT.sizeDelta = V(0,0); fRT.anchoredPosition = V(0,0);
        _flashOverlay = fGO.AddComponent<Image>();
        _flashOverlay.color = C(0,0,0,0);
        _flashOverlay.raycastTarget = false;
        fGO.SetActive(false);

        BuildFinalPanel(R, onRestart, onMenu);

        ShowReady(5f);
    }

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
        var dots = new Image[rounds];
        float startX  = 0.76f;
        float spacing = 0.04f;
        for (int i = 0; i < rounds; i++)
        {
            var go = new GameObject("Dot_"+i);
            go.transform.SetParent(hdr, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = V(startX + i*spacing, 0.5f);
            rt.pivot = V(0.5f,0.5f);
            rt.sizeDelta = V(26f,26f);
            rt.anchoredPosition = V(0,0);
            var img = go.AddComponent<Image>();
            img.sprite = MakeCircleSprite(32);
            img.color  = C(0.25f,0.30f,0.40f);
            dots[i] = img;
        }
        return dots;
    }

    void BuildInstructionPanel(RectTransform R)
    {
        var panel = MkImg(R, "InstrPanel", C(0.04f,0.07f,0.14f,0.88f),
                          V(0,0.12f), V(0,0.88f), V(90f,0), V(160f,0));
        MkImg(panel, "Line", ACCENT, V(1,0), V(1,1), V(-1.5f,0), V(3f,0));

        MkTxt(panel, "T1", "CÓMO\nJUGAR", ACCENT, 18,
              V(0.08f,0.82f), V(0.92f,0.98f)).fontStyle = FontStyles.Bold;

        MkImg(panel, "Sep0", C(1,1,1,0.08f), V(0.1f,0.80f), V(0.9f,0.81f), V(0,0), V(0,0));

        MkTxt(panel, "S1", "①", Color.white, 16, V(0.05f,0.66f), V(0.25f,0.78f));
        MkTxt(panel, "D1", "Ve el\nobjetivo", DIM, 13, V(0.25f,0.66f), V(0.95f,0.78f)).alignment =
            TextAlignmentOptions.MidlineLeft;

        MkTxt(panel, "S2", "②", Color.white, 16, V(0.05f,0.52f), V(0.25f,0.64f));
        MkTxt(panel, "D2", "Pulsa\n¡EMPIEZA!", DIM, 13, V(0.25f,0.52f), V(0.95f,0.64f)).alignment =
            TextAlignmentOptions.MidlineLeft;

        MkTxt(panel, "S3", "③", Color.white, 16, V(0.05f,0.38f), V(0.25f,0.50f));
        MkTxt(panel, "D3", "Cuenta\nsin mirar", DIM, 13, V(0.25f,0.38f), V(0.95f,0.50f)).alignment =
            TextAlignmentOptions.MidlineLeft;

        MkTxt(panel, "S4", "④", Color.white, 16, V(0.05f,0.24f), V(0.25f,0.36f));
        MkTxt(panel, "D4", "Pulsa\n¡YA!", DIM, 13, V(0.25f,0.24f), V(0.95f,0.36f)).alignment =
            TextAlignmentOptions.MidlineLeft;

        MkImg(panel, "Sep1", C(1,1,1,0.08f), V(0.1f,0.20f), V(0.9f,0.21f), V(0,0), V(0,0));

        MkTxt(panel, "Tip", "Sin trucos.\nSolo tú y\nel tiempo.", C(0.50f,0.60f,0.72f), 13,
              V(0.06f,0.01f), V(0.94f,0.19f)).alignment = TextAlignmentOptions.Center;
    }

    void BuildReadyPanel(RectTransform R)
    {
        _readyPanel = new GameObject("ReadyPanel");
        _readyPanel.transform.SetParent(R, false);
        var rt = _readyPanel.AddComponent<RectTransform>();
        rt.anchorMin = V(0.25f, 0.22f);
        rt.anchorMax = V(0.85f, 0.84f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        var bg = _readyPanel.AddComponent<Image>();
        bg.color = C(0.06f, 0.09f, 0.16f, 0.0f);

        _targetLabel = MkTxt(rt, "Label", "MEMORIZA EL TIEMPO",
                             DIM, 18, V(0.05f, 0.72f), V(0.95f, 0.88f));
        _targetLabel.alignment = TextAlignmentOptions.Center;
        _targetLabel.characterSpacing = 2f;

        _targetSeconds = MkTxt(rt, "Seconds", "5 s", Color.white, 96,
                               V(0.05f, 0.32f), V(0.95f, 0.72f));
        _targetSeconds.fontStyle = FontStyles.Bold;
        _targetSeconds.alignment = TextAlignmentOptions.Center;

        _readyHint = MkTxt(rt, "Hint", "Pulsa ¡EMPIEZA! cuando estés listo",
                           DIM, 18, V(0.05f, 0.10f), V(0.95f, 0.28f));
        _readyHint.alignment = TextAlignmentOptions.Center;
        _readyHint.fontStyle = FontStyles.Italic;
    }

    void BuildCountingPanel(RectTransform R)
    {
        _countingPanel = new GameObject("CountingPanel");
        _countingPanel.transform.SetParent(R, false);
        var rt = _countingPanel.AddComponent<RectTransform>();
        rt.anchorMin = V(0.25f, 0.22f);
        rt.anchorMax = V(0.85f, 0.84f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        var ringGO = new GameObject("PulseRing");
        ringGO.transform.SetParent(rt, false);
        var rRT = ringGO.AddComponent<RectTransform>();
        rRT.anchorMin = rRT.anchorMax = V(0.5f, 0.5f);
        rRT.pivot = V(0.5f, 0.5f);
        rRT.sizeDelta = V(220f, 220f);
        rRT.anchoredPosition = V(0, 0);
        _pulseRing = ringGO.AddComponent<Image>();
        _pulseRing.sprite = MakeCircleSprite(128);
        _pulseRing.color  = C(0.18f, 0.80f, 0.58f, 0.12f);

        var qMark = MkTxt(rt, "QMark", "?", C(0.18f, 0.80f, 0.58f, 0.35f), 120,
                          V(0.1f, 0.25f), V(0.9f, 0.78f));
        qMark.fontStyle = FontStyles.Bold;
        qMark.alignment = TextAlignmentOptions.Center;

        _countingHint = MkTxt(rt, "Hint", "¡Pulsa cuando creas que ha pasado el tiempo!",
                              DIM, 18, V(0.05f, 0.05f), V(0.95f, 0.22f));
        _countingHint.alignment = TextAlignmentOptions.Center;
        _countingHint.fontStyle = FontStyles.Italic;

        _countingPanel.SetActive(false);
    }

    void BuildResultPanel(RectTransform R)
    {
        _resultPanel = new GameObject("ResultPanel");
        _resultPanel.transform.SetParent(R, false);
        var rt = _resultPanel.AddComponent<RectTransform>();
        rt.anchorMin = V(0.25f, 0.22f);
        rt.anchorMax = V(0.85f, 0.84f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        var cardRT = MkImg(rt, "Card", C(0.06f,0.09f,0.18f,0.90f),
                           V(0.05f,0.05f), V(0.95f,0.95f), V(0,0), V(0,0));
        MkImg(cardRT, "BorderT", ACCENT, V(0,1), V(1,1), V(0,-2), V(0,4));

        _resultRating = MkTxt(cardRT, "Rating", "PERFECTO", CGREEN, 38,
                              V(0.05f, 0.76f), V(0.95f, 0.96f));
        _resultRating.fontStyle = FontStyles.Bold;
        _resultRating.alignment = TextAlignmentOptions.Center;

        MkTxt(cardRT, "TL", "Objetivo:", DIM, 20, V(0.05f, 0.60f), V(0.45f, 0.74f)).alignment =
            TextAlignmentOptions.MidlineRight;
        _resultTarget = MkTxt(cardRT, "TV", "—", Color.white, 22,
                              V(0.50f, 0.60f), V(0.95f, 0.74f));
        _resultTarget.fontStyle = FontStyles.Bold;
        _resultTarget.alignment = TextAlignmentOptions.MidlineLeft;

        MkTxt(cardRT, "AL", "Tu tiempo:", DIM, 20, V(0.05f, 0.44f), V(0.45f, 0.58f)).alignment =
            TextAlignmentOptions.MidlineRight;
        _resultActual = MkTxt(cardRT, "AV", "—", Color.white, 22,
                              V(0.50f, 0.44f), V(0.95f, 0.58f));
        _resultActual.fontStyle = FontStyles.Bold;
        _resultActual.alignment = TextAlignmentOptions.MidlineLeft;

        MkImg(cardRT, "Sep", C(1,1,1,0.08f), V(0.05f,0.40f), V(0.95f,0.41f), V(0,0), V(0,0));
        MkTxt(cardRT, "DL", "Diferencia:", DIM, 20, V(0.05f, 0.24f), V(0.45f, 0.38f)).alignment =
            TextAlignmentOptions.MidlineRight;
        _resultDiff = MkTxt(cardRT, "DV", "—", CGREEN, 24,
                            V(0.50f, 0.24f), V(0.95f, 0.38f));
        _resultDiff.fontStyle = FontStyles.Bold;
        _resultDiff.alignment = TextAlignmentOptions.MidlineLeft;

        MkTxt(cardRT, "Next", "Pulsa el botón Continuar o ESPACIO para seguir", DIM, 15,
              V(0.05f, 0.04f), V(0.95f, 0.18f)).alignment = TextAlignmentOptions.Center;

        _resultPanel.SetActive(false);
    }

    void BuildMainButton(RectTransform R, Action onMainButton)
    {

        var haloGO = new GameObject("BtnHalo");
        haloGO.transform.SetParent(R, false);
        var hRT = haloGO.AddComponent<RectTransform>();
        hRT.anchorMin = hRT.anchorMax = V(0.555f, 0.195f);
        hRT.pivot = V(0.5f, 0.5f);
        hRT.sizeDelta = V(330f, 100f);
        hRT.anchoredPosition = V(0, 0);
        var hImg = haloGO.AddComponent<Image>();
        hImg.sprite = MakeRoundedRectSprite(330, 100, 20);
        hImg.color  = C(ACCENT.r, ACCENT.g, ACCENT.b, 0.18f);

        var btnGO = new GameObject("MainBtn");
        btnGO.transform.SetParent(R, false);
        var bRT = btnGO.AddComponent<RectTransform>();
        bRT.anchorMin = bRT.anchorMax = V(0.555f, 0.195f);
        bRT.pivot = V(0.5f, 0.5f);
        bRT.sizeDelta = V(310f, 80f);
        bRT.anchoredPosition = V(0, 0);

        _mainBtnImg = btnGO.AddComponent<Image>();
        _mainBtnImg.sprite = MakeRoundedRectSprite(310, 80, 18);
        _mainBtnImg.color  = ACCENT;

        var shGO = new GameObject("Sh");
        shGO.transform.SetParent(btnGO.transform, false);
        var shRT = shGO.AddComponent<RectTransform>();
        shRT.anchorMin = V(0,0.5f); shRT.anchorMax = V(1,1);
        shRT.offsetMin = shRT.offsetMax = Vector2.zero;
        shGO.AddComponent<Image>().color = C(1,1,1,0.10f);

        var btn = btnGO.AddComponent<Button>();
        btn.targetGraphic = _mainBtnImg;
        var cb = btn.colors;
        cb.normalColor = Color.white; cb.highlightedColor = C(1,1,1,0.88f);
        cb.pressedColor = C(0.72f,0.72f,0.72f); btn.colors = cb;
        btn.onClick.AddListener(() => onMainButton?.Invoke());

        _mainBtnTxt = MkTxt(bRT, "T", "¡EMPIEZA!", Color.white, 30, V(0,0), V(1,1));
        _mainBtnTxt.fontStyle = FontStyles.Bold;
    }

    void BuildFinalPanel(RectTransform R, Action onRestart, Action onMenu)
    {
        _finalPanel = new GameObject("FinalPanel");
        _finalPanel.transform.SetParent(R, false);
        var er = _finalPanel.AddComponent<RectTransform>();
        er.anchorMin = V(0,0); er.anchorMax = V(1,1);
        er.sizeDelta = V(0,0); er.anchoredPosition = V(0,0);
        _finalPanel.AddComponent<Image>().color = C(0,0,0,0.88f);

        var card = MkImg(er, "Card", PANEL, V(0.5f,0.5f), V(0.5f,0.5f), V(0,0), V(900f,480f));
        MkImg(card, "Sh",    C(1,1,1,0.03f), V(0,0.5f),   V(1,1),     V(0,0),  V(0,0));
        MkImg(card, "LineT", ACCENT,          V(0,1),      V(1,1),     V(0,-4), V(0,8));
        MkImg(card, "AccL",  ACCENT,          V(0,0.08f),  V(0,0.92f), V(4,0),  V(8,0));

        _finalTitle = MkTxt(card, "FT", "", Color.white, 44,
                            V(0.05f,0.76f), V(0.95f,0.97f));
        _finalTitle.fontStyle = FontStyles.Bold;
        _finalTitle.enableAutoSizing = true;
        _finalTitle.fontSizeMin = 26f; _finalTitle.fontSizeMax = 46f;

        _finalSub = MkTxt(card, "FS", "", C(0.50f,0.68f,0.80f), 22,
                          V(0.05f,0.22f), V(0.95f,0.74f));
        _finalSub.overflowMode = TextOverflowModes.Overflow;
        _finalSub.alignment    = TextAlignmentOptions.Center;
        _finalSub.lineSpacing  = 10f;

        MkBtn(card, "Jugar de nuevo",   ACCENT,                V(0.05f,0.04f), V(0.48f,0.17f), onRestart);
        MkBtn(card, "Elegir minijuego", C(0.18f,0.24f,0.38f), V(0.52f,0.04f), V(0.95f,0.17f), onMenu);

        _finalPanel.SetActive(false);
    }

    public void ShowReady(float targetSeconds)
    {
        _readyPanel.SetActive(true);
        _countingPanel.SetActive(false);
        _resultPanel.SetActive(false);

        _targetSeconds.text = FormatSeconds(targetSeconds);
        _targetSeconds.color = Color.white;

        SetMainButton("¡EMPIEZA!", ACCENT);
    }

    public void ShowCounting()
    {
        _readyPanel.SetActive(false);
        _countingPanel.SetActive(true);
        _resultPanel.SetActive(false);

        SetMainButton("¡YA!", ACCENT);

        if (_pulseCoroutine != null) StopCoroutine(_pulseCoroutine);
        _pulseCoroutine = StartCoroutine(PulseRoutine());
    }

    public void ShowRoundResult(float target, float actual, float diff, bool signPositive,
                                string ratingText, Color ratingColor)
    {
        if (_pulseCoroutine != null) { StopCoroutine(_pulseCoroutine); _pulseCoroutine = null; }

        _readyPanel.SetActive(false);
        _countingPanel.SetActive(false);
        _resultPanel.SetActive(true);

        _resultRating.text  = ratingText;
        _resultRating.color = ratingColor;
        _resultTarget.text  = FormatSeconds(target);
        _resultActual.text  = FormatSeconds(actual);

        string sign = signPositive ? "+" : "-";
        _resultDiff.text  = $"{sign}{diff:F2} s";
        _resultDiff.color = ratingColor;

        SetMainButton("Continuar", C(0.14f, 0.22f, 0.38f));
    }

    public void ShowFinalResult(bool won, int correct, int total, int score)
    {
        if (_pulseCoroutine != null) { StopCoroutine(_pulseCoroutine); _pulseCoroutine = null; }

        _finalPanel.SetActive(true);

        string title = won ? "¡Sentido del tiempo!" : "Sigue entrenando";
        Color  tcol  = won ? CGREEN : CRED;

        string msg = won
            ? $"Acertaste {correct} de {total} rondas.\n" +
              $"Puntuación: {score} pts\n\n" +
              "Tu percepción interna del tiempo es excelente.\n" +
              "El control de impulsos empieza por el tiempo."
            : $"Aciertos: {correct} de {total}\n\n" +
              "El tiempo interno se puede entrenar.\n" +
              "Practica contando despacio y con ritmo constante.";

        _finalTitle.text  = title;
        _finalTitle.color = tcol;
        _finalSub.text    = msg;
    }

    public void SetRoundDot(int index, bool? correct)
    {
        if (_roundDots == null || index >= _roundDots.Length) return;
        _roundDots[index].color = correct == null  ? C(0.25f,0.30f,0.40f)
                                : correct == true  ? CGREEN : CRED;
    }

    public void SetScore(int score)
    {
        if (_scoreText) _scoreText.text = $"{score} pts";
    }

    public void Flash(Color col)
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

    IEnumerator PulseRoutine()
    {
        float t = 0f;
        while (true)
        {
            t += Time.deltaTime * 1.4f;
            float scale = 1f + Mathf.Sin(t * Mathf.PI * 2f) * 0.08f;
            float alpha = 0.10f + Mathf.Sin(t * Mathf.PI * 2f) * 0.06f;
            if (_pulseRing != null)
            {
                _pulseRing.rectTransform.localScale = Vector3.one * scale;
                _pulseRing.color = C(0.18f, 0.80f, 0.58f, alpha);
            }
            yield return null;
        }
    }

    void SetMainButton(string label, Color col)
    {
        if (_mainBtnTxt)  _mainBtnTxt.text  = label;
        if (_mainBtnImg)  _mainBtnImg.color = col;
    }

    static string FormatSeconds(float s) => $"{s:F1} s";

    public static Sprite MakeCircleSprite(int res = 128)
    {
        var tex = new Texture2D(res, res, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float r  = res * 0.5f;
        var  px  = new Color[res * res];
        for (int y = 0; y < res; y++)
        for (int x = 0; x < res; x++)
        {
            float d = Vector2.Distance(new Vector2(x+0.5f, y+0.5f), new Vector2(r,r));
            float a = Mathf.Clamp01(1f - (d - r + 1.5f) / 2f);
            px[y*res+x] = new Color(1,1,1,a);
        }
        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0,0,res,res), V(0.5f,0.5f));
    }

    static Sprite MakeRoundedRectSprite(int w, int h, int r)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        var px = new Color[w * h];
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            int cx = Mathf.Clamp(x, r, w-r);
            int cy = Mathf.Clamp(y, r, h-r);
            float d = Mathf.Sqrt((x-cx)*(x-cx)+(y-cy)*(y-cy));
            float a = Mathf.Clamp01(r - d + 0.5f);
            px[y*w+x] = new Color(1,1,1,a);
        }
        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0,0,w,h), V(0.5f,0.5f));
    }

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
        t.alignment = TextAlignmentOptions.Center;
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
        var t = MkTxt(rt, "T", lbl, Color.white, 24, V(0,0), V(1,1));
        t.fontStyle = FontStyles.Bold;
    }
}
