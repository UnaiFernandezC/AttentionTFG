using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Minijuego "Reduce el estres" — Gestion Emocional.
///
/// DOS mecanicas:
///  1. Ritmo de respiracion: orbe se expande (INHALA → mantén) / contrae (EXHALA → suelta).
///  2. Burbujas de tension: aparecen flotando, hay que hacerles clic para reventarlas.
///
/// Inspector:
///   Facil   → startStress=75,  inhaleRate=14, exhaleRate=4, passiveRate=0
///   Medio   → startStress=100, inhaleRate=10, exhaleRate=3, passiveRate=2
///   Dificil → startStress=100, inhaleRate=7,  exhaleRate=2, passiveRate=4
/// </summary>
public class ReduceStressController : MinigameBase
{
    // ─── Inspector ────────────────────────────────────────────────────────
    [Header("Nivel de estres inicial (0-100)")]   public float startStress  = 75f;
    [Header("Reduccion al respirar bien (u/s)")]  public float inhaleRate   = 14f;
    [Header("Reduccion automatica exhale (u/s)")] public float exhaleRate   = 4f;
    [Header("Reduccion por burbuja")]             public float bubbleReduce = 6f;
    [Header("Aumento pasivo (u/s)")]              public float passiveRate  = 0f;

    // ─── Ciclo de respiracion ────────────────────────────────────────────
    const float INHALE_DUR = 3.0f;
    const float EXHALE_DUR = 4.0f;
    float CycleLen => INHALE_DUR + EXHALE_DUR;
    float _cycleTimer;
    bool  InInhale => _cycleTimer < INHALE_DUR;
    int   _cyclesCompleted;

    // ─── Estado ──────────────────────────────────────────────────────────
    float _stress;
    bool  _holding;
    bool  _over;

    // ─── Burbujas ────────────────────────────────────────────────────────
    const int MAX_B = 5;
    RectTransform[] _bubRT      = new RectTransform[MAX_B];
    Image[]         _bubHalo    = new Image[MAX_B];
    Image[]         _bubMain    = new Image[MAX_B];
    Image[]         _bubShine   = new Image[MAX_B];
    float[]         _bubVX      = new float[MAX_B];
    float[]         _bubVY      = new float[MAX_B];
    float[]         _bubRespawn = new float[MAX_B];
    bool[]          _bubActive  = new bool[MAX_B];
    static readonly Color[] BUB_COLORS =
    {
        new Color(0.95f, 0.20f, 0.28f, 0.90f),
        new Color(0.98f, 0.55f, 0.12f, 0.90f),
        new Color(0.70f, 0.15f, 0.90f, 0.88f),
        new Color(0.15f, 0.78f, 0.92f, 0.88f),
        new Color(0.95f, 0.85f, 0.10f, 0.88f),
        new Color(0.90f, 0.18f, 0.58f, 0.88f),
    };

    // ─── Elementos caoticos de fondo ─────────────────────────────────────
    const int NUM_CHAOS = 7;
    RectTransform[] _chaosRT  = new RectTransform[NUM_CHAOS];
    Image[]         _chaosImg = new Image[NUM_CHAOS];
    float[] _cPhX, _cPhY, _cFrX, _cFrY, _cBX, _cBY, _cAX, _cAY, _cHW, _cHH;
    Color[] _cColC, _cColM;

    // ─── Orbe ────────────────────────────────────────────────────────────
    Image[]         _orbGlows   = new Image[3];    // capas de glow (exterior→interior)
    RectTransform[] _orbGlowRTs = new RectTransform[3];
    RectTransform   _orbRT;
    Image           _orbImg;
    Image           _orbShineImg;

    // ─── Indicadores de fase ─────────────────────────────────────────────
    const int PHASE_DOTS = 7;   // 3 inhale + 4 exhale
    Image[]         _phaseDotsImg = new Image[PHASE_DOTS];
    Image           _phaseBadgeBg;
    TextMeshProUGUI _phaseLabel;
    TextMeshProUGUI _phaseSubLabel;

    // ─── Feedback de sincronizacion ───────────────────────────────────────
    Image           _syncBadgeBg;
    TextMeshProUGUI _syncLabel;

    // ─── HUD ─────────────────────────────────────────────────────────────
    Image           _bgPanel;
    Image           _stressBarFill;
    TextMeshProUGUI _stressLbl;
    TextMeshProUGUI _statusLbl;
    TextMeshProUGUI _cycleLbl;
    Image           _breathBtnImg;
    Image           _breathBtnGlow;
    TextMeshProUGUI _breathBtnTxt;
    TextMeshProUGUI _popFeedbackTxt;
    float           _popFeedbackTimer;
    GameObject      _victoryPanel;
    TextMeshProUGUI _victoryScoreLbl;

    // ─── Paleta ──────────────────────────────────────────────────────────
    static Color Cf(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);
    static readonly Color BG_CHAOS  = Cf(0.14f, 0.03f, 0.06f);
    static readonly Color BG_CALM   = Cf(0.04f, 0.08f, 0.16f);
    static readonly Color HDR       = Cf(0.06f, 0.09f, 0.16f);
    static readonly Color PANEL     = Cf(0.09f, 0.12f, 0.22f);
    static readonly Color PANEL2    = Cf(0.12f, 0.16f, 0.28f);
    static readonly Color ACCENT    = Cf(0.35f, 0.72f, 1.00f);
    static readonly Color DIM       = Cf(0.50f, 0.60f, 0.78f);
    static readonly Color DIM2      = Cf(0.35f, 0.44f, 0.62f);
    static readonly Color GREY      = Cf(0.20f, 0.26f, 0.38f);
    static readonly Color CGREEN    = Cf(0.22f, 0.88f, 0.54f);
    static readonly Color CRED      = Cf(0.92f, 0.26f, 0.32f);
    static readonly Color CYELLOW   = Cf(0.97f, 0.82f, 0.18f);
    static readonly Color ORB_IN    = Cf(0.24f, 0.54f, 0.98f, 0.92f);  // azul electrico — inhale
    static readonly Color ORB_EX    = Cf(0.16f, 0.80f, 0.64f, 0.88f);  // verde-teal     — exhale
    static readonly Color DOT_OFF   = Cf(0.22f, 0.26f, 0.38f, 0.70f);

    // ═════════════════════════════════════════════════════════════════════
    //  MINIGAME BASE
    // ═════════════════════════════════════════════════════════════════════

    protected override string GetIntroDescription() =>
        "El ambiente esta en caos. Usa la respiracion para calmarlo.\n" +
        "Sigue el circulo: INHALA (manten pulsado) / EXHALA (suelta el boton).\n" +
        "Haz clic en las burbujas de tension que flotan. Lleva el estres a cero.";

