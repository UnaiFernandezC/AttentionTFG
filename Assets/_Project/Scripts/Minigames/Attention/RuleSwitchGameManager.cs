using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

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

    int  _stimIndex;
    int  _score;
    int  _correct;
    int  _wrong;
    int  _ruleChanges;
    bool _playerChose;

    protected override string GetIntroDescription() =>
        "Pulsa el objeto si su color coincide con la REGLA ACTIVA.\n" +
        "Ignóralo si no coincide (no hagas click).\n\n" +
        "ATENCION: la regla cambia SIN AVISO. Adáptate rápido.\n\n" +
        "Consigue el maximo de aciertos en " + totalStimuli + " estimulos.";

    void ApplyDifficulty()
    {
        var diff = GameManager.Instance != null
            ? GameManager.Instance.CurrentDifficulty
            : DifficultyLevel.Easy;
        switch (diff)
        {
            case DifficultyLevel.Medium:
                totalStimuli        = 20;
                stimuliPerRuleChange = 4;
                stimulusTime        = 1.5f;
                break;
            case DifficultyLevel.Hard:
                totalStimuli        = 25;
                stimuliPerRuleChange = 3;
                stimulusTime        = 1.0f;
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

        _stimIndex   = 0;
        _score       = 0;
        _correct     = 0;
        _wrong       = 0;
        _ruleChanges = 0;

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
        }
        else if (correct)
        {
            msg = "Bien ignorado";
            col = new Color(0.25f, 0.90f, 0.52f);
        }
        else if (!correct && clicked)
        {
            msg = "Error – no debías pulsarlo";
            col = new Color(0.90f, 0.28f, 0.30f);
        }
        else
        {
            msg = "¡Lo has perdido!";
            col = new Color(0.96f, 0.72f, 0.18f);
        }

        _ui.ShowStatus(msg, col);
    }

    void EndGame()
    {
        int maxScore = totalStimuli * 10;
        bool won = (float)_score / maxScore >= 0.60f;
        CompleteMinigame(_score);

        string sub =
            "Aciertos: " + _correct + "   Errores: " + _wrong + "\n" +
            "Cambios de regla superados: " + _ruleChanges + "\n" +
            "Puntuación: " + _score + " / " + maxScore;

        _ui.ShowFinalResult(won, sub);
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
