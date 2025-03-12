using System.Collections;
using System.Collections.Generic;
using Michsky.UI.Heat;
using ProjectDM;
using ProjectDM.UI;
using UnityEngine;

public class AlbumPanel : Panel
{
    public override PanelType PanelType => PanelType.Album;

    public override void Open()
    {
        base.Open();
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
}
