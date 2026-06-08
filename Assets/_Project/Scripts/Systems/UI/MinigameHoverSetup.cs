using UnityEngine;

public class MinigameHoverSetup : MonoBehaviour
{
    void Start()
    {
        var allObjects = FindObjectsOfType<GameObject>(includeInactive: false);
        foreach (var go in allObjects)
        {
            if (go.name.StartsWith("Minigame"))
            {
                if (go.GetComponent<ButtonHoverScaler>() == null)
                    go.AddComponent<ButtonHoverScaler>();
            }
        }
    }
}
