// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Tutorial de bienvenida que explica TODAS las pantallas de AttentiON.
/// - Se muestra automáticamente la primera vez que se abre el juego
///   (bandera en PlayerPrefs) mediante un lanzador RuntimeInitialize.
/// - Se puede reabrir cuando se quiera con TutorialScreen.Show()
///   (hay un botón "?" en el menú de pausa).
/// Estilo KidUI (apto para no lectores: iconos grandes + texto simple).
/// </summary>
public class TutorialScreen : MonoBehaviour
{
    const string PREF_SEEN = "attention_tutorial_seen";

    static TutorialScreen _current;
    public static bool IsOpen => _current != null;

    int _step;
    RectTransform _root;
    RectTransform _card;
    GameObject _canvasGO;

    struct Step { public string icon; public Color color; public string title; public string body; }
    Step[] _steps;

    // ------------------------------------------------ Entradas

    public static void Show()
    {
        if (_current != null) return;
        KidUI.EnsureEventSystem();
        var go = new GameObject("TutorialScreen");
        _current = go.AddComponent<TutorialScreen>();
        _current.Build();
    }

    public static void ShowIfFirstTime()
    {
        if (PlayerPrefs.GetInt(PREF_SEEN, 0) == 1) return;
        Show();
    }

    static void MarkSeen()
    {
        PlayerPrefs.SetInt(PREF_SEEN, 1);
        PlayerPrefs.Save();
    }

    /// <summary>Auto-muestra el tutorial la primera vez, tras cargar la primera escena.</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoLaunchFirstTime()
    {
        if (PlayerPrefs.GetInt(PREF_SEEN, 0) == 1) return;
        var go = new GameObject("TutorialAutoLauncher");
        DontDestroyOnLoad(go);
        go.AddComponent<TutorialAutoLauncher>();
    }

    void OnDestroy() { if (_current == this) _current = null; }

    // ------------------------------------------------ Contenido

    void BuildSteps()
    {
        _steps = new[]
        {
            new Step {
                icon = "AttentiON", color = KidUI.ACCENT,
                title = "¡Bienvenido a AttentiON!",
                body  = "Un juego para entrenar tu mente: la atencion, la memoria, la calma, " +
                        "las emociones y los planes. Ayudaras a los robots NEO, AXEL y TITAN " +
                        "del planeta Attentia a recuperar sus poderes." },

            new Step {
                icon = "1", color = KidUI.GOOD,
                title = "Elige tu jugador",
                body  = "Al empezar eliges quien juega. Toca tu foto o crea un jugador nuevo con " +
                        "tu nombre, tu avatar y tu edad. Cada jugador guarda su propio progreso." },

            new Step {
                icon = "2", color = new Color(0.30f,0.60f,1f),
                title = "Tres niveles: NEO, AXEL y TITAN",
                body  = "La dificultad va segun tu edad, con un robot en cada nivel: NEO (facil), " +
                        "AXEL (medio) y TITAN (dificil). Cuanto mas mayor, mas reto." },

            new Step {
                icon = "3", color = KidUI.WARN,
                title = "Elige una zona",
                body  = "Hay 5 zonas: Atencion, Memoria, Control de impulsos, Emociones y " +
                        "Planificacion. Cada zona tiene 5 minijuegos distintos. ¡Elige la que quieras!" },

            new Step {
                icon = "4", color = new Color(0.20f,0.80f,0.70f),
                title = "A jugar",
                body  = "Antes de cada minijuego veras una explicacion. Fijate bien, juega y consigue " +
                        "monedas y estrellas. ¡Cuanto mejor lo hagas, mas estrellas!" },

            new Step {
                icon = "ESC", color = new Color(0.58f,0.45f,0.95f),
                title = "El menu de pausa",
                body  = "Pulsa la tecla ESC en cualquier momento para pausar. Ahi puedes cambiar el " +
                        "volumen, ver tus Misiones diarias, cambiar de jugador o pedir ayuda con este tutorial." },

            new Step {
                icon = "P", color = new Color(0.30f,0.65f,1f),
                title = "Planeta Attentia y misiones",
                body  = "Es tu mapa: cada zona muestra tu progreso, tu racha de dias jugando y tus " +
                        "logros. La 'Mision de hoy' te sugiere las zonas que mas te conviene practicar." },

            new Step {
                icon = "PIN", color = KidUI.BAD,
                title = "Zona de adultos",
                body  = "Con el boton 'Adulto' (protegido por un PIN) un padre o profesor puede ver los " +
                        "informes del niño y descargarlos. Todos los datos se guardan solo en este " +
                        "ordenador; nunca se comparten." },

            new Step {
                icon = "GO", color = KidUI.GOOD,
                title = "¡Ya lo sabes todo!",
                body  = "Puedes volver a ver este tutorial cuando quieras desde el menu de pausa (tecla ESC), " +
                        "tocando el boton '?'. ¡Ahora despega y ayuda a los robots!" },
        };
    }

    // ------------------------------------------------ Construcción

