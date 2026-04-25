using UnityEngine;
using TMPro;

public class Timer : MonoBehaviour
{
    public TextMeshProUGUI timerText;
    float time;

    void Update()
    {
        time += Time.deltaTime;
        timerText.text = "Tiempo: " + time.ToString("F1");
    }
}