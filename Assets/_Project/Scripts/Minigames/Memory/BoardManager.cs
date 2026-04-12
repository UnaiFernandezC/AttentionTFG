using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Crea el tablero de cartas dinámicamente y gestiona toda la lógica de selección:
/// comparación de parejas, bloqueo temporal y detección de victoria.
/// </summary>
public class BoardManager : MonoBehaviour
{
    // ─── Configuración ─────────────────────────────────────────────────────
    [Header("Parámetros del tablero")]
    [Tooltip("Tiempo en segundos antes de ocultar una pareja incorrecta.")]
    public float flipBackDelay = 0.85f;

    // ─── Paleta de colores (8 pares posibles) ─────────────────────────────
    /// <summary>
    /// Lista de colores disponibles para las parejas.
    /// Puedes cambiar estos valores para personalizar la paleta visual.
    /// </summary>
    private static readonly Color[] PALETTE = new Color[]
    {
        new Color(0.98f, 0.40f, 0.40f),   // 0 Coral
        new Color(0.30f, 0.82f, 0.76f),   // 1 Turquesa
        new Color(1.00f, 0.88f, 0.35f),   // 2 Amarillo
        new Color(0.62f, 0.58f, 1.00f),   // 3 Lavanda
        new Color(0.30f, 0.90f, 0.65f),   // 4 Menta
        new Color(0.43f, 0.71f, 1.00f),   // 5 Azul cielo
        new Color(1.00f, 0.65f, 0.25f),   // 6 Naranja
        new Color(0.98f, 0.44f, 0.68f),   // 7 Rosa
    };

    // ─── Estado interno ────────────────────────────────────────────────────
    private List<CardController> _allCards = new List<CardController>();
    private CardController _firstSelected;
    private CardController _secondSelected;
    private bool  _isComparing  = false;
    private int   _matchesFound = 0;
    private int   _numPairs     = 0;

    // ─── Eventos ───────────────────────────────────────────────────────────
    /// <summary>Se dispara cada vez que el jugador intenta una pareja (arg: intentos totales).</summary>
    public System.Action<int> OnAttemptMade;

    /// <summary>Se dispara cuando se encuentran todas las parejas.</summary>
    public System.Action OnAllMatched;

    private int _totalAttempts = 0;

    // ─── API pública ───────────────────────────────────────────────────────

    /// <summary>
    /// Inicializa el tablero: crea las cartas, las mezcla y las coloca en un grid.
    /// </summary>
    /// <param name="container">RectTransform padre donde se colocarán las cartas.</param>
    /// <param name="numPairs">Número de parejas (2-8).</param>
    /// <param name="cardSize">Tamaño de cada carta en píxeles.</param>
    /// <param name="spacing">Espacio entre cartas en píxeles.</param>
    public void Initialize(RectTransform container, int numPairs, float cardSize = 120f, float spacing = 12f)
    {
        _numPairs = Mathf.Clamp(numPairs, 2, PALETTE.Length);

        // Crear lista de colores: cada color aparece 2 veces
        var colorIds = new List<int>();
        for (int i = 0; i < _numPairs; i++)
        {
            colorIds.Add(i);
            colorIds.Add(i);
        }

        // Mezcla Fisher-Yates
        for (int i = colorIds.Count - 1; i > 0; i--)
        {
            int rand = Random.Range(0, i + 1);
            (colorIds[i], colorIds[rand]) = (colorIds[rand], colorIds[i]);
        }

        // Calcular grid: preferir ancho > alto, cuadrado si posible
        int totalCards = colorIds.Count;
        // Buscar la distribución más cuadrada donde NO queden celdas vacías.
        // Iterar por factores exactos de totalCards, preferir cols >= rows.
        int cols, rows;
        CalculateBestGrid(totalCards, out cols, out rows);

        // Ajustar el container para que el grid quepa perfectamente
        float totalWidth  = cols * cardSize + (cols - 1) * spacing;
        float totalHeight = rows * cardSize + (rows - 1) * spacing;
        container.sizeDelta = new Vector2(totalWidth, totalHeight);

        // Crear cartas
        for (int i = 0; i < totalCards; i++)
        {
            int col = i % cols;
            int row = i / cols;

            CardController card = CreateCard(container, colorIds[i], col, row, cardSize, spacing);
            _allCards.Add(card);
        }
    }

    // ─── Creación de cartas ────────────────────────────────────────────────

