using System.Collections;
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

public class MemorySelector : MonoBehaviour
{
    [Header("Preguntas y opciones")]
    public List<MemoryQuestion> questions; // Lista original (se mantiene como base)
    private List<MemoryQuestion> remainingQuestions; // Lista interna sin repetir

    public TextMeshProUGUI questionTitleText;
    public List<Button> optionButtons;
    public TextMeshProUGUI[] optionTexts;

    [Header("Controlador de salto")]
    public CharacterJumper characterJumper;

    private MemoryQuestion currentQuestion;

    IEnumerator Start()
    {
        // Panel de introduccion
        bool _started = false;
        var introCanvas = IntroPanel.Build(
            "Aventura emocional",
            "Gestion emocional",
            "Lee cada situacion con atencion y elige la respuesta correcta.\n" +
            "Si aciertas, el personaje avanzara hacia la meta.\n" +
            "Piensa bien antes de responder, las emociones importan!",
            () => _started = true);

        while (!_started)
        {
            if (Input.GetKeyDown(KeyCode.Space)) _started = true;
            yield return null;
        }
        Object.Destroy(introCanvas);

        // Clonar la lista original para no modificarla directamente
        remainingQuestions = new List<MemoryQuestion>(questions);
        LoadRandomQuestion();
    }

    void LoadRandomQuestion()
    {
        if (remainingQuestions.Count == 0)
        {
            Debug.Log("�Se han acabado las preguntas!");
            return;
        }

        // Escoge una aleatoria de las que quedan
        int randomIndex = Random.Range(0, remainingQuestions.Count);
        currentQuestion = remainingQuestions[randomIndex];
        remainingQuestions.RemoveAt(randomIndex); // Eliminarla para no repetir

        // Mostrar t�tulo
        if (questionTitleText != null)
            questionTitleText.text = currentQuestion.questionTitle;

        // Mostrar opciones
        for (int i = 0; i < optionTexts.Length; i++)
        {
            optionTexts[i].text = currentQuestion.options[i];

            int index = i; // Capturar variable local
            optionButtons[i].onClick.RemoveAllListeners();
            optionButtons[i].onClick.AddListener(() => CheckAnswer(index));
        }
    }

    void CheckAnswer(int selectedIndex)
    {
        if (selectedIndex == currentQuestion.correctIndex)
        {
            Debug.Log("�Respuesta correcta!");
            characterJumper.JumpToNextPlatform();
        }
        else
        {
            Debug.Log("Respuesta incorrecta");
            // Feedback visual aqu� si quieres
        }

        // Cargar siguiente pregunta tras un peque�o delay
        Invoke(nameof(LoadRandomQuestion), 1.2f);
    }
}
