// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// HUB del jugador: el planeta Attentia con sus 5 zonas (categorías) y el
/// progreso de reparación de cada una, la MISIÓN DE HOY (3 zonas sugeridas
/// según los puntos débiles del niño), la racha de días y la vitrina de logros.
/// Se muestra tras elegir perfil; desde aquí se despega a jugar.
/// Tocar una zona del planeta lleva directamente a esa categoría.
/// </summary>
public class ProgressMapScreen : MonoBehaviour
{
    const int GAMES_PER_CATEGORY = 5;

    static ProgressMapScreen _current;

    /// <summary>True mientras el hub está visible (el menú ESC no debe abrirse encima).</summary>
    public static bool IsOpen => _current != null;

    ProfileData _profile;
    RectTransform _root;
    GameObject _badgesOverlay;
    int _firstPendingCat = -1;   // primera zona de la misión sin completar hoy

    public static void Show()
    {
        if (_current != null) return;
        var pm = ProfileManager.Instance;
        if (pm == null || !pm.HasActiveProfile) return;
        KidUI.EnsureEventSystem();
        var go = new GameObject("ProgressMap");
        _current = go.AddComponent<ProgressMapScreen>();
        _current._profile = pm.ActiveProfile;
        _current.Build();
    }

    void OnDestroy()
    {
        if (_current == this) _current = null;
    }

    // ================================================================ BUILD

    void Build()
    {
        var cv = KidUI.MakeCanvas("MapCanvas", 820, transform);
        _root = cv.GetComponent<RectTransform>();

        KidUI.BuildSpaceBackground(_root, withPlanet: false);

        var results = ProfileManager.Store != null
            ? ProfileManager.Store.GetResults(_profile.id)
            : new List<MinigameResultData>();

        // ---------- Cabecera
        var title = KidUI.Txt(_root, "Title", "PLANETA ATTENTIA", Color.white, 52,
                              new Vector2(0.04f, 0.90f), new Vector2(0.70f, 0.98f));
        title.fontStyle = FontStyles.Bold;
        title.characterSpacing = 5f;
        title.alignment = TextAlignmentOptions.MidlineLeft;

        var sub = KidUI.Txt(_root, "Sub", "La aventura de " + _profile.nombre, KidUI.DIM, 24,
                            new Vector2(0.04f, 0.855f), new Vector2(0.70f, 0.90f));
        sub.alignment = TextAlignmentOptions.MidlineLeft;

        // Racha
        int streak = AchievementSystem.GetStreakDays(_profile.id);
        var streakChip = KidUI.RoundImg(_root, "Streak",
            new Color(0.98f, 0.80f, 0.10f, streak > 0 ? 0.20f : 0.08f),
            new Vector2(0.04f, 0.79f), new Vector2(0.22f, 0.845f),
            Vector2.zero, Vector2.zero, 2.2f);
        var streakT = KidUI.Txt(streakChip, "T",
            streak > 0 ? $"Racha: {streak} día{(streak == 1 ? "" : "s")} seguidos"
                       : "¡Empieza tu racha hoy!",
            new Color(0.98f, 0.80f, 0.10f), 17, Vector2.zero, Vector2.one);
        streakT.fontStyle = FontStyles.Bold;

        KidUI.Btn(_root, "Cambiar jugador", new Color(0.08f, 0.10f, 0.18f, 0.9f),
                  new Vector2(0.83f, 0.93f), new Vector2(0.97f, 0.975f),
                  () =>
                  {
                      // No destruimos el hub aquí: la transición de escena (fundido a
                      // negro) lo cubre y lo destruye al cambiar de escena, evitando el
                      // "flash" de la PrimeraPantalla.
                      if (ProfileManager.Instance != null)
                          ProfileManager.Instance.SwitchProfile();
                  }, 15f);

        // ---------- Planeta con zonas
        BuildPlanet(results);

        // ---------- Misión de hoy
        BuildMissionCard(results);

        // ---------- Botones inferiores
        // Camino más corto al juego: directo a la primera zona pendiente de la
        // misión de hoy (sin pasar por la pantalla intermedia de categorías).
        KidUI.Btn(_root, "¡A JUGAR!", KidUI.GOOD,
                  new Vector2(0.60f, 0.06f), new Vector2(0.83f, 0.155f),
                  () =>
                  {
                      if (_firstPendingCat >= 0)
                          SceneLoader.LoadCategorySelector((MinigameCategory)_firstPendingCat);
                      else
                          SceneLoader.LoadGameSelector();   // misión cumplida: elige libre
                  }, 30f);

        KidUI.Btn(_root, "LOGROS", KidUI.BTNC,
                  new Vector2(0.855f, 0.06f), new Vector2(0.97f, 0.155f),
                  ShowBadges, 20f);

        // Nota: NO se hace fundido de todo el canvas para que el fondo opaco cubra
        // la pantalla anterior al instante (evita el "flash" de la PrimeraPantalla).
        // La entrada animada la dan las PopIn del contenido.

        // Celebración de logros nuevos
        StartCoroutine(CelebrateNewBadges());
    }

