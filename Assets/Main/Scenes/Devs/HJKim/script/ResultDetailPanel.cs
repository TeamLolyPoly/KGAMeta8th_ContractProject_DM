using System.Collections;
using System.Collections.Generic;
using Michsky.UI.Heat;
using ProjectDM;
using ProjectDM.UI;
using UnityEngine;

public class ResultDetailPanel : Panel
{
    public override PanelType PanelType => PanelType.ResultDetail;

    public override void Open()
    {
        backButton.onClick.AddListener(OnBackButtonClick);
        base.Open();
    }

    public override void Close()
    {
        backButton.onClick.RemoveListener(OnBackButtonClick);
        base.Close();
    }

    [SerializeField]
    private PanelButton backButton;

    private void OnBackButtonClick()
    {
        UIManager.Instance.OpenPanel(PanelType.Result);
    }
}
