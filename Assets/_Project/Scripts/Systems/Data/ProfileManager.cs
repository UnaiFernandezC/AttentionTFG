// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Gestor de perfiles. Singleton DontDestroyOnLoad auto-creado (sin tocar escenas,
/// mismo patrón que SceneTransition). Mantiene el perfil activo durante la sesión,
/// expone el IDataStore y muestra la pantalla de selección de perfil al arrancar
/// (y siempre que se vuelva a la pantalla inicial sin perfil activo).
/// Modo invitado: ActiveProfile == null → no se persiste nada.
/// </summary>
public class ProfileManager : MonoBehaviour
{
    public static ProfileManager Instance { get; private set; }

    /// <summary>Acceso global a la persistencia (JSON detrás de IDataStore).</summary>
    public static IDataStore Store => Instance != null ? Instance._store : null;

    public ProfileData ActiveProfile { get; private set; }
    public bool HasActiveProfile => ActiveProfile != null;

    /// <summary>True mientras el niño eligió "jugar sin guardar" en esta ejecución.</summary>
    public bool GuestMode { get; private set; }

    IDataStore _store;
    bool _gateShownOnce;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        if (Instance != null) return;
        var go = new GameObject("ProfileManager");
        go.AddComponent<ProfileManager>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        _store = new JsonDataStore();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        if (Instance == this) SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != SceneLoader.MAIN_MENU) return;

        // Primerísimo arranque: vídeo de presentación ANTES de todo.
        if (IntroVideoScreen.ShouldShow)
        {
            IntroVideoScreen.Show(onFinished: AfterIntroVideo);
            return;
        }
        AfterIntroVideo();
    }

    /// <summary>Flujo tras el vídeo de presentación (o si ya se vio):
    /// consentimiento parental primero, luego el enrutado normal.</summary>
    void AfterIntroVideo()
    {
        if (!ConsentGiven)
        {
            ConsentScreen.Show(onAccepted: RouteFromMainMenu);
            return;
        }
        RouteFromMainMenu();
    }

    /// <summary>Pantalla inicial: sin perfil → selector de perfiles; con perfil → el HUB
    /// (Planeta Attentia) ES el menú principal.</summary>
    void RouteFromMainMenu()
    {
        if (!HasActiveProfile && !GuestMode)
            ShowProfileGate();
        else if (HasActiveProfile)
            ProgressMapScreen.Show();
    }

    void Start()
    {
        // Por si la escena inicial ya estaba cargada antes de suscribirnos.
        if (_gateShownOnce || SceneManager.GetActiveScene().name != SceneLoader.MAIN_MENU)
            return;
        if (IntroVideoScreen.IsOpen) return;   // el vídeo ya está en marcha
        if (IntroVideoScreen.ShouldShow)
        {
            IntroVideoScreen.Show(onFinished: AfterIntroVideo);
            return;
        }
        if (!ConsentGiven)
        {
            ConsentScreen.Show(onAccepted: RouteFromMainMenu);
            return;
        }
        if (!HasActiveProfile && !GuestMode)
            ShowProfileGate();
    }

    void ShowProfileGate()
    {
        _gateShownOnce = true;
        ProfileScreenController.Show();
    }

    // ------------------------------------------------ API de perfiles

    public List<ProfileData> GetProfiles() => _store.GetAllProfiles();

    public ProfileData CreateProfile(string nombre, string avatarId, int edadTramo)
    {
        long now = DataUtils.NowTicks();
        var p = new ProfileData
        {
            id = DataUtils.NewId(),
            nombre = string.IsNullOrWhiteSpace(nombre) ? "Jugador" : nombre.Trim(),
            avatarId = avatarId,
            edadTramo = Mathf.Clamp(edadTramo, 0, 2),
            fechaCreacionUtcTicks = now,
            fechaCreacionUtc = DataUtils.TicksToIso(now)
        };
        _store.SaveProfile(p);
        return p;
    }

    /// <summary>Activa un perfil: fija su dificultad (elegida o recomendada por edad)
    /// y abre sesión de telemetría. La dificultad NO se vuelve a preguntar.</summary>
    public void SelectProfile(ProfileData profile)
    {
        if (profile == null) return;
        // Cierra la sesión anterior si la hubiera.
        if (TelemetryManager.Instance != null) TelemetryManager.Instance.EndSession();

        ActiveProfile = profile;
        GuestMode = false;

        if (GameManager.Instance != null)
            GameManager.Instance.SetDifficulty(profile.DificultadActiva);

        if (TelemetryManager.Instance != null)
            TelemetryManager.Instance.StartSession(profile);

        Debug.Log($"[ProfileManager] Perfil activo: {profile.nombre} ({profile.EdadTramoLabel}, " +
                  $"dificultad {profile.DificultadActiva})");
    }

    /// <summary>
    /// Guarda en el perfil activo un cambio manual de dificultad (hecho desde el
    /// selector accesible en el menú ESC). Así se recuerda en próximas sesiones.
    /// </summary>
    public void PersistDifficulty(DifficultyLevel d)
    {
        if (!HasActiveProfile) return;
        if (ActiveProfile.dificultad == (int)d) return;
        ActiveProfile.dificultad = (int)d;
        _store.SaveProfile(ActiveProfile);
        Debug.Log($"[ProfileManager] Dificultad de {ActiveProfile.nombre} guardada: {d}");
    }

    /// <summary>Modo invitado: se juega sin persistir datos.</summary>
    public void PlayAsGuest()
    {
        if (TelemetryManager.Instance != null) TelemetryManager.Instance.EndSession();
        ActiveProfile = null;
        GuestMode = true;
        Debug.Log("[ProfileManager] Modo invitado (no se guardan datos).");
    }

    /// <summary>Cierra sesión y vuelve a la pantalla inicial mostrando el selector de perfiles.</summary>
    public void SwitchProfile()
    {
        if (TelemetryManager.Instance != null) TelemetryManager.Instance.EndSession();
        ActiveProfile = null;
        GuestMode = false;
        Time.timeScale = 1f;
        SceneLoader.LoadScene(SceneLoader.MAIN_MENU);
    }

    /// <summary>Borra TODOS los datos de un perfil (derecho de supresión / privacidad).</summary>
    public void DeleteProfileData(string profileId)
    {
        if (ActiveProfile != null && ActiveProfile.id == profileId)
        {
            if (TelemetryManager.Instance != null) TelemetryManager.Instance.EndSession();
            ActiveProfile = null;
        }
        _store.DeleteProfile(profileId);
    }

    /// <summary>Borra TODA la base de datos (todos los perfiles y sus datos).
    /// Cierra la sesión activa. Mantiene el PIN del tutor.</summary>
    public void DeleteAllData()
    {
        if (TelemetryManager.Instance != null) TelemetryManager.Instance.EndSession();
        ActiveProfile = null;
        GuestMode = false;
        _store.DeleteAllData();
    }

    // ------------------------------------------------ Modo profesional y consentimiento

    /// <summary>Modo gabinete: perfiles ilimitados, búsqueda y exportación por lote.</summary>
    public bool ProfessionalMode => _store != null && _store.GetProfessionalMode();

    public void SetProfessionalMode(bool enabled) => _store?.SetProfessionalMode(enabled);

    /// <summary>True si un adulto ya aceptó la versión vigente de la política.</summary>
    public bool ConsentGiven =>
        _store != null && _store.GetConsentVersion() == ConsentScreen.POLICY_VERSION;

    public void GrantConsent() => _store?.SetConsentVersion(ConsentScreen.POLICY_VERSION);

    // ------------------------------------------------ PIN del tutor

    public bool HasTutorPin => !string.IsNullOrEmpty(_store.GetTutorPinHash());

    public bool VerifyTutorPin(string pin) =>
        HasTutorPin && _store.GetTutorPinHash() == DataUtils.HashPin(pin);

    public void SetTutorPin(string pin) => _store.SetTutorPinHash(DataUtils.HashPin(pin));
}
