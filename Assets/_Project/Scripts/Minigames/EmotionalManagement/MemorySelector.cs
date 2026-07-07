// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class MemoryQuestion
{
    public string questionTitle;
    public string[] options = new string[4];
    public int correctIndex;
}

/// <summary>
/// Aventura emocional: preguntas de reconocimiento emocional y empatia.
/// Cada acierto hace saltar al personaje 3D hacia la meta (si la escena lo tiene).
/// Hereda de MinigameBase: intro, telemetria y resultados unificados.
/// </summary>
public class MemorySelector : MinigameBase
{
    [Header("Preguntas y opciones (si esta vacio se usa el banco por defecto)")]
    public List<MemoryQuestion> questions;

    public TextMeshProUGUI questionTitleText;
    public List<Button> optionButtons;
    public TextMeshProUGUI[] optionTexts;

    [Header("Controlador de salto")]
    public CharacterJumper characterJumper;

    int _questionCount = 6;
    int _maxErrors     = 3;

    List<MemoryQuestion> _remaining;
    MemoryQuestion       _current;
    int   _answered;
    int   _correct;
    int   _errors;
    int   _score;
    bool  _busy;
    float _shownAt;

    const int POINTS_PER_CORRECT = 20;

    void Awake()
    {
        // La escena serializa la clase antigua (sin estos campos): se fijan aqui.
        minigameName = "Aventura emocional";
        category     = MinigameCategory.EmotionalManagement;
    }

    protected override string GetIntroDescription() =>
        "Lee cada situacion y elige la mejor respuesta emocional.\n" +
        "Cada acierto hace avanzar al personaje hacia la meta.";

    void ApplyDifficulty()
    {
        var diff = GameManager.Instance != null
            ? GameManager.Instance.CurrentDifficulty
            : DifficultyLevel.Easy;
        switch (diff)
        {
            case DifficultyLevel.Medium: _questionCount = 8;  _maxErrors = 2; break;
            case DifficultyLevel.Hard:   _questionCount = 10; _maxErrors = 1; break;
            default:                     _questionCount = 6;  _maxErrors = 3; break;
        }
    }

    protected override void OnMinigameStart()
    {
        ApplyDifficulty();

        bool usingDefault = questions == null || questions.Count == 0;
        var bank = usingDefault ? DefaultQuestions() : questions;

        var diff = GameManager.Instance != null
            ? GameManager.Instance.CurrentDifficulty
            : DifficultyLevel.Easy;

        if (usingDefault && diff == DifficultyLevel.Hard && bank.Count >= 15)
        {
            // En dificil se garantizan las 5 preguntas matizadas (indices 10-14)
            // y se completan con 5 basicas al azar.
            _remaining = bank.GetRange(10, 5);
            var basics = bank.GetRange(0, 10);
            for (int i = 0; i < 5 && basics.Count > 0; i++)
            {
                int r = Random.Range(0, basics.Count);
                _remaining.Add(basics[r]);
                basics.RemoveAt(r);
            }
        }
        else
        {
            _remaining = new List<MemoryQuestion>(bank);
        }

        _questionCount = Mathf.Min(_questionCount, _remaining.Count);
        _answered = _correct = _errors = _score = 0;
        _busy = false;

        if (optionButtons != null)
            foreach (var b in optionButtons)
                if (b != null) ButtonJuice.Attach(b.gameObject);

        LoadRandomQuestion();
    }

    protected override void OnMinigameComplete() { }
    protected override void OnMinigameFailed()   { }

    void LoadRandomQuestion()
    {
        if (!IsPlaying) return;
        _busy = false;

        if (_remaining.Count == 0) { EndWon(); return; }

        int randomIndex = Random.Range(0, _remaining.Count);
        _current = _remaining[randomIndex];
        _remaining.RemoveAt(randomIndex);

        if (questionTitleText != null)
            questionTitleText.text = _current.questionTitle;

        int n = Mathf.Min(
            optionTexts != null ? optionTexts.Length : 0,
            optionButtons != null ? optionButtons.Count : 0);
        n = Mathf.Min(n, _current.options.Length);

        for (int i = 0; i < n; i++)
        {
            if (optionTexts[i] != null) optionTexts[i].text = _current.options[i];
            if (optionButtons[i] == null) continue;

            int index = i;
            optionButtons[i].onClick.RemoveAllListeners();
            optionButtons[i].onClick.AddListener(() => CheckAnswer(index));
        }

        _shownAt = Time.realtimeSinceStartup;
    }