    protected override void OnMinigameStart()
    {
        EnsureES();
        _stress        = startStress;
        _cycleTimer    = 0f;
        _cyclesCompleted = 0;
        BuildUI();
        for (int i = 0; i < MAX_B; i++) SpawnBubble(i);
    }

    protected override void OnMinigameComplete() { }
    protected override void OnMinigameFailed()   { }

    // ═════════════════════════════════════════════════════════════════════
    //  UPDATE
    // ═════════════════════════════════════════════════════════════════════

    void Update()
    {
        if (!IsPlaying || _over) return;
        float dt = Time.deltaTime;

        bool wasInhale = InInhale;
        _cycleTimer += dt;
        if (_cycleTimer >= CycleLen)
        {
            _cycleTimer -= CycleLen;
            _cyclesCompleted++;
        }
        bool inInhale = InInhale;

        // ── Estrés ──────────────────────────────────────────────────
        _stress += passiveRate * dt;
        if (inInhale)
        {
            _stress -= _holding ? inhaleRate * dt : -0.35f * dt;
        }
        else
        {
            if (!_holding) _stress -= exhaleRate * dt;
            else           _stress += 1.2f * dt;
        }
        _stress = Mathf.Clamp(_stress, 0f, 100f);

        TickBubbles(dt);

        // Pop feedback fade
        if (_popFeedbackTimer > 0f)
        {
            _popFeedbackTimer -= dt * 1.4f;
            if (_popFeedbackTxt != null)
            {
                bool vis = _popFeedbackTimer > 0f;
                _popFeedbackTxt.gameObject.SetActive(vis);
                if (vis)
                {
                    float a = Mathf.Clamp01(_popFeedbackTimer);
                    _popFeedbackTxt.color = new Color(0.28f, 0.95f, 0.58f, a);
                    // flotar hacia arriba
                    var rt = _popFeedbackTxt.rectTransform;
                    var pos = rt.anchoredPosition;
                    pos.y += 60f * dt;
                    rt.anchoredPosition = pos;
                }
            }
        }

        if (_stress <= 0f)
        {
            _over = true;
            CompleteMinigame(CalculateScore());
            ShowVictory();
            return;
        }

        float stressT = _stress / Mathf.Max(startStress, 1f);
        UpdateChaos(stressT);
        UpdateOrbVisuals(inInhale);
        UpdatePhaseIndicators(inInhale);
        UpdateHUD(stressT);
    }

    int CalculateScore()
    {
        int base_ = 800;
        int cycleBonus = _cyclesCompleted * 25;
        return base_ + cycleBonus;
    }

    // ═════════════════════════════════════════════════════════════════════
    //  BURBUJAS
    // ═════════════════════════════════════════════════════════════════════

    void TickBubbles(float dt)
    {
        for (int i = 0; i < MAX_B; i++)
        {
            if (!_bubActive[i])
            {
                _bubRespawn[i] -= dt;
                if (_bubRespawn[i] <= 0f) SpawnBubble(i);
                continue;
            }
            var p = _bubRT[i].anchoredPosition;
            p.x += _bubVX[i] * dt;
            p.y += _bubVY[i] * dt;
            if (p.x >  730f) { p.x =  730f; _bubVX[i] = -Mathf.Abs(_bubVX[i]); }
            if (p.x < -730f) { p.x = -730f; _bubVX[i] =  Mathf.Abs(_bubVX[i]); }
            if (p.y >  255f) { p.y =  255f; _bubVY[i] = -Mathf.Abs(_bubVY[i]); }
            if (p.y < -115f) { p.y = -115f; _bubVY[i] =  Mathf.Abs(_bubVY[i]); }
            _bubRT[i].anchoredPosition = p;

            // Pulso de opacidad (halo y main)
            float pulse = 0.70f + 0.20f * Mathf.Sin(Time.time * 2.5f + i * 1.4f);
            if (_bubHalo[i] != null)
            {
                var c = _bubHalo[i].color;
                _bubHalo[i].color = new Color(c.r, c.g, c.b, pulse * 0.32f);
            }
            if (_bubMain[i] != null)
            {
                var c = _bubMain[i].color;
                _bubMain[i].color = new Color(c.r, c.g, c.b, pulse);
            }
        }
    }

    void SpawnBubble(int i)
    {
        _bubActive[i] = true;
        _bubRT[i].gameObject.SetActive(true);
        Vector2 pos;
        int tries = 0;
        do {
            pos = new Vector2(Random.Range(-700f, 700f), Random.Range(-100f, 220f));
            tries++;
        } while (pos.magnitude < 230f && tries < 20);
        _bubRT[i].anchoredPosition = pos;

        float speed = 42f + _stress * 0.75f;
        float angle = Random.value * Mathf.PI * 2f;
        _bubVX[i] = Mathf.Cos(angle) * speed;
        _bubVY[i] = Mathf.Sin(angle) * speed;

        float sz     = Mathf.Lerp(60f, 105f, _stress / Mathf.Max(startStress, 1f));
        float haloSz = sz * 1.55f;
        _bubRT[i].sizeDelta = new Vector2(haloSz, haloSz);

        Color col = BUB_COLORS[Random.Range(0, BUB_COLORS.Length)];
        if (_bubHalo[i] != null) _bubHalo[i].color = new Color(col.r, col.g, col.b, 0.28f);
        if (_bubMain[i] != null)
        {
            _bubMain[i].color = col;
            _bubMain[i].rectTransform.sizeDelta = new Vector2(sz, sz);
        }
        if (_bubShine[i] != null)
        {
            float shineSz = sz * 0.32f;
            _bubShine[i].rectTransform.sizeDelta = new Vector2(shineSz, shineSz);
        }
    }

    void PopBubble(int i)
    {
        if (!_bubActive[i]) return;
        _stress = Mathf.Max(0f, _stress - bubbleReduce);
        _bubActive[i] = false;
        _bubRT[i].gameObject.SetActive(false);
        _bubRespawn[i] = Random.Range(3.5f, 6.2f);
        ShowPopFeedback();
    }

    void ShowPopFeedback()
    {
        _popFeedbackTimer = 1.2f;
        if (_popFeedbackTxt != null)
        {
            _popFeedbackTxt.text  = "-" + Mathf.RoundToInt(bubbleReduce) + " ESTRES";
            _popFeedbackTxt.color = Cf(0.28f, 0.95f, 0.58f, 1f);
            var rt = _popFeedbackTxt.rectTransform;
            rt.anchoredPosition = new Vector2(0f, -80f);
            _popFeedbackTxt.gameObject.SetActive(true);
        }
    }

    // ═════════════════════════════════════════════════════════════════════
    //  VISUALES — ORB
    // ═════════════════════════════════════════════════════════════════════

