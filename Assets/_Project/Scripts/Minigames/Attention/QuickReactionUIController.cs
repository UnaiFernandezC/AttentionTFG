// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Interfaz del minijuego Reaccion Rapida (Atencion).
/// Construida 100% por codigo con estetica espacial:
/// - Objetivo circular con glow pulsante y anillos concentricos al aparecer.
/// - Cuenta atras visual: circulo radial que se vacia.
/// - HUD flotante redondeado con la paleta amarilla de Atencion.
/// </summary>
public class QuickReactionUIController : MonoBehaviour
{

    static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);
    static Vector2 V(float x, float y) => new Vector2(x, y);

    // Paleta amarilla de la categoria Atencion
    static readonly Color ACCENT  = C(0.98f, 0.80f, 0.10f);
    static readonly Color PANEL   = C(0.10f, 0.13f, 0.24f, 0.94f);
    static readonly Color PANEL2  = C(0.07f, 0.10f, 0.20f, 0.88f);
    static readonly Color DIM2    = C(0.55f, 0.65f, 0.85f);
    static readonly Color CGREEN  = C(0.25f, 0.90f, 0.52f);
    static readonly Color CRED    = C(0.90f, 0.28f, 0.30f);
    static readonly Color CYELLOW = C(0.96f, 0.72f, 0.18f);

    Image           _stimGlow1, _stimGlow2, _stimCore, _stimShine;
    RectTransform   _stimRoot;
    TextMeshProUGUI _stimLabel;

    Image           _countdownRing;
    Image           _countdownFill;

    TextMeshProUGUI _statusLbl;
    TextMeshProUGUI _timeLbl;
    Image[]         _roundDots;

    GameObject      _resultPanel;
    TextMeshProUGUI _resultTitle, _resultSub;

    int  _totalRounds;
    bool _trapMode;
    int  _streak;   // aciertos seguidos (solo para la celebracion visual de racha)

    public void BuildUI(int rounds, Action onRestart, Action onMenu)
    {
        _totalRounds = rounds;

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

        // Fondo espacial coherente (nebulosas + estrellas + planeta)
        KidUI.BuildSpaceBackground(R);

        // Cabecera flotante redondeada con acento amarillo
        var hdr = Pill(R, "Hdr", PANEL, V(0.015f, 0.925f), V(0.985f, 0.988f), 1.3f);
        var hdrLine = Pill(hdr, "Line", ACCENT, V(0f, 0f), V(1f, 0f), 4f);
        hdrLine.anchoredPosition = V(0f, 2f);
        hdrLine.sizeDelta        = V(-30f, 4f);
        hdrLine.GetComponent<Image>().raycastTarget = false;
        var ttl = MkTxt(hdr, "T", "REACCIÓN RÁPIDA", Color.white, 34, V(0.02f, 0.12f), V(0.60f, 0.88f));
        ttl.fontStyle = FontStyles.Bold; ttl.alignment = TextAlignmentOptions.MidlineLeft; ttl.characterSpacing = 2f;
        var cat = MkTxt(hdr, "Cat", "ATENCIÓN", ACCENT, 17, V(0.60f, 0.12f), V(0.98f, 0.88f));
        cat.alignment = TextAlignmentOptions.MidlineRight; cat.characterSpacing = 3f;
        UITween.PopIn(hdr, 0.45f, 0.90f);

        BuildRoundDots(R, rounds);

        BuildStimulus(R);

        _statusLbl = MkTxt(R, "Status", "Espera el estímulo…", DIM2, 28,
                           V(0.10f, 0.16f), V(0.90f, 0.26f));
        _statusLbl.alignment = TextAlignmentOptions.Center;

        _timeLbl = MkTxt(R, "Time", "", ACCENT, 48, V(0.25f, 0.26f), V(0.75f, 0.38f));
        _timeLbl.fontStyle = FontStyles.Bold; _timeLbl.alignment = TextAlignmentOptions.Center;

        // Pastilla inferior de instruccion
        var bot = Pill(R, "Bot", PANEL, V(0.10f, 0.014f), V(0.90f, 0.072f), 1.4f);
        KidUI.CircleAt(bot, "BotDot", ACCENT, V(0.035f, 0.5f), 14f)
             .GetComponent<Image>().raycastTarget = false;
        MkTxt(bot, "Instr", "Haz click (o pulsa ESPACIO) cuando el círculo se ponga verde.",
              C(0.95f, 0.92f, 0.75f), 19, V(0.06f, 0), V(0.97f, 1)).alignment = TextAlignmentOptions.MidlineLeft;
        UITween.PopIn(bot, 0.45f, 0.90f, 0.08f);

        BuildResultPanel(R, onRestart, onMenu);
    }

    void BuildStimulus(RectTransform R)
    {

        var go = new GameObject("Stimulus");
        go.transform.SetParent(R, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = new Vector2(0, 20f);
        go.AddComponent<Image>().color = Color.clear;
        _stimRoot = rt;

        _countdownRing = AddCircleLayer(go.transform, "CDRing", new Vector2(196f, 196f),
                                        C(1, 1, 1, 0.08f));
        _countdownRing.type       = Image.Type.Filled;
        _countdownRing.fillMethod = Image.FillMethod.Radial360;
        _countdownRing.fillOrigin = (int)Image.Origin360.Top;
        _countdownRing.fillClockwise = true;
        _countdownRing.fillAmount = 1f;

        _countdownFill = AddCircleLayer(go.transform, "CDFill", new Vector2(196f, 196f),
                                        CGREEN);
        _countdownFill.type          = Image.Type.Filled;
        _countdownFill.fillMethod    = Image.FillMethod.Radial360;
        _countdownFill.fillOrigin    = (int)Image.Origin360.Top;
        _countdownFill.fillClockwise = true;
        _countdownFill.fillAmount    = 1f;
        _countdownFill.gameObject.SetActive(false);

        _stimGlow1 = AddCircleLayer(go.transform, "SG1", new Vector2(320f, 320f), C(CRED.r, CRED.g, CRED.b, 0.06f));
        _stimGlow2 = AddCircleLayer(go.transform, "SG2", new Vector2(220f, 220f), C(CRED.r, CRED.g, CRED.b, 0.14f));
        _stimCore  = AddCircleLayer(go.transform, "SC",  new Vector2(140f, 140f), CRED);
        _stimShine = AddCircleLayer(go.transform, "SS",  new Vector2( 42f,  42f), C(1, 1, 1, 0.40f));
        _stimShine.rectTransform.anchoredPosition = new Vector2(-34f, 38f);

        _stimLabel = MkTxt(R, "StimLbl", "ESPERA…", C(1, 1, 1, 0.55f), 22,
                           V(0.30f, 0.46f), V(0.70f, 0.55f));
        _stimLabel.fontStyle = FontStyles.Bold; _stimLabel.alignment = TextAlignmentOptions.Center;

        // Entrada juicy del objetivo
        UITween.PopIn(rt, 0.55f, 0.70f, 0.10f);
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
            img.sprite = KidUI.CircleSpr;
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

        var card = Pill(er, "Card", PANEL, V(0.5f, 0.5f), V(0.5f, 0.5f), 1.0f);
        card.sizeDelta = V(780f, 400f);
        var lineT = Pill(card, "LineT", ACCENT, V(0f, 1f), V(1f, 1f), 4f);
        lineT.anchoredPosition = V(0f, -5f);
        lineT.sizeDelta        = V(-60f, 7f);
        lineT.GetComponent<Image>().raycastTarget = false;

        _resultTitle = MkTxt(card, "RT", "", Color.white, 52, V(0.05f, 0.72f), V(0.95f, 0.97f));
        _resultTitle.fontStyle = FontStyles.Bold;
        _resultSub   = MkTxt(card, "RS", "", C(0.95f, 0.90f, 0.65f), 24, V(0.05f, 0.26f), V(0.95f, 0.70f));
        _resultSub.overflowMode = TextOverflowModes.Overflow;

        MkBtn(card, "Jugar de nuevo",      C(0.85f, 0.66f, 0.05f), V(0.05f, 0.20f), V(0.48f, 0.34f), onRestart);
        MkBtn(card, "Volver a la seccion", C(0.18f,0.24f,0.38f),   V(0.52f, 0.20f), V(0.95f, 0.34f), onMenu);
        MkBtn(card, "Menu principal",      C(0.10f,0.13f,0.22f),   V(0.05f, 0.04f), V(0.95f, 0.17f), () => SceneLoader.GoToMainMenu());

        _resultPanel.SetActive(false);
    }

    public void SetWaiting()
    {
        _trapMode = false;
        SetStimulusColor(CRED, false);
        _stimLabel.text  = "ESPERA…";
        _stimLabel.color = C(1, 1, 1, 0.40f);
        _statusLbl.text  = "Espera el estímulo…";
        _statusLbl.color = DIM2;
        _timeLbl.text    = "";

        if (_countdownFill != null) _countdownFill.gameObject.SetActive(false);
        if (_countdownRing != null) _countdownRing.color = C(1, 1, 1, 0.08f);
    }

    public void SetStimulus(float timeLimit)
    {
        _trapMode = false;
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

        // Aparicion inequivoca: pop del nucleo + anillos concentricos
        if (_stimCore != null) UITween.PulseOnce(_stimCore.rectTransform, 1.18f, 0.25f);
        SpawnRings(CGREEN);
    }

    /// <summary>Ronda trampa (dificil): circulo AMARILLO, hay que aguantar sin pulsar.</summary>
    public void SetTrapStimulus()
    {
        _trapMode = true;
        SetStimulusColor(CYELLOW, true);
        _stimLabel.text  = "¡NO PULSES!";
        _stimLabel.color = Color.white;
        _statusLbl.text  = "¡Amarillo! Aguanta sin pulsar...";
        _statusLbl.color = CYELLOW;
        _timeLbl.text    = "";

        if (_countdownFill != null)
        {
            _countdownFill.gameObject.SetActive(true);
            _countdownFill.fillAmount = 1f;
            _countdownFill.color      = CYELLOW;
        }
        if (_countdownRing != null)
            _countdownRing.color = C(1, 1, 1, 0.12f);

        if (_stimCore != null) UITween.PulseOnce(_stimCore.rectTransform, 1.18f, 0.25f);
        SpawnRings(CYELLOW);
    }

    /// <summary>Resultado de una ronda trampa.</summary>
    public void ShowTrapResult(bool resisted)
    {
        if (_countdownFill != null) _countdownFill.gameObject.SetActive(false);

        if (resisted)
        {
            SetStimulusColor(CGREEN, true);
            _stimLabel.text  = "¡Aguantaste!";
            _stimLabel.color = Color.white;
            _statusLbl.text  = "¡Muy bien! Era una trampa";
            _statusLbl.color = CGREEN;
            SpawnRings(CGREEN);
            SpawnBurst(CGREEN, 12);   // rafaga de particulas: resistio la trampa
        }
        else
        {
            SetStimulusColor(CRED, false);
            _stimLabel.text  = "X";
            _stimLabel.color = CRED;
            _statusLbl.text  = "Era trampa, no habia que pulsar";
            _statusLbl.color = CRED;
        }
        _timeLbl.text = "";
    }

    public void UpdateCountdown(float elapsed, float total)
    {
        if (_countdownFill == null || total <= 0f) return;

        float t = Mathf.Clamp01(1f - elapsed / total);
        _countdownFill.fillAmount = t;

        Color col;
        if (t > 0.5f)
            col = Color.Lerp(CYELLOW, CGREEN,   (t - 0.5f) * 2f);
        else
            col = Color.Lerp(CRED,    CYELLOW,  t * 2f);
        _countdownFill.color = col;

        if (_trapMode)
        {
            // En rondas trampa la barra baja pero el mensaje no cambia
            _countdownFill.color = CYELLOW;
            return;
        }

        int secsLeft = Mathf.CeilToInt(total - elapsed);
        if (_stimLabel != null && secsLeft > 0)
        {
            _stimLabel.text  = secsLeft.ToString();
            _stimLabel.color = col;
        }
    }

    public void ShowRoundResult(bool tooEarly, bool timeout, long reactionMs, string evalMsg)
    {
        if (_countdownFill != null) _countdownFill.gameObject.SetActive(false);

        if (tooEarly)
        {
            SetStimulusColor(CYELLOW, false);
            _stimLabel.text  = "¡Pronto!";
            _stimLabel.color = CYELLOW;
            _statusLbl.text  = "¡Espera al verde!";
            _statusLbl.color = CYELLOW;
            _timeLbl.text    = "";
            if (_stimCore != null) GameFeel.Shake(_stimCore.rectTransform, 10f, 0.30f);
        }
        else if (timeout)
        {
            SetStimulusColor(CRED, false);
            _stimLabel.text  = "X";
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
            UITween.PulseOnce(_timeLbl.rectTransform, 1.15f, 0.28f);
            SpawnRings(CGREEN);
            SpawnBurst(CGREEN, 14);   // rafaga de particulas al acertar
        }
    }

    /// <summary>Rafaga de particulas circulares que salen despedidas del objetivo.</summary>
    void SpawnBurst(Color col, int count = 14)
    {
        if (_stimRoot == null) return;
        StartCoroutine(BurstCo(col, count));
    }

    IEnumerator BurstCo(Color col, int count)
    {
        var rts  = new RectTransform[count];
        var imgs = new Image[count];
        var dirs = new Vector2[count];
        for (int i = 0; i < count; i++)
        {
            var go = new GameObject("Burst");
            go.transform.SetParent(_stimRoot, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot            = new Vector2(0.5f, 0.5f);
            float s             = UnityEngine.Random.Range(8f, 18f);
            rt.sizeDelta        = new Vector2(s, s);
            rt.anchoredPosition = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.sprite        = KidUI.CircleSpr;
            img.color         = col;
            img.raycastTarget = false;
            float ang = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            dirs[i]   = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) *
                        UnityEngine.Random.Range(120f, 260f);
            rts[i] = rt; imgs[i] = img;
        }
        float t = 0f, dur = 0.60f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float p    = Mathf.Clamp01(t / dur);
            float ease = 1f - (1f - p) * (1f - p);   // frenada suave hacia fuera
            for (int i = 0; i < count; i++)
            {
                if (rts[i] == null) continue;
                rts[i].anchoredPosition = dirs[i] * ease;
                imgs[i].color = C(col.r, col.g, col.b, 1f - p);
            }
            yield return null;
        }
        for (int i = 0; i < count; i++)
            if (rts[i] != null) Destroy(rts[i].gameObject);
    }

    public void SetRoundDot(int roundIndex, bool correct)
    {
        if (roundIndex < 0 || roundIndex >= _roundDots.Length) return;
        _roundDots[roundIndex].color = correct ? CGREEN : CRED;
        UITween.PulseOnce(_roundDots[roundIndex].rectTransform, 1.35f, 0.28f);

        // Celebracion de racha (solo visual): 2+ aciertos seguidos
        if (correct)
        {
            _streak++;
            if (_streak >= 2)
            {
                GameFeel.PlayStar();
                GameFeel.FloatingText("¡Racha x" + _streak + "!", ACCENT,
                                      new Vector2(0f, 330f), 46f);
                SpawnRings(ACCENT);
            }
        }
        else
        {
            _streak = 0;
        }
    }

    public void PulseGlow(float t)
    {
        if (_stimGlow1 == null) return;
        // Glow pulsante en dos capas (respiracion del objetivo)
        float s = 1f + 0.05f * Mathf.Sin(t * 4f);
        _stimGlow1.rectTransform.localScale = Vector3.one * s;
        if (_stimGlow2 != null)
        {
            float s2 = 1f + 0.035f * Mathf.Sin(t * 4f + 1.2f);
            _stimGlow2.rectTransform.localScale = Vector3.one * s2;
        }
    }

    public void ShowFinalResult(bool win, string sub)
    {
        _resultTitle.text  = win ? "¡Bien hecho!" : "Fin del juego";
        _resultTitle.color = win ? CGREEN : CRED;
        _resultSub.text    = sub;
        _resultPanel.SetActive(true);
    }

    void SetStimulusColor(Color core, bool bright)
    {
        float glow1A = bright ? 0.10f : 0.06f;
        float glow2A = bright ? 0.22f : 0.14f;
        if (_stimCore  != null) _stimCore.color  = core;
        if (_stimGlow1 != null) _stimGlow1.color = C(core.r, core.g, core.b, glow1A);
        if (_stimGlow2 != null) _stimGlow2.color = C(core.r, core.g, core.b, glow2A);
    }

    /// <summary>Tres anillos concentricos que se expanden y desvanecen.</summary>
    void SpawnRings(Color col)
    {
        if (_stimRoot == null) return;
        for (int i = 0; i < 3; i++)
            StartCoroutine(RingCo(col, i * 0.09f));
    }

    IEnumerator RingCo(Color col, float delay)
    {
        if (delay > 0f) yield return new WaitForSecondsRealtime(delay);
        if (_stimRoot == null) yield break;

        var go = new GameObject("RingFX");
        go.transform.SetParent(_stimRoot, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(150f, 150f);
        rt.anchoredPosition = Vector2.zero;
        var img = go.AddComponent<Image>();
        img.sprite        = KidUI.CircleSpr;
        img.raycastTarget = false;

        float t = 0f, dur = 0.55f;
        while (t < dur)
        {
            if (rt == null) yield break;
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / dur);
            rt.localScale = Vector3.one * (1f + p * 1.9f);
            img.color     = C(col.r, col.g, col.b, 0.35f * (1f - p));
            yield return null;
        }
        if (go != null) Destroy(go);
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
        img.sprite        = KidUI.CircleSpr;   // circulo real antialiasado
        img.color         = col;
        img.raycastTarget = false;
        return img;
    }

    /// <summary>Pastilla redondeada (Image con sprite 9-slice de KidUI).</summary>
    RectTransform Pill(RectTransform p, string n, Color col,
                       Vector2 am, Vector2 aM, float cornerScale)
    {
        var rt  = MkImg(p, n, col, am, aM, V(0, 0), V(0, 0));
        var img = rt.GetComponent<Image>();
        img.sprite                  = KidUI.RoundedSprite;
        img.type                    = Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = cornerScale;
        return rt;
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
        var rt = Pill(p, "Btn_" + lbl, bg, am, aM, 1.2f);
        var b = rt.gameObject.AddComponent<Button>(); b.targetGraphic = rt.GetComponent<Image>();
        var cb = b.colors;
        cb.normalColor      = Color.white;
        cb.highlightedColor = new Color(1, 1, 1, .82f);
        cb.pressedColor     = new Color(.72f, .72f, .72f);
        b.colors = cb;
        b.onClick.AddListener(() => click?.Invoke());
        var t = MkTxt(rt, "T", lbl, Color.white, 24, V(0, 0), V(1, 1));
        t.fontStyle = FontStyles.Bold;
        ButtonJuice.Attach(rt.gameObject);
    }
}
