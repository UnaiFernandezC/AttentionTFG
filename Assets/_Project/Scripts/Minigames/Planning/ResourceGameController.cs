// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class ResourceGameController : MinigameBase
{

    [Serializable]
    public class ActionData
    {
        [Tooltip("Icono de texto que se muestra en el boton")]
        public string icon = "+";

        [Tooltip("Nombre de la accion")]
        public string actionName = "Accion";

        [Tooltip("Estrellas que cuesta")]
        public int cost = 1;

        [Tooltip("Progreso que da (sobre 100)")]
        public int progress = 10;

        [Tooltip("Color del boton (si es negro se usa el color automatico)")]
        public Color buttonColor = Color.black;

        [Tooltip("Trampa: cara y poco eficiente")]
        public bool isTrap = false;

        [Tooltip("Arriesgada: progreso aleatorio entre riskyMin y riskyMax")]
        public bool isRisky = false;
        public int  riskyMin = 25;
        public int  riskyMax = 55;
    }

    [Header("=== ESTRELLAS (energia) — fallback, ApplyDifficulty manda ===")]
    [Tooltip("Estrellas disponibles en esta escena")]
    public int stars = 20;

    [Header("=== OBJETIVO ===")]
    [Tooltip("Progreso necesario para ganar")]
    public int goal = 100;

    [Header("=== ACCIONES (fallback, ApplyDifficulty manda) ===")]
    public List<ActionData> actions = new List<ActionData>();

    private int   _maxStars;
    private int   _stars;
    private float _progress;
    private float _rawProgress;      // sin tope, para calcular eficiencia
    private int   _starsSpent;
    private int   _trapUses;
    private float _displayProg;
    private bool  _ended;
    private float _bestPerStar = 10f;
    private float _lastActionTime;

    private RectTransform   _starsFill;
    private Image           _starsFillImg;
    private TextMeshProUGUI _starsLbl;
    private RectTransform   _progFill;
    private TextMeshProUGUI _progLbl;
    private TextMeshProUGUI _progPct;
    private List<Button>         _btns      = new List<Button>();
    private List<Image>          _btnBgs    = new List<Image>();
    private List<CanvasGroup>    _btnGroups = new List<CanvasGroup>();

    static readonly Color BG       = new Color(0.14f, 0.16f, 0.28f);
    static readonly Color PANEL    = new Color(0.18f, 0.20f, 0.35f);
    static readonly Color HEADER   = new Color(0.12f, 0.13f, 0.24f);
    static readonly Color ACCENT   = new Color(0.30f, 0.58f, 1.00f);
    static readonly Color GREEN    = new Color(0.20f, 0.80f, 0.48f);
    static readonly Color RED      = new Color(0.90f, 0.28f, 0.32f);
    static readonly Color YELLOW   = new Color(1.00f, 0.85f, 0.22f);
    static readonly Color ORANGE   = new Color(1.00f, 0.62f, 0.20f);
    static readonly Color DIM      = new Color(0.55f, 0.58f, 0.75f);
    static readonly Color DARK     = new Color(0.08f, 0.09f, 0.16f);
    static readonly Color BTN_OFF  = new Color(0.22f, 0.24f, 0.34f);

    static readonly Color[] AUTO_COLORS = {
        new Color(0.30f, 0.60f, 1.00f),
        new Color(0.92f, 0.45f, 0.20f),
        new Color(0.60f, 0.32f, 0.95f),
        new Color(0.20f, 0.75f, 0.55f),
        new Color(0.90f, 0.30f, 0.50f),
    };

    protected override string GetIntroDescription() =>
        "Tienes estrellas de energia limitadas. Elige bien las acciones\n" +
        "para llenar la barra hasta el 100% antes de quedarte sin estrellas.";

    protected override void OnMinigameStart()
    {
        ApplyDifficulty();

        EnsureES();
        _maxStars       = stars;
        _stars          = _maxStars;
        _progress       = 0f;
        _rawProgress    = 0f;
        _starsSpent     = 0;
        _trapUses       = 0;
        _displayProg    = 0f;
        _ended          = false;
        _lastActionTime = Time.realtimeSinceStartup;

        _bestPerStar = 1f;
        foreach (var a in actions)
            if (!a.isRisky && a.cost > 0)
                _bestPerStar = Mathf.Max(_bestPerStar, (float)a.progress / a.cost);

        BuildUI();
        Refresh();
    }

    void ApplyDifficulty()
    {
        var diff = GameManager.Instance != null
            ? GameManager.Instance.CurrentDifficulty
            : DifficultyLevel.Easy;

        goal = 100;

        switch (diff)
        {
            case DifficultyLevel.Medium:
                stars = 11;
                actions = new List<ActionData>
                {
                    new ActionData { icon = "+", actionName = "Pasito corto",  cost = 1, progress = 9  },
                    new ActionData { icon = "++", actionName = "Buen avance",   cost = 2, progress = 20 },
                    new ActionData { icon = "+++", actionName = "Super impulso", cost = 4, progress = 42 },
                    new ActionData { icon = "$", actionName = "Mega maquina",  cost = 5, progress = 25, isTrap = true },
                };
                break;

            case DifficultyLevel.Hard:
                stars = 9;
                actions = new List<ActionData>
                {
                    new ActionData { icon = "+", actionName = "Pasito corto",  cost = 1, progress = 10 },
                    new ActionData { icon = "++", actionName = "Buen avance",   cost = 2, progress = 22 },
                    new ActionData { icon = "+++", actionName = "Super impulso", cost = 4, progress = 45 },
                    new ActionData { icon = "$", actionName = "Mega maquina",  cost = 5, progress = 24, isTrap = true },
                    new ActionData { icon = "?", actionName = "Salto sorpresa", cost = 2, progress = 40,
                                     isRisky = true, riskyMin = 25, riskyMax = 55 },
                };
                break;

            default: // Easy
                stars = 14;
                actions = new List<ActionData>
                {
                    new ActionData { icon = "+", actionName = "Pasito corto",  cost = 1, progress = 8  },
                    new ActionData { icon = "++", actionName = "Buen avance",   cost = 2, progress = 18 },
                    new ActionData { icon = "+++", actionName = "Super impulso", cost = 4, progress = 40 },
                };
                break;
        }
    }

    protected override void OnMinigameComplete() { }
    protected override void OnMinigameFailed()   { }

    private void Update()
    {
        if (Mathf.Abs(_displayProg - _progress) > 0.15f)
        {
            _displayProg = Mathf.Lerp(_displayProg, _progress, Time.deltaTime * 8f);
            UpdateProgVisual();
        }
    }

    private void DoAction(int i)
    {
        if (_ended || i < 0 || i >= actions.Count) return;
        ActionData a = actions[i];
        if (_stars < a.cost) return;

        int gain = a.isRisky
            ? UnityEngine.Random.Range(a.riskyMin, a.riskyMax + 1)
            : a.progress;

        _stars       -= a.cost;
        _starsSpent  += a.cost;
        _rawProgress += gain;
        _progress     = Mathf.Min(_progress + gain, goal);
        if (a.isTrap) _trapUses++;

        float rtMs = (Time.realtimeSinceStartup - _lastActionTime) * 1000f;
        _lastActionTime = Time.realtimeSinceStartup;
        ReportEvent(!a.isTrap, rtMs);

        GameFeel.PlayPop();
        GameFeel.FloatingText("+" + gain + "%", a.isRisky ? YELLOW : GREEN, new Vector2(240f, 90f));
        if (a.isRisky && gain >= 45) GameFeel.PlayStar();

        Refresh();
        StartCoroutine(Pulse(i));

        if (_progress >= goal)
        {
            _ended = true;
            StartCoroutine(Finish(true));
        }
        else if (!CanDoAny())
        {
            _ended = true;
            StartCoroutine(Finish(false));
        }
    }

    private bool CanDoAny()
    {
        for (int i = 0; i < actions.Count; i++)
            if (_stars >= actions[i].cost) return true;
        return false;
    }

    private int EfficiencyPct()
    {
        if (_starsSpent <= 0) return 100;
        float eff = (_rawProgress / _starsSpent) / _bestPerStar;
        return Mathf.Clamp(Mathf.RoundToInt(eff * 100f), 0, 100);
    }

    private IEnumerator Finish(bool won)
    {
        yield return new WaitForSeconds(0.6f);

        int effPct = EfficiencyPct();

        if (won)
        {
            GameFeel.PlaySuccess();
            GameFeel.Confetti(35);

            int sc = 500 + Mathf.RoundToInt(((float)_stars / _maxStars) * 300f)
                         + Mathf.RoundToInt(effPct * 2f);
            float ratio = Mathf.Clamp01(effPct / 100f - _trapUses * 0.10f);

            CompleteMinigame(sc);
            ShowResults(true, GameFeel.StarsFromRatio(true, ratio), sc,
                new[]
                {
                    "Eficiencia: " + effPct + "%",
                    "Estrellas sobrantes: " + _stars + " / " + _maxStars
                });
        }
        else
        {
            GameFeel.PlayError();
            GameFeel.ScreenFlash(RED, 0.18f, 0.3f);

            FailMinigame();
            ShowResults(false, 0, 0,
                new[]
                {
                    "Progreso: " + Mathf.RoundToInt(_progress) + "%",
                    "Eficiencia: " + effPct + "%"
                },
                "¡Sin estrellas!",
                "Planifica que acciones rinden mas");
        }
    }

    private void Reset()
    {
        if (_ended) return;
        StopAllCoroutines();
        _stars          = _maxStars;
        _progress       = 0f;
        _rawProgress    = 0f;
        _starsSpent     = 0;
        _trapUses       = 0;
        _displayProg    = 0f;
        _lastActionTime = Time.realtimeSinceStartup;
        Refresh();
    }

    private void Refresh()
    {
        float r = Mathf.Clamp01((float)_stars / _maxStars);
        _starsFill.anchorMax = new Vector2(Mathf.Max(r, 0.005f), 1f);
        _starsLbl.text = _stars + " / " + _maxStars;

        if (r > 0.5f)       _starsFillImg.color = YELLOW;
        else if (r > 0.25f) _starsFillImg.color = ORANGE;
        else                _starsFillImg.color = RED;

        UpdateProgVisual();

        for (int i = 0; i < _btns.Count; i++)
        {
            bool ok = _stars >= actions[i].cost && !_ended;
            _btns[i].interactable = ok;
            Color c = GetBtnColor(i);
            _btnBgs[i].color = ok ? c : BTN_OFF;
            _btnGroups[i].alpha = ok ? 1f : 0.45f;
        }
    }

    private void UpdateProgVisual()
    {
        float r = Mathf.Clamp01(_displayProg / goal);
        _progFill.anchorMax = new Vector2(Mathf.Max(r, 0.005f), 1f);
        int pct = Mathf.RoundToInt(_displayProg);
        _progLbl.text = pct + "%";
        _progPct.text = pct + "%";
        _progPct.color = r >= 1f ? GREEN : Color.white;
    }

    private IEnumerator Pulse(int i)
    {
        if (i >= _btns.Count) yield break;
        RectTransform rt = _btns[i].GetComponent<RectTransform>();
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 10f;
            float s = 1f + 0.06f * Mathf.Sin(t * Mathf.PI);
            rt.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
        rt.localScale = Vector3.one;
    }

    private Color GetBtnColor(int i)
    {
        if (actions[i].buttonColor != Color.black)
            return actions[i].buttonColor;
        return AUTO_COLORS[i % AUTO_COLORS.Length];
    }

    private void BuildUI()
    {

        GameObject cGO = new GameObject("Canvas");
        cGO.transform.SetParent(transform, false);
        Canvas cv = cGO.AddComponent<Canvas>();
        cv.renderMode = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 10;
        CanvasScaler cs = cGO.AddComponent<CanvasScaler>();
        cs.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920f, 1080f);
        cs.matchWidthOrHeight  = 0.5f;
        cGO.AddComponent<GraphicRaycaster>();
        RectTransform R = cGO.GetComponent<RectTransform>();

        Img(R, "BG", BG, V(0,0), V(1,1), V(0,0), V(0,0));

        RectTransform hdr = Img(R, "Hdr", HEADER, V(0,1), V(1,1), V(0,-44), V(0,88));
        Img(hdr, "Line", ACCENT, V(0,0), V(1,0), V(0,1.5f), V(0,3));

        TextMeshProUGUI t1 = Txt(hdr, "T", "Gestion de recursos", Color.white, 42, V(0.03f,0), V(0.70f,1));
        t1.fontStyle = FontStyles.Bold;
        t1.alignment = TextAlignmentOptions.MidlineLeft;

        TextMeshProUGUI t2 = Txt(hdr, "I", "Usa tus estrellas para llegar al 100%", DIM, 24, V(0.45f,0), V(0.98f,1));
        t2.alignment = TextAlignmentOptions.MidlineRight;

        RectTransform topP = Img(R, "TopP", PANEL, V(0.03f,0.58f), V(0.97f,0.88f), V(0,0), V(0,0));
        BuildBars(topP);

        RectTransform botP = Img(R, "BotP", PANEL, V(0.03f,0.12f), V(0.97f,0.55f), V(0,0), V(0,0));
        BuildActions(botP);

        RectTransform bar = Img(R, "Bar", HEADER, V(0,0), V(1,0), V(0,45), V(0,90));
        MkBtn(bar, "Reiniciar", new Color(0.32f,0.34f,0.44f), V(0.06f,0.12f), V(0.94f,0.88f), () => Reset());
    }

    private void BuildBars(RectTransform p)
    {

        TextMeshProUGUI sT = Txt(p, "ST", "ESTRELLAS", YELLOW, 28, V(0.03f,0.70f), V(0.30f,0.95f));
        sT.fontStyle = FontStyles.Bold; sT.alignment = TextAlignmentOptions.MidlineLeft;

        _starsLbl = Txt(p, "SV", "", Color.white, 26, V(0.30f,0.70f), V(0.48f,0.95f));
        _starsLbl.alignment = TextAlignmentOptions.MidlineRight;

        RectTransform sBg = Img(p, "SBg", DARK, V(0.03f,0.35f), V(0.48f,0.70f), V(0,0), V(0,0));
        GameObject sf = new GameObject("SF");
        sf.transform.SetParent(sBg, false);
        _starsFill = sf.AddComponent<RectTransform>();
        _starsFill.anchorMin = V(0,0); _starsFill.anchorMax = V(1,1);
        _starsFill.offsetMin = V(0,0); _starsFill.offsetMax = V(0,0);
        _starsFillImg = sf.AddComponent<Image>();
        _starsFillImg.color = YELLOW;

        TextMeshProUGUI sH = Txt(p, "SH", "Cuantas estrellas te quedan", DIM, 19, V(0.03f,0.06f), V(0.48f,0.34f));
        sH.alignment = TextAlignmentOptions.Center;

        TextMeshProUGUI pT = Txt(p, "PT", "PROGRESO", GREEN, 28, V(0.53f,0.70f), V(0.76f,0.95f));
        pT.fontStyle = FontStyles.Bold; pT.alignment = TextAlignmentOptions.MidlineLeft;

        _progLbl = Txt(p, "PV", "0%", Color.white, 26, V(0.76f,0.70f), V(0.97f,0.95f));
        _progLbl.alignment = TextAlignmentOptions.MidlineRight;

        RectTransform pBg = Img(p, "PBg", DARK, V(0.53f,0.35f), V(0.97f,0.70f), V(0,0), V(0,0));
        GameObject pf = new GameObject("PF");
        pf.transform.SetParent(pBg, false);
        _progFill = pf.AddComponent<RectTransform>();
        _progFill.anchorMin = V(0,0); _progFill.anchorMax = V(0,1);
        _progFill.offsetMin = V(0,0); _progFill.offsetMax = V(0,0);
        pf.AddComponent<Image>().color = GREEN;

        _progPct = Txt(p, "PP", "0%", Color.white, 42, V(0.53f,0.06f), V(0.97f,0.34f));
        _progPct.fontStyle = FontStyles.Bold;
        _progPct.alignment = TextAlignmentOptions.Center;
    }

    private void BuildActions(RectTransform p)
    {
        TextMeshProUGUI at = Txt(p, "AT", "Elige una accion:", Color.white, 28, V(0.03f,0.88f), V(0.97f,1f));
        at.fontStyle = FontStyles.Bold; at.alignment = TextAlignmentOptions.Center;

        int n = Mathf.Min(actions.Count, 5);
        _btns.Clear(); _btnBgs.Clear(); _btnGroups.Clear();

        float margin = 0.03f;
        float gap = 0.02f;
        float total = 1f - margin * 2f;
        float w = (total - gap * (n - 1)) / n;

        for (int i = 0; i < n; i++)
        {
            int idx = i;
            ActionData a = actions[i];
            Color col = GetBtnColor(i);

            float xL = margin + (w + gap) * i;
            float xR = xL + w;

            RectTransform brt = Img(p, "A" + i, col, V(xL, 0.04f), V(xR, 0.85f), V(0,0), V(0,0));
            CanvasGroup cg = brt.gameObject.AddComponent<CanvasGroup>();

            Button btn = brt.gameObject.AddComponent<Button>();
            Image bg = brt.GetComponent<Image>();
            btn.targetGraphic = bg;
            ColorBlock cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1,1,1,0.85f);
            cb.pressedColor = new Color(0.7f,0.7f,0.7f);
            cb.disabledColor = new Color(0.5f,0.5f,0.5f,0.4f);
            btn.colors = cb;
            btn.onClick.AddListener(() => DoAction(idx));
            ButtonJuice.Attach(brt.gameObject);

            float nameSize = n >= 5 ? 27f : (n >= 4 ? 30f : 34f);
            TextMeshProUGUI nm = Txt(brt, "Nm", a.actionName, Color.white, nameSize, V(0.04f,0.38f), V(0.96f,0.95f));
            nm.fontStyle = FontStyles.Bold;
            nm.alignment = TextAlignmentOptions.Center;

            RectTransform infoBg = Img(brt, "Info", new Color(0,0,0,0.25f), V(0.06f,0.04f), V(0.94f,0.34f), V(0,0), V(0,0));

            TextMeshProUGUI costT = Txt(infoBg, "C", "-" + a.cost + " E", YELLOW, 22, V(0,0.5f), V(1,1));
            costT.fontStyle = FontStyles.Bold; costT.alignment = TextAlignmentOptions.Center;

            string gainTxt = a.isRisky
                ? "+" + a.riskyMin + "-" + a.riskyMax + "% ?"
                : "+" + a.progress + "%";
            TextMeshProUGUI gainT = Txt(infoBg, "G", gainTxt, GREEN, 22, V(0,0), V(1,0.5f));
            gainT.fontStyle = FontStyles.Bold; gainT.alignment = TextAlignmentOptions.Center;

            _btns.Add(btn);
            _btnBgs.Add(bg);
            _btnGroups.Add(cg);
        }
    }

    private static Vector2 V(float x, float y) { return new Vector2(x, y); }

    private RectTransform Img(RectTransform parent, string name, Color c,
                              Vector2 amin, Vector2 amax, Vector2 pos, Vector2 sd)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = amin; rt.anchorMax = amax;
        rt.pivot = V(0.5f, 0.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = sd;
        go.AddComponent<Image>().color = c;
        return rt;
    }

    private TextMeshProUGUI Txt(RectTransform parent, string name, string text,
                                Color c, float size, Vector2 amin, Vector2 amax)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = amin; rt.anchorMax = amax;
        rt.pivot = V(0.5f, 0.5f);
        rt.anchoredPosition = V(0,0); rt.sizeDelta = V(0,0);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.color = c; tmp.fontSize = size;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        return tmp;
    }

    private void MkBtn(RectTransform parent, string label, Color bgC,
                       Vector2 amin, Vector2 amax,
                       UnityEngine.Events.UnityAction click)
    {
        RectTransform bg = Img(parent, "B_" + label, bgC, amin, amax, V(0,0), V(0,0));
        Button b = bg.gameObject.AddComponent<Button>();
        b.targetGraphic = bg.GetComponent<Image>();
        ColorBlock cb = b.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1,1,1,0.85f);
        cb.pressedColor = new Color(0.7f,0.7f,0.7f);
        b.colors = cb;
        b.onClick.AddListener(click);
        ButtonJuice.Attach(bg.gameObject);
        TextMeshProUGUI t = Txt(bg, "T", label, Color.white, 28, V(0,0), V(1,1));
        t.fontStyle = FontStyles.Bold;
    }

    private static void EnsureES()
    {
        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }
    }
}
