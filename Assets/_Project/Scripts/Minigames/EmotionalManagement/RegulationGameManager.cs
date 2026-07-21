// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// "Vuelve a la calma" — respiracion guiada interactiva (Gestion emocional).
/// Un circulo-guia grande con carita de robot crece (INSPIRA: el nino MANTIENE
/// PULSADO) y decrece (SUELTA: el nino suelta). La sincronia con el ritmo llena
/// un medidor de calma con estrellitas. Nunca hay fracaso: al final siempre se
/// muestran resultados de exito con estrellas segun la sincronia media.
/// UI 100% por codigo sobre fondo espacial opaco (tapa la UI vieja de la escena).
/// </summary>
public class RegulationGameManager : MinigameBase
{
    // ---------- Campos serializados LEGACY (se conservan para no romper la escena) ----------
    [Header("Nivel emocional inicial (legacy, sin uso)")]
    public float startLevel = 100f;

    [Header("Tension automatica por turno (legacy, sin uso)")]
    public float regenerationPerTurn = 8f;

    [Header("Maximo de acciones antes de perder (legacy, sin uso)")]
    public int maxSteps = 10;

    // ---------- Configuracion por dificultad ----------
    int   _cycles     = 4;      // respiraciones completas
    float _inhaleMin  = 4.0f;   // duracion de la inspiracion (s)
    float _inhaleMax  = 4.0f;
    float _exhaleMin  = 4.0f;   // duracion de la exhalacion (s)
    float _exhaleMax  = 4.0f;

    // Paleta de Gestion emocional (verde)
    static readonly Color VERDE      = new Color(0.18f, 0.80f, 0.58f);
    static readonly Color VERDE_SUAV = new Color(0.18f, 0.80f, 0.58f, 0.16f);
    static readonly Color ESTRELLA   = new Color(0.98f, 0.80f, 0.10f);

    // ---------- UI ----------
    RectTransform   _root;
    RectTransform   _circle;       // contenedor que escala (circulo-guia)
    Image           _glowImg, _ringImg, _coreImg;
    RobotFace       _face;
    TextMeshProUGUI _phaseLbl, _hintLbl, _cycleLbl, _feedLbl;
    Image           _meterFill;
    Image[]         _starImgs;
    bool[]          _starLit;
    BreathHoldArea  _hold;

    // ---------- Estado ----------
    float _syncTotal;    // suma de sincronias por ciclo (cada una 0-1)
    int   _cyclesDone;
    float _phaseSync;    // resultado de la ultima fase (lo rellena PhaseRoutine)

    const float SCALE_MIN = 0.70f;
    const float SCALE_MAX = 1.18f;

    protected override void Start()
    {
        // Debe coincidir EXACTAMENTE con GameCatalog.
        minigameName = "Vuelve a la calma";
        category     = MinigameCategory.EmotionalManagement;
        base.Start();
    }

    protected override string GetIntroDescription() =>
        "Respira con el circulo magico:\n" +
        "MANTEN PULSADA la pantalla mientras crece (inspira)\n" +
        "y SUELTA cuando se hace pequeno (suelta el aire).\n\n" +
        "Cuanto mejor sigas el ritmo, mas estrellitas de calma ganas.";

    void ApplyDifficulty()
    {
        var diff = GameManager.Instance != null
            ? GameManager.Instance.CurrentDifficulty
            : DifficultyLevel.Easy;
        switch (diff)
        {
            case DifficultyLevel.Medium:
                _cycles = 5;
                _inhaleMin = _inhaleMax = 3.4f;
                _exhaleMin = _exhaleMax = 3.4f;
                break;
            case DifficultyLevel.Hard:
                // Ritmo variable: cada respiracion dura distinto (hay que ESCUCHAR al circulo).
                _cycles = 6;
                _inhaleMin = 2.6f; _inhaleMax = 4.2f;
                _exhaleMin = 2.6f; _exhaleMax = 4.2f;
                break;
            default:
                _cycles = 4;
                _inhaleMin = _inhaleMax = 4.0f;
                _exhaleMin = _exhaleMax = 4.0f;
                break;
        }
    }

    protected override void OnMinigameStart()
    {
        ApplyDifficulty();
        KidUI.EnsureEventSystem();

        _syncTotal  = 0f;
        _cyclesDone = 0;

        BuildUI();
        StartCoroutine(GameLoop());
    }

    protected override void OnMinigameComplete() { }
    protected override void OnMinigameFailed()   { }

    // ================================================================ UI

