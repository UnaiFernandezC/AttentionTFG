using UnityEngine;

public enum DFMDirection { Left, Right, Up, Down }

public class DontFollowMajorityRuleManager : MonoBehaviour
{

    public DFMDirection CorrectDirection  { get; private set; }
    public DFMDirection MajorityDirection { get; private set; }

    public void GenerateRound()
    {
        var all = new[] { DFMDirection.Left, DFMDirection.Right,
                          DFMDirection.Up,   DFMDirection.Down };

        int a = Random.Range(0, 4);
        int b;
        do { b = Random.Range(0, 4); } while (b == a);

        CorrectDirection  = all[a];
        MajorityDirection = all[b];
    }

    public bool IsCorrect(DFMDirection pressed) => pressed == CorrectDirection;

    public static string ArrowSymbol(DFMDirection d)
    {
        switch (d)
        {
            case DFMDirection.Left:  return "←";
            case DFMDirection.Right: return "→";
            case DFMDirection.Up:    return "↑";
            case DFMDirection.Down:  return "↓";
            default: return "?";
        }
    }

    public static string DirectionName(DFMDirection d)
    {
        switch (d)
        {
            case DFMDirection.Left:  return "Izquierda";
            case DFMDirection.Right: return "Derecha";
            case DFMDirection.Up:    return "Arriba";
            case DFMDirection.Down:  return "Abajo";
            default: return "?";
        }
    }
}
