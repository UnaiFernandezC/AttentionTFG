// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class WordMemoryGameManager : MinigameBase
{

    // ------------------------------------------------ dificultad (runtime)
    int   _totalRounds     = 3;
    int   _roundsToWin     = 2;
    int   _wordsPerRound   = 3;
    int   _distractorCount = 3;
    float _memorizeTime    = 6f;
    bool  _similarDistractors = false;

    [Header("Tiempo de feedback tras confirmar (s)")]
    public float feedbackTime = 2.0f;

    static readonly string[] WORD_POOL = new string[]
    {
        "PERRO",    "GATO",     "CASA",     "ARBOL",    "LUNA",
        "SOL",      "MAR",      "PAN",      "LECHE",    "MESA",
        "SILLA",    "LIBRO",    "TREN",     "AVION",    "COCHE",
        "FLOR",     "NUBE",     "PIEDRA",   "AGUA",     "FUEGO",
        "TIERRA",   "VIENTO",   "ESTRELLA", "MONTANA",  "RIO",
        "PUENTE",   "CIUDAD",   "CAMPO",    "BOSQUE",   "PLAYA",
        "PAJARO",   "PEZ",      "CABALLO",  "LOBO",     "OSO",
        "TIGRE",    "CONEJO",   "RANA",     "SERPIENTE","AGUILA",
        "NARANJA",  "MANZANA",  "UVAS",     "LIMON",    "PERA",
        "ZAPATO",   "RELOJ",    "LAMPARA",  "VENTANA",  "PUERTA"
    };

    // Palabras "trampa" muy parecidas (solo se usan en dificultad alta).
    static readonly Dictionary<string, string> SIMILAR = new Dictionary<string, string>
    {
        { "GATO",   "GATA"   }, { "CASA",  "CAJA"  }, { "LUNA",  "LANA"  },
        { "SOL",    "SAL"    }, { "MESA",  "MASA"  }, { "PAN",   "PLAN"  },
        { "PERA",   "PERLA"  }, { "RANA",  "RAMA"  }, { "OSO",   "OSA"   },
        { "LIBRO",  "LITRO"  }, { "TREN",  "TRES"  }, { "RIO",   "RISA"  },
        { "MAR",    "MAPA"   }, { "FLOR",  "FLAN"  }, { "PEZ",   "PIEZA" },
        { "LOBO",   "LODO"   }, { "NUBE",  "NUEVE" }, { "AGUA",  "AGUJA" },
        { "FUEGO",  "JUEGO"  }, { "COCHE", "NOCHE" }, { "PERRO", "PERA"  },
        { "PUERTA", "PUENTE" },
    };

    WordMemoryUIController _ui;

    int          _score;
    int          _roundsWon;
    int          _errors;
    int          _totalCorrect;
    int          _totalTargets;
    bool         _confirmed;
    int          _currentRound;

    List<string> _currentTargets;
    List<string> _allWordsShown;
    HashSet<int> _targetIndices;

    protected override string GetIntroDescription() =>
        "Memoriza las palabras que aparecen.\n" +
        "Luego, ¡encuéntralas entre las demás!";

    void ApplyDifficulty()
    {
        var diff = GameManager.Instance != null
            ? GameManager.Instance.CurrentDifficulty
            : DifficultyLevel.Easy;

        switch (diff)
        {
            case DifficultyLevel.Medium:
                _wordsPerRound   = 5;
                _distractorCount = 5;
                _memorizeTime    = 5f;
                _similarDistractors = false;
                break;
            case DifficultyLevel.Hard:
                _wordsPerRound   = 7;
                _distractorCount = 8;
                _memorizeTime    = 4f;
                _similarDistractors = true;
                break;
            default:
                _wordsPerRound   = 3;
                _distractorCount = 3;
                _memorizeTime    = 6f;
                _similarDistractors = false;
                break;
        }
        _totalRounds = 3;
        _roundsToWin = 2;
    }

    protected override void OnMinigameStart()
    {
        ApplyDifficulty();
        EnsureEventSystem();

        _ui = GetComponent<WordMemoryUIController>();

        _currentRound = 0;
        _score        = 0;
        _roundsWon    = 0;
        _errors       = 0;
        _totalCorrect = 0;
        _totalTargets = 0;
        _confirmed    = false;

        _ui.BuildUI(
            idx => OnWordToggled(idx),
            ()  => OnConfirm(),
            ()  => RestartMinigame(),
            ()  => ReturnToGameSelector());

        _ui.UpdateScore(0);
        _ui.UpdateRound(1, _totalRounds);

        StartCoroutine(GameLoop());
    }

    protected override void OnMinigameComplete() { }
    protected override void OnMinigameFailed()   { }

    void Update()
    {
        if (IsPlaying && Input.GetKeyDown(KeyCode.Space))
            OnConfirm();
    }

    IEnumerator GameLoop()
    {
        yield return new WaitForSeconds(0.4f);

        for (_currentRound = 0; _currentRound < _totalRounds; _currentRound++)
        {
            int targetCount = _wordsPerRound;
            _totalTargets  += targetCount;

            _ui.UpdateRound(_currentRound + 1, _totalRounds);

            BuildWordLists(targetCount, _distractorCount);

            _ui.SetPhaseLabel("¡Memoriza!", new Color(0.58f, 0.28f, 0.92f));
            _ui.SetInfoLabel(targetCount + " palabras · " + Mathf.RoundToInt(_memorizeTime) + " segundos");
            _ui.ShowMemorizePhase(_currentTargets);
            GameFeel.PlayPop();

            float elapsed = 0f;
            while (elapsed < _memorizeTime)
            {
                elapsed += Time.deltaTime;
                _ui.SetCountdown(1f - elapsed / _memorizeTime);
                yield return null;
            }

            _confirmed = false;
            _ui.SetPhaseLabel("¿Cuáles viste?", Color.white);
            _ui.SetInfoLabel("Toca las palabras que recuerdas y confirma");
            _ui.ShowChoosePhase(_allWordsShown);

            float waitMax   = 30f;
            float waited    = 0f;
            while (!_confirmed && waited < waitMax)
            {
                waited += Time.deltaTime;
                yield return null;
            }

            var selected = _ui.GetSelectedIndices();
            int correct  = 0;
            int wrong    = 0;

            foreach (int idx in selected)
            {
                if (_targetIndices.Contains(idx)) correct++;
                else                              wrong++;
            }

            bool roundWon = (correct >= Mathf.CeilToInt(targetCount * 0.75f)) && (wrong <= 1);
            if (roundWon) _roundsWon++;

            ReportEvent(roundWon, waited * 1000f);

            _totalCorrect += correct;

            int roundScore = correct * 15 - wrong * 8;
            if (correct == targetCount && wrong == 0) roundScore += 25;
            roundScore = Mathf.Max(0, roundScore);
            _score    += roundScore;
            _ui.UpdateScore(_score);

            _ui.ShowWordResult(_targetIndices, selected);

            string msg; Color col;
            if (correct == targetCount && wrong == 0)
            {
                msg = "¡Perfecto! +" + roundScore + " pts";
                col = new Color(0.25f, 0.90f, 0.52f);
                GameFeel.PlaySuccess();
                GameFeel.Confetti(30);
                GameFeel.FloatingText("+" + roundScore, col);
            }
            else if (roundWon)
            {
                msg = "¡Bien! " + correct + "/" + targetCount + " palabras";
                col = new Color(0.96f, 0.72f, 0.18f);
                GameFeel.PlayStar();
            }
            else
            {
                msg = correct + "/" + targetCount + " correctas · " + wrong + " errores";
                col = new Color(0.90f, 0.28f, 0.30f);
                GameFeel.Error(null);
            }

            _ui.SetPhaseLabel(msg, col);
            _ui.SetInfoLabel("Verde = correcto · Naranja = te faltaba · Rojo = error");

            _errors += wrong;

            yield return new WaitForSeconds(feedbackTime);

            if (_errors >= 4)
            {
                FailMinigame();
                ShowFinal(false);
                yield break;
            }
        }

        yield return new WaitForSeconds(0.3f);

        bool won = _roundsWon >= _roundsToWin;
        if (won) CompleteMinigame(_score);
        else     FailMinigame();

        if (won) GameFeel.Confetti(60);
        ShowFinal(won);
    }

    void ShowFinal(bool won)
    {
        float ratio = _totalTargets > 0 ? (float)_totalCorrect / _totalTargets : 0f;
        int   stars = GameFeel.StarsFromRatio(won, ratio);

        ShowResults(won, stars, _score,
            new string[]
            {
                "Rondas ganadas: " + _roundsWon + "/" + _totalRounds,
                "Palabras acertadas: " + _totalCorrect + "/" + _totalTargets,
                "Errores: " + _errors
            },
            won ? "¡Memoria de elefante!" : "¡Casi lo tienes!",
            won ? "Recordaste muchísimas palabras." : "Inténtalo otra vez, ¡tú puedes!");
    }

    void OnWordToggled(int idx)
    {
        if (!IsPlaying || _confirmed) return;
        GameFeel.PlayPop();
        _ui.ToggleWord(idx);
    }

    void OnConfirm()
    {
        if (!IsPlaying || _confirmed) return;
        _confirmed = true;
    }

    void BuildWordLists(int targetCount, int distractorCount)
    {
        var pool = new List<string>(WORD_POOL);
        Shuffle(pool);

        _currentTargets = new List<string>();
        for (int i = 0; i < Mathf.Min(targetCount, pool.Count); i++)
            _currentTargets.Add(pool[i]);

        var used = new HashSet<string>(_currentTargets);
        var distractors = new List<string>();

        // En dificultad alta, priorizamos palabras casi iguales (GATO/GATA).
        if (_similarDistractors)
        {
            foreach (var t in _currentTargets)
            {
                if (distractors.Count >= distractorCount) break;
                if (SIMILAR.TryGetValue(t, out string twin) && !used.Contains(twin))
                {
                    distractors.Add(twin);
                    used.Add(twin);
                }
            }
        }

        for (int i = targetCount; i < pool.Count && distractors.Count < distractorCount; i++)
        {
            if (used.Contains(pool[i])) continue;
            distractors.Add(pool[i]);
            used.Add(pool[i]);
        }

        _allWordsShown = new List<string>(_currentTargets);
        _allWordsShown.AddRange(distractors);
        Shuffle(_allWordsShown);

        _targetIndices = new HashSet<int>();
        var targetSet  = new HashSet<string>(_currentTargets);
        for (int i = 0; i < _allWordsShown.Count; i++)
            if (targetSet.Contains(_allWordsShown[i]))
                _targetIndices.Add(i);
    }

    static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            T tmp = list[i]; list[i] = list[j]; list[j] = tmp;
        }
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