    void BuildUI()
    {
        Canvas cv = KidUI.MakeCanvas("CalmaCanvas", 50, transform);
        _root = cv.GetComponent<RectTransform>();
        KidUI.BuildSpaceBackground(_root);

        // ---- cabecera flotante redondeada ----
        var hdr = KidUI.RoundImg(_root, "Hdr", KidUI.PANEL,
            new Vector2(0.02f, 0.905f), new Vector2(0.98f, 0.985f),
            Vector2.zero, Vector2.zero, 1.4f);
        var hl = KidUI.RoundImg(hdr, "HL", VERDE,
            new Vector2(0.02f, 0f), new Vector2(0.98f, 0f),
            new Vector2(0f, 2f), new Vector2(0f, 4f), 4f);
        hl.GetComponent<Image>().raycastTarget = false;

        var ttl = KidUI.Txt(hdr, "T", "VUELVE A LA CALMA", Color.white, 34,
                            new Vector2(0.03f, 0f), new Vector2(0.55f, 1f));
        ttl.fontStyle = FontStyles.Bold;
        ttl.alignment = TextAlignmentOptions.MidlineLeft;

        var cat = KidUI.Txt(hdr, "Cat", "GESTION EMOCIONAL", VERDE, 18,
                            new Vector2(0.55f, 0f), new Vector2(0.80f, 1f));
        cat.alignment = TextAlignmentOptions.MidlineRight;

        _cycleLbl = KidUI.Txt(hdr, "Cyc", "Respiración 1 de " + _cycles, KidUI.DIM, 22,
                              new Vector2(0.80f, 0f), new Vector2(0.98f, 1f));
        _cycleLbl.fontStyle = FontStyles.Bold;
        _cycleLbl.alignment = TextAlignmentOptions.MidlineRight;
        UITween.PopIn(hdr, 0.45f, 0.90f);

        // ---- texto grande de fase (Inspira... / Suelta...) ----
        _phaseLbl = KidUI.Txt(_root, "Phase", "Prepárate...", VERDE, 64,
                              new Vector2(0.15f, 0.80f), new Vector2(0.85f, 0.90f));
        _phaseLbl.fontStyle = FontStyles.Bold;

        // ---- circulo-guia grande con carita ----
        var circGO = new GameObject("Circle");
        circGO.transform.SetParent(_root, false);
        _circle = circGO.AddComponent<RectTransform>();
        _circle.anchorMin = _circle.anchorMax = new Vector2(0.5f, 0.52f);
        _circle.pivot     = new Vector2(0.5f, 0.5f);
        _circle.sizeDelta = new Vector2(460f, 460f);
        _circle.anchoredPosition = Vector2.zero;

        _glowImg = KidUI.CircleAt(_circle, "Glow", VERDE_SUAV,
                                  new Vector2(0.5f, 0.5f), 560f).GetComponent<Image>();
        _glowImg.raycastTarget = false;
        _ringImg = KidUI.CircleAt(_circle, "Ring", new Color(VERDE.r, VERDE.g, VERDE.b, 0.45f),
                                  new Vector2(0.5f, 0.5f), 470f).GetComponent<Image>();
        _ringImg.raycastTarget = false;
        _coreImg = KidUI.CircleAt(_circle, "Core", new Color(0.07f, 0.16f, 0.15f, 0.98f),
                                  new Vector2(0.5f, 0.5f), 440f).GetComponent<Image>();
        _coreImg.raycastTarget = false;

        _face = EmotionFaceArt.Build(_circle, new Vector2(0.5f, 0.5f), 210f);
        _face.SetEmotion(RobotEmotion.Calma, 0.6f);

        _circle.localScale = Vector3.one * SCALE_MIN;
        UITween.PopIn(_circle, 0.55f, 0.60f, 0.05f);

        // ---- pista de accion (mantén pulsado / suelta) ----
        var hintChip = KidUI.RoundImg(_root, "HintChip", KidUI.PANEL2,
            new Vector2(0.30f, 0.205f), new Vector2(0.70f, 0.265f),
            Vector2.zero, Vector2.zero, 1.6f);
        hintChip.GetComponent<Image>().raycastTarget = false;
        _hintLbl = KidUI.Txt(hintChip, "Hint", "Toca y mantén cuando el círculo crezca",
                             KidUI.DIM, 26, Vector2.zero, Vector2.one);

        // ---- medidor de calma con estrellitas ----
        BuildCalmMeter();

        // ---- feedback breve ----
        _feedLbl = KidUI.Txt(_root, "Feed", "", VERDE, 28,
                             new Vector2(0.15f, 0.015f), new Vector2(0.85f, 0.06f));
        _feedLbl.fontStyle = FontStyles.Bold;

        // ---- boton invisible a pantalla completa (entrada por mantener pulsado) ----
        var holdGO = new GameObject("HoldArea");
        holdGO.transform.SetParent(_root, false);
        var hRT = holdGO.AddComponent<RectTransform>();
        hRT.anchorMin = Vector2.zero; hRT.anchorMax = Vector2.one;
        hRT.sizeDelta = Vector2.zero; hRT.anchoredPosition = Vector2.zero;
        var hImg = holdGO.AddComponent<Image>();
        hImg.color = new Color(0f, 0f, 0f, 0f);   // invisible pero recibe toques
        hImg.raycastTarget = true;
        _hold = holdGO.AddComponent<BreathHoldArea>();
    }

