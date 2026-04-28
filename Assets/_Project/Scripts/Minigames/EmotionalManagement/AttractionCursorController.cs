using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Gestiona el cursor del jugador en "Atraccion Emocional".
///
/// MODELO FISICO:
///   El cursor mostrado NO sigue directamente al raton.
///   En su lugar, acumula un "desplazamiento de atraccion" (_pullOffset)
///   que representa cuanto lo estan arrastrando los estimulos negativos.
///
///   Posicion mostrada = PosicionRaton(canvas) + _pullOffset
///
///   Cada frame:
///     1. Se calcula la fuerza total de los estimulos sobre el cursor actual
///     2. _pullOffset += fuerza * Time.deltaTime   (acumula el arrastre)
///     3. _pullOffset *= decaimiento               (vuelve a 0 si no hay fuerza)
///     4. Cursor mostrado = raton + _pullOffset
///
///   El jugador debe mover el raton en la direccion opuesta a los estimulos
///   para mantener el cursor mostrado dentro de la zona segura.
///
/// PARAMETROS AJUSTABLES:
///   damping: velocidad con la que el pullOffset decae (>= 1 = decaimiento rapido)
///   maxPullOffset: limite maximo del desplazamiento (evita que se dispare)
/// </summary>
public class AttractionCursorController : MonoBehaviour
{
    // ── Estado ────────────────────────────────────────────────────────────
    public Vector2 CursorCanvasPos { get; private set; }

    // ── Config ────────────────────────────────────────────────────────────
    [HideInInspector] public float damping             = 2.2f;   // decaimiento/s del pullOffset
    [HideInInspector] public float maxPullOffset       = 280f;   // limite max (canvas units)
    [HideInInspector] public float cursorRadius        = 18f;
    /// <summary>
    /// Fuerza de inestabilidad de zona (canvas units/s^2).
    /// Rota lentamente: el jugador debe compensar activamente incluso en zona segura.
    /// </summary>
    [HideInInspector] public float instabilityStrength = 0f;

    // ── Referencias internas ──────────────────────────────────────────────
    RectTransform      _canvasRT;
    RectTransform      _cursorRT;
    Image              _cursorImg;
    AttractionController _attraction;

    Vector2 _pullOffset = Vector2.zero;

    // Colores del cursor segun nivel de peligro
    static readonly Color COL_SAFE   = new Color(0.85f, 0.95f, 1.00f, 1f);
    static readonly Color COL_WARN   = new Color(1.00f, 0.88f, 0.20f, 1f);
    static readonly Color COL_DANGER = new Color(0.95f, 0.45f, 0.15f, 1f);
    static readonly Color COL_CRIT   = new Color(0.95f, 0.18f, 0.22f, 1f);

    // ═════════════════════════════════════════════════════════════════════
    // Inicializacion
    // ═════════════════════════════════════════════════════════════════════

    public void Initialize(RectTransform canvasRT, RectTransform cursorRT,
                           AttractionController attraction)
    {
        _canvasRT   = canvasRT;
        _cursorRT   = cursorRT;
        _cursorImg  = cursorRT.GetComponent<Image>();
        _attraction = attraction;
        _pullOffset = Vector2.zero;

        // Posicion inicial = centro de pantalla
        CursorCanvasPos = Vector2.zero;
        _cursorRT.anchoredPosition = Vector2.zero;
    }

    // ═════════════════════════════════════════════════════════════════════
    // Actualizacion por frame (llamado desde GameManager.Update)
    // ═════════════════════════════════════════════════════════════════════

    public void Tick()
    {
        float dt = Time.deltaTime;

        // 1. Posicion del raton en coordenadas del canvas
        Vector2 mouseCanvasPos = GetMouseCanvasPos();

        // 2. Fuerza total de los estimulos sobre el cursor actual
        Vector2 force = _attraction.CalculateTotalForce(CursorCanvasPos);

        // 3. Fuerza de inestabilidad de zona: rota lentamente (~9 seg/vuelta).
        //    Actua siempre, incluso en la zona segura, para que el jugador
        //    no pueda simplemente quedarse quieto y ganar.
        if (instabilityStrength > 0f)
        {
            float angle = Time.time * 0.70f; // ~9s por vuelta completa
            // Segunda componente con frecuencia ligeramente diferente para
            // evitar patron circular perfecto (mas impredecible)
            float angle2 = Time.time * 0.43f + 1.2f;
            var drift = new Vector2(
                Mathf.Cos(angle)  * 0.65f + Mathf.Cos(angle2) * 0.35f,
                Mathf.Sin(angle)  * 0.65f + Mathf.Sin(angle2) * 0.35f);
            force += drift.normalized * instabilityStrength;
        }

        // 4. Acumular desplazamiento de atraccion
        _pullOffset += force * dt;

        // 5. Decaimiento suave del offset (el jugador "reconduce" el cursor)
        _pullOffset = Vector2.Lerp(_pullOffset, Vector2.zero, damping * dt);

        // 6. Limitar el offset maximo (evita que el cursor se dispare)
        if (_pullOffset.magnitude > maxPullOffset)
            _pullOffset = _pullOffset.normalized * maxPullOffset;

        // 7. Posicion mostrada
        CursorCanvasPos = mouseCanvasPos + _pullOffset;
        _cursorRT.anchoredPosition = CursorCanvasPos;

        // 8. Color segun nivel de peligro
        float danger = _attraction.GetDangerLevel(CursorCanvasPos);
        UpdateCursorColor(danger);
    }

    // ═════════════════════════════════════════════════════════════════════
    // Consultas
    // ═════════════════════════════════════════════════════════════════════

    /// <summary>True si el cursor esta dentro de la zona segura central.</summary>
    public bool IsInSafeZone(float safeRadius)
    {
        return CursorCanvasPos.magnitude <= safeRadius;
    }

    /// <summary>True si el cursor esta tocando algun estimulo negativo.</summary>
    public bool IsTouchingStimulus()
    {
        return _attraction.IsTouchingAny(CursorCanvasPos, cursorRadius);
    }

    /// <summary>Nivel de peligro 0-1 basado en proximidad a estimulos.</summary>
    public float DangerLevel => _attraction.GetDangerLevel(CursorCanvasPos);

    // ═════════════════════════════════════════════════════════════════════
    // Helpers internos
    // ═════════════════════════════════════════════════════════════════════

    Vector2 GetMouseCanvasPos()
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRT, Input.mousePosition, null, out Vector2 localPos);
        return localPos;
    }

    void UpdateCursorColor(float danger)
    {
        if (_cursorImg == null) return;

        Color col;
        if      (danger < 0.25f) col = Color.Lerp(COL_SAFE,   COL_WARN,   danger / 0.25f);
        else if (danger < 0.55f) col = Color.Lerp(COL_WARN,   COL_DANGER, (danger - 0.25f) / 0.30f);
        else                     col = Color.Lerp(COL_DANGER,  COL_CRIT,   (danger - 0.55f) / 0.45f);

        _cursorImg.color = col;
    }
}
