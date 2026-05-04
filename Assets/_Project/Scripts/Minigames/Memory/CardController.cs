using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class CardController : MonoBehaviour
{

    public int     ColorId    { get; private set; }
    public bool    IsRevealed { get; private set; }
    public bool    IsMatched  { get; private set; }

    public System.Action<CardController> OnCardClicked;

    private Image _backImage;
    private Image _frontImage;
    private Button _button;

    private static readonly Color BACK_COLOR    = new Color(0.22f, 0.22f, 0.38f);
    private static readonly Color MATCHED_TINT  = new Color(1f, 1f, 1f, 0.55f);
    private static readonly Color BACK_HOVER    = new Color(0.28f, 0.28f, 0.46f);

    private const float FLIP_HALF_DURATION = 0.12f;

    public void Initialize(int colorId, Color faceColor, Image backImg, Image frontImg)
    {
        ColorId     = colorId;
        _backImage  = backImg;
        _frontImage = frontImg;
        _button     = GetComponent<Button>();

        _frontImage.color = faceColor;

        _frontImage.gameObject.SetActive(false);
        _backImage.color = BACK_COLOR;

        _button.onClick.AddListener(HandleClick);

        var cb = _button.colors;
        cb.normalColor      = Color.white;
        cb.highlightedColor = new Color(1f, 1f, 1f, 0.9f);
        cb.pressedColor     = new Color(0.85f, 0.85f, 0.85f);
        cb.selectedColor    = Color.white;
        cb.fadeDuration     = 0.05f;
        _button.colors = cb;
    }

    private void HandleClick()
    {
        if (IsRevealed || IsMatched) return;
        OnCardClicked?.Invoke(this);
    }

    public void FlipReveal()
    {
        if (IsRevealed || IsMatched) return;
        IsRevealed = true;
        _button.interactable = false;
        StartCoroutine(FlipAnimation(reveal: true));
    }

    public void FlipHide()
    {
        IsRevealed = false;
        _button.interactable = true;
        StartCoroutine(FlipAnimation(reveal: false));
    }

    public void SetMatched()
    {
        IsMatched = true;
        _button.interactable = false;

        StartCoroutine(MatchedPulse());
    }

    private IEnumerator FlipAnimation(bool reveal)
    {

        yield return ScaleX(1f, 0f);

        _backImage.gameObject.SetActive(!reveal);
        _frontImage.gameObject.SetActive(reveal);

        yield return ScaleX(0f, 1f);

        if (reveal)
            _button.interactable = false;
        else
            _button.interactable = true;
    }

    private IEnumerator ScaleX(float from, float to)
    {
        float elapsed = 0f;
        Vector3 scale = transform.localScale;

        while (elapsed < FLIP_HALF_DURATION)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / FLIP_HALF_DURATION);

            t = t * t * (3f - 2f * t);
            scale.x = Mathf.Lerp(from, to, t);
            transform.localScale = scale;
            yield return null;
        }

        scale.x = to;
        transform.localScale = scale;
    }

    private IEnumerator MatchedPulse()
    {

        float duration = 0.25f;
        float elapsed  = 0f;
        Vector3 baseScale = Vector3.one;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float s = 1f + 0.12f * Mathf.Sin(t * Mathf.PI);
            transform.localScale = baseScale * s;
            yield return null;
        }

        transform.localScale = baseScale;

        if (_frontImage != null)
        {
            Color c = _frontImage.color;
            c.r = Mathf.Min(1f, c.r + 0.15f);
            c.g = Mathf.Min(1f, c.g + 0.15f);
            c.b = Mathf.Min(1f, c.b + 0.15f);
            _frontImage.color = c;
        }
    }
}
