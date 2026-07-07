// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI por codigo del Flanker infantil: fila de peces dibujados con Images
/// (cuerpo ovalado + cola en rombo + ojo), pez central naranja y mas grande,
/// botones gigantes ◄ ► y barra de tiempo opcional.
/// </summary>
public class DontFollowMajorityUIController : MonoBehaviour
{
    static Vector2 V(float x, float y) => new Vector2(x, y);
    static Color   C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

    static readonly Color BG       = C(0.04f, 0.10f, 0.18f);   // "agua"
    static readonly Color HDR      = C(0.03f, 0.05f, 0.10f);
    static readonly Color ACCENT   = new Color(0.95f, 0.55f, 0.12f);  // naranja categoria
    static readonly Color DIM      = C(0.45f, 0.60f, 0.72f);
    static readonly Color CGREEN   = C(0.22f, 0.86f, 0.54f);
    static readonly Color CRED     = C(0.90f, 0.22f, 0.28f);
    static readonly Color FISH_BLU = C(0.32f, 0.62f, 0.95f);
    static readonly Color FISH_ORG = C(0.98f, 0.60f, 0.12f);

    const int MAX_FISH = 7;

    public RectTransform CenterFishRect { get; private set; }

    RectTransform[] _fishSlots;
    RectTransform   _fishRow;
    TextMeshProUGUI _statusText;
    TextMeshProUGUI _progressText;
    Image           _timerFill;
    GameObject      _timerBarGO;

    public void BuildUI(int fishCount, bool useTimer, DontFollowMajorityInputHandler input)
    {
        var cGO = new GameObject("Canvas_DFM");
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

        // burbujas decorativas
        for (int i = 0; i < 8; i++)
        {
            float bx = 0.06f + (i * 0.125f);
            float by = 0.10f + ((i * 37) % 60) / 100f;
            float bs = 14f + (i * 13) % 26;
            var b = MkImg(R, "Bub" + i, C(1, 1, 1, 0.045f),
                          V(bx, by), V(bx, by), V(0, 0), V(bs, bs));
            b.GetComponent<Image>().sprite = MakeCircleSprite(32);
        }

        // ------------------------------------------------ cabecera
        var hdr = MkImg(R, "Hdr", HDR, V(0, 1), V(1, 1), V(0, -44), V(0, 88));
        MkImg(hdr, "LineB", ACCENT, V(0, 0), V(1, 0), V(0, 1.5f), V(0, 3));
        MkImg(hdr, "AccL",  ACCENT, V(0, 0.18f), V(0, 0.82f), V(3, 0), V(6, 0));

        var ttl = MkTxt(hdr, "T", "NO SIGAS A LA MAYORIA", Color.white, 28,
                        V(0.03f, 0.12f), V(0.50f, 0.88f));
        ttl.fontStyle = FontStyles.Bold;
        ttl.alignment = TextAlignmentOptions.MidlineLeft;
        ttl.characterSpacing = 1.5f;

        MkTxt(hdr, "Cat", "CONTROL DE IMPULSOS", DIM, 15,
              V(0.50f, 0.12f), V(0.74f, 0.88f)).alignment = TextAlignmentOptions.MidlineRight;

        _progressText = MkTxt(hdr, "Prog", "", C(0.90f, 0.92f, 0.96f), 22,
                              V(0.76f, 0.12f), V(0.98f, 0.88f));
        _progressText.fontStyle = FontStyles.Bold;
        _progressText.alignment = TextAlignmentOptions.MidlineRight;

        // ------------------------------------------------ fila de peces
        var rowGO = new GameObject("FishRow");
        rowGO.transform.SetParent(R, false);
        _fishRow = rowGO.AddComponent<RectTransform>();
        _fishRow.anchorMin = _fishRow.anchorMax = V(0.5f, 0.60f);
        _fishRow.pivot     = V(0.5f, 0.5f);
        _fishRow.sizeDelta = V(1400f, 200f);
        _fishRow.anchoredPosition = Vector2.zero;

        _fishSlots = new RectTransform[MAX_FISH];
        for (int i = 0; i < MAX_FISH; i++)
        {
            _fishSlots[i] = BuildFish(_fishRow, i);
            _fishSlots[i].gameObject.SetActive(false);
        }

        // ------------------------------------------------ barra de tiempo
        _timerBarGO = new GameObject("TimerBar");
        _timerBarGO.transform.SetParent(R, false);
        var tbRT = _timerBarGO.AddComponent<RectTransform>();
        tbRT.anchorMin = V(0.30f, 0.44f); tbRT.anchorMax = V(0.70f, 0.46f);
        tbRT.sizeDelta = tbRT.anchoredPosition = Vector2.zero;
        _timerBarGO.AddComponent<Image>().color = C(0.02f, 0.04f, 0.09f);

        var fillGO = new GameObject("Fill");
        fillGO.transform.SetParent(tbRT, false);
        var fillRT = fillGO.AddComponent<RectTransform>();
        fillRT.anchorMin = V(0, 0); fillRT.anchorMax = V(1, 1);
        fillRT.sizeDelta = fillRT.anchoredPosition = Vector2.zero;
        _timerFill            = fillGO.AddComponent<Image>();
        _timerFill.color      = CGREEN;
        _timerFill.type       = Image.Type.Filled;
        _timerFill.fillMethod = Image.FillMethod.Horizontal;
        _timerFill.fillOrigin = 0;
        _timerBarGO.SetActive(useTimer);

        // ------------------------------------------------ estado
        _statusText = MkTxt(R, "Status", "¿Hacia donde mira el pez naranja?",
                            DIM, 28, V(0.10f, 0.35f), V(0.90f, 0.42f));
        _statusText.fontStyle = FontStyles.Bold;

        // ------------------------------------------------ botones ◄ ►
        BuildAnswerButton(R, false, input);
        BuildAnswerButton(R, true,  input);

        MkTxt(R, "Hint", "(tambien valen las flechas del teclado)", DIM, 15,
              V(0.30f, 0.02f), V(0.70f, 0.06f));
    }

