// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using UnityEngine;

/// <summary>
/// Telemetría de juego. Singleton DontDestroyOnLoad auto-creado.
/// - Abre una Session cuando ProfileManager activa un perfil y la cierra al salir,
///   al cambiar de jugador o al cerrar la aplicación.
/// - Registra un MinigameResult al terminar cada minijuego (hooks en MinigameBase).
/// - API opcional por ronda (NotifyRound) para aciertos/errores/tiempos de reacción:
///   los minijuegos que no la usan siguen funcionando con valores por defecto.
/// - Si no hay perfil activo (modo invitado) no se persiste nada.
/// La sesión actualiza su hora de fin tras cada resultado → robusto ante cierres bruscos.
/// </summary>
public class TelemetryManager : MonoBehaviour
{
    public static TelemetryManager Instance { get; private set; }

    SessionData _session;

    // Métricas del minijuego en curso
    bool   _minigameRunning;
    string _mgName;
    int    _mgCategory;
    float  _mgStartRealtime;
    int    _mgAciertos;
    int    _mgErrores;
    float  _mgRtSumMs;
    int    _mgRtCount;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        if (Instance != null) return;
        var go = new GameObject("TelemetryManager");
        go.AddComponent<TelemetryManager>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Application.quitting += OnAppQuitting;
    }

    void OnAppQuitting() => EndSession();

    // ------------------------------------------------ Sesiones

    public void StartSession(ProfileData profile)
    {
        if (profile == null) return;
        EndSession();

        long now = DataUtils.NowTicks();
        _session = new SessionData
        {
            id = DataUtils.NewId(),
            profileId = profile.id,
            inicioUtcTicks = now,
            inicioUtc = DataUtils.TicksToIso(now),
            finUtcTicks = now,
            finUtc = DataUtils.TicksToIso(now),
            dificultad = GameManager.Instance != null
                ? (int)GameManager.Instance.CurrentDifficulty
                : (int)profile.DificultadRecomendada
        };
        ProfileManager.Store?.AddSession(_session);
        Debug.Log($"[Telemetry] Sesión iniciada para {profile.nombre}");
    }

    public void EndSession()
    {
        if (_session == null) return;
        TouchSession();
        Debug.Log("[Telemetry] Sesión cerrada.");
        _session = null;
        _minigameRunning = false;
    }

    void TouchSession()
    {
        if (_session == null) return;
        long now = DataUtils.NowTicks();
        _session.finUtcTicks = now;
        _session.finUtc = DataUtils.TicksToIso(now);
        ProfileManager.Store?.UpdateSession(_session);
    }

    // ------------------------------------------------ Hooks estáticos (null-safe)

    public static void NotifyMinigameStarted(string name, MinigameCategory category)
    {
        if (Instance == null) return;
        Instance._minigameRunning  = true;
        Instance._mgName           = name;
        Instance._mgCategory       = (int)category;
        Instance._mgStartRealtime  = Time.realtimeSinceStartup;
        Instance._mgAciertos       = 0;
        Instance._mgErrores        = 0;
        Instance._mgRtSumMs        = 0f;
        Instance._mgRtCount        = 0;
    }

    /// <summary>Evento por ronda: acierto/fallo y, si aplica, tiempo de reacción en ms.</summary>
    public static void NotifyRound(bool acierto, float tiempoReaccionMs = -1f)
    {
        if (Instance == null || !Instance._minigameRunning) return;
        if (acierto) Instance._mgAciertos++; else Instance._mgErrores++;
        if (tiempoReaccionMs > 0f)
        {
            Instance._mgRtSumMs += tiempoReaccionMs;
            Instance._mgRtCount++;
        }
    }

    public static void NotifyMinigameEnded(string name, MinigameCategory category,
                                           int puntuacion, bool completado)
    {
        if (Instance == null) return;
        Instance.RecordResult(name, category, puntuacion, completado);
    }

    // ------------------------------------------------ Registro

    void RecordResult(string name, MinigameCategory category, int puntuacion, bool completado)
    {
        bool wasRunning = _minigameRunning;
        _minigameRunning = false;

        var pm = ProfileManager.Instance;
        if (pm == null || !pm.HasActiveProfile || ProfileManager.Store == null)
            return; // modo invitado o sin sistema de datos: no se persiste

        // Sesión de respaldo por si se llegó aquí sin pasar por el selector de perfil.
        if (_session == null) StartSession(pm.ActiveProfile);
        if (_session == null) return;

        float duracion = wasRunning
            ? Mathf.Max(0f, Time.realtimeSinceStartup - _mgStartRealtime)
            : 0f;

        int monedas = 0;
        var cm = CoinManager.instance;
        if (cm != null) monedas = cm.coinCount;

        long now = DataUtils.NowTicks();
        var result = new MinigameResultData
        {
            id = DataUtils.NewId(),
            profileId = pm.ActiveProfile.id,
            sessionId = _session.id,
            minijuego = string.IsNullOrEmpty(name) ? "Minijuego" : name,
            categoria = (int)category,
            dificultad = GameManager.Instance != null
                ? (int)GameManager.Instance.CurrentDifficulty
                : _session.dificultad,
            fechaUtcTicks = now,
            fechaUtc = DataUtils.TicksToIso(now),
            duracionSeg = duracion,
            aciertos = _mgAciertos,
            errores = _mgErrores,
            puntuacion = puntuacion,
            tiempoReaccionMedioMs = _mgRtCount > 0 ? _mgRtSumMs / _mgRtCount : -1f,
            completado = completado,
            monedas = monedas
        };

        ProfileManager.Store.AddResult(result);
        TouchSession();
        Debug.Log($"[Telemetry] Resultado guardado: {result.minijuego} " +
                  $"({(completado ? "completado" : "fallado")}, {puntuacion} pts, " +
                  $"{_mgAciertos}A/{_mgErrores}E)");
    }
}