    void UpdateOrbVisuals(bool inInhale)
    {
        float phaseT = inInhale
            ? _cycleTimer / INHALE_DUR
            : (_cycleTimer - INHALE_DUR) / EXHALE_DUR;
        float eased = SmoothStep(phaseT);

        float orbSz = inInhale
            ? Mathf.Lerp(180f, 370f, eased)
            : Mathf.Lerp(370f, 180f, eased);

        // Capas de glow — tamaños relativos al orbe
        float[] glowMult = { 2.10f, 1.65f, 1.28f };
        float[] glowAlpha = { 0.035f, 0.06f, 0.10f };
        Color   orbPhaseCol = inInhale ? ORB_IN : ORB_EX;
        for (int g = 0; g < 3; g++)
        {
            if (_orbGlowRTs[g] == null) continue;
            float gsz = orbSz * glowMult[g];
            _orbGlowRTs[g].sizeDelta = new Vector2(gsz, gsz);
            float pulse = glowAlpha[g] + 0.02f * Mathf.Sin(Time.time * 2.8f + g * 0.7f);
            _orbGlows[g].color = new Color(orbPhaseCol.r, orbPhaseCol.g, orbPhaseCol.b, pulse);
        }

        // Orbe principal
        if (_orbRT   != null) _orbRT.sizeDelta = new Vector2(orbSz, orbSz);
        if (_orbImg  != null)
        {
            // Color del orbe: correcto→más saturado, incorrecto→más gris
            bool correct = (inInhale && _holding) || (!inInhale && !_holding);
            Color target = correct
                ? orbPhaseCol
                : Color.Lerp(orbPhaseCol, Cf(0.30f, 0.32f, 0.40f, 0.80f), 0.45f);
            _orbImg.color = Color.Lerp(_orbImg.color, target, Time.deltaTime * 5f);
        }

        // Brillo interior (punto de luz en esquina superior izquierda)
        if (_orbShineImg != null)
        {
            float shineSz = orbSz * 0.22f;
            _orbShineImg.rectTransform.sizeDelta = new Vector2(shineSz, shineSz);
            float shineAlpha = 0.22f + 0.12f * Mathf.Sin(Time.time * 1.8f);
            _orbShineImg.color = Cf(1f, 1f, 1f, shineAlpha);
        }

        // Badge de fase
        if (_phaseBadgeBg != null)
            _phaseBadgeBg.color = new Color(orbPhaseCol.r * 0.30f, orbPhaseCol.g * 0.30f,
                                            orbPhaseCol.b * 0.30f, 0.88f);
        if (_phaseLabel != null)
        {
            _phaseLabel.text  = inInhale ? "INHALA" : "EXHALA";
            _phaseLabel.color = orbPhaseCol;
        }
        if (_phaseSubLabel != null)
        {
            _phaseSubLabel.text  = inInhale ? "Manten pulsado el boton" : "Suelta el boton suavemente";
            _phaseSubLabel.color = new Color(orbPhaseCol.r, orbPhaseCol.g, orbPhaseCol.b, 0.75f);
        }

        // Badge sync
        bool syncCorrect = (inInhale && _holding) || (!inInhale && !_holding);
        Color syncCol;
        string syncTxt;
        if (inInhale)
        {
            syncCol = _holding ? CGREEN   : DIM;
            syncTxt = _holding ? "Perfecto! Sigue respirando..." : "Manten pulsado ahora";
        }
        else
        {
            syncCol = !_holding ? CGREEN : CRED;
            syncTxt = !_holding ? "Bien! Relaja y deja salir el aire" : "Suelta el boton!";
        }
        if (_syncBadgeBg != null)
        {
            Color sbCol = new Color(syncCol.r * 0.20f, syncCol.g * 0.20f, syncCol.b * 0.20f, 0.75f);
            _syncBadgeBg.color = Color.Lerp(_syncBadgeBg.color, sbCol, Time.deltaTime * 4f);
        }
        if (_syncLabel != null)
        {
            _syncLabel.text  = syncTxt;
            _syncLabel.color = Color.Lerp(_syncLabel.color, syncCol, Time.deltaTime * 4f);
        }

        // Boton RESPIRAR
        Color btnTarget = (_holding && inInhale)
            ? Cf(0.14f, 0.70f, 0.46f)
            : Cf(0.15f, 0.34f, 0.72f);
        if (_breathBtnImg != null)
            _breathBtnImg.color = Color.Lerp(_breathBtnImg.color, btnTarget, Time.deltaTime * 6f);
        if (_breathBtnGlow != null)
        {
            float ba = (_holding && inInhale)
                ? 0.18f + 0.12f * Mathf.Sin(Time.time * 4f)
                : 0.0f;
            _breathBtnGlow.color = Cf(0.22f, 0.88f, 0.55f, ba);
        }
        if (_breathBtnTxt != null)
            _breathBtnTxt.text = (_holding && inInhale) ? "RESPIRANDO..." : "RESPIRAR";
    }

    void UpdatePhaseIndicators(bool inInhale)
    {
        if (_phaseDotsImg[0] == null) return;

        // 3 dots para inhale (idx 0-2), 4 dots para exhale (idx 3-6)
        // Los dots del ciclo actual se van encendiendo, los del anterior apagados
        float phaseProgress = inInhale
            ? _cycleTimer / INHALE_DUR
            : (_cycleTimer - INHALE_DUR) / EXHALE_DUR;

        for (int d = 0; d < PHASE_DOTS; d++)
        {
            if (_phaseDotsImg[d] == null) continue;
            bool isInhaleDot = d < 3;
            bool isActive    = isInhaleDot == inInhale;
            Color phaseCol   = inInhale ? ORB_IN : ORB_EX;
            Color targetCol;

            if (!isActive)
            {
                // Dots del otro ciclo: dimmer version del color contrario
                Color otherCol = isInhaleDot ? ORB_IN : ORB_EX;
                targetCol = new Color(otherCol.r*0.3f, otherCol.g*0.3f, otherCol.b*0.3f, 0.40f);
            }
            else
            {
                // Dots activos: los "cumplidos" brillan, el actual pulsa, los futuros dim
                int dotIndex  = isInhaleDot ? d : d - 3;
                int totalDots = isInhaleDot ? 3 : 4;
                float threshold = (float)dotIndex / totalDots;
                if (phaseProgress >= threshold + (1f / totalDots))
                    targetCol = new Color(phaseCol.r, phaseCol.g, phaseCol.b, 0.95f);
                else if (phaseProgress >= threshold)
                    targetCol = new Color(phaseCol.r, phaseCol.g, phaseCol.b,
                        0.45f + 0.45f * Mathf.Sin(Time.time * 5f));
                else
                    targetCol = DOT_OFF;
            }
            _phaseDotsImg[d].color = Color.Lerp(_phaseDotsImg[d].color, targetCol, Time.deltaTime * 8f);
        }
    }

