using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controla el aspecto visual del boton central en "No pulses todavia".
///
/// ESTADOS VISUALES:
///   Idle    → gris oscuro / apagado (antes de que empiece la ronda)
///   Waiting → rojo pulsante ("NO PULSES")
///   FakeOut → flash amarillo breve (señal falsa, dificultades mas altas)
///   Active  → verde brillante ("¡PULSA AHORA!")
///   Correct → azul/verde flash ("¡Bien hecho!")
///   Early   → rojo oscuro flash ("¡Demasiado pronto!")
///   Missed  → gris flash ("Tiempo agotado")
///
/// Esta clase NO contiene logica de juego: solo responde a llamadas del GameManager.
/// </summary>
public class DontPressButtonController : MonoBehaviour
{
    // ── Colores de cada estado ────────────────────────────────────────────
    static readonly Color COL_IDLE    = new Color(0.14f, 0.19f, 0.30f, 1f);
    static readonly Color COL_WAIT    = new Color(0.80f, 0.18f, 0.22f, 1f);  // rojo
    static readonly Color COL_FAKE    = new Color(0.92f, 0.75f, 0.10f, 1f);  // amarillo
    static readonly Color COL_ACTIVE  = new Color(0.18f, 0.80f, 0.45f, 1f);  // verde
    static readonly Color COL_CORRECT = new Color(0.20f, 0.60f, 0.90f, 1f);  // azul
    static readonly Color COL_EARLY   = new Color(0.55f, 0.10f, 0.15f, 1f);  // rojo oscuro
    static readonly Color COL_MISSED  = new Color(0.35f, 0.38f, 0.45f, 1f);  // gris

    // ── Textos de cada estado ─────────────────────────────────────────────
    const string TXT_IDLE    = "Preparado";
    const string TXT_WAIT    = "NO\nPULSES";
    const string TXT_FAKE    = "¡NO!";
    const string TXT_ACTIVE  = "¡PULSA\nAHORA!";
    const string TXT_CORRECT = "¡BIEN\nHECHO!";
    const string TXT_EARLY   = "¡DEMASIADO\nPRONTO!";
    const string TXT_MISSED  = "TIEMPO\nAGOTADO";

    // ── Referencias (asignadas por DontPressUIController) ─────────────────
    [HideInInspector] public Image           ButtonImage;
    [HideInInspector] public Image           GlowImage;   // halo/resplandor exterior
    [HideInInspector] public TextMeshProUGUI ButtonText;

    // ── Estado ────────────────────────────────────────────────────────────
    public enum State { Idle, Waiting, FakeOut, Active, Correct, Early, Missed }
    State   _state   = State.Idle;
    float   _pulseT  = 0f;
    bool    _pulsing = false;

    // ═════════════════════════════════════════════════════════════════════
    // API publica
    // ═════════════════════════════════════════════════════════════════════

    public void SetIdle()    => Apply(State.Idle,    COL_IDLE,    TXT_IDLE,    pulse: false);
    public void SetWaiting() => Apply(State.Waiting, COL_WAIT,    TXT_WAIT,    pulse: true);
    public void SetFakeOut() => Apply(State.FakeOut, COL_FAKE,    TXT_FAKE,    pulse: false);
    public void SetActive()  => Apply(State.Active,  COL_ACTIVE,  TXT_ACTIVE,  pulse: false);
    public void SetCorrect() => Apply(State.Correct, COL_CORRECT, TXT_CORRECT, pulse: false);
    public void SetEarly()   => Apply(State.Early,   COL_EARLY,   TXT_EARLY,   pulse: false);
    public void SetMissed()  => Apply(State.Missed,  COL_MISSED,  TXT_MISSED,  pulse: false);

    // ── Actualizacion de pulsacion (llamado desde Update del GameManager) ─
    public void Tick()
    {
        if (!_pulsing || ButtonImage == null) return;

        _pulseT += Time.deltaTime * 2.8f;
        float pulse = 0.80f + Mathf.Sin(_pulseT) * 0.20f;

        // Atenuar ligeramente la intensidad del color con el latido
        ButtonImage.color = new Color(
            COL_WAIT.r * pulse,
            COL_WAIT.g * pulse,
            COL_WAIT.b * pulse, 1f);

        if (GlowImage != null)
            GlowImage.color = new Color(COL_WAIT.r, COL_WAIT.g, COL_WAIT.b,
                                        0.25f + Mathf.Sin(_pulseT) * 0.20f);
    }

    // ═════════════════════════════════════════════════════════════════════
    // Privado
    // ═════════════════════════════════════════════════════════════════════

    void Apply(State s, Color col, string txt, bool pulse)
    {
        _state   = s;
        _pulsing = pulse;
        _pulseT  = 0f;

        if (ButtonImage != null) ButtonImage.color = col;
        if (ButtonText  != null)
        {
            ButtonText.text  = txt;
            ButtonText.color = Color.white;
        }
        if (GlowImage != null)
        {
            GlowImage.color = new Color(col.r, col.g, col.b,
                                        s == State.Active ? 0.45f : 0.22f);
        }
    }
}
