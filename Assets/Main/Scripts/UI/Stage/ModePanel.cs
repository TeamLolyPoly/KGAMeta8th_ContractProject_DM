using Michsky.UI.Heat;
using ProjectDM.UI;
using UnityEngine;

public class ModePanel : Panel
{
    public override PanelType PanelType => PanelType.Mode;

    [SerializeField]
    private BoxButtonManager singleButton;

    [SerializeField]
    private BoxButtonManager multiButton;

    [SerializeField]
    private BoxButtonManager backButton;

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
        UIManager.Instance.ClosePanel(PanelType.Mode);
        UIManager.Instance.OpenPanel(PanelType.Title);
    }

    private void OnSingleButtonClick()
    {
        UIManager.Instance.ClosePanel(PanelType.Mode);
        UIManager.Instance.OpenPanel(PanelType.Music);
    }

    private void OnMultiButtonClick()
    {
        //todo : 멀티모드 구현해야함  기획서 참고! 일단 비활성화 처리
        multiButton.isInteractable = false;
        multiButton.UpdateUI();
    }
}
