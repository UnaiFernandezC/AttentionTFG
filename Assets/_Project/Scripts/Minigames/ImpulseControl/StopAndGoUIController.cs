// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI por codigo del GO/NO-GO: estimulo circular central con anillo-temporizador,
/// barra de progreso superior y leyenda inferior. Todo tactil: el propio circulo
/// es un boton gigante (y tambien vale ESPACIO / clic en cualquier parte).
/// </summary>
public class StopAndGoUIController : MonoBehaviour
{
    static Vector2 V(float x, float y) => new Vector2(x, y);
    static Color   C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

    static readonly Color BG       = C(0.05f, 0.08f, 0.14f);
    static readonly Color HDR      = C(0.03f, 0.05f, 0.10f);
    static readonly Color ACCENT   = new Color(0.95f, 0.55f, 0.12f);   // naranja categoria
    static readonly Color DIM      = C(0.40f, 0.55f, 0.65f);
    static readonly Color C_GO     = C(0.18f, 0.82f, 0.45f);
    static readonly Color C_NOGO   = C(0.90f, 0.20f, 0.24f);
    static readonly Color C_SURPR  = C(0.96f, 0.58f, 0.10f);
    static readonly Color C_IDLE   = C(0.10f, 0.14f, 0.24f);

    public RectTransform StimulusRect => _stimRT;

    RectTransform   _stimRT;
    Image           _stimImg;
    Image           _ringImg;
    TextMeshProUGUI _stimLabel;
    TextMeshProUGUI _statusText;
    Image           _progressFill;
    TextMeshProUGUI _progressText;

    public void BuildUI(bool hardMode, StopAndGoInputHandler input)
    {
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

        MkImg(R, "BG", BG, V(0, 0), V(1, 1), V(0, 0), V(0, 0));

        // ------------------------------------------------ cabecera
        var hdr = MkImg(R, "Hdr", HDR, V(0, 1), V(1, 1), V(0, -44), V(0, 88));
        MkImg(hdr, "LineB", ACCENT, V(0, 0), V(1, 0), V(0, 1.5f), V(0, 3));
        MkImg(hdr, "AccL",  ACCENT, V(0, 0.18f), V(0, 0.82f), V(3, 0), V(6, 0));

        var ttl = MkTxt(hdr, "T", "STOP & GO", Color.white, 30,
                        V(0.03f, 0.12f), V(0.45f, 0.88f));
        ttl.fontStyle = FontStyles.Bold;
        ttl.alignment = TextAlignmentOptions.MidlineLeft;
        ttl.characterSpacing = 2f;

        MkTxt(hdr, "Cat", "CONTROL DE IMPULSOS", DIM, 15,
              V(0.45f, 0.12f), V(0.70f, 0.88f)).alignment = TextAlignmentOptions.MidlineRight;

        _progressText = MkTxt(hdr, "Prog", "", C(0.90f, 0.92f, 0.96f), 22,
                              V(0.74f, 0.12f), V(0.98f, 0.88f));
        _progressText.fontStyle = FontStyles.Bold;
        _progressText.alignment = TextAlignmentOptions.MidlineRight;

        // ------------------------------------------------ barra de progreso
        var barBG = MkImg(R, "ProgBG", C(0.02f, 0.04f, 0.09f),
                          V(0.20f, 1f), V(0.80f, 1f), V(0, -112f), V(0, 14f));
        var fillGO = new GameObject("Fill");
        fillGO.transform.SetParent(barBG, false);
        var fillRT = fillGO.AddComponent<RectTransform>();
        fillRT.anchorMin = V(0, 0); fillRT.anchorMax = V(1, 1);
        fillRT.sizeDelta = fillRT.anchoredPosition = Vector2.zero;
        _progressFill            = fillGO.AddComponent<Image>();
        _progressFill.color      = ACCENT;
        _progressFill.type       = Image.Type.Filled;
        _progressFill.fillMethod = Image.FillMethod.Horizontal;
        _progressFill.fillOrigin = 0;
        _progressFill.fillAmount = 0f;

        // ------------------------------------------------ estimulo central
        // anillo temporizador (detras)
        var ringGO = new GameObject("Ring");
        ringGO.transform.SetParent(R, false);
        var ringRT = ringGO.AddComponent<RectTransform>();
        ringRT.anchorMin = ringRT.anchorMax = V(0.5f, 0.52f);
        ringRT.pivot     = V(0.5f, 0.5f);
        ringRT.sizeDelta = V(400f, 400f);
        ringRT.anchoredPosition = Vector2.zero;
        _ringImg            = ringGO.AddComponent<Image>();
        _ringImg.sprite     = MakeCircleSprite(256);
        _ringImg.type       = Image.Type.Filled;
        _ringImg.fillMethod = Image.FillMethod.Radial360;
        _ringImg.fillOrigin = 2;   // arriba
        _ringImg.fillClockwise = false;
        _ringImg.color      = C(1f, 1f, 1f, 0.14f);
        _ringImg.fillAmount = 0f;

        var stimGO = new GameObject("Stimulus");
        stimGO.transform.SetParent(R, false);
        _stimRT = stimGO.AddComponent<RectTransform>();
        _stimRT.anchorMin = _stimRT.anchorMax = V(0.5f, 0.52f);
        _stimRT.pivot     = V(0.5f, 0.5f);
        _stimRT.sizeDelta = V(340f, 340f);
        _stimRT.anchoredPosition = Vector2.zero;
        _stimImg        = stimGO.AddComponent<Image>();
        _stimImg.sprite = MakeCircleSprite(256);
        _stimImg.color  = C_IDLE;

        var btn = stimGO.AddComponent<Button>();
        btn.targetGraphic = _stimImg;
        btn.onClick.AddListener(() => { if (input != null) input.Press(); });

        _stimLabel = MkTxt(_stimRT, "Lbl", "", Color.white, 52, V(0, 0), V(1, 1));
        _stimLabel.fontStyle = FontStyles.Bold;

        // ------------------------------------------------ estado + leyenda
        _statusText = MkTxt(R, "Status", "", DIM, 30, V(0.15f, 0.22f), V(0.85f, 0.30f));
        _statusText.fontStyle = FontStyles.Bold;

        var bot = MkImg(R, "Bot", HDR, V(0, 0), V(1, 0), V(0, 45), V(0, 90));
        MkImg(bot, "LineT", ACCENT, V(0, 1), V(1, 1), V(0, -1.5f), V(0, 3));
        BuildLegend(bot, hardMode);
    }

