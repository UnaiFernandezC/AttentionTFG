using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(AudioSource))]
public class UIAudioManager : MonoBehaviour
{

    [Header("Musica de fondo")]
    [SerializeField] AudioClip backgroundMusic;
    [Range(0f, 1f)]
    [SerializeField] float musicVolume = 0.55f;
    [SerializeField] float fadeInDuration = 1.5f;

    [Header("Sonido de clic")]
    [SerializeField] AudioClip clickSound;
    [Range(0f, 1f)]
    [SerializeField] float clickVolume = 0.75f;

    public static UIAudioManager Instance { get; private set; }

    AudioSource _musicSource;
    AudioSource _sfxSource;

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

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _musicSource = GetComponent<AudioSource>();
        _musicSource.clip        = backgroundMusic;
        _musicSource.loop        = true;
        _musicSource.playOnAwake = false;
        _musicSource.volume      = 0f;

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

        if (clickSound == null)
            clickSound = GenerateClickClip();

        SceneManager.sceneLoaded += OnSceneLoaded;
        RegisterAllButtons();
    }

    static AudioClip GenerateClickClip()
    {
        const int   sampleRate  = 44100;
        const float duration    = 0.055f;
        const float frequency   = 1200f;
        const float attackTime  = 0.003f;
        const float decayRatio  = 0.85f;

        int samples = Mathf.RoundToInt(sampleRate * duration);
        float[] data = new float[samples];

        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / sampleRate;

            float wave = Mathf.Sin(2f * Mathf.PI * frequency * t);

            float attack  = Mathf.Clamp01(t / attackTime);
            float decay   = Mathf.Pow(decayRatio, t * sampleRate / 512f);
            float envelope = attack * decay;

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

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Reiniciar música al volver a escenas de menú/selector
        if (!_musicSource.isPlaying && backgroundMusic != null)
        {
            string n = scene.name;
            bool isMinigame = n.Contains("_Easy") || n.Contains("_Medium") || n.Contains("_Hard")
                              || n.Contains("_Facil") || n.Contains("_Medio") || n.Contains("_Dificil");
            if (!isMinigame)
            {
                _musicSource.volume = 0f;
                _musicSource.Play();
                StartCoroutine(FadeIn());
            }
        }

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

        var buttons = FindObjectsOfType<Button>(includeInactive: false);
        foreach (var btn in buttons)
        {

            btn.onClick.RemoveListener(PlayClick);
            btn.onClick.AddListener(PlayClick);
        }
    }

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