    void UpdateHUD(float stressT)
    {
        // Barra de estres
        if (_stressBarFill != null)
        {
            _stressBarFill.fillAmount = _stress / 100f;
            _stressBarFill.color = Color.Lerp(CGREEN, CRED, stressT);
        }
        if (_stressLbl != null)
        {
            _stressLbl.text  = Mathf.RoundToInt(_stress).ToString() + "%";
            _stressLbl.color = Color.Lerp(CGREEN, CRED, stressT);
        }
        if (_statusLbl != null)
        {
            if      (stressT > 0.66f) { _statusLbl.text = "AMBIENTE CAOTICO";    _statusLbl.color = CRED;    }
            else if (stressT > 0.33f) { _statusLbl.text = "AMBIENTE AGITADO";    _statusLbl.color = CYELLOW; }
            else                      { _statusLbl.text = "AMBIENTE CALMANDOSE"; _statusLbl.color = CGREEN;  }
        }
        if (_cycleLbl != null)
            _cycleLbl.text = "RESPIRACIONES: " + _cyclesCompleted;
    }

    void UpdateChaos(float stressT)
    {
        for (int i = 0; i < NUM_CHAOS; i++)
        {
            if (_chaosRT[i] == null) continue;
            float px = _cBX[i] + _cAX[i] * stressT * Mathf.Sin(Time.time * _cFrX[i] + _cPhX[i]);
            float py = _cBY[i] + _cAY[i] * stressT * Mathf.Sin(Time.time * _cFrY[i] + _cPhY[i]);
            _chaosRT[i].anchorMin = new Vector2(px - _cHW[i], py - _cHH[i]);
            _chaosRT[i].anchorMax = new Vector2(px + _cHW[i], py + _cHH[i]);
            if (_chaosImg[i] != null)
                _chaosImg[i].color = Color.Lerp(_cColM[i], _cColC[i], stressT);
        }
        if (_bgPanel != null)
            _bgPanel.color = Color.Lerp(BG_CALM, BG_CHAOS, stressT);
    }

    void ShowVictory()
    {
        for (int i = 0; i < NUM_CHAOS; i++)
            if (_chaosImg[i] != null) _chaosImg[i].color = _cColM[i];
        if (_bgPanel != null) _bgPanel.color = BG_CALM;
        if (_victoryScoreLbl != null)
            _victoryScoreLbl.text = "+" + CalculateScore() + " puntos  |  " + _cyclesCompleted + " respiraciones completas";
        if (_victoryPanel != null) _victoryPanel.SetActive(true);
    }

    static float SmoothStep(float t) => t * t * (3f - 2f * t);

    // ═════════════════════════════════════════════════════════════════════
    //  CONSTRUCCIÓN UI
    // ═════════════════════════════════════════════════════════════════════

    void BuildUI()
    {
        var cGO = new GameObject("Canvas");
        cGO.transform.SetParent(transform, false);
        var cv = cGO.AddComponent<Canvas>();
        cv.renderMode   = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 10;
        var sc = cGO.AddComponent<CanvasScaler>();
        sc.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920f, 1080f);
        sc.matchWidthOrHeight  = 0.5f;
        cGO.AddComponent<GraphicRaycaster>();
        var R = cGO.GetComponent<RectTransform>();

        // ── CAPA 1: Fondo ──────────────────────────────────────────
        _bgPanel = MkImg(R, "BG", BG_CHAOS, V2(0,0), V2(1,1), V2(0,0), V2(0,0)).img;
        // Overlay de viñeta (bordes más oscuros)
        MkImg(R, "Vig_L", Cf(0,0,0,0.35f), V2(0,0), V2(0.12f,1), V2(0,0), V2(0,0));
        MkImg(R, "Vig_R", Cf(0,0,0,0.35f), V2(0.88f,0), V2(1,1), V2(0,0), V2(0,0));
        MkImg(R, "Vig_T", Cf(0,0,0,0.30f), V2(0,0.80f), V2(1,1), V2(0,0), V2(0,0));

        // ── CAPA 2: Elementos caoticos ─────────────────────────────
        BuildChaosElements(R);

        // ── CAPA 3: Burbujas de tension ────────────────────────────
        BuildBubbles(R);

        // ── CAPA 4: Orbe de respiracion ────────────────────────────
        BuildBreathOrb(R);

        // ── CAPA 5: Header ─────────────────────────────────────────
        var hdr = MkImg(R, "Hdr", HDR, V2(0,1), V2(1,1), V2(0,-44), V2(0,88)).rt;
        MkImg(hdr, "HdrFill", Cf(1,1,1,0.03f), V2(0,0), V2(1,1), V2(0,0), V2(0,0));
        MkImg(hdr, "HdrLine", CRED,  V2(0,0), V2(1,0), V2(0,1.5f), V2(0,3));
        var titleT = MkTxt(hdr, "Title", "REDUCE EL ESTRES", Color.white, 38,
            V2(0.03f, 0.1f), V2(0.56f, 0.9f));
        titleT.fontStyle = FontStyles.Bold;
        titleT.alignment = TextAlignmentOptions.MidlineLeft;
        titleT.characterSpacing = 2f;
        var catT = MkTxt(hdr, "Cat", "GESTION EMOCIONAL", DIM2, 18,
            V2(0.56f, 0.1f), V2(0.97f, 0.9f));
        catT.alignment = TextAlignmentOptions.MidlineRight;
        catT.characterSpacing = 3f;
        // Acento izquierdo en el header
        MkImg(hdr, "HL", CRED, V2(0,0.15f), V2(0.004f,0.85f), V2(0,0), V2(0,0));

        // ── CAPA 6: Zona de estres (debajo del header) ─────────────
        BuildStressSection(R);

        // ── CAPA 7: Status + ciclo counter ─────────────────────────
        var statusArea = MkImg(R, "StatArea", Cf(0,0,0,0), V2(0.04f,0.78f), V2(0.96f,0.86f), V2(0,0), V2(0,0)).rt;
        _statusLbl = MkTxt(statusArea, "Status", "AMBIENTE CAOTICO", CRED, 26,
            V2(0,0), V2(0.62f,1));
        _statusLbl.fontStyle = FontStyles.Bold;
        _statusLbl.alignment = TextAlignmentOptions.MidlineLeft;
        _statusLbl.characterSpacing = 4f;
        _cycleLbl = MkTxt(statusArea, "Cycle", "RESPIRACIONES: 0", DIM2, 18,
            V2(0.60f,0), V2(1f,1));
        _cycleLbl.alignment = TextAlignmentOptions.MidlineRight;
        _cycleLbl.characterSpacing = 1f;

        // ── CAPA 8: Botón RESPIRAR ─────────────────────────────────
        BuildBreathButton(R);

        // ── CAPA 9: Instrucciones ──────────────────────────────────
        BuildInstructions(R);

