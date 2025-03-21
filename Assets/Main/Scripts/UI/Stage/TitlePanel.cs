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

    [SerializeField]
    private PanelButton quitButton;

    public override void Open()
    {
        startButton.onClick.AddListener(OnStartButtonClick);
        settingButton.onClick.AddListener(OnOptionButtonClick);
        quitButton.onClick.AddListener(OnQuitButtonClick);
        base.Open();
    }

    public override void Close()
    {
        startButton.onClick.RemoveListener(OnStartButtonClick);
        settingButton.onClick.RemoveListener(OnOptionButtonClick);
        quitButton.onClick.RemoveListener(OnQuitButtonClick);
        base.Close();
    }

    private void OnStartButtonClick()
    {
        UIManager.Instance.OpenPanel(PanelType.Mode);
        Close();
    }

    private void OnOptionButtonClick()
    {
        UIManager.Instance.OpenPanel(PanelType.Option);
    }

    private void OnQuitButtonClick()
    {
        Application.Quit();
    }
}
