// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Demos animadas "así se juega" ESPECÍFICAS por minijuego. Cada una es una pequeña
/// escena en bucle que enseña la mecánica (no un icono decorativo). Si un minijuego no
/// tiene demo propia, se recurre a la genérica (IntroDemo) — despacho a prueba de fallos.
/// Se engancha desde IntroPanel con el nombre del minijuego.
/// </summary>
public static class MinigameDemos
{
    public static void Attach(RectTransform card, string minigameName, string categoryName)
    {
        DemoKind kind = Classify(minigameName);
        if (kind == DemoKind.Generic)
        {
            IntroDemo.Attach(card, categoryName);   // demo genérica de siempre
            return;
        }

        Color col = IntroPanel.CategoryColor(categoryName);

        var stage = KidUI.RoundImg(card, "DemoStage", new Color(0.03f, 0.045f, 0.10f, 0.9f),
                                   new Vector2(0.58f, 0.34f), new Vector2(0.94f, 0.67f),
                                   Vector2.zero, Vector2.zero, 1.4f);
        stage.GetComponent<Image>().raycastTarget = false;
        // Canvas anidado: la animación de la demo no fuerza rebuilds del canvas del intro
        stage.gameObject.AddComponent<Canvas>();

        var lbl = KidUI.Txt(stage, "Lbl", "ASÍ SE JUEGA", KidUI.DIM, 14,
                            new Vector2(0.05f, 0.85f), new Vector2(0.95f, 0.99f));
        lbl.characterSpacing = 3f;
        lbl.fontStyle = FontStyles.Bold;

        var loop = stage.gameObject.AddComponent<MinigameDemoLoop>();
        loop.Init(kind, col);
    }

    static DemoKind Classify(string name)
    {
        string n = Norm(name);
        if (n.Contains("reacc") || n.Contains("rapid"))                 return DemoKind.QuickReaction;
        if (n.Contains("simon"))                                        return DemoKind.Simon;
        if (n.Contains("pulses") || n.Contains("todav") || n.Contains("nopul")) return DemoKind.DontPress;
        if (n.Contains("equilibr") || n.Contains("balance")) return DemoKind.Balance;
        if (n.Contains("orden"))                                        return DemoKind.Order;
        if (n.Contains("seguim") || n.Contains("tracking"))             return DemoKind.Tracking;
        if (n.Contains("regla"))                                        return DemoKind.RuleSwitch;
        if (n.Contains("laser"))                                        return DemoKind.LaserPath;
        if (n.Contains("cuadra"))                                       return DemoKind.FindOdd;
        if (n.Contains("numeric"))                                      return DemoKind.NumberPath;
        // Memoria
        if (n.Contains("dibujo"))                                       return DemoKind.PatternRecall;
        if (n.Contains("sutil"))                                        return DemoKind.FindChange;
        if (n.Contains("palabra") || n.Contains("fugac"))              return DemoKind.WordFlash;
        if (n.Contains("pareja"))                                       return DemoKind.ColorPairs;
        // Impulsos
        if (n.Contains("stop"))                                         return DemoKind.StopGo;
        if (n.Contains("silenc") || n.Contains("cuenta atras"))        return DemoKind.SilentCountdown;
        if (n.Contains("invers"))                                       return DemoKind.InverseResponse;
        if (n.Contains("mayor"))                                        return DemoKind.FollowMinority;
        // Emocional
        if (n.Contains("regulac"))                                      return DemoKind.Regulation;
        if (n.Contains("consecuenc"))                                   return DemoKind.Consequences;
        if (n.Contains("atracc"))                                       return DemoKind.Attraction;
        // Planificación
        if (n.Contains("recurso"))                                      return DemoKind.Resources;
        if (n.Contains("optim"))                                        return DemoKind.OptimalRoute;
        if (n.Contains("ruta"))                                         return DemoKind.RouteMemory;
        if (n.Contains("secuencia") || n.Contains("accion"))          return DemoKind.ActionSequence;
        return DemoKind.Generic;
    }

    static string Norm(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        s = s.ToLowerInvariant();
        return s.Replace("á", "a").Replace("é", "e").Replace("í", "i")
                .Replace("ó", "o").Replace("ú", "u").Replace("ñ", "n");
    }
}

public enum DemoKind
{
    Generic, QuickReaction, Simon, DontPress, Balance, Order,
    Tracking, RuleSwitch, LaserPath, FindOdd,
    // Atención
    NumberPath,
    // Memoria
    PatternRecall, FindChange, WordFlash, ColorPairs,
    // Impulsos
    StopGo, SilentCountdown, InverseResponse, FollowMinority,
    // Emocional
    Regulation, Consequences, Attraction,
    // Planificación
    Resources, RouteMemory, OptimalRoute, ActionSequence
}

// ============================================================================

public class MinigameDemoLoop : MonoBehaviour
{
    DemoKind _kind;
    Color _col;
    RectTransform _stage;

    static readonly Color OFF   = new Color(0.16f, 0.20f, 0.34f, 1f);
    static readonly Color GREEN = new Color(0.20f, 0.85f, 0.45f, 1f);
    static readonly Color REDC  = new Color(0.92f, 0.28f, 0.32f, 1f);

    public void Init(DemoKind kind, Color col)
    {
        _kind  = kind;
        _col   = col;
        _stage = (RectTransform)transform;
        switch (kind)
        {
            case DemoKind.QuickReaction: StartCoroutine(QuickReaction()); break;
            case DemoKind.Simon:         StartCoroutine(Simon());         break;
            case DemoKind.DontPress:     StartCoroutine(DontPress());     break;
            case DemoKind.Balance:       StartCoroutine(Balance());       break;
            case DemoKind.Order:         StartCoroutine(Order());         break;
            case DemoKind.Tracking:      StartCoroutine(Tracking());      break;
            case DemoKind.RuleSwitch:    StartCoroutine(RuleSwitch());    break;
            case DemoKind.LaserPath:     StartCoroutine(LaserPath());     break;
            case DemoKind.FindOdd:       StartCoroutine(FindOdd());       break;
            case DemoKind.NumberPath:      StartCoroutine(NumberPath());      break;
            case DemoKind.PatternRecall:   StartCoroutine(PatternRecall());   break;
            case DemoKind.FindChange:      StartCoroutine(FindChange());      break;
            case DemoKind.WordFlash:       StartCoroutine(WordFlash());       break;
            case DemoKind.ColorPairs:      StartCoroutine(ColorPairs());      break;
            case DemoKind.StopGo:          StartCoroutine(StopGo());          break;
            case DemoKind.SilentCountdown: StartCoroutine(SilentCountdown()); break;
            case DemoKind.InverseResponse: StartCoroutine(InverseResponse()); break;
            case DemoKind.FollowMinority:  StartCoroutine(FollowMinority());  break;
            case DemoKind.Regulation:      StartCoroutine(Regulation());      break;
            case DemoKind.Consequences:    StartCoroutine(Consequences());    break;
            case DemoKind.Attraction:      StartCoroutine(Attraction());      break;
            case DemoKind.Resources:       StartCoroutine(Resources());       break;
            case DemoKind.RouteMemory:     StartCoroutine(RouteMemory());     break;
            case DemoKind.OptimalRoute:    StartCoroutine(OptimalRoute());    break;
            case DemoKind.ActionSequence:  StartCoroutine(ActionSequence());  break;
        }
    }

    // -------------------------------------------------- Helpers

    RectTransform Circle(RectTransform p, string n, Color c, Vector2 anchor, float size)
    {
        var rt = KidUI.CircleAt(p, n, c, anchor, size);
        var img = rt.GetComponent<Image>();
        if (img != null) img.raycastTarget = false;
        return rt;
    }

