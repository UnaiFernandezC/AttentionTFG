using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// GameManager de "Regulacion Progresiva".
///
/// MECANICA CLAVE — REGENERACION AUTOMATICA:
///   Antes de cada accion, el nivel sube +regenerationPerTurn.
///   Esto hace que las acciones debiles sean contraproducentes:
///
///     Respirar (-22)   neto -14  ✓  muy efectiva
///     Hablar   (-18)   neto -10  ✓  muy efectiva
///     Caminar  (-16)   neto  -8  ✓  efectiva
///     Pensar   (-12)   neto  -4  △  apenas ayuda
///     Ignorar   (-2)   neto  +6  ✗  empeora la situacion
///     Ira      (+15)   neto +23  ✗  desastre garantizado
///
/// El jugador DEBE rotar las tres primeras acciones de forma eficiente.
/// Cualquier accion inutil puede costar la partida.
/// </summary>
public class RegulationGameManager : MinigameBase
{
    [Header("Nivel emocional inicial")]
    public float startLevel = 100f;

    [Header("Tension automatica por turno (antes de actuar)")]
    public float regenerationPerTurn = 8f;

    [Header("Maximo de acciones antes de perder")]
    public int maxSteps = 10;

    RegulationEmotionManager _emotionMgr;
    RegulationUIController   _ui;

    // ════════════════════════════════════════════════════════════════════

    protected override string GetIntroDescription() =>
        "Tu nivel emocional esta muy alto. Debes regularlo.\n\n" +
        "ATENCION: cada turno el nivel sube +8 solo, antes de que actues.\n" +
        "Solo las acciones mas efectivas consiguen reducirlo de verdad.\n" +
        "Las acciones tienen recarga de 2 turnos: no podras repetir la misma.\n\n" +
        "Objetivo: bajar a 10 o menos en " + maxSteps + " acciones.";

    protected override void OnMinigameStart()
    {
        EnsureEventSystem();

        _emotionMgr = new RegulationEmotionManager(startLevel, regenerationPerTurn);
        _ui         = GetComponent<RegulationUIController>();

        _ui.BuildUI(
            idx => HandleAction(idx),
            ()  => RestartMinigame(),
            ()  => ReturnToGameSelector());

        RefreshUI();
    }

    protected override void OnMinigameComplete() { }
    protected override void OnMinigameFailed()   { }

    // ════════════════════════════════════════════════════════════════════

    void HandleAction(int actionIndex)
    {
        if (!IsPlaying) return;
        if (!_emotionMgr.CanUseAction(actionIndex)) return;

        var action = _emotionMgr.ApplyAction(actionIndex);
        if (action == null) return;

        RefreshUI();
        _ui.ShowFeedback(action, _emotionMgr.CurrentLevel, regenerationPerTurn);

        if (_emotionMgr.IsWon)
        {
            int score = _emotionMgr.CalculateScore();
            CompleteMinigame(score);
            _ui.ShowResult(won: true,
                           steps: _emotionMgr.StepsTaken,
                           score: score,
                           finalLevel: Mathf.RoundToInt(_emotionMgr.CurrentLevel));
        }
        else if (_emotionMgr.StepsTaken >= maxSteps)
        {
            CompleteMinigame(0);
            _ui.ShowResult(won: false,
                           steps: _emotionMgr.StepsTaken,
                           score: 0,
                           finalLevel: Mathf.RoundToInt(_emotionMgr.CurrentLevel));
        }
    }

    void RefreshUI()
    {
        _ui.UpdateBar(_emotionMgr.CurrentLevel, _emotionMgr.StepsTaken,
                      maxSteps, regenerationPerTurn);
        _ui.UpdateScore(_emotionMgr.CalculateScore());

        int n   = RegulationEmotionManager.ACTIONS.Length;
        var cds = new int[n];
        for (int i = 0; i < n; i++)
            cds[i] = _emotionMgr.GetCooldown(i);
        _ui.UpdateButtonCooldowns(cds);
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
