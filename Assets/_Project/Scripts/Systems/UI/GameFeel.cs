// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Kit de "juice" compartido por todos los minijuegos: partículas de confeti,
/// textos flotantes (+10), sacudidas, flashes de pantalla, contadores animados
/// y sonidos de éxito/error generados proceduralmente (sin assets).
/// Todo funciona con Time.timeScale = 0 (usa tiempo no escalado).
/// Uso típico:  GameFeel.Success(rt);  GameFeel.Error(rt);  GameFeel.Confetti();
/// </summary>
public static class GameFeel
{
    public static readonly Color[] CONFETTI_COLORS =
    {
        new Color(0.98f, 0.80f, 0.10f), new Color(0.30f, 0.65f, 1.00f),
        new Color(0.18f, 0.80f, 0.58f), new Color(0.95f, 0.55f, 0.12f),
        new Color(0.58f, 0.28f, 0.92f), new Color(0.92f, 0.35f, 0.55f)
    };

    // ============================================================ AUDIO PROCEDURAL

    static AudioSource _audio;
    static AudioClip _clipSuccess, _clipError, _clipPop, _clipStar;

    static void EnsureAudio()
    {
        if (_audio != null) return;
        var go = new GameObject("GameFeelAudio");
        Object.DontDestroyOnLoad(go);
        _audio = go.AddComponent<AudioSource>();
        _audio.playOnAwake = false;

        // Acorde ascendente C5-E5-G5 (éxito), zumbido grave (error),
        // blip corto (pop) y "ding" brillante (estrella).
        _clipSuccess = MakeArpeggio(new[] { 523.25f, 659.25f, 783.99f }, 0.11f);
        _clipError   = MakeBuzz(150f, 0.28f);
        _clipPop     = MakeSweep(650f, 950f, 0.07f);
        _clipStar    = MakeDing(1318.5f, 0.22f);
    }

    static AudioClip MakeArpeggio(float[] freqs, float noteDur)
    {
        const int SR = 44100;
        int noteSamples = (int)(SR * noteDur);
        var data = new float[noteSamples * freqs.Length];
        for (int n = 0; n < freqs.Length; n++)
            for (int i = 0; i < noteSamples; i++)
            {
                float t = (float)i / SR;
                float env = Mathf.Exp(-4f * i / (float)noteSamples);
                data[n * noteSamples + i] =
                    (Mathf.Sin(2f * Mathf.PI * freqs[n] * t) * 0.8f +
                     Mathf.Sin(4f * Mathf.PI * freqs[n] * t) * 0.2f) * env * 0.4f;
            }
        return ToClip("gf_success", data);
    }

