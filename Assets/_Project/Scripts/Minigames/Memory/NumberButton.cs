// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class NumberButton : MonoBehaviour
{

    public int  Number     { get; private set; }
    public bool IsComplete { get; private set; }

    public System.Action<NumberButton> OnClicked;

    private Image                      _bgImage;
    private TMPro.TextMeshProUGUI      _label;
    private Button                     _button;

    private static readonly Color C_IDLE    = new Color(0.20f, 0.22f, 0.40f);
    private static readonly Color C_IDLE_FG = new Color(0.88f, 0.90f, 1.00f);
    private static readonly Color C_CORRECT = new Color(0.22f, 0.78f, 0.50f);
    private static readonly Color C_WRONG   = new Color(0.90f, 0.28f, 0.35f);
    private static readonly Color C_DONE    = new Color(0.18f, 0.65f, 0.42f);
    private static readonly Color C_HOVER   = new Color(0.28f, 0.32f, 0.56f);

    private const float WRONG_FLASH_DURATION = 0.55f;

    public void Initialize(int number, Image bgImage, TMPro.TextMeshProUGUI label)
    {
        Number   = number;
        _bgImage = bgImage;
        _label   = label;
        _button  = GetComponent<Button>();

        _bgImage.color = C_IDLE;
        _label.text    = number.ToString();
        _label.color   = C_IDLE_FG;

        var cb = _button.colors;
        cb.normalColor      = Color.white;
        cb.highlightedColor = new Color(1f, 1f, 1f, 0.88f);
        cb.pressedColor     = new Color(0.80f, 0.80f, 0.80f);
        cb.selectedColor    = Color.white;
        cb.fadeDuration     = 0.06f;
        _button.colors = cb;

        _button.onClick.AddListener(HandleClick);
    }

    private void HandleClick()
    {
        if (IsComplete || !_button.interactable) return;
        OnClicked?.Invoke(this);
    }

    public void SetCorrect()
    {
        IsComplete = true;
        _button.interactable = false;
        StopAllCoroutines();
        StartCoroutine(CorrectPulse());
    }

    public void FlashWrong()
    {
        StopAllCoroutines();
        StartCoroutine(WrongFlash());
    }

    /// <summary>Numero que ya viene colocado: se muestra resuelto sin animacion.</summary>
    public void SetPrePlaced()
    {
        StopAllCoroutines();
        IsComplete = true;
        _button.interactable = false;
        _bgImage.color = C_DONE;
        _label.color   = new Color(1f, 1f, 1f, 0.85f);
        transform.localScale = Vector3.one;
    }

    public void ResetState()
    {
        StopAllCoroutines();
        IsComplete = false;
        _button.interactable = true;
        _bgImage.color = C_IDLE;
        _label.color   = C_IDLE_FG;
        transform.localScale = Vector3.one;
    }

    private IEnumerator CorrectPulse()
    {
        _bgImage.color = C_CORRECT;
        _label.color   = Color.white;

        float duration = 0.28f;
        float elapsed  = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float s = 1f + 0.15f * Mathf.Sin(t * Mathf.PI);
            transform.localScale = Vector3.one * s;
            yield return null;
        }

        transform.localScale = Vector3.one;

        _bgImage.color = C_DONE;
    }

    private IEnumerator WrongFlash()
    {

        _bgImage.color = C_WRONG;
        _label.color   = Color.white;

        float shakeDuration = 0.20f;
        float elapsed       = 0f;
        Vector3 origin      = transform.localPosition;

        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / shakeDuration;
            float offset = Mathf.Sin(t * Mathf.PI * 6f) * 6f * (1f - t);
            transform.localPosition = origin + new Vector3(offset, 0f, 0f);
            yield return null;
        }
        transform.localPosition = origin;

        float fadeDuration = WRONG_FLASH_DURATION - shakeDuration;
        elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;
            _bgImage.color = Color.Lerp(C_WRONG, C_IDLE, t);
            _label.color   = Color.Lerp(Color.white, C_IDLE_FG, t);
            yield return null;
        }

        _bgImage.color = C_IDLE;
        _label.color   = C_IDLE_FG;
    }
}
