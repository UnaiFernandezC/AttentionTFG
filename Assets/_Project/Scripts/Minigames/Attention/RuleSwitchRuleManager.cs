using System;
using UnityEngine;

public enum RSStimColor { Red, Blue, Green }
public enum RSRuleType  { ClickRed, ClickBlue, ClickGreen }

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
            case RSRuleType.ClickRed:   return s.Color == RSStimColor.Red;
            case RSRuleType.ClickBlue:  return s.Color == RSStimColor.Blue;
            case RSRuleType.ClickGreen: return s.Color == RSStimColor.Green;
            default: return false;
        }
    }

    public bool IsCorrect(RSStimData s, bool playerClicked)
        => Matches(s) == playerClicked;

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
