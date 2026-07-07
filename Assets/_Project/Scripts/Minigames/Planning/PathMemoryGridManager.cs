// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PathMemoryGridManager : MonoBehaviour
{
    public enum CellState
    {
        Normal,
        Start,
        Goal,
        Route,
        PlayerCorrect,
        PlayerWrong,
        PlayerCurrent,
        Blocked,
        Detour
    }

    const float CellSize = 96f;

    static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

    static readonly Color ColFloor   = C(0.13f, 0.17f, 0.24f);
    static readonly Color ColStart   = C(0.22f, 0.52f, 0.90f);
    static readonly Color ColGoal    = C(0.92f, 0.78f, 0.06f);
    static readonly Color ColRoute   = C(0.95f, 0.56f, 0.13f);
    static readonly Color ColCorrect = C(0.18f, 0.78f, 0.38f);
    static readonly Color ColWrong   = C(0.92f, 0.22f, 0.22f);
    static readonly Color ColCurrent = C(0.62f, 0.88f, 1.00f);
    static readonly Color ColBlocked = C(0.42f, 0.14f, 0.18f);
    static readonly Color ColDetour  = C(0.45f, 0.62f, 0.85f);
    static readonly Color ColBorder  = C(0.06f, 0.08f, 0.12f);

    Canvas        _canvas;
    RectTransform _gridRoot;
    int           _cols, _rows;
    bool          _inputEnabled;

    readonly Dictionary<Vector2Int, Image>           _cellInnerImages = new();
    readonly Dictionary<Vector2Int, TextMeshProUGUI> _cellLabels      = new();

    public event Action<Vector2Int> CellClicked;

    public Canvas        GridCanvas => _canvas;
    public RectTransform GridRoot   => _gridRoot;
    public int           Cols       => _cols;
    public int           Rows       => _rows;

    public void BuildGrid(int rows, int cols)
    {
        _rows = rows; _cols = cols;
        _inputEnabled = false;
        _cellInnerImages.Clear();
        _cellLabels.Clear();

        _canvas = new GameObject("PathGridCanvas").AddComponent<Canvas>();
        _canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 3;
        var sc = _canvas.gameObject.AddComponent<CanvasScaler>();
        sc.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920f, 1080f);
        sc.matchWidthOrHeight  = 0.5f;
        _canvas.gameObject.AddComponent<GraphicRaycaster>();

        _gridRoot = new GameObject("GridRoot").AddComponent<RectTransform>();
        _gridRoot.SetParent(_canvas.transform, false);
        _gridRoot.anchorMin        = new Vector2(0.5f, 0.5f);
        _gridRoot.anchorMax        = new Vector2(0.5f, 0.5f);
        _gridRoot.pivot            = new Vector2(0.5f, 0.5f);
        float W = cols * CellSize;
        float H = rows * CellSize;
        _gridRoot.sizeDelta        = new Vector2(W, H);
        _gridRoot.anchoredPosition = new Vector2(0f, 30f);

        for (int row = 0; row < rows; row++)
            for (int col = 0; col < cols; col++)
                BuildCell(new Vector2Int(col, row), W, H);
    }

    void BuildCell(Vector2Int pos, float W, float H)
    {

        var go = new GameObject($"Cell_{pos.x}_{pos.y}");
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(_gridRoot, false);
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = new Vector2(CellSize - 3f, CellSize - 3f);
        rt.anchoredPosition = CellLocalPos(pos, W, H);

        var borderImg = go.AddComponent<Image>();
        borderImg.color = ColBorder;

        var inner = new GameObject("Fill");
        var irt   = inner.AddComponent<RectTransform>();
        irt.SetParent(rt, false);
        irt.anchorMin = Vector2.zero; irt.anchorMax = Vector2.one;
        irt.offsetMin = new Vector2(3f,  3f);
        irt.offsetMax = new Vector2(-3f, -3f);
        var fillImg = inner.AddComponent<Image>();
        fillImg.color = ColFloor;
        _cellInnerImages[pos] = fillImg;

        var lblGo = new GameObject("Lbl");
        var lrt   = lblGo.AddComponent<RectTransform>();
        lrt.SetParent(rt, false);
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
        lrt.sizeDelta = Vector2.zero; lrt.anchoredPosition = Vector2.zero;
        var tmp = lblGo.AddComponent<TextMeshProUGUI>();
        tmp.text = ""; tmp.fontSize = 30; tmp.color = Color.white;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.overflowMode = TextOverflowModes.Overflow;
        _cellLabels[pos] = tmp;

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = borderImg;
        btn.transition    = Selectable.Transition.None;
        Vector2Int p = pos;
        btn.onClick.AddListener(() => { if (_inputEnabled) CellClicked?.Invoke(p); });
    }

    public void SetCellState(Vector2Int pos, CellState state, string label = "")
    {
        if (_cellInnerImages.TryGetValue(pos, out var img))
            img.color = StateColor(state);

        if (_cellLabels.TryGetValue(pos, out var lbl))
        {
            lbl.text  = label;
            lbl.color = LabelColor(state);
            lbl.fontSize = (state == CellState.Route) ? 26f : 34f;
        }
    }

    public void ResetAllCells()
    {
        foreach (var kv in _cellInnerImages)
        {
            kv.Value.color = ColFloor;
            if (_cellLabels.TryGetValue(kv.Key, out var lbl)) lbl.text = "";
        }
    }

    public void SetInputEnabled(bool enabled) => _inputEnabled = enabled;

    Vector2 CellLocalPos(Vector2Int pos, float W, float H) =>
        new Vector2(
            -W / 2f + pos.x * CellSize + CellSize / 2f,
             H / 2f - pos.y * CellSize - CellSize / 2f);

    public Vector2 CanvasAnchoredPos(Vector2Int pos)
    {
        float W = _cols * CellSize;
        float H = _rows * CellSize;
        return CellLocalPos(pos, W, H) + _gridRoot.anchoredPosition;
    }

    public bool IsInBounds(Vector2Int pos) =>
        pos.x >= 0 && pos.x < _cols && pos.y >= 0 && pos.y < _rows;

    static Color StateColor(CellState s)
    {
        switch (s)
        {
            case CellState.Start:         return ColStart;
            case CellState.Goal:          return ColGoal;
            case CellState.Route:         return ColRoute;
            case CellState.PlayerCorrect: return ColCorrect;
            case CellState.PlayerWrong:   return ColWrong;
            case CellState.PlayerCurrent: return ColCurrent;
            case CellState.Blocked:       return ColBlocked;
            case CellState.Detour:        return ColDetour;
            default:                      return ColFloor;
        }
    }

    static Color LabelColor(CellState s)
    {
        switch (s)
        {
            case CellState.Goal: return C(0.15f, 0.08f, 0f);
            default:             return Color.white;
        }
    }
}
