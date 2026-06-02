using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using static InverseResponseStimulusManager;

public class InverseResponseGameManager : MinigameBase
{

    [Header("Secuencia")]
    public int   totalStimuli = 10;
    public int   passCount    = 7;

    [Header("Velocidad")]
    public float responseTime        = 3.0f;
    public float pauseAfterResponse  = 0.55f;

    [Header("Cambio de regla")]
    [Tooltip("Cada cuantos estimulos cambia la regla. 999 = nunca (Easy).")]
    public int ruleChangeInterval = 999;

    InverseResponseStimulusManager _stimulus;
    InverseResponseInputHandler    _input;
    InverseResponseUIController    _ui;

    int  _stimulusDone;
    int  _correct;
    int  _errors;
    bool _waitingForNext;

    protected override string GetIntroDescription()
    {
        return "Aparece una figura a la IZQUIERDA o a la DERECHA.\n" +
               "Pero tienes que pulsar el lado CONTRARIO!\n\n" +
               "Figura a la izquierda -> pulsa derecha.\n" +
               "Figura a la derecha -> pulsa izquierda.\n" +
               "No te dejes enganar!";
    }

    void ApplyDifficulty()
    {
        var diff = GameManager.Instance != null
            ? GameManager.Instance.CurrentDifficulty
            : DifficultyLevel.Easy;
        switch (diff)
        {
            case DifficultyLevel.Medium:
                totalStimuli       = 14;
                passCount          = 10;
                responseTime       = 2.5f;
                ruleChangeInterval = 7;
                break;
            case DifficultyLevel.Hard:
                totalStimuli       = 18;
                passCount          = 14;
                responseTime       = 2.0f;
                ruleChangeInterval = 5;
                break;
        }
    }

    protected override void OnMinigameStart()
    {
        ApplyDifficulty();
        EnsureEventSystem();

        _stimulus = GetComponent<InverseResponseStimulusManager>();
        _input    = GetComponent<InverseResponseInputHandler>();
        _ui       = GetComponent<InverseResponseUIController>();

        _stimulus.responseTime       = responseTime;
        _stimulus.ruleChangeInterval = ruleChangeInterval;

        _ui.BuildUI(totalStimuli, () => RestartMinigame(), () => ReturnToGameSelector(), _input);

        _stimulus.OnStimulusShown += HandleStimulus;
        _stimulus.OnTimeout       += HandleTimeout;
        _input.OnDirectionInput   += HandleInput;

        _stimulusDone = 0;
        _correct      = 0;
        _errors       = 0;
        _waitingForNext = false;

        _ui.UpdateScore(0, 0, totalStimuli);

        StartCoroutine(LaunchFirstStimulus());
    }

    protected override void OnMinigameComplete() { }
    protected override void OnMinigameFailed()   { }

    void Update()
    {
        if (!IsPlaying) return;

        _stimulus.Tick();

        if (_stimulus.IsWaitingInput)
            _ui.UpdateTimerBar(_stimulus.StimulusElapsed, responseTime);
    }

    IEnumerator LaunchFirstStimulus()
    {
        yield return new WaitForSeconds(0.4f);
        NextStimulus();
    }

    void NextStimulus()
    {
        if (!IsPlaying) return;
        _input.AcceptInput = true;
        _ui.ShowArrowVisible();
        _stimulus.ShowNext();
    }

    void HandleStimulus(ArrowDirection dir, GameRule rule)
    {
        _ui.ShowArrow(dir, rule);
        _ui.UpdateTimerBar(0f, responseTime);
    }

    void HandleInput(ArrowDirection pressed)
    {
        if (!IsPlaying) return;

        _stimulus.RegisterResponse();
        _input.AcceptInput = false;

        bool correct = (pressed == _stimulus.RequiredResponse);

        if (correct)
        {
            _correct++;
            _ui.ShowFeedback(true, "¡Correcto!");
        }
        else
        {
            _errors++;
            string expected = InverseResponseStimulusManager.DirName(_stimulus.RequiredResponse);
            _ui.ShowFeedback(false, "Error — era " + expected);
        }

        _ui.UpdateScore(_correct, _errors, totalStimuli);
        AdvanceOrEnd();
    }

    void HandleTimeout()
    {
        if (!IsPlaying) return;

        _input.AcceptInput = false;
        _errors++;
        _ui.ShowFeedback(false, "Tiempo agotado");
        _ui.UpdateScore(_correct, _errors, totalStimuli);
        AdvanceOrEnd();
    }

    void AdvanceOrEnd()
    {
        _stimulusDone++;

        int remaining   = totalStimuli - _stimulusDone;
        bool alreadyWon = _correct >= passCount;
        bool canWin     = (_correct + remaining) >= passCount;

        if (alreadyWon || _stimulusDone >= totalStimuli || !canWin)
            StartCoroutine(EndGame(alreadyWon));
        else
            StartCoroutine(NextStimulusDelayed());
    }

    IEnumerator NextStimulusDelayed()
    {
        _ui.HideArrow();
        yield return new WaitForSeconds(pauseAfterResponse);
        NextStimulus();
    }

    IEnumerator EndGame(bool won)
    {
        yield return new WaitForSeconds(0.9f);
        int score = CalculateScore(won);
        CompleteMinigame(score);
        _ui.ShowFinalResult(won, _correct, _errors, totalStimuli, score);
    }

    int CalculateScore(bool won)
    {
        if (!won) return 0;
        int  baseS    = 300;
        int  accuracy = _correct * 60;
        int  penalty  = _errors  * 20;
        return Mathf.Max(0, baseS + accuracy - penalty);
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
