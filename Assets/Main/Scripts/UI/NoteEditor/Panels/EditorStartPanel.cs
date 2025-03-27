using Michsky.UI.Heat;
using ProjectDM.UI;

public class EditorStartPanel : Panel
{
    public override PanelType PanelType => PanelType.EditorStart;
    public ButtonManager NewTrackButton;
    public ButtonManager LoadTrackButton;

    public override void Open()
    {
        base.Open();
        NewTrackButton.onClick.AddListener(OnNewTrackButtonClick);
        LoadTrackButton.onClick.AddListener(OnLoadTrackButtonClick);
    }

    public override void Close(bool objActive = false)
    {
        NewTrackButton.onClick.RemoveListener(OnNewTrackButtonClick);
        LoadTrackButton.onClick.RemoveListener(OnLoadTrackButtonClick);
        base.Close(objActive);
    }

    private void OnNewTrackButtonClick()
    {
        UIManager.Instance.OpenPanel(PanelType.NewTrack);
        Close(true);
    }

    private void OnLoadTrackButtonClick()
    {
        UIManager.Instance.OpenPanel(PanelType.LoadTrack);
        Close(true);
    }
}
