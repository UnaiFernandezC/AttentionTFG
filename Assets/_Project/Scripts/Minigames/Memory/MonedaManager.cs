using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class MonedaUIManager : MonoBehaviour
{
    [Header("Contador Total")]
    public TextMeshProUGUI contadorMonedasTMP;

    [Header("Mensaje Temporal +X")]
    public GameObject mensajeGO;
    public TextMeshProUGUI mensajeTMP;
    public Image iconoMoneda;

    private int monedas = 0;
    private int metaMonedas = 10;

    private Coroutine mensajeCoroutine;

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

    public void Reiniciar()
    {
        monedas = 0;
        ActualizarContador();
        mensajeGO.SetActive(false);
    }
}
