using Michsky.UI.Heat;
using ProjectDM.UI;
using UnityEngine;

public class TitlePanel : Panel
{
    public override PanelType PanelType => PanelType.Title;

    [SerializeField]
    private ButtonManager titleStartButton;

    [SerializeField]
    private ButtonManager settingButton;

    [SerializeField]
    private ButtonManager quitButton;

    public override void Open()
    {
        titleStartButton.onClick.AddListener(OnTitleStartButtonClick);
        settingButton.onClick.AddListener(OnOptionButtonClick);
        quitButton.onClick.AddListener(OnQuitButtonClick);
        base.Open();
    }

    public override void Close(bool objActive = false)
    {
        titleStartButton.onClick.RemoveListener(OnTitleStartButtonClick);
        settingButton.onClick.RemoveListener(OnOptionButtonClick);
        quitButton.onClick.RemoveListener(OnQuitButtonClick);
        base.Close(objActive);
    }

    private void OnTitleStartButtonClick()
    {
        StageUIManager.Instance.OpenPanel(PanelType.Mode);
        Close(true);
    }

    private void OnOptionButtonClick() { }

    private void OnQuitButtonClick()
    {
        Application.Quit();
    }
}
