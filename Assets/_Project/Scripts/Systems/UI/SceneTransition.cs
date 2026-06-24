using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SceneTransition : MonoBehaviour
{
    static SceneTransition _instance;

    CanvasGroup _cg;
    bool _busy;

    [SerializeField] float fadeInDuration  = 0.45f;
    [SerializeField] float fadeOutDuration = 0.32f;

    static readonly Color FADE_COLOR = new Color(0.02f, 0.03f, 0.08f, 1f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        if (_instance != null) return;
        var go = new GameObject("SceneTransition");
        _instance = go.AddComponent<SceneTransition>();
        DontDestroyOnLoad(go);
    }

    void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        BuildOverlay();
        SceneManager.sceneLoaded += OnSceneLoaded;

        _cg.alpha = 1f;
        _cg.blocksRaycasts = true;
        StartCoroutine(FadeTo(0f, fadeInDuration));
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void BuildOverlay()
    {
        var cvGO = new GameObject("TransitionCanvas");
        cvGO.transform.SetParent(transform, false);
        var cv = cvGO.AddComponent<Canvas>();
        cv.renderMode   = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 32760;
        cvGO.AddComponent<GraphicRaycaster>();
        _cg = cvGO.AddComponent<CanvasGroup>();

        var panel = new GameObject("Fade");
        panel.transform.SetParent(cvGO.transform, false);
        var rt = panel.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero; rt.anchoredPosition = Vector2.zero;
        panel.AddComponent<Image>().color = FADE_COLOR;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (_cg == null) return;
        StopAllCoroutines();
        _cg.alpha = 1f;
        _cg.blocksRaycasts = true;
        StartCoroutine(FadeTo(0f, fadeInDuration));
    }

    public static void LoadScene(string sceneName)
    {
        if (_instance == null || _instance._cg == null)
        {
            SceneManager.LoadScene(sceneName);
            return;
        }
        if (_instance._busy) return;
        _instance.StartCoroutine(_instance.FadeAndLoad(sceneName));
    }

    IEnumerator FadeAndLoad(string sceneName)
    {
        _busy = true;
        _cg.blocksRaycasts = true;
        yield return FadeTo(1f, fadeOutDuration);
        _busy = false;
        SceneManager.LoadScene(sceneName);
    }

    IEnumerator FadeTo(float target, float duration)
    {
        float start = _cg.alpha;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            _cg.alpha = Mathf.Lerp(start, target, t / duration);
            yield return null;
        }
        _cg.alpha = target;
        _cg.blocksRaycasts = target > 0.01f;
    }
}
