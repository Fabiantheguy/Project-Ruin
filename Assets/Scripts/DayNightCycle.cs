using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class DayNightCycle : MonoBehaviour
{
    [Header("Cycle Settings")]
    public float sunCycleMinutes = 1f;
    public float moonCycleMinutes = 1f;
    [Range(0f, 1f)] public float sunTimeOfDay = 0f;
    [Range(0f, 1f)] public float moonTimeOfDay = 0f;

    [Header("Sunlight Detection")]
    public Transform playerHead;
    public LayerMask sunlightBlockingLayers;
    public bool isInSunlight;

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

    [Header("Vignette Settings")]
    public Volume postProcessingVolume;
    public float vignetteSpeed = 2f;
    public float sunlightVignetteTarget = 0.4f;
    public float normalVignetteTarget = 0f;
    private Vignette vignette;

    [Header("Sky Color Settings")]
    public Camera mainCamera;
    public Color dayColor = Color.cyan;
    public Color nightColor = Color.black;

    void Start()
    {
        if (postProcessingVolume != null && postProcessingVolume.profile.TryGet(out Vignette v))
        {
            vignette = v;
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main; // Ensure the camera is assigned
        }
    }

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
        CheckSunlight();
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

            Vector3 moonDirection = (player.position - moonPosition).normalized;
            moonLight.transform.rotation = Quaternion.LookRotation(moonDirection);

            moonLight.color = moonColorOverTime.Evaluate(moonTimeOfDay);
        }
    }

    void CheckSunlight()
    {
        if (playerHead == null || sunLight == null) return;

        Vector3 sunDirection = -sunLight.transform.forward;
        Vector3 origin = playerHead.position - sunDirection * 1000f;
        Ray ray = new Ray(origin, sunDirection);

        if (Physics.Raycast(ray, out RaycastHit hit, 2000f, sunlightBlockingLayers))
        {
            isInSunlight = hit.transform == playerHead;
        }
        else
        {
            isInSunlight = true;
        }

        if (vignette != null)
        {
            float targetIntensity = isInSunlight ? sunlightVignetteTarget : normalVignetteTarget;
            vignette.intensity.value = Mathf.Lerp(vignette.intensity.value, targetIntensity, Time.deltaTime * vignetteSpeed);
        }

        UpdateBackgroundColor();
    }

    void UpdateBackgroundColor()
    {
        // Get the sun's and moon's vertical angle to determine if it's day or night
        float sunVerticalAngle = sunObject.transform.rotation.eulerAngles.x;
        float moonVerticalAngle = moonObject.transform.rotation.eulerAngles.x;

        // Transition based on the sun's and moon's vertical position
        if (sunVerticalAngle < 90f && moonVerticalAngle > 90f)
        {
            // Sun is higher than the moon, it's day time
            mainCamera.backgroundColor = Color.Lerp(mainCamera.backgroundColor, dayColor, Time.deltaTime);
        }
        else
        {
            // Moon is higher than the sun, it's night time
            mainCamera.backgroundColor = Color.Lerp(mainCamera.backgroundColor, nightColor, Time.deltaTime  );
        }
    }
}
