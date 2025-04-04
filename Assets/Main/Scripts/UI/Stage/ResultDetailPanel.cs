using System.Collections;
using System.Collections.Generic;
using Michsky.UI.Heat;
using ProjectDM;
using ProjectDM.UI;
using UnityEngine;

public class ResultDetailPanel : Panel
{
    public override PanelType PanelType => PanelType.ResultDetail;

    [SerializeField]
    private ButtonManager roomButton;

    [SerializeField]
    private ButtonManager robyButton;

    private void OnBackButtonClick()
    {
        UIManager.Instance.OpenPanel(PanelType.Result);
    }
}
