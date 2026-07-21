// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;

/// <summary>
/// Vídeo de presentación del PRIMER arranque de la aplicación (AtenttionAD).
/// Se reproduce a pantalla completa ANTES del consentimiento parental; al
/// terminar (o al pulsar "Saltar") continúa el flujo normal (cookies/consent).
/// El vídeo vive en StreamingAssets/AtenttionAD.mp4 (se incluye en la build).
/// Solo se muestra una vez por instalación (PlayerPrefs); si el archivo falta
/// o falla la reproducción, se salta sin bloquear la app.
/// </summary>
public class IntroVideoScreen : MonoBehaviour
{
    // v2: se renombró la clave para que el vídeo se reproduzca de nuevo tras
    // el arreglo de audio (a quien ya lo vio le contará como no visto una vez).
    const string PREFS_SEEN = "intro_video_visto_v2";
    const string FILE_NAME  = "AtenttionAD.mp4";

    static IntroVideoScreen _current;

    /// <summary>True si aún no se ha visto el vídeo de presentación.</summary>
    public static bool ShouldShow => PlayerPrefs.GetInt(PREFS_SEEN, 0) == 0;

    public static bool IsOpen => _current != null;

    System.Action _onFinished;
    VideoPlayer   _player;
    RenderTexture _rt;
    bool          _done;

    public static void Show(System.Action onFinished)
    {
        if (_current != null) return;
        KidUI.EnsureEventSystem();
        var go = new GameObject("IntroVideo");
        _current = go.AddComponent<IntroVideoScreen>();
        _current._onFinished = onFinished;
        _current.Build();
    }

    void OnDestroy()
    {
        if (_current == this) _current = null;
        if (_rt != null) { _rt.Release(); Destroy(_rt); }
    }

    void Build()
    {
        var cv = KidUI.MakeCanvas("IntroVideoCanvas", 2000, transform);
        var root = cv.GetComponent<RectTransform>();

        // Fondo negro opaco (tapa todo lo de detrás y bloquea clics)
        var bg = KidUI.Img(root, "BG", Color.black,
                           Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        bg.GetComponent<Image>().raycastTarget = true;

        string path = Path.Combine(Application.streamingAssetsPath, FILE_NAME);

        // Si el vídeo no existe (p. ej. build sin el archivo), seguimos sin drama
        bool fileOk = true;
#if !UNITY_ANDROID
        fileOk = File.Exists(path);
#endif
        if (!fileOk)
        {
            Debug.LogWarning("[IntroVideo] No se encontró " + path + " — se salta el vídeo.");
            Finish();
            return;
        }

        // Lienzo del vídeo (RawImage + RenderTexture, respetando el aspecto 16:9)
        _rt = new RenderTexture(1920, 1080, 0);
        var vidGO = new GameObject("Video");
        vidGO.transform.SetParent(root, false);
        var vrt = vidGO.AddComponent<RectTransform>();
        vrt.anchorMin = Vector2.zero; vrt.anchorMax = Vector2.one;
        vrt.sizeDelta = Vector2.zero;
        var raw = vidGO.AddComponent<RawImage>();
        raw.texture = _rt;
        raw.raycastTarget = false;
        var fitter = vidGO.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fitter.aspectRatio = 16f / 9f;

        // Audio por AudioSource (el modo Direct suena mal en Windows y además
        // ignora el volumen general de la aplicación).
        var audio = gameObject.AddComponent<AudioSource>();
        audio.playOnAwake = false;
        audio.volume      = 1f;

        _player = gameObject.AddComponent<VideoPlayer>();
        _player.playOnAwake      = false;
        _player.source           = VideoSource.Url;
        _player.url              = path;
        _player.renderMode       = VideoRenderMode.RenderTexture;
        _player.targetTexture    = _rt;
        _player.audioOutputMode  = VideoAudioOutputMode.AudioSource;
        _player.controlledAudioTrackCount = 1;
        _player.EnableAudioTrack(0, true);
        _player.SetTargetAudioSource(0, audio);
        _player.isLooping        = false;
        _player.loopPointReached += _ => Finish();       // fin del vídeo → continuar
        _player.errorReceived    += (_, msg) =>
        {
            Debug.LogWarning("[IntroVideo] Error de reproducción: " + msg);
            Finish();
        };
        _player.prepareCompleted += vp => vp.Play();
        _player.Prepare();

        // Botón "Saltar" discreto (padres/repeticiones de prueba)
        KidUI.Btn(root, "Saltar", new Color(1f, 1f, 1f, 0.10f),
                  new Vector2(0.88f, 0.04f), new Vector2(0.975f, 0.10f),
                  Finish, 16f);
    }

    void Finish()
    {
        if (_done) return;
        _done = true;

        PlayerPrefs.SetInt(PREFS_SEEN, 1);
        PlayerPrefs.Save();

        if (_player != null) _player.Stop();
        var cb = _onFinished;
        Destroy(gameObject);
        cb?.Invoke();
    }
}
