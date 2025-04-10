using System.Collections;
using System.Collections.Generic;
using ProjectDM.UI;
using TMPro;
using UnityEngine;

public class MultiStatusPanel : Panel
{
    public override PanelType PanelType => PanelType.MultiStatus;

    public TextMeshProUGUI statusText;

    public void SetStatus(string status)
    {
        statusText.text = status;
    }

    public override void Open()
    {
        base.Open();
    }
}