        // ── CAPA 10: Barra inferior ────────────────────────────────
        var bot = MkImg(R, "Bot", HDR, V2(0,0), V2(1,0), V2(0,34), V2(0,68)).rt;
        MkImg(bot, "BotLine", ACCENT, V2(0,1), V2(1,1), V2(0,-1.5f), V2(0,3));
        MkImg(bot, "BotFill", Cf(1,1,1,0.02f), V2(0,0), V2(1,1), V2(0,0), V2(0,0));
        MkBtn(bot, "Volver al menu", GREY, V2(0.34f,0.10f), V2(0.66f,0.90f),
            () => ReturnToGameSelector());

        // ── CAPA 11: Panel de victoria (siempre al frente) ─────────
        BuildVictoryPanel(R);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  BUILD: Orbe de respiracion
    // ─────────────────────────────────────────────────────────────────────
    void BuildBreathOrb(RectTransform R)
    {
        float orbCX = 0.5f, orbCY = 0.515f;

        // Tres capas de glow concentricos (de exterior a interior)
        float[] glowAlphas = { 0.035f, 0.06f, 0.10f };
        float[] glowSizes  = { 760f, 590f, 460f };
        for (int g = 0; g < 3; g++)
        {
            var ggo = new GameObject("OG" + g);
            ggo.transform.SetParent(R, false);
            _orbGlowRTs[g] = ggo.AddComponent<RectTransform>();
            _orbGlowRTs[g].anchorMin = _orbGlowRTs[g].anchorMax = new Vector2(orbCX, orbCY);
            _orbGlowRTs[g].pivot = new Vector2(0.5f, 0.5f);
            _orbGlowRTs[g].sizeDelta = new Vector2(glowSizes[g], glowSizes[g]);
            _orbGlowRTs[g].anchoredPosition = Vector2.zero;
            _orbGlows[g] = ggo.AddComponent<Image>();
            _orbGlows[g].color = Cf(ORB_IN.r, ORB_IN.g, ORB_IN.b, glowAlphas[g]);
        }

        // Orbe principal
        var orbGO = new GameObject("Orb");
        orbGO.transform.SetParent(R, false);
        _orbRT = orbGO.AddComponent<RectTransform>();
        _orbRT.anchorMin = _orbRT.anchorMax = new Vector2(orbCX, orbCY);
        _orbRT.pivot = new Vector2(0.5f, 0.5f);
        _orbRT.sizeDelta = new Vector2(180f, 180f);
        _orbRT.anchoredPosition = Vector2.zero;
        _orbImg = orbGO.AddComponent<Image>();
        _orbImg.color = ORB_IN;

        // Capa brillante interior (esquina superior-izquierda del orbe)
        var shineGO = new GameObject("OrbShine");
        shineGO.transform.SetParent(orbGO.transform, false);
        var shineRT = shineGO.AddComponent<RectTransform>();
        shineRT.anchorMin = shineRT.anchorMax = new Vector2(0.5f, 0.5f);
        shineRT.pivot = new Vector2(0.5f, 0.5f);
        shineRT.sizeDelta = new Vector2(40f, 40f);
        shineRT.anchoredPosition = new Vector2(-50f, 55f);
        _orbShineImg = shineGO.AddComponent<Image>();
        _orbShineImg.color = Cf(1f, 1f, 1f, 0.22f);

        // Badge de fase (INHALA/EXHALA) — panel flotante encima del orbe
        var badgeRT = MkImg(R, "PhaseBadge", Cf(0.08f,0.16f,0.40f,0.88f),
            V2(0.30f, 0.68f), V2(0.70f, 0.78f), V2(0,0), V2(0,0)).rt;
        MkImg(badgeRT, "BL", ORB_IN, V2(0,0), V2(0,1), V2(2,0), V2(4,0));
        MkImg(badgeRT, "BR", ORB_IN, V2(1,0), V2(1,1), V2(-2,0), V2(4,0));
        _phaseBadgeBg = badgeRT.GetComponent<Image>();
        _phaseLabel = MkTxt(badgeRT, "PhaseLbl", "INHALA",
            ORB_IN, 42, V2(0,0.44f), V2(1,1));
        _phaseLabel.fontStyle = FontStyles.Bold;
        _phaseLabel.characterSpacing = 6f;
        _phaseSubLabel = MkTxt(badgeRT, "PhaseSub", "Manten pulsado el boton",
            ORB_IN, 18, V2(0.02f,0.02f), V2(0.98f,0.46f));

        // Puntos de progreso de fase (3 inhale + 4 exhale)
        BuildPhaseDots(R, orbCX, orbCY);

        // Badge de sincronizacion — debajo del orbe
        var syncRT = MkImg(R, "SyncBadge", Cf(0.05f,0.12f,0.08f,0.75f),
            V2(0.22f, 0.29f), V2(0.78f, 0.38f), V2(0,0), V2(0,0)).rt;
        _syncBadgeBg = syncRT.GetComponent<Image>();
        _syncLabel = MkTxt(syncRT, "SyncLbl", "Manten pulsado ahora",
            DIM, 22, V2(0.02f,0), V2(0.98f,1));

        // Texto flotante de feedback pop
        _popFeedbackTxt = MkTxt(R, "PopFB", "", CGREEN, 36,
            V2(0.30f, 0.44f), V2(0.70f, 0.56f));
        _popFeedbackTxt.fontStyle = FontStyles.Bold;
        _popFeedbackTxt.gameObject.SetActive(false);
    }

