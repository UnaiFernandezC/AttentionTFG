// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using System.Collections;
using UnityEngine;

/// <summary>
/// NO SIGAS A LA MAYORIA — tarea de flancos (Flanker) infantil:
/// una fila de peces nada en pantalla; casi todos miran al mismo lado, pero el
/// pez NARANJA del centro puede mirar al contrario. El niño debe pulsar la
/// direccion del pez CENTRAL ignorando a la mayoria (◄ ► o flechas del teclado).
/// Mide RT y aciertos, con rondas incongruentes como medida de interferencia.
/// </summary>
public class DontFollowMajorityGameManager : MinigameBase
{
    // ------------- parametros de dificultad (se fijan en ApplyDifficulty)
    int   _rounds           = 12;
    float _incongruentRatio = 0.40f;
    float _timeLimit        = 0f;      // 0 = sin limite
    int   _fishCount        = 5;

    DontFollowMajorityUIController     _ui;
    DontFollowMajorityInputHandler     _input;
    DontFollowMajorityStimulusGenerator _gen;

    int   _round;
    int   _correct;
    int   _incongruentTotal;
    int   _incongruentCorrect;
    long  _rtSum;
    int   _rtCount;
    bool  _centerRight;
    bool  _isIncongruent;
    bool  _answered;
    float _shownAt;
    bool  _ended;
    Coroutine _timeoutCo;

    static readonly Color TXT_GREEN  = new Color(0.25f, 0.90f, 0.52f);
    static readonly Color TXT_RED    = new Color(0.95f, 0.35f, 0.35f);
    static readonly Color TXT_ORANGE = new Color(0.96f, 0.62f, 0.18f);

    protected override string GetIntroDescription()
    {
        return "Mira SOLO al pez NARANJA del centro.\n" +
               "Pulsa hacia donde mira EL... ¡aunque los demas miren al reves!";
    }

    void ApplyDifficulty()
    {
        var diff = GameManager.Instance != null
            ? GameManager.Instance.CurrentDifficulty
            : DifficultyLevel.Easy;

        switch (diff)
        {
            case DifficultyLevel.Medium:
                _rounds = 16; _incongruentRatio = 0.50f; _timeLimit = 2.0f; _fishCount = 5;
                break;
            case DifficultyLevel.Hard:
                _rounds = 20; _incongruentRatio = 0.60f; _timeLimit = 1.4f; _fishCount = 7;
                break;
            default:
                _rounds = 12; _incongruentRatio = 0.40f; _timeLimit = 0f;   _fishCount = 5;
                break;
        }
    }

    protected override void OnMinigameStart()
    {
        ApplyDifficulty();
        KidUI.EnsureEventSystem();

        _ui    = GetComponent<DontFollowMajorityUIController>();
        _input = GetComponent<DontFollowMajorityInputHandler>();
        _gen   = GetComponent<DontFollowMajorityStimulusGenerator>();

        _gen.BuildPlan(_rounds, _incongruentRatio);
        _ui.BuildUI(_fishCount, _timeLimit > 0f, _input);

        _input.OnAnswer   += HandleAnswer;
        _input.AcceptInput = false;

        _round = 0; _correct = 0;
        _incongruentTotal = 0; _incongruentCorrect = 0;
        _rtSum = 0; _rtCount = 0;
        _ended = false;

        StartCoroutine(RunRound(0.7f));
    }

    protected override void OnMinigameComplete() { }
    protected override void OnMinigameFailed()   { }

    IEnumerator RunRound(float delay)
    {
        _ui.HideFish();
        _ui.SetProgress(_correct, _round, _rounds);
        yield return new WaitForSeconds(delay);
        if (!IsPlaying) yield break;

        _centerRight   = _gen.RandomRight();
        _isIncongruent = _gen.IsIncongruent(_round);
        if (_isIncongruent) _incongruentTotal++;

        bool majorityRight = _isIncongruent ? !_centerRight : _centerRight;

        _answered = false;
        _ui.ShowFish(_centerRight, majorityRight, _fishCount);
        _shownAt = Time.time;
        _input.AcceptInput = true;

        if (_timeLimit > 0f)
            _timeoutCo = StartCoroutine(TimeoutRoutine());
    }

    IEnumerator TimeoutRoutine()
    {
        float t = 0f;
        while (t < _timeLimit)
        {
            if (_answered || !IsPlaying) yield break;
            t += Time.deltaTime;
            _ui.UpdateTimerBar(1f - t / _timeLimit);
            yield return null;
        }
        if (_answered || !IsPlaying) yield break;

        // Sin respuesta a tiempo
        _answered = true;
        _input.AcceptInput = false;
        ReportEvent(false, -1f);

        GameFeel.PlayError();
        GameFeel.FloatingText("¡Muy lento!", TXT_ORANGE, new Vector2(0f, 200f), 40f);
        _ui.ShowRoundFeedback(false, "Se acabo el tiempo", TXT_ORANGE);
        NextOrEnd();
    }

    void HandleAnswer(bool right)
    {
        if (!IsPlaying || _answered) return;
        _answered = true;
        _input.AcceptInput = false;
        if (_timeoutCo != null) { StopCoroutine(_timeoutCo); _timeoutCo = null; }

        float rtMs = (Time.time - _shownAt) * 1000f;
        bool correct = (right == _centerRight);
        ReportEvent(correct, rtMs);   // RT real tambien en errores (interferencia)

        if (correct)
        {
            _correct++;
            _rtSum += (long)rtMs;
            _rtCount++;
            if (_isIncongruent) _incongruentCorrect++;

            GameFeel.PlaySuccess();
            GameFeel.FloatingText(Mathf.RoundToInt(rtMs) + " ms", TXT_GREEN,
                                  new Vector2(0f, 200f), 40f);
            _ui.ShowRoundFeedback(true,
                _isIncongruent ? "¡No te engañaron!" : "¡Bien visto!", TXT_GREEN);
        }
        else
        {
            GameFeel.Error(_ui.CenterFishRect);
            GameFeel.FloatingText(
                _isIncongruent ? "¡Te llevaron los demas!" : "¡Mira al naranja!",
                TXT_RED, new Vector2(0f, 200f), 38f);
            _ui.ShowRoundFeedback(false, "El del centro miraba al otro lado", TXT_RED);
        }

        NextOrEnd();
    }

    void NextOrEnd()
    {
        _round++;
        _ui.SetProgress(_correct, _round, _rounds);

        if (_round >= _rounds) StartCoroutine(EndGame());
        else                   StartCoroutine(RunRound(0.85f));
    }

    IEnumerator EndGame()
    {
        if (_ended) yield break;
        _ended = true;

        yield return new WaitForSeconds(0.9f);

        float ratio = _rounds > 0 ? (float)_correct / _rounds : 0f;
        bool  won   = _correct >= Mathf.CeilToInt(_rounds * 0.6f);
        long  avgMs = _rtCount > 0 ? _rtSum / _rtCount : 0;

        int score = 0;
        if (won)
        {
            int speedBonus = _rtCount > 0
                ? Mathf.Max(0, Mathf.RoundToInt((900f - avgMs) * 0.4f))
                : 0;
            score = 250 + _correct * 40 + _incongruentCorrect * 40 + speedBonus;
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
                "Aciertos: " + _correct + "/" + _rounds,
                "Trampas superadas: " + _incongruentCorrect + "/" + _incongruentTotal,
                rtStat
            },
            null,
            won ? "¡No te dejaste llevar por la mayoria!"
                : "Fijate solo en el pez naranja del centro");
    }
}
