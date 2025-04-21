using UnityEngine;

public class DayNightCycle : MonoBehaviour
{
    [Header("Cycle Settings")]
    public float dayDurationInMinutes = 1f;
    [Range(0f, 1f)]
    public float timeOfDay = 0f;

    [Header("Sun Settings")]
    public Light sunLight;
    public GameObject sunObject;
    public Transform player; // Add this!
    public float sunDistance = 50f;
    public float sunInitialAngle = -90f;

    [Header("Lighting Dynamics")]
    public AnimationCurve sunIntensityCurve;
    public Gradient sunColorOverTime;

    void Update()
    {
        sunLight.shadowBias = 0.05f;
        sunLight.shadowNormalBias = 0.4f;

        float daySpeed = 1f / (dayDurationInMinutes * 60f);
        timeOfDay += Time.deltaTime * daySpeed;
        if (timeOfDay > 1f) timeOfDay -= 1f;

        UpdateSunPositionAndLighting();
    }

    void UpdateSunPositionAndLighting()
    {
        // Calculate sun angle
        float sunAngle = Mathf.Lerp(sunInitialAngle, 270f, timeOfDay);

        // Set the light's rotation to match the sun's path
        sunLight.transform.rotation = Quaternion.Euler(sunAngle, 0f, 0f);

        // Place the visual sun object opposite the light's direction
        if (sunObject != null && player != null)
        {
            // Get direction the light is shining from (opposite of forward)
            Vector3 sunDirection = -sunLight.transform.forward;

            // Place sun object far away in that direction
            sunObject.transform.position = player.position + sunDirection * sunDistance;

            // Optionally rotate the sun sphere to always face the player
            sunObject.transform.LookAt(player.position);
        }

        // Update lighting and ambient color
        sunLight.intensity = sunIntensityCurve.Evaluate(timeOfDay);
        sunLight.color = sunColorOverTime.Evaluate(timeOfDay);
        RenderSettings.ambientLight = sunLight.color * 0.5f;
    }
}