    void BuildPhaseDots(RectTransform R, float cx, float cy)
    {
        // Los 7 dots se colocan entre el badge y el orbe
        // Separados en dos grupos: 3 izquierda (inhale) + separador + 4 derecha (exhale)
        float dotAreaY   = 0.645f;
        float dotSize    = 14f;
        float spacing    = 26f;

        // Grupo inhale (3 dots) centrado a la izquierda
        float groupInhaleX = cx - 0.07f;  // offset izquierda del centro
        // Grupo exhale (4 dots) centrado a la derecha
        float groupExhaleX = cx + 0.07f;

        float[] dotAnchX = new float[7];
        for (int d = 0; d < 3; d++)
        {
            // 3 dots inhale: distribuidos alrededor de groupInhaleX
            float baseX = groupInhaleX - ((3 - 1) * 0.016f) * 0.5f + d * 0.016f;
            dotAnchX[d] = baseX;
        }
        for (int d = 0; d < 4; d++)
        {
            float baseX = groupExhaleX - ((4 - 1) * 0.016f) * 0.5f + d * 0.016f;
            dotAnchX[d + 3] = baseX;
        }

        // Separador entre los dos grupos
        MkImg(R, "DotSep", Cf(1,1,1,0.12f),
            V2(cx - 0.005f, dotAreaY - 0.005f),
            V2(cx + 0.005f, dotAreaY + 0.008f),
            V2(0,0), V2(0,0));

        // Labels de grupo
        MkTxt(R, "DotInLbl", "INHALA", Cf(ORB_IN.r,ORB_IN.g,ORB_IN.b,0.55f), 13,
            V2(cx-0.14f, dotAreaY-0.012f), V2(cx-0.01f, dotAreaY+0.010f))
            .characterSpacing = 2f;
        MkTxt(R, "DotExLbl", "EXHALA", Cf(ORB_EX.r,ORB_EX.g,ORB_EX.b,0.55f), 13,
            V2(cx+0.01f, dotAreaY-0.012f), V2(cx+0.14f, dotAreaY+0.010f))
            .characterSpacing = 2f;

        for (int d = 0; d < PHASE_DOTS; d++)
        {
            float ax = dotAnchX[d];
            float ay = dotAreaY;
            var dGO = new GameObject("Dot" + d);
            dGO.transform.SetParent(R, false);
            var dRT = dGO.AddComponent<RectTransform>();
            dRT.anchorMin = dRT.anchorMax = new Vector2(ax, ay);
            dRT.pivot = new Vector2(0.5f, 0.5f);
            dRT.sizeDelta = new Vector2(dotSize, dotSize);
            dRT.anchoredPosition = Vector2.zero;
            _phaseDotsImg[d] = dGO.AddComponent<Image>();
            _phaseDotsImg[d].color = DOT_OFF;
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    //  BUILD: Burbujas
    // ─────────────────────────────────────────────────────────────────────
    void BuildBubbles(RectTransform R)
    {
        for (int i = 0; i < MAX_B; i++)
        {
            var root = new GameObject("Bub" + i);
            root.transform.SetParent(R, false);
            _bubRT[i] = root.AddComponent<RectTransform>();
            _bubRT[i].anchorMin = _bubRT[i].anchorMax = new Vector2(0.5f, 0.52f);
            _bubRT[i].pivot = new Vector2(0.5f, 0.5f);
            _bubRT[i].sizeDelta = new Vector2(125f, 125f);  // halo size
            _bubRT[i].anchoredPosition = Vector2.zero;
            root.AddComponent<Image>().color = Cf(0,0,0,0); // invisible root

            // Halo exterior (más grande, muy transparente)
            var haloGO = new GameObject("H");
            haloGO.transform.SetParent(root.transform, false);
            var haloRT = haloGO.AddComponent<RectTransform>();
            haloRT.anchorMin = haloRT.anchorMax = new Vector2(0.5f, 0.5f);
            haloRT.pivot = new Vector2(0.5f, 0.5f);
            haloRT.sizeDelta = new Vector2(120f, 120f);
            haloRT.anchoredPosition = Vector2.zero;
            _bubHalo[i] = haloGO.AddComponent<Image>();
            _bubHalo[i].color = Cf(1f, 0.3f, 0.3f, 0.25f);

            // Cuerpo principal
            var mainGO = new GameObject("M");
            mainGO.transform.SetParent(root.transform, false);
            var mainRT = mainGO.AddComponent<RectTransform>();
            mainRT.anchorMin = mainRT.anchorMax = new Vector2(0.5f, 0.5f);
            mainRT.pivot = new Vector2(0.5f, 0.5f);
            mainRT.sizeDelta = new Vector2(80f, 80f);
            mainRT.anchoredPosition = Vector2.zero;
            _bubMain[i] = mainGO.AddComponent<Image>();
            _bubMain[i].color = BUB_COLORS[i % BUB_COLORS.Length];

            // Brillo interior (esquina sup-izq)
            var shineGO = new GameObject("S");
            shineGO.transform.SetParent(mainGO.transform, false);
            var shineRT = shineGO.AddComponent<RectTransform>();
            shineRT.anchorMin = shineRT.anchorMax = new Vector2(0.5f, 0.5f);
            shineRT.pivot = new Vector2(0.5f, 0.5f);
            shineRT.sizeDelta = new Vector2(22f, 22f);
            shineRT.anchoredPosition = new Vector2(-18f, 20f);
            _bubShine[i] = shineGO.AddComponent<Image>();
            _bubShine[i].color = Cf(1f, 1f, 1f, 0.35f);

            // "!" texto dentro de la burbuja
            var txtGO = new GameObject("T");
            txtGO.transform.SetParent(mainGO.transform, false);
            var txtRT = txtGO.AddComponent<RectTransform>();
            txtRT.anchorMin = Vector2.zero; txtRT.anchorMax = Vector2.one;
            txtRT.sizeDelta = Vector2.zero; txtRT.anchoredPosition = Vector2.zero;
            var tmp = txtGO.AddComponent<TextMeshProUGUI>();
            tmp.text = "!"; tmp.fontSize = 36; tmp.fontStyle = FontStyles.Bold;
            tmp.color = Cf(1f, 1f, 1f, 0.70f);
            tmp.alignment = TextAlignmentOptions.Center;

            // Botón en el cuerpo principal (el que el jugador clicka)
            var btn = mainGO.AddComponent<Button>();
            btn.targetGraphic = _bubMain[i];
            var colors = btn.colors;
            colors.normalColor      = Color.white;
            colors.highlightedColor = new Color(1.3f, 1.3f, 1.3f, 1f);
            colors.pressedColor     = new Color(1.6f, 1.6f, 1.6f, 1f);
            btn.colors = colors;
            int idx = i;
            btn.onClick.AddListener(() => PopBubble(idx));

            root.SetActive(false);
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    //  BUILD: Botón RESPIRAR
    // ─────────────────────────────────────────────────────────────────────
    void BuildBreathButton(RectTransform R)
    {
        // Sombra detrás del botón
        MkImg(R, "BtnShadow", Cf(0,0,0,0.40f),
            V2(0.352f, 0.115f), V2(0.648f, 0.235f), V2(3,-3), V2(0,0));

        // Glow pulsante (más grande que el botón)
        var glowRT = MkImg(R, "BtnGlow", Cf(0.22f,0.88f,0.55f,0f),
            V2(0.330f, 0.100f), V2(0.670f, 0.248f), V2(0,0), V2(0,0)).rt;
        _breathBtnGlow = glowRT.GetComponent<Image>();

        // Cuerpo del botón
        var btnRT = MkImg(R, "BtnBreath", Cf(0.15f, 0.34f, 0.72f),
            V2(0.360f, 0.120f), V2(0.640f, 0.234f), V2(0,0), V2(0,0)).rt;
        _breathBtnImg = btnRT.GetComponent<Image>();

        // Gradiente simulado (mitad superior más clara)
        MkImg(btnRT, "GradTop", Cf(1,1,1,0.10f), V2(0,0.5f), V2(1,1), V2(0,0), V2(0,0));

        // Borde sutil
        MkImg(btnRT, "Border", Cf(1,1,1,0.12f), V2(0,0), V2(1,0), V2(0,1.5f), V2(0,3));
        MkImg(btnRT, "BorderT", Cf(1,1,1,0.08f), V2(0,1), V2(1,1), V2(0,-1.5f), V2(0,3));

        // Texto principal
        _breathBtnTxt = MkTxt(btnRT, "T", "RESPIRAR", Color.white, 40,
            V2(0f, 0.35f), V2(1f, 1f));
        _breathBtnTxt.fontStyle = FontStyles.Bold;
        _breathBtnTxt.characterSpacing = 4f;

        // Subtexto
        MkTxt(btnRT, "Sub", "manten pulsado", Cf(1,1,1,0.50f), 18,
            V2(0.05f, 0f), V2(0.95f, 0.38f))
            .alignment = TextAlignmentOptions.Center;

        var hb = btnRT.gameObject.AddComponent<HoldButtonHandler>();
        hb.OnDown = () => _holding = true;
        hb.OnUp   = () => _holding = false;
    }

    // ─────────────────────────────────────────────────────────────────────
    //  BUILD: Seccion de estres
    // ─────────────────────────────────────────────────────────────────────
    void BuildStressSection(RectTransform R)
    {
        // Fondo sutil de la seccion
        var sect = MkImg(R, "StressSect", Cf(0f,0f,0f,0.18f),
            V2(0, 0.857f), V2(1, 0.925f), V2(0,0), V2(0,0)).rt;

        // Label izquierdo
        var lbl = MkTxt(sect, "SLabel", "NIVEL DE ESTRES", DIM2, 17,
            V2(0.02f, 0), V2(0.28f, 1));
        lbl.alignment = TextAlignmentOptions.MidlineLeft;
        lbl.characterSpacing = 3f;

        // Porcentaje grande
        _stressLbl = MkTxt(sect, "SPct",
            Mathf.RoundToInt(startStress).ToString() + "%",
            CRED, 38, V2(0.74f, 0), V2(0.98f, 1));
        _stressLbl.fontStyle = FontStyles.Bold;
        _stressLbl.alignment = TextAlignmentOptions.MidlineRight;

        // Objetivo
        MkTxt(sect, "SGoal", "OBJETIVO: 0%", DIM2, 14,
            V2(0.74f, 0), V2(0.98f, 1))
            .alignment = TextAlignmentOptions.BottomRight;

        // Barra de progreso
        var barOuter = MkImg(R, "SBar", Cf(0.05f,0.06f,0.12f),
            V2(0.00f, 0.922f), V2(1.00f, 0.965f), V2(0,0), V2(0,0)).rt;
        // Segmentos decorativos (ticks cada 25%)
        for (int t = 1; t <= 3; t++)
        {
            float tx = t / 4f;
            MkImg(barOuter, "Tick"+t, Cf(1,1,1,0.10f),
                V2(tx-0.002f, 0.1f), V2(tx+0.002f, 0.9f), V2(0,0), V2(0,0));
        }
        // Fill animado
        var fillGO = new GameObject("SF");
        fillGO.transform.SetParent(barOuter, false);
        var fRT = fillGO.AddComponent<RectTransform>();
        fRT.anchorMin = Vector2.zero; fRT.anchorMax = Vector2.one;
        fRT.sizeDelta = Vector2.zero; fRT.anchoredPosition = Vector2.zero;
        _stressBarFill = fillGO.AddComponent<Image>();
        _stressBarFill.color      = CRED;
        _stressBarFill.type       = Image.Type.Filled;
        _stressBarFill.fillMethod = Image.FillMethod.Horizontal;
        _stressBarFill.fillAmount = startStress / 100f;
        // Brillo sobre el fill
        MkImg(barOuter, "BarShine", Cf(1,1,1,0.06f), V2(0,0.55f), V2(1,1), V2(0,0), V2(0,0));
    }

    // ─────────────────────────────────────────────────────────────────────
    //  BUILD: Instrucciones
    // ─────────────────────────────────────────────────────────────────────
    void BuildInstructions(RectTransform R)
    {
        var instrArea = MkImg(R, "InstrArea", Cf(0f,0f,0f,0.20f),
            V2(0.04f, 0.065f), V2(0.96f, 0.112f), V2(0,0), V2(0,0)).rt;
        MkImg(instrArea, "IL", Cf(ORB_IN.r,ORB_IN.g,ORB_IN.b,0.40f),
            V2(0,0.1f), V2(0,0.9f), V2(2,0), V2(4,0));

        // Izquierda: instruccion respiracion
        var t1 = MkTxt(instrArea, "I1",
            "[ RESPIRAR ]  INHALA: manten  /  EXHALA: suelta",
            Cf(ORB_IN.r+0.15f, ORB_IN.g+0.15f, ORB_IN.b+0.15f, 1f),
            17, V2(0.01f,0), V2(0.52f,1));
        t1.alignment = TextAlignmentOptions.MidlineLeft;

        // Separador vertical
        MkImg(instrArea, "InstrSep", Cf(1,1,1,0.10f),
            V2(0.52f,0.1f), V2(0.524f,0.9f), V2(0,0), V2(0,0));

        // Derecha: instruccion burbujas
        var t2 = MkTxt(instrArea, "I2",
            "[ BURBUJAS ]  Haz clic para liberar tension",
            Cf(0.98f, 0.70f, 0.25f, 1f),
            17, V2(0.53f,0), V2(0.99f,1));
        t2.alignment = TextAlignmentOptions.MidlineLeft;
    }

    // ─────────────────────────────────────────────────────────────────────
    //  BUILD: Elementos caoticos
    // ─────────────────────────────────────────────────────────────────────
    void BuildChaosElements(RectTransform R)
    {
        _cPhX=new float[NUM_CHAOS]; _cPhY=new float[NUM_CHAOS];
        _cFrX=new float[NUM_CHAOS]; _cFrY=new float[NUM_CHAOS];
        _cBX =new float[NUM_CHAOS]; _cBY =new float[NUM_CHAOS];
        _cAX =new float[NUM_CHAOS]; _cAY =new float[NUM_CHAOS];
        _cHW =new float[NUM_CHAOS]; _cHH =new float[NUM_CHAOS];
        _cColC=new Color[NUM_CHAOS]; _cColM=new Color[NUM_CHAOS];

        float[] bx={0.07f,0.91f,0.18f,0.82f,0.50f,0.13f,0.87f};
        float[] by={0.80f,0.76f,0.62f,0.64f,0.82f,0.50f,0.52f};
        float[] ax={0.06f,0.07f,0.05f,0.07f,0.08f,0.05f,0.07f};
        float[] ay={0.04f,0.05f,0.06f,0.04f,0.05f,0.07f,0.04f};
        float[] fx={1.1f,0.9f,1.3f,0.7f,1.5f,1.0f,0.8f};
        float[] fy={0.8f,1.2f,0.6f,1.4f,0.9f,1.1f,1.3f};
        float[] px={0.0f,1.2f,2.4f,0.8f,3.1f,1.8f,0.4f};
        float[] py={1.5f,0.3f,2.1f,0.9f,1.7f,0.5f,2.8f};
        float[] sw={0.10f,0.08f,0.12f,0.09f,0.07f,0.11f,0.08f};
        float[] sh={0.08f,0.10f,0.06f,0.09f,0.11f,0.07f,0.09f};
        Color[] cc={
            Cf(0.92f,0.14f,0.20f,0.70f),Cf(0.96f,0.48f,0.08f,0.70f),
            Cf(0.80f,0.10f,0.72f,0.65f),Cf(0.96f,0.88f,0.10f,0.68f),
            Cf(0.85f,0.22f,0.52f,0.68f),Cf(0.28f,0.92f,0.38f,0.60f),
            Cf(0.72f,0.18f,0.92f,0.65f)};
        Color[] mc={
            Cf(0.12f,0.22f,0.48f,0.18f),Cf(0.14f,0.28f,0.52f,0.14f),
            Cf(0.10f,0.20f,0.44f,0.14f),Cf(0.14f,0.28f,0.50f,0.16f),
            Cf(0.12f,0.24f,0.46f,0.14f),Cf(0.14f,0.30f,0.52f,0.12f),
            Cf(0.16f,0.26f,0.48f,0.14f)};

        for(int i=0;i<NUM_CHAOS;i++)
        {
            _cBX[i]=bx[i];_cBY[i]=by[i];_cAX[i]=ax[i];_cAY[i]=ay[i];
            _cFrX[i]=fx[i];_cFrY[i]=fy[i];_cPhX[i]=px[i];_cPhY[i]=py[i];
            _cHW[i]=sw[i]*0.5f;_cHH[i]=sh[i]*0.5f;
            _cColC[i]=cc[i];_cColM[i]=mc[i];
            var rt=MkImg(R,"Ch"+i,cc[i],
                V2(bx[i]-_cHW[i],by[i]-_cHH[i]),
                V2(bx[i]+_cHW[i],by[i]+_cHH[i]),
                V2(0,0),V2(0,0)).rt;
            _chaosRT[i]=rt;_chaosImg[i]=rt.GetComponent<Image>();
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    //  BUILD: Panel de victoria
    // ─────────────────────────────────────────────────────────────────────
    void BuildVictoryPanel(RectTransform R)
    {
        _victoryPanel = new GameObject("VP");
        _victoryPanel.transform.SetParent(R, false);
        var er = _victoryPanel.AddComponent<RectTransform>();
        er.anchorMin=Vector2.zero; er.anchorMax=Vector2.one;
        er.sizeDelta=Vector2.zero; er.anchoredPosition=Vector2.zero;
        _victoryPanel.AddComponent<Image>().color = Cf(0f,0f,0f,0.88f);

        var card = MkImg(er, "Card", PANEL, V2(0.5f,0.5f), V2(0.5f,0.5f), V2(0,0), V2(800f,480f)).rt;
        // Borde superior verde
        MkImg(card, "BarTop", CGREEN, V2(0,1), V2(1,1), V2(0,-15), V2(0,30));
        // Brillo interior del card
        MkImg(card, "CardShine", Cf(1,1,1,0.03f), V2(0,0.5f), V2(1,1), V2(0,0), V2(0,0));
        // Acento izquierdo
        MkImg(card, "AccL", CGREEN, V2(0,0.1f), V2(0,0.9f), V2(3,0), V2(6,0));

        var ti = MkTxt(card, "Ti", "Ambiente estabilizado!", Color.white, 54,
            V2(0.05f, 0.65f), V2(0.95f, 0.93f));
        ti.fontStyle = FontStyles.Bold;

        _victoryScoreLbl = MkTxt(card, "Score", "",
            CGREEN, 22, V2(0.05f, 0.52f), V2(0.95f, 0.66f));

        MkTxt(card, "Su",
            "Has reducido el estres con respiracion consciente y liberando tension.\nSigue practicando este ritmo en tu vida diaria.",
            DIM, 24, V2(0.05f, 0.28f), V2(0.95f, 0.52f))
            .overflowMode = TextOverflowModes.Overflow;

        MkBtn(card, "Jugar de nuevo", ACCENT, V2(0.05f,0.06f), V2(0.46f,0.23f),
            () => RestartMinigame());
        MkBtn(card, "Menu", GREY, V2(0.54f,0.06f), V2(0.95f,0.23f),
            () => ReturnToGameSelector());

        _victoryPanel.SetActive(false);
    }

    // ═════════════════════════════════════════════════════════════════════
    //  HELPERS UI
    // ═════════════════════════════════════════════════════════════════════

    struct UIResult { public RectTransform rt; public Image img; }

    UIResult MkImg(RectTransform p, string n, Color col,
                   Vector2 amin, Vector2 amax, Vector2 pos, Vector2 sd)
    {
        var go = new GameObject(n);
        go.transform.SetParent(p, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin=amin; rt.anchorMax=amax;
        rt.pivot=new Vector2(0.5f,0.5f);
        rt.anchoredPosition=pos; rt.sizeDelta=sd;
        var img = go.AddComponent<Image>();
        img.color=col;
        return new UIResult { rt=rt, img=img };
    }

    TextMeshProUGUI MkTxt(RectTransform p, string n, string text,
                          Color col, float size, Vector2 amin, Vector2 amax)
    {
        var go = new GameObject(n);
        go.transform.SetParent(p, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin=amin; rt.anchorMax=amax;
        rt.pivot=new Vector2(0.5f,0.5f);
        rt.anchoredPosition=Vector2.zero; rt.sizeDelta=Vector2.zero;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text=text; tmp.color=col; tmp.fontSize=size;
        tmp.alignment=TextAlignmentOptions.Center;
        tmp.overflowMode=TextOverflowModes.Overflow;
        return tmp;
    }

    void MkBtn(RectTransform p, string label, Color bgC,
               Vector2 amin, Vector2 amax, UnityEngine.Events.UnityAction click)
    {
        var r = MkImg(p,"Btn"+label,bgC,amin,amax,V2(0,0),V2(0,0));
        MkImg(r.rt,"BtnShine",Cf(1,1,1,0.10f),V2(0,0.5f),V2(1,1),V2(0,0),V2(0,0));
        var b = r.rt.gameObject.AddComponent<Button>();
        b.targetGraphic=r.img;
        var cb=b.colors;
        cb.normalColor=Color.white; cb.highlightedColor=new Color(1,1,1,0.85f);
        cb.pressedColor=new Color(0.75f,0.75f,0.75f);
        b.colors=cb; b.onClick.AddListener(click);
        var t=MkTxt(r.rt,"T",label,Color.white,26,V2(0,0),V2(1,1));
        t.fontStyle=FontStyles.Bold;
    }

    static Vector2 V2(float x, float y) => new Vector2(x, y);

    static void EnsureES()
    {
        if (FindObjectOfType<EventSystem>()==null)
        {
            var go=new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }
    }
}
