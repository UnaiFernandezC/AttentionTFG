using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StopAndGoGameManager : MinigameBase
{

    [Header("Config")]
    public int   totalRounds       = 3;
    public int   stopsPerRound     = 3;
    public int   stopsToWinRound   = 2;
    public int   roundsToWin       = 2;
    public float pauseAfterStop    = 0.9f;

    static readonly float[] RoundZoneSpan = { 64f, 44f, 26f };
    static readonly float[] RoundSpeed    = { 75f, 95f, 118f };

    StopAndGoObjectMover  _mover;
    StopAndGoZoneManager  _zone;
    StopAndGoInputHandler _input;
    StopAndGoUIController _ui;

    int _currentRound   = 0;
    int _currentStop    = 0;
    int _correctInRound = 0;
    int _roundsWon      = 0;
    int _score          = 0;

    float _currentZoneSpan;

    protected override string GetIntroDescription() =>
        "Un punto da vueltas en un circulo.\n" +
        "Tienes que pararlo cuando este en la zona VERDE.\n\n" +
        "Pulsa ESPACIO o el boton PARA cuando el punto llegue al verde.\n" +
        "Si te pasas o te quedas corto, la zona se hace mas pequena!";

    void ApplyDifficulty()
    {
        var diff = GameManager.Instance != null
            ? GameManager.Instance.CurrentDifficulty
            : DifficultyLevel.Easy;
        switch (diff)
        {
            case DifficultyLevel.Medium:
                totalRounds = 4;
                roundsToWin = 3;
                break;
            case DifficultyLevel.Hard:
                totalRounds = 5;
                roundsToWin = 4;
                break;
        }
    }

    protected override void OnMinigameStart()
    {
        ApplyDifficulty();
        _mover = GetComponent<StopAndGoObjectMover>() ?? gameObject.AddComponent<StopAndGoObjectMover>();
        _zone  = GetComponent<StopAndGoZoneManager>()  ?? gameObject.AddComponent<StopAndGoZoneManager>();
        _input = GetComponent<StopAndGoInputHandler>() ?? gameObject.AddComponent<StopAndGoInputHandler>();
        _ui    = GetComponent<StopAndGoUIController>() ?? gameObject.AddComponent<StopAndGoUIController>();

        _ui.BuildUI(totalRounds, stopsPerRound,
                    OnStopPressed, RestartMinigame, ReturnToGameSelector);

        _mover.trackRadius = 185f;
        _mover.Init(_ui.GetMarkerRT());
        _input.OnStopPressed += OnStopPressed;

        StartCoroutine(BeginRound());
    }

    protected override void OnMinigameComplete() { }
    protected override void OnMinigameFailed()   { }

    IEnumerator BeginRound()
    {
        _currentRound++;
        _currentStop    = 0;
        _correctInRound = 0;

        int ri = Mathf.Clamp(_currentRound - 1, 0, RoundZoneSpan.Length - 1);
        _currentZoneSpan        = RoundZoneSpan[ri];
        _mover.degreesPerSecond = RoundSpeed[ri];

        _ui.SetRoundLabel(_currentRound, totalRounds);
        _ui.ResetStopDots();
        _ui.SetScore(_score);
        _input.AcceptInput = false;

        PlaceZoneRandomly();

        yield return new WaitForSeconds(0.55f);
        _mover.StopMoving();
        _mover.StartMoving();
        _input.AcceptInput = true;
    }

    void PlaceZoneRandomly()
    {
        float start = Random.Range(0f, 360f);
        _zone.zones = new List<StopAndGoZoneManager.SafeZone>
        {
            new StopAndGoZoneManager.SafeZone { startAngle = start, spanAngle = _currentZoneSpan }
        };
        _ui.UpdateZoneArc(start, _currentZoneSpan);
    }

    void OnStopPressed()
    {
        if (!IsPlaying) return;

        _mover.StopMoving();
        _input.AcceptInput = false;

        bool inZone = _zone.IsInZone(_mover.CurrentAngle);

        if (inZone)
        {
            _correctInRound++;
            _score += 50;
            _ui.SetStopDot(_currentStop, true);
            _ui.Flash(new Color(0.22f, 0.86f, 0.54f, 0.38f));
        }
        else
        {
            _score = Mathf.Max(0, _score - 20);
            _ui.SetStopDot(_currentStop, false);
            _ui.Flash(new Color(0.90f, 0.22f, 0.28f, 0.35f));
        }

        _currentStop++;
        _ui.SetScore(_score);

        if (_currentStop >= stopsPerRound)
            StartCoroutine(EndRound(pauseAfterStop));
        else
            StartCoroutine(NextStopDelay(pauseAfterStop));
    }

    IEnumerator NextStopDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        PlaceZoneRandomly();

        yield return new WaitForSeconds(0.30f);
        _mover.StartMoving();
        _input.AcceptInput = true;
    }

    IEnumerator EndRound(float delay)
    {
        yield return new WaitForSeconds(delay);

        bool roundWon = _correctInRound >= stopsToWinRound;
        if (roundWon) _roundsWon++;

        _ui.SetRoundDot(_currentRound - 1, roundWon);

        if (_currentRound >= totalRounds)
        {

            bool gameWon = _roundsWon >= roundsToWin;
            int finalScore = 300 + _score;
            if (!gameWon) finalScore = Mathf.Max(0, finalScore - 80);
            _ui.ShowFinalResult(gameWon, _roundsWon, totalRounds, _score, finalScore);
            CompleteMinigame(finalScore);
        }
        else
        {
            yield return new WaitForSeconds(0.4f);
            StartCoroutine(BeginRound());
        }
    }

    void Update()
    {
        if (!IsPlaying || _mover == null || _zone == null || _ui == null) return;
        _ui.SetMarkerAngle(_mover.CurrentAngle, _zone.IsInZone(_mover.CurrentAngle));
    }
}
