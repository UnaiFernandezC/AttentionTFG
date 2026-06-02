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

    RectTransform   _indicatorRT;
    Image           _indicatorImg;
    float           _barHalfWidth;

    const int       STABILITY_SQUARES = 12;
    Image[]         _stabilitySquares;
    TextMeshProUGUI _timerLbl;
    TextMeshProUGUI _statusLbl;

    GameObject      _endPanel;
    Image           _endBarImg;
    TextMeshProUGUI _endTitle;
    TextMeshProUGUI _endSub;

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
        "El indicador se mueve solo. Mantelo en la zona verde!\n\n" +
        "Usa los botones de pantalla o las flechas del teclado.\n" +
        "Muevelo despacio, no de golpe.\n" +
        "Si se va a la zona roja demasiado tiempo, pierdes!";

    protected override void OnMinigameStart()
    {
        EnsureES();
        _noiseOff    = Random.value * 100f;
        _inputEnabled = true;
        BuildUI();
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
        _vel *= Mathf.Pow(damping, Time.deltaTime * 60f);
        _pos += _vel * Time.deltaTime;

        if (_pos >  1f) { _pos =  1f; _vel = -Mathf.Abs(_vel) * 0.35f; }
        if (_pos < -1f) { _pos = -1f; _vel =  Mathf.Abs(_vel) * 0.35f; }

        float absP    = Mathf.Abs(_pos);
        bool inSafe   = absP <= safeZoneWidth;
        bool inRed    = absP >  yellowZoneWidth;
        bool inYellow = !inSafe && !inRed;

        if (inSafe)   { _goodTime += Time.deltaTime; _badTime  = 0f; }
        else if (inRed) { _badTime  += Time.deltaTime; }
        else            { _badTime   = 0f; }

        if (_goodTime >= winTime)
        {
            _over = true;
            int score = Mathf.Max(500, 1000 - Mathf.RoundToInt(_badTime * 30));
            CompleteMinigame(score);
            ShowEnd(true);
            return;
        }
        if (_badTime >= loseTime)
        {
            _over = true;
            FailMinigame();
            ShowEnd(false);
            return;
        }

        UpdateBarUI(inSafe, inRed, inYellow);
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

    void ShowEnd(bool won)
    {
        _endBarImg.color  = won ? CGREEN : CRED;
        _endTitle.text    = won ? "Genial! En equilibrio!" : "Perdiste el control";
        _endSub.text      = won
            ? "Has mantenido la calma durante " + Mathf.RoundToInt(winTime) + " segundos."
            : "El indicador se salio demasiado tiempo de la zona segura.";
        _endPanel.SetActive(true);
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
            DIM, 28, V2(0.05f,0.86f), V2(0.95f,0.93f));
        instrT.alignment = TextAlignmentOptions.Center;

        BuildEmotionalBar(R);

        BuildStabilityBar(R);

        _statusLbl = MkTxt(R, "Status", "Preparate...", DIM, 34,
            V2(0.05f, 0.10f), V2(0.95f, 0.22f));
        _statusLbl.fontStyle = FontStyles.Bold;

        BuildControlButtons(R);

        var bot = MkImg(R, "Bot", HDR, V2(0,0), V2(1,0), V2(0,45), V2(0,90));
        MkImg(bot, "BL", ACCENT, V2(0,1), V2(1,1), V2(0,-1.5f), V2(0,3));
        MkBtn(bot, "Volver al menu", GREY, V2(0.30f,0.12f), V2(0.70f,0.88f),
            () => ReturnToGameSelector());

        BuildEndPanel(R);
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

    void BuildEndPanel(RectTransform R)
    {
        _endPanel = new GameObject("EndPanel");
        _endPanel.transform.SetParent(R, false);
        var er = _endPanel.AddComponent<RectTransform>();
        er.anchorMin = Vector2.zero; er.anchorMax = Vector2.one;
        er.sizeDelta = Vector2.zero; er.anchoredPosition = Vector2.zero;
        _endPanel.AddComponent<Image>().color = new Color(0, 0, 0, 0.85f);

        var card = MkImg(er, "Card", PANEL, V2(0.5f,0.5f), V2(0.5f,0.5f), V2(0,0), V2(720, 400));
        _endBarImg = MkImg(card, "Bar", CGREEN, V2(0,1), V2(1,1), V2(0,-13), V2(0,26)).GetComponent<Image>();
        _endTitle  = MkTxt(card, "Ti", "", Color.white, 52, V2(0.05f,0.55f), V2(0.95f,0.92f));
        _endTitle.fontStyle = FontStyles.Bold;
        _endSub    = MkTxt(card, "Su", "", DIM, 26, V2(0.05f,0.28f), V2(0.95f,0.55f));
        _endSub.overflowMode = TextOverflowModes.Overflow;

        MkBtn(card, "Jugar de nuevo", ACCENT, V2(0.06f,0.04f), V2(0.46f,0.22f), () =>
        {
            StopAllCoroutines();
            _endPanel.SetActive(false);
            _over         = false;
            _goodTime     = 0;
            _badTime      = 0;
            _pos          = 0;
            _vel          = 0;
            _noiseOff     = Random.value * 100f;
            _inputEnabled = true;
            IsPlaying     = true;
            if (_stabilitySquares != null)
                for (int i = 0; i < STABILITY_SQUARES; i++)
                    if (_stabilitySquares[i] != null)
                        _stabilitySquares[i].enabled = false;
            if (_timerLbl != null) _timerLbl.text = "0";
        });
        MkBtn(card, "Menu", GREY, V2(0.54f,0.04f), V2(0.94f,0.22f), () => ReturnToGameSelector());

        _endPanel.SetActive(false);
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

    void MkBtn(RectTransform p, string label, Color bgC,
               Vector2 amin, Vector2 amax,
               UnityEngine.Events.UnityAction click)
    {
        var bg = MkImg(p, "Btn" + label, bgC, amin, amax, V2(0,0), V2(0,0));
        var b  = bg.gameObject.AddComponent<Button>();
        b.targetGraphic = bg.GetComponent<Image>();
        var cb = b.colors;
        cb.normalColor      = Color.white;
        cb.highlightedColor = new Color(1,1,1,0.85f);
        cb.pressedColor     = new Color(0.7f,0.7f,0.7f);
        b.colors = cb;
        b.onClick.AddListener(click);
        var t = MkTxt(bg, "T", label, Color.white, 28, V2(0,0), V2(1,1));
        t.fontStyle = FontStyles.Bold;
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
