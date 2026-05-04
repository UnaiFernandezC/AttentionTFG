using UnityEngine;
using TMPro;

public class CoinManager : MonoBehaviour
{
    public static CoinManager instance;

    public int coinCount = 0;
    public int totalCoins = 10;
    public TextMeshProUGUI coinText;

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }

    public void AddCoin()
    {
        coinCount++;
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
