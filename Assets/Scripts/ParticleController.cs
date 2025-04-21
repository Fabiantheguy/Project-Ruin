using UnityEngine;
public class ParticleController : MonoBehaviour
{
    [Header("References")]
    public Transform playerTransform;                    // Reference to player transform
    public Rigidbody playerRigidbody;                    // Reference to player Rigidbody for velocity
    public ParticleSystem windParticles;                 // The stylized wind ParticleSystem

    [Header("Speed Settings")]
    public float minSpeedThreshold = 5f;                 // Speed at which wind begins
    public float maxSpeedThreshold = 20f;                // Speed at which wind is at full intensity

    [Header("Particle Settings")]
    public float minEmissionRate = 0f;                   // Emission rate at lowest speed
    public float maxEmissionRate = 50f;                  // Emission rate at highest speed
    public float minStartSpeed = 1f;                     // Particle speed at lowest player speed
    public float maxStartSpeed = 10f;                    // Particle speed at highest player speed
    public float minLifetime = 0.2f;                     // Shortest wind trail at low speed
    public float maxLifetime = 1.2f;                     // Longest wind trail at high speed

    private ParticleSystem.EmissionModule emission;
    private ParticleSystem.MainModule main;

    void Start()
    {
        if (windParticles == null || playerRigidbody == null)
        {
            Debug.LogWarning("ParticleController is missing required references.");
            return;
        }

        emission = windParticles.emission;
        main = windParticles.main;
    }

    void Update()
    {
        if (windParticles == null || playerRigidbody == null) return;

        float speed = playerRigidbody.linearVelocity.magnitude;

        // Enable or disable particles based on threshold
        if (speed > minSpeedThreshold)
        {
            if (!windParticles.isPlaying)
                windParticles.Play();

            float t = Mathf.InverseLerp(minSpeedThreshold, maxSpeedThreshold, speed);

            // Adjust particle settings based on speed
            emission.rateOverTime = Mathf.Lerp(minEmissionRate, maxEmissionRate, t);
            main.startSpeed = Mathf.Lerp(minStartSpeed, maxStartSpeed, t);
            main.startLifetime = Mathf.Lerp(minLifetime, maxLifetime, t);
        }
        else
        {
            if (windParticles.isPlaying)
                windParticles.Stop();
        }
    }
}
