using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SimonGame : MonoBehaviour
{
    [Header("Pantalla")]
    public GameObject pantalla;
    public Renderer pantallaRenderer;

    [Header("Colores y sonidos")]
    public Color[] coloresDisponibles;
    public AudioClip[] sonidosColores;

    [Header("Botones del jugador")]
    public Button[] botonesUI;

    [Header("Duraciones")]
    public float delayInicio = 2f;
    public float duracionColor = 1f;
    public float delayEntreRondas = 1f;

    [Header("Nivel de dificultad")]
    [Range(1, 5)] public int nivel = 1;

    [Header("Sonidos generales")]
    public AudioSource audioSource;
    public AudioClip sonidoAcierto;
    public AudioClip sonidoFallo;

    [Header("Feedback monedas")]
    public GameObject feedbackMonedas;

    private List<int> secuencia = new List<int>();
    private List<int> entradaJugador = new List<int>();
    private int rondaActual = 1;
    private bool esperandoRespuesta = false;

    IEnumerator Start()
    {
        pantalla.SetActive(false);
        foreach (var b in botonesUI) b.gameObject.SetActive(false);

        if (feedbackMonedas != null)
            feedbackMonedas.SetActive(false);

        bool _started = false;
        var introCanvas = IntroPanel.Build(
            "Simon Dice",
            "Memoria",
            "Observa los colores que aparecen en pantalla.\n" +
            "Cuando sea tu turno, pulsa los botones en el mismo orden.\n" +
            "La secuencia se hace mas larga con cada ronda. Concentracion!",
            () => _started = true);

        while (!_started)
        {
            if (Input.GetKeyDown(KeyCode.Space)) _started = true;
            yield return null;
        }
        Object.Destroy(introCanvas);

        StartCoroutine(IniciarJuego());
    }

    IEnumerator IniciarJuego()
    {
        yield return new WaitForSeconds(delayInicio);
        pantalla.SetActive(true);

        secuencia.Clear();
        for (int i = 0; i < nivel; i++)
        {
            secuencia.Add(Random.Range(0, coloresDisponibles.Length));
        }

        rondaActual = 1;
        yield return MostrarRonda();
    }

    IEnumerator MostrarRonda()
    {
        entradaJugador.Clear();
        esperandoRespuesta = false;

        for (int i = 0; i < rondaActual; i++)
        {
            int colorIndex = secuencia[i];

            pantallaRenderer.material.color = coloresDisponibles[colorIndex];

            if (colorIndex >= 0 && colorIndex < sonidosColores.Length)
            {
                audioSource.PlayOneShot(sonidosColores[colorIndex]);
            }

            yield return new WaitForSeconds(duracionColor);

        }

        pantallaRenderer.material.color = Color.black;

        foreach (var b in botonesUI) b.gameObject.SetActive(true);
        esperandoRespuesta = true;
    }

    public void BotonJugadorPresionado(int id)
    {
        if (!esperandoRespuesta) return;

        if (id >= 0 && id < sonidosColores.Length)
        {
            audioSource.PlayOneShot(sonidosColores[id]);
        }

        entradaJugador.Add(id);
        int actual = entradaJugador.Count - 1;

        if (entradaJugador[actual] != secuencia[actual])
        {

            esperandoRespuesta = false;
            audioSource.PlayOneShot(sonidoFallo);

            foreach (var b in botonesUI) b.gameObject.SetActive(false);

            Debug.Log("❌ ¡Fallaste! Reiniciando ronda " + rondaActual);
            StartCoroutine(ReiniciarRondaConDelay());
            return;
        }

        if (entradaJugador.Count == rondaActual)
        {
            esperandoRespuesta = false;

            if (CoinManager2.instance != null)
            {
                CoinManager2.instance.AddCoins(2);
            }

            if (feedbackMonedas != null)
            {
                StartCoroutine(MostrarFeedbackMonedas());
            }

            StartCoroutine(ReproducirAciertoDespuesDeSonido());

            foreach (var b in botonesUI) b.gameObject.SetActive(false);

            if (rondaActual == nivel)
            {
                Debug.Log("✅ ¡Completaste el nivel!");
                return;
            }

            rondaActual++;
            StartCoroutine(SiguienteRondaConDelay());
        }
    }

    IEnumerator MostrarFeedbackMonedas()
    {
        feedbackMonedas.SetActive(true);
        yield return new WaitForSeconds(1.5f);
        feedbackMonedas.SetActive(false);
    }

    IEnumerator ReiniciarRondaConDelay()
    {
        yield return new WaitForSeconds(delayEntreRondas);
        yield return MostrarRonda();
    }

    IEnumerator ReproducirAciertoDespuesDeSonido()
    {
        yield return new WaitForSeconds(0.5f);
        audioSource.PlayOneShot(sonidoAcierto);
    }

    IEnumerator SiguienteRondaConDelay()
    {
        yield return new WaitForSeconds(delayEntreRondas);
        yield return MostrarRonda();
    }
}
