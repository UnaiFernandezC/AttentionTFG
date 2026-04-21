using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Datos de un elemento visual en el tablero.
/// Almacena el estado original y el actual para comparar cambios.
/// </summary>
public class ElementData
{
    public int            Id;
    public Vector2        OrigPos;
    public Vector2        OrigSize;
    public Color          OrigColor;
    public float          OrigRotation;

    public Vector2        CurPos;
    public Vector2        CurSize;
    public Color          CurColor;
    public float          CurRotation;

    public bool           IsChanged;
    public GameObject     Go;
    public RectTransform  RT;
    public Image          Img;

    public void CaptureOriginal()
    {
        OrigPos      = CurPos;
        OrigSize     = CurSize;
        OrigColor    = CurColor;
        OrigRotation = CurRotation;
    }
}

/// <summary>
/// Genera el grid de elementos visuales del minijuego "Cambios sutiles".
///
/// Configuración de dificultad:
///   Fácil  → columns=3 rows=2 (6 elem), size=140, gap=30
///   Medio  → columns=3 rows=3 (9 elem), size=120, gap=25
///   Difícil→ columns=4 rows=3 (12 elem), size=100, gap=20
/// </summary>
public class SceneGenerator : MonoBehaviour
{
    [Header("Grid")]
    public int   columns  = 3;
    public int   rows     = 2;
    public float elemSize = 140f;
    public float gap      = 30f;

    // Paleta de colores vivos y distintos
    static readonly Color[] PALETTE =
    {
        new Color(0.92f, 0.25f, 0.28f),   // rojo
        new Color(0.22f, 0.52f, 0.92f),   // azul
        new Color(0.18f, 0.78f, 0.38f),   // verde
        new Color(0.96f, 0.78f, 0.12f),   // amarillo
        new Color(0.68f, 0.22f, 0.90f),   // morado
        new Color(0.96f, 0.52f, 0.12f),   // naranja
        new Color(0.12f, 0.82f, 0.88f),   // cian
        new Color(0.92f, 0.28f, 0.68f),   // rosa
        new Color(0.42f, 0.82f, 0.22f),   // lima
        new Color(0.28f, 0.68f, 0.88f),   // celeste
        new Color(0.88f, 0.42f, 0.22f),   // terracota
        new Color(0.58f, 0.92f, 0.42f),   // verde claro
    };

    // Formas: 0=cuadrado, 1=rombo (45°), 2=rectángulo h, 3=rectángulo v
    static readonly float[] ROTATIONS     = { 0f, 45f, 0f, 0f };
    static readonly Vector2[] SIZE_MULTS  =
    {
        new Vector2(1f, 1f),
        new Vector2(1f, 1f),
        new Vector2(1.35f, 0.70f),
        new Vector2(0.70f, 1.35f),
    };

    // ═════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Genera los elementos en el parent dado y devuelve el array de datos.
    /// Los elementos NO tienen botón aún (lo añade FindChangeInputHandler).
    /// </summary>
    public ElementData[] Generate(RectTransform parent)
    {
        int total   = columns * rows;
        var data    = new ElementData[total];
        var usedColors = new System.Collections.Generic.List<int>();

        float totalW = columns * elemSize + (columns - 1) * gap;
        float totalH = rows    * elemSize + (rows    - 1) * gap;
        float startX = -totalW * 0.5f + elemSize * 0.5f;
        float startY =  totalH * 0.5f - elemSize * 0.5f;

        // Asignar colores únicos (mezcla aleatoria)
        var colorOrder = UniqueRandomOrder(total, PALETTE.Length);
        var shapeOrder = UniqueRandomOrder(total, ROTATIONS.Length);

        int idx = 0;
        for (int r = 0; r < rows; r++)
        for (int c = 0; c < columns; c++)
        {
            int shapeIdx = shapeOrder[idx];
            float rot    = ROTATIONS[shapeIdx];
            Vector2 sm   = SIZE_MULTS[shapeIdx];
            Vector2 sz   = new Vector2(elemSize * sm.x, elemSize * sm.y);
            Vector2 pos  = new Vector2(startX + c * (elemSize + gap),
                                       startY - r * (elemSize + gap));
            Color col    = PALETTE[colorOrder[idx]];

            var e      = new ElementData();
            e.Id       = idx;
            e.CurPos   = pos;
            e.CurSize  = sz;
            e.CurColor = col;
            e.CurRotation = rot;
            e.CaptureOriginal();

            // Glow/sombra detrás
            var shadowGO = new GameObject("Shadow" + idx);
            shadowGO.transform.SetParent(parent, false);
            var sRT = shadowGO.AddComponent<RectTransform>();
            sRT.anchorMin = sRT.anchorMax = new Vector2(0.5f, 0.5f);
            sRT.pivot     = new Vector2(0.5f, 0.5f);
            sRT.sizeDelta = sz * 1.25f;
            sRT.anchoredPosition = pos + new Vector2(4f, -4f);
            sRT.localEulerAngles = new Vector3(0, 0, rot);
            shadowGO.AddComponent<Image>().color = new Color(0, 0, 0, 0.28f);

            // Elemento principal
            var go = new GameObject("Elem" + idx);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = sz;
            rt.anchoredPosition = pos;
            rt.localEulerAngles = new Vector3(0, 0, rot);
            var img = go.AddComponent<Image>();
            img.color = col;

            // Brillo interior
            var shineGO = new GameObject("Shine");
            shineGO.transform.SetParent(go.transform, false);
            var shRT = shineGO.AddComponent<RectTransform>();
            shRT.anchorMin = new Vector2(0.08f, 0.55f);
            shRT.anchorMax = new Vector2(0.45f, 0.88f);
            shRT.sizeDelta = Vector2.zero;
            shRT.anchoredPosition = Vector2.zero;
            shineGO.AddComponent<Image>().color = new Color(1, 1, 1, 0.18f);

            e.Go  = go;
            e.RT  = rt;
            e.Img = img;
            data[idx] = e;
            idx++;
        }

        return data;
    }

    /// <summary>
    /// Actualiza visualmente un elemento según su estado CurXXX.
    /// </summary>
    public static void ApplyState(ElementData e)
    {
        if (e.RT  != null) { e.RT.anchoredPosition = e.CurPos;  e.RT.sizeDelta = e.CurSize; e.RT.localEulerAngles = new Vector3(0,0,e.CurRotation); }
        if (e.Img != null)   e.Img.color = e.CurColor;
    }

    // ─── Helpers ─────────────────────────────────────────────────────────

    static int[] UniqueRandomOrder(int count, int max)
    {
        var pool = new System.Collections.Generic.List<int>();
        for (int i = 0; i < max; i++) pool.Add(i);
        // Fisher-Yates
        for (int i = pool.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int tmp = pool[i]; pool[i] = pool[j]; pool[j] = tmp;
        }
        var result = new int[count];
        for (int i = 0; i < count; i++) result[i] = pool[i % pool.Count];
        return result;
    }
}
