using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PatternGridManager : MonoBehaviour
{

    private readonly List<PatternCell>  _cells      = new List<PatternCell>();
    private readonly HashSet<int>       _patternSet = new HashSet<int>();
    private int _cols, _rows;

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

                _cells.Add(CreateCell(container, idx, new Vector2(x, y), cellSize));
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
        frameGO.AddComponent<Image>().color = new Color(0.06f, 0.07f, 0.15f);

        frameGO.AddComponent<Button>();

        var bgGO = new GameObject("BG");
        bgGO.transform.SetParent(frameGO.transform, false);
        var bgRT = bgGO.AddComponent<RectTransform>();
        bgRT.anchorMin        = Vector2.zero;
        bgRT.anchorMax        = Vector2.one;
        bgRT.anchoredPosition = Vector2.zero;
        bgRT.sizeDelta        = new Vector2(-7f, -7f);
        var bgImg = bgGO.AddComponent<Image>();
        bgImg.color = new Color(0.18f, 0.20f, 0.36f);

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

    public void GeneratePattern(int count)
    {
        _patternSet.Clear();

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
    }

    public void ShowPattern()
    {
        foreach (var cell in _cells)
            cell.SetState(_patternSet.Contains(cell.Index)
                ? PatternCell.CellState.PatternShow
                : PatternCell.CellState.Idle);
    }

    public void HidePattern()
    {
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