    void BuildLegend(RectTransform bot, bool hardMode)
    {
        float x = hardMode ? 0.16f : 0.26f;
        LegendItem(bot, C_GO,   "VERDE  ¡toca!",   x);
        LegendItem(bot, C_NOGO, "ROJO  ¡quieto!",  x + 0.24f);
        if (hardMode)
            LegendItem(bot, C_SURPR, "NARANJA  ¡quieto!", x + 0.48f);
    }

    void LegendItem(RectTransform p, Color col, string label, float ax)
    {
        var dot = new GameObject("Dot");
        dot.transform.SetParent(p, false);
        var rt = dot.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = V(ax, 0.5f);
        rt.pivot = V(0.5f, 0.5f);
        rt.sizeDelta = V(30f, 30f);
        rt.anchoredPosition = Vector2.zero;
        var img = dot.AddComponent<Image>();
        img.sprite = MakeCircleSprite(64);
        img.color  = col;

        var t = MkTxt(p, "L_" + label, label, C(0.85f, 0.88f, 0.94f), 20,
                      V(ax + 0.012f, 0f), V(ax + 0.22f, 1f));
        t.alignment = TextAlignmentOptions.MidlineLeft;
        t.fontStyle = FontStyles.Bold;
    }

    // ================================================================ API

    public void ShowStimulus(StopAndGoGameManager.StimType st)
    {
        switch (st)
        {
            case StopAndGoGameManager.StimType.Go:
                _stimImg.color  = C_GO;
                _stimLabel.text = "¡TOCA!";
                break;
            case StopAndGoGameManager.StimType.Surprise:
                _stimImg.color  = C_SURPR;
                _stimLabel.text = "¡QUIETO!";
                break;
            default:
                _stimImg.color  = C_NOGO;
                _stimLabel.text = "¡QUIETO!";
                break;
        }
        _ringImg.fillAmount = 1f;
        UITween.PopIn(_stimRT, 0.16f, 0.55f);
        SetStatus("", DIM);
    }

    public void ShowStimulusResult(bool good)
    {
        if (good) UITween.PulseOnce(_stimRT, 1.12f, 0.20f);
        _stimImg.color = good ? C(_stimImg.color.r, _stimImg.color.g, _stimImg.color.b, 0.55f)
                              : C(0.35f, 0.16f, 0.18f);
    }

    public void HideStimulus()
    {
        _stimImg.color      = C_IDLE;
        _stimLabel.text     = "";
        _ringImg.fillAmount = 0f;
    }

    public void UpdateTimerRing(float frac)
    {
        if (_ringImg != null) _ringImg.fillAmount = Mathf.Clamp01(frac);
    }

    public void SetProgress(int done, int total)
    {
        if (_progressFill != null)
            _progressFill.fillAmount = total > 0 ? (float)done / total : 0f;
        if (_progressText != null)
            _progressText.text = done + " / " + total;
    }

    public void SetStatus(string txt, Color col)
    {
        if (_statusText == null) return;
        _statusText.text  = txt;
        _statusText.color = col;
    }

    // ================================================================ helpers

    public static Sprite MakeCircleSprite(int res = 128)
    {
        var tex = new Texture2D(res, res, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        var center = new Vector2(res * 0.5f, res * 0.5f);
        float r = res * 0.5f;
        var px = new Color[res * res];
        for (int y = 0; y < res; y++)
            for (int x = 0; x < res; x++)
            {
                float d = Vector2.Distance(new Vector2(x + .5f, y + .5f), center);
                float a = Mathf.Clamp01(1f - (d - r + 1.5f) / 2f);
                px[y * res + x] = new Color(1, 1, 1, a);
            }
        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, res, res), V(0.5f, 0.5f));
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

    TextMeshProUGUI MkTxt(RectTransform p, string n, string txt, Color col,
                          float sz, Vector2 am, Vector2 aM)
    {
        var go = new GameObject(n);
        go.transform.SetParent(p, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = am; rt.anchorMax = aM;
        rt.pivot = V(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero; rt.sizeDelta = Vector2.zero;
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = txt; t.color = col; t.fontSize = sz;
        t.alignment    = TextAlignmentOptions.Center;
        t.overflowMode = TextOverflowModes.Overflow;
        return t;
    }
}
