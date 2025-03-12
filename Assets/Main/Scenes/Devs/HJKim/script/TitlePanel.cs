using System.Collections;
using System.Collections.Generic;
using Michsky.UI.Heat;
using ProjectDM.UI;
using UnityEngine;
using UnityEngine.UI;

public class TitlePanel : Panel
{
    public override PanelType PanelType => PanelType.Title;

    [SerializeField]
    private PanelButton startButton;

    [SerializeField]
    private PanelButton settingButton;

    public override void Open()
    {
        startButton.onClick.AddListener(OnStartButtonClick);
        settingButton.onClick.AddListener(OnOptionButtonClick);
        base.Open();
    }

    public override void Close()
    {
        startButton.onClick.RemoveListener(OnStartButtonClick);
        settingButton.onClick.RemoveListener(OnOptionButtonClick);
        base.Close();
    }

    private void OnStartButtonClick()
    {
        //mode 패널 열기
        UIManager.Instance.OpenPanel(PanelType.Mode);
    }

    private void OnOptionButtonClick()
    {
        //option 패널 열기
        UIManager.Instance.OpenPanel(PanelType.Option);
    }
}