    void Build()
    {
        BuildSteps();

        var cv = KidUI.MakeCanvas("TutorialCanvas", 950, transform);
        _canvasGO = cv.gameObject;
        _root = cv.GetComponent<RectTransform>();

        KidUI.BuildSpaceBackground(_root, withPlanet: true);

        _card = KidUI.RoundImg(_root, "Card", new Color(0.06f, 0.09f, 0.18f, 0.98f),
                               new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                               Vector2.zero, new Vector2(1150f, 720f), 0.7f);

        _step = 0;
        RenderStep();

        // Fondo opaco al instante (sin fundido de canvas) + entrada animada de la tarjeta.
        UITween.PopIn(_card, 0.3f, 0.9f);
    }

    void RenderStep()
    {
        foreach (Transform t in _card) Destroy(t.gameObject);
        var s = _steps[_step];

        // Etiqueta + contador
        var tag = KidUI.Txt(_card, "Tag", "COMO SE JUEGA   ·   PASO " + (_step + 1) + " DE " + _steps.Length,
                            KidUI.DIM, 18, new Vector2(0.05f, 0.895f), new Vector2(0.95f, 0.965f));
        tag.characterSpacing = 3f;

        // Puntos de progreso
        var dotsRow = KidUI.Img(_card, "Dots", Color.clear,
                                new Vector2(0.30f, 0.83f), new Vector2(0.70f, 0.885f),
                                Vector2.zero, Vector2.zero);
        float step = 1f / _steps.Length;
        for (int i = 0; i < _steps.Length; i++)
        {
            float cx = step * (i + 0.5f);
            var d = KidUI.CircleAt(dotsRow, "d" + i,
                i == _step ? s.color : new Color(1f, 1f, 1f, 0.20f),
                new Vector2(cx, 0.5f), i == _step ? 20f : 12f);
            d.GetComponent<Image>().raycastTarget = false;
        }

        // Icono grande (círculo de color con símbolo/letra)
        var halo = KidUI.CircleAt(_card, "Halo", new Color(s.color.r, s.color.g, s.color.b, 0.18f),
                                  new Vector2(0.5f, 0.685f), 210f);
        halo.GetComponent<Image>().raycastTarget = false;
        var disc = KidUI.CircleAt(_card, "Disc", new Color(0.10f, 0.14f, 0.26f, 1f),
                                  new Vector2(0.5f, 0.685f), 168f);
        disc.GetComponent<Image>().raycastTarget = false;
        var symT = KidUI.Txt(disc, "Sym", s.icon, s.color,
                             s.icon.Length > 3 ? 34 : 54, Vector2.zero, Vector2.one);
        symT.fontStyle = FontStyles.Bold;
        UITween.PopIn(disc, 0.35f, 0.7f);

        // Título
        var title = KidUI.Txt(_card, "Title", s.title, Color.white, 40,
                              new Vector2(0.06f, 0.44f), new Vector2(0.94f, 0.55f));
        title.fontStyle = FontStyles.Bold;

        // Cuerpo
        var body = KidUI.Txt(_card, "Body", s.body, new Color(0.86f, 0.91f, 1f), 25,
                             new Vector2(0.10f, 0.20f), new Vector2(0.90f, 0.43f));
        body.alignment = TextAlignmentOptions.Top;
        body.enableWordWrapping = true;

        // Botones
        KidUI.Btn(_card, "Saltar", KidUI.BTNC,
                  new Vector2(0.06f, 0.05f), new Vector2(0.24f, 0.135f), Close, 18f);

        if (_step > 0)
            KidUI.Btn(_card, "Atras", KidUI.BTNC,
                      new Vector2(0.39f, 0.05f), new Vector2(0.57f, 0.135f), Prev, 18f);

        bool last = _step == _steps.Length - 1;
        KidUI.Btn(_card, last ? "¡Empezar!" : "Siguiente",
                  last ? KidUI.GOOD : KidUI.ACCENT,
                  new Vector2(0.62f, 0.05f), new Vector2(0.94f, 0.135f), Next, 22f);
    }

    // ------------------------------------------------ Navegación

    void Next()
    {
        if (UIAudioManager.Instance != null) UIAudioManager.Instance.PlayClick();
        if (_step >= _steps.Length - 1) { Close(); return; }
        _step++;
        RenderStep();
    }

    void Prev()
    {
        if (_step <= 0) return;
        if (UIAudioManager.Instance != null) UIAudioManager.Instance.PlayClick();
        _step--;
        RenderStep();
    }

    void Close()
    {
        MarkSeen();
        var go = gameObject;
        if (_canvasGO != null)
            UITween.FadeOut(_canvasGO, 0.2f, () => { if (go != null) Destroy(go); });
        else
            Destroy(go);
    }

    void Update()
    {
        // Teclado (evitamos Espacio/Enter para no interferir con los minijuegos de detrás).
        if (Input.GetKeyDown(KeyCode.RightArrow)) Next();
        else if (Input.GetKeyDown(KeyCode.LeftArrow)) Prev();
        else if (Input.GetKeyDown(KeyCode.Escape)) Close();
    }
}

/// <summary>Lanzador que espera a que la primera escena esté lista y muestra el tutorial.</summary>
public class TutorialAutoLauncher : MonoBehaviour
{
    IEnumerator Start()
    {
        // Deja que se creen EventSystem, ProfileManager y la pantalla inicial.
        yield return new WaitForSecondsRealtime(0.6f);
        TutorialScreen.ShowIfFirstTime();
        Destroy(gameObject);
    }
}