    void BuildCalmMeter()
    {
        var panel = KidUI.RoundImg(_root, "MeterPanel", KidUI.PANEL,
            new Vector2(0.24f, 0.095f), new Vector2(0.76f, 0.175f),
            Vector2.zero, Vector2.zero, 1.4f);
        panel.GetComponent<Image>().raycastTarget = false;

        var lbl = KidUI.Txt(panel, "L", "CALMA", KidUI.DIM, 18,
                            new Vector2(0.02f, 0f), new Vector2(0.14f, 1f));
        lbl.fontStyle = FontStyles.Bold;

        var barBG = KidUI.RoundImg(panel, "BarBG", new Color(0.02f, 0.05f, 0.10f, 0.95f),
            new Vector2(0.15f, 0.28f), new Vector2(0.86f, 0.72f),
            Vector2.zero, Vector2.zero, 3f);
        barBG.GetComponent<Image>().raycastTarget = false;

        var fillGO = new GameObject("Fill");
        fillGO.transform.SetParent(barBG, false);
        var fRT = fillGO.AddComponent<RectTransform>();
        fRT.anchorMin = Vector2.zero; fRT.anchorMax = Vector2.one;
        fRT.sizeDelta = new Vector2(-6f, -6f); fRT.anchoredPosition = Vector2.zero;
        _meterFill = fillGO.AddComponent<Image>();
        _meterFill.sprite        = KidUI.RoundedSprite;
        _meterFill.type          = Image.Type.Filled;
        _meterFill.fillMethod    = Image.FillMethod.Horizontal;
        _meterFill.fillOrigin    = 0;
        _meterFill.fillAmount    = 0f;
        _meterFill.color         = VERDE;
        _meterFill.raycastTarget = false;

        // 3 estrellitas que se encienden al llenar el medidor
        _starImgs = new Image[3];
        _starLit  = new bool[3];
        float[] fracs = { 0.35f, 0.62f, 0.90f };
        for (int i = 0; i < 3; i++)
        {
            float x = Mathf.Lerp(0.15f, 0.86f, fracs[i]);
            var star = KidUI.CircleAt(panel, "Star" + i,
                new Color(1f, 1f, 1f, 0.20f), new Vector2(x, 1.02f), 26f);
            star.GetComponent<Image>().raycastTarget = false;
            _starImgs[i] = star.GetComponent<Image>();
        }
        UITween.PopIn(panel, 0.45f, 0.88f, 0.10f);
    }

    // ================================================================ BUCLE

    IEnumerator GameLoop()
    {
        // Cuenta de entrada suave
        _hintLbl.text = "Ponte cómodo...";
        for (int i = 3; i >= 1; i--)
        {
            _phaseLbl.text = "Empezamos en " + i + "...";
            GameFeel.PlayPop();
            yield return new WaitForSeconds(0.9f);
        }

        for (int c = 0; c < _cycles; c++)
        {
            if (!IsPlaying) yield break;
            _cycleLbl.text = "Respiración " + (c + 1) + " de " + _cycles;

            float inhale = Random.Range(_inhaleMin, _inhaleMax);
            float exhale = Random.Range(_exhaleMin, _exhaleMax);

            // --- INSPIRA: el circulo crece, hay que MANTENER PULSADO ---
            yield return PhaseRoutine(true, inhale);
            float syncIn = _phaseSync;

            // --- EXHALA: el circulo decrece, hay que SOLTAR ---
            yield return PhaseRoutine(false, exhale);
            float syncOut = _phaseSync;

            float cycleSync = (syncIn + syncOut) * 0.5f;
            _syncTotal += cycleSync;
            _cyclesDone++;

            // Telemetria por ciclo: acierto = buena sincronia con el ritmo.
            ReportEvent(cycleSync >= 0.6f, (inhale + exhale) * 1000f);

            UpdateMeter();
            _face.Pulse();

            if (cycleSync >= 0.8f)
            {
                _feedLbl.text  = "¡Respiración perfecta!";
                _feedLbl.color = VERDE;
                GameFeel.PlayStar();
            }
            else if (cycleSync >= 0.5f)
            {
                _feedLbl.text  = "¡Muy bien, sigue así!";
                _feedLbl.color = VERDE;
                GameFeel.PlayPop();
            }
            else
            {
                _feedLbl.text  = "Tranquilo, escucha al círculo...";
                _feedLbl.color = KidUI.DIM;
            }

            _phaseLbl.text = "Descansa...";
            yield return new WaitForSeconds(0.9f);
            _feedLbl.text = "";
        }

        FinishGame();
    }

