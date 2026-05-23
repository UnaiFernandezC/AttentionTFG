using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class AttractionGameManager : MinigameBase
{

    [Header("Condicion de victoria")]
    public float targetSafeTime = 15f;

    [Header("Vidas del jugador")]
    public int startLives = 3;

    [Header("Estimulos")]
    public int   stimulusCount      = 3;
    public float attractionStrength = 160f;
    public float influenceRadius    = 320f;
    public float contactRadius      = 46f;

    [Header("Zona segura (radio en canvas units)")]
    public float safeZoneRadius = 115f;

    [Header("Fisica del cursor (menor damping = mas dificil)")]
    public float dampingFactor = 1.4f;

    [Header("Inestabilidad de zona (0 = desactivado)")]
    public float instabilityStrength = 110f;

    AttractionController    _attraction;
    AttractionCursorController _cursor;
    AttractionUIController  _ui;

    float _safeTime;
    int   _lives;
    bool  _invulnerable;
    const float INVULN_DURATION = 1.2f;

    protected override string GetIntroDescription() =>
        "Los circulos rojos atraen tu cursor hacia ellos.\n" +
        "Mueve el raton para resistir y quedarte en la zona verde.\n\n" +
        "Cada contacto con un circulo rojo te quita una vida.\n" +
        "Aguanta " + targetSafeTime.ToString("0") + " segundos en la zona segura para ganar.";

    void ApplyDifficulty()
    {
        var diff = GameManager.Instance != null
            ? GameManager.Instance.CurrentDifficulty
            : DifficultyLevel.Easy;
        switch (diff)
        {
            case DifficultyLevel.Medium:
                targetSafeTime      = 20f;
                stimulusCount       = 4;
                attractionStrength  = 200f;
                dampingFactor       = 1.2f;
                break;
            case DifficultyLevel.Hard:
                targetSafeTime      = 25f;
                stimulusCount       = 5;
                attractionStrength  = 240f;
                dampingFactor       = 1.0f;
                break;
        }
    }

    protected override void OnMinigameStart()
    {
        ApplyDifficulty();
        EnsureEventSystem();

        _attraction = GetComponent<AttractionController>();
        _cursor     = GetComponent<AttractionCursorController>();
        _ui         = GetComponent<AttractionUIController>();

        _ui.BuildUI(safeZoneRadius, () => RestartMinigame(), () => ReturnToGameSelector());

        var positions = GetStimulusPositions(stimulusCount);
        _attraction.BuildStimuli(_ui.GameAreaRT, positions,
                                 attractionStrength, influenceRadius, contactRadius);

        _cursor.damping             = dampingFactor;
        _cursor.maxPullOffset       = 380f;
        _cursor.cursorRadius        = 18f;
        _cursor.instabilityStrength = instabilityStrength;
        _cursor.Initialize(_ui.CanvasRT, _ui.CursorRT, _attraction);

        _safeTime    = 0f;
        _lives       = startLives;
        _invulnerable = false;

        _ui.UpdateLives(_lives, startLives);
        _ui.UpdateSafeBar(0f, targetSafeTime);
    }

    protected override void OnMinigameComplete() { }
    protected override void OnMinigameFailed()   { }

    void Update()
    {
        if (!IsPlaying) return;

        _cursor.Tick();

        bool inSafe   = _cursor.IsInSafeZone(safeZoneRadius);
        bool touching = !_invulnerable && _cursor.IsTouchingStimulus();

        if (inSafe)
        {
            _safeTime += Time.deltaTime;
            _ui.UpdateSafeBar(_safeTime, targetSafeTime);
            _ui.SetSafeZoneActive(true);
        }
        else
        {
            _ui.SetSafeZoneActive(false);
        }

        _ui.UpdateDangerIndicator(_cursor.DangerLevel, inSafe);

        if (touching)
        {
            _lives--;
            _ui.UpdateLives(_lives, startLives);
            _ui.FlashHit();
            StartCoroutine(InvulnerabilityRoutine());

            if (_lives <= 0)
            {
                EndGame(won: false);
                return;
            }
        }

        if (_safeTime >= targetSafeTime)
        {
            EndGame(won: true);
        }
    }

    void EndGame(bool won)
    {
        int score = won ? Mathf.RoundToInt(200f - _cursor.DangerLevel * 20f) : 0;
        CompleteMinigame(score);
        _ui.ShowResult(won, score, _safeTime, targetSafeTime);
    }

    IEnumerator InvulnerabilityRoutine()
    {
        _invulnerable = true;
        yield return new WaitForSeconds(INVULN_DURATION);
        _invulnerable = false;
    }

    static List<Vector2> GetStimulusPositions(int count)
    {

        var all = new List<Vector2>
        {
            new Vector2(-370f,    0f),
            new Vector2( 370f,    0f),
            new Vector2(   0f,  310f),
            new Vector2(   0f, -310f),
            new Vector2(-280f,  240f),
        };

        var result = new List<Vector2>();
        for (int i = 0; i < Mathf.Min(count, all.Count); i++)
            result.Add(all[i]);
        return result;
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
