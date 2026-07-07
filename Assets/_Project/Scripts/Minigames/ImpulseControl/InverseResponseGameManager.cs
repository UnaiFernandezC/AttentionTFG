// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using System.Collections;
using UnityEngine;
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
    long _rtSum;
    int  _rtCount;

    static readonly Color TXT_GREEN  = new Color(0.25f, 0.90f, 0.52f);
    static readonly Color TXT_RED    = new Color(0.95f, 0.35f, 0.35f);
    static readonly Color TXT_ORANGE = new Color(0.96f, 0.62f, 0.18f);

    protected override string GetIntroDescription()
    {
        var diff = GameManager.Instance != null
            ? GameManager.Instance.CurrentDifficulty
            : DifficultyLevel.Easy;

        if (diff == DifficultyLevel.Easy)
            return "La flecha te quiere engañar: pulsa el lado CONTRARIO.\n" +
                   "¡Piensa un momento antes de pulsar!";

        return "Mira la regla: INVERSA = lado contrario, IGUAL = mismo lado.\n" +
               "¡La regla cambia de repente, piensa antes de pulsar!";
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
            default:                       // Easy: mas tiempo para pensar
                totalStimuli       = 10;
                passCount          = 7;
                responseTime       = 4.0f;
                ruleChangeInterval = 999;  // la regla nunca cambia
                break;
        }
    }

    protected override void OnMinigameStart()
    {
        ApplyDifficulty();
        KidUI.EnsureEventSystem();

        _stimulus = GetComponent<InverseResponseStimulusManager>();
        _input    = GetComponent<InverseResponseInputHandler>();
        _ui       = GetComponent<InverseResponseUIController>();

        _stimulus.responseTime       = responseTime;
        _stimulus.ruleChangeInterval = ruleChangeInterval;

        _ui.BuildUI(totalStimuli, _input);

        _stimulus.OnStimulusShown += HandleStimulus;
        _stimulus.OnTimeout       += HandleTimeout;
        _input.OnDirectionInput   += HandleInput;

        _stimulusDone = 0;
        _correct      = 0;
        _errors       = 0;
        _rtSum        = 0;
        _rtCount      = 0;
        _waitingForNext = false;

        _ui.UpdateScore(0, 0, totalStimuli);

        StartCoroutine(LaunchFirstStimulus());
    }

    protected override void OnMinigameComplete() { }
    protected override void OnMinigameFailed()   { }

    void Update()
    {
        if (!IsPlaying) return;

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

        float rtMs = _stimulus.StimulusElapsed * 1000f;
        _stimulus.RegisterResponse();
        _input.AcceptInput = false;

        bool correct = (pressed == _stimulus.RequiredResponse);
        ReportEvent(correct, rtMs);   // RT real tambien en errores (impulsividad)

        if (correct)
        {
            _correct++;
            _rtSum += (long)rtMs;
            _rtCount++;

            GameFeel.PlaySuccess();
            GameFeel.FloatingText(Mathf.RoundToInt(rtMs) + " ms", TXT_GREEN,
                                  new Vector2(0f, 230f), 42f);
            _ui.ShowFeedback(true, "¡Correcto!");
        }
        else
        {
            _errors++;
            string expected = InverseResponseStimulusManager.DirName(_stimulus.RequiredResponse);

            GameFeel.Error(_ui.ArrowRect);
            GameFeel.FloatingText("¡Era " + expected + "!", TXT_RED,
                                  new Vector2(0f, 230f), 38f);
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
        ReportEvent(false, -1f);   // omision: no respondio a tiempo

        GameFeel.PlayError();
        GameFeel.FloatingText("¡Se acabo el tiempo!", TXT_ORANGE,
                              new Vector2(0f, 230f), 38f);
        _ui.ShowFeedback(false, "Tiempo agotado");
        _ui.UpdateScore(_correct, _errors, totalStimuli);
        AdvanceOrEnd();
    }

    void AdvanceOrEnd()
    {
        _stimulusDone++;

        int  remaining = totalStimuli - _stimulusDone;
        bool canWin    = (_correct + remaining) >= passCount;

        // Se juegan TODOS los estimulos (asi un juego perfecto llega a 3 estrellas);
        // solo se corta antes si ganar ya es imposible.
        if (_stimulusDone >= totalStimuli || !canWin)
            StartCoroutine(EndGame(_correct >= passCount));
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
        _stimulus.StopAll();
        _ui.HideArrow();
        yield return new WaitForSeconds(0.9f);

        int score = CalculateScore(won);
        if (won) CompleteMinigame(score);
        else     FailMinigame();

        float ratio = totalStimuli > 0 ? (float)_correct / totalStimuli : 0f;
        int   stars = GameFeel.StarsFromRatio(won, ratio);
        long  avgMs = _rtCount > 0 ? _rtSum / _rtCount : 0;

        string rtStat = _rtCount > 0
            ? "Velocidad media: " + avgMs + " ms"
            : "Velocidad media: -";

        ShowResults(won, stars, score,
            new[]
            {
                "Aciertos: " + _correct + "/" + totalStimuli,
                "Errores: " + _errors,
                rtStat
            },
            null,
            won ? "¡Frenaste el impulso y pensaste primero!"
                : "Respira y piensa: ¿que lado toca de verdad?");
    }

    int CalculateScore(bool won)
    {
        if (!won) return 0;
        int  baseS    = 300;
        int  accuracy = _correct * 60;
        int  penalty  = _errors  * 20;
        return Mathf.Max(0, baseS + accuracy - penalty);
    }
}