    /// <summary>
    /// Una fase de respiracion. inhale=true: el circulo crece y el nino debe
    /// MANTENER PULSADO; inhale=false: decrece y debe SOLTAR.
    /// Deja la sincronia 0-1 en _phaseSync.
    /// </summary>
    IEnumerator PhaseRoutine(bool inhale, float dur)
    {
        _phaseLbl.text  = inhale ? "Inspira..." : "Suelta...";
        _phaseLbl.color = inhale ? VERDE : new Color(0.55f, 0.80f, 1.00f);
        _hintLbl.text   = inhale ? "MANTÉN PULSADO mientras crece"
                                 : "SUELTA mientras baja";
        GameFeel.PlayPop();

        float t = 0f, good = 0f;
        while (t < dur)
        {
            if (!IsPlaying) { _phaseSync = 0f; yield break; }
            float dt = Time.deltaTime;
            t += dt;

            float p = Mathf.Clamp01(t / dur);
            float s = inhale
                ? Mathf.Lerp(SCALE_MIN, SCALE_MAX, Mathf.SmoothStep(0f, 1f, p))
                : Mathf.Lerp(SCALE_MAX, SCALE_MIN, Mathf.SmoothStep(0f, 1f, p));
            _circle.localScale = Vector3.one * s;

            bool held    = _hold != null && (_hold.IsHeld || Input.GetKey(KeyCode.Space));
            bool inSync  = held == inhale;
            if (inSync) good += dt;

            // El glow responde en vivo: brillante cuando vas en sincronia.
            float a = inSync ? 0.30f : 0.10f;
            _glowImg.color = new Color(VERDE.r, VERDE.g, VERDE.b, a);
            _ringImg.color = new Color(VERDE.r, VERDE.g, VERDE.b, inSync ? 0.65f : 0.30f);

            yield return null;
        }
        _phaseSync = Mathf.Clamp01(good / dur);
    }

    void UpdateMeter()
    {
        float fill = _cycles > 0 ? _syncTotal / _cycles : 0f;
        _meterFill.fillAmount = Mathf.Clamp01(fill);
        UITween.PulseOnce(_meterFill.rectTransform.parent as RectTransform, 1.05f, 0.25f);

        float[] thresholds = { 0.30f, 0.55f, 0.80f };
        for (int i = 0; i < 3; i++)
        {
            if (!_starLit[i] && fill >= thresholds[i])
            {
                _starLit[i] = true;
                _starImgs[i].color = ESTRELLA;
                _starImgs[i].gameObject.AddComponent<StarTwinkle>();
                UITween.PulseOnce(_starImgs[i].rectTransform, 1.6f, 0.35f);
                GameFeel.PlayStar();
                GameFeel.FloatingText("¡Estrellita de calma!", ESTRELLA,
                                      new Vector2(0f, -260f), 40f);
            }
        }
    }

    void FinishGame()
    {
        float avg   = _cyclesDone > 0 ? _syncTotal / _cyclesDone : 0f;
        int   score = Mathf.RoundToInt(avg * 1000f);
        int   stars = GameFeel.StarsFromRatio(true, avg);

        // Nunca hay fracaso en este juego: respirar siempre suma.
        CompleteMinigame(score);
        GameFeel.Confetti();
        GameFeel.PlaySuccess();

        int lit = 0;
        for (int i = 0; i < 3; i++) if (_starLit[i]) lit++;

        ShowResults(
            true,
            stars,
            score,
            new[]
            {
                "Respiraciones completas: " + _cyclesDone,
                "Sincronía con el ritmo: " + Mathf.RoundToInt(avg * 100f) + "%",
                "Estrellitas de calma: " + lit + " de 3"
            },
            "¡Has vuelto a la calma!",
            "Cuando algo te agobie, respira despacio como el círculo mágico.");
    }
}

/// <summary>
/// Componente de entrada de "Vuelve a la calma": boton invisible a pantalla
/// completa que detecta mantener pulsado / soltar (raton o dedo).
/// </summary>
public class BreathHoldArea : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public bool IsHeld { get; private set; }

    public void OnPointerDown(PointerEventData eventData) { IsHeld = true; }
    public void OnPointerUp(PointerEventData eventData)   { IsHeld = false; }

    void OnDisable() { IsHeld = false; }
}