    RectTransform Box(RectTransform p, string n, Color c, Vector2 amin, Vector2 amax, float corner)
    {
        var rt = KidUI.RoundImg(p, n, c, amin, amax, Vector2.zero, Vector2.zero, corner);
        var img = rt.GetComponent<Image>();
        if (img != null) img.raycastTarget = false;
        return rt;
    }

    RectTransform Hand()
    {
        var h = Circle(_stage, "Hand", new Color(1f, 1f, 1f, 0.95f), new Vector2(0.5f, 0.12f), 28f);
        Circle(h, "Tip", new Color(1f, 1f, 1f, 0.5f), new Vector2(0.5f, 1.05f), 12f);
        return h;
    }

    static void SetAnchor(RectTransform rt, Vector2 a)
    {
        if (rt == null) return;
        rt.anchorMin = rt.anchorMax = a;
        rt.anchoredPosition = Vector2.zero;
    }

    static void SetColor(RectTransform rt, Color c)
    {
        if (rt == null) return;
        var img = rt.GetComponent<Image>();
        if (img != null) img.color = c;
    }

    static Color Dim(Color c) => new Color(c.r * 0.32f, c.g * 0.32f, c.b * 0.32f, 1f);

    IEnumerator Move(RectTransform rt, Vector2 from, Vector2 to, float dur)
    {
        float t = 0f;
        while (t < dur)
        {
            if (rt == null || _stage == null) yield break;
            t += Time.unscaledDeltaTime;
            SetAnchor(rt, Vector2.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / dur)));
            yield return null;
        }
        SetAnchor(rt, to);
    }

    void Sparkle(Vector2 anchor)
    {
        for (int i = 0; i < 6; i++)
        {
            var p = Circle(_stage, "sp", new Color(1f, 0.85f, 0.2f, 0.95f), anchor, Random.Range(6f, 10f));
            StartCoroutine(SparkFly(p, Random.insideUnitCircle.normalized * Random.Range(40f, 70f)));
        }
    }

    IEnumerator SparkFly(RectTransform p, Vector2 vel)
    {
        var img = p != null ? p.GetComponent<Image>() : null;
        float t = 0f, dur = 0.45f;
        while (t < dur)
        {
            if (p == null) yield break;
            float dt = Time.unscaledDeltaTime;
            t += dt;
            p.anchoredPosition += vel * dt;
            if (img != null) { var c = img.color; c.a = 1f - t / dur; img.color = c; }
            yield return null;
        }
        if (p != null) Destroy(p.gameObject);
    }

    // -------------------------------------------------- ATENCIÓN · Reacción rápida
    // Un círculo gris se pone VERDE de golpe y la mano lo toca lo más rápido posible.

    IEnumerator QuickReaction()
    {
        var circle = Circle(_stage, "C", OFF, new Vector2(0.5f, 0.58f), 92f);
        var tag = KidUI.Txt(_stage, "Tag", "", Color.white, 19, new Vector2(0.15f, 0.72f), new Vector2(0.85f, 0.83f));
        tag.fontStyle = FontStyles.Bold;
        var hand = Hand();
        while (true)
        {
            if (_stage == null) yield break;
            SetColor(circle, OFF); tag.text = "Espera..."; tag.color = KidUI.DIM;
            SetAnchor(hand, new Vector2(0.5f, 0.12f));
            yield return new WaitForSecondsRealtime(1.1f);

            SetColor(circle, GREEN); UITween.PulseOnce(circle, 1.2f, 0.3f);
            tag.text = "¡YA!"; tag.color = GREEN;
            yield return Move(hand, new Vector2(0.5f, 0.12f), new Vector2(0.5f, 0.58f), 0.22f);
            UITween.PulseOnce(hand, 0.7f, 0.15f); UITween.PulseOnce(circle, 1.3f, 0.25f);
            Sparkle(new Vector2(0.5f, 0.58f));
            yield return new WaitForSecondsRealtime(1.1f);
        }
    }

    // -------------------------------------------------- MEMORIA · Simón
    // Se ilumina una secuencia de colores y luego la mano la repite en el mismo orden.

    IEnumerator Simon()
    {
        Vector2[] pos = { new Vector2(0.36f, 0.68f), new Vector2(0.64f, 0.68f),
                          new Vector2(0.36f, 0.40f), new Vector2(0.64f, 0.40f) };
        Color[] cols = { new Color(0.90f, 0.30f, 0.30f), new Color(0.30f, 0.60f, 1f),
                         new Color(0.30f, 0.85f, 0.45f), new Color(0.95f, 0.80f, 0.20f) };
        var btns = new RectTransform[4];
        for (int i = 0; i < 4; i++) btns[i] = Circle(_stage, "b" + i, Dim(cols[i]), pos[i], 56f);
        var hand = Hand();
        int[] seq = { 0, 1, 3 };

        while (true)
        {
            if (_stage == null) yield break;
            for (int i = 0; i < 4; i++) SetColor(btns[i], Dim(cols[i]));
            SetAnchor(hand, new Vector2(0.5f, 0.10f));

            // Mostrar la secuencia
            for (int k = 0; k < seq.Length; k++)
            {
                int b = seq[k];
                SetColor(btns[b], cols[b]); UITween.PulseOnce(btns[b], 1.25f, 0.25f);
                yield return new WaitForSecondsRealtime(0.42f);
                SetColor(btns[b], Dim(cols[b]));
                yield return new WaitForSecondsRealtime(0.12f);
            }
            yield return new WaitForSecondsRealtime(0.3f);

            // Repetir con la mano
            for (int k = 0; k < seq.Length; k++)
            {
                int b = seq[k];
                yield return Move(hand, hand.anchorMin, pos[b], 0.32f);
                SetColor(btns[b], cols[b]); UITween.PulseOnce(btns[b], 1.2f, 0.2f);
                UITween.PulseOnce(hand, 0.75f, 0.15f);
                yield return new WaitForSecondsRealtime(0.22f);
                SetColor(btns[b], Dim(cols[b]));
            }
            Sparkle(new Vector2(0.5f, 0.54f));
            yield return new WaitForSecondsRealtime(0.9f);
        }
    }

    // -------------------------------------------------- CONTROL DE IMPULSOS · No pulses todavía
    // Señal naranja = NO tocar (la mano se acerca y se retira). Señal verde = ¡ahora sí!

    IEnumerator DontPress()
    {
        var button = Circle(_stage, "Btn", OFF, new Vector2(0.5f, 0.55f), 92f);
        var tag = KidUI.Txt(_stage, "Tag", "", Color.white, 18, new Vector2(0.10f, 0.71f), new Vector2(0.90f, 0.83f));
        tag.fontStyle = FontStyles.Bold;
        var hand = Hand();
        var orange = new Color(0.95f, 0.55f, 0.12f);

        while (true)
        {
            if (_stage == null) yield break;
            SetColor(button, orange); tag.text = "Espera... no toques"; tag.color = orange;
            SetAnchor(hand, new Vector2(0.5f, 0.14f));
            yield return Move(hand, new Vector2(0.5f, 0.14f), new Vector2(0.5f, 0.34f), 0.30f);
            yield return Move(hand, new Vector2(0.5f, 0.34f), new Vector2(0.5f, 0.14f), 0.30f);
            yield return new WaitForSecondsRealtime(0.35f);

            SetColor(button, GREEN); UITween.PulseOnce(button, 1.15f, 0.25f);
            tag.text = "¡Ahora! Toca"; tag.color = GREEN;
            yield return Move(hand, new Vector2(0.5f, 0.14f), new Vector2(0.5f, 0.55f), 0.24f);
            UITween.PulseOnce(hand, 0.7f, 0.15f); UITween.PulseOnce(button, 1.3f, 0.25f);
            Sparkle(new Vector2(0.5f, 0.55f));
            yield return new WaitForSecondsRealtime(1.0f);
        }
    }

    // -------------------------------------------------- GESTIÓN EMOCIONAL · Equilibrio
    // Un indicador se va a un extremo (carita triste) y hay que devolverlo al centro (verde, feliz).

    IEnumerator Balance()
    {
        Box(_stage, "Track", new Color(0.10f, 0.14f, 0.24f, 1f),
            new Vector2(0.12f, 0.50f), new Vector2(0.88f, 0.60f), 2f);
        Box(_stage, "Zone", new Color(0.20f, 0.80f, 0.45f, 0.35f),
            new Vector2(0.42f, 0.48f), new Vector2(0.58f, 0.62f), 2f);
        var needle = Circle(_stage, "N", new Color(0.95f, 0.80f, 0.20f), new Vector2(0.5f, 0.55f), 36f);
        var face = KidUI.Txt(needle, "F", ":)", new Color(0.10f, 0.10f, 0.10f), 20, Vector2.zero, Vector2.one);
        face.fontStyle = FontStyles.Bold;

        while (true)
        {
            if (_stage == null) yield break;
            float side = Random.value < 0.5f ? 0.20f : 0.80f;
            face.text = ":(";
            yield return Move(needle, needle.anchorMin, new Vector2(side, 0.55f), 0.95f);
            yield return new WaitForSecondsRealtime(0.25f);

            face.text = ":)";
            yield return Move(needle, new Vector2(side, 0.55f), new Vector2(0.5f, 0.55f), 0.7f);
            UITween.PulseOnce(needle, 1.2f, 0.25f);
            Sparkle(new Vector2(0.5f, 0.55f));
            yield return new WaitForSecondsRealtime(0.6f);
        }
    }

    // -------------------------------------------------- PLANIFICACIÓN · Orden correcto
    // Fichas con números desordenados (2,3,1) y la mano las toca en orden: 1, 2, 3.

    IEnumerator Order()
    {
        Vector2[] pos = { new Vector2(0.28f, 0.54f), new Vector2(0.50f, 0.54f), new Vector2(0.72f, 0.54f) };
        int[] shown = { 2, 3, 1 };                 // número en cada ficha
        int[] order = { 2, 0, 1 };                 // índices a tocar: 1(idx2), 2(idx0), 3(idx1)
        var chips = new RectTransform[3];
        for (int i = 0; i < 3; i++)
        {
            chips[i] = Circle(_stage, "n" + i, Dim(_col), pos[i], 60f);
            var t = KidUI.Txt(chips[i], "t", shown[i].ToString(), Color.white, 30, Vector2.zero, Vector2.one);
            t.fontStyle = FontStyles.Bold;
        }
        var hand = Hand();

        while (true)
        {
            if (_stage == null) yield break;
            for (int i = 0; i < 3; i++) SetColor(chips[i], Dim(_col));
            SetAnchor(hand, new Vector2(0.5f, 0.14f));

            for (int k = 0; k < 3; k++)
            {
                int idx = order[k];
                yield return Move(hand, hand.anchorMin, pos[idx], 0.34f);
                SetColor(chips[idx], _col);
                UITween.PulseOnce(chips[idx], 1.2f, 0.2f);
                UITween.PulseOnce(hand, 0.75f, 0.15f);
                yield return new WaitForSecondsRealtime(0.25f);
            }
            Sparkle(new Vector2(0.5f, 0.54f));
            yield return new WaitForSecondsRealtime(0.85f);
        }
    }

    // -------------------------------------------------- ATENCIÓN · Seguimiento de objeto
    // Un objeto se mueve y hay que mantener el aro encima; el aro lo persigue con retardo.

    IEnumerator Tracking()
    {
        var ring  = Circle(_stage, "Ring", new Color(1f, 1f, 1f, 0.5f), new Vector2(0.3f, 0.55f), 72f);
        var inner = Circle(ring, "In", new Color(0.03f, 0.045f, 0.10f, 1f), new Vector2(0.5f, 0.5f), 52f);
        var obj   = Circle(_stage, "Obj", _col, new Vector2(0.3f, 0.55f), 40f);

        Vector2[] wp = { new Vector2(0.22f, 0.42f), new Vector2(0.50f, 0.70f),
                         new Vector2(0.78f, 0.42f), new Vector2(0.50f, 0.55f) };
        int seg = 0; float segT = 0f, segDur = 1.0f;
        Vector2 ringA = new Vector2(0.30f, 0.55f);

        while (true)
        {
            if (_stage == null || obj == null || ring == null) yield break;
            Vector2 a = wp[seg], b = wp[(seg + 1) % wp.Length];
            segT += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(segT / segDur);
            Vector2 objA = Vector2.Lerp(a, b, Mathf.SmoothStep(0f, 1f, u));
            SetAnchor(obj, objA);
            ringA = Vector2.Lerp(ringA, objA, 1f - Mathf.Exp(-6f * Time.unscaledDeltaTime));
            SetAnchor(ring, ringA);
            bool on = (ringA - objA).magnitude < 0.03f;
            SetColor(ring, on ? new Color(0.30f, 0.90f, 0.50f, 0.85f) : new Color(1f, 1f, 1f, 0.5f));
            if (u >= 1f) { segT = 0f; seg = (seg + 1) % wp.Length; }
            yield return null;
        }
    }

    // -------------------------------------------------- ATENCIÓN · Cambio de regla
    // La regla activa cambia ("toca los rojos" → "toca los azules"); hay que adaptarse.

    IEnumerator RuleSwitch()
    {
        var red  = new Color(0.90f, 0.30f, 0.30f);
        var blue = new Color(0.30f, 0.60f, 1f);
        Vector2[] pos = { new Vector2(0.28f, 0.46f), new Vector2(0.50f, 0.46f), new Vector2(0.72f, 0.46f) };
        int[] kind = { 0, 1, 0 };   // 0 = rojo, 1 = azul
        var shapes = new RectTransform[3];
        for (int i = 0; i < 3; i++)
            shapes[i] = Circle(_stage, "s" + i, kind[i] == 0 ? red : blue, pos[i], 54f);

        var rule = KidUI.Txt(_stage, "Rule", "", Color.white, 18,
                             new Vector2(0.06f, 0.74f), new Vector2(0.94f, 0.87f));
        rule.fontStyle = FontStyles.Bold;
        var hand = Hand();

        while (true)
        {
            if (_stage == null) yield break;
            rule.text = "Toca los ROJOS"; rule.color = red;
            SetAnchor(hand, new Vector2(0.5f, 0.12f));
            for (int i = 0; i < 3; i++)
                if (kind[i] == 0)
                {
                    yield return Move(hand, hand.anchorMin, pos[i], 0.3f);
                    UITween.PulseOnce(shapes[i], 1.2f, 0.2f); UITween.PulseOnce(hand, 0.75f, 0.15f);
                    yield return new WaitForSecondsRealtime(0.2f);
                }
            yield return new WaitForSecondsRealtime(0.4f);

            rule.text = "¡CAMBIO! Toca los AZULES"; rule.color = blue;
            UITween.PulseOnce(rule.rectTransform, 1.15f, 0.3f);
            yield return new WaitForSecondsRealtime(0.5f);
            for (int i = 0; i < 3; i++)
                if (kind[i] == 1)
                {
                    yield return Move(hand, hand.anchorMin, pos[i], 0.3f);
                    UITween.PulseOnce(shapes[i], 1.2f, 0.2f); UITween.PulseOnce(hand, 0.75f, 0.15f);
                    yield return new WaitForSecondsRealtime(0.2f);
                }
            Sparkle(new Vector2(0.5f, 0.46f));
            yield return new WaitForSecondsRealtime(0.7f);
        }
    }

    // -------------------------------------------------- ATENCIÓN · Camino láser
    // Girar el espejo para desviar el rayo hasta la meta.

    IEnumerator LaserPath()
    {
        Circle(_stage, "Src", _col, new Vector2(0.12f, 0.35f), 24f);
        Box(_stage, "B1", REDC, new Vector2(0.12f, 0.335f), new Vector2(0.50f, 0.365f), 1.5f);
        var mirror = Box(_stage, "Mir", new Color(0.82f, 0.88f, 0.98f, 1f),
                         new Vector2(0.46f, 0.29f), new Vector2(0.54f, 0.41f), 2.5f);
        var goal = Box(_stage, "Goal", new Color(0.15f, 0.30f, 0.20f, 1f),
                       new Vector2(0.44f, 0.76f), new Vector2(0.56f, 0.92f), 2f);
        var goalT = KidUI.Txt(goal, "g", "META", GREEN, 12, Vector2.zero, Vector2.one);
        goalT.fontStyle = FontStyles.Bold;
        var beam2 = Box(_stage, "B2", REDC, new Vector2(0.485f, 0.35f), new Vector2(0.515f, 0.35f), 1.5f);
        var hand = Hand();

        while (true)
        {
            if (_stage == null || mirror == null || beam2 == null) yield break;
            mirror.localRotation = Quaternion.Euler(0, 0, 90f);
            beam2.anchorMax = new Vector2(0.515f, 0.35f);
            SetColor(goal, new Color(0.15f, 0.30f, 0.20f, 1f));
            SetAnchor(hand, new Vector2(0.5f, 0.12f));
            yield return new WaitForSecondsRealtime(0.5f);

            yield return Move(hand, new Vector2(0.5f, 0.12f), new Vector2(0.5f, 0.30f), 0.4f);
            float t = 0f;
            while (t < 0.5f)
            {
                if (mirror == null) yield break;
                t += Time.unscaledDeltaTime;
                mirror.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(90f, 45f, t / 0.5f));
                yield return null;
            }
            t = 0f;
            while (t < 0.45f)
            {
                if (beam2 == null) yield break;
                t += Time.unscaledDeltaTime;
                beam2.anchorMax = new Vector2(0.515f, Mathf.Lerp(0.35f, 0.84f, t / 0.45f));
                yield return null;
            }
            SetColor(goal, new Color(0.20f, 0.55f, 0.32f, 1f)); UITween.PulseOnce(goal, 1.2f, 0.25f);
            Sparkle(new Vector2(0.5f, 0.84f));
            yield return new WaitForSecondsRealtime(1.0f);
        }
    }

    // -------------------------------------------------- ATENCIÓN · Algo no cuadra
    // Fila de formas iguales y una distinta; la mano toca la que no encaja.

    IEnumerator FindOdd()
    {
        Vector2[] pos = { new Vector2(0.20f, 0.55f), new Vector2(0.40f, 0.55f),
                          new Vector2(0.60f, 0.55f), new Vector2(0.80f, 0.55f) };
        var same = new Color(0.30f, 0.60f, 1f);
        int odd = 2;
        var shapes = new RectTransform[4];
        for (int i = 0; i < 4; i++)
        {
            if (i == odd)
                shapes[i] = Box(_stage, "odd",
                    new Color(0.95f, 0.55f, 0.15f),
                    new Vector2(pos[i].x - 0.07f, pos[i].y - 0.10f),
                    new Vector2(pos[i].x + 0.07f, pos[i].y + 0.10f), 2f);
            else
                shapes[i] = Circle(_stage, "s" + i, same, pos[i], 52f);
        }
        var tag = KidUI.Txt(_stage, "Tag", "¿Cual es diferente?", KidUI.DIM, 17,
                            new Vector2(0.08f, 0.74f), new Vector2(0.92f, 0.86f));
        tag.fontStyle = FontStyles.Bold;
        var hand = Hand();

        while (true)
        {
            if (_stage == null) yield break;
            SetAnchor(hand, new Vector2(0.5f, 0.12f));
            yield return new WaitForSecondsRealtime(0.9f);
            yield return Move(hand, new Vector2(0.5f, 0.12f), pos[odd], 0.4f);
            UITween.PulseOnce(shapes[odd], 1.3f, 0.3f); UITween.PulseOnce(hand, 0.75f, 0.15f);
            Sparkle(pos[odd]);
            yield return new WaitForSecondsRealtime(1.2f);
        }
    }

    // -------------------------------------------------- Helper: flecha con formas
    // (la fuente por defecto no trae glifos ← →, así que la dibujamos con cajas)
    RectTransform Arrow(RectTransform parent, Vector2 anchor, bool right, Color c, float sizePx)
    {
        var root = Circle(parent, "arrow", new Color(0f, 0f, 0f, 0f), anchor, sizePx);
        Box(root, "sh", c, new Vector2(right ? 0.15f : 0.30f, 0.44f),
                           new Vector2(right ? 0.70f : 0.85f, 0.56f), 2f);
        var head = Box(root, "hd", c, new Vector2(right ? 0.58f : 0.12f, 0.30f),
                                      new Vector2(right ? 0.88f : 0.42f, 0.70f), 1.5f);
        head.localRotation = Quaternion.Euler(0, 0, 45f);
        return root;
    }

    // -------------------------------------------------- ATENCIÓN · Camino numérico
    // Unir los puntos en orden 1 → 2 → 3.

    IEnumerator NumberPath()
    {
        Vector2[] p = { new Vector2(0.25f, 0.60f), new Vector2(0.70f, 0.64f), new Vector2(0.52f, 0.30f) };
        var dots = new RectTransform[3];
        for (int i = 0; i < 3; i++)
        {
            dots[i] = Circle(_stage, "d" + i, OFF, p[i], 56f);
            var t = KidUI.Txt(dots[i], "n", (i + 1).ToString(), Color.white, 26, Vector2.zero, Vector2.one);
            t.fontStyle = FontStyles.Bold;
        }
        var hand = Hand();
        while (true)
        {
            if (_stage == null) yield break;
            for (int i = 0; i < 3; i++) SetColor(dots[i], OFF);
            SetAnchor(hand, new Vector2(0.5f, 0.12f));
            yield return new WaitForSecondsRealtime(0.5f);
            for (int i = 0; i < 3; i++)
            {
                yield return Move(hand, hand.anchorMin, p[i], 0.35f);
                SetColor(dots[i], GREEN); UITween.PulseOnce(dots[i], 1.2f, 0.2f);
                UITween.PulseOnce(hand, 0.75f, 0.15f);
                yield return new WaitForSecondsRealtime(0.15f);
            }
            Sparkle(p[2]);
            yield return new WaitForSecondsRealtime(0.9f);
        }
    }

    // -------------------------------------------------- MEMORIA · Repite el dibujo
    // Se enciende un patrón de celdas; luego hay que repetirlo.

    IEnumerator PatternRecall()
    {
        Vector2[] c = { new Vector2(0.36f, 0.62f), new Vector2(0.64f, 0.62f),
                        new Vector2(0.36f, 0.36f), new Vector2(0.64f, 0.36f) };
        var cells = new RectTransform[4];
        for (int i = 0; i < 4; i++)
            cells[i] = Box(_stage, "c" + i, OFF,
                new Vector2(c[i].x - 0.10f, c[i].y - 0.12f),
                new Vector2(c[i].x + 0.10f, c[i].y + 0.12f), 2.5f);
        int[] seq = { 0, 3, 1 };
        var tag = KidUI.Txt(_stage, "t", "", Color.white, 15,
                            new Vector2(0.05f, 0.80f), new Vector2(0.95f, 0.93f));
        tag.fontStyle = FontStyles.Bold;
        var hand = Hand();
        while (true)
        {
            if (_stage == null) yield break;
            for (int i = 0; i < 4; i++) SetColor(cells[i], OFF);
            SetAnchor(hand, new Vector2(0.5f, 0.10f));
            tag.text = "MIRA"; tag.color = _col;
            yield return new WaitForSecondsRealtime(0.4f);
            foreach (int s in seq)
            {
                SetColor(cells[s], _col); UITween.PulseOnce(cells[s], 1.15f, 0.25f);
                yield return new WaitForSecondsRealtime(0.5f);
                SetColor(cells[s], OFF);
                yield return new WaitForSecondsRealtime(0.15f);
            }
            tag.text = "REPITE"; tag.color = GREEN;
            yield return new WaitForSecondsRealtime(0.3f);
            foreach (int s in seq)
            {
                yield return Move(hand, hand.anchorMin, c[s], 0.3f);
                SetColor(cells[s], GREEN); UITween.PulseOnce(cells[s], 1.2f, 0.2f);
                UITween.PulseOnce(hand, 0.75f, 0.15f);
                yield return new WaitForSecondsRealtime(0.15f);
            }
            Sparkle(new Vector2(0.5f, 0.5f));
            yield return new WaitForSecondsRealtime(0.7f);
        }
    }

    // -------------------------------------------------- MEMORIA · Cambios sutiles
    // La escena parpadea y algo cambia; hay que señalar qué.

    IEnumerator FindChange()
    {
        Vector2[] p = { new Vector2(0.28f, 0.52f), new Vector2(0.50f, 0.52f), new Vector2(0.72f, 0.52f) };
        var baseC = new Color(0.45f, 0.62f, 0.95f);
        var chg   = new Color(0.95f, 0.55f, 0.20f);
        var sh = new RectTransform[3];
        for (int i = 0; i < 3; i++) sh[i] = Circle(_stage, "s" + i, baseC, p[i], 56f);
        int changed = 1;
        var tag = KidUI.Txt(_stage, "t", "¿Qué cambió?", KidUI.DIM, 16,
                            new Vector2(0.05f, 0.76f), new Vector2(0.95f, 0.90f));
        tag.fontStyle = FontStyles.Bold;
        var hand = Hand();
        while (true)
        {
            if (_stage == null) yield break;
            for (int i = 0; i < 3; i++) SetColor(sh[i], baseC);
            SetAnchor(hand, new Vector2(0.5f, 0.12f));
            yield return new WaitForSecondsRealtime(0.7f);
            for (int i = 0; i < 3; i++) SetColor(sh[i], Dim(baseC));
            yield return new WaitForSecondsRealtime(0.25f);
            for (int i = 0; i < 3; i++) SetColor(sh[i], i == changed ? chg : baseC);
            UITween.PulseOnce(sh[changed], 1.1f, 0.2f);
            yield return new WaitForSecondsRealtime(0.5f);
            yield return Move(hand, new Vector2(0.5f, 0.12f), p[changed], 0.4f);
            UITween.PulseOnce(sh[changed], 1.3f, 0.3f); UITween.PulseOnce(hand, 0.75f, 0.15f);
            Sparkle(p[changed]);
            yield return new WaitForSecondsRealtime(1.1f);
        }
    }

    // -------------------------------------------------- MEMORIA · Palabras fugaces
    // Aparece una "palabra" (símbolo) un instante, se oculta y hay que reconocerla.

    IEnumerator WordFlash()
    {
        var target = new Color(0.98f, 0.80f, 0.10f);
        var other  = new Color(0.58f, 0.28f, 0.92f);
        var card = Box(_stage, "card", new Color(0.14f, 0.17f, 0.30f, 1f),
                       new Vector2(0.36f, 0.55f), new Vector2(0.64f, 0.85f), 2f);
        var glyph = Circle(card, "g", target, new Vector2(0.5f, 0.5f), 40f);
        var tag = KidUI.Txt(_stage, "t", "", Color.white, 15,
                            new Vector2(0.05f, 0.86f), new Vector2(0.95f, 0.98f));
        tag.fontStyle = FontStyles.Bold;
        Vector2[] op = { new Vector2(0.34f, 0.28f), new Vector2(0.66f, 0.28f) };
        var o0 = Circle(_stage, "o0", target, op[0], 46f);
        var o1 = Circle(_stage, "o1", other,  op[1], 46f);
        var hand = Hand();
        while (true)
        {
            if (_stage == null || glyph == null) yield break;
            glyph.gameObject.SetActive(true); SetColor(glyph, target);
            tag.text = "MEMORIZA"; tag.color = _col;
            SetColor(o0, Dim(target)); SetColor(o1, Dim(other));
            SetAnchor(hand, new Vector2(0.5f, 0.10f));
            yield return new WaitForSecondsRealtime(0.9f);
            glyph.gameObject.SetActive(false);
            tag.text = "¿CUÁL ERA?"; tag.color = GREEN;
            SetColor(o0, target); SetColor(o1, other);
            yield return new WaitForSecondsRealtime(0.5f);
            yield return Move(hand, new Vector2(0.5f, 0.10f), op[0], 0.4f);
            UITween.PulseOnce(o0, 1.3f, 0.3f); UITween.PulseOnce(hand, 0.75f, 0.15f);
            Sparkle(op[0]);
            yield return new WaitForSecondsRealtime(1.0f);
        }
    }

    // -------------------------------------------------- MEMORIA · Parejas de colores
    // Voltear dos cartas y encontrar la pareja igual.

    IEnumerator ColorPairs()
    {
        Vector2[] p = { new Vector2(0.22f, 0.5f), new Vector2(0.41f, 0.5f),
                        new Vector2(0.60f, 0.5f), new Vector2(0.79f, 0.5f) };
        Color[] face = { new Color(0.92f, 0.30f, 0.34f), new Color(0.30f, 0.60f, 1f),
                         new Color(0.92f, 0.30f, 0.34f), new Color(0.30f, 0.60f, 1f) };
        var back = new Color(0.20f, 0.24f, 0.38f, 1f);
        var cards = new RectTransform[4];
        for (int i = 0; i < 4; i++)
            cards[i] = Box(_stage, "c" + i, back,
                new Vector2(p[i].x - 0.075f, p[i].y - 0.16f),
                new Vector2(p[i].x + 0.075f, p[i].y + 0.16f), 2f);
        var hand = Hand();
        int a = 0, b = 2;
        while (true)
        {
            if (_stage == null) yield break;
            for (int i = 0; i < 4; i++) SetColor(cards[i], back);
            SetAnchor(hand, new Vector2(0.5f, 0.10f));
            yield return new WaitForSecondsRealtime(0.5f);
            yield return Move(hand, hand.anchorMin, p[a], 0.35f);
            SetColor(cards[a], face[a]); UITween.PulseOnce(cards[a], 1.1f, 0.2f);
            yield return new WaitForSecondsRealtime(0.35f);
            yield return Move(hand, p[a], p[b], 0.35f);
            SetColor(cards[b], face[b]); UITween.PulseOnce(cards[b], 1.1f, 0.2f);
            yield return new WaitForSecondsRealtime(0.4f);
            UITween.PulseOnce(cards[a], 1.25f, 0.3f); UITween.PulseOnce(cards[b], 1.25f, 0.3f);
            Sparkle(new Vector2((p[a].x + p[b].x) / 2f, 0.5f));
            yield return new WaitForSecondsRealtime(0.9f);
        }
    }

    // -------------------------------------------------- IMPULSOS · Stop & Go
    // Esperar en ROJO y tocar sólo en VERDE.

    IEnumerator StopGo()
    {
        var light = Circle(_stage, "light", REDC, new Vector2(0.5f, 0.82f), 46f);
        var obj   = Circle(_stage, "obj", _col, new Vector2(0.15f, 0.45f), 40f);
        var tag = KidUI.Txt(_stage, "t", "", Color.white, 17,
                            new Vector2(0.05f, 0.64f), new Vector2(0.95f, 0.76f));
        tag.fontStyle = FontStyles.Bold;
        var hand = Hand();
        while (true)
        {
            if (_stage == null) yield break;
            SetColor(light, REDC); tag.text = "ESPERA"; tag.color = REDC;
            SetAnchor(obj, new Vector2(0.15f, 0.45f));
            SetAnchor(hand, new Vector2(0.5f, 0.12f));
            yield return Move(obj, new Vector2(0.15f, 0.45f), new Vector2(0.5f, 0.45f), 1.0f);
            SetColor(light, GREEN); tag.text = "¡YA!"; tag.color = GREEN;
            UITween.PulseOnce(light, 1.2f, 0.2f);
            yield return Move(hand, new Vector2(0.5f, 0.12f), new Vector2(0.5f, 0.40f), 0.3f);
            UITween.PulseOnce(obj, 1.3f, 0.3f); UITween.PulseOnce(hand, 0.75f, 0.15f);
            Sparkle(new Vector2(0.5f, 0.45f));
            yield return new WaitForSecondsRealtime(0.9f);
        }
    }

    // -------------------------------------------------- IMPULSOS · Cuenta atrás silenciosa
    // Cuenta 3-2-1 en silencio y toca en el momento justo.

    IEnumerator SilentCountdown()
    {
        var neutral = new Color(0.14f, 0.17f, 0.30f, 1f);
        var disc = Circle(_stage, "disc", neutral, new Vector2(0.5f, 0.58f), 100f);
        var num = KidUI.Txt(disc, "n", "3", Color.white, 52, Vector2.zero, Vector2.one);
        num.fontStyle = FontStyles.Bold;
        var tag = KidUI.Txt(_stage, "t", "", KidUI.DIM, 14,
                            new Vector2(0.05f, 0.82f), new Vector2(0.95f, 0.95f));
        tag.fontStyle = FontStyles.Bold;
        var hand = Hand();
        while (true)
        {
            if (_stage == null) yield break;
            SetColor(disc, neutral); num.color = Color.white;
            SetAnchor(hand, new Vector2(0.5f, 0.12f));
            tag.text = "CUENTA EN SILENCIO";
            for (int k = 3; k >= 1; k--)
            {
                num.text = k.ToString(); UITween.PulseOnce(num.rectTransform, 1.2f, 0.25f);
                yield return new WaitForSecondsRealtime(0.6f);
            }
            num.text = "?"; num.color = _col;
            yield return new WaitForSecondsRealtime(0.5f);
            yield return Move(hand, new Vector2(0.5f, 0.12f), new Vector2(0.5f, 0.42f), 0.35f);
            SetColor(disc, new Color(0.20f, 0.55f, 0.32f, 1f)); num.text = "¡YA!"; num.color = GREEN;
            num.fontSize = 30;
            UITween.PulseOnce(disc, 1.15f, 0.25f); UITween.PulseOnce(hand, 0.75f, 0.15f);
            Sparkle(new Vector2(0.5f, 0.58f));
            yield return new WaitForSecondsRealtime(0.9f);
            num.fontSize = 52;
        }
    }

    // -------------------------------------------------- IMPULSOS · Respuesta inversa
    // La flecha apunta a un lado; hay que tocar el CONTRARIO.

    IEnumerator InverseResponse()
    {
        Arrow(_stage, new Vector2(0.5f, 0.70f), false, Color.white, 120f);
        var idle = new Color(0.30f, 0.40f, 0.55f);
        var left  = Circle(_stage, "L", idle, new Vector2(0.28f, 0.32f), 56f);
        var right = Circle(_stage, "R", idle, new Vector2(0.72f, 0.32f), 56f);
        var tag = KidUI.Txt(_stage, "t", "Haz lo CONTRARIO", new Color(0.98f, 0.6f, 0.3f), 15,
                            new Vector2(0.05f, 0.46f), new Vector2(0.95f, 0.57f));
        tag.fontStyle = FontStyles.Bold;
        var hand = Hand();
        while (true)
        {
            if (_stage == null) yield break;
            SetColor(left, idle); SetColor(right, idle);
            SetAnchor(hand, new Vector2(0.5f, 0.12f));
            yield return new WaitForSecondsRealtime(0.7f);
            yield return Move(hand, new Vector2(0.5f, 0.12f), new Vector2(0.72f, 0.32f), 0.45f);
            SetColor(right, GREEN); UITween.PulseOnce(right, 1.3f, 0.3f); UITween.PulseOnce(hand, 0.75f, 0.15f);
            Sparkle(new Vector2(0.72f, 0.32f));
            yield return new WaitForSecondsRealtime(1.1f);
        }
    }

    // -------------------------------------------------- IMPULSOS · No sigas la mayoría
    // Cuatro flechas iguales y una distinta: elige la diferente.

    IEnumerator FollowMinority()
    {
        Vector2[] p = { new Vector2(0.18f, 0.52f), new Vector2(0.34f, 0.52f), new Vector2(0.50f, 0.52f),
                        new Vector2(0.66f, 0.52f), new Vector2(0.82f, 0.52f) };
        int odd = 2;
        var idle = new Color(0.20f, 0.24f, 0.38f, 1f);
        var cards = new RectTransform[5];
        for (int i = 0; i < 5; i++)
        {
            cards[i] = Circle(_stage, "a" + i, idle, p[i], 52f);
            Arrow(cards[i], new Vector2(0.5f, 0.5f), i != odd, Color.white, 44f);
        }
        var tag = KidUI.Txt(_stage, "t", "Elige el DIFERENTE", KidUI.DIM, 15,
                            new Vector2(0.05f, 0.72f), new Vector2(0.95f, 0.85f));
        tag.fontStyle = FontStyles.Bold;
        var hand = Hand();
        while (true)
        {
            if (_stage == null) yield break;
            for (int i = 0; i < 5; i++) SetColor(cards[i], idle);
            SetAnchor(hand, new Vector2(0.5f, 0.12f));
            yield return new WaitForSecondsRealtime(0.9f);
            yield return Move(hand, new Vector2(0.5f, 0.12f), p[odd], 0.4f);
            SetColor(cards[odd], GREEN); UITween.PulseOnce(cards[odd], 1.3f, 0.3f); UITween.PulseOnce(hand, 0.75f, 0.15f);
            Sparkle(p[odd]);
            yield return new WaitForSecondsRealtime(1.2f);
        }
    }

    // -------------------------------------------------- EMOCIONAL · Regulación progresiva
    // El nivel sube (nerviosismo) y hay que respirar para bajarlo a la zona calma.

    IEnumerator Regulation()
    {
        var track = Box(_stage, "tr", new Color(0.14f, 0.17f, 0.30f, 1f),
                        new Vector2(0.44f, 0.18f), new Vector2(0.56f, 0.82f), 3f);
        var fill = Box(track, "fi", GREEN, new Vector2(0f, 0f), new Vector2(1f, 0.2f), 3f);
        var tag = KidUI.Txt(_stage, "t", "", Color.white, 15,
                            new Vector2(0.02f, 0.85f), new Vector2(0.98f, 0.97f));
        tag.fontStyle = FontStyles.Bold;
        var hand = Hand();
        while (true)
        {
            if (_stage == null || fill == null) yield break;
            tag.text = "SE ALTERA..."; tag.color = REDC;
            float t = 0f;
            while (t < 1.0f)
            {
                if (fill == null) yield break;
                t += Time.unscaledDeltaTime; float v = Mathf.Lerp(0.2f, 0.95f, t / 1.0f);
                fill.anchorMax = new Vector2(1f, v); SetColor(fill, Color.Lerp(GREEN, REDC, v));
                yield return null;
            }
            tag.text = "RESPIRA..."; tag.color = new Color(0.3f, 0.7f, 1f);
            SetAnchor(hand, new Vector2(0.66f, 0.70f));
            t = 0f;
            while (t < 1.1f)
            {
                if (fill == null) yield break;
                t += Time.unscaledDeltaTime; float u = t / 1.1f; float v = Mathf.Lerp(0.95f, 0.2f, u);
                fill.anchorMax = new Vector2(1f, v); SetColor(fill, Color.Lerp(GREEN, REDC, v));
                SetAnchor(hand, new Vector2(0.66f, Mathf.Lerp(0.70f, 0.28f, u)));
                yield return null;
            }
            tag.text = "¡EN CALMA!"; tag.color = GREEN;
            Sparkle(new Vector2(0.5f, 0.28f));
            yield return new WaitForSecondsRealtime(0.9f);
        }
    }

    // -------------------------------------------------- EMOCIONAL · Consecuencias
    // Elegir la acción amable y ver la cara ponerse contenta.

    IEnumerator Consequences()
    {
        var happy = new Color(0.98f, 0.85f, 0.3f);
        var face = Circle(_stage, "face", happy, new Vector2(0.5f, 0.66f), 90f);
        Circle(face, "eL", new Color(0.1f, 0.1f, 0.15f), new Vector2(0.36f, 0.60f), 12f);
        Circle(face, "eR", new Color(0.1f, 0.1f, 0.15f), new Vector2(0.64f, 0.60f), 12f);
        var mouth = Box(face, "m", new Color(0.1f, 0.1f, 0.15f),
                        new Vector2(0.34f, 0.30f), new Vector2(0.66f, 0.35f), 2f);
        var good = Circle(_stage, "g", GREEN, new Vector2(0.35f, 0.24f), 52f);
        var bad  = Circle(_stage, "b", new Color(0.55f, 0.32f, 0.6f), new Vector2(0.65f, 0.24f), 52f);
        KidUI.Txt(good, "p", "+", Color.white, 30, Vector2.zero, Vector2.one).fontStyle = FontStyles.Bold;
        KidUI.Txt(bad,  "m2", "-", Color.white, 30, Vector2.zero, Vector2.one).fontStyle = FontStyles.Bold;
        var tag = KidUI.Txt(_stage, "t", "Elige y mira qué pasa", KidUI.DIM, 14,
                            new Vector2(0.03f, 0.44f), new Vector2(0.97f, 0.55f));
        tag.fontStyle = FontStyles.Bold;
        var hand = Hand();
        while (true)
        {
            if (_stage == null || mouth == null) yield break;
            mouth.anchorMin = new Vector2(0.34f, 0.30f); mouth.anchorMax = new Vector2(0.66f, 0.35f);
            SetAnchor(hand, new Vector2(0.5f, 0.10f));
            yield return new WaitForSecondsRealtime(0.7f);
            yield return Move(hand, new Vector2(0.5f, 0.10f), new Vector2(0.35f, 0.24f), 0.45f);
            UITween.PulseOnce(good, 1.3f, 0.3f); UITween.PulseOnce(hand, 0.75f, 0.15f);
            if (mouth != null) { mouth.anchorMin = new Vector2(0.30f, 0.26f); mouth.anchorMax = new Vector2(0.70f, 0.31f); }
            UITween.PulseOnce(face, 1.12f, 0.35f);
            Sparkle(new Vector2(0.5f, 0.66f));
            yield return new WaitForSecondsRealtime(1.1f);
        }
    }

    // -------------------------------------------------- EMOCIONAL · Atracción emocional
    // Un objeto tentador "tira" de la mano, pero hay que resistir y volver a la calma.

    IEnumerator Attraction()
    {
        var lure = Circle(_stage, "lure", new Color(0.98f, 0.35f, 0.55f), new Vector2(0.78f, 0.6f), 56f);
        var calm = Circle(_stage, "calm", new Color(0.20f, 0.30f, 0.5f, 1f), new Vector2(0.30f, 0.32f), 60f);
        var tag = KidUI.Txt(_stage, "t", "", Color.white, 15,
                            new Vector2(0.05f, 0.82f), new Vector2(0.95f, 0.95f));
        tag.fontStyle = FontStyles.Bold;
        var hand = Hand();
        while (true)
        {
            if (_stage == null) yield break;
            SetAnchor(hand, new Vector2(0.30f, 0.32f));
            tag.text = "¡RESISTE!"; tag.color = new Color(0.98f, 0.5f, 0.6f);
            UITween.PulseOnce(lure, 1.2f, 0.5f);
            // la mano es atraída hacia el señuelo...
            yield return Move(hand, new Vector2(0.30f, 0.32f), new Vector2(0.58f, 0.46f), 0.6f);
            UITween.PulseOnce(lure, 1.25f, 0.3f);
            yield return new WaitForSecondsRealtime(0.25f);
            // ...pero se resiste y regresa a la calma
            tag.text = "VUELVE A LA CALMA"; tag.color = new Color(0.3f, 0.7f, 1f);
            yield return Move(hand, new Vector2(0.58f, 0.46f), new Vector2(0.30f, 0.32f), 0.55f);
            SetColor(calm, GREEN); UITween.PulseOnce(calm, 1.2f, 0.3f); UITween.PulseOnce(hand, 0.75f, 0.15f);
            Sparkle(new Vector2(0.30f, 0.32f));
            yield return new WaitForSecondsRealtime(0.7f);
            SetColor(calm, new Color(0.20f, 0.30f, 0.5f, 1f));
        }
    }

    // -------------------------------------------------- PLANIFICACIÓN · Gestión de recursos
    // Repartir las monedas entre las dos necesidades.

    IEnumerator Resources()
    {
        var binC = new Color(0.20f, 0.30f, 0.5f, 1f);
        var bin0 = Box(_stage, "b0", binC, new Vector2(0.16f, 0.16f), new Vector2(0.40f, 0.40f), 2f);
        var bin1 = Box(_stage, "b1", binC, new Vector2(0.60f, 0.16f), new Vector2(0.84f, 0.40f), 2f);
        var tag = KidUI.Txt(_stage, "t", "Reparte las monedas", KidUI.DIM, 15,
                            new Vector2(0.05f, 0.84f), new Vector2(0.95f, 0.97f));
        tag.fontStyle = FontStyles.Bold;
        var coinC = new Color(0.98f, 0.80f, 0.10f);
        var hand = Hand();
        Vector2 pile = new Vector2(0.5f, 0.68f);
        Vector2[] dest = { new Vector2(0.28f, 0.28f), new Vector2(0.72f, 0.28f), new Vector2(0.28f, 0.28f) };
        while (true)
        {
            if (_stage == null) yield break;
            SetAnchor(hand, new Vector2(0.5f, 0.10f));
            for (int i = 0; i < dest.Length; i++)
            {
                var coin = Circle(_stage, "coin", coinC, pile, 26f);
                yield return Move(hand, hand.anchorMin, pile, 0.3f);
                float t = 0f;
                while (t < 0.45f)
                {
                    if (coin == null) yield break;
                    t += Time.unscaledDeltaTime; float u = Mathf.SmoothStep(0, 1, t / 0.45f);
                    Vector2 a = Vector2.Lerp(pile, dest[i], u);
                    SetAnchor(coin, a); SetAnchor(hand, a);
                    yield return null;
                }
                UITween.PulseOnce(i % 2 == 0 ? bin0 : bin1, 1.1f, 0.2f);
                Destroy(coin.gameObject);
                yield return new WaitForSecondsRealtime(0.15f);
            }
            Sparkle(new Vector2(0.5f, 0.28f));
            yield return new WaitForSecondsRealtime(0.7f);
        }
    }

    // -------------------------------------------------- PLANIFICACIÓN · Memoria de ruta
    // Memorizar una ruta por la cuadrícula y rehacerla.

    IEnumerator RouteMemory()
    {
        float[] xs = { 0.32f, 0.5f, 0.68f };
        float[] ys = { 0.66f, 0.5f, 0.34f };
        var cells = new RectTransform[9]; var cp = new Vector2[9];
        for (int r = 0; r < 3; r++)
            for (int col = 0; col < 3; col++)
            {
                int i = r * 3 + col; cp[i] = new Vector2(xs[col], ys[r]);
                cells[i] = Circle(_stage, "c" + i, OFF, cp[i], 38f);
            }
        int[] path = { 0, 1, 4, 7 };
        var tag = KidUI.Txt(_stage, "t", "", Color.white, 15,
                            new Vector2(0.05f, 0.82f), new Vector2(0.95f, 0.95f));
        tag.fontStyle = FontStyles.Bold;
        var hand = Hand();
        while (true)
        {
            if (_stage == null) yield break;
            for (int i = 0; i < 9; i++) SetColor(cells[i], OFF);
            SetAnchor(hand, new Vector2(0.5f, 0.10f));
            tag.text = "MEMORIZA LA RUTA"; tag.color = _col;
            yield return new WaitForSecondsRealtime(0.35f);
            foreach (int s in path)
            {
                SetColor(cells[s], _col); UITween.PulseOnce(cells[s], 1.15f, 0.2f);
                yield return new WaitForSecondsRealtime(0.4f);
            }
            yield return new WaitForSecondsRealtime(0.3f);
            for (int i = 0; i < 9; i++) SetColor(cells[i], OFF);
            tag.text = "REHAZLA"; tag.color = GREEN;
            yield return new WaitForSecondsRealtime(0.25f);
            foreach (int s in path)
            {
                yield return Move(hand, hand.anchorMin, cp[s], 0.28f);
                SetColor(cells[s], GREEN); UITween.PulseOnce(cells[s], 1.2f, 0.2f);
                UITween.PulseOnce(hand, 0.75f, 0.12f);
                yield return new WaitForSecondsRealtime(0.1f);
            }
            Sparkle(cp[7]);
            yield return new WaitForSecondsRealtime(0.7f);
        }
    }

    // -------------------------------------------------- PLANIFICACIÓN · Ruta óptima
    // De dos caminos, elegir el más corto.

    IEnumerator OptimalRoute()
    {
        var start = Circle(_stage, "s", _col, new Vector2(0.18f, 0.30f), 40f);
        var goal  = Circle(_stage, "g", new Color(0.3f, 0.8f, 0.5f), new Vector2(0.82f, 0.30f), 40f);
        KidUI.Txt(start, "a", "A", Color.white, 20, Vector2.zero, Vector2.one).fontStyle = FontStyles.Bold;
        KidUI.Txt(goal,  "b", "B", Color.white, 20, Vector2.zero, Vector2.one).fontStyle = FontStyles.Bold;
        var longC = new Color(0.30f, 0.35f, 0.5f, 1f);
        Box(_stage, "l1", longC, new Vector2(0.18f, 0.30f), new Vector2(0.21f, 0.72f), 2f);
        Box(_stage, "l2", longC, new Vector2(0.18f, 0.70f), new Vector2(0.82f, 0.73f), 2f);
        Box(_stage, "l3", longC, new Vector2(0.79f, 0.30f), new Vector2(0.82f, 0.72f), 2f);
        Box(_stage, "s0", longC, new Vector2(0.20f, 0.285f), new Vector2(0.80f, 0.315f), 2f);
        var shortFill = Box(_stage, "sf", GREEN, new Vector2(0.20f, 0.285f), new Vector2(0.20f, 0.315f), 2f);
        var tag = KidUI.Txt(_stage, "t", "El camino más corto", KidUI.DIM, 15,
                            new Vector2(0.05f, 0.82f), new Vector2(0.95f, 0.95f));
        tag.fontStyle = FontStyles.Bold;
        var hand = Hand();
        while (true)
        {
            if (_stage == null || shortFill == null) yield break;
            shortFill.anchorMax = new Vector2(0.20f, 0.315f);
            SetAnchor(hand, new Vector2(0.18f, 0.14f));
            yield return new WaitForSecondsRealtime(0.5f);
            float t = 0f;
            while (t < 1.0f)
            {
                if (shortFill == null) yield break;
                t += Time.unscaledDeltaTime; float u = t / 1.0f; float x = Mathf.Lerp(0.20f, 0.80f, u);
                shortFill.anchorMax = new Vector2(x, 0.315f);
                SetAnchor(hand, new Vector2(x, 0.20f));
                yield return null;
            }
            UITween.PulseOnce(goal, 1.3f, 0.3f);
            Sparkle(new Vector2(0.82f, 0.30f));
            yield return new WaitForSecondsRealtime(0.9f);
        }
    }

    // -------------------------------------------------- PLANIFICACIÓN · Secuencia de acciones
    // Completar los pasos en orden, uno tras otro.

    IEnumerator ActionSequence()
    {
        Vector2[] p = { new Vector2(0.5f, 0.72f), new Vector2(0.5f, 0.50f), new Vector2(0.5f, 0.28f) };
        var rows = new RectTransform[3]; var chk = new RectTransform[3];
        var idle = new Color(0.30f, 0.35f, 0.5f, 1f);
        for (int i = 0; i < 3; i++)
        {
            rows[i] = Box(_stage, "r" + i, new Color(0.16f, 0.20f, 0.34f, 1f),
                new Vector2(0.18f, p[i].y - 0.09f), new Vector2(0.82f, p[i].y + 0.09f), 2f);
            var num = KidUI.Txt(rows[i], "n", (i + 1).ToString(), Color.white, 22,
                                new Vector2(0.02f, 0f), new Vector2(0.22f, 1f));
            num.fontStyle = FontStyles.Bold;
            chk[i] = Circle(rows[i], "k", idle, new Vector2(0.88f, 0.5f), 34f);
        }
        var tag = KidUI.Txt(_stage, "t", "Paso a paso, en orden", KidUI.DIM, 14,
                            new Vector2(0.05f, 0.88f), new Vector2(0.95f, 0.99f));
        tag.fontStyle = FontStyles.Bold;
        var hand = Hand();
        while (true)
        {
            if (_stage == null) yield break;
            for (int i = 0; i < 3; i++) SetColor(chk[i], idle);
            SetAnchor(hand, new Vector2(0.5f, 0.10f));
            yield return new WaitForSecondsRealtime(0.4f);
            for (int i = 0; i < 3; i++)
            {
                yield return Move(hand, hand.anchorMin, new Vector2(0.74f, p[i].y), 0.35f);
                SetColor(chk[i], GREEN); UITween.PulseOnce(chk[i], 1.25f, 0.25f);
                UITween.PulseOnce(rows[i], 1.03f, 0.2f); UITween.PulseOnce(hand, 0.75f, 0.12f);
                yield return new WaitForSecondsRealtime(0.25f);
            }
            Sparkle(new Vector2(0.5f, 0.5f));
            yield return new WaitForSecondsRealtime(0.8f);
        }
    }
}
