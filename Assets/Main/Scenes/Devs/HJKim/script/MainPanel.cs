using System.Collections;
using System.Collections.Generic;
using Michsky.UI.Heat;
using ProjectDM.UI;
using UnityEngine;
using UnityEngine.UI;

public class MainPanel : Panel
{
    public override PanelType PanelType => PanelType.Main;

    [SerializeField]
    private ButtonManager StartButton;

    [SerializeField]
    private ButtonManager OptionButton;

    public override void Open()
    {
        StartButton.onClick.AddListener(OnStartButtonClick);
        OptionButton.onClick.AddListener(OnOptionButtonClick);
        base.Open();
    }

    public override void Close()
    {
        StartButton.onClick.RemoveListener(OnStartButtonClick);
        OptionButton.onClick.RemoveListener(OnOptionButtonClick);
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
