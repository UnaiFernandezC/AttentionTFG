using UnityEngine;

public class RegulationEmotionManager
{
    public const int COOLDOWN_TURNS = 2;

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

    public float CurrentLevel         { get; private set; }
    public int   StepsTaken           { get; private set; }
    public float RegenerationPerTurn  { get; private set; }
    public float LastRegenAmount      { get; private set; }
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

    public EmotionAction ApplyAction(int index)
    {
        if (!CanUseAction(index)) return null;

        var action = ACTIONS[index];

        LastRegenAmount = RegenerationPerTurn;
        CurrentLevel   += RegenerationPerTurn;

        CurrentLevel = Mathf.Max(0f, CurrentLevel + action.impact);

        _cooldowns[index] = COOLDOWN_TURNS + 1;
        for (int i = 0; i < _cooldowns.Length; i++)
            if (_cooldowns[i] > 0) _cooldowns[i]--;

        StepsTaken++;
        return action;
    }

    public int CalculateScore()
    {
        return Mathf.Max(0, 200 - Mathf.Max(0, StepsTaken - 8) * 10);
    }
}
