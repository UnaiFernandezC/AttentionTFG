using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class JuegoMemoria : MonoBehaviour
{
    [System.Serializable]
    public class BotonObjeto
    {
        public Button botonUI;
        public GameObject objetoEscena;
    }

    [Header("Emparejamientos Botón ↔ Objeto")]
    public List<BotonObjeto> botonesObjetos;

    [Header("Cámara")]
    public Transform camaraPrincipal;
    public Transform puntoObservacion;
    private Vector3 posicionInicial;
    private Quaternion rotacionInicial;

    [Header("UI")]
    public TextMeshProUGUI contadorTMP;
    public GameObject panelBotones;

    [Header("Sonidos")]
    public AudioSource sonidoCorrecto;
    public AudioSource sonidoError;

    [Header("Tiempos")]
    public float tiempoObservacionInicial = 10f;
    public float tiempoEnPunto = 2f;
    public float tiempoObservacionFinal = 5f;
    public float duracionMovimientoCamara = 1.5f;

    [Header("Monedas")]
    public MonedaUIManager monedaUIManager;

    private int rondaActual = 1;
    private int objetosADesaparecer = 1;
    private List<GameObject> objetosEliminados = new List<GameObject>();

    private int aciertosNecesarios;
    private int aciertosActuales;
    private bool respondio;
    private bool aciertoRonda;

    private readonly int[] monedasPorRonda = { 2, 4, 6 };

    private void Start()
    {
        posicionInicial = camaraPrincipal.position;
        rotacionInicial = camaraPrincipal.rotation;
        panelBotones.SetActive(false);

        StartCoroutine(FlujoJuego());
    }

    private IEnumerator FlujoJuego()
    {
        while (rondaActual <= 3)
        {
            // 1️⃣ Observación inicial
            yield return StartCoroutine(Contador(tiempoObservacionInicial));

            // 2️⃣ Mover cámara al punto de observación
            yield return StartCoroutine(MoverCamara(puntoObservacion.position, puntoObservacion.rotation));
            yield return new WaitForSeconds(tiempoEnPunto);

            // 3️⃣ Eliminar objetos
            EliminarObjetosAleatorios(objetosADesaparecer);

            // 4️⃣ Volver a posición inicial
            yield return StartCoroutine(MoverCamara(posicionInicial, rotacionInicial));

            // 5️⃣ Observación final
            yield return StartCoroutine(Contador(tiempoObservacionFinal));

            // 6️⃣ Rotar cámara 180° para mostrar botones
            yield return StartCoroutine(RotarCamara180());

            // 7️⃣ Preparar selección múltiple
            panelBotones.SetActive(true);
            aciertosNecesarios = objetosEliminados.Count;
            aciertosActuales = 0;
            respondio = false;
            aciertoRonda = false;

            // Asignar listeners
            foreach (var par in botonesObjetos)
            {
                par.botonUI.onClick.RemoveAllListeners();
                par.botonUI.interactable = true;
                par.botonUI.image.color = Color.white;

                par.botonUI.onClick.AddListener(() =>
                {
                    if (objetosEliminados.Contains(par.objetoEscena))
                    {
                        // ✅ Selección correcta
                        par.botonUI.interactable = false;
                        par.botonUI.image.color = Color.green;
                        aciertosActuales++;

                        if (aciertosActuales >= aciertosNecesarios)
                        {
                            aciertoRonda = true;
                            respondio = true;
                        }
                    }
                    else
                    {
                        // ❌ Selección incorrecta
                        par.botonUI.image.color = Color.red;
                        aciertoRonda = false;
                        respondio = true;
                    }
                });
            }

            // Esperar a que el jugador responda
            yield return new WaitUntil(() => respondio);

            // 8️⃣ Sonido y monedas
            int monedasGanadas = 0;

            if (aciertoRonda)
            {
                sonidoCorrecto.Play();
                monedasGanadas = monedasPorRonda[rondaActual - 1];
                rondaActual++;
                objetosADesaparecer++;
            }
            else
            {
                sonidoError.Play();
                monedasGanadas = 0;
            }

            monedaUIManager.AgregarMonedas(monedasGanadas);

            // 9️⃣ Ocultar botones y restaurar objetos
            panelBotones.SetActive(false);
            RestaurarObjetos();

            // 🔄 10️⃣ Girar cámara a posición original
            yield return StartCoroutine(RotarCamara180());
        }

        Debug.Log("Juego finalizado");
    }

    private IEnumerator Contador(float tiempo)
    {
        contadorTMP.gameObject.SetActive(true);
        contadorTMP.transform.SetAsLastSibling();

        float tiempoRestante = tiempo;
        while (tiempoRestante > 0)
        {
            contadorTMP.text = Mathf.CeilToInt(tiempoRestante).ToString();
            tiempoRestante -= Time.deltaTime;
            yield return null;
        }

        contadorTMP.gameObject.SetActive(false);
    }

    private IEnumerator MoverCamara(Vector3 destino, Quaternion rotacionDestino)
    {
        Vector3 inicioPos = camaraPrincipal.position;
        Quaternion inicioRot = camaraPrincipal.rotation;
        float tiempo = 0;

        while (tiempo < duracionMovimientoCamara)
        {
            camaraPrincipal.position = Vector3.Lerp(inicioPos, destino, tiempo / duracionMovimientoCamara);
            camaraPrincipal.rotation = Quaternion.Slerp(inicioRot, rotacionDestino, tiempo / duracionMovimientoCamara);
            tiempo += Time.deltaTime;
            yield return null;
        }

        camaraPrincipal.position = destino;
        camaraPrincipal.rotation = rotacionDestino;
    }

    private IEnumerator RotarCamara180()
    {
        Quaternion inicioRot = camaraPrincipal.rotation;
        Quaternion destinoRot = inicioRot * Quaternion.Euler(0, 180, 0);
        float tiempo = 0;

        while (tiempo < duracionMovimientoCamara)
        {
            camaraPrincipal.rotation = Quaternion.Slerp(inicioRot, destinoRot, tiempo / duracionMovimientoCamara);
            tiempo += Time.deltaTime;
            yield return null;
        }

        camaraPrincipal.rotation = destinoRot;
    }

    private void EliminarObjetosAleatorios(int cantidad)
    {
        objetosEliminados.Clear();
        List<BotonObjeto> copia = new List<BotonObjeto>(botonesObjetos);

        for (int i = 0; i < cantidad; i++)
        {
            if (copia.Count == 0) break;

            var par = copia[Random.Range(0, copia.Count)];
            par.objetoEscena.SetActive(false);
            objetosEliminados.Add(par.objetoEscena);
            copia.Remove(par);
        }
    }

    private void RestaurarObjetos()
    {
        foreach (GameObject obj in objetosEliminados)
        {
            if (obj != null)
                obj.SetActive(true);
        }
    }
}
