using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Interfaz del minijuego Camino Laser.
/// Usa solo caracteres ASCII estandar (sin emojis ni Unicode especial).
/// Disenado para ninos: colores vivos, texto grande y claro.
/// </summary>
public class LaserUIController : MonoBehaviour
{
    // -- Colores ---------------------------------------------------------------
    static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);
    static Vector2 V(float x, float y) => new Vector2(x, y);

    // Fondo de pantalla azul oscuro
    static readonly Color BG        = C(0.07f, 0.10f, 0.18f);
    // Cabecera
    static readonly Color HDR       = C(0.04f, 0.07f, 0.14f);
    // Celda vacia
    static readonly Color CELL_BG   = C(0.14f, 0.20f, 0.34f);
    static readonly Color CELL_BRD  = C(0.22f, 0.30f, 0.48f);
    // Laser (amarillo brillante)
    static readonly Color LASER_COL = C(1.00f, 0.92f, 0.05f);
    static readonly Color LASER_BRD = C(0.55f, 0.50f, 0.00f);
    // Emisor (verde)
    static readonly Color EMIT_BG   = C(0.08f, 0.58f, 0.18f);
    static readonly Color EMIT_BRD  = C(0.04f, 0.36f, 0.10f);
    // Objetivo (rojo)
    static readonly Color TARGET_BG = C(0.82f, 0.10f, 0.10f);
    static readonly Color TARGET_BRD= C(0.55f, 0.05f, 0.05f);
    // Espejo ROTABLE -- naranja intenso, muy diferente a todo lo demas
    static readonly Color MIR_BG    = C(0.88f, 0.42f, 0.00f);
    static readonly Color MIR_BRD   = C(1.00f, 0.75f, 0.20f);   // borde dorado brillante
    static readonly Color MIR_TXT   = Color.white;
    static readonly Color MIR_HINT  = C(1.00f, 0.90f, 0.65f);   // texto "GIRAR" claro
    // Espejo FIJO (gris, no clickable)
    static readonly Color FIX_BG    = C(0.28f, 0.30f, 0.38f);
    static readonly Color FIX_BRD   = C(0.18f, 0.20f, 0.26f);
    static readonly Color FIX_TXT   = C(0.65f, 0.70f, 0.80f);
    // Pared
    static readonly Color WALL_BG   = C(0.22f, 0.22f, 0.28f);
    // Acento cabecera
    static readonly Color ACCENT    = C(0.30f, 0.75f, 1.00f);
    // Timer
    static readonly Color TIMER_OK  = C(0.30f, 0.90f, 0.40f);
    static readonly Color TIMER_WARN= C(1.00f, 0.75f, 0.10f);
    static readonly Color TIMER_BAD = C(1.00f, 0.28f, 0.28f);
    // Resultado
    static readonly Color WIN_C     = C(0.20f, 0.85f, 0.40f);
    static readonly Color LOSE_C    = C(0.90f, 0.25f, 0.25f);
    static readonly Color PANEL_C   = C(0.08f, 0.12f, 0.22f);

    // -- Elementos UI ---------------------------------------------------------
    Image[,]           _cellBorder;   // borde exterior
    Image[,]           _cellFill;     // relleno principal (color del tipo de celda)
    Image[,]           _cellLaser;    // overlay laser amarillo
    TextMeshProUGUI[,] _cellSym;      // simbolo grande: > < ^ v / \ META
    TextMeshProUGUI[,] _cellHint;     // etiqueta pequena bajo el simbolo (ej. "GIRAR")

    float _baseFontSize;

    TextMeshProUGUI _timerLbl;
    TextMeshProUGUI _puzzleLbl;
    TextMeshProUGUI _hintLbl;
    TextMeshProUGUI _flashLbl;
    GameObject      _flashPanel;
    GameObject      _resultPanel;
    TextMeshProUGUI _resultTitle;
    TextMeshProUGUI _resultSub;

    int _rows, _cols;

    public event Action<int, int> OnCellClicked;

    // -- Construccion principal ------------------------------------------------
    public void BuildUI(int rows, int cols, Action onRestart, Action onMenu)
    {
        _rows = rows;
        _cols = cols;

        // Canvas
        var cGO = new GameObject("Canvas_Laser");
        cGO.transform.SetParent(transform, false);
        var cv = cGO.AddComponent<Canvas>();
        cv.renderMode   = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 5;
        var sc = cGO.AddComponent<CanvasScaler>();
        sc.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920f, 1080f);
        sc.matchWidthOrHeight  = 0.5f;
        cGO.AddComponent<GraphicRaycaster>();
        var R = cGO.GetComponent<RectTransform>();

        // Fondo
        Img(R, "BG", BG, V(0,0), V(1,1), V(0,0), V(0,0));

        // Cabecera
        var hdr = Img(R, "Hdr", HDR, V(0,1), V(1,1), V(0,-44f), V(0,88f));
        Img(hdr, "HdrLine", ACCENT, V(0,0), V(1,0), V(0,1.5f), V(0,3f));

        var ttl = Txt(hdr, "Titulo", "CAMINO LASER", Color.white, 36,
                      V(0.03f,0.10f), V(0.65f,0.90f));
        ttl.fontStyle = FontStyles.Bold;
        ttl.alignment = TextAlignmentOptions.MidlineLeft;

        Txt(hdr, "Cat", "ATENCION", ACCENT, 18,
            V(0.65f,0.10f), V(0.97f,0.90f)).alignment = TextAlignmentOptions.MidlineRight;

        // Franja info: puzzle + timer (DEBAJO de la cabecera, sin solaparse)
        // La cabecera ocupa los 88px superiores (~8% a 1080p).
        // Esta franja ocupa el siguiente bloque: y de 0.855 a 0.912
        var infoBar = Img(R, "InfoBar", C(0.05f,0.08f,0.16f),
                          V(0f,0.855f), V(1f,0.912f), V(0,0), V(0,0));
        Img(infoBar, "InfoLineTop", ACCENT, V(0,1), V(1,1), V(0,-1f), V(0,2f));
        Img(infoBar, "InfoLineBot", C(0.15f,0.22f,0.38f), V(0,0), V(1,0), V(0,1f), V(0,2f));

        _puzzleLbl = Txt(infoBar, "Puzzle", "Puzzle 1 de 3", C(0.75f,0.85f,1f), 22,
                         V(0.02f, 0f), V(0.65f, 1f));
        _puzzleLbl.alignment = TextAlignmentOptions.MidlineLeft;

        _timerLbl = Txt(infoBar, "Timer", "45", TIMER_OK, 38,
                        V(0.68f, 0f), V(0.98f, 1f));
        _timerLbl.fontStyle = FontStyles.Bold;
        _timerLbl.alignment = TextAlignmentOptions.MidlineRight;

        // Pista justo debajo de la franja info
        _hintLbl = Txt(R, "Hint", "", C(0.60f, 0.78f, 1f), 18,
                       V(0.02f, 0.798f), V(0.98f, 0.852f));
        _hintLbl.alignment = TextAlignmentOptions.MidlineLeft;

        // Cuadricula
        BuildGrid(R, rows, cols);

        // Barra inferior de instruccion
        var bot = Img(R, "Bot", HDR, V(0,0), V(1,0), V(0,38f), V(0,76f));
        Img(bot, "BotLine", ACCENT, V(0,1), V(1,1), V(0,-1.5f), V(0,3f));
        var instr = Txt(bot, "Instr",
            "Haz clic en los espejos NARANJAS ( / o \\ ) para girarlos y llevar el laser hasta META",
            Color.white, 18, V(0.01f,0f), V(0.99f,1f));
        instr.alignment = TextAlignmentOptions.MidlineLeft;

        // Panel de exito por puzzle: banner centrado con fondo verde
        var flashGO = new GameObject("FlashPanel");
        flashGO.transform.SetParent(R, false);
        var flashRT = flashGO.AddComponent<RectTransform>();
        flashRT.anchorMin        = V(0.5f, 0.5f);
        flashRT.anchorMax        = V(0.5f, 0.5f);
        flashRT.pivot            = V(0.5f, 0.5f);
        flashRT.sizeDelta        = new Vector2(780f, 110f);
        flashRT.anchoredPosition = new Vector2(0f, 20f);
        var flashBg = flashGO.AddComponent<Image>();
        flashBg.color = C(0.06f, 0.52f, 0.22f, 0.96f);
        // Borde superior e inferior de acento
        var flashAccTop = Img(flashRT, "AccT", C(0.30f,0.95f,0.55f),
                              V(0,1), V(1,1), V(0,-2f), V(0,4f));
        var flashAccBot = Img(flashRT, "AccB", C(0.30f,0.95f,0.55f),
                              V(0,0), V(1,0), V(0, 2f), V(0,4f));
        _flashLbl = Txt(flashRT, "FlashTxt", "", Color.white, 40,
                        V(0.02f, 0f), V(0.98f, 1f));
        _flashLbl.fontStyle = FontStyles.Bold;
        _flashLbl.alignment = TextAlignmentOptions.Center;
        flashGO.SetActive(false);
        // Guardar referencia al panel completo para Show/Hide
        _flashPanel = flashGO;

        // Panel resultado final
        BuildResultPanel(R, onRestart, onMenu);
    }

    // -- Cuadricula ------------------------------------------------------------
    void BuildGrid(RectTransform root, int rows, int cols)
    {
        _cellBorder = new Image[rows, cols];
        _cellFill   = new Image[rows, cols];
        _cellLaser  = new Image[rows, cols];
        _cellSym    = new TextMeshProUGUI[rows, cols];
        _cellHint   = new TextMeshProUGUI[rows, cols];

        float cellSize = Mathf.Min(90f, Mathf.Min(680f / cols, 500f / rows));
        float gap      = 6f;
        float totalW   = cols * (cellSize + gap) - gap;
        float totalH   = rows * (cellSize + gap) - gap;
        _baseFontSize  = cellSize * 0.50f;

        var gGO = new GameObject("Grid");
        gGO.transform.SetParent(root, false);
        var gRT = gGO.AddComponent<RectTransform>();
        gGO.AddComponent<Image>().color = Color.clear;
        gRT.anchorMin        = gRT.anchorMax = V(0.5f, 0.5f);
        gRT.pivot            = V(0.5f, 0.5f);
        gRT.sizeDelta        = new Vector2(totalW, totalH);
        gRT.anchoredPosition = new Vector2(0f, -10f);

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                float px = c * (cellSize + gap) - totalW * 0.5f + cellSize * 0.5f;
                float py = -(r * (cellSize + gap) - totalH * 0.5f + cellSize * 0.5f);

                var goCell = new GameObject($"C{r}_{c}");
                goCell.transform.SetParent(gRT, false);
                var rtCell = goCell.AddComponent<RectTransform>();
                rtCell.anchorMin = rtCell.anchorMax = V(0.5f, 0.5f);
                rtCell.sizeDelta        = new Vector2(cellSize, cellSize);
                rtCell.anchoredPosition = new Vector2(px, py);

                // Capa 1: borde exterior (5 px de margen)
                var brdImg = goCell.AddComponent<Image>();
                brdImg.color = CELL_BRD;
                _cellBorder[r, c] = brdImg;

                // Capa 2: relleno interior
                var fillRT = Img(rtCell, "Fill", CELL_BG,
                                 V(0,0), V(1,1), V(0,0), V(-5f,-5f));
                _cellFill[r, c] = fillRT.GetComponent<Image>();

                // Capa 3: overlay laser
                var lgGO = new GameObject("Laser");
                lgGO.transform.SetParent(fillRT, false);
                var lgRT = lgGO.AddComponent<RectTransform>();
                lgRT.anchorMin = Vector2.zero; lgRT.anchorMax = Vector2.one;
                lgRT.sizeDelta = Vector2.zero; lgRT.anchoredPosition = Vector2.zero;
                _cellLaser[r, c] = lgGO.AddComponent<Image>();
                _cellLaser[r, c].color = Color.clear;

                // Capa 4: simbolo principal (ocupa la parte superior-central de la celda)
                var sym = Txt(fillRT, "Sym", "", Color.white,
                              _baseFontSize, V(0.05f, 0.28f), V(0.95f, 0.95f));
                sym.fontStyle = FontStyles.Bold;
                sym.alignment = TextAlignmentOptions.Center;
                _cellSym[r, c] = sym;

                // Capa 5: etiqueta inferior pequena (ej. "GIRAR" para espejos rotatables)
                var hint = Txt(fillRT, "Hint", "", MIR_HINT,
                               _baseFontSize * 0.30f, V(0.02f, 0.02f), V(0.98f, 0.30f));
                hint.fontStyle = FontStyles.Bold;
                hint.alignment = TextAlignmentOptions.Center;
                _cellHint[r, c] = hint;

                // Boton (tinta el relleno al pasar el raton)
                int lr = r, lc = c;
                var btn = goCell.AddComponent<Button>();
                btn.targetGraphic = _cellFill[r, c];
                var col = btn.colors;
                col.normalColor      = Color.white;
                col.highlightedColor = new Color(1.35f, 1.35f, 1.35f);
                col.pressedColor     = new Color(0.68f, 0.68f, 0.68f);
                col.selectedColor    = Color.white;
                btn.colors = col;
                btn.onClick.AddListener(() => OnCellClicked?.Invoke(lr, lc));
            }
        }
    }

    // -- Refresco de estado ----------------------------------------------------
    public void RefreshGrid(LaserGridManager mgr)
    {
        for (int r = 0; r < _rows; r++)
        {
            for (int c = 0; c < _cols; c++)
            {
                var cell   = mgr.GetCell(r, c);
                var border = _cellBorder[r, c];
                var fill   = _cellFill[r, c];
                var laser  = _cellLaser[r, c];
                var sym    = _cellSym[r, c];
                var hint   = _cellHint[r, c];

                // Reset
                laser.color          = Color.clear;
                hint.text            = "";
                sym.enableAutoSizing = false;
                sym.fontSize         = _baseFontSize;

                switch (cell.type)
                {
                    case LaserCellType.Empty:
                        border.color = CELL_BRD;
                        fill.color   = CELL_BG;
                        sym.text     = "";
                        break;

                    case LaserCellType.Emitter:
                        border.color = EMIT_BRD;
                        fill.color   = EMIT_BG;
                        sym.text     = DirArrow(cell.emitterDir);
                        sym.color    = Color.white;
                        sym.fontSize = _baseFontSize * 1.30f;
                        break;

                    case LaserCellType.Target:
                        border.color           = TARGET_BRD;
                        fill.color             = TARGET_BG;
                        sym.text               = "META";
                        sym.color              = Color.white;
                        sym.enableAutoSizing   = true;
                        sym.fontSizeMin        = 10f;
                        sym.fontSizeMax        = _baseFontSize * 0.68f;
                        break;

                    case LaserCellType.Wall:
                        border.color = FIX_BRD;
                        fill.color   = WALL_BG;
                        sym.text     = "";
                        break;

                    case LaserCellType.Mirror:
                        if (cell.isFixed)
                        {
                            // Espejo fijo: gris, sin etiqueta
                            border.color = FIX_BRD;
                            fill.color   = FIX_BG;
                            sym.text     = cell.mirrorKind == LaserMirrorKind.Slash ? "/" : "\\";
                            sym.color    = FIX_TXT;
                        }
                        else
                        {
                            // Espejo rotable: naranja brillante + etiqueta "GIRAR"
                            border.color = MIR_BRD;
                            fill.color   = MIR_BG;
                            sym.text     = cell.mirrorKind == LaserMirrorKind.Slash ? "/" : "\\";
                            sym.color    = MIR_TXT;
                            hint.text    = "GIRAR";
                            hint.color   = MIR_HINT;
                        }
                        break;
                }
            }
        }

        // Laser path encima del color base
        foreach (var pos in mgr.LaserPath)
        {
            int r = pos.x, c = pos.y;
            var cell = mgr.GetCell(r, c);

            if (cell.type == LaserCellType.Empty)
            {
                _cellBorder[r, c].color = LASER_BRD;
                _cellFill[r, c].color   = LASER_COL;
            }
            else
            {
                // Overlay semitransparente para celdas con contenido
                _cellLaser[r, c].color = C(1f, 0.90f, 0.00f, 0.30f);
            }
        }
    }

    /// <summary>Flash visual rapido al hacer clic en un espejo.</summary>
    public void FlashMirrorClick(int row, int col)
    {
        if (_cellFill == null) return;
        StartCoroutine(ClickFlashCo(row, col));
    }

    IEnumerator ClickFlashCo(int row, int col)
    {
        if (row >= _rows || col >= _cols) yield break;
        // Destello blanco-naranja brillante
        _cellBorder[row, col].color = Color.white;
        _cellFill[row, col].color   = C(1.00f, 0.88f, 0.45f);
        yield return new WaitForSeconds(0.07f);
        // Restaurar al color naranja correcto del espejo rotable
        _cellBorder[row, col].color = MIR_BRD;
        _cellFill[row, col].color   = MIR_BG;
    }

    // -- Metodos publicos de UI ------------------------------------------------
    public void SetTimer(float t, float maxT)
    {
        if (_timerLbl == null) return;
        _timerLbl.text = Mathf.CeilToInt(Mathf.Max(0, t)).ToString();
        float ratio = t / maxT;
        _timerLbl.color = ratio > 0.5f ? TIMER_OK
                        : ratio > 0.25f ? TIMER_WARN
                                        : TIMER_BAD;
    }

    public void SetPuzzleLabel(int current, int total) =>
        _puzzleLbl.text = $"Puzzle {current} de {total}";

    public void SetHint(string h)
    {
        if (_hintLbl != null) _hintLbl.text = h;
    }

    public void ShowWinFlash(string msg)
    {
        if (_flashPanel == null) return;
        _flashLbl.text = msg;
        _flashPanel.SetActive(true);
    }

    public void HideWinFlash()
    {
        if (_flashPanel != null) _flashPanel.SetActive(false);
    }

    public void ShowFinalResult(bool win, string sub)
    {
        _resultTitle.text  = win ? "Muy bien!" : "Fin del juego";
        _resultTitle.color = win ? WIN_C : LOSE_C;
        _resultSub.text    = sub;
        _resultPanel.SetActive(true);
    }

    // -- Panel resultado final -------------------------------------------------
    void BuildResultPanel(RectTransform root, Action onRestart, Action onMenu)
    {
        _resultPanel = new GameObject("ResultPanel");
        _resultPanel.transform.SetParent(root, false);
        var er = _resultPanel.AddComponent<RectTransform>();
        er.anchorMin = Vector2.zero; er.anchorMax = Vector2.one;
        er.sizeDelta = Vector2.zero; er.anchoredPosition = Vector2.zero;
        _resultPanel.AddComponent<Image>().color = C(0,0,0,0.88f);

        var card = Img(er, "Card", PANEL_C, V(0.5f,0.5f), V(0.5f,0.5f), V(0,0), V(840f,460f));
        Img(card, "LineT", ACCENT, V(0,1), V(1,1), V(0,-4f), V(0,8f));

        _resultTitle = Txt(card, "RT", "", Color.white, 58,
                           V(0.05f,0.72f), V(0.95f,0.97f));
        _resultTitle.fontStyle = FontStyles.Bold;
        _resultTitle.alignment = TextAlignmentOptions.Center;

        _resultSub = Txt(card, "RS", "", C(0.65f,0.78f,1f), 26,
                         V(0.05f,0.28f), V(0.95f,0.70f));
        _resultSub.alignment    = TextAlignmentOptions.Center;
        _resultSub.overflowMode = TextOverflowModes.Overflow;

        MkBtn(card, "Jugar de nuevo", ACCENT,
              V(0.05f, 0.21f), V(0.47f, 0.35f), onRestart);

        MkBtn(card, "Volver a la seccion", C(0.18f, 0.24f, 0.38f),
              V(0.53f, 0.21f), V(0.95f, 0.35f), onMenu);

        MkBtn(card, "Menu principal", C(0.10f, 0.13f, 0.22f),
              V(0.05f, 0.05f), V(0.95f, 0.18f), () => SceneLoader.GoToMainMenu());

        _resultPanel.SetActive(false);
    }

    // -- Helpers ---------------------------------------------------------------
    static string DirArrow(LaserDirection d) => d switch
    {
        LaserDirection.Right => ">",
        LaserDirection.Left  => "<",
        LaserDirection.Up    => "^",
        LaserDirection.Down  => "v",
        _                    => "?"
    };

    RectTransform Img(RectTransform p, string n, Color col,
                      Vector2 am, Vector2 aM, Vector2 pos, Vector2 sd)
    {
        var go = new GameObject(n); go.transform.SetParent(p, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = am; rt.anchorMax = aM;
        rt.pivot = V(.5f, .5f);
        rt.anchoredPosition = pos; rt.sizeDelta = sd;
        go.AddComponent<Image>().color = col;
        return rt;
    }

    TextMeshProUGUI Txt(RectTransform p, string n, string txt, Color col, float sz,
                        Vector2 am, Vector2 aM)
    {
        var go = new GameObject(n); go.transform.SetParent(p, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = am; rt.anchorMax = aM;
        rt.pivot = V(.5f, .5f);
        rt.anchoredPosition = Vector2.zero; rt.sizeDelta = Vector2.zero;
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = txt; t.color = col; t.fontSize = sz;
        t.alignment = TextAlignmentOptions.Center;
        t.overflowMode = TextOverflowModes.Overflow;
        return t;
    }

    void MkBtn(RectTransform p, string lbl, Color bg, Vector2 am, Vector2 aM, Action click)
    {
        var rt = Img(p, "Btn_" + lbl, bg, am, aM, V(0,0), V(0,0));
        var b  = rt.gameObject.AddComponent<Button>();
        b.targetGraphic = rt.GetComponent<Image>();
        var cb = b.colors;
        cb.normalColor      = Color.white;
        cb.highlightedColor = new Color(1f,1f,1f,.82f);
        cb.pressedColor     = new Color(.72f,.72f,.72f);
        b.colors = cb;
        b.onClick.AddListener(() => click?.Invoke());
        var t = Txt(rt, "T", lbl, Color.white, 24, V(0,0), V(1,1));
        t.fontStyle = FontStyles.Bold;
    }
}
