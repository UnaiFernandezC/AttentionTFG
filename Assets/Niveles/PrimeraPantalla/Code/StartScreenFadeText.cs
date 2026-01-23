using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class StartScreenFadeText : MonoBehaviour
{
    public TextMeshProUGUI pressEnterText;
    public float fadeSpeed = 2f; // Cuanto mayor, más rápido el fade

    void Update()
    {
        // Hacer fade in/out usando Mathf.PingPong
        float alpha = Mathf.PingPong(Time.time * fadeSpeed, 1f);
        Color color = pressEnterText.color;
        color.a = alpha;
        pressEnterText.color = color;

        // Cambio de escena al presionar Enter
        if (Input.GetKeyDown(KeyCode.Return))
        {
            SceneManager.LoadScene("DifficultySelector"); // Cambia por el nombre real de tu escena
        }
    }
}
