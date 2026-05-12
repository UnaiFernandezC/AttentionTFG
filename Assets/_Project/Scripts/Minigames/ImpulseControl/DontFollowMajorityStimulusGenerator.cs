using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Genera y posiciona las flechas en pantalla para cada ronda.
///
/// Crea (totalArrows - minorityCount) flechas de mayoría y
/// minorityCount flechas de minoría, las mezcla aleatoriamente
/// y las distribuye en una rejilla con leve variación de posición.
///
/// Todas las flechas tienen el mismo estilo visual para que el
/// jugador deba contar, no distinguir por color.
/// </summary>
public class DontFollowMajorityStimulusGenerator : MonoBehaviour
{
    // ------------------------------------------------------------------ //
    // Inspector
    // ------------------------------------------------------------------ //
    [Header("Configuración de estímulos")]
    public int totalArrows   = 10;
    public int minorityCount = 2;

    // ------------------------------------------------------------------ //
    // Privado
    // ------------------------------------------------------------------ //
    readonly List<GameObject> _active = new List<GameObject>();

    // ------------------------------------------------------------------ //
    // API pública
    // ------------------------------------------------------------------ //

    /// <summary>
    /// Crea las flechas dentro de <paramref name="container"/>.
    /// Llama a Clear() antes de crear las nuevas.
    /// </summary>
    public void Generate(RectTransform container,
                         DFMDirection majority,
                         DFMDirection minority)
    {
        Clear();

        int majCount = Mathf.Max(1, totalArrows - minorityCount);
        int minCount = Mathf.Max(1, minorityCount);

        // Construir lista mezclada
        var dirs = new List<DFMDirection>(totalArrows);
        for (int i = 0; i < majCount; i++) dirs.Add(majority);
        for (int i = 0; i < minCount; i++) dirs.Add(minority);

        // Fisher–Yates shuffle
        for (int i = dirs.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            DFMDirection tmp = dirs[i];
            dirs[i] = dirs[j];
            dirs[j] = tmp;
        }

        PlaceArrows(container, dirs);
    }

    /// <summary>Destruye todas las flechas generadas.</summary>
    public void Clear()
    {
        foreach (var go in _active)
            if (go != null) Destroy(go);
        _active.Clear();
    }

    // ------------------------------------------------------------------ //
    // Posicionamiento
    // ------------------------------------------------------------------ //

    void PlaceArrows(RectTransform container, List<DFMDirection> dirs)
    {
        int n = dirs.Count;

        // Calcular rejilla (más ancha que alta, aspecto ~1.8:1)
        int cols = Mathf.CeilToInt(Mathf.Sqrt(n * 1.8f));
        cols = Mathf.Max(cols, 3);
        int rows = Mathf.CeilToInt((float)n / cols);

        float cellW = 1f / cols;
        float cellH = 1f / rows;

        // Generar slots y mezclar para distribuir sin patrón fijo
        int totalSlots = cols * rows;
        var slots = new List<int>(totalSlots);
        for (int i = 0; i < totalSlots; i++) slots.Add(i);
        for (int i = slots.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int tmp = slots[i]; slots[i] = slots[j]; slots[j] = tmp;
        }

        for (int i = 0; i < n; i++)
        {
            int slot = slots[i];
            int col  = slot % cols;
            int row  = slot / cols;

            // Centro del slot + jitter leve (±10% de celda)
            float cx = (col + 0.5f) * cellW + Random.Range(-cellW * 0.10f, cellW * 0.10f);
            float cy = 1f - (row + 0.5f) * cellH + Random.Range(-cellH * 0.10f, cellH * 0.10f);

            var go = new GameObject("Arrow_" + i);
            go.transform.SetParent(container, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(cx, cy);
            rt.pivot     = Vector2.one * 0.5f;
            rt.sizeDelta = new Vector2(80f, 80f);
            rt.anchoredPosition = Vector2.zero;

            var t = go.AddComponent<TextMeshProUGUI>();
            t.text       = DontFollowMajorityRuleManager.ArrowSymbol(dirs[i]);
            t.fontSize   = 56f;
            t.fontStyle  = FontStyles.Bold;
            t.alignment  = TextAlignmentOptions.Center;
            t.color      = new Color(0.78f, 0.86f, 0.96f);
            t.overflowMode = TextOverflowModes.Overflow;

            _active.Add(go);
        }
    }
}
