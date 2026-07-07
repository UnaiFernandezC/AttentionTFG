// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Demo animada "así se juega" para el IntroPanel: una manita toca en bucle el
/// objetivo correcto y celebra el acierto. Comprensible sin saber leer.
/// Genérica para todos los minijuegos, tematizada con el color de la categoría.
/// </summary>
public static class IntroDemo
{
    public static void Attach(RectTransform card, string categoryName)
    {
        Color catCol = IntroPanel.CategoryColor(categoryName);

        var stage = KidUI.RoundImg(card, "DemoStage", new Color(0.03f, 0.045f, 0.10f, 0.9f),
                                   new Vector2(0.58f, 0.34f), new Vector2(0.94f, 0.67f),
                                   Vector2.zero, Vector2.zero, 1.4f);
        stage.GetComponent<Image>().raycastTarget = false;
        // Canvas anidado: la animación de la demo no fuerza rebuilds del canvas del intro
        stage.gameObject.AddComponent<Canvas>();

        var lbl = KidUI.Txt(stage, "Lbl", "ASÍ SE JUEGA", KidUI.DIM, 14,
                            new Vector2(0.05f, 0.84f), new Vector2(0.95f, 0.98f));
        lbl.characterSpacing = 3f;
        lbl.fontStyle = FontStyles.Bold;

        var loop = stage.gameObject.AddComponent<IntroDemoLoop>();
        loop.Init(catCol);
    }
}

public class IntroDemoLoop : MonoBehaviour
{
    Color _catCol;
    RectTransform _stage;
    RectTransform[] _targets;
    Image[] _targetImgs;
    RectTransform _hand;

    static readonly Color TARGET_OFF = new Color(0.16f, 0.20f, 0.34f, 1f);

    public void Init(Color catCol)
    {
        _catCol = catCol;
        _stage = (RectTransform)transform;
        BuildStage();
        StartCoroutine(Loop());
    }

    void BuildStage()
    {
        // Tres objetivos redondos
        _targets = new RectTransform[3];
        _targetImgs = new Image[3];
        float[] xs = { 0.22f, 0.50f, 0.78f };
        for (int i = 0; i < 3; i++)
        {
            var t = KidUI.CircleAt(_stage, "T" + i, TARGET_OFF, new Vector2(xs[i], 0.58f), 62f);
            t.GetComponent<Image>().raycastTarget = false;
            _targets[i] = t;
            _targetImgs[i] = t.GetComponent<Image>();
        }

        // "Manita": círculo blanco con puntita
        _hand = KidUI.CircleAt(_stage, "Hand", new Color(1f, 1f, 1f, 0.95f),
                               new Vector2(0.5f, 0.16f), 30f);
        _hand.GetComponent<Image>().raycastTarget = false;
        var tip = KidUI.CircleAt(_hand, "Tip", new Color(1f, 1f, 1f, 0.55f),
                                 new Vector2(0.5f, 1.05f), 14f);
        tip.GetComponent<Image>().raycastTarget = false;
    }

    IEnumerator Loop()
    {
        var wait = new WaitForSecondsRealtime(0.55f);
        int correct = 1;
        while (true)
        {
            if (_stage == null) yield break;

            // Resetea y elige objetivo correcto
            for (int i = 0; i < 3; i++)
                if (_targetImgs[i] != null) _targetImgs[i].color = TARGET_OFF;
            correct = Random.Range(0, 3);
            if (_targetImgs[correct] != null) _targetImgs[correct].color = _catCol;
            SetHandAnchor(new Vector2(0.5f, 0.16f));
            yield return wait;

            // La mano viaja hasta el objetivo correcto
            Vector2 from = new Vector2(0.5f, 0.16f);
            Vector2 to = _targets[correct] != null
                ? _targets[correct].anchorMin
                : new Vector2(0.5f, 0.58f);
            float t = 0f, dur = 0.6f;
            while (t < dur)
            {
                if (_hand == null) yield break;
                t += Time.unscaledDeltaTime;
                SetHandAnchor(Vector2.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / dur)));
                yield return null;
            }

            // Toque + celebración
            if (_hand != null) UITween.PulseOnce(_hand, 0.75f, 0.18f);
            if (_targets[correct] != null) UITween.PulseOnce(_targets[correct], 1.35f, 0.30f);
            yield return new WaitForSecondsRealtime(0.12f);
            Sparkle(to);
            yield return new WaitForSecondsRealtime(0.85f);
        }
    }

    void SetHandAnchor(Vector2 a)
    {
        if (_hand == null) return;
        _hand.anchorMin = _hand.anchorMax = a;
        _hand.anchoredPosition = Vector2.zero;
    }

    /// <summary>Mini-destello de partículas alrededor del objetivo acertado.</summary>
    void Sparkle(Vector2 anchor)
    {
        for (int i = 0; i < 6; i++)
        {
            var p = KidUI.CircleAt(_stage, "spark",
                new Color(1f, 0.85f, 0.2f, 0.95f), anchor, Random.Range(6f, 11f));
            p.GetComponent<Image>().raycastTarget = false;
            StartCoroutine(SparkleFly(p, Random.insideUnitCircle.normalized *
                                          Random.Range(45f, 80f)));
        }
    }

    IEnumerator SparkleFly(RectTransform p, Vector2 vel)
    {
        var img = p.GetComponent<Image>();
        float t = 0f, dur = 0.45f;
        while (t < dur)
        {
            if (p == null) yield break;
            float dt = Time.unscaledDeltaTime;
            t += dt;
            p.anchoredPosition += vel * dt;
            if (img != null)
            {
                var c = img.color;
                c.a = 1f - t / dur;
                img.color = c;
            }
            yield return null;
        }
        if (p != null) Destroy(p.gameObject);
    }
}
