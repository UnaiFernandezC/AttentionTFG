// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using System;
using UnityEngine;

public enum RSStimColor { Red, Blue, Green, Yellow }

public enum RSRuleType
{
    ClickRed, ClickBlue, ClickGreen, ClickYellow,
    // Reglas INVERSAS (dificil): pulsa todos MENOS ese color
    AvoidRed, AvoidBlue, AvoidGreen, AvoidYellow
}

public class RSStimData
{
    public RSStimColor Color;
}

public class RuleSwitchRuleManager : MonoBehaviour
{
    [Header("Reglas disponibles para esta dificultad")]
    public RSRuleType[] availableRules = new RSRuleType[]
        { RSRuleType.ClickRed, RSRuleType.ClickBlue, RSRuleType.ClickGreen };

    public RSRuleType CurrentRule { get; private set; }

    public event Action<RSRuleType> OnRuleChanged;

    public void SetInitialRule()
    {
        CurrentRule = availableRules[UnityEngine.Random.Range(0, availableRules.Length)];
    }

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

    public bool Matches(RSStimData s)
    {
        switch (CurrentRule)
        {
            case RSRuleType.ClickRed:    return s.Color == RSStimColor.Red;
            case RSRuleType.ClickBlue:   return s.Color == RSStimColor.Blue;
            case RSRuleType.ClickGreen:  return s.Color == RSStimColor.Green;
            case RSRuleType.ClickYellow: return s.Color == RSStimColor.Yellow;
            case RSRuleType.AvoidRed:    return s.Color != RSStimColor.Red;
            case RSRuleType.AvoidBlue:   return s.Color != RSStimColor.Blue;
            case RSRuleType.AvoidGreen:  return s.Color != RSStimColor.Green;
            case RSRuleType.AvoidYellow: return s.Color != RSStimColor.Yellow;
            default: return false;
        }
    }

    public bool IsCorrect(RSStimData s, bool playerClicked)
        => Matches(s) == playerClicked;

    public string GetRuleText(RSRuleType r)
    {
        switch (r)
        {
            case RSRuleType.ClickRed:    return "Pulsa solo los ROJOS";
            case RSRuleType.ClickBlue:   return "Pulsa solo los AZULES";
            case RSRuleType.ClickGreen:  return "Pulsa solo los VERDES";
            case RSRuleType.ClickYellow: return "Pulsa solo los AMARILLOS";
            case RSRuleType.AvoidRed:    return "Pulsa TODOS MENOS el rojo";
            case RSRuleType.AvoidBlue:   return "Pulsa TODOS MENOS el azul";
            case RSRuleType.AvoidGreen:  return "Pulsa TODOS MENOS el verde";
            case RSRuleType.AvoidYellow: return "Pulsa TODOS MENOS el amarillo";
            default: return "—";
        }
    }

    public string GetCurrentRuleText() => GetRuleText(CurrentRule);

    public static Color GetStimColor(RSStimColor c)
    {
        switch (c)
        {
            case RSStimColor.Red:    return new Color(0.88f, 0.22f, 0.22f);
            case RSStimColor.Blue:   return new Color(0.22f, 0.52f, 0.90f);
            case RSStimColor.Green:  return new Color(0.18f, 0.80f, 0.38f);
            case RSStimColor.Yellow: return new Color(0.95f, 0.78f, 0.10f);
            default: return Color.white;
        }
    }

    public static string GetColorName(RSStimColor c)
    {
        switch (c)
        {
            case RSStimColor.Red:    return "ROJO";
            case RSStimColor.Blue:   return "AZUL";
            case RSStimColor.Green:  return "VERDE";
            case RSStimColor.Yellow: return "AMARILLO";
            default: return "";
        }
    }

    public static Color GetRuleColor(RSRuleType r)
    {
        switch (r)
        {
            case RSRuleType.ClickRed:    return GetStimColor(RSStimColor.Red);
            case RSRuleType.ClickBlue:   return GetStimColor(RSStimColor.Blue);
            case RSRuleType.ClickGreen:  return GetStimColor(RSStimColor.Green);
            case RSRuleType.ClickYellow: return GetStimColor(RSStimColor.Yellow);
            case RSRuleType.AvoidRed:    return GetStimColor(RSStimColor.Red);
            case RSRuleType.AvoidBlue:   return GetStimColor(RSStimColor.Blue);
            case RSRuleType.AvoidGreen:  return GetStimColor(RSStimColor.Green);
            case RSRuleType.AvoidYellow: return GetStimColor(RSStimColor.Yellow);
            default: return Color.white;
        }
    }

    public static RSStimColor RuleToStimColor(RSRuleType r)
    {
        switch (r)
        {
            case RSRuleType.ClickRed:    return RSStimColor.Red;
            case RSRuleType.ClickBlue:   return RSStimColor.Blue;
            case RSRuleType.ClickGreen:  return RSStimColor.Green;
            case RSRuleType.ClickYellow: return RSStimColor.Yellow;
            case RSRuleType.AvoidRed:    return RSStimColor.Red;
            case RSRuleType.AvoidBlue:   return RSStimColor.Blue;
            case RSRuleType.AvoidGreen:  return RSStimColor.Green;
            case RSRuleType.AvoidYellow: return RSStimColor.Yellow;
            default: return RSStimColor.Red;
        }
    }
}
