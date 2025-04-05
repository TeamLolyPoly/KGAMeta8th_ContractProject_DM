using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LogBox : MonoBehaviour
{
    public TextMeshProUGUI logText;

    public void Initialize(string log, LogType type)
    {
        string color = GetLogTypeColor(type);
        logText.text = $"{color}{log}</color>";
    }

    private string GetLogTypeColor(LogType type)
    {
        switch (type)
        {
            case LogType.Error:
                return "<color=red>";
            case LogType.Assert:
                return "<color=red>";
            case LogType.Warning:
                return "<color=yellow>";
            case LogType.Log:
                return "<color=white>";
            case LogType.Exception:
                return "<color=#FF00FF>";
            default:
                return "<color=white>";
        }
    }
}
