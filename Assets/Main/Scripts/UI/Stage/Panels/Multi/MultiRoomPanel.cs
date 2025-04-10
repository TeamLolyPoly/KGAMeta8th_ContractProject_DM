using System.Collections;
using System.Collections.Generic;
using Michsky.UI.Heat;
using Photon.Pun;
using ProjectDM.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MultiRoomPanel : Panel
{
    public enum Difficulty
    {
        Easy,
        Normal,
        Hard,
    }

    public override PanelType PanelType => PanelType.Multi_Room;

    [Header("============Common============")]
    [SerializeField]
    private BoxButtonManager CloseButton;

    [Header("============Local Player============")]
    [SerializeField]
    private Animator LocalPlayerBox;

    [SerializeField]
    private GameObject LocalTrackNameBox;

    [SerializeField]
    private GameObject LocalTrackDifficultyBox;

    [SerializeField]
    private TextMeshProUGUI LocalPlayerTrackNameText;

    [SerializeField]
    public GameObject[] LocalPlayerSelectedDifficulty;

    [SerializeField]
    private GameObject PlayerButtonBox;

    [SerializeField]
    private ButtonManager TrackSelectButton;

    [SerializeField]
    private ButtonManager ReadyButton;

    [Header("============Remote Player============")]
    [SerializeField]
    private Animator WaitingRemotePlayerBox;

    [SerializeField]
    private Animator RemotePlayerBox;

    [SerializeField]
    private GameObject RemoteTrackNameBox;

    [SerializeField]
    private GameObject RemoteTrackDifficultyBox;

    [SerializeField]
    public GameObject[] RemotePlayerSelectedDifficulty;

    [SerializeField]
    private GameObject RemotePlayerSelectingBox;

    [SerializeField]
    private GameObject RemotePlayerOnReadyBox;

    [SerializeField]
    private TextMeshProUGUI RemotePlayerTrackText;

    public override void Open()
    {
        base.Open();
    }

    public override void Close(bool objActive = true)
    {
        base.Close(objActive);
    }
}
