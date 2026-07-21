// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// PANTALLA DE DISTRITO — sustituye visualmente las escenas de selección de
/// minijuego sin tocarlas: un overlay opaco generado 100% por código se muestra
/// encima al cargar la escena de categoría (ver WorldNavRouter).
///
/// Narrativa: el distrito está dividido en 5 SECTORES destrozados por la
/// Tormenta del Caos. Cada sector es un minijuego; al completarlo, el sector
/// REVIVE (color, luces, movimiento) y queda así para siempre (telemetría).
/// Con los 5 sectores revividos, la Fuente Cognitiva del distrito se restaura.
/// </summary>
public class DistrictScreen : MonoBehaviour
{
    static DistrictScreen _current;
    public static bool IsOpen => _current != null;

    int _cat;
    RectTransform _root;

    public static void Show(int cat)
    {
        if (_current != null) return;
        KidUI.EnsureEventSystem();
        var go = new GameObject("DistrictScreen");
        _current = go.AddComponent<DistrictScreen>();
        _current._cat = cat;
        _current.Build();
    }

    void OnDestroy()
    {
        if (_current == this) _current = null;
    }

    // ================================================================ BUILD

    void Build()
    {
        var cv = KidUI.MakeCanvas("DistrictCanvas", 400, transform);
        _root = cv.GetComponent<RectTransform>();

        var d = GameCatalog.Get(_cat);
        Color col = GameCatalog.CatColor(_cat);
        string profileId = GameCatalog.ActiveProfileId;
        int revividos = GameCatalog.CompletedCount(profileId, _cat);

        KidUI.BuildSpaceBackground(_root, withPlanet: false);

        // Tinte ambiental del distrito (el color de la zona baña la pantalla)
        var tint = KidUI.Img(_root, "Tint", new Color(col.r, col.g, col.b, 0.05f),
                             Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        tint.GetComponent<Image>().raycastTarget = false;

        // ---------- Cabecera
        KidUI.Btn(_root, "◀  PLANETA", KidUI.BTNC,
                  new Vector2(0.015f, 0.925f), new Vector2(0.135f, 0.975f),
                  () => SceneLoader.GoToMainMenu(), 16f);

        var title = KidUI.Txt(_root, "Title", d.nombre, col, 46,
                              new Vector2(0.16f, 0.905f), new Vector2(0.84f, 0.985f));
        title.fontStyle = FontStyles.Bold;
        title.characterSpacing = 4f;

        var lema = KidUI.Txt(_root, "Lema", d.lema, KidUI.DIM, 18,
                             new Vector2(0.16f, 0.862f), new Vector2(0.84f, 0.905f));

        // Fuente Cognitiva: ahora mide el RETO acumulado del distrito (suma de
        // rangos de los 5 sectores, 0-20). Con 20/20 → Fuente Legendaria.
        int sumaReto = ChallengeSystem.SumaDistrito(profileId, _cat);
        bool legendaria = sumaReto >= 20;
        var chip = KidUI.RoundImg(_root, "Fuente",
            new Color(col.r, col.g, col.b, legendaria ? 0.30f : 0.14f),
            new Vector2(0.815f, 0.925f), new Vector2(0.985f, 0.975f),
            Vector2.zero, Vector2.zero, 2.2f);
        chip.GetComponent<Image>().raycastTarget = false;
        var ft = KidUI.Txt(chip, "T",
            legendaria ? "FUENTE LEGENDARIA" : $"FUENTE: {sumaReto}/20",
            legendaria ? new Color(0.98f, 0.80f, 0.10f) : col,
            16, Vector2.zero, Vector2.one);
        ft.fontStyle = FontStyles.Bold;

        // ---------- Los 5 sectores (3 arriba, 2 abajo)
        Vector2[] centers =
        {
            new Vector2(0.185f, 0.60f), new Vector2(0.50f, 0.60f), new Vector2(0.815f, 0.60f),
            new Vector2(0.345f, 0.245f), new Vector2(0.655f, 0.245f)
        };
        for (int i = 0; i < 5; i++)
            BuildSector(i, d, col, centers[i],
                        ChallengeSystem.Rank(profileId, d.games[i].sceneBase));

        // ---------- Robot guía con su frase según el estado del distrito
        BuildGuide(d, revividos);

        // ---------- Celebración si algún sector acaba de subir de rango
        StartCoroutine(CelebrateRankUps(profileId, d));
    }

    // ---------------------------------------------------------------- CELEBRACIÓN

    System.Collections.IEnumerator CelebrateRankUps(string profileId, GameCatalog.DistrictInfo d)
    {
        yield return new WaitForSecondsRealtime(0.5f);

        int bestI = -1, bestRank = 0;
        for (int i = 0; i < 5; i++)
        {
            string sb = d.games[i].sceneBase;
            int r = ChallengeSystem.Rank(profileId, sb);
            int seen = ChallengeSystem.RangoVisto(profileId, sb);
            if (r > seen)
            {
                if (r >= bestRank) { bestRank = r; bestI = i; }
                ChallengeSystem.MarcarVisto(profileId, sb, r);   // no repetir la fiesta
            }
        }
        if (bestI < 0) yield break;

        GameFeel.Confetti(40);
        GameFeel.PlayStar();

        Color rc = ChallengeSystem.ColorRango(bestRank);
        var banner = KidUI.RoundImg(_root, "RankUp", new Color(rc.r, rc.g, rc.b, 0.95f),
            new Vector2(0.27f, 0.80f), new Vector2(0.73f, 0.885f),
            Vector2.zero, Vector2.zero, 1.4f);
        DistrictArt.NoRay(banner);
        var t = KidUI.Txt(banner, "T",
            $"¡{d.sectorTag} {bestI + 1} YA ES DE {ChallengeSystem.NombreRango(bestRank)}!",
            Color.white, 22, Vector2.zero, Vector2.one);
        t.fontStyle = FontStyles.Bold;
        UITween.PopIn(banner, 0.35f, 0.7f);

        yield return new WaitForSecondsRealtime(3f);
        if (banner != null) UITween.FadeOut(banner.gameObject, 0.4f,
            () => { if (banner != null) Destroy(banner.gameObject); });
    }

    // ---------------------------------------------------------------- SECTOR

    void BuildSector(int i, GameCatalog.DistrictInfo d, Color col, Vector2 center, int rank)
    {
        var g = d.games[i];
        bool alive = rank >= 1;
        Color rankCol = ChallengeSystem.ColorRango(rank);

        var holder = new GameObject("Sector_" + i);
        holder.transform.SetParent(_root, false);
        var rt = holder.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = center;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(520f, 320f);

        // Borde con el COLOR DEL RANGO cuando el sector está vivo (bronce→diamante)
        if (alive)
        {
            var edge = KidUI.RoundImg(rt, "Edge", new Color(rankCol.r, rankCol.g, rankCol.b, 0.55f),
                Vector2.zero, Vector2.one, Vector2.zero, new Vector2(8f, 8f), 1.0f);
            DistrictArt.NoRay(edge);
        }

        var bg = KidUI.RoundImg(rt, "Bg",
            alive ? new Color(0.07f, 0.10f, 0.20f, 0.97f) : new Color(0.055f, 0.065f, 0.115f, 0.97f),
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 1.0f);

        // Etiqueta del sector con su rango ("PLANTA 1 · ORO")
        var tag = KidUI.RoundImg(bg, "Tag", new Color(1f, 1f, 1f, 0.07f),
                                 new Vector2(0.035f, 0.84f), new Vector2(0.52f, 0.955f),
                                 Vector2.zero, Vector2.zero, 2.6f);
        DistrictArt.NoRay(tag);
        var tagT = KidUI.Txt(tag, "T",
                             alive ? $"{d.sectorTag} {i + 1} · {ChallengeSystem.NombreRango(rank)}"
                                   : $"{d.sectorTag} {i + 1}",
                             alive ? rankCol : KidUI.DIM, 14, Vector2.zero, Vector2.one);
        tagT.fontStyle = FontStyles.Bold;
        tagT.characterSpacing = 2f;

        // Insignia superior derecha: estrella (bronce/plata/oro) o gema (diamante)
        if (rank >= 1 && rank <= 3)
            DistrictArt.Star(bg, new Vector2(0.92f, 0.90f), 32f, new Color(1f, 0.82f, 0.12f));
        else if (rank >= 4)
        {
            var gem = KidUI.CircleAt(bg, "Gem", new Color(0.45f, 0.90f, 1f), new Vector2(0.92f, 0.90f), 30f);
            DistrictArt.NoRay(gem);
            gem.gameObject.AddComponent<StarTwinkle>();
        }

        // Arte del sector: en ruinas → arte procedural roto; revivido → el LOGO
        // del minijuego (la imagen clásica) brillando sobre el sector.
        var art = KidUI.Img(bg, "Art", Color.clear,
                            new Vector2(0.06f, 0.36f), new Vector2(0.94f, 0.86f),
                            Vector2.zero, Vector2.zero);
        art.GetComponent<Image>().raycastTarget = false;
        Sprite logo = alive ? g.LoadLogo() : null;
        if (logo != null)
        {
            var lg = KidUI.Sprite(art, "Logo", logo,
                                  new Vector2(0.30f, 0.02f), new Vector2(0.70f, 0.98f));
            DistrictArt.NoRay(lg);
            lg.gameObject.AddComponent<FloatBob>().Configure(5f, 1.1f);
        }
        else
        {
            DistrictArt.Sector(art, _cat, alive, col, i);
        }

        // Nombre del minijuego
        var name = KidUI.Txt(bg, "Name", g.display, alive ? Color.white : new Color(0.72f, 0.78f, 0.90f),
                             24, new Vector2(0.05f, 0.19f), new Vector2(0.95f, 0.36f));
        name.fontStyle = FontStyles.Bold;

        // Estado / llamada a la acción según el rango del sector
        if (rank == 0)
        {
            var cta = KidUI.RoundImg(bg, "Cta", col,
                                     new Vector2(0.28f, 0.045f), new Vector2(0.72f, 0.175f),
                                     Vector2.zero, Vector2.zero, 2.4f);
            DistrictArt.NoRay(cta);
            var ct = KidUI.Txt(cta, "T", "¡REVIVIR!", Color.white, 18, Vector2.zero, Vector2.one);
            ct.fontStyle = FontStyles.Bold;
            cta.gameObject.AddComponent<StarTwinkle>();   // parpadeo suave, invita a tocar
        }
        else if (rank <= 3)
        {
            int need = ChallengeSystem.PuntosParaRango(GameCatalog.ActiveProfileId, g.sceneBase, rank + 1);
            string sig = ChallengeSystem.NombreRango(rank + 1);
            string txt = need > 0 ? $"Saca {need} pts más para {sig}" : $"¡A por el {sig}!";
            var st = KidUI.Txt(bg, "St", txt,
                               rankCol, 16, new Vector2(0.06f, 0.045f), new Vector2(0.94f, 0.17f));
            st.fontStyle = FontStyles.Bold;
        }
        else
        {
            var st = KidUI.Txt(bg, "St", "SECTOR DE DIAMANTE", new Color(0.45f, 0.90f, 1f), 15,
                               new Vector2(0.12f, 0.045f), new Vector2(0.88f, 0.17f));
            st.fontStyle = FontStyles.Bold;
            st.characterSpacing = 2f;
        }

        var btn = holder.AddComponent<Button>();
        btn.targetGraphic = bg.GetComponent<Image>();
        btn.onClick.AddListener(() =>
        {
            if (UIAudioManager.Instance != null) UIAudioManager.Instance.PlayClick();
            SceneLoader.LoadScene(GameCatalog.SceneFor(g));
        });
        ButtonJuice.Attach(holder);

        UITween.PopIn(rt, 0.38f, 0.82f, 0.06f * i);
    }

    // ---------------------------------------------------------------- GUÍA

    void BuildGuide(GameCatalog.DistrictInfo d, int revividos)
    {
        var (avatarId, nombre, gcol) = GameCatalog.Guide();
        string frase = d.guia[revividos >= 5 ? 2 : revividos > 0 ? 1 : 0];

        var holder = KidUI.Img(_root, "Guide", Color.clear,
                               new Vector2(0.015f, 0.015f), new Vector2(0.09f, 0.135f),
                               Vector2.zero, Vector2.zero);
        holder.GetComponent<Image>().raycastTarget = false;
        var sp = KidUI.LoadAvatar(avatarId);
        if (sp != null) DistrictArt.NoRay(KidUI.Sprite(holder, "Av", sp, Vector2.zero, Vector2.one));
        else
        {
            var c = KidUI.CircleAt(holder, "C", gcol, new Vector2(0.5f, 0.5f), 90f);
            DistrictArt.NoRay(c);
        }
        holder.gameObject.AddComponent<FloatBob>().Configure(6f, 1.1f);

        var bubble = KidUI.RoundImg(_root, "Bubble", new Color(0.05f, 0.07f, 0.15f, 0.95f),
                                    new Vector2(0.10f, 0.025f), new Vector2(0.52f, 0.125f),
                                    Vector2.zero, Vector2.zero, 1.8f);
        DistrictArt.NoRay(bubble);
        var nm = KidUI.Txt(bubble, "N", nombre, gcol, 14,
                           new Vector2(0.04f, 0.58f), new Vector2(0.5f, 0.95f));
        nm.fontStyle = FontStyles.Bold;
        nm.alignment = TextAlignmentOptions.MidlineLeft;
        var tx = KidUI.Txt(bubble, "T", frase, Color.white, 17,
                           new Vector2(0.04f, 0.06f), new Vector2(0.96f, 0.62f));
        tx.alignment = TextAlignmentOptions.MidlineLeft;
        tx.enableWordWrapping = true;

        UITween.PopIn(bubble, 0.4f, 0.85f, 0.35f);
    }
}

// ====================================================================
//  ARTE PROCEDURAL DE LOS DISTRITOS (compartido con DistrictPickScreen)
// ====================================================================

/// <summary>Dibuja el arte temático de un sector: en ruinas (gris, grietas)
/// o revivido (color, luces, movimiento). Todo con primitivas de KidUI.</summary>
public static class DistrictArt
{
    static readonly Color GRAY  = new Color(0.30f, 0.34f, 0.46f, 1f);
    static readonly Color DARK  = new Color(0.04f, 0.06f, 0.12f, 1f);
    static readonly Color LIGHT = new Color(1f, 0.88f, 0.45f, 0.95f);

