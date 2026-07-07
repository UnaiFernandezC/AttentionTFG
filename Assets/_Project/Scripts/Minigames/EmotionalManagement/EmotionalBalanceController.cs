// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class EmotionalBalanceController : MinigameBase
{

    [Header("Zona segura (fraccion 0-1 del ancho de la barra)")]
    public float safeZoneWidth   = 0.52f;
    [Header("Limite zona amarilla (fraccion 0-1)")]
    public float yellowZoneWidth = 0.78f;

    [Header("Movimiento automatico")]
    public float driftAmplitude  = 0.10f;
    public float driftSpeed      = 0.22f;

    [Header("Control del jugador")]
    public float inputForce      = 2.20f;
    public float damping         = 0.82f;

    [Header("Condiciones")]
    public float winTime         = 10f;
    public float loseTime        = 9f;

    float _pos;
    float _vel;
    float _noiseOff;
    float _goodTime;
    float _badTime;
    bool  _over;
    bool  _inputEnabled;

    bool _leftHeld;
    bool _rightHeld;

    // Viento emocional (solo dificil): rafagas anunciadas que empujan el indicador.
    bool  _windEnabled;
    float _gustTime;
    float _gustDir;
    const float GUST_FORCE    = 1.05f;
    const float GUST_DURATION = 0.9f;

    // Telemetria y valoracion
    float _elapsed;
    float _greenReportTimer;
    int   _redEntries;
    bool  _wasInRed;
    int   _lastLitSquares;

    RectTransform   _indicatorRT;
    Image           _indicatorImg;
    float           _barHalfWidth;

    const int       STABILITY_SQUARES = 12;
    Image[]         _stabilitySquares;
    TextMeshProUGUI _timerLbl;
    TextMeshProUGUI _statusLbl;
    EmotionFaceWidget _face;
    int             _lastZone = -1;   // 0 verde, 1 amarillo, 2 rojo

    static readonly Color BG      = C(0.06f, 0.10f, 0.16f);
    static readonly Color HDR     = C(0.08f, 0.13f, 0.22f);
    static readonly Color PANEL   = C(0.10f, 0.15f, 0.25f);
    static readonly Color ACCENT  = C(0.30f, 0.68f, 1.00f);
    static readonly Color CGREEN  = C(0.22f, 0.80f, 0.50f);
    static readonly Color CYELLOW = C(0.97f, 0.80f, 0.20f);
    static readonly Color CRED    = C(0.85f, 0.28f, 0.32f);
    static readonly Color DIM     = C(0.50f, 0.62f, 0.78f);
    static readonly Color GREY    = C(0.24f, 0.30f, 0.44f);
    static readonly Color GREENBG = C(0.16f, 0.55f, 0.34f);
    static readonly Color YLWBG   = C(0.60f, 0.50f, 0.10f);
    static readonly Color REDBG   = C(0.55f, 0.16f, 0.20f);

    static Color C(float r, float g, float b) => new Color(r, g, b);

    protected override string GetIntroDescription() =>
        "Las emociones empujan el indicador: mantenlo en la zona verde.\n" +
        "Usa las flechas o los botones, con movimientos suaves.";

    void ApplyDifficulty()
    {
        var diff = GameManager.Instance != null
            ? GameManager.Instance.CurrentDifficulty
            : DifficultyLevel.Easy;
        switch (diff)
        {
            case DifficultyLevel.Medium:
                winTime        = 11f;
                driftAmplitude = 0.10f;
                driftSpeed     = 0.24f;
                loseTime       = 8f;
                break;
            case DifficultyLevel.Hard:
                winTime        = 14f;
                driftAmplitude = 0.14f;
                driftSpeed     = 0.30f;
                loseTime       = 7f;
                _windEnabled   = true;   // rafagas de viento emocional anunciadas
                break;
            default:
                winTime        = 8f;
                driftAmplitude = 0.07f;
                driftSpeed     = 0.18f;
                loseTime       = 9f;
                break;
        }
    }

    protected override void OnMinigameStart()
    {
        EnsureES();
        ApplyDifficulty();
        _noiseOff    = Random.value * 100f;
        _inputEnabled = true;
        BuildUI();
        if (_windEnabled) StartCoroutine(WindRoutine());
    }

    IEnumerator WindRoutine()
    {
        while (IsPlaying && !_over)
        {
            yield return new WaitForSeconds(Random.Range(4f, 8f));
            if (!IsPlaying || _over) yield break;

            // Anuncio: flash + aviso, para que el jugador pueda ANTICIPARSE.
            GameFeel.ScreenFlash(new Color(0.97f, 0.80f, 0.20f), 0.20f, 0.35f);
            GameFeel.PlayPop();
            GameFeel.FloatingText("¡Viento emocional!", CYELLOW, new Vector2(0f, 240f), 44f);

            yield return new WaitForSeconds(0.65f);
            if (!IsPlaying || _over) yield break;

            _gustDir  = Random.value < 0.5f ? -1f : 1f;
            _gustTime = GUST_DURATION;
        }
    }

    protected override void OnMinigameComplete() { }
    protected override void OnMinigameFailed()   { }

    void Update()
    {
        if (!_inputEnabled || !IsPlaying || _over) return;

        bool kb_left  = Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow);
        bool kb_right = Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow);
        float inputDir = ((kb_right || _rightHeld) ? 1f : 0f)
                       - ((kb_left  || _leftHeld)  ? 1f : 0f);

        _noiseOff += driftSpeed * Time.deltaTime;
        float drift = (Mathf.PerlinNoise(_noiseOff, 0.3f) - 0.5f) * 2f * driftAmplitude;

        _vel += drift      * Time.deltaTime * 60f;
        _vel += inputDir   * inputForce * Time.deltaTime * 60f;

        if (_gustTime > 0f)
        {
            _gustTime -= Time.deltaTime;
            _vel += _gustDir * GUST_FORCE * Time.deltaTime * 60f;
        }
        _vel *= Mathf.Pow(damping, Time.deltaTime * 60f);
        _pos += _vel * Time.deltaTime;

        if (_pos >  1f) { _pos =  1f; _vel = -Mathf.Abs(_vel) * 0.35f; }
        if (_pos < -1f) { _pos = -1f; _vel =  Mathf.Abs(_vel) * 0.35f; }

        float absP    = Mathf.Abs(_pos);
        bool inSafe   = absP <= safeZoneWidth;
        bool inRed    = absP >  yellowZoneWidth;
        bool inYellow = !inSafe && !inRed;

        _elapsed += Time.deltaTime;

        if (inSafe)   { _goodTime += Time.deltaTime; _badTime  = 0f; }
        else if (inRed) { _badTime  += Time.deltaTime; }
        else            { _badTime   = 0f; }

        // Telemetria: cada 2 s seguidos en verde = acierto.
        if (inSafe)
        {
            _greenReportTimer += Time.deltaTime;
            if (_greenReportTimer >= 2f)
            {
                _greenReportTimer -= 2f;
                ReportEvent(true);
            }
        }
        else
        {
            _greenReportTimer = 0f;
        }

        // Entrar en rojo = fallo (una vez por entrada).
        if (inRed && !_wasInRed)
        {
            _redEntries++;
            ReportEvent(false);
            GameFeel.PlayError();
            GameFeel.ScreenFlash(new Color(0.90f, 0.22f, 0.28f), 0.16f, 0.25f);
            if (_indicatorRT != null) GameFeel.Shake(_indicatorRT, 8f, 0.25f);
        }
        _wasInRed = inRed;

        if (_goodTime >= winTime)
        {
            EndGame(won: true);
            return;
        }
        if (_badTime >= loseTime)
        {
            EndGame(won: false);
            return;
        }

        UpdateBarUI(inSafe, inRed, inYellow);
    }

    void EndGame(bool won)
    {
        _over = true;
        int score = won ? Mathf.Max(500, 1000 - _redEntries * 60) : 0;

        if (won)
        {
            CompleteMinigame(score);
            GameFeel.PlaySuccess();
            GameFeel.Confetti();
        }
        else
        {
            FailMinigame();
        }

        // Eficiencia: jugar sin salirse apenas del verde da mas estrellas.
        float ratio = _elapsed > 0f ? Mathf.Clamp01(winTime / _elapsed) : 0f;

        ShowResults(
            won,
            GameFeel.StarsFromRatio(won, ratio),
            score,
            new[]
            {
                "Tiempo en calma: " + Mathf.RoundToInt(_goodTime) + " s de " + Mathf.RoundToInt(winTime) + " s",
                "Veces en zona roja: " + _redEntries
            },
            won ? "¡Encontraste el equilibrio!" : "El indicador se descontrolo",
            won ? "Pequenos ajustes y con calma: asi se equilibran las emociones."
                : "Truco: mueve poquito a poquito, sin dar golpes de direccion.");
    }

    void UpdateBarUI(bool inSafe, bool inRed, bool inYellow)
    {

        if (_indicatorRT != null)
            _indicatorRT.anchoredPosition = new Vector2(_pos * _barHalfWidth, 0f);

        if (_indicatorImg != null)
            _indicatorImg.color = inSafe ? CGREEN : (inRed ? CRED : CYELLOW);

        if (_stabilitySquares != null)
        {
            int lit = Mathf.Min(
                Mathf.FloorToInt(_goodTime / winTime * STABILITY_SQUARES),
                STABILITY_SQUARES);
            for (int i = 0; i < STABILITY_SQUARES; i++)
                if (_stabilitySquares[i] != null)
                    _stabilitySquares[i].enabled = (i < lit);

            if (lit > _lastLitSquares) GameFeel.PlayPop();
            _lastLitSquares = lit;
        }

        // Carita central: feliz en verde, preocupada en amarillo, agobiada en rojo.
        if (_face != null)
        {
            int zone = inSafe ? 0 : (inRed ? 2 : 1);
            _face.SetMood(inSafe ? 1f : (inRed ? 0.05f : 0.5f));
            if (zone != _lastZone)
            {
                if (_lastZone >= 0) _face.Pulse();
                _lastZone = zone;
            }
        }

        if (_timerLbl != null)
            _timerLbl.text = Mathf.FloorToInt(_goodTime).ToString();

        if (_statusLbl != null)
        {
            if (inSafe)
            {
                _statusLbl.text  = "En equilibrio";
                _statusLbl.color = CGREEN;
            }
            else if (inRed)
            {
                _statusLbl.text  = "Fuera de control";
                _statusLbl.color = CRED;
            }
            else
            {
                _statusLbl.text  = "Recupera el equilibrio";
                _statusLbl.color = CYELLOW;
            }
        }
    }

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

        MkImg(R, "BG", BG, V2(0,0), V2(1,1), V2(0,0), V2(0,0));

        var hdr = MkImg(R, "Hdr", HDR, V2(0,1), V2(1,1), V2(0,-40), V2(0,80));
        MkImg(hdr, "HL", ACCENT, V2(0,0), V2(1,0), V2(0,1.5f), V2(0,3));
        var ht = MkTxt(hdr, "T", "Manten el equilibrio", Color.white, 40, V2(0.03f,0), V2(0.60f,1));
        ht.fontStyle = FontStyles.Bold;
        ht.alignment = TextAlignmentOptions.MidlineLeft;
        var subT = MkTxt(hdr, "S", "Gestion emocional", DIM, 22, V2(0.60f,0), V2(0.97f,1));
        subT.alignment = TextAlignmentOptions.MidlineRight;

        var instrT = MkTxt(R, "Instr",
            "Mantén el indicador dentro de la zona verde",
            DIM, 28, V2(0.0f,0.86f), V2(0.90f,0.93f));
        instrT.alignment           = TextAlignmentOptions.Center;
        instrT.overflowMode        = TextOverflowModes.Overflow;
        instrT.enableWordWrapping  = false;

        BuildEmotionalBar(R);

        BuildStabilityBar(R);

        _statusLbl = MkTxt(R, "Status", "Preparate...", DIM, 34,
            V2(0.05f, 0.10f), V2(0.95f, 0.22f));
        _statusLbl.fontStyle = FontStyles.Bold;

        // Carita central que refleja la zona: feliz / preocupada / agobiada.
        _face = EmotionFaceWidget.Build(R, new Vector2(0.5f, 0.785f), 120f);
        _face.SetMood(1f);

        BuildControlButtons(R);
    }

    void BuildEmotionalBar(RectTransform R)
    {

        var barCont = MkImg(R, "BarCont", new Color(0,0,0,0),
            V2(0.06f, 0.52f), V2(0.94f, 0.68f), V2(0,0), V2(0,0));

        MkImg(barCont, "Border", new Color(1,1,1,0.08f),
            V2(0,0), V2(1,1), V2(0,0), V2(0,0));

        float halfSafe   = safeZoneWidth   * 0.5f;
        float halfYellow = yellowZoneWidth * 0.5f;

        MkImg(barCont, "RedL",  REDBG,   V2(0,         0), V2(0.5f - halfYellow, 1), V2(0,0), V2(0,0));
        MkImg(barCont, "RedR",  REDBG,   V2(0.5f + halfYellow, 0), V2(1, 1), V2(0,0), V2(0,0));

        MkImg(barCont, "YelL",  YLWBG,   V2(0.5f - halfYellow, 0), V2(0.5f - halfSafe, 1), V2(0,0), V2(0,0));
        MkImg(barCont, "YelR",  YLWBG,   V2(0.5f + halfSafe, 0), V2(0.5f + halfYellow, 1), V2(0,0), V2(0,0));

        MkImg(barCont, "Green", GREENBG, V2(0.5f - halfSafe, 0), V2(0.5f + halfSafe, 1), V2(0,0), V2(0,0));

        var lG = MkTxt(barCont, "LG", "ZONA SEGURA", CGREEN, 16, V2(0.5f - halfSafe, 0), V2(0.5f + halfSafe, 1));
        lG.fontStyle = FontStyles.Bold;
        var lL = MkTxt(barCont, "LL", "PELIGRO", CRED, 13, V2(0, 0), V2(0.5f - halfYellow, 1));
        var lR = MkTxt(barCont, "LR", "PELIGRO", CRED, 13, V2(0.5f + halfYellow, 0), V2(1, 1));

        var indGO = new GameObject("Indicator");
        indGO.transform.SetParent(barCont, false);
        _indicatorRT = indGO.AddComponent<RectTransform>();
        _indicatorRT.anchorMin        = new Vector2(0.5f, 0f);
        _indicatorRT.anchorMax        = new Vector2(0.5f, 1f);
        _indicatorRT.pivot            = new Vector2(0.5f, 0.5f);
        _indicatorRT.sizeDelta        = new Vector2(28f, 0f);
        _indicatorRT.anchoredPosition = Vector2.zero;
        _indicatorImg = indGO.AddComponent<Image>();
        _indicatorImg.color = CGREEN;

        _barHalfWidth = (0.94f - 0.06f) * 1920f * 0.5f - 20f;
    }

    void BuildStabilityBar(RectTransform R)
    {

        var lbl = MkTxt(R, "StabLbl", "Estabilidad acumulada", DIM, 24,
            V2(0.06f, 0.46f), V2(0.55f, 0.52f));
        lbl.alignment = TextAlignmentOptions.MidlineLeft;

        _timerLbl = MkTxt(R, "Timer", "0",
            Color.white, 48, V2(0.72f, 0.40f), V2(0.94f, 0.52f));
        _timerLbl.fontStyle = FontStyles.Bold;
        _timerLbl.alignment = TextAlignmentOptions.MidlineRight;
        MkTxt(R, "TimerLbl", "seg", DIM, 22, V2(0.72f, 0.36f), V2(0.94f, 0.42f))
            .alignment = TextAlignmentOptions.MidlineRight;

        _stabilitySquares = new Image[STABILITY_SQUARES];
        float areaLeft  = 0.06f;
        float squareW   = (0.68f - 0.06f - (STABILITY_SQUARES - 1) * 0.004f) / STABILITY_SQUARES;
        float gap       = 0.004f;
        float yBot      = 0.35f;
        float yTop      = 0.46f;

        for (int i = 0; i < STABILITY_SQUARES; i++)
        {
            float xLeft  = areaLeft + i * (squareW + gap);
            float xRight = xLeft + squareW;

            var bg = MkImg(R, "SqBg" + i,
                new Color(0.10f, 0.12f, 0.16f),
                V2(xLeft, yBot), V2(xRight, yTop), V2(0,0), V2(0,0));

            MkImg(bg, "B", new Color(1,1,1,0.08f), V2(0,0), V2(1,1), V2(0,0), V2(0,0));

            var fillGO = new GameObject("Sq" + i);
            fillGO.transform.SetParent(bg, false);
            var fillRT = fillGO.AddComponent<RectTransform>();
            fillRT.anchorMin = V2(0.06f, 0.06f);
            fillRT.anchorMax = V2(0.94f, 0.94f);
            fillRT.sizeDelta = Vector2.zero;
            fillRT.anchoredPosition = Vector2.zero;
            var img = fillGO.AddComponent<Image>();
            img.color = SquareColor(i, STABILITY_SQUARES);
            img.enabled = false;
            _stabilitySquares[i] = img;
        }
    }

    static Color SquareColor(int i, int total)
    {
        float t = (float)i / (total - 1);
        if (t < 0.5f)
            return Color.Lerp(new Color(0.85f,0.25f,0.25f), new Color(0.97f,0.80f,0.15f), t * 2f);
        else
            return Color.Lerp(new Color(0.97f,0.80f,0.15f), new Color(0.18f,0.85f,0.45f), (t - 0.5f) * 2f);
    }

    void BuildControlButtons(RectTransform R)
    {

        var hint = MkTxt(R, "KeyHint", "Teclas A / D  o  flechas del teclado  /  botones de abajo",
            DIM, 20, V2(0.05f, 0.25f), V2(0.95f, 0.31f));
        hint.alignment = TextAlignmentOptions.Center;

        var lBg = MkImg(R, "BtnL", new Color(0.20f,0.25f,0.50f), V2(0.06f,0.13f), V2(0.34f,0.24f), V2(0,0), V2(0,0));
        var lT  = MkTxt(lBg, "T", "Izquierda", Color.white, 32, V2(0,0), V2(1,1));
        lT.fontStyle = FontStyles.Bold;
        var lHb = lBg.gameObject.AddComponent<HoldButtonHandler>();
        lHb.OnDown = () => _leftHeld  = true;
        lHb.OnUp   = () => _leftHeld  = false;

        var rBg = MkImg(R, "BtnR", new Color(0.20f,0.25f,0.50f), V2(0.66f,0.13f), V2(0.94f,0.24f), V2(0,0), V2(0,0));
        var rT  = MkTxt(rBg, "T", "Derecha", Color.white, 32, V2(0,0), V2(1,1));
        rT.fontStyle = FontStyles.Bold;
        var rHb = rBg.gameObject.AddComponent<HoldButtonHandler>();
        rHb.OnDown = () => _rightHeld = true;
        rHb.OnUp   = () => _rightHeld = false;
    }

    static Vector2 V2(float x, float y) => new Vector2(x, y);

    RectTransform MkImg(RectTransform p, string n, Color col,
                        Vector2 amin, Vector2 amax, Vector2 pos, Vector2 sd)
    {
        var go = new GameObject(n);
        go.transform.SetParent(p, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = amin; rt.anchorMax = amax;
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = sd;
        go.AddComponent<Image>().color = col;
        return rt;
    }

    TextMeshProUGUI MkTxt(RectTransform p, string n, string text,
                          Color col, float size, Vector2 amin, Vector2 amax)
    {
        var go = new GameObject(n);
        go.transform.SetParent(p, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = amin; rt.anchorMax = amax;
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero; rt.sizeDelta = Vector2.zero;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text         = text;
        tmp.color        = col;
        tmp.fontSize     = size;
        tmp.alignment    = TextAlignmentOptions.Center;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        return tmp;
    }

    static void EnsureES()
    {
        if (FindObjectOfType<EventSystem>() == null)
        {
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }
    }
}

public class HoldButtonHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public System.Action OnDown;
    public System.Action OnUp;

    public void OnPointerDown(PointerEventData eventData) => OnDown?.Invoke();
    public void OnPointerUp(PointerEventData eventData)   => OnUp?.Invoke();
}
