using System.Collections;
using System.Collections.Generic;
using Michsky.UI.Heat;
using ProjectDM.UI;
using UnityEngine;

public class OptionPanel : Panel
{
    public override PanelType PanelType => PanelType.Option;

    [SerializeField]
    private PanelButton backButton;

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

    private void OnBackButtonClick()
    {
        UIManager.Instance.OpenPanel(PanelType.Title);
    }
}