    static AudioClip MakeBuzz(float freq, float dur)
    {
        const int SR = 44100;
        int total = (int)(SR * dur);
        var data = new float[total];
        for (int i = 0; i < total; i++)
        {
            float t = (float)i / SR;
            float env = Mathf.Exp(-6f * i / (float)total);
            float square = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * freq * t)) * 0.3f;
            float sine   = Mathf.Sin(2f * Mathf.PI * freq * 0.5f * t) * 0.7f;
            data[i] = (square + sine) * env * 0.30f;
        }
        return ToClip("gf_error", data);
    }

    static AudioClip MakeSweep(float f0, float f1, float dur)
    {
        const int SR = 44100;
        int total = (int)(SR * dur);
        var data = new float[total];
        float phase = 0f;
        for (int i = 0; i < total; i++)
        {
            float p = (float)i / total;
            float f = Mathf.Lerp(f0, f1, p);
            phase += 2f * Mathf.PI * f / SR;
            data[i] = Mathf.Sin(phase) * (1f - p) * 0.35f;
        }
        return ToClip("gf_pop", data);
    }

    static AudioClip MakeDing(float freq, float dur)
    {
        const int SR = 44100;
        int total = (int)(SR * dur);
        var data = new float[total];
        for (int i = 0; i < total; i++)
        {
            float t = (float)i / SR;
            float env = Mathf.Exp(-5f * i / (float)total);
            data[i] = (Mathf.Sin(2f * Mathf.PI * freq * t) * 0.7f +
                       Mathf.Sin(2f * Mathf.PI * freq * 1.5f * t) * 0.3f) * env * 0.35f;
        }
        return ToClip("gf_star", data);
    }

    static AudioClip ToClip(string name, float[] data)
    {
        var clip = AudioClip.Create(name, data.Length, 1, 44100, false);
        clip.SetData(data, 0);
        return clip;
    }

    public static void PlaySuccess() { EnsureAudio(); _audio.PlayOneShot(_clipSuccess, 0.9f); }
    public static void PlayError()   { EnsureAudio(); _audio.PlayOneShot(_clipError, 0.9f); }
    public static void PlayPop()     { EnsureAudio(); _audio.PlayOneShot(_clipPop, 0.8f); }
    public static void PlayStar()    { EnsureAudio(); _audio.PlayOneShot(_clipStar, 0.9f); }

    // ============================================================ CANVAS TRANSITORIO

    static Canvas MakeOverlay(float lifeSeconds)
    {
        var cvGO = new GameObject("GameFeelFX");
        var cv = cvGO.AddComponent<Canvas>();
        cv.renderMode = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 950;
        var sc = cvGO.AddComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920f, 1080f);
        sc.matchWidthOrHeight = 0.5f;
        Object.Destroy(cvGO, lifeSeconds);
        return cv;
    }

    // ============================================================ EFECTOS VISUALES

    /// <summary>Lluvia de confeti a pantalla completa (celebración).</summary>
    public static void Confetti(int count = 45)
    {
        var cv = MakeOverlay(3f);
        var R = cv.GetComponent<RectTransform>();
        for (int i = 0; i < count; i++)
        {
            var go = new GameObject("c");
            go.transform.SetParent(R, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.35f);
            float size = Random.Range(10f, 24f);
            rt.sizeDelta = new Vector2(size, size * Random.Range(0.5f, 1f));
            rt.anchoredPosition = new Vector2(Random.Range(-250f, 250f), Random.Range(-60f, 60f));
            var img = go.AddComponent<Image>();
            img.color = CONFETTI_COLORS[Random.Range(0, CONFETTI_COLORS.Length)];
            UITween.Run(ConfettiRoutine(rt, img));
        }
    }

    static IEnumerator ConfettiRoutine(RectTransform rt, Image img)
    {
        Vector2 vel = new Vector2(Random.Range(-380f, 380f), Random.Range(420f, 900f));
        float rotSpeed = Random.Range(-540f, 540f);
        float life = Random.Range(1.2f, 2.2f);
        float t = 0f;
        Color c = img.color;
        while (t < life)
        {
            if (rt == null) yield break;
            float dt = Time.unscaledDeltaTime;
            t += dt;
            vel.y -= 1400f * dt;
            rt.anchoredPosition += vel * dt;
            rt.Rotate(0, 0, rotSpeed * dt);
            float p = t / life;
            if (p > 0.65f) { c.a = 1f - (p - 0.65f) / 0.35f; img.color = c; }
            yield return null;
        }
        if (rt != null) Object.Destroy(rt.gameObject);
    }

    /// <summary>Texto flotante ("+10", "¡Bien!") que sube y se desvanece.
    /// screenAnchored: posición en coordenadas del canvas 1920x1080 respecto al centro
    /// (null = centro de pantalla).</summary>
    public static void FloatingText(string text, Color color, Vector2? screenAnchored = null,
                                    float fontSize = 52f)
    {
        var cv = MakeOverlay(1.4f);
        var R = cv.GetComponent<RectTransform>();
        var go = new GameObject("float");
        go.transform.SetParent(R, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(600f, 120f);
        rt.anchoredPosition = screenAnchored ?? Vector2.zero;
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = text; t.color = color; t.fontSize = fontSize;
        t.fontStyle = FontStyles.Bold;
        t.alignment = TextAlignmentOptions.Center;
        t.overflowMode = TextOverflowModes.Overflow;
        UITween.Run(FloatRoutine(rt, t));
    }

    static IEnumerator FloatRoutine(RectTransform rt, TextMeshProUGUI t)
    {
        Vector2 start = rt.anchoredPosition;
        rt.localScale = Vector3.one * 0.6f;
        float dur = 1.1f, e = 0f;
        Color c = t.color;
        while (e < dur)
        {
            if (rt == null) yield break;
            e += Time.unscaledDeltaTime;
            float p = e / dur;
            rt.anchoredPosition = start + new Vector2(0f, 90f * p);
            rt.localScale = Vector3.one * Mathf.Min(1f, 0.6f + p * 1.6f);
            c.a = p < 0.7f ? 1f : 1f - (p - 0.7f) / 0.3f;
            t.color = c;
            yield return null;
        }
    }

    /// <summary>Sacudida horizontal (error). Amplitud en píxeles de canvas.</summary>
    public static void Shake(RectTransform rt, float amplitude = 14f, float duration = 0.35f)
    {
        if (rt == null) return;
        UITween.Run(ShakeRoutine(rt, amplitude, duration));
    }

    static IEnumerator ShakeRoutine(RectTransform rt, float amp, float dur)
    {
        Vector2 basePos = rt.anchoredPosition;
        float t = 0f;
        while (t < dur)
        {
            if (rt == null) yield break;
            t += Time.unscaledDeltaTime;
            float decay = 1f - t / dur;
            rt.anchoredPosition = basePos + new Vector2(
                Mathf.Sin(t * 55f) * amp * decay, 0f);
            yield return null;
        }
        if (rt != null) rt.anchoredPosition = basePos;
    }

    /// <summary>Flash de color a pantalla completa (suave, p. ej. verde éxito / rojo error).</summary>
    public static void ScreenFlash(Color color, float maxAlpha = 0.22f, float duration = 0.30f)
    {
        var cv = MakeOverlay(duration + 0.1f);
        var R = cv.GetComponent<RectTransform>();
        var go = new GameObject("flash");
        go.transform.SetParent(R, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        var img = go.AddComponent<Image>();
        img.raycastTarget = false;
        UITween.Run(FlashRoutine(img, color, maxAlpha, duration));
    }

    static IEnumerator FlashRoutine(Image img, Color color, float maxA, float dur)
    {
        float t = 0f;
        while (t < dur)
        {
            if (img == null) yield break;
            t += Time.unscaledDeltaTime;
            float p = t / dur;
            float a = p < 0.3f ? (p / 0.3f) : 1f - (p - 0.3f) / 0.7f;
            img.color = new Color(color.r, color.g, color.b, a * maxA);
            yield return null;
        }
    }

    /// <summary>Contador animado (cuenta de from a to en el label).</summary>
    public static void CountUp(TMP_Text label, int from, int to, float duration = 0.8f,
                               string prefix = "", string suffix = "")
    {
        if (label == null) return;
        UITween.Run(CountRoutine(label, from, to, duration, prefix, suffix));
    }

    static IEnumerator CountRoutine(TMP_Text label, int from, int to, float dur,
                                    string prefix, string suffix)
    {
        float t = 0f;
        while (t < dur)
        {
            if (label == null) yield break;
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / dur);
            p = 1f - (1f - p) * (1f - p);   // ease-out
            label.text = prefix + Mathf.RoundToInt(Mathf.Lerp(from, to, p)) + suffix;
            yield return null;
        }
        if (label != null) label.text = prefix + to + suffix;
    }

    // ============================================================ ATAJOS COMBINADOS

    /// <summary>Feedback completo de acierto sobre un elemento: pop + sonido.</summary>
    public static void Success(RectTransform target, bool floatText = false)
    {
        PlaySuccess();
        if (target != null) UITween.PulseOnce(target, 1.15f, 0.28f);
        if (floatText) FloatingText("¡Bien!", new Color(0.18f, 0.80f, 0.58f));
    }

    /// <summary>Feedback completo de error: sacudida + flash rojo suave + sonido.</summary>
    public static void Error(RectTransform target)
    {
        PlayError();
        if (target != null) Shake(target);
        ScreenFlash(new Color(0.90f, 0.22f, 0.28f), 0.15f, 0.25f);
    }

    /// <summary>Estrellas 0-3 a partir del rendimiento (ratio 0-1). Fracaso = 0.
    /// En Difícil (TITAN) los umbrales bajan un poco: los desafíos ya son más
    /// exigentes de por sí, así que las estrellas deben seguir siendo alcanzables
    /// (sin regalarlas: 3 estrellas siguen pidiendo un 75% de rendimiento).</summary>
    public static int StarsFromRatio(bool success, float ratio)
    {
        if (!success) return 0;
        bool hard = GameManager.Instance != null &&
                    GameManager.Instance.CurrentDifficulty == DifficultyLevel.Hard;
        float tres = hard ? 0.75f : 0.85f;
        float dos  = hard ? 0.50f : 0.60f;
        if (ratio >= tres) return 3;
        if (ratio >= dos)  return 2;
        return 1;
    }
}
