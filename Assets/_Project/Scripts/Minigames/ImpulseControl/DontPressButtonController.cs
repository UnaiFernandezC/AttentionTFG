// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DontPressButtonController : MonoBehaviour
{

    static readonly Color COL_IDLE    = new Color(0.14f, 0.19f, 0.30f, 1f);
    static readonly Color COL_WAIT    = new Color(0.80f, 0.18f, 0.22f, 1f);
    static readonly Color COL_FAKE    = new Color(0.92f, 0.75f, 0.10f, 1f);
    static readonly Color COL_ORANGE  = new Color(0.96f, 0.50f, 0.10f, 1f);
    static readonly Color COL_ACTIVE  = new Color(0.18f, 0.80f, 0.45f, 1f);
    static readonly Color COL_CORRECT = new Color(0.20f, 0.60f, 0.90f, 1f);
    static readonly Color COL_EARLY   = new Color(0.55f, 0.10f, 0.15f, 1f);
    static readonly Color COL_MISSED  = new Color(0.35f, 0.38f, 0.45f, 1f);

    const string TXT_IDLE    = "Preparado";
    const string TXT_WAIT    = "NO\nPULSES";
    const string TXT_FAKE    = "¡NO!";
    const string TXT_ORANGE  = "¡NO!";
    const string TXT_ACTIVE  = "¡PULSA\nAHORA!";
    const string TXT_FAKEGRN = "¿YA?\n¡NO!";
    const string TXT_CORRECT = "¡BIEN\nHECHO!";
    const string TXT_EARLY   = "¡DEMASIADO\nPRONTO!";
    const string TXT_MISSED  = "TIEMPO\nAGOTADO";

    [HideInInspector] public Image           ButtonImage;
    [HideInInspector] public Image           GlowImage;
    /// <summary>Aro de estado alrededor del boton (rojo = espera, verde = ¡ahora!).</summary>
    [HideInInspector] public Image           RingImage;
    [HideInInspector] public TextMeshProUGUI ButtonText;

    public enum State { Idle, Waiting, FakeOut, Orange, Active, FakeGreen, Correct, Early, Missed }
    State   _state   = State.Idle;
    float   _pulseT  = 0f;
    bool    _pulsing = false;

    public void SetIdle()      => Apply(State.Idle,      COL_IDLE,    TXT_IDLE,    pulse: false);
    public void SetWaiting()   => Apply(State.Waiting,   COL_WAIT,    TXT_WAIT,    pulse: true);
    public void SetFakeOut()   => Apply(State.FakeOut,   COL_FAKE,    TXT_FAKE,    pulse: false);
    public void SetOrange()    => Apply(State.Orange,    COL_ORANGE,  TXT_ORANGE,  pulse: false);
    public void SetActive()    => Apply(State.Active,    COL_ACTIVE,  TXT_ACTIVE,  pulse: false);
    /// <summary>Falsa alarma (Hard): mismo verde pero PARPADEANDO. Verde fijo = real.</summary>
    public void SetFakeGreen() => Apply(State.FakeGreen, COL_ACTIVE,  TXT_FAKEGRN, pulse: false);
    public void SetCorrect()   => Apply(State.Correct,   COL_CORRECT, TXT_CORRECT, pulse: false);
    public void SetEarly()     => Apply(State.Early,     COL_EARLY,   TXT_EARLY,   pulse: false);
    public void SetMissed()    => Apply(State.Missed,    COL_MISSED,  TXT_MISSED,  pulse: false);

    // Tiempo total en el estado actual (para la tension creciente de la espera)
    float _stateT = 0f;

    public void Tick()
    {
        if (ButtonImage == null) return;

        if (_pulsing)   // rojo "respirando" durante la espera
        {
            _stateT += Time.deltaTime;
            // Tension visual sutil creciente: la respiracion se acelera un poco
            // cuanto mas dura la espera (solo estetica, no cambia tiempos).
            float speed = 2.8f + Mathf.Min(1.4f, _stateT * 0.22f);
            _pulseT += Time.deltaTime * speed;
            float pulse = 0.80f + Mathf.Sin(_pulseT) * 0.20f;

            ButtonImage.color = new Color(
                COL_WAIT.r * pulse,
                COL_WAIT.g * pulse,
                COL_WAIT.b * pulse, 1f);

            if (GlowImage != null)
                GlowImage.color = new Color(COL_WAIT.r, COL_WAIT.g, COL_WAIT.b,
                                            0.25f + Mathf.Sin(_pulseT) * 0.20f);

            // El aro rojo late en contrafase (marca inequivoca de "espera")
            if (RingImage != null)
                RingImage.color = new Color(COL_WAIT.r, COL_WAIT.g, COL_WAIT.b,
                                            0.65f + Mathf.Sin(_pulseT + Mathf.PI) * 0.25f);
        }
        else if (_state == State.FakeGreen)   // verde PARPADEANTE = trampa
        {
            _pulseT += Time.deltaTime;
            bool on = Mathf.FloorToInt(_pulseT / 0.13f) % 2 == 0;
            ButtonImage.color = on ? COL_ACTIVE : COL_IDLE;
            if (GlowImage != null)
                GlowImage.color = new Color(COL_ACTIVE.r, COL_ACTIVE.g, COL_ACTIVE.b,
                                            on ? 0.35f : 0.05f);
            if (RingImage != null)
                RingImage.color = new Color(COL_ACTIVE.r, COL_ACTIVE.g, COL_ACTIVE.b,
                                            on ? 0.85f : 0.15f);
        }
    }

    void Apply(State s, Color col, string txt, bool pulse)
    {
        _state   = s;
        _pulsing = pulse;
        _pulseT  = 0f;
        _stateT  = 0f;

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
        // Aro de estado: verde solido brillante en "¡ahora!", tenue en reposo,
        // y del color del estado en el resto de feedbacks.
        if (RingImage != null)
        {
            float ringA = s == State.Active  ? 0.95f
                        : s == State.Idle    ? 0.10f
                        : s == State.Correct ? 0.85f
                                             : 0.55f;
            Color ringC = s == State.Idle ? Color.white : col;
            RingImage.color = new Color(ringC.r, ringC.g, ringC.b, ringA);
        }
    }
}
