using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// GameManager del minijuego "Seguimiento de objeto".
/// Hereda MinigameBase (intro panel automático).
///
/// Mecánica:
///   - El objeto se mueve por la pantalla.
///   - Mantener cursor sobre él acumula trackProgress.
///   - Si el cursor se pierde, el progreso decrece lentamente.
///   - Ganar al alcanzar winTime segundos de seguimiento acumulado.
///
/// Ajuste de dificultad en Inspector:
///   Fácil  → speed=160, dirChangeRate=2.5, winTime=8,  lossRate=0.25
///   Medio  → speed=230, dirChangeRate=1.8, winTime=12, lossRate=0.40
///   Difícil→ speed=310, dirChangeRate=0.9, winTime=15, lossRate=0.60
/// </summary>
public class TrackingGameManager : MinigameBase
{
    [Header("Segundos de seguimiento para ganar")]
    public float winTime   = 8f;
    [Header("Velocidad de pérdida de progreso (s/s)")]
    public float lossRate  = 0.25f;

    ObjectMover          _mover;
    TrackingDetector     _detector;
    TrackingUIController _ui;

    float _trackProgress;   // 0 → winTime
    bool  _over;
    float _pulseT;

    // ═════════════════════════════════════════════════════════════════════

    protected override string GetIntroDescription() =>
        "Un objeto se moverá por la pantalla.\n" +
        "Mantén el cursor sobre él el mayor tiempo posible.\n" +
        "Consigue " + (int)winTime + " segundos de seguimiento para ganar.\n" +
        "Si lo pierdes, el progreso retrocede lentamente.";

    protected override void OnMinigameStart()
    {
        EnsureEventSystem();

        _mover    = GetComponent<ObjectMover>();
        _detector = GetComponent<TrackingDetector>();
        _ui       = GetComponent<TrackingUIController>();

        _trackProgress = 0f;
        _over          = false;

        // Construir UI
        _ui.BuildUI(() => RestartMinigame(), () => ReturnToGameSelector());

        // Conectar detector
        _detector.CanvasRT    = _ui.CanvasRT;
        _detector.ObjectRT    = _ui.ObjectRT;
        _detector.TrackRadius = 55f;
        _detector.Active      = true;

        // Conectar mover
        _mover.ObjectRT = _ui.ObjectRT;
        _mover.StartMoving();

        _ui.SetProgress(0f);
        _ui.SetStatus("¡Sigue el objeto!", new Color(0.40f, 0.72f, 1.00f));
    }

    protected override void OnMinigameComplete() { }
    protected override void OnMinigameFailed()   { }

    // ═════════════════════════════════════════════════════════════════════

    void Update()
    {
        if (!IsPlaying || _over) return;

        _pulseT += Time.deltaTime;
        bool tracking = _detector.IsTracking;

        // Acumular / perder progreso
        if (tracking)
            _trackProgress += Time.deltaTime;
        else
            _trackProgress -= lossRate * Time.deltaTime;

        _trackProgress = Mathf.Clamp(_trackProgress, 0f, winTime);

        // Actualizar UI
        _ui.SetProgress(_trackProgress / winTime);
        _ui.UpdateObjectVisuals(tracking, _pulseT);

        if (tracking)
            _ui.SetStatus("Perfecto! Sigue así...", new Color(0.25f, 0.90f, 0.52f));
        else
            _ui.SetStatus("¡No lo pierdas!", new Color(0.96f, 0.72f, 0.18f));

        // Comprobar victoria
        if (_trackProgress >= winTime)
        {
            _over = true;
            _detector.Active = false;
            _mover.StopMoving();
            _ui.UpdateObjectVisuals(true, _pulseT);
            _ui.SetStatus("¡Victoria!", new Color(0.25f, 0.90f, 0.52f));
            CompleteMinigame(CalculateScore());
            _ui.ShowResult(true, "Seguimiento completado en " + Mathf.RoundToInt(_pulseT) + " segundos.\n+" + CalculateScore() + " puntos");
        }
    }

    int CalculateScore()
    {
        // Puntuación base + bonus por rapidez
        int base_ = 700;
        int speedBonus = Mathf.Max(0, Mathf.RoundToInt((40f - _pulseT) * 10f));
        return base_ + speedBonus;
    }

    static void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() == null)
        {
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }
    }
}