    // ---------------------------------------------------------------- PLANETA

    void BuildPlanet(List<MinigameResultData> results)
    {
        Vector2 center = new Vector2(0.30f, 0.44f);

        // Cuerpo del planeta
        var halo = KidUI.CircleAt(_root, "PlanetHalo", new Color(0.30f, 0.50f, 1f, 0.10f), center, 560f);
        halo.GetComponent<Image>().raycastTarget = false;
        var planet = KidUI.CircleAt(_root, "Planet", new Color(0.12f, 0.20f, 0.40f, 1f), center, 470f);
        planet.GetComponent<Image>().raycastTarget = false;

        // Detalles de superficie
        var d1 = KidUI.CircleAt(planet, "D1", new Color(0.18f, 0.30f, 0.55f, 0.8f), new Vector2(0.34f, 0.62f), 120f);
        var d2 = KidUI.CircleAt(planet, "D2", new Color(0.16f, 0.26f, 0.50f, 0.8f), new Vector2(0.68f, 0.36f), 90f);
        var d3 = KidUI.CircleAt(planet, "D3", new Color(0.09f, 0.15f, 0.32f, 0.9f), new Vector2(0.58f, 0.72f), 70f);
        d1.GetComponent<Image>().raycastTarget = false;
        d2.GetComponent<Image>().raycastTarget = false;
        d3.GetComponent<Image>().raycastTarget = false;

        var core = KidUI.Txt(planet, "Core", "ATTENTIA", new Color(1f, 1f, 1f, 0.25f), 30,
                             new Vector2(0.1f, 0.40f), new Vector2(0.9f, 0.60f));
        core.characterSpacing = 6f;
        core.fontStyle = FontStyles.Bold;

        // Zonas (una por categoría) alrededor del planeta
        string[] shortNames = { "Memoria", "Impulsos", "Emocional", "Atención", "Planif." };
        for (int c = 0; c < 5; c++)
        {
            float ang = (90f - c * 72f) * Mathf.Deg2Rad;
            Vector2 offset = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * 300f;
            BuildZoneNode(c, shortNames[c], center, offset, results);
        }
    }

