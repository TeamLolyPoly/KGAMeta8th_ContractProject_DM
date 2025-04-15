using Michsky.UI.Heat;
using Photon.Pun;
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
        if (PhotonNetwork.IsConnected)
        {
            StageUIManager.Instance.OpenPanel(PanelType.Multi_Room);
        }
        else
        {
            StageUIManager.Instance.OpenPanel(PanelType.Mode);
        }
    }
}