    private CardController CreateCard(RectTransform container, int colorId,
                                      int col, int row, float cardSize, float spacing)
    {
        // ── Root de la carta ──
        var cardGO = new GameObject($"Card_{colorId}_{col}_{row}");
        cardGO.transform.SetParent(container, false);

        var cardRT = cardGO.AddComponent<RectTransform>();
        cardRT.sizeDelta = new Vector2(cardSize, cardSize);

        // Posición en el grid (origen arriba-izquierda del container)
        float x = col * (cardSize + spacing) - (container.sizeDelta.x - cardSize) * 0.5f;
        float y = -row * (cardSize + spacing) + (container.sizeDelta.y - cardSize) * 0.5f;
        cardRT.anchoredPosition = new Vector2(x, y);

        // Añadir Button (necesario para CardController)
        cardGO.AddComponent<Button>();

        // Añadir CardController
        var card = cardGO.AddComponent<CardController>();

        // ── Marco exterior (borde oscuro de la carta) ──
        var frameGO = CreatePanel(cardGO.transform, "Frame",
            new Color(0.08f, 0.08f, 0.16f),
            Vector2.zero, new Vector2(cardSize, cardSize));

        // ── Panel trasero (reverso de la carta) ──
        var backGO = CreatePanel(cardGO.transform, "Back",
            new Color(0.20f, 0.21f, 0.38f),
            Vector2.zero, new Vector2(cardSize - 6f, cardSize - 6f));

        // Brillo sutil en la esquina superior-izquierda del reverso
        CreatePanel(backGO.transform, "BackShine",
            new Color(1f, 1f, 1f, 0.06f),
            new Vector2(-(cardSize - 6f) * 0.25f, (cardSize - 6f) * 0.25f),
            new Vector2((cardSize - 6f) * 0.5f, (cardSize - 6f) * 0.5f));

        // Símbolo "?" en el reverso
        AddCenteredText(backGO.transform, "?",
            new Color(0.65f, 0.68f, 0.90f), cardSize * 0.40f);

        // ── Panel frontal (color de la pareja) ──
        var frontGO = CreatePanel(cardGO.transform, "Front",
            PALETTE[colorId],
            Vector2.zero, new Vector2(cardSize - 6f, cardSize - 6f));

        // Brillo sutil en esquina superior
        CreatePanel(frontGO.transform, "Shine",
            new Color(1f, 1f, 1f, 0.22f),
            new Vector2(0f, (cardSize - 6f) * 0.28f),
            new Vector2(cardSize - 6f, (cardSize - 6f) * 0.44f));

        // Sombra interior inferior
        CreatePanel(frontGO.transform, "Shadow",
            new Color(0f, 0f, 0f, 0.18f),
            new Vector2(0f, -(cardSize - 6f) * 0.30f),
            new Vector2(cardSize - 6f, (cardSize - 6f) * 0.40f));

        // Inicializar la carta con sus referencias visuales
        card.Initialize(colorId, PALETTE[colorId],
                        backGO.GetComponent<Image>(),
                        frontGO.GetComponent<Image>());

        // Suscribirse al click
        card.OnCardClicked += HandleCardClicked;

        return card;
    }

    // ─── Lógica de selección ───────────────────────────────────────────────

    private void HandleCardClicked(CardController card)
    {
        if (_isComparing || card.IsMatched || card.IsRevealed) return;

        card.FlipReveal();

        if (_firstSelected == null)
        {
            _firstSelected = card;
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
        // Esperar a que termine la animación de giro antes de comparar
        yield return new WaitForSeconds(0.28f);

        if (_firstSelected.ColorId == _secondSelected.ColorId)
        {
            // ✅ PAREJA CORRECTA
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
            // ❌ PAREJA INCORRECTA → esperar y ocultar
            yield return new WaitForSeconds(flipBackDelay);

            _firstSelected.FlipHide();
            _secondSelected.FlipHide();

            _firstSelected  = null;
            _secondSelected = null;

            // Esperar a que termine la animación antes de permitir otra selección
            yield return new WaitForSeconds(0.28f);
            _isComparing = false;
        }
    }

    // ─── Helpers de construcción de UI ────────────────────────────────────

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

        // Intentar añadir TextMeshProUGUI; si no está disponible, usar Text legacy
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

    // ─── Cálculo de grid ──────────────────────────────────────────────────

    /// <summary>
    /// Devuelve la distribución de columnas y filas más cuadrada posible
    /// sin dejar celdas vacías (solo usa divisores exactos de totalCards).
    ///
    /// Ejemplos:
    ///   8  cartas → 4 cols × 2 filas
    ///   12 cartas → 4 cols × 3 filas
    ///   16 cartas → 4 cols × 4 filas
    ///   10 cartas → 5 cols × 2 filas
    /// </summary>
    private static void CalculateBestGrid(int total, out int cols, out int rows)
    {
        cols = total;
        rows = 1;

        // Recorrer factores desde la raíz cuadrada hacia abajo
        for (int c = Mathf.FloorToInt(Mathf.Sqrt(total)); c >= 1; c--)
        {
            if (total % c == 0)
            {
                rows = c;
                cols = total / c;
                // Asegurar que cols >= rows (tablero más ancho que alto)
                if (cols < rows) { int tmp = cols; cols = rows; rows = tmp; }
                return;
            }
        }
    }

    // ─── Reset del juego ───────────────────────────────────────────────────

    /// <summary>Destruye todas las cartas actuales (para reiniciar).</summary>
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