    void BuildZoneNode(int cat, string shortName, Vector2 centerAnchor, Vector2 offsetPx,
                       List<MinigameResultData> results)
    {
        string catName = MinigameResultData.CategoryDisplayName((MinigameCategory)cat);
        Color col = IntroPanel.CategoryColor(catName);

        int reparados = results.Where(r => r.categoria == cat && r.completado)
                               .Select(r => r.minijuego).Distinct().Count();
        reparados = Mathf.Min(reparados, GAMES_PER_CATEGORY);
        float progress = (float)reparados / GAMES_PER_CATEGORY;

        var holder = new GameObject("Zone_" + catName);
        holder.transform.SetParent(_root, false);
        var hrt = holder.AddComponent<RectTransform>();
        hrt.anchorMin = hrt.anchorMax = centerAnchor;
        hrt.pivot = new Vector2(0.5f, 0.5f);
        hrt.anchoredPosition = offsetPx;
        hrt.sizeDelta = new Vector2(150f, 168f);

        var hit = holder.AddComponent<Image>();
        hit.color = new Color(0, 0, 0, 0.001f);

        // Anillo de progreso (radial)
        var ringBg = KidUI.CircleAt(hrt, "RingBg", new Color(1f, 1f, 1f, 0.10f),
                                    new Vector2(0.5f, 0.66f), 104f);
        ringBg.GetComponent<Image>().raycastTarget = false;
        var ring = KidUI.CircleAt(hrt, "Ring", col, new Vector2(0.5f, 0.66f), 104f);
        var ringImg = ring.GetComponent<Image>();
        ringImg.raycastTarget = false;
        ringImg.type = Image.Type.Filled;
        ringImg.fillMethod = Image.FillMethod.Radial360;
        ringImg.fillOrigin = (int)Image.Origin360.Top;
        ringImg.fillClockwise = true;
        ringImg.fillAmount = Mathf.Max(progress, 0.02f);

        var inner = KidUI.CircleAt(hrt, "Inner", new Color(0.06f, 0.09f, 0.18f, 1f),
                                   new Vector2(0.5f, 0.66f), 84f);
        inner.GetComponent<Image>().raycastTarget = false;
        var count = KidUI.Txt(inner, "N", $"{reparados}/{GAMES_PER_CATEGORY}",
                              progress >= 1f ? col : Color.white, 24,
                              Vector2.zero, Vector2.one);
        count.fontStyle = FontStyles.Bold;

        // Etiqueta
        var chip = KidUI.RoundImg(hrt, "Chip", new Color(col.r, col.g, col.b, 0.18f),
                                  new Vector2(0f, 0.02f), new Vector2(1f, 0.26f),
                                  Vector2.zero, Vector2.zero, 2.4f);
        chip.GetComponent<Image>().raycastTarget = false;
        var lbl = KidUI.Txt(chip, "T", shortName, col, 17, Vector2.zero, Vector2.one);
        lbl.fontStyle = FontStyles.Bold;

        // Zona completa = estrella dorada dibujada (la fuente TMP no trae "★")
        if (progress >= 1f)
            DrawMiniStar(hrt, new Vector2(0.80f, 0.95f), 30f, new Color(1f, 0.82f, 0.12f));

        var btn = holder.AddComponent<Button>();
        btn.targetGraphic = hit;
        MinigameCategory captured = (MinigameCategory)cat;
        btn.onClick.AddListener(() =>
        {
            if (UIAudioManager.Instance != null) UIAudioManager.Instance.PlayClick();
            // Sin Destroy: el fundido de escena cubre el hub y lo destruye al cambiar
            // de escena (evita el "flash" de la PrimeraPantalla al ir a la categoría).
            SceneLoader.LoadCategorySelector(captured);
        });
        ButtonJuice.Attach(holder);

        UITween.PopIn(hrt, 0.4f, 0.6f, 0.07f * cat);
    }

    // ---------------------------------------------------------------- MISIÓN

    void BuildMissionCard(List<MinigameResultData> results)
    {
        var card = KidUI.RoundImg(_root, "Mission", new Color(0.055f, 0.075f, 0.15f, 0.95f),
                                  new Vector2(0.60f, 0.30f), new Vector2(0.97f, 0.76f),
                                  Vector2.zero, Vector2.zero, 0.9f);
        var pill = KidUI.RoundImg(card, "Pill", KidUI.WARN,
                                  new Vector2(0.32f, 0.985f), new Vector2(0.68f, 0.995f),
                                  Vector2.zero, Vector2.zero, 4f);
        pill.GetComponent<Image>().raycastTarget = false;

        var title = KidUI.Txt(card, "T", "MISIÓN DE HOY", Color.white, 28,
                              new Vector2(0.06f, 0.86f), new Vector2(0.94f, 0.97f));
        title.fontStyle = FontStyles.Bold;
        title.characterSpacing = 3f;

        KidUI.Txt(card, "Sub", "Los robots necesitan tu ayuda en estas zonas:",
                  KidUI.DIM, 17, new Vector2(0.06f, 0.78f), new Vector2(0.94f, 0.86f));

        // 3 categorías objetivo: las que menos completadas / peor % llevan
        var targets = Enumerable.Range(0, 5)
            .OrderBy(c => results.Count(r => r.categoria == c && r.completado))
            .ThenBy(c =>
            {
                var rr = results.Where(r => r.categoria == c && r.Intentos > 0).ToList();
                return rr.Count > 0 ? rr.Average(r => r.PorcentajeAcierto) : 0f;
            })
            .Take(3)
            .ToList();

        string today = DateTime.Now.ToString("dd/MM/yyyy");
        int done = 0;
        for (int i = 0; i < targets.Count; i++)
        {
            int cat = targets[i];
            string catName = MinigameResultData.CategoryDisplayName((MinigameCategory)cat);
            Color col = IntroPanel.CategoryColor(catName);
            bool completed = results.Any(r => r.categoria == cat && r.completado &&
                                              DataUtils.TicksToLocalDate(r.fechaUtcTicks) == today);
            if (completed) done++;
            else if (_firstPendingCat < 0) _firstPendingCat = cat;

            float y1 = 0.72f - i * 0.215f;
            var row = KidUI.RoundImg(card, "Row" + i, new Color(1f, 1f, 1f, 0.04f),
                                     new Vector2(0.06f, y1 - 0.17f), new Vector2(0.94f, y1),
                                     Vector2.zero, Vector2.zero, 1.8f);
            row.GetComponent<Image>().raycastTarget = false;

            var dot = KidUI.CircleAt(row, "Dot", col, new Vector2(0.075f, 0.5f), 26f);
            dot.GetComponent<Image>().raycastTarget = false;

            var txt = KidUI.Txt(row, "Txt", "Completa 1 juego de " + catName,
                                completed ? KidUI.DIM : Color.white, 18,
                                new Vector2(0.14f, 0f), new Vector2(0.72f, 1f));
            txt.alignment = TextAlignmentOptions.MidlineLeft;

            if (completed)
            {
                DrawCheck(row, new Vector2(0.86f, 0.5f), 42f);
            }
            else
            {
                MinigameCategory captured = (MinigameCategory)cat;
                KidUI.Btn(row, "IR", col,
                          new Vector2(0.76f, 0.16f), new Vector2(0.94f, 0.84f),
                          () =>
                          {
                              // Sin Destroy: la transición de escena cubre y destruye
                              // el hub (evita el flash de la PrimeraPantalla).
                              SceneLoader.LoadCategorySelector(captured);
                          }, 18f);
            }
        }

        // Estado de la misión
        var status = KidUI.Txt(card, "Status",
            done >= 3 ? "¡MISIÓN CUMPLIDA! Eres increíble." : $"Progreso: {done} / 3",
            done >= 3 ? KidUI.GOOD : KidUI.DIM, 17,
            new Vector2(0.06f, 0.015f), new Vector2(0.94f, 0.09f));
        status.fontStyle = FontStyles.Bold;
        if (done >= 3) GameFeel.Confetti(30);
    }

