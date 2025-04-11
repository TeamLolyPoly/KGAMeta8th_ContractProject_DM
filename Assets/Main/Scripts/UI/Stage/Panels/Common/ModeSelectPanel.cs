using Michsky.UI.Heat;
using ProjectDM.UI;
using UnityEngine;

public class ModeSelectPanel : Panel
{
    public override PanelType PanelType => PanelType.Mode;

    [SerializeField]
    private BoxButtonManager singleModeButton;

    [SerializeField]
    private BoxButtonManager multiModeButton;

    [SerializeField]
    private ButtonManager closeButton;

    public override void Open()
    {
        closeButton.onClick.AddListener(OnCloseButtonClick);
        singleModeButton.onClick.AddListener(OnSingleModeButtonClick);
        multiModeButton.onClick.AddListener(OnMultiModeButtonClick);
        base.Open();
    }

    public override void Close(bool objActive = false)
    {
        closeButton.onClick.RemoveListener(OnCloseButtonClick);
        singleModeButton.onClick.RemoveListener(OnSingleModeButtonClick);
        multiModeButton.onClick.RemoveListener(OnMultiModeButtonClick);
        base.Close(objActive);
    }

    private void OnCloseButtonClick()
    {
        Close(true);
        StageUIManager.Instance.OpenPanel(PanelType.Title);
    }

    private void OnSingleModeButtonClick()
    {
        Close(true);
        StageUIManager.Instance.OpenPanel(PanelType.AlbumSelect);
    }

    private void OnMultiModeButtonClick()
    {
        Close(true);
        GameManager.Instance.StartMultiplayer();
    }
}
