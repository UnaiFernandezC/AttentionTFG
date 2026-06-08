using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class GameSelectorMusicManager : MonoBehaviour
{
    [Header("Music")]
    [SerializeField] private AudioClip backgroundMusic;

    [Range(0f, 1f)]
    [SerializeField] private float volume = 0.6f;

    [Header("Fade")]
    [SerializeField] private float fadeInDuration = 1.5f;

    private AudioSource _audioSource;
    private static GameSelectorMusicManager _instance;

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        _audioSource = GetComponent<AudioSource>();
        _audioSource.clip = backgroundMusic;
        _audioSource.loop = true;
        _audioSource.playOnAwake = false;
        _audioSource.volume = 0f;
    }

    void Start()
    {
        if (backgroundMusic == null)
        {
            Debug.LogWarning("GameSelectorMusicManager: no hay ningún AudioClip asignado. " +
                             "Arrastra tu MP3 al campo 'Background Music' en el Inspector.");
            return;
        }

        _audioSource.Play();
        StartCoroutine(FadeIn());
    }

    private System.Collections.IEnumerator FadeIn()
    {
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            _audioSource.volume = Mathf.Lerp(0f, volume, elapsed / fadeInDuration);
            yield return null;
        }
        _audioSource.volume = volume;
    }

    public static void StopMusic()
    {
        if (_instance != null)
            _instance._audioSource.Stop();
    }

    public static void DestroyInstance()
    {
        if (_instance != null)
        {
            Destroy(_instance.gameObject);
            _instance = null;
        }
    }
}