    // ---------------------------------------------------------------- LOGROS

    void ShowBadges()
    {
        if (_badgesOverlay != null) { Destroy(_badgesOverlay); _badgesOverlay = null; return; }

        _badgesOverlay = new GameObject("BadgesOverlay");
        _badgesOverlay.transform.SetParent(_root, false);
        var ort = _badgesOverlay.AddComponent<RectTransform>();
        ort.anchorMin = Vector2.zero; ort.anchorMax = Vector2.one;
        ort.sizeDelta = Vector2.zero;

        var dim = KidUI.Img(ort, "Dim", new Color(0f, 0f, 0f, 0.75f),
                            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        var card = KidUI.RoundImg(ort, "Card", new Color(0.055f, 0.075f, 0.15f, 0.98f),
                                  new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                                  Vector2.zero, new Vector2(1240f, 760f), 0.7f);

        var title = KidUI.Txt(card, "T", "TUS LOGROS", Color.white, 34,
                              new Vector2(0.05f, 0.90f), new Vector2(0.70f, 0.985f));
        title.fontStyle = FontStyles.Bold;
        title.characterSpacing = 4f;
        title.alignment = TextAlignmentOptions.MidlineLeft;

        KidUI.Btn(card, "Cerrar", KidUI.BAD,
                  new Vector2(0.85f, 0.915f), new Vector2(0.965f, 0.975f),
                  () => { Destroy(_badgesOverlay); _badgesOverlay = null; }, 17f);

        var badges = AchievementSystem.Evaluate(_profile.id);
        int unlockedCount = badges.Count(b => b.unlocked);
        KidUI.Txt(card, "Count", $"{unlockedCount} / {badges.Count} conseguidos",
                  KidUI.DIM, 19, new Vector2(0.05f, 0.855f), new Vector2(0.70f, 0.90f))
            .alignment = TextAlignmentOptions.MidlineLeft;

        // Rejilla 5 x 3
        int cols = 5;
        for (int i = 0; i < badges.Count; i++)
        {
            var b = badges[i];
            int col = i % cols, row = i / cols;
            float x0 = 0.04f + col * 0.192f;
            float y1 = 0.82f - row * 0.27f;

            var cell = KidUI.RoundImg(card, "B_" + b.id,
                new Color(1f, 1f, 1f, b.unlocked ? 0.06f : 0.025f),
                new Vector2(x0, y1 - 0.245f), new Vector2(x0 + 0.176f, y1),
                Vector2.zero, Vector2.zero, 1.4f);

            Color medal = b.unlocked ? b.color : new Color(0.25f, 0.29f, 0.40f, 1f);
            var ring = KidUI.CircleAt(cell, "Ring", medal, new Vector2(0.5f, 0.70f), 74f);
            ring.GetComponent<Image>().raycastTarget = false;
            var innerC = KidUI.CircleAt(cell, "In", new Color(0.05f, 0.07f, 0.14f, 1f),
                                        new Vector2(0.5f, 0.70f), 58f);
            innerC.GetComponent<Image>().raycastTarget = false;
            var sym = KidUI.Txt(innerC, "S", b.unlocked ? b.symbol : "?",
                                b.unlocked ? medal : KidUI.DIM, 24,
                                Vector2.zero, Vector2.one);
            sym.fontStyle = FontStyles.Bold;

            var nm = KidUI.Txt(cell, "N", b.nombre,
                               b.unlocked ? Color.white : KidUI.DIM, 15,
                               new Vector2(0.04f, 0.24f), new Vector2(0.96f, 0.44f));
            nm.fontStyle = FontStyles.Bold;
            KidUI.Txt(cell, "D", b.desc, KidUI.DIM, 12,
                      new Vector2(0.04f, 0.03f), new Vector2(0.96f, 0.24f));

            if (b.unlocked) UITween.PopIn(cell, 0.3f, 0.8f, 0.03f * i);
        }

        UITween.FadeIn(_badgesOverlay, 0.2f);
    }

    // ---------------------------------------------------------------- FORMAS

    /// <summary>Estrella pequeña dibujada con 3 cuadrados rotados (sin fuente).</summary>
    void DrawMiniStar(RectTransform parent, Vector2 anchorPoint, float sizePx, Color color)
    {
        var h = new GameObject("MiniStar");
        h.transform.SetParent(parent, false);
        var rt = h.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchorPoint;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(sizePx, sizePx);
        float[] angles = { 45f, 22.5f, 67.5f };
        foreach (float a in angles)
        {
            var part = KidUI.Img(rt, "p", color,
                new Vector2(0.18f, 0.18f), new Vector2(0.82f, 0.82f),
                Vector2.zero, Vector2.zero);
            part.localRotation = Quaternion.Euler(0, 0, a);
            part.GetComponent<Image>().raycastTarget = false;
        }
        UITween.PopIn(rt, 0.35f, 0.4f);
    }

    /// <summary>Marca de "hecho" dibujada con dos pastillas (sin fuente).</summary>
    void DrawCheck(RectTransform parent, Vector2 anchorPoint, float sizePx)
    {
        var h = new GameObject("Check");
        h.transform.SetParent(parent, false);
        var rt = h.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchorPoint;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(sizePx, sizePx);

        var a1 = KidUI.RoundImg(rt, "a1", KidUI.GOOD,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(-sizePx * 0.20f, -sizePx * 0.08f),
            new Vector2(sizePx * 0.42f, sizePx * 0.16f), 5f);
        a1.localRotation = Quaternion.Euler(0, 0, 45f);
        a1.GetComponent<Image>().raycastTarget = false;

        var a2 = KidUI.RoundImg(rt, "a2", KidUI.GOOD,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(sizePx * 0.10f, 0f),
            new Vector2(sizePx * 0.70f, sizePx * 0.16f), 5f);
        a2.localRotation = Quaternion.Euler(0, 0, -45f);
        a2.GetComponent<Image>().raycastTarget = false;
    }

    IEnumerator CelebrateNewBadges()
    {
        yield return new WaitForSecondsRealtime(0.6f);
        var newly = AchievementSystem.TakeNewlyUnlocked(_profile.id);
        if (newly.Count == 0) yield break;

        GameFeel.Confetti(45);
        GameFeel.PlayStar();

        var b = newly[0];
        var banner = KidUI.RoundImg(_root, "NewBadge",
            new Color(b.color.r, b.color.g, b.color.b, 0.95f),
            new Vector2(0.30f, 0.80f), new Vector2(0.70f, 0.875f),
            Vector2.zero, Vector2.zero, 1.4f);
        string extra = newly.Count > 1 ? $"  (+{newly.Count - 1} más)" : "";
        var t = KidUI.Txt(banner, "T", "¡NUEVO LOGRO!  " + b.nombre + extra,
                          Color.white, 22, Vector2.zero, Vector2.one);
        t.fontStyle = FontStyles.Bold;
        UITween.PopIn(banner, 0.35f, 0.7f);

        yield return new WaitForSecondsRealtime(3f);
        if (banner != null) UITween.FadeOut(banner.gameObject, 0.4f,
            () => { if (banner != null) Destroy(banner.gameObject); });
    }
}
