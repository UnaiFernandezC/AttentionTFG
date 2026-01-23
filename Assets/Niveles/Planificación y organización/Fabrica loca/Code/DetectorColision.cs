using UnityEngine;

public class DetectorColision : MonoBehaviour
{
    private Spawner spawner;
    private Collider colliderObjetivo;

    public void Inicializar(Spawner spawnerRef, Collider objetivo)
    {
        spawner = spawnerRef;
        colliderObjetivo = objetivo;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == colliderObjetivo)
        {
            spawner.SumarEstrella();
            Destroy(gameObject);
        }
    }
}
