using UnityEngine;
using TMPro;

public class Timer : MonoBehaviour
{
    public TextMeshProUGUI timerText;         // Assign in Inspector
    public Collider colliderA;                // First collider to watch
    public Collider colliderB;                // Second collider to watch

    private float elapsedTime = 0f;
    private bool timerRunning = true;

    void Update()
    {
        if (!timerRunning)
        {
            timerText.color = Color.green; // Set text color to green
            return;
        }

        elapsedTime += Time.deltaTime;

        int minutes = Mathf.FloorToInt(elapsedTime / 60f);
        int seconds = Mathf.FloorToInt(elapsedTime % 60f);
        int milliseconds = Mathf.FloorToInt((elapsedTime * 100f) % 100);

        timerText.text = string.Format("{0:00}:{1:00}.{2:00}", minutes, seconds, milliseconds);
    }

    void FixedUpdate()
    {
        // Check if the two colliders are currently touching
        if (timerRunning && colliderA != null && colliderB != null)
        {
            if (colliderA.bounds.Intersects(colliderB.bounds))
            {
                timerRunning = false;
            }
        }
    }
}
