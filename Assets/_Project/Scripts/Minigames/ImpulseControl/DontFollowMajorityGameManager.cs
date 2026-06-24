using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class DontFollowMajorityGameManager : MinigameBase
{

    [Header("Rondas")]
    public int   totalRounds  = 10;
    public int   passCount    = 7;

    [Header("Tiempo de respuesta (segundos)")]
    public float responseTime         = 3.5f;
    public float pauseAfterResponse   = 0.70f;

    [Header("Estímulos")]
    public int totalArrows   = 10;
    public int minorityCount = 2;

    DontFollowMajorityRuleManager       _rule;
    DontFollowMajorityStimulusGenerator _gen;
    DontFollowMajorityInputHandler      _input;
    DontFollowMajorityUIController      _ui;

    int   _round;
    int   _correct;
    int   _errors;
    int   _score;
    float _elapsed;
    bool  _waitingForNext;

    protected override string GetIntroDescription()
    {
        return
            "Ves muchas flechas apuntando a distintos lados.\n" +
            "Tu tarea: elegir la direccion que tiene MENOS flechas.\n\n" +
            "No hagas lo que hace la mayoria!\n" +
            "Piensa antes de pulsar.";
    }

    void ApplyDifficulty()
    {
        var diff = GameManager.Instance != null
            ? GameManager.Instance.CurrentDifficulty
            : DifficultyLevel.Easy;
        switch (diff)
        {
            case DifficultyLevel.Medium:
                totalRounds   = 10;
                passCount     = 7;
                responseTime  = 3.0f;
                totalArrows   = 12;
                minorityCount = 2;
                break;
            case DifficultyLevel.Hard:
                totalRounds   = 12;
                passCount     = 9;
                responseTime  = 2.5f;
                totalArrows   = 15;
                minorityCount = 2;
                break;
        }
    }

    protected override void OnMinigameStart()
    {
        ApplyDifficulty();
        EnsureEventSystem();

        _rule  = GetComponent<DontFollowMajorityRuleManager>();
        _gen   = GetComponent<DontFollowMajorityStimulusGenerator>();
        _input = GetComponent<DontFollowMajorityInputHandler>();
        _ui    = GetComponent<DontFollowMajorityUIController>();

        _gen.totalArrows   = totalArrows;
        _gen.minorityCount = minorityCount;

        _ui.BuildUI(totalRounds,
                    d => _input.PressDirection(d),
                    () => RestartMinigame(),
                    () => ReturnToGameSelector());

        _input.OnDirectionInput += HandleResponse;

        _round          = 0;
        _correct        = 0;
        _errors         = 0;
        _score          = 0;
        _elapsed        = 0f;
        _waitingForNext = false;

        _ui.SetScore(0);
        StartCoroutine(DelayedStart());
    }

    protected override void OnMinigameComplete() { }
    protected override void OnMinigameFailed()   { }

    void Update()
    {
        if (!IsPlaying || _waitingForNext || !_input.AcceptInput) return;

        _elapsed += Time.deltaTime;
        _ui.SetTimerBar(1f - _elapsed / responseTime);

        if (_elapsed >= responseTime)
        {
            _input.AcceptInput = false;
            HandleTimeout();
        }
    }

    IEnumerator DelayedStart()
    {
        yield return new WaitForSeconds(0.35f);
        NextRound();
    }

    void NextRound()
    {
        if (!IsPlaying) return;

        _round++;
        _elapsed = 0f;
        _ui.HideFeedback();
        _ui.SetTimerBar(1f);

        _rule.GenerateRound();
        _gen.Generate(_ui.StimulusContainer,
                      _rule.MajorityDirection,
                      _rule.CorrectDirection);

        _input.AcceptInput = true;
    }

    void HandleResponse(DFMDirection dir)
    {
        if (!IsPlaying || _waitingForNext) return;

        bool correct = _rule.IsCorrect(dir);
        if (correct)
        {
            int pts = ComputePoints();
            _correct++;
            _score += pts;
        }
        else
        {
            _errors++;
        }

        _ui.SetRoundDot(_round - 1, correct);
        _ui.SetScore(_score);
        _ui.ShowFeedback(correct,
                         DontFollowMajorityRuleManager.DirectionName(_rule.CorrectDirection));

        StartCoroutine(AdvanceAfterDelay());
    }

    void HandleTimeout()
    {
        if (_waitingForNext) return;

        _ui.SetRoundDot(_round - 1, false);
        _ui.ShowFeedback(false,
                         DontFollowMajorityRuleManager.DirectionName(_rule.CorrectDirection));

        StartCoroutine(AdvanceAfterDelay());
    }

    IEnumerator AdvanceAfterDelay()
    {
        _waitingForNext = true;
        yield return new WaitForSeconds(pauseAfterResponse);
        _waitingForNext = false;

        _gen.Clear();

        if (_errors >= 3)
            EndGame(forceFail: true);
        else if (_round >= totalRounds)
            EndGame();
        else
            NextRound();
    }

    void EndGame(bool forceFail = false)
    {
        bool won = !forceFail && _correct >= passCount;
        if (won) CompleteMinigame(_score);
        else     FailMinigame();
        _ui.ShowFinalResult(won, _correct, totalRounds, _score);
    }

    int ComputePoints()
    {
        float ratio = Mathf.Clamp01(1f - _elapsed / responseTime);
        return Mathf.RoundToInt(Mathf.Lerp(60f, 100f, ratio));
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
