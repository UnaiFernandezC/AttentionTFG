using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Crea y gestiona la cuadrícula de casillas del minijuego "Repite el dibujo".
///
/// Responsabilidades:
///   - Instanciar las celdas (PatternCell) con su layout en grid.
///   - Generar un patrón aleatorio (Fisher-Yates sobre los índices).
///   - Controlar las fases: mostrar patrón → ocultar → habilitar input del jugador.
///   - Validar la respuesta y colorear el resultado.
/// </summary>
public class PatternGridManager : MonoBehaviour
{
    // ─── Estado interno ───────────────────────────────────────────────────
    private readonly List<PatternCell>  _cells      = new List<PatternCell>();
    private readonly HashSet<int>       _patternSet = new HashSet<int>();
    private int _cols, _rows;

    // ─── Propiedades públicas ─────────────────────────────────────────────
    public int TotalCells    => _cells.Count;
    public int PatternCount  => _patternSet.Count;
    public int SelectedCount { get; private set; }

    // ─── Evento ───────────────────────────────────────────────────────────
    /// <summary>Se dispara cuando el jugador selecciona o deselecciona una casilla.</summary>
    public System.Action<int> OnSelectionChanged;

    // ─── Inicialización ───────────────────────────────────────────────────

    /// <summary>
    /// Crea la cuadrícula de cols × rows celdas dentro del contenedor dado.
    /// </summary>
    public void Initialize(RectTransform container, int cols, int rows,
                           float cellSize = 115f, float spacing = 12f)
    {
        _cols = cols;
        _rows = rows;
        SelectedCount = 0;

        float totalW = cols * cellSize + (cols - 1) * spacing;
        float totalH = rows * cellSize + (rows - 1) * spacing;
        container.sizeDelta = new Vector2(totalW, totalH);

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                int   idx = r * cols + c;
                float x   = c * (cellSize + spacing) - (totalW - cellSize) * 0.5f;
                float y   = -r * (cellSize + spacing) + (totalH - cellSize) * 0.5f;

                _cells.Add(CreateCell(container, idx, new Vector2(x, y), cellSize));
            }
        }
    }

    // ─── Creación de celdas ───────────────────────────────────────────────

    private PatternCell CreateCell(RectTransform container, int index,
                                   Vector2 pos, float size)
    {
        // Marco exterior (borde oscuro)
        var frameGO = new GameObject($"Cell_{index}");
        frameGO.transform.SetParent(container, false);
        var frameRT = frameGO.AddComponent<RectTransform>();
        frameRT.anchorMin        = new Vector2(0.5f, 0.5f);
        frameRT.anchorMax        = new Vector2(0.5f, 0.5f);
        frameRT.pivot            = new Vector2(0.5f, 0.5f);
        frameRT.anchoredPosition = pos;
        frameRT.sizeDelta        = new Vector2(size, size);
        frameGO.AddComponent<Image>().color = new Color(0.06f, 0.07f, 0.15f);

        // Añadir Button ANTES que PatternCell (RequireComponent)
        frameGO.AddComponent<Button>();

        // Panel de fondo de la celda
        var bgGO = new GameObject("BG");
        bgGO.transform.SetParent(frameGO.transform, false);
        var bgRT = bgGO.AddComponent<RectTransform>();
        bgRT.anchorMin        = Vector2.zero;
        bgRT.anchorMax        = Vector2.one;
        bgRT.anchoredPosition = Vector2.zero;
        bgRT.sizeDelta        = new Vector2(-7f, -7f);
        var bgImg = bgGO.AddComponent<Image>();
        bgImg.color = new Color(0.18f, 0.20f, 0.36f);

        // Capa de brillo (transparente por defecto)
        var shineGO = new GameObject("Shine");
        shineGO.transform.SetParent(bgGO.transform, false);
        var shineRT = shineGO.AddComponent<RectTransform>();
        shineRT.anchorMin        = Vector2.zero;
        shineRT.anchorMax        = Vector2.one;
        shineRT.anchoredPosition = Vector2.zero;
        shineRT.sizeDelta        = Vector2.zero;
        var shineImg = shineGO.AddComponent<Image>();
        shineImg.color = new Color(1f, 1f, 1f, 0f);

        var cell = frameGO.AddComponent<PatternCell>();
        cell.Initialize(index, bgImg, shineImg);
        cell.OnClicked += HandleCellClicked;

        return cell;
    }

    // ─── Generación del patrón ────────────────────────────────────────────

    /// <summary>
    /// Selecciona aleatoriamente <paramref name="count"/> celdas como patrón (Fisher-Yates).
    /// </summary>
    public void GeneratePattern(int count)
    {
        _patternSet.Clear();

        var pool = new List<int>(_cells.Count);
        for (int i = 0; i < _cells.Count; i++) pool.Add(i);

        // Fisher-Yates shuffle
        for (int i = pool.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }

        int n = Mathf.Min(count, _cells.Count);
        for (int i = 0; i < n; i++)
            _patternSet.Add(pool[i]);
    }

    // ─── Control de fases ─────────────────────────────────────────────────

    /// <summary>Ilumina las celdas del patrón (fase de memorización).</summary>
    public void ShowPattern()
    {
        foreach (var cell in _cells)
            cell.SetState(_patternSet.Contains(cell.Index)
                ? PatternCell.CellState.PatternShow
                : PatternCell.CellState.Idle);
    }

    /// <summary>Apaga todas las celdas (transición a fase de respuesta).</summary>
    public void HidePattern()
    {
        foreach (var cell in _cells)
            cell.SetState(PatternCell.CellState.Idle);
    }

    /// <summary>Habilita o deshabilita el input en todas las celdas.</summary>
    public void EnableInput(bool enable)
    {
        foreach (var cell in _cells)
            cell.EnableInput(enable);
    }

    // ─── Validación y resultado ───────────────────────────────────────────

    /// <summary>
    /// Devuelve true solo si la selección del jugador coincide exactamente con el patrón.
    /// </summary>
    public bool ValidateAnswer()
    {
        foreach (var cell in _cells)
            if (_patternSet.Contains(cell.Index) != cell.IsSelected)
                return false;
        return true;
    }

    /// <summary>
    /// Colorea cada celda según el resultado:
    ///   Correct  → en patrón y seleccionada (verde)
    ///   Wrong    → seleccionada pero NO en patrón (rojo)
    ///   Missed   → en patrón pero NO seleccionada (naranja — muestra la solución)
    ///   Idle     → no en patrón y no seleccionada (neutral)
    /// Devuelve estadísticas (correct, wrong, missed).
    /// </summary>
    public (int correct, int wrong, int missed) ShowResult()
    {
        int correct = 0, wrong = 0, missed = 0;

        foreach (var cell in _cells)
        {
            bool inPat = _patternSet.Contains(cell.Index);
            bool sel   = cell.IsSelected;

            if      ( inPat &&  sel) { cell.SetState(PatternCell.CellState.Correct); correct++; }
            else if (!inPat &&  sel) { cell.SetState(PatternCell.CellState.Wrong);   wrong++;   }
            else if ( inPat && !sel) { cell.SetState(PatternCell.CellState.Missed);  missed++;  }
            // !inPat && !sel → stays Idle
        }

        return (correct, wrong, missed);
    }

    /// <summary>Destruye todas las celdas y limpia el estado.</summary>
    public void ClearGrid()
    {
        for (int i = _cells.Count - 1; i >= 0; i--)
            if (_cells[i] != null) Destroy(_cells[i].gameObject);
        _cells.Clear();
        _patternSet.Clear();
        SelectedCount = 0;
    }

    // ─── Input del jugador ────────────────────────────────────────────────

    private void HandleCellClicked(PatternCell cell)
    {
        if (cell.IsSelected)
        {
            cell.SetState(PatternCell.CellState.Idle);
            SelectedCount--;
        }
        else
        {
            cell.SetState(PatternCell.CellState.Selected);
            SelectedCount++;
        }

        OnSelectionChanged?.Invoke(SelectedCount);
    }
}
