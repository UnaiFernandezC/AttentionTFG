// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Interfaz del minijuego Camino Laser (Atencion).
/// Construida 100% por codigo con estetica espacial:
/// - Laser con glow neon por capas (pastillas redondeadas con alfa).
/// - Nodos/espejos redondeados con anillo expansivo al pulsarlos.
/// - Animacion del haz recorriendo el camino celda a celda.
/// - HUD redondeado con la paleta amarilla de Atencion.
/// Usa solo caracteres ASCII estandar (sin emojis ni Unicode especial).
/// </summary>
public class LaserUIController : MonoBehaviour
{
    // -- Colores ---------------------------------------------------------------
    static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);
    static Vector2 V(float x, float y) => new Vector2(x, y);

    // Paleta amarilla de la categoria Atencion
    static readonly Color ACCENT    = C(0.98f, 0.80f, 0.10f);
    static readonly Color ACCENT_DIM= C(0.98f, 0.80f, 0.10f, 0.55f);
    // Paneles del HUD (flotantes, redondeados)
    static readonly Color PANEL_C   = C(0.10f, 0.13f, 0.24f, 0.94f);
    static readonly Color PANEL_SOFT= C(0.07f, 0.10f, 0.20f, 0.88f);
    // Celda vacia
    static readonly Color CELL_BG   = C(0.12f, 0.16f, 0.30f, 0.94f);
    static readonly Color CELL_BRD  = C(0.20f, 0.27f, 0.46f, 0.95f);
    // Laser neon (amarillo brillante + halo)
    static readonly Color LASER_COL = C(1.00f, 0.92f, 0.15f);
    static readonly Color LASER_BRD = C(1.00f, 0.98f, 0.55f);
    static readonly Color LASER_GLOW= C(1.00f, 0.90f, 0.20f, 0.30f);
    // Emisor (verde)
    static readonly Color EMIT_BG   = C(0.10f, 0.62f, 0.24f);
    static readonly Color EMIT_BRD  = C(0.30f, 0.95f, 0.50f, 0.85f);
    // Objetivo (rojo)
    static readonly Color TARGET_BG = C(0.85f, 0.14f, 0.16f);
    static readonly Color TARGET_BRD= C(1.00f, 0.45f, 0.40f, 0.9f);
    // Espejo ROTABLE -- naranja intenso con borde dorado, muy distinto al resto
    static readonly Color MIR_BG    = C(0.90f, 0.44f, 0.02f);
    static readonly Color MIR_BRD   = C(1.00f, 0.78f, 0.22f);
    static readonly Color MIR_TXT   = Color.white;
    static readonly Color MIR_HINT  = C(1.00f, 0.90f, 0.65f);
    // Espejo FIJO (gris, no clickable)
    static readonly Color FIX_BG    = C(0.28f, 0.30f, 0.38f);
    static readonly Color FIX_BRD   = C(0.18f, 0.20f, 0.26f);
    static readonly Color FIX_TXT   = C(0.65f, 0.70f, 0.80f);
    // Pared
    static readonly Color WALL_BG   = C(0.20f, 0.20f, 0.27f);
    // Timer
    static readonly Color TIMER_OK  = C(0.30f, 0.90f, 0.40f);
    static readonly Color TIMER_WARN= C(1.00f, 0.75f, 0.10f);
    static readonly Color TIMER_BAD = C(1.00f, 0.28f, 0.28f);
    // Resultado
    static readonly Color WIN_C     = C(0.20f, 0.85f, 0.40f);
    static readonly Color LOSE_C    = C(0.90f, 0.25f, 0.25f);

    // -- Elementos UI ---------------------------------------------------------
    Image[,]           _cellBorder;   // marco redondeado exterior
    Image[,]           _cellFill;     // relleno principal (color del tipo de celda)
    Image[,]           _cellGlow;     // halo neon exterior (capa de glow del laser)
    Image[,]           _cellLaser;    // overlay laser semitransparente
    TextMeshProUGUI[,] _cellSym;      // simbolo grande: > < ^ v / \ META
    TextMeshProUGUI[,] _cellHint;     // etiqueta pequena bajo el simbolo (ej. "GIRAR")

    float _baseFontSize;

    TextMeshProUGUI _timerLbl;
    Image           _timerPill;
    TextMeshProUGUI _puzzleLbl;
    TextMeshProUGUI _hintLbl;
    TextMeshProUGUI _flashLbl;
    GameObject      _flashPanel;
    GameObject      _resultPanel;
    TextMeshProUGUI _resultTitle;
    TextMeshProUGUI _resultSub;
    RectTransform   _gridRT;

    Coroutine _laserAnimCo;
    int       _lastTimerSec = -1;

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

        // Fondo espacial coherente (gradiente + nebulosas + estrellas + planeta)
        KidUI.BuildSpaceBackground(R);

        // Cabecera flotante redondeada
        var hdr = Pill(R, "Hdr", PANEL_C, V(0.015f, 0.925f), V(0.985f, 0.988f), 1.3f);
        var hdrLine = Pill(hdr, "HdrLine", ACCENT, V(0f, 0f), V(1f, 0f), 4f);
        hdrLine.anchoredPosition = V(0f, 2f);
        hdrLine.sizeDelta        = V(-30f, 4f);
        hdrLine.GetComponent<Image>().raycastTarget = false;

        var ttl = Txt(hdr, "Titulo", "CAMINO LASER", Color.white, 34,
                      V(0.02f, 0.10f), V(0.60f, 0.90f));
        ttl.fontStyle        = FontStyles.Bold;
        ttl.alignment        = TextAlignmentOptions.MidlineLeft;
        ttl.characterSpacing = 2f;

        var cat = Txt(hdr, "Cat", "ATENCION", ACCENT, 18,
                      V(0.60f, 0.10f), V(0.98f, 0.90f));
        cat.alignment        = TextAlignmentOptions.MidlineRight;
        cat.characterSpacing = 3f;
        UITween.PopIn(hdr, 0.45f, 0.90f);

        // Chip del puzzle actual (pastilla redondeada a la izquierda)
        var puzzlePill = Pill(R, "PuzzlePill", PANEL_SOFT,
                              V(0.015f, 0.858f), V(0.24f, 0.916f), 1.6f);
        _puzzleLbl = Txt(puzzlePill, "Puzzle", "Puzzle 1 de 3", C(0.90f, 0.93f, 1f), 22,
                         V(0.10f, 0f), V(0.95f, 1f));
        _puzzleLbl.alignment = TextAlignmentOptions.MidlineLeft;
        _puzzleLbl.fontStyle = FontStyles.Bold;
        KidUI.CircleAt(puzzlePill, "PuzzleDot", ACCENT, V(0.065f, 0.5f), 14f)
             .GetComponent<Image>().raycastTarget = false;
        UITween.PopIn(puzzlePill, 0.45f, 0.85f, 0.05f);

        // Pastilla del timer (a la derecha)
        var timerPillRT = Pill(R, "TimerPill", PANEL_SOFT,
                               V(0.86f, 0.852f), V(0.985f, 0.918f), 1.6f);
        _timerPill = timerPillRT.GetComponent<Image>();
        _timerLbl  = Txt(timerPillRT, "Timer", "45", TIMER_OK, 40,
                         V(0.05f, 0f), V(0.95f, 1f));
        _timerLbl.fontStyle = FontStyles.Bold;
        _timerLbl.alignment = TextAlignmentOptions.Center;
        UITween.PopIn(timerPillRT, 0.45f, 0.85f, 0.08f);

        // Pista (entre los chips)
        _hintLbl = Txt(R, "Hint", "", C(1.00f, 0.90f, 0.55f, 0.92f), 19,
                       V(0.25f, 0.855f), V(0.85f, 0.915f));
        _hintLbl.alignment = TextAlignmentOptions.Center;
        _hintLbl.fontStyle = FontStyles.Italic;

        // Cuadricula
        BuildGrid(R, rows, cols);

        // Barra inferior de instruccion (pastilla flotante)
        var bot = Pill(R, "Bot", PANEL_C, V(0.10f, 0.014f), V(0.90f, 0.072f), 1.4f);
        KidUI.CircleAt(bot, "BotDot", ACCENT, V(0.035f, 0.5f), 14f)
             .GetComponent<Image>().raycastTarget = false;
        var instr = Txt(bot, "Instr",
            "Haz clic en los espejos NARANJAS ( / o \\ ) para girarlos y llevar el laser hasta META",
            C(0.92f, 0.94f, 1f), 18, V(0.06f, 0f), V(0.97f, 1f));
        instr.alignment = TextAlignmentOptions.MidlineLeft;
        UITween.PopIn(bot, 0.45f, 0.90f, 0.10f);

        // Panel de exito por puzzle: banner redondeado centrado
        var flashGO = new GameObject("FlashPanel");
        flashGO.transform.SetParent(R, false);
        var flashRT = flashGO.AddComponent<RectTransform>();
        flashRT.anchorMin        = V(0.5f, 0.5f);
        flashRT.anchorMax        = V(0.5f, 0.5f);
        flashRT.pivot            = V(0.5f, 0.5f);
        flashRT.sizeDelta        = new Vector2(780f, 116f);
        flashRT.anchoredPosition = new Vector2(0f, 20f);
        var flashBg = flashGO.AddComponent<Image>();
        flashBg.color                   = C(0.06f, 0.52f, 0.22f, 0.97f);
        flashBg.sprite                  = KidUI.RoundedSprite;
        flashBg.type                    = Image.Type.Sliced;
        flashBg.pixelsPerUnitMultiplier = 0.8f;
        // Halo verde suave detras del banner
        var flashGlow = Pill(flashRT, "FlashGlow", C(0.30f, 0.95f, 0.55f, 0.18f),
                             V(0f, 0f), V(1f, 1f), 0.6f);
        flashGlow.sizeDelta = V(34f, 34f);
        flashGlow.SetAsFirstSibling();
        flashGlow.GetComponent<Image>().raycastTarget = false;
        var flashLine = Pill(flashRT, "AccB", C(0.30f, 0.95f, 0.55f), V(0f, 0f), V(1f, 0f), 4f);
        flashLine.anchoredPosition = V(0f, 4f);
        flashLine.sizeDelta        = V(-60f, 5f);
        flashLine.GetComponent<Image>().raycastTarget = false;
        _flashLbl = Txt(flashRT, "FlashTxt", "", Color.white, 40,
                        V(0.02f, 0f), V(0.98f, 1f));
        _flashLbl.fontStyle = FontStyles.Bold;
        _flashLbl.alignment = TextAlignmentOptions.Center;
        flashGO.SetActive(false);
        _flashPanel = flashGO;

        // Panel resultado final
        BuildResultPanel(R, onRestart, onMenu);
    }

    // -- Cuadricula ------------------------------------------------------------
    void BuildGrid(RectTransform root, int rows, int cols)
    {
        _cellBorder = new Image[rows, cols];
        _cellFill   = new Image[rows, cols];
        _cellGlow   = new Image[rows, cols];
        _cellLaser  = new Image[rows, cols];
        _cellSym    = new TextMeshProUGUI[rows, cols];
        _cellHint   = new TextMeshProUGUI[rows, cols];

        float cellSize = Mathf.Min(90f, Mathf.Min(680f / cols, 500f / rows));
        float gap      = 7f;
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
        _gridRT = gRT;

        // Bandeja flotante redondeada bajo el tablero (con halo amarillo suave)
        var trayGlow = Pill(gRT, "TrayGlow", C(0.98f, 0.80f, 0.10f, 0.06f),
                            V(0f, 0f), V(1f, 1f), 0.5f);
        trayGlow.sizeDelta = V(64f, 64f);
        trayGlow.GetComponent<Image>().raycastTarget = false;
        var tray = Pill(gRT, "Tray", C(0.05f, 0.07f, 0.16f, 0.72f),
                        V(0f, 0f), V(1f, 1f), 0.8f);
        tray.sizeDelta = V(36f, 36f);
        tray.GetComponent<Image>().raycastTarget = false;

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

                // Capa 0: halo neon exterior (glow del laser, apagado por defecto)
                var glowRT = Pill(rtCell, "Glow", Color.clear, V(0f, 0f), V(1f, 1f), 0.7f);
                glowRT.sizeDelta = V(26f, 26f);
                var glowImg = glowRT.GetComponent<Image>();
                glowImg.raycastTarget = false;
                _cellGlow[r, c] = glowImg;

                // Capa 1: marco exterior redondeado
                var brdImg = goCell.AddComponent<Image>();
                brdImg.color                   = CELL_BRD;
                brdImg.sprite                  = KidUI.RoundedSprite;
                brdImg.type                    = Image.Type.Sliced;
                brdImg.pixelsPerUnitMultiplier = 1.3f;
                _cellBorder[r, c] = brdImg;

                // Capa 2: relleno interior redondeado
                var fillRT = Pill(rtCell, "Fill", CELL_BG, V(0f, 0f), V(1f, 1f), 1.5f);
                fillRT.sizeDelta = V(-6f, -6f);
                _cellFill[r, c] = fillRT.GetComponent<Image>();

                // Capa 3: overlay laser semitransparente
                var lgRT = Pill(fillRT, "Laser", Color.clear, V(0f, 0f), V(1f, 1f), 1.5f);
                _cellLaser[r, c] = lgRT.GetComponent<Image>();
                _cellLaser[r, c].raycastTarget = false;

                // Capa 4: simbolo principal (parte superior-central de la celda)
                var sym = Txt(fillRT, "Sym", "", Color.white,
                              _baseFontSize, V(0.05f, 0.28f), V(0.95f, 0.95f));
                sym.fontStyle = FontStyles.Bold;
                sym.alignment = TextAlignmentOptions.Center;
                _cellSym[r, c] = sym;

                // Capa 5: etiqueta inferior pequena (ej. "GIRAR" para espejos)
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

        // Entrada juicy del tablero + balanceo espacial muy sutil (flotar)
        UITween.PopIn(gRT, 0.55f, 0.82f, 0.05f);
        gGO.AddComponent<FloatBob>().Configure(5f, 0.7f);
    }

    // -- Refresco de estado ----------------------------------------------------
    public void RefreshGrid(LaserGridManager mgr)
    {
        // Detener cualquier animacion de haz anterior
        if (_laserAnimCo != null) { StopCoroutine(_laserAnimCo); _laserAnimCo = null; }

        for (int r = 0; r < _rows; r++)
        {
            for (int c = 0; c < _cols; c++)
            {
                var cell   = mgr.GetCell(r, c);
                var border = _cellBorder[r, c];
                var fill   = _cellFill[r, c];
                var laser  = _cellLaser[r, c];
                var glow   = _cellGlow[r, c];
                var sym    = _cellSym[r, c];
                var hint   = _cellHint[r, c];

                // Reset
                laser.color          = Color.clear;
                glow.color           = Color.clear;
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
                        glow.color   = C(EMIT_BRD.r, EMIT_BRD.g, EMIT_BRD.b, 0.14f);
                        sym.text     = DirArrow(cell.emitterDir);
                        sym.color    = Color.white;
                        sym.fontSize = _baseFontSize * 1.30f;
                        break;

                    case LaserCellType.Target:
                        border.color           = TARGET_BRD;
                        fill.color             = TARGET_BG;
                        glow.color             = C(TARGET_BRD.r, TARGET_BRD.g, TARGET_BRD.b, 0.12f);
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
                            // Espejo rotable: naranja brillante + anillo dorado + "GIRAR"
                            border.color = MIR_BRD;
                            fill.color   = MIR_BG;
                            glow.color   = C(MIR_BRD.r, MIR_BRD.g, MIR_BRD.b, 0.10f);
                            sym.text     = cell.mirrorKind == LaserMirrorKind.Slash ? "/" : "\\";
                            sym.color    = MIR_TXT;
                            hint.text    = "GIRAR";
                            hint.color   = MIR_HINT;
                        }
                        break;
                }
            }
        }

        // Animar el haz recorriendo el camino (copia del path: la lista
        // original se vacia en el siguiente TraceLaser)
        var path = new List<Vector2Int>(mgr.LaserPath);
        _laserAnimCo = StartCoroutine(AnimateLaserCo(path, mgr));
    }

    /// <summary>Enciende las celdas del camino una a una (haz recorriendo).</summary>
    IEnumerator AnimateLaserCo(List<Vector2Int> path, LaserGridManager mgr)
    {
        var wait = new WaitForSeconds(0.035f);
        for (int i = 0; i < path.Count; i++)
        {
            int r = path[i].x, c = path[i].y;
            if (r < 0 || r >= _rows || c < 0 || c >= _cols) continue;
            var cell = mgr.GetCell(r, c);

            if (cell.type == LaserCellType.Empty)
            {
                // Neon por capas: halo exterior + marco claro + relleno brillante
                _cellGlow[r, c].color   = LASER_GLOW;
                _cellBorder[r, c].color = LASER_BRD;
                _cellFill[r, c].color   = LASER_COL;
                _cellLaser[r, c].color  = C(1f, 1f, 1f, 0.22f);   // nucleo blanco
            }
            else
            {
                // Overlay semitransparente para celdas con contenido
                _cellLaser[r, c].color = C(1f, 0.90f, 0.10f, 0.32f);
                _cellGlow[r, c].color  = C(1f, 0.90f, 0.20f, 0.20f);
            }
            yield return wait;
        }

        // Si el haz llega a META: pulso de celebracion en la celda objetivo
        if (mgr.LaserReachedTarget && path.Count > 0)
        {
            int tr = path[path.Count - 1].x, tc = path[path.Count - 1].y;
            if (tr >= 0 && tr < _rows && tc >= 0 && tc < _cols)
            {
                _cellGlow[tr, tc].color = C(1f, 0.92f, 0.30f, 0.40f);
                UITween.PulseOnce(_cellBorder[tr, tc].rectTransform, 1.22f, 0.35f);
                // Chispas doradas al conectar el laser con la META
                StartCoroutine(SparkBurstCo(_cellBorder[tr, tc].rectTransform));
            }
        }
        _laserAnimCo = null;
    }

    /// <summary>Chispas doradas que salen despedidas de la celda META al conectar.</summary>
    IEnumerator SparkBurstCo(RectTransform cell)
    {
        const int n = 10;
        var sparks = new RectTransform[n];
        var imgs   = new Image[n];
        var dirs   = new Vector2[n];
        for (int i = 0; i < n; i++)
        {
            var go = new GameObject("Spark");
            go.transform.SetParent(cell, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = V(0.5f, 0.5f);
            rt.pivot            = V(0.5f, 0.5f);
            float s             = UnityEngine.Random.Range(6f, 13f);
            rt.sizeDelta        = new Vector2(s, s);
            rt.anchoredPosition = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.sprite        = KidUI.CircleSpr;
            img.color         = C(1f, UnityEngine.Random.Range(0.75f, 0.95f), 0.25f);
            img.raycastTarget = false;
            float ang = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            dirs[i]   = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) *
                        UnityEngine.Random.Range(70f, 150f);
            sparks[i] = rt; imgs[i] = img;
        }
        float t = 0f, dur = 0.55f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / dur);
            // Frenada suave hacia fuera + desvanecimiento
            float ease = 1f - (1f - p) * (1f - p);
            for (int i = 0; i < n; i++)
            {
                if (sparks[i] == null) continue;
                sparks[i].anchoredPosition = dirs[i] * ease;
                var c = imgs[i].color; c.a = 1f - p; imgs[i].color = c;
            }
            yield return null;
        }
        for (int i = 0; i < n; i++)
            if (sparks[i] != null) Destroy(sparks[i].gameObject);
    }

    /// <summary>Feedback al hacer clic en un espejo: pop + anillo expansivo.</summary>
    public void FlashMirrorClick(int row, int col)
    {
        if (_cellFill == null) return;
        GameFeel.PlayPop();
        if (row < _rows && col < _cols && _cellBorder[row, col] != null)
        {
            UITween.PulseOnce(_cellBorder[row, col].rectTransform, 1.18f, 0.22f);
            StartCoroutine(RingBurstCo(_cellBorder[row, col].rectTransform));
        }
        StartCoroutine(ClickFlashCo(row, col));
    }

    /// <summary>Anillo dorado que se expande y desvanece sobre el espejo pulsado.</summary>
    IEnumerator RingBurstCo(RectTransform cell)
    {
        var go = new GameObject("RingFX");
        go.transform.SetParent(cell, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero; rt.anchoredPosition = Vector2.zero;
        var img = go.AddComponent<Image>();
        img.sprite                  = KidUI.RoundedSprite;
        img.type                    = Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = 0.9f;
        img.raycastTarget           = false;

        float t = 0f, dur = 0.38f;
        while (t < dur)
        {
            if (rt == null) yield break;
            t += Time.unscaledDeltaTime;
            float p = t / dur;
            rt.localScale = Vector3.one * (1f + p * 0.85f);
            img.color     = C(1f, 0.85f, 0.25f, 0.55f * (1f - p));
            yield return null;
        }
        if (go != null) Destroy(go);
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
        int secs = Mathf.CeilToInt(Mathf.Max(0, t));
        _timerLbl.text = secs.ToString();
        float ratio = t / maxT;
        _timerLbl.color = ratio > 0.5f ? TIMER_OK
                        : ratio > 0.25f ? TIMER_WARN
                                        : TIMER_BAD;
        // Tension visual sutil: pulso por segundo cuando queda poco tiempo
        if (ratio <= 0.25f && secs != _lastTimerSec && _timerPill != null)
            UITween.PulseOnce(_timerPill.rectTransform, 1.10f, 0.20f);
        _lastTimerSec = secs;
    }

    public void SetPuzzleLabel(int current, int total)
    {
        _puzzleLbl.text = $"Puzzle {current} de {total}";
        UITween.PulseOnce(_puzzleLbl.rectTransform, 1.08f, 0.22f);
    }

    public void SetHint(string h)
    {
        if (_hintLbl != null) _hintLbl.text = h;
    }

    public void ShowWinFlash(string msg)
    {
        if (_flashPanel == null) return;
        _flashLbl.text = msg;
        _flashPanel.SetActive(true);
        UITween.PopIn(_flashPanel.transform as RectTransform, 0.40f, 0.75f);
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
        _resultPanel.AddComponent<Image>().color = C(0, 0, 0, 0.88f);

        var card = Pill(er, "Card", PANEL_C, V(0.5f, 0.5f), V(0.5f, 0.5f), 1.0f);
        card.sizeDelta = V(840f, 460f);
        var lineT = Pill(card, "LineT", ACCENT, V(0f, 1f), V(1f, 1f), 4f);
        lineT.anchoredPosition = V(0f, -5f);
        lineT.sizeDelta        = V(-60f, 7f);
        lineT.GetComponent<Image>().raycastTarget = false;

        _resultTitle = Txt(card, "RT", "", Color.white, 58,
                           V(0.05f, 0.72f), V(0.95f, 0.97f));
        _resultTitle.fontStyle = FontStyles.Bold;
        _resultTitle.alignment = TextAlignmentOptions.Center;

        _resultSub = Txt(card, "RS", "", C(0.95f, 0.90f, 0.65f), 26,
                         V(0.05f, 0.28f), V(0.95f, 0.70f));
        _resultSub.alignment    = TextAlignmentOptions.Center;
        _resultSub.overflowMode = TextOverflowModes.Overflow;

        MkBtn(card, "Jugar de nuevo", C(0.85f, 0.66f, 0.05f),
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

    /// <summary>Pastilla redondeada (Image con sprite 9-slice de KidUI).</summary>
    RectTransform Pill(RectTransform p, string n, Color col,
                       Vector2 am, Vector2 aM, float cornerScale)
    {
        var rt  = Img(p, n, col, am, aM, Vector2.zero, Vector2.zero);
        var img = rt.GetComponent<Image>();
        img.sprite                  = KidUI.RoundedSprite;
        img.type                    = Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = cornerScale;
        return rt;
    }

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
        var rt = Pill(p, "Btn_" + lbl, bg, am, aM, 1.2f);
        var b  = rt.gameObject.AddComponent<Button>();
        b.targetGraphic = rt.GetComponent<Image>();
        var cb = b.colors;
        cb.normalColor      = Color.white;
        cb.highlightedColor = new Color(1f, 1f, 1f, .82f);
        cb.pressedColor     = new Color(.72f, .72f, .72f);
        b.colors = cb;
        b.onClick.AddListener(() => click?.Invoke());
        var t = Txt(rt, "T", lbl, Color.white, 24, V(0, 0), V(1, 1));
        t.fontStyle = FontStyles.Bold;
        ButtonJuice.Attach(rt.gameObject);
    }
}
