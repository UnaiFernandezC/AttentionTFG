using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Gestor de audio global para la aplicación.
/// Maneja música de fondo en bucle y sonido de clic para todos los botones.
///
/// SETUP:
/// 1. Crea un GameObject vacío llamado "UIAudioManager" en la primera escena.
/// 2. Añade este script al GameObject.
/// 3. Arrastra tu MP3 de música al campo "Background Music" en el Inspector.
/// 4. Arrastra un AudioClip corto de clic al campo "Click Sound".
/// 5. El GameObject sobrevive entre escenas (DontDestroyOnLoad).
///
/// El sonido de clic se dispara automáticamente en cualquier Button.onClick
/// detectado en la escena activa.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class UIAudioManager : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────
    [Header("Musica de fondo")]
    [SerializeField] AudioClip backgroundMusic;
    [Range(0f, 1f)]
    [SerializeField] float musicVolume = 0.55f;
    [SerializeField] float fadeInDuration = 1.5f;

    [Header("Sonido de clic")]
    [SerializeField] AudioClip clickSound;
    [Range(0f, 1f)]
    [SerializeField] float clickVolume = 0.75f;

    // ── Singleton ─────────────────────────────────────────────────────────────
    public static UIAudioManager Instance { get; private set; }

    AudioSource _musicSource;
    AudioSource _sfxSource;

    // ── Propiedades publicas ──────────────────────────────────────────────────
    public float MusicVolume
    {
        get => musicVolume;
        set
        {
            musicVolume = Mathf.Clamp01(value);
            if (_musicSource) _musicSource.volume = musicVolume;
        }
    }

    public float ClickVolume
    {
        get => clickVolume;
        set { clickVolume = Mathf.Clamp01(value); }
    }

    public bool MusicEnabled
    {
        get => _musicSource && _musicSource.isPlaying;
        set
        {
            if (_musicSource == null) return;
            if (value)
            {
                if (!_musicSource.isPlaying)
                    _musicSource.Play();
            }
            else
            {
                _musicSource.Pause();
            }
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Unity lifecycle
    // ═════════════════════════════════════════════════════════════════════════

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // AudioSource principal → música
        _musicSource = GetComponent<AudioSource>();
        _musicSource.clip        = backgroundMusic;
        _musicSource.loop        = true;
        _musicSource.playOnAwake = false;
        _musicSource.volume      = 0f;

        // AudioSource secundario → SFX
        _sfxSource = gameObject.AddComponent<AudioSource>();
        _sfxSource.playOnAwake = false;
        _sfxSource.loop        = false;
    }

    void Start()
    {
        if (backgroundMusic != null)
        {
            _musicSource.Play();
            StartCoroutine(FadeIn());
        }

        // Generar el clic por codigo si no hay clip asignado
        if (clickSound == null)
            clickSound = GenerateClickClip();

        // Suscribirse a cambios de escena para re-registrar botones
        SceneManager.sceneLoaded += OnSceneLoaded;
        RegisterAllButtons();
    }

    // ── Generacion procedural del sonido de clic ──────────────────────────────
    // Produce un "tick" limpio y suave: tono corto a 1200 Hz con envelope rapido.
    static AudioClip GenerateClickClip()
    {
        const int   sampleRate  = 44100;
        const float duration    = 0.055f;          // 55 ms
        const float frequency   = 1200f;           // tono agudo
        const float attackTime  = 0.003f;          // 3 ms attack
        const float decayRatio  = 0.85f;           // decay suave

        int samples = Mathf.RoundToInt(sampleRate * duration);
        float[] data = new float[samples];

        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / sampleRate;

            // Onda senoidal
            float wave = Mathf.Sin(2f * Mathf.PI * frequency * t);

            // Envelope: attack rapido + decay exponencial
            float attack  = Mathf.Clamp01(t / attackTime);
            float decay   = Mathf.Pow(decayRatio, t * sampleRate / 512f);
            float envelope = attack * decay;

            // Pequena componente de ruido para dar cuerpo
            float noise = (UnityEngine.Random.value * 2f - 1f) * 0.15f;

            data[i] = (wave * 0.85f + noise) * envelope;
        }

        var clip = AudioClip.Create("ClickGenerated", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Registro de botones
    // ═════════════════════════════════════════════════════════════════════════

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Pequeño delay para que la escena termine de construir su UI
        StartCoroutine(RegisterButtonsDelayed());
    }

    System.Collections.IEnumerator RegisterButtonsDelayed()
    {
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        RegisterAllButtons();
    }

    void RegisterAllButtons()
    {
        // Registra el sonido de clic en TODOS los Button activos de la escena
        var buttons = FindObjectsOfType<Button>(includeInactive: false);
        foreach (var btn in buttons)
        {
            // Evitar registrar dos veces usando un tag de listener
            btn.onClick.RemoveListener(PlayClick);
            btn.onClick.AddListener(PlayClick);
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Audio
    // ═════════════════════════════════════════════════════════════════════════

    public void PlayClick()
    {
        if (clickSound == null || _sfxSource == null) return;
        _sfxSource.PlayOneShot(clickSound, clickVolume);
    }

    System.Collections.IEnumerator FadeIn()
    {
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            _musicSource.volume = Mathf.Lerp(0f, musicVolume, elapsed / fadeInDuration);
            yield return null;
        }
        _musicSource.volume = musicVolume;
    }

    public void StopMusic()
    {
        if (_musicSource) _musicSource.Stop();
    }

    public void SetMusicVolume(float v)  => MusicVolume  = v;
    public void SetClickVolume(float v)  => ClickVolume  = v;
}
