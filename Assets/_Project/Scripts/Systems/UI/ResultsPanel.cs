// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Pantalla de resultados unificada para TODOS los minijuegos:
/// estrellas 1-3 con animación secuencial, robot (NEO/AXEL/TITAN según dificultad)
/// que celebra o anima, contador de puntuación animado, confeti si lo haces muy bien
/// y botones grandes "Jugar otra vez" / "Elegir juego".
/// Uso desde un minijuego (vía MinigameBase.ShowResults) o directamente:
///   ResultsPanel.Show(new ResultsPanel.Config { ... });
/// </summary>
public class ResultsPanel : MonoBehaviour
{
    public class Config
    {
        public bool success = true;
        public int stars = 1;                 // 0-3
        public int score = 0;
        public string title;                  // null = automático
        public string subtitle;               // null = automático
        public string[] stats;                // líneas pequeñas opcionales
        public string categoryName = "";      // para el color de acento
        public System.Action onReplay;
        public System.Action onExit;
    }

    static ResultsPanel _current;
    Config _cfg;
    RectTransform _card;
    StarTint[] _stars;

    public static void Show(Config cfg)
    {
        if (_current != null) Destroy(_current.gameObject);
        KidUI.EnsureEventSystem();
        var go = new GameObject("ResultsPanel");
        _current = go.AddComponent<ResultsPanel>();
        _current._cfg = cfg ?? new Config();
        _current.Build();
    }

    void OnDestroy()
    {
        if (_current == this) _current = null;
    }

    static readonly Color STAR_ON  = new Color(1.00f, 0.82f, 0.12f);
    static readonly Color STAR_OFF = new Color(0.16f, 0.20f, 0.34f);

    void Build()
    {
        var cfg = _cfg;
        Color accent = IntroPanel.CategoryColor(cfg.categoryName);

        var cv = KidUI.MakeCanvas("ResultsCanvas", 600, transform);
        var R = cv.GetComponent<RectTransform>();

        // Fondo espacial (sustituye al velo plano)
        KidUI.BuildSpaceBackground(R, withPlanet: false);

        _card = KidUI.RoundImg(R, "Card", new Color(0.055f, 0.075f, 0.15f, 0.98f),
                               new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                               Vector2.zero, new Vector2(860f, 640f), 0.8f);
        var topEdge = KidUI.RoundImg(_card, "Top", accent,
                                     new Vector2(0.36f, 0.985f), new Vector2(0.64f, 0.994f),
                                     Vector2.zero, Vector2.zero, 4f);
        topEdge.GetComponent<Image>().raycastTarget = false;

        // Robot según dificultad activa
        string robot = "neo";
        string robotName = "NEO";
        if (GameManager.Instance != null)
        {
            switch (GameManager.Instance.CurrentDifficulty)
            {
                case DifficultyLevel.Medium: robot = "axel";  robotName = "AXEL";  break;
                case DifficultyLevel.Hard:   robot = "titan"; robotName = "TITAN"; break;
            }
        }
        var robotSp = KidUI.LoadAvatar(robot);
        if (robotSp != null)
        {
            var rrt = KidUI.Sprite(_card, "Robot", robotSp,
                                   new Vector2(0.02f, 0.60f), new Vector2(0.26f, 0.97f));
            UITween.PopIn(rrt, 0.5f, 0.5f, 0.15f);
            if (!cfg.success)
            {
                var img = rrt.GetComponent<Image>();
                img.color = new Color(0.75f, 0.75f, 0.85f);   // robot "apagado" si fallas
            }
        }

        // Título y subtítulo
        string title = cfg.title ?? (cfg.success ? "¡MUY BIEN!" : "¡CASI LO TIENES!");
        string sub = cfg.subtitle ?? (cfg.success
            ? robotName + " está orgulloso de ti"
            : robotName + " sabe que la próxima vez lo lograrás");

        var titleT = KidUI.Txt(_card, "Title", title,
                               cfg.success ? STAR_ON : KidUI.ACCENT, 56,
                               new Vector2(0.24f, 0.80f), new Vector2(0.98f, 0.97f));
        titleT.fontStyle = FontStyles.Bold;
        titleT.characterSpacing = 2f;

        KidUI.Txt(_card, "Sub", sub, KidUI.DIM, 22,
                  new Vector2(0.24f, 0.72f), new Vector2(0.98f, 0.80f));

        // Estrellas
        _stars = new StarTint[3];
        for (int i = 0; i < 3; i++)
        {
            float x0 = 0.335f + i * 0.12f;
            var srt = MakeStar(_card, new Vector2(x0, 0.52f), new Vector2(x0 + 0.10f, 0.70f));
            _stars[i] = srt.GetComponent<StarTint>();
            _stars[i].SetColor(STAR_OFF);
        }

        // Puntuación con contador animado
        var scoreT = KidUI.Txt(_card, "Score", "0", Color.white, 64,
                               new Vector2(0.20f, 0.34f), new Vector2(0.80f, 0.50f));
        scoreT.fontStyle = FontStyles.Bold;
        KidUI.Txt(_card, "ScoreLbl", "PUNTOS", KidUI.DIM, 18,
                  new Vector2(0.20f, 0.30f), new Vector2(0.80f, 0.36f));
        GameFeel.CountUp(scoreT, 0, Mathf.Max(0, cfg.score), 0.9f);

        // Estadísticas opcionales
        if (cfg.stats != null && cfg.stats.Length > 0)
        {
            string joined = string.Join("      ", cfg.stats);
            KidUI.Txt(_card, "Stats", joined, KidUI.DIM, 19,
                      new Vector2(0.05f, 0.22f), new Vector2(0.95f, 0.30f));
        }

        // Botones grandes
        KidUI.Btn(_card, "JUGAR OTRA VEZ", accent,
                  new Vector2(0.08f, 0.05f), new Vector2(0.48f, 0.18f),
                  () => { Close(); cfg.onReplay?.Invoke(); }, 26f);
        KidUI.Btn(_card, "ELEGIR JUEGO", KidUI.BTNC,
                  new Vector2(0.52f, 0.05f), new Vector2(0.92f, 0.18f),
                  () => { Close(); cfg.onExit?.Invoke(); }, 26f);

        // Entrada animada + secuencia de estrellas + celebración
        UITween.FadeIn(cv.gameObject, 0.25f);
        UITween.PopIn(_card, 0.4f, 0.85f);
        StartCoroutine(StarSequence());

        if (cfg.success && cfg.stars >= 2) GameFeel.Confetti(cfg.stars >= 3 ? 60 : 35);
        if (cfg.success) GameFeel.PlaySuccess();
        else { GameFeel.PlayError(); GameFeel.Shake(_card, 10f, 0.4f); }
    }

