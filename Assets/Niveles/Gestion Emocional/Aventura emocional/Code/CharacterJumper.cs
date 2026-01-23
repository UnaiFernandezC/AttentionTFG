using UnityEngine;

public class CharacterJumper : MonoBehaviour
{
    [Header("Plataformas de salto")]
    public Transform[] jumpTargets;
    private int currentPlatformIndex = 0;

    [Header("Movimiento")]
    public float jumpSpeed = 1.5f;             // Reduce para que el salto sea más lento y fluido
    public float jumpHeight = 2f;              // Ajusta la altura de la parábola
    public float landingYOffset = 0.5f;        // Ajusta para que no se hunda en la nube

    [Header("Animación")]
    public Animator animator;

    public void JumpToNextPlatform()
    {
        if (currentPlatformIndex >= jumpTargets.Length) return;

        // Activar animación de salto
        if (animator != null)
            animator.SetTrigger("Jump");

        Transform nextPlatform = jumpTargets[currentPlatformIndex];
        StartCoroutine(JumpTo(nextPlatform.position));
        currentPlatformIndex++;
    }

    System.Collections.IEnumerator JumpTo(Vector3 targetPosition)
    {
        Vector3 start = transform.position;
        Vector3 end = targetPosition + new Vector3(0, landingYOffset, 0); // subir un poco el destino

        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * jumpSpeed;
            float curvedY = Mathf.Sin(Mathf.PI * t) * jumpHeight;
            Vector3 midPoint = Vector3.Lerp(start, end, t);
            transform.position = new Vector3(midPoint.x, midPoint.y + curvedY, midPoint.z);
            yield return null;
        }

        transform.position = end;
    }
}
