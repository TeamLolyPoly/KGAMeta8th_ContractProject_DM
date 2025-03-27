using System.Collections;
using System.Collections.Generic;
using Michsky.UI.Heat;
using ProjectDM.UI;
using UnityEngine;

public class MusicPanel : Panel
{
    public override PanelType PanelType => PanelType.Music;

    [SerializeField]
    private BoxButtonManager backButton;

    [SerializeField]
    private BoxButtonManager albumButton;

    [SerializeField]
    private BoxButtonManager SampleButton;

    // todo: 곡들을 어떤식으로 담을지 고민해야함 + 버튼 클릭 시 곡 재생,앨범사진도 바뀌게? 아니면 아티스트 대표사진만 노출?

    public override void Open()
    {
        backButton.onClick.AddListener(OnBackButtonClick);
        albumButton.onClick.AddListener(OnAlbumButtonClick);
        SampleButton.onClick.AddListener(OnSampleButtonClick);
        base.Open();
    }

    public override void Close(bool objActive = false)
    {
        backButton.onClick.RemoveListener(OnBackButtonClick);
        albumButton.onClick.RemoveListener(OnAlbumButtonClick);
        SampleButton.onClick.RemoveListener(OnSampleButtonClick);
        base.Close(objActive);
    }

    private void OnBackButtonClick()
    {
        UIManager.Instance.OpenPanel(PanelType.Album);
        UIManager.Instance.ClosePanel(PanelType.Music);
    }

    private void OnAlbumButtonClick()
    {
        // TODO: 앨범 정보 표시
    }

    private void OnSampleButtonClick()
    {
        UIManager.Instance.OpenPanel(PanelType.Difficult);
        UIManager.Instance.ClosePanel(PanelType.Music);
    }
}
