// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Fade")]
    [SerializeField] private Image fadePanel;
    [SerializeField] private float defaultFadeDuration = 0.4f;

    [Header("Puntuación global (opcional)")]
    [SerializeField] private TextMeshProUGUI scoreText;

    [Header("Mensaje de estado (opcional)")]
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private float statusDisplayDuration = 2f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (fadePanel != null)
            SetFadeAlpha(0f);
    }

    public void FadeAndLoadScene(string sceneName, float duration = -1f)
    {
        float d = duration < 0 ? defaultFadeDuration : duration;
        StartCoroutine(FadeAndLoadRoutine(sceneName, d));
    }

    private IEnumerator FadeAndLoadRoutine(string sceneName, float duration)
    {
        yield return FadeRoutine(0f, 1f, duration);
        SceneLoader.LoadScene(sceneName);
        yield return FadeRoutine(1f, 0f, duration);
    }

    private IEnumerator FadeRoutine(float from, float to, float duration)
    {
        if (fadePanel == null) yield break;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            SetFadeAlpha(Mathf.Lerp(from, to, elapsed / duration));
            yield return null;
        }
        SetFadeAlpha(to);
    }

    private void SetFadeAlpha(float alpha)
    {
        if (fadePanel == null) return;
        Color c = fadePanel.color;
        c.a = alpha;
        fadePanel.color = c;
        fadePanel.gameObject.SetActive(alpha > 0f);
    }

    public void UpdateScoreDisplay(int score)
    {
        if (scoreText != null)
            scoreText.text = score.ToString();
    }

    public void ShowStatus(string message)
    {
        if (statusText == null) return;
        StopCoroutine(nameof(HideStatusRoutine));
        statusText.text = message;
        statusText.gameObject.SetActive(true);
        StartCoroutine(HideStatusRoutine());
    }

    private IEnumerator HideStatusRoutine()
    {
        yield return new WaitForSeconds(statusDisplayDuration);
        if (statusText != null)
            statusText.gameObject.SetActive(false);
    }
}