    void CheckAnswer(int selectedIndex)
    {
        if (!IsPlaying || _busy || _current == null) return;
        _busy = true;
        _answered++;

        float rtMs = (Time.realtimeSinceStartup - _shownAt) * 1000f;
        bool  ok   = selectedIndex == _current.correctIndex;
        ReportEvent(ok, rtMs);

        RectTransform btnRT = null;
        if (optionButtons != null && selectedIndex < optionButtons.Count
            && optionButtons[selectedIndex] != null)
            btnRT = optionButtons[selectedIndex].GetComponent<RectTransform>();

        if (ok)
        {
            _correct++;
            _score += POINTS_PER_CORRECT;
            GameFeel.Success(btnRT);
            GameFeel.FloatingText("+" + POINTS_PER_CORRECT,
                                  new Color(0.22f, 0.86f, 0.54f), new Vector2(0f, 160f));

            // Salto 3D null-safe: solo si la escena tiene jumper con plataformas.
            if (characterJumper != null
                && characterJumper.jumpTargets != null
                && characterJumper.jumpTargets.Length > 0)
                characterJumper.JumpToNextPlatform();
        }
        else
        {
            _errors++;
            GameFeel.Error(btnRT);
            GameFeel.FloatingText("Piensa como se sentiria...",
                                  new Color(0.92f, 0.45f, 0.35f), new Vector2(0f, 160f), 36f);
        }

        if (_errors >= _maxErrors)       { Invoke(nameof(EndLost), 1.0f); return; }
        if (_answered >= _questionCount) { Invoke(nameof(EndWon),  1.0f); return; }

        Invoke(nameof(LoadRandomQuestion), 1.2f);
    }

    void EndWon()
    {
        if (!IsPlaying) return;
        CompleteMinigame(_score);
        GameFeel.Confetti();

        float ratio = _answered > 0 ? (float)_correct / _answered : 0f;
        ShowResults(
            true,
            GameFeel.StarsFromRatio(true, ratio),
            _score,
            BuildStats(),
            "¡Llegaste a la meta!",
            "Entender como se sienten los demas ayuda a elegir mejor.");
    }

    void EndLost()
    {
        if (!IsPlaying) return;
        FailMinigame();
        ShowResults(
            false,
            0,
            _score,
            BuildStats(),
            "El camino se corto",
            "Antes de responder, imagina como se siente cada persona.");
    }

    string[] BuildStats() => new[]
    {
        "Aciertos: " + _correct + " de " + _questionCount,
        "Errores: " + _errors + " (maximo " + _maxErrors + ")"
    };

