using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Crea la cuadrícula de botones numéricos dinámicamente y gestiona toda la lógica de selección:
/// validación del orden, conteo de aciertos y detección de victoria/fallo.
/// </summary>
public class OrderManager : MonoBehaviour
{
    // ─── Configuración ─────────────────────────────────────────────────────
    [Header("Parámetros de la cuadrícula")]
    [Tooltip("Tiempo en segundos que el flash de error permanece visible.")]
    public float wrongFlashDelay = 0.55f;

    // ─── Paleta de colores de los botones ─────────────────────────────────
    private static readonly Color[] NUMBER_PALETTE = new Color[]
    {
        new Color(0.98f, 0.40f, 0.40f),   // 0 Coral
        new Color(0.30f, 0.82f, 0.76f),   // 1 Turquesa
        new Color(1.00f, 0.88f, 0.35f),   // 2 Amarillo
        new Color(0.62f, 0.58f, 1.00f),   // 3 Lavanda
        new Color(0.30f, 0.90f, 0.65f),   // 4 Menta
        new Color(0.43f, 0.71f, 1.00f),   // 5 Azul cielo
        new Color(1.00f, 0.65f, 0.25f),   // 6 Naranja
        new Color(0.98f, 0.44f, 0.68f),   // 7 Rosa
        new Color(0.55f, 0.90f, 0.35f),   // 8 Verde lima
        new Color(0.80f, 0.50f, 1.00f),   // 9 Violeta
    };

    // ─── Estado interno ────────────────────────────────────────────────────
    private List<NumberButton> _allButtons = new List<NumberButton>();
    private int  _nextExpected  = 1;
    private int  _totalNumbers  = 0;
    private bool _isLocked      = false;   // Bloqueado durante el flash de error
    private int  _wrongCount    = 0;
    private int  _correctCount  = 0;

    // ─── Eventos ───────────────────────────────────────────────────────────
    /// <summary>Se dispara al pulsar un número correcto. Arg: siguiente número esperado.</summary>
    public System.Action<int> OnCorrectPress;

    /// <summary>Se dispara al pulsar un número incorrecto. Arg: número de errores totales.</summary>
    public System.Action<int> OnWrongPress;

    /// <summary>Se dispara cuando todos los números han sido pulsados en orden.</summary>
    public System.Action OnComplete;

    // ─── API pública ───────────────────────────────────────────────────────

    public int WrongCount   => _wrongCount;
    public int CorrectCount => _correctCount;

    /// <summary>
    /// Inicializa la cuadrícula: crea los botones, los mezcla y los coloca.
    /// </summary>
    /// <param name="container">RectTransform padre.</param>
    /// <param name="numCount">Cantidad de números (4-8).</param>
    /// <param name="btnSize">Tamaño de cada botón en píxeles.</param>
    /// <param name="spacing">Espacio entre botones en píxeles.</param>
    public void Initialize(RectTransform container, int numCount, float btnSize = 130f, float spacing = 14f)
    {
        _totalNumbers = Mathf.Clamp(numCount, 2, 10);

        // Crear lista de números 1..N y mezclarlos (Fisher-Yates)
        var numbers = new List<int>();
        for (int i = 1; i <= _totalNumbers; i++)
            numbers.Add(i);

        for (int i = numbers.Count - 1; i > 0; i--)
        {
            int rand = Random.Range(0, i + 1);
            (numbers[i], numbers[rand]) = (numbers[rand], numbers[i]);
        }

        // Calcular la mejor disposición en cuadrícula (sin celdas vacías)
        int cols, rows;
        CalculateBestGrid(_totalNumbers, out cols, out rows);

        // Ajustar el container
        float totalWidth  = cols * btnSize + (cols - 1) * spacing;
        float totalHeight = rows * btnSize + (rows - 1) * spacing;
        container.sizeDelta = new Vector2(totalWidth, totalHeight);

        // Crear botones
        for (int i = 0; i < _totalNumbers; i++)
        {
            int col = i % cols;
            int row = i / cols;
            NumberButton btn = CreateButton(container, numbers[i], col, row, btnSize, spacing);
            _allButtons.Add(btn);
        }
    }

    // ─── Creación de botones ───────────────────────────────────────────────

