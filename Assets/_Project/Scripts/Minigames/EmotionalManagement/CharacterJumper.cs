// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using UnityEngine;

public class CharacterJumper : MonoBehaviour
{
    [Header("Plataformas de salto")]
    public Transform[] jumpTargets;
    private int currentPlatformIndex = 0;

    [Header("Movimiento")]
    public float jumpSpeed = 1.5f;
    public float jumpHeight = 2f;
    public float landingYOffset = 0.5f;

    [Header("Animaci�n")]
    public Animator animator;

    public void JumpToNextPlatform()
    {
        if (jumpTargets == null || currentPlatformIndex >= jumpTargets.Length) return;

        Transform nextPlatform = jumpTargets[currentPlatformIndex];
        currentPlatformIndex++;
        if (nextPlatform == null) return;

        if (animator != null)
            animator.SetTrigger("Jump");

        StartCoroutine(JumpTo(nextPlatform.position));
    }

    System.Collections.IEnumerator JumpTo(Vector3 targetPosition)
    {
        Vector3 start = transform.position;
        Vector3 end = targetPosition + new Vector3(0, landingYOffset, 0);

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
