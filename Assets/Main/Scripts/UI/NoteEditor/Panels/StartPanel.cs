using Michsky.UI.Heat;
using ProjectDM.UI;

public class EditorStartPanel : Panel
{
    public override PanelType PanelType => PanelType.EditorStart;
    public PanelButton NewTrackButton;
    public PanelButton LoadTrackButton;

    public override void Open()
    {
        base.Open();
        NewTrackButton.onClick.AddListener(OnNewTrackButtonClick);
        LoadTrackButton.onClick.AddListener(OnLoadTrackButtonClick);
    }

    public override void Close()
    {
        NewTrackButton.onClick.RemoveListener(OnNewTrackButtonClick);
        LoadTrackButton.onClick.RemoveListener(OnLoadTrackButtonClick);
        base.Close();
    }

    private void OnNewTrackButtonClick()
    {
        UIManager.Instance.OpenPanel(PanelType.NewTrack);
        Close();
    }

    private void OnLoadTrackButtonClick()
    {
        UIManager.Instance.OpenPanel(PanelType.LoadTrack);
        Close();
    }
}
