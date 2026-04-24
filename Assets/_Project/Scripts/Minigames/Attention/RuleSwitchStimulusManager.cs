using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Genera y muestra estímulos de color en el área de juego.
///
/// Un estímulo = cuadrado centrado con:
///   • Glow exterior suave del mismo color
///   • Sombra desplazada (profundidad)
///   • Cuadrado principal (Button → detecta click)
///   • Brillo interior (esquina superior)
///   • Etiqueta con el nombre del color (accesibilidad)
///
/// El jugador interactúa haciendo click en el cuadrado.
/// Sin click en el tiempo límite = rechazo implícito.
/// </summary>
public class RuleSwitchStimulusManager : MonoBehaviour
{
    /// <summary>RectTransform del área de juego. Asignado por el GameManager tras BuildUI.</summary>
    [HideInInspector] public RectTransform AreaRT;

    /// <summary>Disparado cuando el jugador hace click en el estímulo.</summary>
    public event Action OnStimulusClicked;

    GameObject _stimGO;
    RSStimData _current;
    public RSStimData Current => _current;

    // ─────────────────────────────────────────────────────────────────

    /// <summary>Genera un estímulo aleatorio.</summary>
    public RSStimData GenerateRandom()
        => new RSStimData { Color = (RSStimColor)UnityEngine.Random.Range(0, 3) };

    /// <summary>Crea y muestra el estímulo en pantalla.</summary>
    public void ShowStimulus(RSStimData data)
    {
        HideStimulus();
        _current = data;

        Color col = RuleSwitchRuleManager.GetStimColor(data.Color);

        // ── Raíz del estímulo ─────────────────────────────────────────
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
        _stimGO.transform.localScale = Vector3.zero;  // animado desde 0

        // ── Glow exterior ─────────────────────────────────────────────
        Layer(_stimGO.transform, "Glow", new Vector2(290f, 290f),
              new Color(col.r, col.g, col.b, 0.09f));

        // ── Sombra ────────────────────────────────────────────────────
        Rect(_stimGO.transform, "Shadow", new Vector2(206f, 206f),
             new Color(0f, 0f, 0f, 0.28f), new Vector2(5f, -5f)).raycastTarget = false;

        // ── Cuadrado principal (interactivo) ──────────────────────────
        var mainImg = Rect(_stimGO.transform, "Main", new Vector2(200f, 200f),
                           col, Vector2.zero);

        // Brillo interior
        var shineImg = Rect(mainImg.transform, "Shine", new Vector2(58f, 58f),
                            new Color(1f, 1f, 1f, 0.28f), new Vector2(-50f, 52f));
        shineImg.raycastTarget = false;

        // Etiqueta nombre del color
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

        // ── Button sobre el cuadrado principal ────────────────────────
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

    /// <summary>Destruye el estímulo actual (si existe).</summary>
    public void HideStimulus()
    {
        if (_stimGO != null) { Destroy(_stimGO); _stimGO = null; }
    }

    /// <summary>
    /// Anima la entrada del estímulo (escala 0→1 en ~0.18 s).
    /// Llamar pasando el tiempo total transcurrido desde ShowStimulus.
    /// </summary>
    public void AnimateIn(float totalElapsed)
    {
        if (_stimGO == null) return;
        float s = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(totalElapsed / 0.18f));
        _stimGO.transform.localScale = Vector3.one * s;
    }

    /// <summary>Aplica un tinte de feedback en el glow del estímulo.</summary>
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

    // ─────────────────────────────────────────────────────────────────
    // Helpers de construcción
    // ─────────────────────────────────────────────────────────────────

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