    public static void NoRay(RectTransform rt)
    {
        var img = rt.GetComponent<Image>();
        if (img != null) img.raycastTarget = false;
    }

    /// <summary>Estrella dorada (3 cuadrados rotados, sin depender de la fuente).</summary>
    public static void Star(RectTransform parent, Vector2 anchor, float sizePx, Color color)
    {
        var h = new GameObject("Star");
        h.transform.SetParent(parent, false);
        var rt = h.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(sizePx, sizePx);
        float[] angles = { 45f, 22.5f, 67.5f };
        foreach (float a in angles)
        {
            var part = KidUI.Img(rt, "p", color, new Vector2(0.18f, 0.18f),
                                 new Vector2(0.82f, 0.82f), Vector2.zero, Vector2.zero);
            part.localRotation = Quaternion.Euler(0, 0, a);
            NoRay(part);
        }
        UITween.PopIn(rt, 0.35f, 0.4f);
    }

    /// <summary>index (0..4) varía la composición: cada sector de un distrito es
    /// DISTINTO aunque comparta tema (otra estantería, otro jardín, otra torre...).</summary>
    public static void Sector(RectTransform art, int cat, bool alive, Color col, int index)
    {
        Color body = alive ? Color.Lerp(GRAY, col, 0.75f) : GRAY;
        int i = Mathf.Abs(index) % 5;
        switch ((MinigameCategory)cat)
        {
            case MinigameCategory.Memory:              Biblioteca(art, alive, col, body, i); break;
            case MinigameCategory.ImpulseControl:      Central(art, alive, col, body, i);    break;
            case MinigameCategory.EmotionalManagement: Jardin(art, alive, col, body, i);     break;
            case MinigameCategory.Attention:           Torre(art, alive, col, body, i);      break;
            default:                                   Fabrica(art, alive, col, body, i);    break;
        }
        if (!alive) Cracks(art, i);
    }

