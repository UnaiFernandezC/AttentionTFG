using UnityEngine;
using TMPro;

public class CoinManager : MonoBehaviour
{
    public static CoinManager instance;

    public int coinCount = 0;
    public int totalCoins = 10; // Cambia este valor si tienes más monedas en la escena
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
            Debug.Log("¡Has recogido todas las monedas!");
            // Aquí puedes activar una animación, pantalla de victoria, etc.
        }
    }

    private void UpdateUI()
    {
        if (coinText != null)
            coinText.text = coinCount + "/" + totalCoins;
    }
}
