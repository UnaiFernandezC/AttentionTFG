using UnityEngine;

/// <summary>
/// Dirección de respuesta del minijuego "No sigas la mayoría".
/// </summary>
public enum DFMDirection { Left, Right, Up, Down }

/// <summary>
/// Genera y almacena la regla de cada ronda:
///   - MajorityDirection → dirección incorrecta (la mayoría de flechas)
///   - CorrectDirection  → dirección correcta   (la minoría de flechas)
/// </summary>
public class DontFollowMajorityRuleManager : MonoBehaviour
{
    // ------------------------------------------------------------------ //
    // Estado de la ronda actual
    // ------------------------------------------------------------------ //
    public DFMDirection CorrectDirection  { get; private set; }
    public DFMDirection MajorityDirection { get; private set; }

    // ------------------------------------------------------------------ //
    // Generación
    // ------------------------------------------------------------------ //

    /// <summary>
    /// Selecciona dos direcciones distintas al azar:
    /// una para la mayoría (incorrecta) y otra para la minoría (correcta).
    /// </summary>
    public void GenerateRound()
    {
        var all = new[] { DFMDirection.Left, DFMDirection.Right,
                          DFMDirection.Up,   DFMDirection.Down };

        int a = Random.Range(0, 4);
        int b;
        do { b = Random.Range(0, 4); } while (b == a);

        CorrectDirection  = all[a];   // minoría → respuesta correcta
        MajorityDirection = all[b];   // mayoría → trampa
    }

    // ------------------------------------------------------------------ //
    // Evaluación
    // ------------------------------------------------------------------ //
    public bool IsCorrect(DFMDirection pressed) => pressed == CorrectDirection;

    // ------------------------------------------------------------------ //
    // Helpers estáticos
    // ------------------------------------------------------------------ //
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
