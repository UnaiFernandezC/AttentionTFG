using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StopAndGoUIController : MonoBehaviour
{

    static Vector2 V(float x, float y) => new Vector2(x, y);
    static Color   C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

    static readonly Color BG     = C(0.05f, 0.08f, 0.14f);
    static readonly Color HDR    = C(0.03f, 0.05f, 0.10f);
    static readonly Color PANEL  = C(0.07f, 0.11f, 0.20f);
    static readonly Color ACCENT = C(0.18f, 0.80f, 0.58f);
    static readonly Color DIM    = C(0.40f, 0.55f, 0.65f);
    static readonly Color CRED   = C(0.90f, 0.22f, 0.28f);
    static readonly Color CGREEN = C(0.22f, 0.86f, 0.54f);

    const float TRACK_R     = 185f;
    const float TRACK_THICK = 32f;
    const float MARKER_SIZE = 38f;

    Image[]          _roundDots;
    Image[]          _stopDots;
    TextMeshProUGUI  _scoreText;
    TextMeshProUGUI  _roundLabel;
    Image            _zoneArcImg;
    RectTransform    _markerRT;
    Image            _markerImg;
    RectTransform    _haloRT;
    Image            _haloImg;
    Image            _flashOverlay;
    GameObject       _resultPanel;
    TextMeshProUGUI  _resultTitle;
    TextMeshProUGUI  _resultSub;

    int _stopsPerRound;

    public void BuildUI(
        int    rounds,
        int    stopsPerRound,
        Action onStop,
        Action onRestart,
        Action onMenu)
    {
        _stopsPerRound = stopsPerRound;

        var cGO = new GameObject("Canvas_StopAndGo");
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

        var ttl = MkTxt(hdr, "Title", "STOP & GO", Color.white, 30,
                        V(0.03f,0.12f), V(0.52f,0.88f));
        ttl.fontStyle = FontStyles.Bold;
        ttl.alignment = TextAlignmentOptions.MidlineLeft;
        ttl.characterSpacing = 1.5f;

        MkTxt(hdr, "Cat", "CONTROL DE IMPULSOS", DIM, 15,
              V(0.52f,0.12f), V(0.72f,0.88f)).alignment = TextAlignmentOptions.MidlineRight;

        _roundDots = BuildDotRow(hdr, rounds, 0.76f, 0.04f, 26f);

        _scoreText = MkTxt(R, "Score", "0 pts", Color.white, 30,
                           V(0.72f, 0.88f), V(0.98f, 0.97f));
        _scoreText.alignment = TextAlignmentOptions.MidlineRight;

        BuildLegendPanel(R, stopsPerRound);

        BuildTrack(R);

        var footer = MkImg(R, "Footer", HDR, V(0,0), V(1,0), V(0,40f), V(0,80f));
        MkImg(footer, "LineT", ACCENT, V(0,1), V(1,1), V(0,-1.5f), V(0,3f));
        MkTxt(footer, "Hint",
              "Para el punto dentro de la zona VERDE  ·  ESPACIO o ¡PARA!",
              C(ACCENT.r, ACCENT.g-0.08f, ACCENT.b-0.05f), 16,
              V(0.01f,0), V(0.76f,1)).alignment = TextAlignmentOptions.MidlineLeft;
        MkImg(footer, "Sep", C(1,1,1,0.10f), V(0.76f,0.1f), V(0.762f,0.9f), V(0,0), V(0,0));

        var stopRT = MkImg(footer, "StopBtn", ACCENT, V(0.77f,0.08f), V(0.88f,0.92f), V(0,0), V(0,0));
        MkImg(stopRT, "Sh", C(1,1,1,0.12f), V(0,0.5f), V(1,1), V(0,0), V(0,0));
        var stopBtn = stopRT.gameObject.AddComponent<Button>();
        stopBtn.targetGraphic = stopRT.GetComponent<Image>();
        var sc2 = stopBtn.colors;
        sc2.normalColor = Color.white; sc2.highlightedColor = C(1,1,1,0.85f);
        sc2.pressedColor = C(0.72f,0.72f,0.72f); stopBtn.colors = sc2;
        stopBtn.onClick.AddListener(() => onStop?.Invoke());
        var stopTxt = MkTxt(stopRT, "T", "¡PARA!", Color.white, 24, V(0,0), V(1,1));
        stopTxt.fontStyle = FontStyles.Bold;


        var fGO = new GameObject("Flash");
        fGO.transform.SetParent(R, false);
        var fRT = fGO.AddComponent<RectTransform>();
        fRT.anchorMin = V(0,0); fRT.anchorMax = V(1,1);
        fRT.sizeDelta = V(0,0); fRT.anchoredPosition = V(0,0);
        _flashOverlay = fGO.AddComponent<Image>();
        _flashOverlay.color = C(0,0,0,0);
        _flashOverlay.raycastTarget = false;
        fGO.SetActive(false);

        BuildResultPanel(R, onRestart, onMenu);
    }

    void BuildGrid(RectTransform R)
    {
        for (int i = 1; i < 6; i++)
        {
            float t = i / 6f;
            MkImg(R, "GH"+i, C(1,1,1,0.018f), V(0,t-0.001f),     V(1,t+0.001f),     V(0,0), V(0,0));
            MkImg(R, "GV"+i, C(1,1,1,0.018f), V(t-0.0006f,0),    V(t+0.0006f,1),    V(0,0), V(0,0));
        }
    }

    Image[] BuildDotRow(RectTransform parent, int count, float startX, float spacing, float size)
    {
        var dots = new Image[count];
        for (int i = 0; i < count; i++)
        {
            var go = new GameObject("Dot_"+i);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = V(startX + i*spacing, 0.5f);
            rt.pivot = V(0.5f, 0.5f);
            rt.sizeDelta = V(size, size);
            rt.anchoredPosition = V(0, 0);
            var img = go.AddComponent<Image>();
            img.sprite = MakeCircleSprite(32);
            img.color  = C(0.25f, 0.30f, 0.40f);
            dots[i] = img;
        }
        return dots;
    }

    void BuildLegendPanel(RectTransform R, int stopsPerRound)
    {
        var panel = MkImg(R, "Legend", C(0.04f,0.07f,0.14f,0.88f),
                          V(0,0.12f), V(0,0.88f), V(100f,0), V(180f,0));
        MkImg(panel, "Line", ACCENT, V(1,0), V(1,1), V(-1.5f,0), V(3f,0));

        _roundLabel = MkTxt(panel, "Round", "Ronda 1/3", Color.white, 15,
                            V(0.04f,0.84f), V(0.96f,0.98f));
        _roundLabel.fontStyle = FontStyles.Bold;
        _roundLabel.alignment = TextAlignmentOptions.Center;

        MkTxt(panel, "StopsLbl", "Paradas", DIM, 13,
              V(0.08f, 0.76f), V(0.92f, 0.84f)).alignment = TextAlignmentOptions.Center;

        _stopDots = new Image[stopsPerRound];
        float dotSpacingX = 0.28f;
        float dotsStartX = 0.5f - (stopsPerRound - 1) * dotSpacingX * 0.5f;
        for (int i = 0; i < stopsPerRound; i++)
        {
            var go = new GameObject("StopDot_"+i);
            go.transform.SetParent(panel, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = V(dotsStartX + i * dotSpacingX, 0.695f);
            rt.pivot = V(0.5f, 0.5f);
            rt.sizeDelta = V(20f, 20f);
            rt.anchoredPosition = V(0, 0);
            var img = go.AddComponent<Image>();
            img.sprite = MakeCircleSprite(32);
            img.color  = C(0.25f, 0.30f, 0.40f);
            _stopDots[i] = img;
        }

        MkImg(panel, "Sep1", C(1,1,1,0.08f), V(0.1f,0.64f), V(0.9f,0.65f), V(0,0), V(0,0));

        MkTxt(panel, "T2", "VERDE", CGREEN, 19, V(0.1f,0.50f), V(0.9f,0.63f)).fontStyle = FontStyles.Bold;
        MkTxt(panel, "D2", "¡Para aquí!", DIM, 13, V(0.1f,0.40f), V(0.9f,0.51f));

        MkImg(panel, "Sep2", C(1,1,1,0.08f), V(0.1f,0.37f), V(0.9f,0.38f), V(0,0), V(0,0));

        MkTxt(panel, "T1", "ROJO", CRED, 19, V(0.1f,0.23f), V(0.9f,0.36f)).fontStyle = FontStyles.Bold;
        MkTxt(panel, "D1", "Fuera de zona", DIM, 13, V(0.1f,0.13f), V(0.9f,0.24f));

        MkImg(panel, "Sep3", C(1,1,1,0.08f), V(0.1f,0.10f), V(0.9f,0.11f), V(0,0), V(0,0));

        MkTxt(panel, "Tip", "Cada ronda\nmás difícil", C(0.50f,0.60f,0.72f), 13,
              V(0.06f,0.01f), V(0.94f,0.10f)).alignment = TextAlignmentOptions.Center;
    }

    void BuildTrack(RectTransform R)
    {
        var tGO = new GameObject("TrackArea");
        tGO.transform.SetParent(R, false);
        var tRT = tGO.AddComponent<RectTransform>();
        tRT.anchorMin = tRT.anchorMax = V(0.5f, 0.52f);
        tRT.pivot = V(0.5f, 0.5f);
        tRT.sizeDelta = V(500f, 500f);
        tRT.anchoredPosition = V(0, 0);

        BuildDisc(tRT, "Ring_BG",    C(0.12f,0.16f,0.26f), TRACK_R + TRACK_THICK + 2f);

        BuildDisc(tRT, "Ring_Inner", BG,                    TRACK_R - TRACK_THICK - 2f);

        BuildDisc(tRT, "Ring_Rim",   C(1,1,1,0.05f),        TRACK_R + TRACK_THICK + 5f);

        var arcGO = new GameObject("ZoneArc");
        arcGO.transform.SetParent(tRT, false);
        var arcRT = arcGO.AddComponent<RectTransform>();
        arcRT.anchorMin = arcRT.anchorMax = V(0.5f,0.5f);
        arcRT.pivot = V(0.5f, 0.5f);
        float arcDiam = (TRACK_R + TRACK_THICK + 4f) * 2f;
        arcRT.sizeDelta = V(arcDiam, arcDiam);
        arcRT.anchoredPosition = V(0,0);
        _zoneArcImg = arcGO.AddComponent<Image>();
        _zoneArcImg.color = Color.white;

        var hGO = new GameObject("MarkerHalo");
        hGO.transform.SetParent(tRT, false);
        _haloRT = hGO.AddComponent<RectTransform>();
        _haloRT.anchorMin = _haloRT.anchorMax = V(0.5f,0.5f);
        _haloRT.pivot = V(0.5f,0.5f);
        _haloRT.sizeDelta = V(MARKER_SIZE+14f, MARKER_SIZE+14f);
        _haloRT.anchoredPosition = V(0,0);
        _haloImg = hGO.AddComponent<Image>();
        _haloImg.sprite = MakeCircleSprite(64);
        _haloImg.color  = C(1,1,1,0.12f);
        _haloImg.raycastTarget = false;

        var mGO = new GameObject("Marker");
        mGO.transform.SetParent(tRT, false);
        _markerRT = mGO.AddComponent<RectTransform>();
        _markerRT.anchorMin = _markerRT.anchorMax = V(0.5f,0.5f);
        _markerRT.pivot = V(0.5f,0.5f);
        _markerRT.sizeDelta = V(MARKER_SIZE, MARKER_SIZE);
        _markerImg = mGO.AddComponent<Image>();
        _markerImg.sprite = MakeCircleSprite(64);
        _markerImg.color  = Color.white;
    }

    void BuildDisc(RectTransform parent, string name, Color color, float radius)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = V(0.5f,0.5f);
        rt.pivot = V(0.5f,0.5f);
        rt.sizeDelta = V(radius*2f, radius*2f);
        rt.anchoredPosition = V(0,0);
        var img = go.AddComponent<Image>();
        img.sprite = MakeCircleSprite(256);
        img.color  = color;
    }

    void BuildResultPanel(RectTransform R, Action onRestart, Action onMenu)
    {
        _resultPanel = new GameObject("ResultPanel");
        _resultPanel.transform.SetParent(R, false);
        var er = _resultPanel.AddComponent<RectTransform>();
        er.anchorMin = V(0,0); er.anchorMax = V(1,1);
        er.sizeDelta = V(0,0); er.anchoredPosition = V(0,0);
        _resultPanel.AddComponent<Image>().color = C(0,0,0,0.88f);

        var card = MkImg(er, "Card", PANEL, V(0.5f,0.5f), V(0.5f,0.5f), V(0,0), V(900f,480f));
        MkImg(card, "Sh",    C(1,1,1,0.03f), V(0,0.5f),    V(1,1),      V(0,0),  V(0,0));
        MkImg(card, "LineT", ACCENT,          V(0,1),       V(1,1),      V(0,-4), V(0,8));
        MkImg(card, "AccL",  ACCENT,          V(0,0.08f),   V(0,0.92f),  V(4,0),  V(8,0));

        _resultTitle = MkTxt(card, "RT", "", Color.white, 44,
                             V(0.05f,0.76f), V(0.95f,0.97f));
        _resultTitle.fontStyle = FontStyles.Bold;
        _resultTitle.enableAutoSizing = true;
        _resultTitle.fontSizeMin = 26f; _resultTitle.fontSizeMax = 46f;

        _resultSub = MkTxt(card, "RS", "", C(0.50f,0.68f,0.80f), 22,
                           V(0.05f,0.37f), V(0.95f,0.74f));
        _resultSub.overflowMode = TextOverflowModes.Overflow;
        _resultSub.alignment    = TextAlignmentOptions.Center;
        _resultSub.lineSpacing  = 10f;

        MkBtn(card, "Jugar de nuevo",     ACCENT,                V(0.05f,0.20f), V(0.48f,0.33f), onRestart);
        MkBtn(card, "Volver a la seccion", C(0.18f,0.24f,0.38f), V(0.52f,0.20f), V(0.95f,0.33f), onMenu);
        MkBtn(card, "Menu principal",     C(0.10f,0.13f,0.22f),  V(0.05f,0.05f), V(0.95f,0.17f), () => SceneLoader.GoToMainMenu());

        _resultPanel.SetActive(false);
    }

    public RectTransform GetMarkerRT() => _markerRT;

    public void SetMarkerAngle(float angleDeg, bool inZone)
    {
        if (_markerRT == null) return;

        float rad = (90f - angleDeg) * Mathf.Deg2Rad;
        var pos = new Vector2(Mathf.Cos(rad) * TRACK_R, Mathf.Sin(rad) * TRACK_R);
        _markerRT.anchoredPosition = pos;
        _markerImg.color = inZone ? CGREEN : Color.white;

        if (_haloRT != null)
        {
            _haloRT.anchoredPosition = pos;
            _haloImg.color = inZone
                ? C(CGREEN.r, CGREEN.g, CGREEN.b, 0.28f)
                : C(1f, 1f, 1f, 0.10f);
        }
    }

    public void UpdateZoneArc(float startAngle, float spanAngle)
    {
        if (_zoneArcImg == null) return;
        _zoneArcImg.sprite = MakeArcSprite(512, startAngle, spanAngle,
                                           TRACK_R - TRACK_THICK * 0.5f,
                                           TRACK_R + TRACK_THICK * 0.5f,
                                           CGREEN);
    }

    public void SetRoundDot(int index, bool? won)
    {
        if (_roundDots == null || index >= _roundDots.Length) return;
        _roundDots[index].color = won == null  ? C(0.25f,0.30f,0.40f)
                                : won == true  ? CGREEN : CRED;
    }

    public void SetStopDot(int index, bool? correct)
    {
        if (_stopDots == null || index >= _stopDots.Length) return;
        _stopDots[index].color = correct == null  ? C(0.25f,0.30f,0.40f)
                               : correct == true  ? CGREEN : CRED;
    }

    public void ResetStopDots()
    {
        if (_stopDots == null) return;
        foreach (var d in _stopDots) d.color = C(0.25f, 0.30f, 0.40f);
    }

    public void SetRoundLabel(int current, int total)
    {
        if (_roundLabel) _roundLabel.text = $"Ronda {current}/{total}";
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

    public void ShowFinalResult(bool won, int roundsWon, int totalRounds, int rawScore, int finalScore)
    {
        string title = won ? "¡Control total!" : "El impulso ganó";
        Color  tcol  = won ? CGREEN : CRED;

        string msg = won
            ? $"Superaste {roundsWon} de {totalRounds} rondas.\n" +
              $"Puntuación: {finalScore} pts\n\n" +
              "Detener el impulso en el momento exacto es una habilidad.\n" +
              "¡La estás dominando!"
            : $"Rondas superadas: {roundsWon} de {totalRounds}\n\n" +
              "La zona se hace más pequeña con cada ronda.\n" +
              "Anticipa el movimiento y actúa con precisión.";

        _resultTitle.text  = title;
        _resultTitle.color = tcol;
        _resultSub.text    = msg;
        _resultPanel.SetActive(true);
    }

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

    static Sprite MakeArcSprite(int res, float startDeg, float spanDeg,
                                 float innerR, float outerR, Color color)
    {
        var tex = new Texture2D(res, res, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float cx = res * 0.5f, cy = res * 0.5f;
        float scale = (res * 0.5f) / (outerR + 4f);
        float rOut  = outerR * scale;
        float rIn   = innerR * scale;
        float startRad = NormRad(startDeg);
        float endRad   = NormRad(startDeg + spanDeg);

        var px = new Color[res * res];
        for (int py = 0; py < res; py++)
        for (int px2 = 0; px2 < res; px2++)
        {
            float dx = px2 - cx, dy = py - cy;
            float dist = Mathf.Sqrt(dx*dx + dy*dy);

            if (dist < rIn - 0.5f || dist > rOut + 0.5f)
                { px[py*res+px2] = Color.clear; continue; }

            float a = Mathf.Atan2(dx, dy);
            if (a < 0) a += 2f * Mathf.PI;

            bool inArc = endRad >= startRad
                ? a >= startRad && a <= endRad
                : a >= startRad || a <= endRad;

            if (!inArc) { px[py*res+px2] = Color.clear; continue; }

            float alpha = Mathf.Clamp01(dist - rIn + 1f) *
                          Mathf.Clamp01(rOut - dist + 1f);
            px[py*res+px2] = new Color(color.r, color.g, color.b, alpha);
        }
        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0,0,res,res), V(0.5f,0.5f));
    }

    static float NormRad(float deg)
    {
        float r = deg * Mathf.Deg2Rad % (2f * Mathf.PI);
        return r < 0 ? r + 2f * Mathf.PI : r;
    }

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

    TextMeshProUGUI MkTxt(RectTransform p, string n, string txt,
                           Color col, float sz, Vector2 am, Vector2 aM)
    {
        var go = new GameObject(n);
        go.transform.SetParent(p, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = am; rt.anchorMax = aM;
        rt.pivot = V(0.5f, 0.5f);
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
