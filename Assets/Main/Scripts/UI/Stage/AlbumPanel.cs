using Michsky.UI.Heat;
using ProjectDM.UI;
using UnityEngine;

public class AlbumPanel : Panel
{
    public override PanelType PanelType => PanelType.Album;

    public override void Open()
    {
        base.Open();
        Initialize();
    }

    //addListener 할 때 함수로 빼고 난 뒤에 open함수 호출하는 방식으로 변경해야함.
    public void Initialize()
    {
        backButton.onClick.AddListener(OnBackButtonClick);
        laftTogle.onClick.AddListener(OnLeftToggleClick);
        rightTogle.onClick.AddListener(OnRightToggleClick);
        selectAlbum.onClick.AddListener(OnSelectAlbumClick);
        leftAlbum.onClick.AddListener(OnLeftAlbumClick);
        rightAlbum.onClick.AddListener(OnRightAlbumClick);
    }

    public override void Close()
    {
        backButton.onClick.RemoveListener(OnBackButtonClick);
        laftTogle.onClick.RemoveListener(OnLeftToggleClick);
        rightTogle.onClick.RemoveListener(OnRightToggleClick);
        selectAlbum.onClick.RemoveListener(OnSelectAlbumClick);
        leftAlbum.onClick.RemoveListener(OnLeftAlbumClick);
        rightAlbum.onClick.RemoveListener(OnRightAlbumClick);
        base.Close();
    }

    [SerializeField]
    private PanelButton backButton;

    [SerializeField]
    private PanelButton laftTogle;

    [SerializeField]
    private PanelButton rightTogle;

    [SerializeField]
    private BoxButtonManager selectAlbum;

    [SerializeField]
    private BoxButtonManager leftAlbum;

    [SerializeField]
    private BoxButtonManager rightAlbum;

    //todo:  앨범 위치 바뀌면서 이동하는 기능을 가진 스크롤을 만들어야함 좌우 버튼 클릭과 양옆 작은 앨범도 누르면 중앙으로 이동해야함 곡을 선택하려면 무조건 중앙으로 위치하도록 해야함.

    private void OnBackButtonClick()
    {
        UIManager.Instance.OpenPanel(PanelType.Mode);
        UIManager.Instance.ClosePanel(PanelType.Album);
    }

    private void OnLeftToggleClick()
    {
        // TODO: 왼쪽으로 앨범 이동
    }

    private void OnRightToggleClick()
    {
        // TODO: 오른쪽으로 앨범 이동
    }

    private void OnSelectAlbumClick()
    {
        UIManager.Instance.OpenPanel(PanelType.Music);
        UIManager.Instance.ClosePanel(PanelType.Album);
    }

    private void OnLeftAlbumClick()
    {
        // TODO: 왼쪽 앨범을 중앙으로 이동
    }

    private void OnRightAlbumClick()
    {
        // TODO: 오른쪽 앨범을 중앙으로 이동
    }
}
