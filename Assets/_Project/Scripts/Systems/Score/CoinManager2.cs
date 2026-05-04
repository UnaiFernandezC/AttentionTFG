using UnityEngine;
using TMPro;

public class CoinManager2 : MonoBehaviour
{
    public static CoinManager2 instance;

    public int coinCount = 0;
    public int totalCoins = 100;
    public TextMeshProUGUI coinText;

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }

    public void AddCoins(int cantidad)
    {
        coinCount += cantidad;
        if (coinCount > totalCoins)
            coinCount = totalCoins;

        UpdateUI();

        if (coinCount >= totalCoins)
        {
            Debug.Log("�Has recogido todas las monedas!");

        }
    }

    private void UpdateUI()
    {
        if (coinText != null)
            coinText.text = coinCount + "/" + totalCoins;
    }
}
