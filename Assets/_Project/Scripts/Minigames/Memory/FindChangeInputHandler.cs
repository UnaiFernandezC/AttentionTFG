using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Añade botones a los elementos generados y rutas los clicks al GameManager.
/// Solo activo durante la fase FIND (mientras el jugador busca el cambio).
/// </summary>
public class FindChangeInputHandler : MonoBehaviour
{
    public bool AcceptInput { get; set; } = false;

    public event Action<int> OnElementClicked;

    // ═════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Registra botones en todos los elementos.
    /// Llamar después de SceneGenerator.Generate().
    /// </summary>
    public void RegisterElements(ElementData[] elements)
    {
        foreach (var e in elements)
        {
            if (e.Go == null) continue;
            int capturedId = e.Id;

            // Añadir o reusar Button
            var btn = e.Go.GetComponent<Button>();
            if (btn == null) btn = e.Go.AddComponent<Button>();
            btn.targetGraphic = e.Img;

            // Highlight sutil al pasar por encima
            var cb = btn.colors;
            cb.normalColor      = Color.white;
            cb.highlightedColor = new Color(1f, 1f, 1f, 0.80f);
            cb.pressedColor     = new Color(0.70f, 0.70f, 0.70f);
            cb.fadeDuration     = 0.10f;
            btn.colors = cb;

            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                if (AcceptInput) OnElementClicked?.Invoke(capturedId);
            });
        }
    }

    public void SetElementsInteractable(ElementData[] elements, bool value)
    {
        foreach (var e in elements)
        {
            if (e.Go == null) continue;
            var btn = e.Go.GetComponent<Button>();
            if (btn != null) btn.interactable = value;
        }
    }
}