    // Planta de la Gran Biblioteca: cada planta con su propia estantería
    static void Biblioteca(RectTransform a, bool alive, Color col, Color body, int i)
    {
        float shelfW = 260f + 20f * (i % 3);
        var shelf = KidUI.RoundImg(a, "Shelf", body, new Vector2(0.5f, 0.16f), new Vector2(0.5f, 0.16f),
                                   Vector2.zero, new Vector2(shelfW, 16f), 1.0f);
        NoRay(shelf);

        int books = 4 + (i % 2);                       // 4 o 5 libros
        float x0 = 0.5f - 0.05f * (books - 1);
        int fallen = (i * 2 + 1) % books;              // cada planta tira un libro distinto
        Color[] tints = { col, col * 0.8f, col * 1.15f, col * 0.9f, col * 1.05f };
        for (int b = 0; b < books; b++)
        {
            Color bc = alive ? tints[(b + i) % 5] : new Color(0.24f, 0.28f, 0.38f, 1f);
            float h = 46f + ((b + i) % 3) * 12f;
            var book = KidUI.RoundImg(a, "Book" + b, bc,
                new Vector2(x0 + 0.10f * b, 0.24f), new Vector2(x0 + 0.10f * b, 0.24f),
                new Vector2(0f, h / 2f), new Vector2(24f, h), 1.4f);
            NoRay(book);
            if (!alive && b == fallen)
                book.localRotation = Quaternion.Euler(0, 0, i % 2 == 0 ? -16f : 22f);
        }

        // La lámpara cambia de lado por planta
        var lamp = KidUI.CircleAt(a, "Lamp", alive ? LIGHT : new Color(0.22f, 0.25f, 0.34f, 1f),
                                  new Vector2(i % 2 == 0 ? 0.86f : 0.14f, 0.80f), 20f);
        NoRay(lamp);
        if (alive) lamp.gameObject.AddComponent<StarTwinkle>();

        // Detalle propio: globo terráqueo (planta 1), escalera (3), archivo (4)
        if (i == 1)
        {
            var globe = KidUI.CircleAt(a, "Globe", alive ? new Color(0.30f, 0.60f, 0.95f, 1f)
                                                         : new Color(0.26f, 0.30f, 0.40f, 1f),
                                       new Vector2(0.14f, 0.34f), 34f);
            NoRay(globe);
            if (alive) globe.gameObject.AddComponent<FloatBob>().Configure(3f, 1.2f);
        }
        else if (i == 3)
        {
            var ladder = KidUI.RoundImg(a, "Ladder", body * 1.2f, new Vector2(0.82f, 0.42f),
                                        new Vector2(0.82f, 0.42f), Vector2.zero, new Vector2(10f, 96f), 0.8f);
            ladder.localRotation = Quaternion.Euler(0, 0, alive ? -14f : -32f);
            NoRay(ladder);
        }
        else if (i == 4)
        {
            var box = KidUI.RoundImg(a, "Archivo", body * 1.15f, new Vector2(0.15f, 0.26f),
                                     new Vector2(0.15f, 0.26f), Vector2.zero, new Vector2(46f, 40f), 1.6f);
            NoRay(box);
        }
    }

