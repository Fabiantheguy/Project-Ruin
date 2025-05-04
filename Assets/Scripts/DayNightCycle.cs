using UnityEngine;

public class DayNightCycle : MonoBehaviour
{
    [Header("Cycle Settings")]
    public float sunCycleMinutes = 1f;
    public float moonCycleMinutes = 1f;
    [Range(0f, 1f)] public float sunTimeOfDay = 0f;
    [Range(0f, 1f)] public float moonTimeOfDay = 0f;

    [Header("Sun Settings")]
    public Light sunLight;
    public GameObject sunObject;
    public Transform player;
    public float sunDistance = 30000f;
    public float sunInitialAngle = -90f;
    public Gradient sunColorOverTime;

    [Header("Moon Settings")]
    public Light moonLight;
    public GameObject moonObject;
    public float moonDistance = -3000f;
    public float moonInitialAngle = 90f;
    public Gradient moonColorOverTime;

    void Update()
    {
        float sunSpeed = 1f / (sunCycleMinutes * 60f);
        float moonSpeed = 1f / (moonCycleMinutes * 60f);

        sunTimeOfDay += Time.deltaTime * sunSpeed;
        if (sunTimeOfDay > 1f) sunTimeOfDay -= 1f;

        moonTimeOfDay += Time.deltaTime * moonSpeed;
        if (moonTimeOfDay > 1f) moonTimeOfDay -= 1f;

        UpdateSunPositionAndLighting();
        UpdateMoonPositionAndLighting();
    }

    void UpdateSunPositionAndLighting()
    {
        if (sunObject != null && player != null)
        {
            float sunAngle = Mathf.Lerp(sunInitialAngle, sunInitialAngle + 360f, sunTimeOfDay);
            Quaternion sunRotation = Quaternion.Euler(sunAngle, 0f, 0f);
            Vector3 sunOffset = sunRotation * Vector3.forward * sunDistance;
            Vector3 sunPosition = player.position + sunOffset;

            sunObject.transform.position = sunPosition;
            sunObject.transform.LookAt(player.position);

            // Set the light to shine toward the player
            Vector3 sunDirection = (player.position - sunPosition).normalized;
            sunLight.transform.rotation = Quaternion.LookRotation(sunDirection);

            sunLight.color = sunColorOverTime.Evaluate(sunTimeOfDay);
            RenderSettings.ambientLight = sunLight.color * 0.5f;
        }
    }

    void UpdateMoonPositionAndLighting()
    {
        if (moonObject != null && player != null)
        {
            float moonAngle = Mathf.Lerp(moonInitialAngle, moonInitialAngle + 360f, moonTimeOfDay);
            Quaternion moonRotation = Quaternion.Euler(moonAngle, 0f, 0f);
            Vector3 moonOffset = moonRotation * Vector3.forward * moonDistance;
            Vector3 moonPosition = player.position + moonOffset;

            moonObject.transform.position = moonPosition;
            moonObject.transform.LookAt(player.position);

            // Set the light to shine toward the player
            Vector3 moonDirection = (player.position - moonPosition).normalized;
            moonLight.transform.rotation = Quaternion.LookRotation(moonDirection);

            moonLight.color = moonColorOverTime.Evaluate(moonTimeOfDay);
        }
    }
}
