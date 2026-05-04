using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RuleSwitchStimulusManager : MonoBehaviour
{

    [HideInInspector] public RectTransform AreaRT;

    public event Action OnStimulusClicked;

    GameObject _stimGO;
    RSStimData _current;
    public RSStimData Current => _current;

    public RSStimData GenerateRandom()
        => new RSStimData { Color = (RSStimColor)UnityEngine.Random.Range(0, 3) };

    public void ShowStimulus(RSStimData data)
    {
        HideStimulus();
        _current = data;

        Color col = RuleSwitchRuleManager.GetStimColor(data.Color);

        _stimGO = new GameObject("RSStim");
        _stimGO.transform.SetParent(AreaRT, false);
        var rootRT = _stimGO.AddComponent<RectTransform>();
        rootRT.anchorMin = rootRT.anchorMax = new Vector2(0.5f, 0.5f);
        rootRT.pivot     = new Vector2(0.5f, 0.5f);
        rootRT.sizeDelta = Vector2.zero;
        rootRT.anchoredPosition = Vector2.zero;
        var rootImg = _stimGO.AddComponent<Image>();
        rootImg.color         = Color.clear;
        rootImg.raycastTarget = false;
        _stimGO.transform.localScale = Vector3.zero;

        Layer(_stimGO.transform, "Glow", new Vector2(290f, 290f),
              new Color(col.r, col.g, col.b, 0.09f));

        Rect(_stimGO.transform, "Shadow", new Vector2(206f, 206f),
             new Color(0f, 0f, 0f, 0.28f), new Vector2(5f, -5f)).raycastTarget = false;

        var mainImg = Rect(_stimGO.transform, "Main", new Vector2(200f, 200f),
                           col, Vector2.zero);

        var shineImg = Rect(mainImg.transform, "Shine", new Vector2(58f, 58f),
                            new Color(1f, 1f, 1f, 0.28f), new Vector2(-50f, 52f));
        shineImg.raycastTarget = false;

        var lblGO = new GameObject("ColorLbl");
        lblGO.transform.SetParent(mainImg.transform, false);
        var lRT = lblGO.AddComponent<RectTransform>();
        lRT.anchorMin = Vector2.zero; lRT.anchorMax = Vector2.one;
        lRT.sizeDelta = Vector2.zero; lRT.anchoredPosition = Vector2.zero;
        var lbl = lblGO.AddComponent<TextMeshProUGUI>();
        lbl.text          = RuleSwitchRuleManager.GetColorName(data.Color);
        lbl.color         = new Color(1f, 1f, 1f, 0.50f);
        lbl.fontSize      = 28f;
        lbl.fontStyle     = FontStyles.Bold;
        lbl.alignment     = TextAlignmentOptions.Center;
        lbl.raycastTarget = false;

        var btn = mainImg.gameObject.AddComponent<Button>();
        var bc  = btn.colors;
        bc.normalColor      = Color.white;
        bc.highlightedColor = new Color(1f, 1f, 1f, 0.80f);
        bc.pressedColor     = new Color(0.70f, 0.70f, 0.70f, 1f);
        bc.selectedColor    = Color.white;
        btn.colors        = bc;
        btn.targetGraphic = mainImg;
        btn.onClick.AddListener(() => OnStimulusClicked?.Invoke());
    }

    public void HideStimulus()
    {
        if (_stimGO != null) { Destroy(_stimGO); _stimGO = null; }
    }

    public void AnimateIn(float totalElapsed)
    {
        if (_stimGO == null) return;
        float s = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(totalElapsed / 0.18f));
        _stimGO.transform.localScale = Vector3.one * s;
    }

    public void ApplyFeedbackTint(bool correct)
    {
        if (_stimGO == null) return;
        var glowTf = _stimGO.transform.Find("Glow");
        if (glowTf == null) return;
        var img = glowTf.GetComponent<Image>();
        img.color = correct
            ? new Color(0.22f, 0.90f, 0.50f, 0.42f)
            : new Color(0.90f, 0.22f, 0.22f, 0.42f);
    }

    static Image Rect(Transform parent, string name, Vector2 size, Color col, Vector2 offset)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = offset;
        var img = go.AddComponent<Image>();
        img.color = col;
        return img;
    }

    static void Layer(Transform parent, string name, Vector2 size, Color col)
    {
        var img = Rect(parent, name, size, col, Vector2.zero);
        img.raycastTarget = false;
    }
}