    private NumberButton CreateButton(RectTransform container, int number,
                                      int col, int row, float btnSize, float spacing)
    {
        // ── Root del botón ──
        var go = new GameObject($"NumBtn_{number}");
        go.transform.SetParent(container, false);

        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(btnSize, btnSize);

        // Posición en el grid (centrado en el container)
        float x = col * (btnSize + spacing) - (container.sizeDelta.x - btnSize) * 0.5f;
        float y = -row * (btnSize + spacing) + (container.sizeDelta.y - btnSize) * 0.5f;
        rt.anchoredPosition = new Vector2(x, y);

        go.AddComponent<Button>();

        var numBtn = go.AddComponent<NumberButton>();

        // ── Marco exterior ──
        var frame = CreatePanel(go.transform, "Frame",
            new Color(0.08f, 0.08f, 0.18f),
            Vector2.zero, new Vector2(btnSize, btnSize));

        // ── Panel de fondo del botón ──
        var bg = CreatePanel(go.transform, "BG",
            new Color(0.20f, 0.22f, 0.40f),
            Vector2.zero, new Vector2(btnSize - 6f, btnSize - 6f));

        // Brillo sutil en la esquina superior izquierda
        CreatePanel(bg.transform, "Shine",
            new Color(1f, 1f, 1f, 0.10f),
            new Vector2(-(btnSize - 6f) * 0.22f, (btnSize - 6f) * 0.22f),
            new Vector2((btnSize - 6f) * 0.55f, (btnSize - 6f) * 0.55f));

        // ── Etiqueta del número ──
        var labelGO = new GameObject("Label");
        labelGO.transform.SetParent(go.transform, false);
        var labelRT = labelGO.AddComponent<RectTransform>();
        labelRT.anchorMin = Vector2.zero;
        labelRT.anchorMax = Vector2.one;
        labelRT.sizeDelta = Vector2.zero;
        labelRT.anchoredPosition = Vector2.zero;

        var tmp = labelGO.AddComponent<TMPro.TextMeshProUGUI>();
        tmp.text      = number.ToString();
        tmp.color     = new Color(0.88f, 0.90f, 1.00f);
        tmp.fontSize  = btnSize * 0.42f;
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        tmp.fontStyle = TMPro.FontStyles.Bold;

        // Inicializar NumberButton
        numBtn.Initialize(number, bg.GetComponent<Image>(), tmp);
        numBtn.OnClicked += HandleButtonClicked;

        return numBtn;
    }

    // ─── Lógica de selección ───────────────────────────────────────────────

    private void HandleButtonClicked(NumberButton btn)
    {
        if (_isLocked) return;

        if (btn.Number == _nextExpected)
        {
            // CORRECTO
            btn.SetCorrect();
            _correctCount++;
            _nextExpected++;
            OnCorrectPress?.Invoke(_nextExpected);

            if (_correctCount >= _totalNumbers)
            {
                StartCoroutine(DelayedComplete());
            }
        }
        else
        {
            // INCORRECTO
            _wrongCount++;
            _isLocked = true;
            btn.FlashWrong();
            OnWrongPress?.Invoke(_wrongCount);
            StartCoroutine(UnlockAfterFlash());
        }
    }

    private System.Collections.IEnumerator UnlockAfterFlash()
    {
        yield return new UnityEngine.WaitForSeconds(wrongFlashDelay);
        _isLocked = false;
    }

    private System.Collections.IEnumerator DelayedComplete()
    {
        yield return new UnityEngine.WaitForSeconds(0.40f);
        OnComplete?.Invoke();
    }

    // ─── Reset ────────────────────────────────────────────────────────────

    /// <summary>Destruye todos los botones actuales.</summary>
    public void ClearBoard()
    {
        foreach (var btn in _allButtons)
        {
            if (btn != null)
                Destroy(btn.gameObject);
        }
        _allButtons.Clear();
        _nextExpected = 1;
        _wrongCount   = 0;
        _correctCount = 0;
        _isLocked     = false;
    }

    // ─── Helpers de construcción de UI ────────────────────────────────────

    private GameObject CreatePanel(Transform parent, string name, Color color,
                                   Vector2 anchoredPos, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta        = size;

        var img = go.AddComponent<Image>();
        img.color = color;

        return go;
    }

    // ─── Cálculo de grid ──────────────────────────────────────────────────

    /// <summary>
    /// Devuelve la distribución más cuadrada sin celdas vacías usando divisores exactos.
    /// </summary>
    private static void CalculateBestGrid(int total, out int cols, out int rows)
    {
        cols = total;
        rows = 1;

        for (int c = Mathf.FloorToInt(Mathf.Sqrt(total)); c >= 1; c--)
        {
            if (total % c == 0)
            {
                rows = c;
                cols = total / c;
                if (cols < rows) { int tmp = cols; cols = rows; rows = tmp; }
                return;
            }
        }
    }
}
