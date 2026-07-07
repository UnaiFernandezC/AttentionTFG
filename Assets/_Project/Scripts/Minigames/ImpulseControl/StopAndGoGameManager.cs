// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// STOP & GO — paradigma GO/NO-GO clasico (nucleo del entrenamiento de
/// inhibicion en TDAH). Rafaga de estimulos breves:
///   VERDE  = GO     -> tocar lo mas rapido posible.
///   ROJO   = NO-GO  -> no tocar (frenarse).
///   NARANJA (Hard)  = NO-GO sorpresa (tambien hay que frenarse).
/// Mide comisiones (tocar rojo), omisiones (no tocar verde) y RT de los GO.
/// </summary>
public class StopAndGoGameManager : MinigameBase
{
    public enum StimType { Go, NoGo, Surprise }

    // ------------- parametros de dificultad (se fijan en ApplyDifficulty)
    int   _totalStimuli   = 15;
    float _goProportion   = 0.75f;
    float _stimulusWindow = 1.4f;
    int   _surpriseCount  = 0;
    float _interStimulus  = 0.55f;

    StopAndGoUIController _ui;
    StopAndGoInputHandler _input;

    StimType[] _plan;
    int   _index;
    int   _goHits, _goMisses, _inhibitions, _commissions;
    long  _rtSum;
    int   _rtCount;
    bool  _stimActive;
    bool  _resolved;
    float _stimShownAt;
    bool  _ended;

    static readonly Color TXT_GREEN  = new Color(0.25f, 0.90f, 0.52f);
    static readonly Color TXT_RED    = new Color(0.95f, 0.35f, 0.35f);
    static readonly Color TXT_ORANGE = new Color(0.96f, 0.62f, 0.18f);
    static readonly Color TXT_GRAY   = new Color(0.60f, 0.66f, 0.75f);

    protected override string GetIntroDescription()
    {
        var diff = GameManager.Instance != null
            ? GameManager.Instance.CurrentDifficulty
            : DifficultyLevel.Easy;

        if (diff == DifficultyLevel.Hard)
            return "VERDE = ¡toca rapido!   ROJO o NARANJA = ¡quieto!\n" +
                   "Van muy deprisa... ¡no te dejes llevar!";

        return "VERDE = ¡toca rapido!   ROJO = ¡quieto, no toques!\n" +
               "¡Atento, van uno detras de otro!";
    }

    void ApplyDifficulty()
    {
        var diff = GameManager.Instance != null
            ? GameManager.Instance.CurrentDifficulty
            : DifficultyLevel.Easy;

        switch (diff)
        {
            case DifficultyLevel.Medium:
                _totalStimuli   = 20;
                _goProportion   = 0.70f;
                _stimulusWindow = 1.1f;
                _surpriseCount  = 0;
                _interStimulus  = 0.50f;
                break;
            case DifficultyLevel.Hard:
                _totalStimuli   = 25;
                _goProportion   = 0.80f;   // mas tentacion de tocar
                _stimulusWindow = 0.8f;
                _surpriseCount  = 2;
                _interStimulus  = 0.45f;
                break;
            default:
                _totalStimuli   = 15;
                _goProportion   = 0.75f;
                _stimulusWindow = 1.4f;
                _surpriseCount  = 0;
                _interStimulus  = 0.55f;
                break;
        }
    }

    protected override void OnMinigameStart()
    {
        ApplyDifficulty();
        KidUI.EnsureEventSystem();

        _ui    = GetComponent<StopAndGoUIController>();
        _input = GetComponent<StopAndGoInputHandler>();

        BuildPlan();

        _ui.BuildUI(_surpriseCount > 0, _input);
        _input.OnPress    += HandlePress;
        _input.AcceptInput = false;

        _index = 0;
        _goHits = _goMisses = _inhibitions = _commissions = 0;
        _rtSum = 0; _rtCount = 0;
        _ended = false;

        StartCoroutine(RunSequence());
    }

    protected override void OnMinigameComplete() { }
    protected override void OnMinigameFailed()   { }

    void BuildPlan()
    {
        int goCount  = Mathf.RoundToInt(_totalStimuli * _goProportion);
        int noGo     = _totalStimuli - goCount;
        int surprise = Mathf.Min(_surpriseCount, noGo);

        var list = new List<StimType>(_totalStimuli);
        for (int i = 0; i < goCount; i++)          list.Add(StimType.Go);
        for (int i = 0; i < noGo - surprise; i++)  list.Add(StimType.NoGo);
        for (int i = 0; i < surprise; i++)         list.Add(StimType.Surprise);

        // Fisher-Yates
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            var tmp = list[i]; list[i] = list[j]; list[j] = tmp;
        }

