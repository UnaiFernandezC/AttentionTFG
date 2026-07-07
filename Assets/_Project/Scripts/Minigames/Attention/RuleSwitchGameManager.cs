// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Cambio de regla (Atencion / flexibilidad cognitiva).
/// Aparecen figuras de colores y hay que pulsar solo las que pide la regla,
/// que cambia cada pocos estimulos.
/// En dificil: 4o color (amarillo) y reglas INVERSAS ("Pulsa TODOS MENOS el rojo").
/// </summary>
public class RuleSwitchGameManager : MinigameBase
{

    [Header("Cantidad de estímulos totales")]
    public int totalStimuli = 15;

    [Header("Cambio de regla cada N estímulos (0 = nunca)")]
    public int stimuliPerRuleChange = 5;

    [Header("Tiempo visible por estímulo (s)")]
    public float stimulusTime = 2.0f;

    [Header("Tiempo de feedback tras respuesta (s)")]
    public float feedbackTime = 0.45f;

    [Header("¿Mostrar nueva regla en el label al cambiar? (false = solo dot)")]
    public bool showRuleOnChange = true;

    RuleSwitchRuleManager     _rule;
    RuleSwitchStimulusManager _stim;
    RuleSwitchInputHandler    _input;
    RuleSwitchUIController    _ui;

    int   _stimIndex;
    int   _score;
    int   _correct;
    int   _wrong;
    int   _ruleChanges;
    bool  _playerChose;
    float _stimShownAt;
    float _chooseMs;
    float _rtSumMs;
    int   _rtCount;

    RSRuleType[] _rulesForDiff;
    int          _colorCount = 3;

    protected override string GetIntroDescription()
    {
        var diff = GameManager.Instance != null
            ? GameManager.Instance.CurrentDifficulty
            : DifficultyLevel.Easy;

        if (diff == DifficultyLevel.Hard)
            return "Aparecen figuras de 4 colores.\n" +
                   "Pulsa las que te pida la regla del centro.\n\n" +
                   "¡Ojo! A veces la regla se da la vuelta:\n" +
                   "\"Pulsa TODOS MENOS el rojo\". ¡Lee bien!";

        return "Aparecen figuras de colores.\n" +
               "Pulsa las que te indique la regla!\n\n" +
               "Atencion: la regla puede cambiar de repente.\n" +
               "Mira siempre el aviso grande del centro.";
    }

    void ApplyDifficulty()
    {
        var diff = GameManager.Instance != null
            ? GameManager.Instance.CurrentDifficulty
            : DifficultyLevel.Easy;

        switch (diff)
        {
            case DifficultyLevel.Medium:
                totalStimuli         = 20;
                stimuliPerRuleChange = 4;
                stimulusTime         = 1.5f;
                _colorCount          = 3;
                _rulesForDiff        = new[]
                    { RSRuleType.ClickRed, RSRuleType.ClickBlue, RSRuleType.ClickGreen };
                break;
            case DifficultyLevel.Hard:
                totalStimuli         = 25;
                stimuliPerRuleChange = 3;
                stimulusTime         = 1.2f;
                _colorCount          = 4;
                // 4o color + reglas inversas: exige leer la regla, no memorizarla
                _rulesForDiff        = new[]
                {
                    RSRuleType.ClickRed,  RSRuleType.ClickBlue,
                    RSRuleType.ClickGreen, RSRuleType.ClickYellow,
                    RSRuleType.AvoidRed,  RSRuleType.AvoidBlue,
                    RSRuleType.AvoidGreen, RSRuleType.AvoidYellow
                };
                break;
            default:
                _colorCount   = 3;
                _rulesForDiff = new[]
                    { RSRuleType.ClickRed, RSRuleType.ClickBlue, RSRuleType.ClickGreen };
                break;
        }
    }

    protected override void OnMinigameStart()
    {
        ApplyDifficulty();
        EnsureEventSystem();

        _rule  = GetComponent<RuleSwitchRuleManager>();
        _stim  = GetComponent<RuleSwitchStimulusManager>();
        _input = GetComponent<RuleSwitchInputHandler>();
        _ui    = GetComponent<RuleSwitchUIController>();

        _rule.availableRules = _rulesForDiff;
        _stim.ColorCount     = _colorCount;

        _stimIndex   = 0;
        _score       = 0;
        _correct     = 0;
        _wrong       = 0;
        _ruleChanges = 0;
        _rtSumMs     = 0f;
        _rtCount     = 0;

        _stim.AreaRT = _ui.BuildUI(() => RestartMinigame(), () => ReturnToGameSelector());

        _rule.SetInitialRule();
        _ui.SetRuleLabel(
            _rule.GetCurrentRuleText(),
            RuleSwitchRuleManager.GetRuleColor(_rule.CurrentRule));

        _stim.OnStimulusClicked += OnPlayerChose;
        _input.OnPlayerChoose   += OnPlayerChose;

        _ui.UpdateScore(0);
        _ui.UpdateProgress(0, totalStimuli);
        _ui.SetTimerBar(1f);

        StartCoroutine(GameLoop());
    }