    void BuildAnswerButton(RectTransform R, bool right, DontFollowMajorityInputHandler input)
    {
        Vector2 am = right ? V(0.56f, 0.09f) : V(0.24f, 0.09f);
        Vector2 aM = right ? V(0.76f, 0.28f) : V(0.44f, 0.28f);

        // "<" y ">" existen en la fuente por defecto (las flechas ◄► no)
        var btn = KidUI.Btn(R, right ? ">" : "<", C(0.12f, 0.20f, 0.36f),
                            am, aM, () => { if (input != null) input.Press(right); }, 96f);
        var lbl = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (lbl != null) { lbl.color = CGREEN; lbl.fontStyle = FontStyles.Bold; }
    }

    // ================================================================ peces

    RectTransform BuildFish(RectTransform parent, int index)
    {
        var slotGO = new GameObject("Fish_" + index);
        slotGO.transform.SetParent(parent, false);
        var slot = slotGO.AddComponent<RectTransform>();
        slot.pivot     = V(0.5f, 0.5f);
        slot.sizeDelta = V(150f, 100f);
        slot.anchorMin = slot.anchorMax = V(0.5f, 0.5f);

        // cola (rombo) — detras, a la izquierda cuando mira a la derecha
        var tail = new GameObject("Tail");
        tail.transform.SetParent(slot, false);
        var tailRT = tail.AddComponent<RectTransform>();
        tailRT.pivot            = V(0.5f, 0.5f);
        tailRT.sizeDelta        = V(42f, 42f);
        tailRT.anchoredPosition = V(-58f, 0f);
        tailRT.localRotation    = Quaternion.Euler(0, 0, 45f);
        tail.AddComponent<Image>();

        // cuerpo (ovalo con sprite circular escalado)
        var body = new GameObject("Body");
        body.transform.SetParent(slot, false);
        var bodyRT = body.AddComponent<RectTransform>();
        bodyRT.pivot            = V(0.5f, 0.5f);
        bodyRT.sizeDelta        = V(104f, 62f);
        bodyRT.anchoredPosition = V(0f, 0f);
        var bodyImg = body.AddComponent<Image>();
        bodyImg.sprite = MakeCircleSprite(128);

        // ojo (blanco + pupila), en el morro (derecha por defecto)
        var eye = new GameObject("Eye");
        eye.transform.SetParent(slot, false);
        var eyeRT = eye.AddComponent<RectTransform>();
        eyeRT.pivot            = V(0.5f, 0.5f);
        eyeRT.sizeDelta        = V(20f, 20f);
        eyeRT.anchoredPosition = V(30f, 10f);
        var eyeImg = eye.AddComponent<Image>();
        eyeImg.sprite = MakeCircleSprite(32);
        eyeImg.color  = Color.white;

        var pupil = new GameObject("Pupil");
        pupil.transform.SetParent(eyeRT, false);
        var pupRT = pupil.AddComponent<RectTransform>();
        pupRT.pivot            = V(0.5f, 0.5f);
        pupRT.sizeDelta        = V(9f, 9f);
        pupRT.anchoredPosition = V(3f, 0f);
        var pupImg = pupil.AddComponent<Image>();
        pupImg.sprite = MakeCircleSprite(32);
        pupImg.color  = C(0.05f, 0.08f, 0.14f);

        return slot;
    }

