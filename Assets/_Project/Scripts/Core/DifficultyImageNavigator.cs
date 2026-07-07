// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Selector de dificultad por imágenes (pantalla DifficultySelector, accesible
/// solo desde el menú ESC). ARREGLADO: antes cargaba escenas inexistentes
/// ("GameSelector", "GameSelector 1", "GameSelector 2"); ahora fija la
/// dificultad (que se persiste en el perfil vía GameManager) y navega por
/// SceneLoader al selector correcto de esa dificultad.
/// </summary>
public class DifficultyImageNavigator : MonoBehaviour
{
    void Start()
    {
        Setup("Image",      DifficultyLevel.Easy);
        Setup("Image (1)",  DifficultyLevel.Medium);
        Setup("Image (2)",  DifficultyLevel.Hard);
    }

    void Setup(string objectName, DifficultyLevel level)
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

        ButtonJuice.Attach(go);

        DifficultyLevel captured = level;
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() =>
        {
            if (GameManager.Instance != null)
                GameManager.Instance.SetDifficulty(captured);   // se persiste en el perfil
            SceneLoader.LoadGameSelector();                     // escena válida garantizada
        });

        Debug.Log($"[DifficultyImageNavigator] '{objectName}' → dificultad {level}");
    }
}
