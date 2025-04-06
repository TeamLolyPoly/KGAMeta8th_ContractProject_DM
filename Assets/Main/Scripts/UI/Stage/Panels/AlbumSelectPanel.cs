using Michsky.UI.Heat;
using ProjectDM.UI;
using UnityEngine;

public class AlbumSelectPanel : Panel
{
    public override PanelType PanelType => PanelType.AlbumSelect;

    [SerializeField]
    private CarouselMenu carouselMenu;

    [SerializeField]
    private ButtonManager closeButton;

    public override void Open()
    {
        base.Open();
        Initialize();
    }

    public override void Close(bool objActive = false)
    {
        base.Close(objActive);
        carouselMenu.CleanUp();
    }

    private void Initialize()
    {
        carouselMenu.Initialize(DataManager.Instance.AlbumDataList);
        closeButton.onClick.AddListener(OnCloseButtonClick);
    }

    private void OnCloseButtonClick()
    {
        Close(true);
        StageUIManager.Instance.OpenPanel(PanelType.Mode);
    }
}
