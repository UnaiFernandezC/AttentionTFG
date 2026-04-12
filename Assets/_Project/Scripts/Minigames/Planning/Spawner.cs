using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class Spawner : MonoBehaviour
{
    public GameObject objetoA;
    public GameObject objetoB;
    public Transform puntoSpawn;

    public Collider colliderA;
    public Collider colliderB;

    public TMP_Text textoEstrellas;
    public float tiempoEntreSpawns = 1.5f;

    public AudioClip sonidoAcierto; // 🔊 Sonido cuando acierta

    private AudioSource audioSource;

    private int estrellas = 0;
    private int totalObjetos = 10;

    void Start()
    {
        // Configurar AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        ActualizarTexto();
        StartCoroutine(SpawnAleatorio());
    }

    IEnumerator SpawnAleatorio()
    {
        List<GameObject> listaObjetos = new List<GameObject>();

        // Agregar 5 de cada objeto
        for (int i = 0; i < 5; i++)
        {
            listaObjetos.Add(objetoA);
            listaObjetos.Add(objetoB);
        }

        // Mezclar lista aleatoriamente (Fisher-Yates shuffle)
        for (int i = listaObjetos.Count - 1; i > 0; i--)
        {
            int rand = Random.Range(0, i + 1);
            (listaObjetos[i], listaObjetos[rand]) = (listaObjetos[rand], listaObjetos[i]);
        }

        // Spawnear de uno en uno
        foreach (var prefab in listaObjetos)
        {
            Spawn(prefab);
            yield return new WaitForSeconds(tiempoEntreSpawns);
        }
    }

    void Spawn(GameObject prefab)
    {
        Vector3 posicion = puntoSpawn.position + Vector3.up * 1.0f;

        GameObject instancia = Instantiate(prefab, posicion, Quaternion.identity);

        Rigidbody rb = instancia.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = instancia.AddComponent<Rigidbody>();
        }

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.useGravity = true;
        rb.isKinematic = false;
        rb.constraints = RigidbodyConstraints.None;

        Collider colliderObjetivo = (prefab == objetoA) ? colliderA : colliderB;
        colliderObjetivo.isTrigger = true;

        DetectorColision detector = instancia.AddComponent<DetectorColision>();
        detector.Inicializar(this, colliderObjetivo);
    }

    public void SumarEstrella()
    {
        estrellas++;
        estrellas = Mathf.Clamp(estrellas, 0, totalObjetos);
        ActualizarTexto();

        // 🔊 Reproducir sonido de acierto
        if (sonidoAcierto != null && audioSource != null)
        {
            audioSource.PlayOneShot(sonidoAcierto);
        }
    }

    void ActualizarTexto()
    {
        textoEstrellas.text = estrellas + " / " + totalObjetos;
    }
}
