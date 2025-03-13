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
        singleButton.onClick.AddListener(OnSingleButtonClick);
        multiButton.onClick.AddListener(OnMultiButtonClick);
        base.Open();
    }

    public override void Close()
    {
        backButton.onClick.RemoveListener(OnBackButtonClick);
        singleButton.onClick.RemoveListener(OnSingleButtonClick);
        multiButton.onClick.RemoveListener(OnMultiButtonClick);
        base.Close();
    }

    private void OnBackButtonClick()
    {
        UIManager.Instance.OpenPanel(PanelType.Title);
    }

    private void OnSingleButtonClick()
    {
        UIManager.Instance.OpenPanel(PanelType.Album);
    }

    private void OnMultiButtonClick()
    {
        //todo : 멀티모드 구현해야함  기획서 참고!
    }
}