    void TintFish(RectTransform slot, Color col)
    {
        var tail = slot.Find("Tail");
        var body = slot.Find("Body");
        if (tail != null) tail.GetComponent<Image>().color =
            new Color(col.r * 0.82f, col.g * 0.82f, col.b * 0.82f);
        if (body != null) body.GetComponent<Image>().color = col;
    }

    /// <summary>Coloca y orienta la fila de peces para una ronda.</summary>
    public void ShowFish(bool centerRight, bool majorityRight, int fishCount)
    {
        fishCount = Mathf.Clamp(fishCount, 3, MAX_FISH);
        int center = fishCount / 2;
        float spacing = fishCount > 5 ? 185f : 230f;

        for (int i = 0; i < MAX_FISH; i++)
        {
            bool active = i < fishCount;
            _fishSlots[i].gameObject.SetActive(active);
            if (!active) continue;

            bool isCenter = (i == center);
            bool faceRight = isCenter ? centerRight : majorityRight;

            _fishSlots[i].anchoredPosition = V((i - center) * spacing, 0f);
            float s = isCenter ? 1.30f : 1.0f;
            _fishSlots[i].localScale = new Vector3(faceRight ? s : -s, s, 1f);

            TintFish(_fishSlots[i], isCenter ? FISH_ORG : FISH_BLU);

            if (isCenter) CenterFishRect = _fishSlots[i];
        }

        UITween.PopIn(_fishRow, 0.22f, 0.75f);
        if (_timerFill != null) _timerFill.fillAmount = 1f;
        SetStatus("¿Hacia donde mira el pez naranja?", DIM);
    }

    public void HideFish()
    {
        if (_fishSlots == null) return;
        foreach (var s in _fishSlots)
            if (s != null) s.gameObject.SetActive(false);
        if (_timerFill != null) _timerFill.fillAmount = 0f;
    }

    public void UpdateTimerBar(float frac)
    {
        if (_timerFill == null) return;
        frac = Mathf.Clamp01(frac);
        _timerFill.fillAmount = frac;
        _timerFill.color = Color.Lerp(CRED, CGREEN, frac);
    }

    public void SetProgress(int correct, int done, int total)
    {
        if (_progressText != null)
            _progressText.text = correct + " aciertos  ·  " + done + "/" + total;
    }

    public void ShowRoundFeedback(bool good, string msg, Color col)
    {
        SetStatus(msg, col);
        if (good && CenterFishRect != null)
            UITween.PulseOnce(CenterFishRect, 1.18f, 0.25f);
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