    // Módulo de la Central: cada módulo con su propia maquinaria
    static void Central(RectTransform a, bool alive, Color col, Color body, int i)
    {
        // Conductos: posición y altura distintas por módulo
        float px = 0.22f + 0.03f * i;
        var left = KidUI.RoundImg(a, "P1", body, new Vector2(px, 0.45f), new Vector2(px, 0.45f),
                                  Vector2.zero, new Vector2(20f, 70f + 10f * (i % 3)), 1.0f);
        NoRay(left);
        var right = KidUI.RoundImg(a, "P2", body, new Vector2(1f - px, 0.45f), new Vector2(1f - px, 0.45f),
                                   Vector2.zero, new Vector2(20f, 96f - 10f * (i % 3)), 1.0f);
        NoRay(right);

        float coreX = i == 2 ? 0.42f : 0.5f;
        var core = KidUI.CircleAt(a, "Core", body, new Vector2(coreX, 0.48f), 62f + 6f * (i % 3));
        NoRay(core);
        var inner = KidUI.CircleAt(core, "In", alive ? Color.white : DARK, new Vector2(0.5f, 0.5f), 28f);
        NoRay(inner);
        if (alive)
        {
            inner.gameObject.AddComponent<StarTwinkle>();
            core.gameObject.AddComponent<FloatBob>().Configure(4f, 1.6f);
        }

        // Núcleo auxiliar (módulo 2) o batería (módulo 4)
        if (i == 2)
        {
            var aux = KidUI.CircleAt(a, "Aux", body * 0.9f, new Vector2(0.72f, 0.40f), 34f);
            NoRay(aux);
            if (alive) aux.gameObject.AddComponent<FloatBob>().Configure(3f, 2.0f);
        }
        else if (i == 4)
        {
            var bat = KidUI.RoundImg(a, "Bat", body * 1.15f, new Vector2(0.76f, 0.30f),
                                     new Vector2(0.76f, 0.30f), Vector2.zero, new Vector2(40f, 56f), 1.4f);
            NoRay(bat);
        }

        // Luz de aviso: en un sitio distinto según el módulo
        Vector2 warnPos = new Vector2(0.30f + 0.10f * i, 0.88f);
        var warn = KidUI.CircleAt(a, "Warn", alive ? new Color(1f, 0.45f, 0.25f, 0.95f)
                                                   : new Color(0.24f, 0.27f, 0.36f, 1f), warnPos, 16f);
        NoRay(warn);
        if (alive) warn.gameObject.AddComponent<StarTwinkle>();
    }

