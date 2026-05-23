using UnityEngine;

/// <summary>
/// Manages looping background music on GameSelector and EscenasEleccion screens.
///
/// SETUP:
/// 1. Place this script on a new empty GameObject called "MusicManager" in each
///    GameSelector scene (GameSelector, GameSelector 1, GameSelector 2).
/// 2. Assign your MP3 file to the "Background Music" field in the Inspector.
/// 3. The music will play automatically on loop, persist across EscenasEleccion
///    sub-scenes, and stop when leaving the GameSelector flow.
///
/// MP3 IMPORT: drag your .mp3 into Assets/_Project/Audio/, then drag the resulting
/// AudioClip asset into the "Background Music" slot on this component.
/// </summary>
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

    /// <summary>
    /// Call this from any minigame scene to stop the music when the player
    /// actually starts playing (optional — music persists by default).
    /// </summary>
    public static void StopMusic()
    {
        if (_instance != null)
            _instance._audioSource.Stop();
    }

    /// <summary>
    /// Destroys the music manager when leaving the GameSelector flow entirely
    /// (e.g. returning to MainMenu). Call from MainMenu's Awake/Start.
    /// </summary>
    public static void DestroyInstance()
    {
        if (_instance != null)
        {
            Destroy(_instance.gameObject);
            _instance = null;
        }
    }
}
