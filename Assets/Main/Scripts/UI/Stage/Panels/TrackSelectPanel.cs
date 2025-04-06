using System.Collections;
using System.Collections.Generic;
using Michsky.UI.Heat;
using ProjectDM.UI;
using UnityEngine;

public class TrackSelectPanel : Panel
{
    public override PanelType PanelType => PanelType.TrackSelect;

    private TrackData selectedTrack;
    private NoteMap selectedNoteMap;

    [SerializeField]
    private ButtonManager closeButton;

    [SerializeField]
    private ShopButtonManager trackButtonPrefab;

    [SerializeField]
    private Transform trackParent;

    [SerializeField]
    private Animator trackInfoPanel;

    [SerializeField]
    private BoxButtonManager openRankButton;

    [SerializeField]
    private ButtonManager easyButton;

    [SerializeField]
    private ButtonManager normalButton;

    [SerializeField]
    private ButtonManager hardButton;

    [SerializeField]
    private ButtonManager playButton;

    [SerializeField]
    private Animator rankPanel;

    [SerializeField]
    private ButtonManager rankCloseButton;

    [SerializeField]
    private RankBox rankBoxPrefab;

    private List<BoxButtonManager> trackButtons = new List<BoxButtonManager>();

    public override void Open()
    {
        base.Open();
    }
}