    // Jardín de la Calma: cada jardín con su propio árbol y rincones
    static void Jardin(RectTransform a, bool alive, Color col, Color body, int i)
    {
        var ground = KidUI.CircleAt(a, "Ground",
            alive ? new Color(0.14f, 0.42f, 0.30f, 1f) : new Color(0.20f, 0.24f, 0.32f, 1f),
            new Vector2(0.5f, 0.10f), 100f);
        ground.sizeDelta = new Vector2(240f + 16f * i, 56f);
        NoRay(ground);

        // Árbol descentrado y de copa distinta por jardín
        float tx = 0.34f + 0.08f * i;
        var trunk = KidUI.RoundImg(a, "Trunk", new Color(0.45f, 0.33f, 0.24f, 1f),
                                   new Vector2(tx, 0.36f), new Vector2(tx, 0.36f),
                                   Vector2.zero, new Vector2(14f, 50f + 6f * (i % 3)), 1.0f);
        if (!alive && i % 2 == 1) trunk.localRotation = Quaternion.Euler(0, 0, 10f); // árbol vencido
        NoRay(trunk);
        var crown = KidUI.CircleAt(a, "Crown", alive ? Color.Lerp(col, new Color(0.20f, 0.75f, 0.45f), 0.5f)
                                                     : new Color(0.28f, 0.32f, 0.40f, 1f),
                                   new Vector2(tx, 0.68f), 68f + 8f * (i % 3));
        NoRay(crown);
        if (alive) crown.gameObject.AddComponent<FloatBob>().Configure(4f, 1.0f);

        // Estanque (jardín 1), roca (3), arbusto (0 y 4)
        if (i == 1)
        {
            var pond = KidUI.CircleAt(a, "Pond", alive ? new Color(0.30f, 0.60f, 0.95f, 1f)
                                                       : new Color(0.16f, 0.20f, 0.30f, 1f),
                                      new Vector2(0.72f, 0.14f), 70f);
            pond.sizeDelta = new Vector2(90f, 34f);
            NoRay(pond);
        }
        else if (i == 3)
        {
            NoRay(KidUI.CircleAt(a, "Rock", new Color(0.35f, 0.38f, 0.48f, 1f), new Vector2(0.74f, 0.16f), 36f));
        }
        else
        {
            NoRay(KidUI.CircleAt(a, "Bush",
                alive ? new Color(0.18f, 0.55f, 0.36f, 1f) : new Color(0.24f, 0.28f, 0.36f, 1f),
                new Vector2(0.76f, 0.15f), 34f));
        }

        int flowers = 2 + i % 3;
        for (int f = 0; f < flowers; f++)
        {
            var flower = KidUI.CircleAt(a, "Fl" + f,
                alive ? (f % 2 == 1 ? new Color(1f, 0.55f, 0.70f, 1f) : LIGHT)
                      : new Color(0.26f, 0.29f, 0.38f, 1f),
                new Vector2(0.20f + 0.15f * f + 0.03f * i, 0.13f), 13f);
            NoRay(flower);
            if (alive) flower.gameObject.AddComponent<StarTwinkle>();
        }
    }

