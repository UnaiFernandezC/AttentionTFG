using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class DifficultyImageNavigator : MonoBehaviour
{
    [Header("Nombres de escena destino")]
    [SerializeField] string easyScene   = "GameSelector";
    [SerializeField] string mediumScene = "GameSelector 1";
    [SerializeField] string hardScene   = "GameSelector 2";

    void Start()
    {
        Setup("Image",      easyScene);
        Setup("Image (1)",  mediumScene);
        Setup("Image (2)",  hardScene);
    }

    void Setup(string objectName, string sceneName)
    {
        var go = GameObject.Find(objectName);
        if (go == null)
        {
            Debug.LogWarning($"[DifficultyImageNavigator] No se encontro '{objectName}'");
            return;
        }

        if (go.GetComponent<Image>() == null)
            go.AddComponent<Image>().color = Color.clear;

        var btn = go.GetComponent<Button>();
        if (btn == null) btn = go.AddComponent<Button>();

        var cb = btn.colors;
        cb.normalColor      = Color.white;
        cb.highlightedColor = new Color(1f, 1f, 1f, 0.85f);
        cb.pressedColor     = new Color(0.85f, 0.85f, 0.85f);
        btn.colors = cb;

        string target = sceneName;
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() =>
        {
            if (GameManager.Instance != null)
            {
                if (target == easyScene)   GameManager.Instance.SetDifficulty(DifficultyLevel.Easy);
                if (target == mediumScene) GameManager.Instance.SetDifficulty(DifficultyLevel.Medium);
                if (target == hardScene)   GameManager.Instance.SetDifficulty(DifficultyLevel.Hard);
            }
            SceneTransition.LoadScene(target);
        });

        Debug.Log($"[DifficultyImageNavigator] '{objectName}' → '{sceneName}'");
    }
}
