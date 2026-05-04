using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OrderManager : MonoBehaviour
{

    [Header("Parámetros de la cuadrícula")]
    [Tooltip("Tiempo en segundos que el flash de error permanece visible.")]
    public float wrongFlashDelay = 0.55f;

    private static readonly Color[] NUMBER_PALETTE = new Color[]
    {
        new Color(0.98f, 0.40f, 0.40f),
        new Color(0.30f, 0.82f, 0.76f),
        new Color(1.00f, 0.88f, 0.35f),
        new Color(0.62f, 0.58f, 1.00f),
        new Color(0.30f, 0.90f, 0.65f),
        new Color(0.43f, 0.71f, 1.00f),
        new Color(1.00f, 0.65f, 0.25f),
        new Color(0.98f, 0.44f, 0.68f),
        new Color(0.55f, 0.90f, 0.35f),
        new Color(0.80f, 0.50f, 1.00f),
    };

    private List<NumberButton> _allButtons = new List<NumberButton>();
    private int  _nextExpected  = 1;
    private int  _totalNumbers  = 0;
    private bool _isLocked      = false;
    private int  _wrongCount    = 0;
    private int  _correctCount  = 0;

    public System.Action<int> OnCorrectPress;

    public System.Action<int> OnWrongPress;

    public System.Action OnComplete;

    public int WrongCount   => _wrongCount;
    public int CorrectCount => _correctCount;

    public void Initialize(RectTransform container, int numCount, float btnSize = 130f, float spacing = 14f)
    {
        _totalNumbers = Mathf.Clamp(numCount, 2, 10);

        var numbers = new List<int>();
        for (int i = 1; i <= _totalNumbers; i++)
            numbers.Add(i);

        for (int i = numbers.Count - 1; i > 0; i--)
        {
            int rand = Random.Range(0, i + 1);
            (numbers[i], numbers[rand]) = (numbers[rand], numbers[i]);
        }

        int cols, rows;
        CalculateBestGrid(_totalNumbers, out cols, out rows);

        float totalWidth  = cols * btnSize + (cols - 1) * spacing;
        float totalHeight = rows * btnSize + (rows - 1) * spacing;
        container.sizeDelta = new Vector2(totalWidth, totalHeight);

        for (int i = 0; i < _totalNumbers; i++)
        {
            int col = i % cols;
            int row = i / cols;
            NumberButton btn = CreateButton(container, numbers[i], col, row, btnSize, spacing);
            _allButtons.Add(btn);
        }
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

        var frame = CreatePanel(go.transform, "Frame",
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
        if (_isLocked) return;

        if (btn.Number == _nextExpected)
        {

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
        _nextExpected = 1;
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
