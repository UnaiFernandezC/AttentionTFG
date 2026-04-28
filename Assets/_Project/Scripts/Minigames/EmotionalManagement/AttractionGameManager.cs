using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// GameManager del minijuego "Atraccion Emocional".
/// Hereda MinigameBase → panel de introduccion automatico.
///
/// FLUJO DE JUEGO:
///   1. IntroPanel (MinigameBase) → jugador pulsa Comenzar
///   2. OnMinigameStart() → construye UI, crea estimulos, inicializa cursor
///   3. Update()          → actualiza cursor, comprueba colisiones y zona segura
///   4a. safeTime >= targetSafeTime  → victoria
///   4b. lives <= 0                  → derrota
///
/// MECANICA:
///   El cursor mostrado = posicion raton + offset de atraccion.
///   Los estimulos negativos (circulos rojos) atraen el cursor hacia ellos.
///   El jugador debe mover el raton para mantener el cursor dentro de la
///   zona segura (circulo verde central) durante [targetSafeTime] segundos.
///   Cada contacto con un estimulo resta una vida y aplica invulnerabilidad breve.
///
/// AJUSTE DE DIFICULTAD (Inspector):
///   targetSafeTime:     15s (F) / 20s (M) / 25s (D)
///   startLives:         3   (F) / 3   (M) / 2   (D)
///   attractionStrength: 160 (F) / 260 (M) / 360 (D)
///   influenceRadius:    320 (F) / 380 (M) / 430 (D)
///   stimulusCount:      3   (F) / 4   (M) / 5   (D)
/// </summary>
public class AttractionGameManager : MinigameBase
{
    // ── Inspector ─────────────────────────────────────────────────────────
    [Header("Condicion de victoria")]
    public float targetSafeTime = 15f;

    [Header("Vidas del jugador")]
    public int startLives = 3;

    [Header("Estimulos")]
    public int   stimulusCount      = 3;
    public float attractionStrength = 160f;
    public float influenceRadius    = 320f;
    public float contactRadius      = 46f;

    [Header("Zona segura (radio en canvas units)")]
    public float safeZoneRadius = 115f;

    [Header("Fisica del cursor (menor damping = mas dificil)")]
    public float dampingFactor = 1.4f;

    [Header("Inestabilidad de zona (0 = desactivado)")]
    public float instabilityStrength = 110f;

    // ── Componentes ───────────────────────────────────────────────────────
    AttractionController    _attraction;
    AttractionCursorController _cursor;
    AttractionUIController  _ui;

    // ── Estado ────────────────────────────────────────────────────────────
    float _safeTime;
    int   _lives;
    bool  _invulnerable;
    const float INVULN_DURATION = 1.2f;

    // ═════════════════════════════════════════════════════════════════════

    protected override string GetIntroDescription() =>
        "Los circulos rojos atraen tu cursor hacia ellos.\n" +
        "Mueve el raton para resistir y quedarte en la zona verde.\n\n" +
        "Cada contacto con un circulo rojo te quita una vida.\n" +
        "Aguanta " + targetSafeTime.ToString("0") + " segundos en la zona segura para ganar.";

    protected override void OnMinigameStart()
    {
        EnsureEventSystem();

        _attraction = GetComponent<AttractionController>();
        _cursor     = GetComponent<AttractionCursorController>();
        _ui         = GetComponent<AttractionUIController>();

        // 1. Construir UI y obtener referencias de canvas
        _ui.BuildUI(safeZoneRadius, () => RestartMinigame(), () => ReturnToGameSelector());

        // 2. Crear estimulos en el canvas del juego
        var positions = GetStimulusPositions(stimulusCount);
        _attraction.BuildStimuli(_ui.GameAreaRT, positions,
                                 attractionStrength, influenceRadius, contactRadius);

        // 3. Inicializar cursor
        _cursor.damping             = dampingFactor;
        _cursor.maxPullOffset       = 380f;
        _cursor.cursorRadius        = 18f;
        _cursor.instabilityStrength = instabilityStrength;
        _cursor.Initialize(_ui.CanvasRT, _ui.CursorRT, _attraction);

        // 4. Estado inicial
        _safeTime    = 0f;
        _lives       = startLives;
        _invulnerable = false;

        _ui.UpdateLives(_lives, startLives);
        _ui.UpdateSafeBar(0f, targetSafeTime);
    }

    protected override void OnMinigameComplete() { }
    protected override void OnMinigameFailed()   { }

    // ═════════════════════════════════════════════════════════════════════
    // Bucle de juego
    // ═════════════════════════════════════════════════════════════════════

    void Update()
    {
        if (!IsPlaying) return;

        // Actualizar cursor (fisica de atraccion)
        _cursor.Tick();

        bool inSafe   = _cursor.IsInSafeZone(safeZoneRadius);
        bool touching = !_invulnerable && _cursor.IsTouchingStimulus();

        // Acumular tiempo seguro
        if (inSafe)
        {
            _safeTime += Time.deltaTime;
            _ui.UpdateSafeBar(_safeTime, targetSafeTime);
            _ui.SetSafeZoneActive(true);
        }
        else
        {
            _ui.SetSafeZoneActive(false);
        }

        // Actualizar indicador de peligro en UI
        _ui.UpdateDangerIndicator(_cursor.DangerLevel, inSafe);

        // Contacto con estimulo
        if (touching)
        {
            _lives--;
            _ui.UpdateLives(_lives, startLives);
            _ui.FlashHit();
            StartCoroutine(InvulnerabilityRoutine());

            if (_lives <= 0)
            {
                EndGame(won: false);
                return;
            }
        }

        // Victoria
        if (_safeTime >= targetSafeTime)
        {
            EndGame(won: true);
        }
    }

    // ═════════════════════════════════════════════════════════════════════
    // Fin de juego
    // ═════════════════════════════════════════════════════════════════════

    void EndGame(bool won)
    {
        int score = won ? Mathf.RoundToInt(200f - _cursor.DangerLevel * 20f) : 0;
        CompleteMinigame(score);
        _ui.ShowResult(won, score, _safeTime, targetSafeTime);
    }

    IEnumerator InvulnerabilityRoutine()
    {
        _invulnerable = true;
        yield return new WaitForSeconds(INVULN_DURATION);
        _invulnerable = false;
    }

    // ═════════════════════════════════════════════════════════════════════
    // Posiciones de estimulos segun cantidad
    // ═════════════════════════════════════════════════════════════════════

    static List<Vector2> GetStimulusPositions(int count)
    {
        // Distribuidos a ~340-380 canvas units del centro.
        // Mas cerca que antes para que SIEMPRE ejerzan fuerza sobre el cursor,
        // incluso cuando el jugador esta en la zona segura central.
        var all = new List<Vector2>
        {
            new Vector2(-370f,    0f),   // izquierda
            new Vector2( 370f,    0f),   // derecha
            new Vector2(   0f,  310f),   // arriba
            new Vector2(   0f, -310f),   // abajo
            new Vector2(-280f,  240f),   // diagonal superior-izq
        };

        var result = new List<Vector2>();
        for (int i = 0; i < Mathf.Min(count, all.Count); i++)
            result.Add(all[i]);
        return result;
    }

    // ═════════════════════════════════════════════════════════════════════
    static void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() == null)
        {
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }
    }
}
