using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
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
    public List<MemoryQuestion> questions;
    private List<MemoryQuestion> remainingQuestions;

    public TextMeshProUGUI questionTitleText;
    public List<Button> optionButtons;
    public TextMeshProUGUI[] optionTexts;

    [Header("Controlador de salto")]
    public CharacterJumper characterJumper;

    private MemoryQuestion currentQuestion;

    private int _questionsAnswered = 0;
    private int _errors = 0;
    private const int MAX_QUESTIONS = 10;
    private bool _finished = false;

    static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);
    static Vector2 V(float x, float y) => new Vector2(x, y);

    IEnumerator Start()
    {

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

        GameSelectorMusicManager.StopMusic();
        if (UIAudioManager.Instance != null) UIAudioManager.Instance.StopMusic();

        remainingQuestions = new List<MemoryQuestion>(questions);
        LoadRandomQuestion();
    }

    void LoadRandomQuestion()
    {
        if (_finished) return;

        if (remainingQuestions.Count == 0)
        {
            ShowFinalPanel(true);
            return;
        }

        int randomIndex = Random.Range(0, remainingQuestions.Count);
        currentQuestion = remainingQuestions[randomIndex];
        remainingQuestions.RemoveAt(randomIndex);

        if (questionTitleText != null)
            questionTitleText.text = currentQuestion.questionTitle;

        for (int i = 0; i < optionTexts.Length; i++)
        {
            optionTexts[i].text = currentQuestion.options[i];

            int index = i;
            optionButtons[i].onClick.RemoveAllListeners();
            optionButtons[i].onClick.AddListener(() => CheckAnswer(index));
        }
    }

    void CheckAnswer(int selectedIndex)
    {
        if (_finished) return;

        if (selectedIndex == currentQuestion.correctIndex)
        {
            if (characterJumper != null) characterJumper.JumpToNextPlatform();
        }
        else
        {
            _errors++;
        }

        _questionsAnswered++;

        if (_errors >= 3)
        {
            ShowFinalPanel(false);
            return;
        }

        if (_questionsAnswered >= MAX_QUESTIONS)
        {
            ShowFinalPanel(true);
            return;
        }

        Invoke(nameof(LoadRandomQuestion), 1.2f);
    }

    MinigameCategory ResolveCategory()
    {
        string scene = SceneManager.GetActiveScene().name;
        if (scene.Contains("AlgoNoCuadra")) return MinigameCategory.Attention;
        return MinigameCategory.EmotionalManagement;
    }

    void ShowFinalPanel(bool success)
    {
        if (_finished) return;
        _finished = true;

        MinigameCategory cat = ResolveCategory();

        var cGO = new GameObject("Canvas_FinalResult");
        cGO.transform.SetParent(transform, false);
        var cv = cGO.AddComponent<Canvas>();
        cv.renderMode   = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 50;
        var sc = cGO.AddComponent<CanvasScaler>();
        sc.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920f, 1080f);
        sc.matchWidthOrHeight  = 0.5f;
        cGO.AddComponent<GraphicRaycaster>();
        var R = cGO.GetComponent<RectTransform>();

        var bg = MkImg(R, "BG", C(0, 0, 0, 0.85f), V(0, 0), V(1, 1), V(0, 0), V(0, 0));

        var card = MkImg(bg, "Card", C(0.08f, 0.11f, 0.22f), V(0.5f, 0.5f), V(0.5f, 0.5f), V(0, 0), V(820f, 460f));
        MkImg(card, "LineT", C(0.40f, 0.72f, 1.00f), V(0, 1), V(1, 1), V(0, -4), V(0, 8));

        var title = MkTxt(card, "Title",
            success ? "¡Bien hecho!" : "Has fallado demasiadas veces",
            success ? C(0.25f, 0.90f, 0.52f) : C(0.90f, 0.28f, 0.30f),
            48, V(0.05f, 0.70f), V(0.95f, 0.95f));
        title.fontStyle = FontStyles.Bold;

        var sub = MkTxt(card, "Sub",
            "Preguntas respondidas: " + _questionsAnswered + "\nErrores: " + _errors,
            C(0.55f, 0.66f, 0.82f), 26, V(0.05f, 0.40f), V(0.95f, 0.68f));
        sub.overflowMode = TextOverflowModes.Overflow;

        MkBtn(card, "Jugar de nuevo",     C(0.40f, 0.72f, 1.00f), V(0.05f, 0.20f), V(0.48f, 0.34f),
            () => SceneLoader.ReloadCurrentScene());
        MkBtn(card, "Volver a la seccion", C(0.18f, 0.24f, 0.38f), V(0.52f, 0.20f), V(0.95f, 0.34f),
            () => SceneLoader.LoadCategorySelector(cat));
        MkBtn(card, "Menu principal",     C(0.10f, 0.13f, 0.22f), V(0.05f, 0.04f), V(0.95f, 0.17f),
            () => SceneLoader.GoToMainMenu());
    }

    RectTransform MkImg(Transform p, string n, Color col, Vector2 am, Vector2 aM, Vector2 pos, Vector2 sd)
    {
        var go = new GameObject(n); go.transform.SetParent(p, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = am; rt.anchorMax = aM; rt.pivot = V(0.5f, 0.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = sd;
        go.AddComponent<Image>().color = col;
        return rt;
    }

    TextMeshProUGUI MkTxt(Transform p, string n, string txt, Color col, float sz, Vector2 am, Vector2 aM)
    {
        var go = new GameObject(n); go.transform.SetParent(p, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = am; rt.anchorMax = aM; rt.pivot = V(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero; rt.sizeDelta = Vector2.zero;
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = txt; t.color = col; t.fontSize = sz;
        t.alignment = TextAlignmentOptions.Center;
        t.overflowMode = TextOverflowModes.Overflow;
        return t;
    }

    void MkBtn(Transform p, string lbl, Color bg, Vector2 am, Vector2 aM, System.Action click)
    {
        var rt = MkImg(p, "Btn_" + lbl, bg, am, aM, V(0, 0), V(0, 0));
        var b = rt.gameObject.AddComponent<Button>();
        b.targetGraphic = rt.GetComponent<Image>();
        var cb = b.colors;
        cb.normalColor      = Color.white;
        cb.highlightedColor = C(1, 1, 1, 0.82f);
        cb.pressedColor     = C(0.72f, 0.72f, 0.72f);
        b.colors = cb;
        b.onClick.AddListener(() => click?.Invoke());
        var t = MkTxt(rt, "T", lbl, Color.white, 24, V(0, 0), V(1, 1));
        t.fontStyle = FontStyles.Bold;
    }
}
