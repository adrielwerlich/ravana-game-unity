using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Clock : MonoBehaviour
{
    private TextMeshProUGUI timeText;
    void Start()
    {
        timeText = this.transform.Find("TimeText").GetComponent<TextMeshProUGUI>();
        this.UpdateTime();
    }

    void UpdateTime()
    {
        int hours = Mathf.FloorToInt(DayLightCycle.Instance.GetHour());
        string formattedHours = hours < 10 ? "0" + hours.ToString() : hours.ToString();
        timeText.text = "Time: " + formattedHours + ":00";
    }

    void Update()
    {
        this.UpdateTime();

    }
}
