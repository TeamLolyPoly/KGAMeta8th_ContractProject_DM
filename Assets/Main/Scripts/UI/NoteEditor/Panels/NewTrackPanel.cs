using System.Collections;
using Michsky.UI.Heat;
using ProjectDM.UI;

public class NewTrackPanel : Panel
{
    public override PanelType PanelType => PanelType.NewTrack;

    public PanelButton BackButton;

    public override void Open()
    {
        base.Open();
        BackButton.onClick.AddListener(OnBackButtonClick);
    }

    public override void Close()
    {
        BackButton.onClick.RemoveListener(OnBackButtonClick);
        base.Close();
    }

    private void OnBackButtonClick()
    {
        UIManager.Instance.OpenPanel(PanelType.EditorStart);
        Close();
    }
}
