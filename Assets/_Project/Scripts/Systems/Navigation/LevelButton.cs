// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelButton : MonoBehaviour
{
    [Header("GameSelector")]
    public string sceneToLoad;

    private Button button;

    void Awake()
    {

        button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(LoadScene);
        }
        else
        {
            Debug.LogWarning("No se encontr� un componente Button en " + gameObject.name);
        }
    }

    public void LoadScene()
    {
        // Ruta única de navegación: SceneLoader valida la escena (existencia en
        // Build Settings) y muestra una pantalla de error amable si está rota.
        if (string.IsNullOrEmpty(sceneToLoad))
        {
            Debug.LogWarning("No se ha asignado una escena para " + gameObject.name);
            NavErrorScreen.Show(gameObject.name + " (sin escena asignada)");
            return;
        }
        SceneLoader.LoadScene(sceneToLoad);
    }
}
