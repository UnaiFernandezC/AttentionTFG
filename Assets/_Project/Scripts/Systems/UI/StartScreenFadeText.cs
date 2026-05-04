using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class StartScreenFadeText : MonoBehaviour
{
    public TextMeshProUGUI pressEnterText;
    public float fadeSpeed = 2f;

    void Update()
    {

        float alpha = Mathf.PingPong(Time.time * fadeSpeed, 1f);
        Color color = pressEnterText.color;
        color.a = alpha;
        pressEnterText.color = color;

        if (Input.GetKeyDown(KeyCode.Return))
        {
            SceneManager.LoadScene("DifficultySelector");
        }
    }
}
