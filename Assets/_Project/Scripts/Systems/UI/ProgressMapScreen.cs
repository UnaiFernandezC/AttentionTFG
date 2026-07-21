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

        // ---------- Planeta vivo e ilustrado (zonas como islas sobre la superficie)
        BuildPlanet(results);

        // ---------- Misión de hoy (visual, casi sin texto: 3 botones-estrella)
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
                          SceneLoader.LoadGameSelector();    // misión cumplida: elige libre
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

    /// <summary>Planeta VIVO y limpio: disco despejado con vetas de luz girando en
    /// dos capas contrarias (la rotación se ve, sin sopa de círculos), borde
    /// atmosférico luminoso, anillo elíptico por detrás, satélites en órbita, luna
    /// y robots flotando. Las 5 zonas son medallones GRANDES en pentágono, quietos
    /// y clicables.</summary>
    void BuildPlanet(List<MinigameResultData> results)
    {
        Vector2 center = new Vector2(0.285f, 0.45f);
        const float SIZE = 600f;

        // ----- Glow atmosférico exterior
        NoRay(KidUI.CircleAt(_root, "Halo2", new Color(0.30f, 0.55f, 1f, 0.06f), center, SIZE + 170f));
        NoRay(KidUI.CircleAt(_root, "Halo1", new Color(0.35f, 0.60f, 1f, 0.10f), center, SIZE + 80f));

        // ----- Anillo elíptico POR DETRÁS (el cuerpo lo tapa por el medio)
        var ringA = RingImg(_root, "RingA", new Color(0.60f, 0.75f, 1f, 0.45f), center, new Vector2(1120f, 320f));
        ringA.localRotation = Quaternion.Euler(0, 0, -16f);
        var ringB = RingImg(_root, "RingB", new Color(0.60f, 0.75f, 1f, 0.16f), center, new Vector2(1230f, 360f));
        ringB.localRotation = Quaternion.Euler(0, 0, -16f);

        // ----- Cuerpo
        var planet = KidUI.CircleAt(_root, "Planet", new Color(0.09f, 0.16f, 0.36f, 1f), center, SIZE);
        NoRay(planet);

        // ----- Disco despejado: la vida la ponen el borde atmosférico, los
        // satélites en órbita, la luna y los medallones (sin detalles dentro).

        // ----- Borde atmosférico luminoso (rim light)
        RingImg(_root, "Rim",  new Color(0.55f, 0.85f, 1f, 0.35f), center, new Vector2(SIZE + 14f, SIZE + 14f));
        RingImg(_root, "Rim2", new Color(0.55f, 0.85f, 1f, 0.12f), center, new Vector2(SIZE + 52f, SIZE + 52f));

        // ----- Las 5 zonas: medallones grandes en pentágono perfecto
        // (QUIETOS encima de la superficie giratoria, clicables)
        string[] shortNames = { "Memoria", "Impulsos", "Emocional", "Atención", "Planif." };
        for (int c = 0; c < 5; c++)
        {
            float zr = (90f - c * 72f) * Mathf.Deg2Rad;
            Vector2 zpos = new Vector2(0.5f, 0.5f) +
                           new Vector2(Mathf.Cos(zr), Mathf.Sin(zr)) * (195f / SIZE);
            BuildZoneBadge(planet, c, shortNames[c], zpos, results);
        }

        // ----- Satélites en órbita visible alrededor del planeta
        var orbit = KidUI.CircleAt(_root, "Orbit", Color.clear, center, SIZE + 190f);
        orbit.GetComponent<Image>().enabled = false;
        orbit.gameObject.AddComponent<SlowSpin>().degreesPerSecond = 8f;
        var sat = KidUI.CircleAt(orbit, "Sat", new Color(0.75f, 0.82f, 0.95f, 0.95f), new Vector2(1f, 0.5f), 22f);
        NoRay(sat);
        NoRay(KidUI.CircleAt(sat, "SatGlow", new Color(0.55f, 0.85f, 1f, 0.30f), new Vector2(0.5f, 0.5f), 40f));
        var spark = KidUI.CircleAt(orbit, "Spark", new Color(0.65f, 0.90f, 1f, 0.8f), new Vector2(0f, 0.5f), 12f);
        NoRay(spark);
        spark.gameObject.AddComponent<StarTwinkle>();

        // ----- Luna con cráter
        var moon = KidUI.CircleAt(_root, "Moon", new Color(0.60f, 0.63f, 0.75f, 0.95f),
                                  center + new Vector2(0.215f, 0.26f), 64f);
        NoRay(moon);
        NoRay(KidUI.CircleAt(moon, "MC", new Color(0.45f, 0.48f, 0.60f, 1f), new Vector2(0.36f, 0.58f), 20f));
        moon.gameObject.AddComponent<FloatBob>().Configure(7f, 0.7f);

        UITween.PopIn(planet, 0.5f, 0.85f);
    }

    /// <summary>Medallón-zona: aro de progreso grueso del color de la categoría,
    /// glow pulsante, contador grande y estrella al completar. Grande, limpio y
    /// clicable — sin sopa de círculos.</summary>
    void BuildZoneBadge(RectTransform planet, int cat, string shortName, Vector2 pos,
                        List<MinigameResultData> results)
    {
        string catName = MinigameResultData.CategoryDisplayName((MinigameCategory)cat);
        Color col = IntroPanel.CategoryColor(catName);

        // El aro del medallón mide el RETO acumulado de la zona (suma de rangos de
        // sus 5 sectores, 0-20). La misión diaria de más abajo sigue usando su
        // propio cómputo por juegos completados (GAMES_PER_CATEGORY) sin cambios.
        string profileId = _profile != null ? _profile.id : null;
        int sumaReto = ChallengeSystem.SumaDistrito(profileId, cat);   // 0-20
        const int MAX_RETO = 20;
        float progress = Mathf.Clamp01((float)sumaReto / MAX_RETO);

        var holder = new GameObject("Zone_" + catName);
        holder.transform.SetParent(planet, false);
        var hrt = holder.AddComponent<RectTransform>();
        hrt.anchorMin = hrt.anchorMax = pos;
        hrt.pivot = new Vector2(0.5f, 0.5f);
        hrt.anchoredPosition = Vector2.zero;
        hrt.sizeDelta = new Vector2(150f, 168f);

        var hit = holder.AddComponent<Image>();
        hit.color = new Color(0, 0, 0, 0.001f);

        Vector2 cc = new Vector2(0.5f, 0.60f);   // centro del medallón (deja sitio al chip)

        // Glow pulsante del color de la zona (más intenso con el progreso)
        var glow = KidUI.CircleAt(hrt, "Glow",
            new Color(col.r, col.g, col.b, 0.18f + 0.22f * progress), cc, 150f);
        NoRay(glow);
        glow.gameObject.AddComponent<StarTwinkle>();

        // Disco interior oscuro con borde
        NoRay(KidUI.CircleAt(hrt, "Border", new Color(0.03f, 0.05f, 0.13f, 1f), cc, 112f));
        NoRay(KidUI.CircleAt(hrt, "Disc",   new Color(0.07f, 0.11f, 0.22f, 1f), cc, 102f));

        // Aro de progreso grueso: base tenue + relleno radial brillante
        RingImg(hrt, "RingBase", new Color(col.r, col.g, col.b, 0.25f), cc,
                new Vector2(122f, 122f), thick: true);
        var ringFill = RingImg(hrt, "RingFill", col, cc, new Vector2(122f, 122f), thick: true);
        var rf = ringFill.GetComponent<Image>();
        rf.type = Image.Type.Filled;
        rf.fillMethod = Image.FillMethod.Radial360;
        rf.fillOrigin = (int)Image.Origin360.Top;
        rf.fillClockwise = true;
        rf.fillAmount = Mathf.Max(progress, 0.03f);

        // Contador en el centro (reto acumulado de la zona)
        var count = KidUI.Txt(hrt, "N", $"{sumaReto}/{MAX_RETO}",
                              progress >= 1f ? col : Color.white, 24,
                              new Vector2(0.10f, 0.46f), new Vector2(0.90f, 0.74f));
        count.fontStyle = FontStyles.Bold;

        // Baliza viva sobre el medallón en cuanto hay progreso
        if (progress > 0f)
        {
            var beacon = KidUI.CircleAt(hrt, "Beacon", Color.white, new Vector2(0.5f, 0.965f), 12f);
            NoRay(beacon);
            beacon.gameObject.AddComponent<StarTwinkle>();
        }

        // Etiqueta bajo el medallón, del color de la zona
        var chip = KidUI.RoundImg(hrt, "Chip", new Color(col.r, col.g, col.b, 0.26f),
                                  new Vector2(0.02f, 0.0f), new Vector2(0.98f, 0.155f),
                                  Vector2.zero, Vector2.zero, 2.4f);
        NoRay(chip);
        var lbl = KidUI.Txt(chip, "T", shortName, Color.white, 18, Vector2.zero, Vector2.one);
        lbl.fontStyle = FontStyles.Bold;

        // Zona completa = estrella dorada
        if (progress >= 1f)
            DrawMiniStar(hrt, new Vector2(0.86f, 0.90f), 34f, new Color(1f, 0.82f, 0.12f));

        var btn = holder.AddComponent<Button>();
        btn.targetGraphic = hit;
        MinigameCategory captured = (MinigameCategory)cat;
        btn.onClick.AddListener(() =>
        {
            if (UIAudioManager.Instance != null) UIAudioManager.Instance.PlayClick();
            // Sin Destroy: el fundido de escena cubre el hub y lo destruye al cambiar
            // de escena (evita el "flash" de la PrimeraPantalla).
            SceneLoader.LoadCategorySelector(captured);
        });
        ButtonJuice.Attach(holder);

        UITween.PopIn(hrt, 0.4f, 0.6f, 0.08f * cat + 0.2f);
    }

    static void NoRay(RectTransform rt) => rt.GetComponent<Image>().raycastTarget = false;

    // ------------------------------------------------ sprites de ARO (anillo hueco)
    // Generados una sola vez por código: fino para el anillo orbital y el borde
    // atmosférico; grueso para los aros de progreso de las zonas.
    static Sprite _ringThin, _ringThick;
    static Sprite RingThin  => _ringThin  != null ? _ringThin  : (_ringThin  = MakeRing(16f));
    static Sprite RingThick => _ringThick != null ? _ringThick : (_ringThick = MakeRing(42f));

    static Sprite MakeRing(float thickness)
    {
        const int size = 256;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        float c = (size - 1) / 2f;
        float rOut = size / 2f - 1.5f;
        float rIn  = rOut - thickness;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                float a = Mathf.Clamp01(rOut - d + 0.75f) * Mathf.Clamp01(d - rIn + 0.75f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    /// <summary>Aro colocado por punto central y tamaño en píxeles (con tamaño no
    /// uniforme se vuelve elíptico). thick=true usa el aro grueso.</summary>
    RectTransform RingImg(RectTransform p, string n, Color col, Vector2 anchorPoint,
                          Vector2 sizePx, bool thick = false)
    {
        var rt = KidUI.Img(p, n, col, anchorPoint, anchorPoint, Vector2.zero, sizePx);
        rt.GetComponent<Image>().sprite = thick ? RingThick : RingThin;
        NoRay(rt);
        return rt;
    }

    // ---------------------------------------------------------------- MISIÓN

    /// <summary>Misión de hoy ESPECÍFICA: 3 retos concretos, cada uno un minijuego
    /// nombrado de las zonas más flojas del niño, con un objetivo claro (reavivarlo o
    /// subirlo de rango). Deterministas por día y perfil; cada fila lleva directa al
    /// juego. Se marcan al jugarlos hoy.</summary>
    void BuildMissionCard(List<MinigameResultData> results)
    {
        var card = KidUI.RoundImg(_root, "Mission", new Color(0.055f, 0.075f, 0.15f, 0.95f),
                                  new Vector2(0.60f, 0.30f), new Vector2(0.97f, 0.72f),
                                  Vector2.zero, Vector2.zero, 0.9f);
        var pill = KidUI.RoundImg(card, "Pill", KidUI.WARN,
                                  new Vector2(0.32f, 0.985f), new Vector2(0.68f, 0.995f),
                                  Vector2.zero, Vector2.zero, 4f);
        pill.GetComponent<Image>().raycastTarget = false;

        var title = KidUI.Txt(card, "T", "MISIÓN DE HOY", Color.white, 30,
                              new Vector2(0.06f, 0.90f), new Vector2(0.94f, 0.99f));
        title.fontStyle = FontStyles.Bold;
        title.characterSpacing = 3f;
        var subt = KidUI.Txt(card, "Sub", "3 retos elegidos para " + _profile.nombre,
                             KidUI.DIM, 14, new Vector2(0.06f, 0.85f), new Vector2(0.94f, 0.905f));

        string pid = _profile.id;
        var missions = BuildDailyMissions(pid);

        int done = 0;
        for (int i = 0; i < missions.Count; i++)
        {
            var mis = missions[i];
            Color col = IntroPanel.CategoryColor(MinigameResultData.CategoryDisplayName((MinigameCategory)mis.cat));
            bool completed = mis.done;
            if (completed) done++;
            else if (_firstPendingCat < 0) _firstPendingCat = mis.cat;

            // Fila del reto
            float top = 0.80f - i * 0.205f;
            var row = new GameObject("Mission" + i);
            row.transform.SetParent(card, false);
            var rrt = row.AddComponent<RectTransform>();
            rrt.anchorMin = new Vector2(0.05f, top - 0.185f);
            rrt.anchorMax = new Vector2(0.95f, top);
            rrt.offsetMin = rrt.offsetMax = Vector2.zero;
            var rbg = row.AddComponent<Image>();
            rbg.color = new Color(col.r, col.g, col.b, completed ? 0.10f : 0.18f);
            rbg.sprite = KidUI.RoundedSprite; rbg.type = Image.Type.Sliced; rbg.pixelsPerUnitMultiplier = 1.4f;

            // Disco con logo del minijuego (o color de la zona)
            var disc = KidUI.CircleAt(rrt, "Disc",
                completed ? new Color(col.r * 0.5f, col.g * 0.5f, col.b * 0.5f, 1f) : col,
                new Vector2(0.11f, 0.5f), 66f);
            NoRay(disc);
            Sprite logo = mis.g.LoadLogo();
            if (logo != null)
            {
                var lg = KidUI.Sprite(disc, "Logo", logo, new Vector2(0.12f, 0.12f), new Vector2(0.88f, 0.88f));
                NoRay(lg);
            }

            // Nombre del minijuego + objetivo CONCRETO (rango + puntos)
            var nm = KidUI.Txt(rrt, "N", mis.g.display, completed ? KidUI.DIM : Color.white, 18,
                               new Vector2(0.24f, 0.50f), new Vector2(0.85f, 0.95f));
            nm.fontStyle = FontStyles.Bold;
            nm.alignment = TextAlignmentOptions.MidlineLeft;
            var gl = KidUI.Txt(rrt, "G", mis.goal, completed ? KidUI.DIM : mis.goalCol, 13,
                               new Vector2(0.24f, 0.08f), new Vector2(0.85f, 0.52f));
            gl.alignment = TextAlignmentOptions.MidlineLeft;

            // Estado a la derecha: estrella + check si está logrado
            DrawMiniStar(rrt, new Vector2(0.92f, 0.68f), 34f,
                completed ? new Color(1f, 0.82f, 0.12f) : new Color(1f, 1f, 1f, 0.35f));
            if (completed) DrawCheck(rrt, new Vector2(0.92f, 0.30f), 26f);

            // Toda la fila lleva directa al minijuego
            var btn = row.AddComponent<Button>();
            btn.targetGraphic = rbg;
            var captured = mis.g;
            btn.onClick.AddListener(() =>
            {
                if (UIAudioManager.Instance != null) UIAudioManager.Instance.PlayClick();
                SceneLoader.LoadScene(GameCatalog.SceneFor(captured));
            });
            ButtonJuice.Attach(row);

            UITween.PopIn(rrt, 0.4f, 0.7f, 0.08f * i + 0.1f);
        }

        // Progreso: 3 estrellas
        for (int s = 0; s < 3; s++)
            DrawMiniStar(card, new Vector2(0.40f + 0.10f * s, 0.10f), 26f,
                s < done ? new Color(1f, 0.82f, 0.12f) : new Color(1f, 1f, 1f, 0.18f));
        if (done >= 3)
        {
            var wow = KidUI.Txt(card, "Wow", "¡MISIÓN CUMPLIDA!", KidUI.GOOD, 19,
                                new Vector2(0.06f, 0.015f), new Vector2(0.94f, 0.07f));
            wow.fontStyle = FontStyles.Bold;
            GameFeel.Confetti(30);
        }
    }

    // ---------------------------------------------------------------- MISIONES (datos)

    struct Mision
    {
        public GameCatalog.GameInfo g;
        public int cat;
        public int target;      // 1=reavivar(bronce) · 2/3/4=diana de rango · 5=récord(diamante+)
        public bool done;
        public string goal;
        public Color goalCol;
    }

    /// <summary>3 misiones diarias CONCRETAS, fijadas por día y perfil (persisten en
    /// PlayerPrefs para que al lograrlas se queden marcadas). Cada una apunta a un
    /// minijuego concreto con objetivo claro: llegar a un rango (con su diana de
    /// puntos) o batir el récord. Se logran de verdad al subir el rango del sector.</summary>
    List<Mision> BuildDailyMissions(string pid)
    {
        string keyDay = DateTime.Now.ToString("yyyyMMdd");
        string todayDisp = DateTime.Now.ToString("dd/MM/yyyy");
        string key = "mision_" + pid + "_" + keyDay;
        var list = new List<Mision>();

        string saved = PlayerPrefs.GetString(key, "");
        if (!string.IsNullOrEmpty(saved))
            foreach (var part in saved.Split(';'))
            {
                var kv = part.Split('|');
                if (kv.Length != 2) continue;
                var found = FindGame(kv[0]);
                if (found.g != null && int.TryParse(kv[1], out int t))
                    list.Add(MakeMision(pid, found.g, found.cat, t, todayDisp));
            }

        if (list.Count < 3)
        {
            // Baraja diaria estable + prioriza sectores de menor rango (más flojos)
            var rng = new System.Random(key.GetHashCode());
            var pool = new List<(GameCatalog.GameInfo g, int cat, int rank)>();
            for (int c = 0; c < 5; c++)
                foreach (var gi in GameCatalog.Get(c).games)
                    pool.Add((gi, c, ChallengeSystem.Rank(pid, gi.sceneBase)));
            var chosen = pool.OrderBy(_ => rng.Next()).OrderBy(x => x.rank).Take(3).ToList();

            list.Clear();
            var save = new List<string>();
            foreach (var x in chosen)
            {
                // Objetivo del día: el siguiente rango, con un mínimo de PLATA para que
                // hasta un sector nuevo tenga una meta de puntos concreta (no solo "reavívalo").
                int target = x.rank >= ChallengeSystem.MAX_RANK ? 5 : Mathf.Max(2, x.rank + 1);
                list.Add(MakeMision(pid, x.g, x.cat, target, todayDisp));
                save.Add(x.g.sceneBase + "|" + target);
            }
            PlayerPrefs.SetString(key, string.Join(";", save));
            PlayerPrefs.Save();
        }
        return list;
    }

    Mision MakeMision(string pid, GameCatalog.GameInfo g, int cat, int target, string todayDisp)
    {
        int live = ChallengeSystem.Rank(pid, g.sceneBase);
        string goal; bool done; int colRank;

        if (target >= 5)
        {
            // Sector ya en DIAMANTE: batir la mejor marca
            int best = ChallengeSystem.MejorPuntuacion(pid, g.sceneBase);
            goal = best > 0 ? $"Bate tu récord: supera {best} pts" : "Consigue tu mejor marca";
            done = GameCatalog.CompletedToday(pid, g, todayDisp); colRank = 4;
        }
        else
        {
            // Objetivo concreto: llegar a un rango con su diana de puntos
            int diana = ChallengeSystem.DianaPuntos(g.sceneBase, target);
            string rn = ChallengeSystem.NombreRango(target);
            goal = diana > 0 ? $"Llega a {rn}: saca {diana} pts" : $"Llega a {rn}";
            done = live >= target; colRank = target;
        }
        return new Mision
        {
            g = g, cat = cat, target = target, done = done, goal = goal,
            goalCol = ChallengeSystem.ColorRango(colRank)
        };
    }

    static (GameCatalog.GameInfo g, int cat) FindGame(string sceneBase)
    {
        for (int c = 0; c < 5; c++)
            foreach (var gi in GameCatalog.Get(c).games)
                if (gi.sceneBase == sceneBase) return (gi, c);
        return (null, 0);
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

/// <summary>Rotación lenta y constante (tiempo no escalado). Se usa en la
/// superficie, las nubes y la órbita del planeta para que "viva" sin distraer.</summary>
public class SlowSpin : MonoBehaviour
{
    public float degreesPerSecond = 1f;

    void Update() =>
        transform.Rotate(0f, 0f, degreesPerSecond * Time.unscaledDeltaTime);
}