    // Torre de Observación: cada torre con su silueta
    static void Torre(RectTransform a, bool alive, Color col, Color body, int i)
    {
        float tx = i == 4 ? 0.38f : 0.5f;
        float th = 84f + 12f * (i % 3);
        var baseR = KidUI.RoundImg(a, "Base", body * 1.1f, new Vector2(tx, 0.10f), new Vector2(tx, 0.10f),
                                   Vector2.zero, new Vector2(110f, 22f), 1.2f);
        NoRay(baseR);
        var tower = KidUI.RoundImg(a, "Tower", body, new Vector2(tx, 0.40f), new Vector2(tx, 0.40f),
                                   Vector2.zero, new Vector2(30f + 4f * (i % 2), th), 1.0f);
        if (!alive && i % 2 == 0) tower.localRotation = Quaternion.Euler(0, 0, 6f); // torre torcida
        NoRay(tower);
        var dish = KidUI.CircleAt(a, "Dish", body * 1.25f, new Vector2(tx, 0.74f + 0.04f * (i % 2)), 44f + 6f * (i % 3));
        NoRay(dish);
        if (alive) dish.gameObject.AddComponent<FloatBob>().Configure(5f, 2.0f);
        var tip = KidUI.CircleAt(a, "Tip", alive ? col : new Color(0.24f, 0.27f, 0.36f, 1f),
                                 new Vector2(tx, 0.92f), 15f);
        NoRay(tip);
        if (alive) tip.gameObject.AddComponent<StarTwinkle>();

        // Antena auxiliar (torre 2) o torre gemela pequeña (torre 4)
        if (i == 2)
        {
            var ant = KidUI.RoundImg(a, "Ant", body * 0.9f, new Vector2(0.78f, 0.36f),
                                     new Vector2(0.78f, 0.36f), Vector2.zero, new Vector2(8f, 84f), 0.8f);
            NoRay(ant);
            NoRay(KidUI.CircleAt(a, "AntTip", alive ? col : new Color(0.24f, 0.27f, 0.36f, 1f),
                                 new Vector2(0.78f, 0.60f), 10f));
        }
        else if (i == 4)
        {
            var mini = KidUI.RoundImg(a, "Mini", body * 0.85f, new Vector2(0.72f, 0.28f),
                                      new Vector2(0.72f, 0.28f), Vector2.zero, new Vector2(22f, 58f), 1.0f);
            NoRay(mini);
            NoRay(KidUI.CircleAt(a, "MiniDish", body * 1.1f, new Vector2(0.72f, 0.48f), 28f));
        }

        if (alive)
        {
            var drone = KidUI.CircleAt(a, "Drone", new Color(col.r, col.g, col.b, 0.9f),
                                       new Vector2(0.18f + 0.14f * i, 0.72f), 16f);
            NoRay(drone);
            drone.gameObject.AddComponent<FloatBob>().Configure(9f, 1.5f);
        }
    }