        // El primero siempre GO (arranque amable y didactico)
        if (list[0] != StimType.Go)
        {
            int gi = list.IndexOf(StimType.Go);
            if (gi > 0) { var tmp = list[0]; list[0] = list[gi]; list[gi] = tmp; }
        }

        _plan = list.ToArray();
    }

    IEnumerator RunSequence()
    {
        _ui.SetStatus("¡Preparado!", TXT_GRAY);
        yield return new WaitForSeconds(0.8f);

        for (_index = 0; _index < _plan.Length; _index++)
        {
            if (!IsPlaying) yield break;

            var st = _plan[_index];
            _resolved   = false;
            _stimActive = true;
            _stimShownAt = Time.time;

            _ui.SetProgress(_index, _totalStimuli);
            _ui.ShowStimulus(st);
            _input.AcceptInput = true;

            float t = 0f;
            while (t < _stimulusWindow && !_resolved)
            {
                if (!IsPlaying) yield break;
                t += Time.deltaTime;
                _ui.UpdateTimerRing(1f - Mathf.Clamp01(t / _stimulusWindow));
                yield return null;
            }

            _input.AcceptInput = false;
            _stimActive = false;
            if (!_resolved) ResolveTimeout(st);

            yield return new WaitForSeconds(0.30f);   // que se vea el feedback
            _ui.HideStimulus();
            yield return new WaitForSeconds(_interStimulus);
        }

        _ui.SetProgress(_totalStimuli, _totalStimuli);
        if (IsPlaying) StartCoroutine(EndGame());
    }

    void HandlePress()
    {
        if (!IsPlaying || !_stimActive || _resolved) return;

        _resolved   = true;
        _stimActive = false;
        _input.AcceptInput = false;

        float rtMs = (Time.time - _stimShownAt) * 1000f;
        var   st   = _plan[_index];

        if (st == StimType.Go)
        {
            _goHits++;
            _rtSum += (long)rtMs;
            _rtCount++;
            ReportEvent(true, rtMs);

            GameFeel.PlaySuccess();
            GameFeel.FloatingText(Mathf.RoundToInt(rtMs) + " ms", TXT_GREEN,
                                  new Vector2(0f, 240f), 42f);
            _ui.ShowStimulusResult(true);
        }
        else
        {
            _commissions++;
            ReportEvent(false, rtMs);   // comision: respuesta impulsiva con RT real

            GameFeel.Error(_ui.StimulusRect);
            GameFeel.FloatingText(
                st == StimType.Surprise ? "¡Naranja tambien es STOP!" : "¡Rojo = quieto!",
                st == StimType.Surprise ? TXT_ORANGE : TXT_RED,
                new Vector2(0f, 240f), 38f);
            _ui.ShowStimulusResult(false);
        }
    }

    void ResolveTimeout(StimType st)
    {
        if (st == StimType.Go)
        {
            _goMisses++;
            ReportEvent(false, -1f);    // omision

            GameFeel.PlayError();
            GameFeel.FloatingText("¡Se escapo!", TXT_GRAY, new Vector2(0f, 240f), 36f);
            _ui.ShowStimulusResult(false);
        }
        else
        {
            _inhibitions++;
            ReportEvent(true, -1f);     // inhibicion correcta

            GameFeel.PlayPop();
            GameFeel.FloatingText("¡Frenaste a tiempo!", TXT_GREEN,
                                  new Vector2(0f, 240f), 36f);
            _ui.ShowStimulusResult(true);
        }
    }

    IEnumerator EndGame()
    {
        if (_ended) yield break;
        _ended = true;

        yield return new WaitForSeconds(0.7f);

        int   goTotal = _goHits + _goMisses;
        int   correct = _goHits + _inhibitions;
        float ratio   = _totalStimuli > 0 ? (float)correct / _totalStimuli : 0f;
        bool  won     = ratio >= 0.65f;
        long  avgMs   = _rtCount > 0 ? _rtSum / _rtCount : 0;

        int score = 0;
        if (won)
        {
            int speedBonus = _rtCount > 0
                ? Mathf.Max(0, Mathf.RoundToInt((600f - avgMs) * 0.5f))
                : 0;
            score = Mathf.Max(0, 200 + _goHits * 40 + _inhibitions * 70
                                 - _commissions * 25 + speedBonus);
        }

        if (won) CompleteMinigame(score);
        else     FailMinigame();

        int stars = GameFeel.StarsFromRatio(won, ratio);

        string rtStat = _rtCount > 0
            ? "Velocidad media: " + avgMs + " ms"
            : "Velocidad media: -";

        ShowResults(won, stars, score,
            new[]
            {
                "Te frenaste a tiempo " + _inhibitions + " de " + (_inhibitions + _commissions) + " veces",
                "Verdes atrapados: " + _goHits + "/" + goTotal,
                rtStat
            },
            null,
            won ? "¡Tienes buenos frenos!" : "Recuerda: rojo = quieto");
    }
}