    /// <summary>
    /// Banco por defecto (15): reconocimiento de emociones, empatia y
    /// "que hacer cuando..." para 5-10 anos. Las 5 ultimas son matizadas
    /// (opciones mas parecidas entre si) y se priorizan en dificil.
    /// </summary>
    static List<MemoryQuestion> DefaultQuestions() => new List<MemoryQuestion>
    {
        // ----- Reconocimiento de emociones -----
        new MemoryQuestion { questionTitle = "Tu amiga llora porque perdio su peluche. ¿Como se siente?",
            options = new[] { "Contenta", "Triste", "Aburrida", "Sorprendida" }, correctIndex = 1 },
        new MemoryQuestion { questionTitle = "Marcos grita y aprieta los punos porque le quitaron su turno. ¿Que siente?",
            options = new[] { "Enfado", "Alegria", "Sueno", "Calma" }, correctIndex = 0 },
        new MemoryQuestion { questionTitle = "Lucia sonrie y salta porque manana es su cumpleanos. ¿Que siente?",
            options = new[] { "Miedo", "Verguenza", "Alegria", "Tristeza" }, correctIndex = 2 },
        new MemoryQuestion { questionTitle = "A Hugo le tiemblan las piernas antes de hablar en clase. ¿Que siente?",
            options = new[] { "Nervios o miedo", "Rabia", "Felicidad", "Aburrimiento" }, correctIndex = 0 },
        new MemoryQuestion { questionTitle = "Sara se pone roja cuando la aplauden. ¿Que puede sentir?",
            options = new[] { "Verguenza", "Enfado", "Hambre", "Frio" }, correctIndex = 0 },

        // ----- Empatia -----
        new MemoryQuestion { questionTitle = "Un companero nuevo esta solo en el recreo. ¿Que puedes hacer?",
            options = new[] { "Ignorarlo", "Reirme de el", "Invitarlo a jugar", "Esconderme" }, correctIndex = 2 },
        new MemoryQuestion { questionTitle = "Tu hermano rompio su juguete favorito y llora. ¿Que le dices?",
            options = new[] { "\"No es para tanto\"", "\"Te entiendo, era tu favorito\"", "\"Callate ya\"", "Nada, me voy" }, correctIndex = 1 },
        new MemoryQuestion { questionTitle = "Alguien se cae en el patio y todos rien. ¿Que haces tu?",
            options = new[] { "Reirme mas fuerte", "Hacerle una foto", "Mirar hacia otro lado", "Preguntarle si esta bien" }, correctIndex = 3 },

        // ----- Que hacer cuando... -----
        new MemoryQuestion { questionTitle = "Estas muy enfadado con un amigo. ¿Que haces primero?",
            options = new[] { "Pegarle", "Respirar hondo y calmarme", "Gritarle muy fuerte", "Romper sus cosas" }, correctIndex = 1 },
        new MemoryQuestion { questionTitle = "Pierdes en un juego y tienes ganas de llorar. ¿Que puedes hacer?",
            options = new[] { "Tirar el juego al suelo", "Culpar a los demas", "Respirar y pedir la revancha", "No jugar nunca mas" }, correctIndex = 2 },

        // ----- Matizadas (opciones mas parecidas: se usan en dificil) -----
        new MemoryQuestion { questionTitle = "Tu amigo saco mala nota y tu sacaste un 10. ¿Que es mejor decirle?",
            options = new[] { "\"Yo saque un 10, mira\"", "\"Si quieres practicamos juntos\"", "\"La proxima vez estudia\"", "\"No pasa nada, olvidalo\"" }, correctIndex = 1 },
        new MemoryQuestion { questionTitle = "Ves a tu mejor amiga jugando con otra nina y sientes celos. ¿Que haces?",
            options = new[] { "Decirle que ya no es mi amiga", "Jugar solo y no contarselo", "Acercarme y jugar los tres", "Decirle a la otra nina que se vaya" }, correctIndex = 2 },
        new MemoryQuestion { questionTitle = "Rompiste sin querer el dibujo de un companero. ¿Que es lo mejor?",
            options = new[] { "Esconder el dibujo", "Decir que fue otro", "Pedir perdon y ofrecer ayuda", "Esperar a que no se de cuenta" }, correctIndex = 2 },
        new MemoryQuestion { questionTitle = "Tu amigo esta callado y con la mirada baja, pero dice \"estoy bien\". ¿Que haces?",
            options = new[] { "Creerle y marcharme", "Decirle \"te noto triste, ¿quieres hablar?\"", "Contarselo a todos", "Hacerle cosquillas sin preguntar" }, correctIndex = 1 },
        new MemoryQuestion { questionTitle = "Estas nervioso por un examen aunque estudiaste mucho. ¿Que piensas?",
            options = new[] { "\"Seguro que suspendo\"", "\"Me prepare bien, lo intentare con calma\"", "\"No voy a ir al examen\"", "\"Los examenes no importan\"" }, correctIndex = 1 },
    };
}
