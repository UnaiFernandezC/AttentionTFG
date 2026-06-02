using UnityEngine;
using UnityEngine.EventSystems;

public class TrackingGameManager : MinigameBase
{
    [Header("Segundos de seguimiento para ganar")]
    public float winTime   = 8f;
    [Header("Velocidad de pérdida de progreso (s/s)")]
    public float lossRate  = 0.25f;

    ObjectMover          _mover;
    TrackingDetector     _detector;
    TrackingUIController _ui;

    float _trackProgress;
    bool  _over;
    float _pulseT;

    protected override string GetIntroDescription() =>
        "Un punto se mueve por la pantalla.\n" +
        "Mueve el raton encima del punto y no lo pierdas!\n\n" +
        "Cuanto mas tiempo lo sigas, mas puntos consigues.\n" +
        "Concentracion al maximo!";

    void ApplyDifficulty()
    {
        var diff = GameManager.Instance != null
            ? GameManager.Instance.CurrentDifficulty
            : DifficultyLevel.Easy;
        switch (diff)
        {
            case DifficultyLevel.Medium:
                winTime  = 12f;
                lossRate = 0.4f;
                break;
            case DifficultyLevel.Hard:
                winTime  = 16f;
                lossRate = 0.6f;
                break;
        }
    }

    protected override void OnMinigameStart()
    {
        ApplyDifficulty();
        EnsureEventSystem();

        _mover    = GetComponent<ObjectMover>();
        _detector = GetComponent<TrackingDetector>();
        _ui       = GetComponent<TrackingUIController>();

        _trackProgress = 0f;
        _over          = false;

        _ui.BuildUI(() => RestartMinigame(), () => ReturnToGameSelector());

        _detector.CanvasRT    = _ui.CanvasRT;
        _detector.ObjectRT    = _ui.ObjectRT;
        _detector.TrackRadius = 55f;
        _detector.Active      = true;

        _mover.ObjectRT = _ui.ObjectRT;
        _mover.StartMoving();

        _ui.SetProgress(0f);
        _ui.SetStatus("¡Sigue el objeto!", new Color(0.40f, 0.72f, 1.00f));
    }

    protected override void OnMinigameComplete() { }
    protected override void OnMinigameFailed()   { }

    void Update()
    {
        if (!IsPlaying || _over) return;

        _pulseT += Time.deltaTime;
        bool tracking = _detector.IsTracking;

        if (tracking)
            _trackProgress += Time.deltaTime;
        else
            _trackProgress -= lossRate * Time.deltaTime;

        _trackProgress = Mathf.Clamp(_trackProgress, 0f, winTime);

        _ui.SetProgress(_trackProgress / winTime);
        _ui.UpdateObjectVisuals(tracking, _pulseT);

        if (tracking)
            _ui.SetStatus("Perfecto! Sigue así...", new Color(0.25f, 0.90f, 0.52f));
        else
            _ui.SetStatus("¡No lo pierdas!", new Color(0.96f, 0.72f, 0.18f));

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
