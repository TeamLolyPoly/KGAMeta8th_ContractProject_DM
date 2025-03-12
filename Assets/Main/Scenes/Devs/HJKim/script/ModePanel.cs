using Michsky.UI.Heat;
using ProjectDM;
using ProjectDM.UI;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class ModePanel : Panel
{
    public override PanelType PanelType => PanelType.Mode;

    [SerializeField]
    private BoxButtonManager singleButton;

    [SerializeField]
    private BoxButtonManager multiButton;

    [SerializeField]
    private PanelButton backButton;

    public override void Open()
    {
        backButton.onClick.AddListener(OnBackButtonClick);
        base.Open();
    }

    public override void Close()
    {
        backButton.onClick.RemoveListener(OnBackButtonClick);
        base.Close();
    }

    private void OnBackButtonClick()
    {
        UIManager.Instance.OpenPanel(PanelType.Title);
    }
}