    // Taller de la Gran Fábrica: cada taller con su nave y mecanismo
    static void Fabrica(RectTransform a, bool alive, Color col, Color body, int i)
    {
        float bx = 0.40f + 0.03f * (i % 3);
        var bodyR = KidUI.RoundImg(a, "Body", body, new Vector2(bx, 0.32f), new Vector2(bx, 0.32f),
                                   Vector2.zero, new Vector2(180f + 14f * (i % 3), 86f), 1.2f);
        NoRay(bodyR);

        int chims = 1 + i % 2;
        for (int c = 0; c < chims; c++)
        {
            var chim = KidUI.RoundImg(a, "Chim" + c, body * 1.15f,
                new Vector2(0.24f + 0.12f * c, 0.66f), new Vector2(0.24f + 0.12f * c, 0.66f),
                Vector2.zero, new Vector2(22f, 46f + 14f * ((c + i) % 2)), 1.0f);
            if (!alive && c == 0 && i % 2 == 0) chim.localRotation = Quaternion.Euler(0, 0, -12f);
            NoRay(chim);
        }

        float gx = 0.70f + 0.04f * (i % 2);
        var gear = KidUI.CircleAt(a, "Gear", body * 1.3f, new Vector2(gx, 0.58f + 0.05f * (i % 2)),
                                  48f + 6f * (i % 3));
        NoRay(gear);
        NoRay(KidUI.CircleAt(gear, "In", DARK, new Vector2(0.5f, 0.5f), 22f));
        if (alive) gear.gameObject.AddComponent<FloatBob>().Configure(6f, 1.8f);

        // Puente a medias (taller 1) o caja de piezas (taller 3)
        if (i == 1)
        {
            var bridge = KidUI.RoundImg(a, "Bridge", body * 1.1f, new Vector2(0.80f, 0.20f),
                                        new Vector2(0.80f, 0.20f), Vector2.zero,
                                        new Vector2(alive ? 96f : 54f, 10f), 0.8f);
            NoRay(bridge);
        }
        else if (i == 3)
        {
            NoRay(KidUI.RoundImg(a, "Crate", body * 1.15f, new Vector2(0.82f, 0.24f),
                                 new Vector2(0.82f, 0.24f), Vector2.zero, new Vector2(38f, 34f), 1.6f));
        }

        int wins = 2 + i % 2;
        for (int w = 0; w < wins; w++)
        {
            var win = KidUI.CircleAt(a, "Win" + w,
                alive ? LIGHT : new Color(0.22f, 0.25f, 0.34f, 1f),
                new Vector2(bx - 0.12f + 0.11f * w, 0.32f), 15f);
            NoRay(win);
            if (alive) win.gameObject.AddComponent<StarTwinkle>();
        }
    }

