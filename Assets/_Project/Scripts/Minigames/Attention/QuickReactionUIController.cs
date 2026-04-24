using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Construye toda la UI del minijuego "Reacción rápida" de forma procedural.
/// No requiere prefabs ni assets externos.
///
/// Layout:
///   • Header fijo con título y categoría
///   • Zona central: círculo de estímulo (ROJO en espera → VERDE al reaccionar)
///     + arco radial de cuenta atrás que depleta mientras corre el tiempo
///   • Indicadores de ronda (puntitos)
///   • Label de estado + tiempo de reacción en ms
///   • Barra inferior: instrucciones + botón menú
///   • Panel de resultado final (overlay)
/// </summary>
public class QuickReactionUIController : MonoBehaviour
{
    // ------------------------------------------------------------------ //
    // Paleta
    // ------------------------------------------------------------------ //
    static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);
    static Vector2 V(float x, float y) => new Vector2(x, y);

    static readonly Color BG      = C(0.08f, 0.10f, 0.16f);
    static readonly Color HDR     = C(0.05f, 0.08f, 0.15f);
    static readonly Color PANEL   = C(0.08f, 0.12f, 0.22f);
    static readonly Color ACCENT  = C(0.40f, 0.72f, 1.00f);
    static readonly Color DIM2    = C(0.30f, 0.42f, 0.62f);
    static readonly Color CGREEN  = C(0.25f, 0.90f, 0.52f);
    static readonly Color CRED    = C(0.90f, 0.28f, 0.30f);
    static readonly Color CYELLOW = C(0.96f, 0.72f, 0.18f);

    // ────────────────────────────────────────────────────────────────────
    // Referencias internas
    // ────────────────────────────────────────────────────────────────────

    // Círculo de estímulo
    Image           _stimGlow1, _stimGlow2, _stimCore, _stimShine;
    TextMeshProUGUI _stimLabel;          // "ESPERA…" / "¡YA!" / "3" "2" "1"

    // Arco de cuenta atrás (Image.Type.Filled radial, alrededor del círculo)
    Image           _countdownRing;      // pista exterior vacía (dim)
    Image           _countdownFill;      // arco que se depleta

    // Info de ronda
    TextMeshProUGUI _statusLbl;
    TextMeshProUGUI _timeLbl;
    Image[]         _roundDots;

    // Resultado final
    GameObject      _resultPanel;
    TextMeshProUGUI _resultTitle, _resultSub;

    int _totalRounds;

    // ════════════════════════════════════════════════════════════════════
    // API pública — construcción
    // ════════════════════════════════════════════════════════════════════

    public void BuildUI(int rounds, Action onRestart, Action onMenu)
    {
        _totalRounds = rounds;

        // ── Canvas principal ──────────────────────────────────────────
        var cGO = new GameObject("Canvas_QuickReaction");
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

        // Fondo + gradiente
        MkImg(R, "BG",    BG,                                V(0, 0),     V(1, 1),     V(0, 0), V(0, 0));
        MkImg(R, "GradT", C(0.10f, 0.20f, 0.38f, 0.28f),    V(0, 0.70f), V(1, 1),     V(0, 0), V(0, 0));

        // ── Header ────────────────────────────────────────────────────
        var hdr = MkImg(R, "Hdr", HDR, V(0, 1), V(1, 1), V(0, -44), V(0, 88));
        MkImg(hdr, "Line", ACCENT, V(0, 0),      V(1, 0),      V(0, 1.5f), V(0, 3));
        MkImg(hdr, "AccL", ACCENT, V(0, 0.18f),  V(0, 0.82f),  V(3, 0),    V(6, 0));
        var ttl = MkTxt(hdr, "T", "REACCIÓN RÁPIDA", Color.white, 35, V(0.03f, 0.12f), V(0.60f, 0.88f));
        ttl.fontStyle = FontStyles.Bold; ttl.alignment = TextAlignmentOptions.MidlineLeft; ttl.characterSpacing = 2f;
        MkTxt(hdr, "Cat", "ATENCIÓN", DIM2, 16, V(0.60f, 0.12f), V(0.97f, 0.88f)).alignment = TextAlignmentOptions.MidlineRight;

        // ── Indicadores de ronda ──────────────────────────────────────
        BuildRoundDots(R, rounds);

        // ── Círculo de estímulo + arco cuenta atrás ───────────────────
        BuildStimulus(R);

        // ── Status / tiempo ───────────────────────────────────────────
        _statusLbl = MkTxt(R, "Status", "Espera el estímulo…", DIM2, 28,
                           V(0.10f, 0.16f), V(0.90f, 0.26f));
        _statusLbl.alignment = TextAlignmentOptions.Center;

        _timeLbl = MkTxt(R, "Time", "", ACCENT, 48, V(0.25f, 0.26f), V(0.75f, 0.38f));
        _timeLbl.fontStyle = FontStyles.Bold; _timeLbl.alignment = TextAlignmentOptions.Center;

        // ── Barra inferior ────────────────────────────────────────────
        var bot = MkImg(R, "Bot", HDR, V(0, 0), V(1, 0), V(0, 40), V(0, 80));
        MkImg(bot, "BotLine", ACCENT, V(0, 1), V(1, 1), V(0, -1.5f), V(0, 3));
        MkTxt(bot, "Instr", "Haz click (o pulsa ESPACIO) cuando el círculo se ponga verde.",
              C(ACCENT.r + 0.10f, ACCENT.g + 0.10f, ACCENT.b + 0.10f, 1f),
              19, V(0.01f, 0), V(0.78f, 1)).alignment = TextAlignmentOptions.MidlineLeft;
        MkImg(bot, "Sep", C(1, 1, 1, 0.10f), V(0.78f, 0.1f), V(0.782f, 0.9f), V(0, 0), V(0, 0));
        MkBtn(bot, "Menu", C(0.12f, 0.20f, 0.36f), V(0.80f, 0.08f), V(0.99f, 0.92f), onMenu);

        // ── Panel resultado final ─────────────────────────────────────
        BuildResultPanel(R, onRestart, onMenu);
    }

    // ────────────────────────────────────────────────────────────────────
    // Construcción de piezas
    // ────────────────────────────────────────────────────────────────────

    void BuildStimulus(RectTransform R)
    {
        // Contenedor centrado
        var go = new GameObject("Stimulus");
        go.transform.SetParent(R, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = new Vector2(0, 20f);
        go.AddComponent<Image>().color = Color.clear;

        // ── Arco de cuenta atrás (detrás del círculo) ─────────────────
        // Pista (fondo del arco, dim)
        _countdownRing = AddCircleLayer(go.transform, "CDRing", new Vector2(196f, 196f),
                                        C(1, 1, 1, 0.08f));
        _countdownRing.type       = Image.Type.Filled;
        _countdownRing.fillMethod = Image.FillMethod.Radial360;
        _countdownRing.fillOrigin = (int)Image.Origin360.Top;
        _countdownRing.fillClockwise = true;
        _countdownRing.fillAmount = 1f;

        // Relleno activo (el que se depleta)
        _countdownFill = AddCircleLayer(go.transform, "CDFill", new Vector2(196f, 196f),
                                        CGREEN);
        _countdownFill.type          = Image.Type.Filled;
        _countdownFill.fillMethod    = Image.FillMethod.Radial360;
        _countdownFill.fillOrigin    = (int)Image.Origin360.Top;
        _countdownFill.fillClockwise = true;
        _countdownFill.fillAmount    = 1f;
        _countdownFill.gameObject.SetActive(false); // oculto hasta que aparezca el estímulo

        // ── Capas del círculo ─────────────────────────────────────────
        _stimGlow1 = AddCircleLayer(go.transform, "SG1", new Vector2(320f, 320f), C(CRED.r, CRED.g, CRED.b, 0.06f));
        _stimGlow2 = AddCircleLayer(go.transform, "SG2", new Vector2(220f, 220f), C(CRED.r, CRED.g, CRED.b, 0.14f));
        _stimCore  = AddCircleLayer(go.transform, "SC",  new Vector2(140f, 140f), CRED);
        _stimShine = AddCircleLayer(go.transform, "SS",  new Vector2( 42f,  42f), C(1, 1, 1, 0.40f));
        _stimShine.rectTransform.anchoredPosition = new Vector2(-34f, 38f);

        // Label sobre el círculo
        _stimLabel = MkTxt(R, "StimLbl", "ESPERA…", C(1, 1, 1, 0.55f), 22,
                           V(0.30f, 0.46f), V(0.70f, 0.55f));
        _stimLabel.fontStyle = FontStyles.Bold; _stimLabel.alignment = TextAlignmentOptions.Center;
    }

    void BuildRoundDots(RectTransform R, int rounds)
    {
        _roundDots = new Image[rounds];
        float totalW = rounds * 28f + (rounds - 1) * 12f;
        float startX = -totalW / 2f;
        float dotY   = 400f;

        var dotsGO = new GameObject("RoundDots");
        dotsGO.transform.SetParent(R, false);
        var dotsRT = dotsGO.AddComponent<RectTransform>();
        dotsRT.anchorMin = dotsRT.anchorMax = new Vector2(0.5f, 0.5f);
        dotsRT.pivot     = new Vector2(0.5f, 0.5f);
        dotsRT.sizeDelta = Vector2.zero;
        dotsRT.anchoredPosition = new Vector2(0, dotY);

        for (int i = 0; i < rounds; i++)
        {
            var dGO = new GameObject($"Dot{i}");
            dGO.transform.SetParent(dotsRT, false);
            var dRT = dGO.AddComponent<RectTransform>();
            dRT.anchorMin = dRT.anchorMax = new Vector2(0.5f, 0.5f);
            dRT.pivot     = new Vector2(0.5f, 0.5f);
            dRT.sizeDelta = new Vector2(24f, 24f);
            dRT.anchoredPosition = new Vector2(startX + i * 40f, 0);
            var img = dGO.AddComponent<Image>();
            img.color = C(1, 1, 1, 0.18f);
            img.raycastTarget = false;
            _roundDots[i] = img;
        }
    }

    void BuildResultPanel(RectTransform R, Action onRestart, Action onMenu)
    {
        _resultPanel = new GameObject("ResultPanel");
        _resultPanel.transform.SetParent(R, false);
        var er = _resultPanel.AddComponent<RectTransform>();
        er.anchorMin = Vector2.zero; er.anchorMax = Vector2.one;
        er.sizeDelta = Vector2.zero; er.anchoredPosition = Vector2.zero;
        _resultPanel.AddComponent<Image>().color = C(0, 0, 0, 0.85f);

        var card = MkImg(er, "Card", PANEL, V(0.5f, 0.5f), V(0.5f, 0.5f), V(0, 0), V(780f, 400f));
        MkImg(card, "Sh",    C(1, 1, 1, 0.03f), V(0, 0.5f),   V(1, 1),    V(0, 0),  V(0, 0));
        MkImg(card, "LineT", ACCENT,             V(0, 1),      V(1, 1),    V(0, -4), V(0, 8));
        MkImg(card, "AccL",  ACCENT,             V(0, 0.08f),  V(0, 0.92f),V(4, 0),  V(8, 0));

        _resultTitle = MkTxt(card, "RT", "", Color.white, 52, V(0.05f, 0.72f), V(0.95f, 0.97f));
        _resultTitle.fontStyle = FontStyles.Bold;
        _resultSub   = MkTxt(card, "RS", "", C(0.48f, 0.62f, 0.80f), 24, V(0.05f, 0.26f), V(0.95f, 0.70f));
        _resultSub.overflowMode = TextOverflowModes.Overflow;

        MkBtn(card, "Jugar de nuevo", ACCENT,                V(0.05f, 0.05f), V(0.46f, 0.18f), onRestart);
        MkBtn(card, "Menu",           C(0.14f, 0.22f, 0.38f), V(0.54f, 0.05f), V(0.95f, 0.18f), onMenu);

        _resultPanel.SetActive(false);
    }

    // ════════════════════════════════════════════════════════════════════
    // API pública — estado de juego
    // ════════════════════════════════════════════════════════════════════

    /// <summary>Cambia el círculo a modo "espera" (rojo). Oculta el arco.</summary>
    public void SetWaiting()
    {
        SetStimulusColor(CRED, false);
        _stimLabel.text  = "ESPERA…";
        _stimLabel.color = C(1, 1, 1, 0.40f);
        _statusLbl.text  = "Espera el estímulo…";
        _statusLbl.color = DIM2;
        _timeLbl.text    = "";

        if (_countdownFill != null) _countdownFill.gameObject.SetActive(false);
        if (_countdownRing != null) _countdownRing.color = C(1, 1, 1, 0.08f);
    }

    /// <summary>Activa el estímulo visual (verde) y muestra el arco de cuenta atrás.</summary>
    public void SetStimulus(float timeLimit)
    {
        SetStimulusColor(CGREEN, true);
        _stimLabel.text  = "¡YA!";
        _stimLabel.color = Color.white;
        _statusLbl.text  = "¡Haz click ahora!";
        _statusLbl.color = CGREEN;
        _timeLbl.text    = "";

        if (_countdownFill != null)
        {
            _countdownFill.gameObject.SetActive(true);
            _countdownFill.fillAmount = 1f;
            _countdownFill.color      = CGREEN;
        }
        if (_countdownRing != null)
            _countdownRing.color = C(1, 1, 1, 0.12f);
    }

    /// <summary>
    /// Actualiza el arco radial de cuenta atrás.
    /// Llamar cada frame mientras el estímulo está activo.
    /// </summary>
    public void UpdateCountdown(float elapsed, float total)
    {
        if (_countdownFill == null || total <= 0f) return;

        float t = Mathf.Clamp01(1f - elapsed / total);
        _countdownFill.fillAmount = t;

        // Color: verde → amarillo → rojo conforme el tiempo se acaba
        Color col;
        if (t > 0.5f)
            col = Color.Lerp(CYELLOW, CGREEN,   (t - 0.5f) * 2f);
        else
            col = Color.Lerp(CRED,    CYELLOW,  t * 2f);
        _countdownFill.color = col;

        // Número de segundos restantes dentro del label del círculo
        int secsLeft = Mathf.CeilToInt(total - elapsed);
        if (_stimLabel != null && secsLeft > 0)
        {
            _stimLabel.text  = secsLeft.ToString();
            _stimLabel.color = col;
        }
    }

    /// <summary>Muestra el resultado de una ronda individual.</summary>
    public void ShowRoundResult(bool tooEarly, bool timeout, long reactionMs, string evalMsg)
    {
        if (_countdownFill != null) _countdownFill.gameObject.SetActive(false);

        if (tooEarly)
        {
            SetStimulusColor(CYELLOW, false);
            _stimLabel.text  = "¡Pronto!";
            _stimLabel.color = CYELLOW;
            _statusLbl.text  = "Demasiado pronto";
            _statusLbl.color = CYELLOW;
            _timeLbl.text    = "";
        }
        else if (timeout)
        {
            SetStimulusColor(CRED, false);
            _stimLabel.text  = "✗";
            _stimLabel.color = CRED;
            _statusLbl.text  = "¡Tiempo agotado!";
            _statusLbl.color = CRED;
            _timeLbl.text    = "";
        }
        else
        {
            SetStimulusColor(CGREEN, true);
            _stimLabel.text  = evalMsg;
            _stimLabel.color = Color.white;
            _statusLbl.text  = evalMsg;
            _statusLbl.color = CGREEN;
            _timeLbl.text    = reactionMs + " ms";
            _timeLbl.color   = Color.Lerp(CGREEN, CYELLOW, Mathf.InverseLerp(200f, 700f, reactionMs));
        }
    }

    /// <summary>Actualiza el dot de la ronda indicada.</summary>
    public void SetRoundDot(int roundIndex, bool correct)
    {
        if (roundIndex < 0 || roundIndex >= _roundDots.Length) return;
        _roundDots[roundIndex].color = correct ? CGREEN : CRED;
    }

    /// <summary>Pulso de escala en el glow exterior.</summary>
    public void PulseGlow(float t)
    {
        if (_stimGlow1 == null) return;
        float s = 1f + 0.05f * Mathf.Sin(t * 4f);
        _stimGlow1.rectTransform.localScale = Vector3.one * s;
    }

    /// <summary>Muestra el panel de resultado final.</summary>
    public void ShowFinalResult(bool win, string sub)
    {
        _resultTitle.text  = win ? "¡Bien hecho!" : "Fin del juego";
        _resultTitle.color = win ? CGREEN : CRED;
        _resultSub.text    = sub;
        _resultPanel.SetActive(true);
    }

    // ════════════════════════════════════════════════════════════════════
    // Helpers internos
    // ════════════════════════════════════════════════════════════════════

    void SetStimulusColor(Color core, bool bright)
    {
        float glow1A = bright ? 0.10f : 0.06f;
        float glow2A = bright ? 0.22f : 0.14f;
        if (_stimCore  != null) _stimCore.color  = core;
        if (_stimGlow1 != null) _stimGlow1.color = C(core.r, core.g, core.b, glow1A);
        if (_stimGlow2 != null) _stimGlow2.color = C(core.r, core.g, core.b, glow2A);
    }

    Image AddCircleLayer(Transform parent, string name, Vector2 size, Color col)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = Vector2.zero;
        var img = go.AddComponent<Image>();
        img.color         = col;
        img.raycastTarget = false;
        return img;
    }

    RectTransform MkImg(RectTransform p, string n, Color col, Vector2 am, Vector2 aM, Vector2 pos, Vector2 sd)
    {
        var go = new GameObject(n); go.transform.SetParent(p, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = am; rt.anchorMax = aM; rt.pivot = new Vector2(.5f, .5f);
        rt.anchoredPosition = pos; rt.sizeDelta = sd;
        go.AddComponent<Image>().color = col;
        return rt;
    }

    TextMeshProUGUI MkTxt(RectTransform p, string n, string txt, Color col, float sz, Vector2 am, Vector2 aM)
    {
        var go = new GameObject(n); go.transform.SetParent(p, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = am; rt.anchorMax = aM; rt.pivot = new Vector2(.5f, .5f);
        rt.anchoredPosition = Vector2.zero; rt.sizeDelta = Vector2.zero;
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = txt; t.color = col; t.fontSize = sz;
        t.alignment = TextAlignmentOptions.Center; t.overflowMode = TextOverflowModes.Overflow;
        return t;
    }

    void MkBtn(RectTransform p, string lbl, Color bg, Vector2 am, Vector2 aM, Action click)
    {
        var rt = MkImg(p, "Btn_" + lbl, bg, am, aM, V(0, 0), V(0, 0));
        MkImg(rt, "Sh", C(1, 1, 1, .09f), V(0, .5f), V(1, 1), V(0, 0), V(0, 0));
        var b = rt.gameObject.AddComponent<Button>(); b.targetGraphic = rt.GetComponent<Image>();
        var cb = b.colors;
        cb.normalColor      = Color.white;
        cb.highlightedColor = new Color(1, 1, 1, .82f);
        cb.pressedColor     = new Color(.72f, .72f, .72f);
        b.colors = cb;
        b.onClick.AddListener(() => click?.Invoke());
        var t = MkTxt(rt, "T", lbl, Color.white, 24, V(0, 0), V(1, 1));
        t.fontStyle = FontStyles.Bold;
    }
}
