using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Asigna al componente Button de este GameObject la acción de seleccionar
/// una dificultad concreta y navegar al GameSelector correspondiente.
///
/// Flujo: clic → SetDifficulty en GameManager → SceneLoader.LoadGameSelector()
/// </summary>
[RequireComponent(typeof(Button))]
public class DifficultyButton : MonoBehaviour
{
    [Header("Dificultad de este botón")]
    [SerializeField] private DifficultyLevel difficulty = DifficultyLevel.Easy;

    void Awake()
    {
        var btn = GetComponent<Button>();
        btn.onClick.AddListener(OnClick);
    }

    void OnClick()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.SetDifficulty(difficulty);
        else
            Debug.LogWarning("[DifficultyButton] GameManager no encontrado en la escena.");

        SceneLoader.LoadGameSelector();
    }
}
