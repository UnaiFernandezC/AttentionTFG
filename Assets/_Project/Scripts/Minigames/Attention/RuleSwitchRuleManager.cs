using System;
using UnityEngine;

// ── Tipos compartidos (accesibles desde todos los scripts del minijuego) ─────

public enum RSStimColor { Red, Blue, Green }
public enum RSRuleType  { ClickRed, ClickBlue, ClickGreen }

/// <summary>Datos de un estímulo: solo color (Easy). Se puede ampliar con Shape para Medium/Hard.</summary>
public class RSStimData
{
    public RSStimColor Color;
}

// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Gestiona la regla activa del minijuego "Cambio de regla".
///
/// La regla puede cambiar silenciosamente (SwitchRule) durante la partida.
/// El GameManager decide si avisar o no al jugador; este script NO notifica la UI.
///
/// Dificultad (adjusting reglas disponibles):
///   Fácil   → availableRules = [ClickRed, ClickBlue, ClickGreen]  (solo colores)
///   Medio   → añadir formas en versiones futuras
///   Difícil → ídem con más variedad y sin pistas
/// </summary>
public class RuleSwitchRuleManager : MonoBehaviour
{
    [Header("Reglas disponibles para esta dificultad")]
    public RSRuleType[] availableRules = new RSRuleType[]
        { RSRuleType.ClickRed, RSRuleType.ClickBlue, RSRuleType.ClickGreen };

    public RSRuleType CurrentRule { get; private set; }

    /// <summary>
    /// Disparado cuando la regla cambia.
    /// El GameManager escucha esto para decidir si actualiza la UI o no.
    /// </summary>
    public event Action<RSRuleType> OnRuleChanged;

    // ─────────────────────────────────────────────────────────────────

    /// <summary>Establece una regla inicial aleatoria.</summary>
    public void SetInitialRule()
    {
        CurrentRule = availableRules[UnityEngine.Random.Range(0, availableRules.Length)];
    }

    /// <summary>
    /// Cambia SILENCIOSAMENTE la regla a una diferente de la activa.
    /// Siempre elige una regla distinta a la actual.
    /// </summary>
    public void SwitchRule()
    {
        if (availableRules.Length < 2) return;
        RSRuleType next;
        int tries = 0;
        do
        {
            next = availableRules[UnityEngine.Random.Range(0, availableRules.Length)];
            tries++;
        }
        while (next == CurrentRule && tries < 20);

        CurrentRule = next;
        OnRuleChanged?.Invoke(CurrentRule);
    }

    // ─────────────────────────────────────────────────────────────────
    // Evaluación
    // ─────────────────────────────────────────────────────────────────

    /// <summary>¿El estímulo coincide con la regla activa?</summary>
    public bool Matches(RSStimData s)
    {
        switch (CurrentRule)
        {
            case RSRuleType.ClickRed:   return s.Color == RSStimColor.Red;
            case RSRuleType.ClickBlue:  return s.Color == RSStimColor.Blue;
            case RSRuleType.ClickGreen: return s.Color == RSStimColor.Green;
            default: return false;
        }
    }

    /// <summary>
    /// ¿La respuesta del jugador es correcta?
    /// Correcto si: (coincide Y pulsó) O (no coincide Y no pulsó).
    /// </summary>
    public bool IsCorrect(RSStimData s, bool playerClicked)
        => Matches(s) == playerClicked;

    // ─────────────────────────────────────────────────────────────────
    // Helpers de texto y color
    // ─────────────────────────────────────────────────────────────────

    public string GetRuleText(RSRuleType r)
    {
        switch (r)
        {
            case RSRuleType.ClickRed:   return "Pulsa solo los ROJOS";
            case RSRuleType.ClickBlue:  return "Pulsa solo los AZULES";
            case RSRuleType.ClickGreen: return "Pulsa solo los VERDES";
            default: return "—";
        }
    }

    public string GetCurrentRuleText() => GetRuleText(CurrentRule);

    public static Color GetStimColor(RSStimColor c)
    {
        switch (c)
        {
            case RSStimColor.Red:   return new Color(0.88f, 0.22f, 0.22f);
            case RSStimColor.Blue:  return new Color(0.22f, 0.52f, 0.90f);
            case RSStimColor.Green: return new Color(0.18f, 0.80f, 0.38f);
            default: return Color.white;
        }
    }

    public static string GetColorName(RSStimColor c)
    {
        switch (c)
        {
            case RSStimColor.Red:   return "ROJO";
            case RSStimColor.Blue:  return "AZUL";
            case RSStimColor.Green: return "VERDE";
            default: return "";
        }
    }

    /// <summary>Devuelve el color que debe mostrar el dot indicador de la UI para una regla.</summary>
    public static Color GetRuleColor(RSRuleType r)
    {
        switch (r)
        {
            case RSRuleType.ClickRed:   return GetStimColor(RSStimColor.Red);
            case RSRuleType.ClickBlue:  return GetStimColor(RSStimColor.Blue);
            case RSRuleType.ClickGreen: return GetStimColor(RSStimColor.Green);
            default: return Color.white;
        }
    }

    public static RSStimColor RuleToStimColor(RSRuleType r)
    {
        switch (r)
        {
            case RSRuleType.ClickRed:   return RSStimColor.Red;
            case RSRuleType.ClickBlue:  return RSStimColor.Blue;
            case RSRuleType.ClickGreen: return RSStimColor.Green;
            default: return RSStimColor.Red;
        }
    }
}
