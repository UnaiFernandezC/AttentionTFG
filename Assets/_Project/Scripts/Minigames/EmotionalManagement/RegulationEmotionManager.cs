using UnityEngine;

/// <summary>
/// Gestiona el estado emocional y las acciones disponibles en
/// "Regulacion Progresiva".
///
/// Nivel emocional: 0-∞  (empieza en 100 = muy alterado)
/// Objetivo: bajar a <= 10
///
/// MECANICA CENTRAL — REGENERACION AUTOMATICA:
///   Al comienzo de cada turno, ANTES de que el jugador actue,
///   el nivel sube automaticamente +RegenerationPerTurn.
///   Esto convierte las acciones debiles en contraproducentes:
///
///     Respirar (-22)         →  neto -14 ✓
///     Hablar (-18)           →  neto -10 ✓
///     Caminar (-16)          →  neto  -8 ✓
///     Pensar (-12)           →  neto  -4 (apenas ayuda)
///     Ignorar (-2)           →  neto  +6  ← EMPEORA
///     Reaccionar (+15)       →  neto +23  ← DESASTRE
///
/// COOLDOWN (2 turnos):
///   Tras usar una accion, queda bloqueada 2 turnos.
///   Junto con la regeneracion, obliga a seleccionar
///   cuidadosamente para no quedarse sin acciones utiles.
/// </summary>
public class RegulationEmotionManager
{
    public const int COOLDOWN_TURNS = 2;

    // ── Definicion de acciones ────────────────────────────────────────────

    public class EmotionAction
    {
        public string name;
        public int    impact;
        public string feedbackGood;
        public string feedbackBad;

        public EmotionAction(string n, int imp, string fGood, string fBad = "")
        {
            name         = n;
            impact       = imp;
            feedbackGood = fGood;
            feedbackBad  = string.IsNullOrEmpty(fBad) ? fGood : fBad;
        }
    }

    public static readonly EmotionAction[] ACTIONS = new EmotionAction[]
    {
        new EmotionAction(
            "Respirar profundamente",  -22,
            "La respiracion profunda activa el sistema nervioso parasimpatico,\n" +
            "reduciendo el cortisol y calmando la respuesta de estres rapidamente."),

        new EmotionAction(
            "Hablar con alguien\nde confianza",  -18,
            "Compartir lo que sentimos alivia la carga emocional.\n" +
            "El apoyo social es uno de los mejores reguladores del bienestar."),

        new EmotionAction(
            "Salir a caminar",  -16,
            "El movimiento fisico libera endorfinas y aleja la mente\n" +
            "del bucle de pensamientos negativos."),

        new EmotionAction(
            "Pensar en algo positivo",  -12,
            "Reencuadrar ayuda, pero la activacion sigue subiendo.\n" +
            "Esta accion apenas compensa la tension acumulada."),

        new EmotionAction(
            "Ignorar el problema",   -2,
            "Evitar no resuelve el problema: la tension sigue subiendo.\n" +
            "Esta accion es casi inutil cuando el nivel se regenera solo.",
            "Evitar no resuelve el problema: la tension sigue subiendo.\n" +
            "Esta accion es casi inutil cuando el nivel se regenera solo."),

        new EmotionAction(
            "Reaccionar con ira",   +15,
            "La ira descontrolada suma a la regeneracion natural.\n" +
            "El nivel sube muchisimo: evita esta accion a toda costa.",
            "La ira descontrolada suma a la regeneracion natural.\n" +
            "El nivel sube muchisimo: evita esta accion a toda costa.")
    };

    // ── Estado ────────────────────────────────────────────────────────────

    public float CurrentLevel         { get; private set; }
    public int   StepsTaken           { get; private set; }
    public float RegenerationPerTurn  { get; private set; }
    public float LastRegenAmount      { get; private set; }   // para mostrar en UI
    public bool  IsWon                => CurrentLevel <= 10f;

    private int[] _cooldowns;

    public RegulationEmotionManager(float startLevel = 100f, float regenPerTurn = 8f)
    {
        CurrentLevel        = startLevel;
        RegenerationPerTurn = regenPerTurn;
        StepsTaken          = 0;
        LastRegenAmount     = 0f;
        _cooldowns          = new int[ACTIONS.Length];
    }

    public bool CanUseAction(int index)
    {
        if (index < 0 || index >= ACTIONS.Length) return false;
        return _cooldowns[index] <= 0;
    }

    public int GetCooldown(int index)
    {
        if (index < 0 || index >= ACTIONS.Length) return 0;
        return _cooldowns[index];
    }

    /// <summary>
    /// Aplica la accion indicada (si no esta en cooldown).
    /// Primero aplica la regeneracion automatica, luego el efecto de la accion.
    /// Devuelve null si la accion esta bloqueada.
    /// </summary>
    public EmotionAction ApplyAction(int index)
    {
        if (!CanUseAction(index)) return null;

        var action = ACTIONS[index];

        // 1. Regeneracion automatica (la tension siempre sube)
        LastRegenAmount = RegenerationPerTurn;
        CurrentLevel   += RegenerationPerTurn;   // sin tope superior: puede superar 100

        // 2. Efecto de la accion
        CurrentLevel = Mathf.Max(0f, CurrentLevel + action.impact);

        // 3. Actualizar cooldowns
        _cooldowns[index] = COOLDOWN_TURNS + 1;
        for (int i = 0; i < _cooldowns.Length; i++)
            if (_cooldowns[i] > 0) _cooldowns[i]--;

        StepsTaken++;
        return action;
    }

    /// <summary>Puntuacion: base 200, -10 por cada paso sobre el optimo (8).</summary>
    public int CalculateScore()
    {
        return Mathf.Max(0, 200 - Mathf.Max(0, StepsTaken - 8) * 10);
    }
}
