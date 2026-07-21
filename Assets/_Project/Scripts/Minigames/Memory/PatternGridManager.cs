// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PatternGridManager : MonoBehaviour
{

    private readonly List<PatternCell>  _cells      = new List<PatternCell>();
    private readonly HashSet<int>       _patternSet = new HashSet<int>();
    private int _cols, _rows;
    private int _decoyIndex = -1;

    public int TotalCells    => _cells.Count;
    public int PatternCount  => _patternSet.Count;
    public int SelectedCount { get; private set; }

    public System.Action<int> OnSelectionChanged;

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

                var newCell = CreateCell(container, idx, new Vector2(x, y), cellSize);
                _cells.Add(newCell);

                // Entrada escalonada en oleada diagonal (solo presentacion)
                UITween.PopIn((RectTransform)newCell.transform, 0.28f, 0.60f,
                              (r + c) * 0.025f);
            }
        }
    }

    private PatternCell CreateCell(RectTransform container, int index,
                                   Vector2 pos, float size)
    {

        var frameGO = new GameObject($"Cell_{index}");
        frameGO.transform.SetParent(container, false);
        var frameRT = frameGO.AddComponent<RectTransform>();
        frameRT.anchorMin        = new Vector2(0.5f, 0.5f);
        frameRT.anchorMax        = new Vector2(0.5f, 0.5f);
        frameRT.pivot            = new Vector2(0.5f, 0.5f);
        frameRT.anchoredPosition = pos;
        frameRT.sizeDelta        = new Vector2(size, size);
        // Marco redondeado con tinte morado oscuro (paleta Memoria)
        var frameImg = frameGO.AddComponent<Image>();
        frameImg.color                   = new Color(0.09f, 0.05f, 0.20f);
        frameImg.sprite                  = KidUI.RoundedSprite;
        frameImg.type                    = Image.Type.Sliced;
        frameImg.pixelsPerUnitMultiplier = 1.4f;

        frameGO.AddComponent<Button>();

        // Halo morado suave detras de la celda (glow)
        var glowGO = new GameObject("Glow");
        glowGO.transform.SetParent(frameGO.transform, false);
        glowGO.transform.SetAsFirstSibling();
        var glowRT = glowGO.AddComponent<RectTransform>();
        glowRT.anchorMin        = Vector2.zero;
        glowRT.anchorMax        = Vector2.one;
        glowRT.anchoredPosition = Vector2.zero;
        glowRT.sizeDelta        = new Vector2(16f, 16f);
        var glowImg = glowGO.AddComponent<Image>();
        glowImg.color                   = new Color(0.58f, 0.28f, 0.92f, 0.10f);
        glowImg.sprite                  = KidUI.RoundedSprite;
        glowImg.type                    = Image.Type.Sliced;
        glowImg.pixelsPerUnitMultiplier = 0.9f;
        glowImg.raycastTarget           = false;

        var bgGO = new GameObject("BG");
        bgGO.transform.SetParent(frameGO.transform, false);
        var bgRT = bgGO.AddComponent<RectTransform>();
        bgRT.anchorMin        = Vector2.zero;
        bgRT.anchorMax        = Vector2.one;
        bgRT.anchoredPosition = Vector2.zero;
        bgRT.sizeDelta        = new Vector2(-7f, -7f);
        var bgImg = bgGO.AddComponent<Image>();
        bgImg.color                   = new Color(0.20f, 0.16f, 0.38f);
        bgImg.sprite                  = KidUI.RoundedSprite;
        bgImg.type                    = Image.Type.Sliced;
        bgImg.pixelsPerUnitMultiplier = 1.6f;

        var shineGO = new GameObject("Shine");
        shineGO.transform.SetParent(bgGO.transform, false);
        var shineRT = shineGO.AddComponent<RectTransform>();
        shineRT.anchorMin        = Vector2.zero;
        shineRT.anchorMax        = Vector2.one;
        shineRT.anchoredPosition = Vector2.zero;
        shineRT.sizeDelta        = Vector2.zero;
        var shineImg = shineGO.AddComponent<Image>();
        shineImg.color                   = new Color(1f, 1f, 1f, 0f);
        shineImg.sprite                  = KidUI.RoundedSprite;
        shineImg.type                    = Image.Type.Sliced;
        shineImg.pixelsPerUnitMultiplier = 1.6f;
        shineImg.raycastTarget           = false;

        var cell = frameGO.AddComponent<PatternCell>();
        // Se pasa tambien el halo para que la celda lo encienda al revelarse
        cell.Initialize(index, bgImg, shineImg, glowImg);
        cell.OnClicked += HandleCellClicked;

        return cell;
    }

    public void GeneratePattern(int count, bool withDecoy = false)
    {
        _patternSet.Clear();
        _decoyIndex = -1;

        var pool = new List<int>(_cells.Count);
        for (int i = 0; i < _cells.Count; i++) pool.Add(i);

        for (int i = pool.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }

        int n = Mathf.Min(count, _cells.Count);
        for (int i = 0; i < n; i++)
            _patternSet.Add(pool[i]);

        // Celda señuelo: parpadea distinto pero no cuenta en el patrón.
        if (withDecoy && n < pool.Count)
            _decoyIndex = pool[n];
    }

    public void ShowPattern()
    {
        // Revelado escalonado: las celdas del patron se encienden una a una
        // (solo visual; el tiempo de memorizacion no cambia).
        if (_revealCo != null) StopCoroutine(_revealCo);
        _revealCo = StartCoroutine(ShowPatternStaggered());
    }

    private Coroutine _revealCo;

    private System.Collections.IEnumerator ShowPatternStaggered()
    {
        foreach (var cell in _cells)
            if (!_patternSet.Contains(cell.Index) && cell.Index != _decoyIndex)
                cell.SetState(PatternCell.CellState.Idle);

        var wait = new WaitForSeconds(0.07f);
        foreach (var cell in _cells)
        {
            if (!_patternSet.Contains(cell.Index)) continue;
            // SetState(PatternShow) ya incluye su propio pulso de escala
            cell.SetState(PatternCell.CellState.PatternShow);
            yield return wait;
        }

        if (_decoyIndex >= 0 && _decoyIndex < _cells.Count)
            _cells[_decoyIndex].SetState(PatternCell.CellState.Decoy);
        _revealCo = null;
    }

    public void HidePattern()
    {
        if (_revealCo != null) { StopCoroutine(_revealCo); _revealCo = null; }
        foreach (var cell in _cells)
            cell.SetState(PatternCell.CellState.Idle);
    }

    public void EnableInput(bool enable)
    {
        foreach (var cell in _cells)
            cell.EnableInput(enable);
    }

    public bool ValidateAnswer()
    {
        foreach (var cell in _cells)
            if (_patternSet.Contains(cell.Index) != cell.IsSelected)
                return false;
        return true;
    }

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

        }

        return (correct, wrong, missed);
    }

    public void ClearGrid()
    {
        for (int i = _cells.Count - 1; i >= 0; i--)
            if (_cells[i] != null) Destroy(_cells[i].gameObject);
        _cells.Clear();
        _patternSet.Clear();
        SelectedCount = 0;
    }

    private void HandleCellClicked(PatternCell cell)
    {
        GameFeel.PlayPop();
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