    /// <summary>Grietas de la Tormenta del Caos: patrón distinto en cada sector.</summary>
    static void Cracks(RectTransform a, int i)
    {
        int n = 2 + i % 2;
        for (int k = 0; k < n; k++)
        {
            Vector2 p = new Vector2(0.15f + 0.17f * ((i * 2 + k * 3) % 5),
                                    0.25f + 0.16f * ((i + k * 2) % 4));
            float rot = -70f + 37f * ((i + k) % 5);
            float len = 50f + 18f * ((i + k) % 3);
            var c = KidUI.RoundImg(a, "Crack" + k, new Color(0.02f, 0.03f, 0.08f, 0.85f),
                                   p, p, Vector2.zero, new Vector2(len, 5f), 0.6f);
            c.localRotation = Quaternion.Euler(0, 0, rot);
            NoRay(c);
        }
    }
}

// ====================================================================
//  ROUTER: muestra los overlays según la escena cargada (sin tocar escenas)
// ====================================================================

/// <summary>Se auto-crea al arrancar. Cuando se carga una escena de categoría
/// muestra su DistrictScreen; cuando se carga un selector de juegos muestra el
/// DistrictPickScreen. Las escenas originales quedan intactas debajo.</summary>
public class WorldNavRouter : MonoBehaviour
{
    static WorldNavRouter _i;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Boot()
    {
        if (_i != null) return;
        var go = new GameObject("WorldNavRouter");
        go.AddComponent<WorldNavRouter>();
    }

    void Awake()
    {
        if (_i != null && _i != this) { Destroy(gameObject); return; }
        _i = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnLoaded;
    }

    void OnDestroy()
    {
        if (_i == this) SceneManager.sceneLoaded -= OnLoaded;
    }

    void Start() => Route(SceneManager.GetActiveScene().name);

    void OnLoaded(Scene s, LoadSceneMode m) => Route(s.name);

    static void Route(string scene)
    {
        switch (scene)
        {
            case SceneLoader.ESCENAS_MEMORIA_EASY:
            case SceneLoader.ESCENAS_MEMORIA_MEDIUM:
            case SceneLoader.ESCENAS_MEMORIA_HARD:
                DistrictScreen.Show((int)MinigameCategory.Memory); break;

            case SceneLoader.ESCENAS_IMPULSOS_EASY:
            case SceneLoader.ESCENAS_IMPULSOS_MEDIUM:
            case SceneLoader.ESCENAS_IMPULSOS_HARD:
                DistrictScreen.Show((int)MinigameCategory.ImpulseControl); break;

            case SceneLoader.ESCENAS_EMOCIONAL_EASY:
            case SceneLoader.ESCENAS_EMOCIONAL_MEDIUM:
            case SceneLoader.ESCENAS_EMOCIONAL_HARD:
                DistrictScreen.Show((int)MinigameCategory.EmotionalManagement); break;

            case SceneLoader.ESCENAS_ATENCION_EASY:
            case SceneLoader.ESCENAS_ATENCION_MEDIUM:
            case SceneLoader.ESCENAS_ATENCION_HARD:
                DistrictScreen.Show((int)MinigameCategory.Attention); break;

            case SceneLoader.ESCENAS_PLANIF_EASY:
            case SceneLoader.ESCENAS_PLANIF_MEDIUM:
            case SceneLoader.ESCENAS_PLANIF_HARD:
                DistrictScreen.Show((int)MinigameCategory.Planning); break;

            case SceneLoader.GAME_SELECTOR_EASY:
            case SceneLoader.GAME_SELECTOR_MEDIUM:
            case SceneLoader.GAME_SELECTOR_HARD:
                DistrictPickScreen.Show(); break;
        }
    }
}
