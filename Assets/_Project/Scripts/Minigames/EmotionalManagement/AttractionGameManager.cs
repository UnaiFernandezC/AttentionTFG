// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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
    int   _bonusScore;
    int   _goldenIgnored;
    float _milestoneTimer;
    bool  _invulnerable;
    bool  _goldenEnabled;
    const float INVULN_DURATION = 1.2f;
    const float MILESTONE_SECONDS = 5f;    // cada 5 s en la burbuja = acierto

    protected override string GetIntroDescription() =>
        "Las distracciones y enfados (circulos rojos) tiran de tu cursor.\n" +
        "Resiste y quedate dentro de tu burbuja de calma para ganar.";

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
                _goldenEnabled      = true;   // estimulo dorado: bonus si lo IGNORAS
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

        _ui.BuildUI(safeZoneRadius);

        var positions = GetStimulusPositions(stimulusCount);
        _attraction.BuildStimuli(_ui.GameAreaRT, positions,
                                 attractionStrength, influenceRadius, contactRadius);

        _cursor.damping             = dampingFactor;
        _cursor.maxPullOffset       = 380f;
        _cursor.cursorRadius        = 18f;
        _cursor.instabilityStrength = instabilityStrength;
        _cursor.Initialize(_ui.CanvasRT, _ui.CursorRT, _attraction);

        _safeTime       = 0f;
        _lives          = startLives;
        _bonusScore     = 0;
        _goldenIgnored  = 0;
        _milestoneTimer = 0f;
        _invulnerable   = false;

        _ui.UpdateLives(_lives, startLives);
        _ui.UpdateSafeBar(0f, targetSafeTime);

        if (_goldenEnabled)
            StartCoroutine(GoldenStimulusRoutine());
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

            // Cada 5 s seguidos dentro de la burbuja = acierto (telemetria) + refuerzo.
            _milestoneTimer += Time.deltaTime;
            if (_milestoneTimer >= MILESTONE_SECONDS)
            {
                _milestoneTimer -= MILESTONE_SECONDS;
                ReportEvent(true);
                GameFeel.PlayPop();
                GameFeel.FloatingText("+" + Mathf.RoundToInt(MILESTONE_SECONDS) + " s de calma",
                                      new Color(0.22f, 0.86f, 0.54f), new Vector2(0f, 200f), 40f);
            }
        }
        else
        {
            _milestoneTimer = 0f;
            _ui.SetSafeZoneActive(false);
        }

        _ui.UpdateDangerIndicator(_cursor.DangerLevel, inSafe);

        if (touching)
        {
            _lives--;
            ReportEvent(false);
            GameFeel.PlayError();
            _ui.UpdateLives(_lives, startLives);
            _ui.FlashHit();
            GameFeel.FloatingText("¡Te atrapo una distraccion!",
                                  new Color(0.90f, 0.30f, 0.30f), new Vector2(0f, -200f), 38f);
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
        int score = won
            ? Mathf.Max(0, Mathf.RoundToInt(200f - _cursor.DangerLevel * 20f)) + _bonusScore
            : 0;

        if (won)
        {
            CompleteMinigame(score);
            GameFeel.PlaySuccess();
            GameFeel.Confetti();
        }
        else
        {
            FailMinigame();
        }

        float ratio = startLives > 0 ? (float)_lives / startLives : 0f;

        var stats = _goldenEnabled
            ? new[]
              {
                  "Tiempo en tu burbuja: " + _safeTime.ToString("0.0") + " s / " + targetSafeTime.ToString("0") + " s",
                  "Vidas restantes: " + Mathf.Max(0, _lives) + " de " + startLives,
                  "Brillos ignorados: " + _goldenIgnored + " (+" + _bonusScore + " pts)"
              }
            : new[]
              {
                  "Tiempo en tu burbuja: " + _safeTime.ToString("0.0") + " s / " + targetSafeTime.ToString("0") + " s",
                  "Vidas restantes: " + Mathf.Max(0, _lives) + " de " + startLives
              };

        ShowResults(
            won,
            GameFeel.StarsFromRatio(won, ratio),
            score,
            stats,
            won ? "¡Protegiste tu calma!" : "Las distracciones te atraparon",
            won ? "Igual que en la vida: notar lo que te altera y no dejarte arrastrar."
                : "Las distracciones y enfados tiran fuerte, pero se les puede resistir.");
    }

    IEnumerator GoldenStimulusRoutine()
    {
        // Estimulo dorado (solo en dificil): aparece 2 s. Si lo IGNORAS, ganas
        // un bonus. Si lo tocas, pierdes el bonus. Entrena inhibir el impulso
        // de ir hacia lo llamativo.
        var goldCol = new Color(1.00f, 0.82f, 0.15f, 0.95f);

        while (IsPlaying)
        {
            yield return new WaitForSeconds(Random.Range(5f, 8f));
            if (!IsPlaying) yield break;

            float   ang = Random.Range(0f, Mathf.PI * 2f);
            Vector2 pos = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang))
                          * Random.Range(safeZoneRadius + 110f, safeZoneRadius + 220f);

            var go = new GameObject("GoldenStimulus");
            go.transform.SetParent(_ui.GameAreaRT, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta        = Vector2.one * 76f;
            var img = go.AddComponent<Image>();
            img.sprite        = AttractionUIController.MakeCircleSprite(128);
            img.color         = goldCol;
            img.raycastTarget = false;
            var lblGO = new GameObject("Lbl");
            lblGO.transform.SetParent(rt, false);
            var lblRT = lblGO.AddComponent<RectTransform>();
            lblRT.anchorMin = new Vector2(0f, -0.65f);
            lblRT.anchorMax = new Vector2(1f, -0.05f);
            lblRT.sizeDelta = new Vector2(160f, 0f);
            var lbl = lblGO.AddComponent<TMPro.TextMeshProUGUI>();
            lbl.text      = "¡Ignorame!";
            lbl.color     = goldCol;
            lbl.fontSize  = 22f;
            lbl.fontStyle = TMPro.FontStyles.Bold;
            lbl.alignment = TMPro.TextAlignmentOptions.Center;
            UITween.PulseOnce(rt, 1.25f, 0.35f);
            GameFeel.PlayPop();

            bool  touched = false;
            float life    = 0f;
            const float GOLD_LIFETIME = 2f;
            const float GOLD_RADIUS   = 38f;

            while (life < GOLD_LIFETIME && IsPlaying)
            {
                life += Time.deltaTime;
                if (Vector2.Distance(_cursor.CursorCanvasPos, pos)
                    < GOLD_RADIUS + _cursor.cursorRadius)
                {
                    touched = true;
                    break;
                }
                yield return null;
            }

            Destroy(go);
            if (!IsPlaying) yield break;

            if (!touched)
            {
                _bonusScore += 25;
                _goldenIgnored++;
                ReportEvent(true);
                GameFeel.PlayStar();
                GameFeel.FloatingText("+25 ¡Lo ignoraste!", goldCol, new Vector2(0f, 120f));
            }
            else
            {
                ReportEvent(false);
                GameFeel.PlayError();
                GameFeel.FloatingText("El brillo te distrajo...",
                                      new Color(0.90f, 0.45f, 0.30f), new Vector2(0f, 120f), 38f);
            }
        }
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
