using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class MonedaUIManager : MonoBehaviour
{
    [Header("Contador Total")]
    public TextMeshProUGUI contadorMonedasTMP; // Texto tipo "6/10"

    [Header("Mensaje Temporal +X")]
    public GameObject mensajeGO;               // Panel central con texto + icono
    public TextMeshProUGUI mensajeTMP;         // Texto tipo "+2"
    public Image iconoMoneda;                  // Icono de moneda (puedes animarlo si deseas)

    private int monedas = 0;
    private int metaMonedas = 10;

    private Coroutine mensajeCoroutine;

    /// <summary>
    /// Llama este método desde otro script para mostrar ganancia y actualizar contador.
    /// </summary>
    public void AgregarMonedas(int cantidad)
    {
        monedas += cantidad;
        ActualizarContador();

        if (mensajeCoroutine != null)
            StopCoroutine(mensajeCoroutine);

        mensajeCoroutine = StartCoroutine(MostrarMensajeTemporal(cantidad));
    }

    private void ActualizarContador()
    {
        contadorMonedasTMP.text = $"{monedas}/{metaMonedas}";
    }

    private IEnumerator MostrarMensajeTemporal(int cantidad)
    {
        mensajeGO.SetActive(true);

        mensajeTMP.text = $"+{cantidad}";
        mensajeTMP.color = cantidad == 0 ? Color.red : Color.yellow;

        mensajeGO.transform.localScale = Vector3.one * 1.5f;

        float tiempoAnim = 0.3f;
        float elapsed = 0f;

        // Animación simple de escala
        while (elapsed < tiempoAnim)
        {
            float t = elapsed / tiempoAnim;
            mensajeGO.transform.localScale = Vector3.Lerp(Vector3.one * 1.5f, Vector3.one, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        mensajeGO.transform.localScale = Vector3.one;

        yield return new WaitForSeconds(1f);

        mensajeGO.SetActive(false);
    }

    // Por si quieres reiniciar las monedas (opcional)
    public void Reiniciar()
    {
        monedas = 0;
        ActualizarContador();
        mensajeGO.SetActive(false);
    }
}
