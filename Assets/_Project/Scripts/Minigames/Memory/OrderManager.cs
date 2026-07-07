// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tablero de numeros desordenados. Admite cualquier conjunto de valores,
/// orden ascendente o descendente y numeros "ya colocados" (ayuda visual).
/// </summary>
public class OrderManager : MonoBehaviour
{

    [Header("Parámetros de la cuadrícula")]
    [Tooltip("Tiempo en segundos que el flash de error permanece visible.")]
    public float wrongFlashDelay = 0.55f;

    private List<NumberButton> _allButtons = new List<NumberButton>();
    private List<int> _expectedOrder = new List<int>();
    private int  _nextIdx      = 0;
    private bool _isLocked     = false;
    private int  _wrongCount   = 0;
    private int  _correctCount = 0;

    /// <summary>Se invoca tras cada acierto con el SIGUIENTE valor esperado (-1 si no queda).</summary>
    public System.Action<int> OnCorrectPress;

    /// <summary>Se invoca tras cada fallo con el total de fallos.</summary>
    public System.Action<int> OnWrongPress;

    /// <summary>Se invoca al completar todos los numeros.</summary>
    public System.Action OnComplete;

    public int WrongCount   => _wrongCount;
    public int CorrectCount => _correctCount;
    public int NextExpectedValue => _nextIdx < _expectedOrder.Count ? _expectedOrder[_nextIdx] : -1;

    /// <summary>
    /// Crea el tablero. <paramref name="values"/> son los numeros a mostrar;
    /// el orden a pulsar es ascendente o descendente segun <paramref name="descending"/>.
    /// Los primeros <paramref name="prePlaced"/> del orden esperado aparecen ya resueltos.
    /// </summary>
    public void Initialize(RectTransform container, int[] values, bool descending = false,
                           int prePlaced = 0, float btnSize = 130f, float spacing = 14f)
    {
        _expectedOrder = new List<int>(values);
        _expectedOrder.Sort();
        if (descending) _expectedOrder.Reverse();

        _nextIdx      = 0;
        _wrongCount   = 0;
        _correctCount = 0;
        _isLocked     = false;

        // Posiciones barajadas en la cuadricula
        var shuffled = new List<int>(values);
        for (int i = shuffled.Count - 1; i > 0; i--)
        {
            int rand = Random.Range(0, i + 1);
            (shuffled[i], shuffled[rand]) = (shuffled[rand], shuffled[i]);
        }

        int cols, rows;
        CalculateBestGrid(shuffled.Count, out cols, out rows);

        float totalWidth  = cols * btnSize + (cols - 1) * spacing;
        float totalHeight = rows * btnSize + (rows - 1) * spacing;
        container.sizeDelta = new Vector2(totalWidth, totalHeight);

        for (int i = 0; i < shuffled.Count; i++)
        {
            int col = i % cols;
            int row = i / cols;
            NumberButton btn = CreateButton(container, shuffled[i], col, row, btnSize, spacing);
            _allButtons.Add(btn);
        }

        // Numeros ya colocados (los primeros del orden esperado)
        prePlaced = Mathf.Clamp(prePlaced, 0, _expectedOrder.Count - 1);
        for (int i = 0; i < prePlaced; i++)
        {
            NumberButton b = FindButton(_expectedOrder[i]);
            if (b == null) continue;
            b.SetPrePlaced();
            _nextIdx++;
        }
    }

    private NumberButton FindButton(int value)
    {
        foreach (var b in _allButtons)
            if (b != null && b.Number == value) return b;
        return null;
    }

    private NumberButton CreateButton(RectTransform container, int number,
                                      int col, int row, float btnSize, float spacing)
    {

        var go = new GameObject($"NumBtn_{number}");
        go.transform.SetParent(container, false);

        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(btnSize, btnSize);

        float x = col * (btnSize + spacing) - (container.sizeDelta.x - btnSize) * 0.5f;
        float y = -row * (btnSize + spacing) + (container.sizeDelta.y - btnSize) * 0.5f;
        rt.anchoredPosition = new Vector2(x, y);

        go.AddComponent<Button>();

        var numBtn = go.AddComponent<NumberButton>();

        CreatePanel(go.transform, "Frame",
            new Color(0.08f, 0.08f, 0.18f),
            Vector2.zero, new Vector2(btnSize, btnSize));

        var bg = CreatePanel(go.transform, "BG",
            new Color(0.20f, 0.22f, 0.40f),
            Vector2.zero, new Vector2(btnSize - 6f, btnSize - 6f));

        CreatePanel(bg.transform, "Shine",
            new Color(1f, 1f, 1f, 0.10f),
            new Vector2(-(btnSize - 6f) * 0.22f, (btnSize - 6f) * 0.22f),
            new Vector2((btnSize - 6f) * 0.55f, (btnSize - 6f) * 0.55f));

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

        numBtn.Initialize(number, bg.GetComponent<Image>(), tmp);
        numBtn.OnClicked += HandleButtonClicked;

        return numBtn;
    }

    private void HandleButtonClicked(NumberButton btn)
    {
        if (_isLocked || _nextIdx >= _expectedOrder.Count) return;

        if (btn.Number == _expectedOrder[_nextIdx])
        {

            btn.SetCorrect();
            _correctCount++;
            _nextIdx++;
            OnCorrectPress?.Invoke(NextExpectedValue);

            if (_nextIdx >= _expectedOrder.Count)
            {
                StartCoroutine(DelayedComplete());
            }
        }
        else
        {

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

    public void ClearBoard()
    {
        foreach (var btn in _allButtons)
        {
            if (btn != null)
                Destroy(btn.gameObject);
        }
        _allButtons.Clear();
        _expectedOrder.Clear();
        _nextIdx      = 0;
        _wrongCount   = 0;
        _correctCount = 0;
        _isLocked     = false;
    }

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
