using UnityEngine;

/// <summary>
/// Aplica un cambio sutil a uno de los elementos del tablero.
///
/// Tipos de cambio según dificultad:
///   Fácil  → solo COLOR (cambio a un color muy diferente)
///   Medio  → COLOR sutil o SIZE (±20%)
///   Difícil→ SIZE sutil (±12%) o POSITION_SWAP
///
/// changeSubtlety: 0=obvio  1=sutil  2=muy sutil
/// </summary>
public class ChangeManager : MonoBehaviour
{
    [Header("0=obvio 1=sutil 2=muy sutil")]
    public int changeSubtlety = 0;

    [Header("0=solo color, 1=color+size, 2=size+swap")]
    public int changeTypeMask = 0;

    // Paleta completa (misma que SceneGenerator para elegir colores distintos)
    static readonly Color[] PALETTE =
    {
        new Color(0.92f, 0.25f, 0.28f), new Color(0.22f, 0.52f, 0.92f),
        new Color(0.18f, 0.78f, 0.38f), new Color(0.96f, 0.78f, 0.12f),
        new Color(0.68f, 0.22f, 0.90f), new Color(0.96f, 0.52f, 0.12f),
        new Color(0.12f, 0.82f, 0.88f), new Color(0.92f, 0.28f, 0.68f),
        new Color(0.42f, 0.82f, 0.22f), new Color(0.28f, 0.68f, 0.88f),
        new Color(0.88f, 0.42f, 0.22f), new Color(0.58f, 0.92f, 0.42f),
    };

    // ─── Resultado público ────────────────────────────────────────────────
    public int  ChangedElementId { get; private set; } = -1;
    public string ChangeDescription { get; private set; } = "";

    // ═════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Selecciona un elemento aleatorio y le aplica el cambio.
    /// Marca ElementData.IsChanged = true en el elemento modificado.
    /// Llama a SceneGenerator.ApplyState() para actualizar la visuals.
    /// </summary>
    public void ApplyChange(ElementData[] elements)
    {
        if (elements == null || elements.Length == 0) return;

        int targetIdx = Random.Range(0, elements.Length);
        var e = elements[targetIdx];
        e.IsChanged = true;
        ChangedElementId = e.Id;

        // Elegir tipo de cambio según máscara
        int type = PickChangeType();

        switch (type)
        {
            case 0: ApplyColorChange(e);    break;
            case 1: ApplySizeChange(e);     break;
            case 2: ApplyPositionSwap(e, elements); break;
        }

        SceneGenerator.ApplyState(e);
    }

    // ─── Tipos de cambio ─────────────────────────────────────────────────

    void ApplyColorChange(ElementData e)
    {
        // Elegir un color de la paleta que sea diferente al actual
        Color orig = e.CurColor;
        Color next = orig;
        int   tries = 0;
        while (ColorDistance(next, orig) < (changeSubtlety == 0 ? 0.6f : 0.35f) && tries < 30)
        {
            next  = PALETTE[Random.Range(0, PALETTE.Length)];
            tries++;
        }
        // subtlety 2: mezclar ligeramente con el original
        if (changeSubtlety == 2)
            next = Color.Lerp(orig, next, 0.40f);

        e.CurColor = next;
        ChangeDescription = "color";
    }

    void ApplySizeChange(ElementData e)
    {
        float factor = changeSubtlety == 0 ? 0.30f
                     : changeSubtlety == 1 ? 0.20f : 0.12f;
        bool  grow   = Random.value > 0.5f;
        e.CurSize    = e.OrigSize * (grow ? 1f + factor : 1f - factor);
        ChangeDescription = grow ? "tamaño (mayor)" : "tamaño (menor)";
    }

    void ApplyPositionSwap(ElementData e, ElementData[] all)
    {
        // Buscar un vecino distinto para intercambiar posición
        int other = Random.Range(0, all.Length);
        if (other == e.Id) other = (other + 1) % all.Length;
        var b = all[other];

        Vector2 tmp  = e.CurPos;
        e.CurPos     = b.CurPos;
        b.CurPos     = tmp;
        SceneGenerator.ApplyState(b);
        ChangeDescription = "posición";
    }

    // ─── Helpers ─────────────────────────────────────────────────────────

    int PickChangeType()
    {
        if (changeTypeMask == 0) return 0;                          // solo color
        if (changeTypeMask == 1) return Random.value > 0.5f ? 0:1; // color o size
        return Random.value > 0.5f ? 1 : 2;                        // size o swap
    }

    static float ColorDistance(Color a, Color b)
    {
        float dr = a.r - b.r, dg = a.g - b.g, db = a.b - b.b;
        return Mathf.Sqrt(dr*dr + dg*dg + db*db);
    }
}