    /// <summary>Estrella dibujada con rombo + rombo girado (sin sprites).</summary>
    RectTransform MakeStar(RectTransform parent, Vector2 am, Vector2 aM)
    {
        var holder = KidUI.Img(parent, "Star", new Color(0, 0, 0, 0), am, aM,
                               Vector2.zero, Vector2.zero);
        holder.GetComponent<Image>().raycastTarget = false;

        var a = KidUI.Img(holder, "A", Color.white,
                          new Vector2(0.15f, 0.15f), new Vector2(0.85f, 0.85f),
                          Vector2.zero, Vector2.zero);
        a.localRotation = Quaternion.Euler(0, 0, 45f);
        var b = KidUI.Img(holder, "B", Color.white,
                          new Vector2(0.15f, 0.15f), new Vector2(0.85f, 0.85f),
                          Vector2.zero, Vector2.zero);
        b.localRotation = Quaternion.Euler(0, 0, 22.5f);
        var c = KidUI.Img(holder, "C", Color.white,
                          new Vector2(0.15f, 0.15f), new Vector2(0.85f, 0.85f),
                          Vector2.zero, Vector2.zero);
        c.localRotation = Quaternion.Euler(0, 0, 67.5f);

        // El color del conjunto se controla con el Image del holder vía tint de hijos.
        var proxy = holder.gameObject.AddComponent<StarTint>();
        proxy.parts = new[] { a.GetComponent<Image>(), b.GetComponent<Image>(), c.GetComponent<Image>() };
        return holder;
    }

    IEnumerator StarSequence()
    {
        yield return new WaitForSecondsRealtime(0.45f);
        for (int i = 0; i < 3; i++)
        {
            bool on = i < _cfg.stars;
            var tint = _stars[i];
            if (tint == null) continue;
            tint.SetColor(on ? STAR_ON : STAR_OFF);
            if (on)
            {
                GameFeel.PlayStar();
                UITween.PopIn((RectTransform)tint.transform, 0.35f, 0.3f);
                yield return new WaitForSecondsRealtime(0.30f);
            }
        }
    }

    void Close()
    {
        Time.timeScale = 1f;
        Destroy(gameObject);
    }
}

/// <summary>Aplica un color a las tres capas que forman la estrella.</summary>
public class StarTint : MonoBehaviour
{
    public Image[] parts;
    public void SetColor(Color c)
    {
        if (parts == null) return;
        foreach (var p in parts) if (p != null) p.color = c;
    }
}
