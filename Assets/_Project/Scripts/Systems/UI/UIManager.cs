using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

/// <summary>
/// Gestor de UI global. Singleton que persiste entre escenas.
/// Gestiona el fade de pantalla, overlays de puntuación y mensajes de estado.
/// Adjunta este componente a un Canvas persistente en la escena GameManager
/// o en cualquier escena principal.
/// </summary>
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

    // ------------------------------------------------------------------ //
    // Lifecycle
    // ------------------------------------------------------------------ //

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Asegurarse de que el fade empieza transparente
        if (fadePanel != null)
            SetFadeAlpha(0f);
    }

    // ------------------------------------------------------------------ //
    // Fade de pantalla
    // ------------------------------------------------------------------ //

    /// <summary>Hace fade a negro y luego carga la escena indicada.</summary>
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

    // ------------------------------------------------------------------ //
    // Puntuación
    // ------------------------------------------------------------------ //

    public void UpdateScoreDisplay(int score)
    {
        if (scoreText != null)
            scoreText.text = score.ToString();
    }

    // ------------------------------------------------------------------ //
    // Mensajes de estado
    // ------------------------------------------------------------------ //

    /// <summary>Muestra un mensaje temporal en pantalla.</summary>
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
