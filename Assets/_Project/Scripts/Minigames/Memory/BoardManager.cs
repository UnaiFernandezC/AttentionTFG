// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BoardManager : MonoBehaviour
{

    [Header("Parámetros del tablero")]
    [Tooltip("Tiempo en segundos antes de ocultar una pareja incorrecta.")]
    public float flipBackDelay = 0.85f;

    private static readonly Color[] PALETTE = new Color[]
    {
        new Color(0.98f, 0.40f, 0.40f),
        new Color(0.30f, 0.82f, 0.76f),
        new Color(1.00f, 0.88f, 0.35f),
        new Color(0.62f, 0.58f, 1.00f),
        new Color(0.30f, 0.90f, 0.65f),
        new Color(0.43f, 0.71f, 1.00f),
        new Color(1.00f, 0.65f, 0.25f),
        new Color(0.98f, 0.44f, 0.68f),
        new Color(0.55f, 0.30f, 0.10f),
        new Color(0.80f, 0.80f, 0.85f),
        new Color(0.45f, 0.10f, 0.55f),
        new Color(0.10f, 0.45f, 0.25f),
    };

    private List<CardController> _allCards = new List<CardController>();
    private CardController _firstSelected;
    private CardController _secondSelected;
    private bool  _isComparing  = false;
    private int   _matchesFound = 0;
    private int   _numPairs     = 0;

    public System.Action<int> OnAttemptMade;

    /// <summary>Se dispara al resolver cada intento de pareja: (acierto, tiempo de reacción ms).</summary>
    public System.Action<bool, float> OnPairResolved;

    public System.Action OnAllMatched;

    private int   _totalAttempts = 0;
    private float _attemptStart  = -1f;

    public void Initialize(RectTransform container, int numPairs, float cardSize = 120f, float spacing = 12f)
    {
        _numPairs = Mathf.Clamp(numPairs, 2, PALETTE.Length);

        var colorIds = new List<int>();
        for (int i = 0; i < _numPairs; i++)
        {
            colorIds.Add(i);
            colorIds.Add(i);
        }

        for (int i = colorIds.Count - 1; i > 0; i--)
        {
            int rand = Random.Range(0, i + 1);
            (colorIds[i], colorIds[rand]) = (colorIds[rand], colorIds[i]);
        }

        int totalCards = colorIds.Count;

        int cols, rows;
        CalculateBestGrid(totalCards, out cols, out rows);

        float totalWidth  = cols * cardSize + (cols - 1) * spacing;
        float totalHeight = rows * cardSize + (rows - 1) * spacing;
        container.sizeDelta = new Vector2(totalWidth, totalHeight);

        for (int i = 0; i < totalCards; i++)
        {
            int col = i % cols;
            int row = i / cols;

            CardController card = CreateCard(container, colorIds[i], col, row, cardSize, spacing);
            _allCards.Add(card);
        }
    }

    private CardController CreateCard(RectTransform container, int colorId,
                                      int col, int row, float cardSize, float spacing)
    {

        var cardGO = new GameObject($"Card_{colorId}_{col}_{row}");
        cardGO.transform.SetParent(container, false);

        var cardRT = cardGO.AddComponent<RectTransform>();
        cardRT.sizeDelta = new Vector2(cardSize, cardSize);

        float x = col * (cardSize + spacing) - (container.sizeDelta.x - cardSize) * 0.5f;
        float y = -row * (cardSize + spacing) + (container.sizeDelta.y - cardSize) * 0.5f;
        cardRT.anchoredPosition = new Vector2(x, y);

        cardGO.AddComponent<Button>();

        var card = cardGO.AddComponent<CardController>();

        var frameGO = CreatePanel(cardGO.transform, "Frame",
            new Color(0.08f, 0.08f, 0.16f),
            Vector2.zero, new Vector2(cardSize, cardSize));

        var backGO = CreatePanel(cardGO.transform, "Back",
            new Color(0.20f, 0.21f, 0.38f),
            Vector2.zero, new Vector2(cardSize - 6f, cardSize - 6f));

        CreatePanel(backGO.transform, "BackShine",
            new Color(1f, 1f, 1f, 0.06f),
            new Vector2(-(cardSize - 6f) * 0.25f, (cardSize - 6f) * 0.25f),
            new Vector2((cardSize - 6f) * 0.5f, (cardSize - 6f) * 0.5f));

        AddCenteredText(backGO.transform, "?",
            new Color(0.65f, 0.68f, 0.90f), cardSize * 0.40f);

        var frontGO = CreatePanel(cardGO.transform, "Front",
            PALETTE[colorId],
            Vector2.zero, new Vector2(cardSize - 6f, cardSize - 6f));

        CreatePanel(frontGO.transform, "Shine",
            new Color(1f, 1f, 1f, 0.22f),
            new Vector2(0f, (cardSize - 6f) * 0.28f),
            new Vector2(cardSize - 6f, (cardSize - 6f) * 0.44f));

        CreatePanel(frontGO.transform, "Shadow",
            new Color(0f, 0f, 0f, 0.18f),
            new Vector2(0f, -(cardSize - 6f) * 0.30f),
            new Vector2(cardSize - 6f, (cardSize - 6f) * 0.40f));

        card.Initialize(colorId, PALETTE[colorId],
                        backGO.GetComponent<Image>(),
                        frontGO.GetComponent<Image>());

        card.OnCardClicked += HandleCardClicked;

        return card;
    }

    private void HandleCardClicked(CardController card)
    {
        if (_isComparing || card.IsMatched || card.IsRevealed) return;

        GameFeel.PlayPop();
        card.FlipReveal();

        if (_firstSelected == null)
        {
            _firstSelected = card;
            _attemptStart  = Time.time;
        }
        else if (_secondSelected == null && card != _firstSelected)
        {
            _secondSelected = card;
            _isComparing    = true;
            _totalAttempts++;
            OnAttemptMade?.Invoke(_totalAttempts);
            StartCoroutine(CompareCards());
        }
    }

    private IEnumerator CompareCards()
    {

        yield return new WaitForSeconds(0.28f);

        float rtMs = _attemptStart >= 0f ? (Time.time - _attemptStart) * 1000f : -1f;
        bool  match = _firstSelected.ColorId == _secondSelected.ColorId;
        OnPairResolved?.Invoke(match, rtMs);

        if (match)
        {
            GameFeel.PlaySuccess();

            _firstSelected.SetMatched();
            _secondSelected.SetMatched();
            _matchesFound++;

            _firstSelected  = null;
            _secondSelected = null;
            _isComparing    = false;

            if (_matchesFound >= _numPairs)
            {
                yield return new WaitForSeconds(0.35f);
                OnAllMatched?.Invoke();
            }
        }
        else
        {
            GameFeel.PlayError();
            var rt1 = _firstSelected.GetComponent<RectTransform>();
            var rt2 = _secondSelected.GetComponent<RectTransform>();
            GameFeel.Shake(rt1, 8f, 0.28f);
            GameFeel.Shake(rt2, 8f, 0.28f);

            yield return new WaitForSeconds(flipBackDelay);

            _firstSelected.FlipHide();
            _secondSelected.FlipHide();

            _firstSelected  = null;
            _secondSelected = null;

            yield return new WaitForSeconds(0.28f);
            _isComparing = false;
        }
    }

    /// <summary>Vista previa: muestra todas las cartas y las vuelve a tapar (modo fácil).</summary>
    public IEnumerator PreviewAll(float seconds)
    {
        _isComparing = true;   // bloquea la entrada mientras dura la vista previa

        foreach (var card in _allCards)
            if (card != null) card.FlipReveal();

        yield return new WaitForSeconds(seconds + 0.3f);

        foreach (var card in _allCards)
            if (card != null) card.FlipHide();

        yield return new WaitForSeconds(0.3f);
        _isComparing = false;
    }

    private GameObject CreatePanel(Transform parent, string name, Color color,
                                   Vector2 anchoredPos, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var rt  = go.AddComponent<RectTransform>();
        rt.anchorMin       = new Vector2(0.5f, 0.5f);
        rt.anchorMax       = new Vector2(0.5f, 0.5f);
        rt.pivot           = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta       = size;

        var img  = go.AddComponent<Image>();
        img.color = color;

        return go;
    }

    private void AddCenteredText(Transform parent, string content, Color color, float fontSize)
    {
        var go = new GameObject("Label");
        go.transform.SetParent(parent, false);

        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin  = Vector2.zero;
        rt.anchorMax  = Vector2.one;
        rt.sizeDelta  = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;

        try
        {
            var tmp = go.AddComponent<TMPro.TextMeshProUGUI>();
            tmp.text      = content;
            tmp.color     = color;
            tmp.fontSize  = fontSize;
            tmp.alignment = TMPro.TextAlignmentOptions.Center;
            tmp.fontStyle = TMPro.FontStyles.Bold;
        }
        catch
        {
            var txt = go.AddComponent<Text>();
            txt.text      = content;
            txt.color     = color;
            txt.fontSize  = (int)fontSize;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.fontStyle = FontStyle.Bold;
        }
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

    public void ClearBoard()
    {
        foreach (var card in _allCards)
        {
            if (card != null)
                Destroy(card.gameObject);
        }
        _allCards.Clear();
        _firstSelected  = null;
        _secondSelected = null;
        _isComparing    = false;
        _matchesFound   = 0;
        _totalAttempts  = 0;
    }
}