    protected override void OnMinigameComplete() { }
    protected override void OnMinigameFailed()   { }

    IEnumerator GameLoop()
    {
        yield return new WaitForSeconds(0.30f);

        for (_stimIndex = 0; _stimIndex < totalStimuli; _stimIndex++)
        {

            if (stimuliPerRuleChange > 0 &&
                _stimIndex > 0 &&
                _stimIndex % stimuliPerRuleChange == 0)
            {
                _rule.SwitchRule();
                _ruleChanges++;
                GameFeel.PlayPop();

                if (showRuleOnChange)
                    _ui.SetRuleLabel(
                        _rule.GetCurrentRuleText(),
                        RuleSwitchRuleManager.GetRuleColor(_rule.CurrentRule));
                else

                    _ui.SetRuleIndicatorOnly(
                        RuleSwitchRuleManager.GetRuleColor(_rule.CurrentRule));
            }

            var data = _stim.GenerateRandom();
            _stim.ShowStimulus(data);
            _playerChose       = false;
            _chooseMs          = -1f;
            _stimShownAt       = Time.time;
            _input.AcceptInput = true;

            _ui.ClearStatus();
            _ui.UpdateProgress(_stimIndex, totalStimuli);
            _ui.SetTimerBar(1f);

            float elapsed = 0f;
            while (elapsed < stimulusTime && !_playerChose)
            {
                elapsed += Time.deltaTime;
                _ui.SetTimerBar(1f - elapsed / stimulusTime);
                _stim.AnimateIn(elapsed);
                yield return null;
            }

            _input.AcceptInput = false;

            bool clicked = _playerChose;
            bool matches = _rule.Matches(data);
            bool correct = _rule.IsCorrect(data, clicked);

            if (correct) _correct++;
            else         _wrong++;

            // Telemetria por estimulo: RT real si hubo click
            ReportEvent(correct, clicked && _chooseMs >= 0f ? _chooseMs : -1f);
            if (correct && clicked && _chooseMs >= 0f)
            {
                _rtSumMs += _chooseMs;
                _rtCount++;
            }

            int delta = correct ? 10 : -5;
            _score = Mathf.Max(0, _score + delta);
            _ui.UpdateScore(_score);

            _stim.ApplyFeedbackTint(correct);
            _ui.SetTimerBar(correct ? 1f : 0f);
            ShowFeedbackMsg(correct, matches, clicked);

            yield return new WaitForSeconds(feedbackTime);

            _stim.HideStimulus();
            _ui.ClearStatus();
            _ui.SetTimerBar(1f);
            yield return new WaitForSeconds(0.18f);
        }

        _ui.UpdateProgress(totalStimuli, totalStimuli);
        yield return new WaitForSeconds(0.50f);
        EndGame();
    }

    void OnPlayerChose()
    {
        if (!IsPlaying || _playerChose) return;
        _playerChose       = true;
        _chooseMs          = (Time.time - _stimShownAt) * 1000f;
        _input.AcceptInput = false;
    }

    void ShowFeedbackMsg(bool correct, bool matches, bool clicked)
    {
        string msg;
        Color  col;

        if (correct && clicked)
        {
            msg = "¡Correcto!";
            col = new Color(0.25f, 0.90f, 0.52f);
            GameFeel.PlaySuccess();
            GameFeel.FloatingText("+10", col, new Vector2(0f, 180f), 44f);
        }
        else if (correct)
        {
            msg = "¡Bien! No habia que pulsar";
            col = new Color(0.25f, 0.90f, 0.52f);
            GameFeel.PlayPop();
        }
        else if (!correct && clicked)
        {
            msg = "Error – no debias pulsarlo";
            col = new Color(0.90f, 0.28f, 0.30f);
            GameFeel.Error(null);
        }
        else
        {
            msg = "¡Se te ha escapado!";
            col = new Color(0.96f, 0.72f, 0.18f);
            GameFeel.PlayError();
        }

        _ui.ShowStatus(msg, col);
    }

    void EndGame()
    {
        int   maxScore = totalStimuli * 10;
        float ratio    = (float)_correct / totalStimuli;
        bool  won      = (float)_score / maxScore >= 0.60f;
        int   stars    = GameFeel.StarsFromRatio(won, ratio);

        if (won) CompleteMinigame(_score);
        else     FailMinigame();

        string rtStat = _rtCount > 0
            ? "Reaccion media: " + Mathf.RoundToInt(_rtSumMs / _rtCount) + " ms"
            : "Reaccion media: -";

        ShowResults(won, stars, _score,
            new[]
            {
                "Aciertos: " + _correct + "/" + totalStimuli,
                "Cambios de regla: " + _ruleChanges,
                rtStat
            },
            null,
            won ? "¡Cambias de regla como un campeon!"
                : "Lee la regla del centro antes de pulsar");
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
