using UnityEngine;
using TMPro; // Use UnityEngine.UI instead if you're not using TextMeshPro

public class Timer : MonoBehaviour
{
    public TextMeshProUGUI timerText; // Assign this in the inspector
    private float elapsedTime = 0f;

    void Update()
    {
        elapsedTime += Time.deltaTime;

        int minutes = Mathf.FloorToInt(elapsedTime / 60f);
        int seconds = Mathf.FloorToInt(elapsedTime % 60f);
        int milliseconds = Mathf.FloorToInt((elapsedTime * 100f) % 100);

        timerText.text = string.Format("{0:00}:{1:00}.{2:00}", minutes, seconds, milliseconds);
    }
}
