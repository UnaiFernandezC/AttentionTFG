using UnityEngine;

/// <summary>
/// Cronómetro oculto para "Cuenta Atrás Silenciosa".
/// El tiempo transcurre internamente pero NO se expone a la UI durante la cuenta.
/// </summary>
public class SilentCountdownTimerManager : MonoBehaviour
{
    // ------------------------------------------------------------------ //
    // Estado
    // ------------------------------------------------------------------ //
    public float ElapsedTime { get; private set; } = 0f;
    public bool  IsRunning   { get; private set; } = false;

    // ------------------------------------------------------------------ //
    // API pública
    // ------------------------------------------------------------------ //

    /// <summary>Arranca el cronómetro desde cero.</summary>
    public void StartCounting()
    {
        ElapsedTime = 0f;
        IsRunning   = true;
    }

    /// <summary>Detiene el cronómetro y devuelve el tiempo transcurrido.</summary>
    public float StopCounting()
    {
        IsRunning = false;
        return ElapsedTime;
    }

    /// <summary>Resetea sin detener.</summary>
    public void Reset()
    {
        ElapsedTime = 0f;
    }

    // ------------------------------------------------------------------ //
    // Update
    // ------------------------------------------------------------------ //
    void Update()
    {
        if (IsRunning)
            ElapsedTime += Time.deltaTime;
    }
}
