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
    private ShopButtonManager startButton;

    [SerializeField]
    private ShopButtonManager settingButton;

    [SerializeField]
    private ShopButtonManager quitButton;

    public override void Open()
    {
        startButton.onClick.AddListener(OnStartButtonClick);
        settingButton.onClick.AddListener(OnOptionButtonClick);
        quitButton.onClick.AddListener(OnQuitButtonClick);
        base.Open();
    }

    public override void Close(bool objActive = false)
    {
        startButton.onClick.RemoveListener(OnStartButtonClick);
        settingButton.onClick.RemoveListener(OnOptionButtonClick);
        quitButton.onClick.RemoveListener(OnQuitButtonClick);
        base.Close(objActive);
    }

    private void OnStartButtonClick()
    {
        UIManager.Instance.OpenPanel(PanelType.Mode);
        Close(false);
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
