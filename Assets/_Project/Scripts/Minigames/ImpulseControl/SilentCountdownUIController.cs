// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI por codigo del "semaforo escondido": circulo-semaforo central con el
/// numero de la cuenta, que se tapa con "?" cuando la cuenta se esconde,
/// boton grande "¡AHORA!" y puntos de ronda.
/// </summary>
public class SilentCountdownUIController : MonoBehaviour
{
    static Vector2 V(float x, float y) => new Vector2(x, y);
    static Color   C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

    static readonly Color BG      = C(0.05f, 0.08f, 0.14f);
    static readonly Color HDR     = C(0.03f, 0.05f, 0.10f);
    static readonly Color ACCENT  = new Color(0.95f, 0.55f, 0.12f);   // naranja categoria
    static readonly Color DIM     = C(0.40f, 0.55f, 0.65f);
    static readonly Color CGREEN  = C(0.22f, 0.86f, 0.54f);
    static readonly Color CRED    = C(0.90f, 0.22f, 0.28f);
    static readonly Color SEM_ON  = C(0.16f, 0.55f, 0.95f);
    static readonly Color SEM_OFF = C(0.10f, 0.13f, 0.24f);

    public RectTransform SemaphoreRect => _semRT;
    public RectTransform ButtonRect    => _btnRT;

    RectTransform   _semRT;
    Image           _semImg;
    TextMeshProUGUI _numberText;
    TextMeshProUGUI _statusText;
    TextMeshProUGUI _roundLabel;
    RectTransform   _btnRT;
    Image[]         _roundDots;

    public void BuildUI(int rounds, SilentCountdownInputHandler input)
    {
        var cGO = new GameObject("Canvas_SilentCountdown");
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

        var ttl = MkTxt(hdr, "T", "EL SEMAFORO ESCONDIDO", Color.white, 28,
                        V(0.03f, 0.12f), V(0.50f, 0.88f));
        ttl.fontStyle = FontStyles.Bold;
        ttl.alignment = TextAlignmentOptions.MidlineLeft;
        ttl.characterSpacing = 1.5f;

        MkTxt(hdr, "Cat", "CONTROL DE IMPULSOS", DIM, 15,
              V(0.50f, 0.12f), V(0.72f, 0.88f)).alignment = TextAlignmentOptions.MidlineRight;

        _roundLabel = MkTxt(hdr, "Round", "", C(0.90f, 0.92f, 0.96f), 22,
                            V(0.74f, 0.12f), V(0.86f, 0.88f));
        _roundLabel.fontStyle = FontStyles.Bold;
        _roundLabel.alignment = TextAlignmentOptions.MidlineRight;

        _roundDots = BuildRoundDots(hdr, rounds);

        // ------------------------------------------------ semaforo central
        var glow = MkImg(R, "Glow", C(SEM_ON.r, SEM_ON.g, SEM_ON.b, 0.12f),
                         V(0.5f, 0.58f), V(0.5f, 0.58f), V(0, 0), V(430f, 430f));
        glow.GetComponent<Image>().sprite = MakeCircleSprite(128);

        var semGO = new GameObject("Semaphore");
        semGO.transform.SetParent(R, false);
        _semRT = semGO.AddComponent<RectTransform>();
        _semRT.anchorMin = _semRT.anchorMax = V(0.5f, 0.58f);
        _semRT.pivot     = V(0.5f, 0.5f);
        _semRT.sizeDelta = V(360f, 360f);
        _semRT.anchoredPosition = Vector2.zero;
        _semImg        = semGO.AddComponent<Image>();
        _semImg.sprite = MakeCircleSprite(256);
        _semImg.color  = SEM_ON;

        _numberText = MkTxt(_semRT, "Num", "", Color.white, 160, V(0, 0), V(1, 1));
        _numberText.fontStyle = FontStyles.Bold;

        // ------------------------------------------------ estado
        _statusText = MkTxt(R, "Status", "", DIM, 30, V(0.10f, 0.30f), V(0.90f, 0.38f));
        _statusText.fontStyle = FontStyles.Bold;

        // ------------------------------------------------ boton ¡AHORA!
        var btn = KidUI.Btn(R, "¡AHORA!", ACCENT,
                            V(0.38f, 0.08f), V(0.62f, 0.22f),
                            () => { if (input != null) input.Press(); }, 44f);
        _btnRT = btn.GetComponent<RectTransform>();

        MkTxt(R, "Hint", "(o pulsa ESPACIO)", DIM, 16, V(0.38f, 0.03f), V(0.62f, 0.07f));
    }

    Image[] BuildRoundDots(RectTransform hdr, int rounds)
    {
        var dots = new Image[rounds];
        float startX = 0.88f;
        float spacing = 0.022f;
        for (int i = 0; i < rounds; i++)
        {
            var gO = new GameObject("Dot_" + i);
            gO.transform.SetParent(hdr, false);
            var rt = gO.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = V(startX + i * spacing, 0.5f);
            rt.pivot     = V(0.5f, 0.5f);
            rt.sizeDelta = V(22f, 22f);
            rt.anchoredPosition = Vector2.zero;
            var img = gO.AddComponent<Image>();
            img.sprite = MakeCircleSprite(32);
            img.color  = C(0.25f, 0.30f, 0.40f);
            dots[i] = img;
        }
        return dots;
    }

    // ================================================================ API

    public void SetRoundLabel(int current, int total)
    {
        if (_roundLabel != null) _roundLabel.text = "Ronda " + current + "/" + total;
    }

    public void ShowGetReady(int hideAt)
    {
        _semImg.color    = SEM_OFF;
        _numberText.text = "...";
        _numberText.color = DIM;
        SetStatus("Cuando el numero se esconda... ¡sigue contando tu!", DIM);
    }

    public void ShowNumber(int n)
    {
        _semImg.color     = SEM_ON;
        _numberText.text  = n.ToString();
        _numberText.color = Color.white;
        UITween.PulseOnce(_semRT, 1.06f, 0.18f);
        SetStatus("Pulsa justo cuando llegue a 0", DIM);
    }

    public void HideNumber()
    {
        _semImg.color     = SEM_OFF;
        _numberText.text  = "?";
        _numberText.color = C(0.55f, 0.62f, 0.78f);
        UITween.PulseOnce(_semRT, 1.10f, 0.25f);
        SetStatus("Shhh... cuenta en silencio", C(0.55f, 0.62f, 0.78f));
    }

    public void ShowRoundFeedback(bool good, string msg, Color col)
    {
        _semImg.color     = good ? CGREEN : CRED;
        _numberText.text  = good ? ":)" : ":(";
        _numberText.color = Color.white;
        SetStatus(msg, col);
    }

    public void SetRoundDot(int index, bool correct)
    {
        if (_roundDots == null || index < 0 || index >= _roundDots.Length) return;
        _roundDots[index].color = correct ? CGREEN : CRED;
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
