using ProjectDM.UI;
using TMPro;

public class MultiStatusPanel : Panel
{
    public override PanelType PanelType => PanelType.Multi_Status;

    public TextMeshProUGUI statusText;

    public void SetStatus(string status)
    {
        statusText.text = status;
    }

    public override void Open()
    {
        base.Open();
    }
}
