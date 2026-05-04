using UnityEngine;

public class CoinPickup : MonoBehaviour
{
    public AudioClip pickupSound;
    public GameObject audioSourcePrefab;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {

            CoinManager.instance.AddCoin();

            if (pickupSound != null && audioSourcePrefab != null)
            {
                GameObject audioObj = Instantiate(audioSourcePrefab, transform.position, Quaternion.identity);
                AudioSource source = audioObj.GetComponent<AudioSource>();
                if (source != null)
                {
                    source.clip = pickupSound;
                    source.Play();
                    Destroy(audioObj, pickupSound.length + 0.1f);
                }
                else
                {
                    Debug.LogWarning("AudioSource no encontrado en el prefab AudioSourcePrefab");
                }
            }
            else
            {
                Debug.LogWarning("pickupSound o audioSourcePrefab no asignados");
            }

            Destroy(gameObject);
        }
    }
}
