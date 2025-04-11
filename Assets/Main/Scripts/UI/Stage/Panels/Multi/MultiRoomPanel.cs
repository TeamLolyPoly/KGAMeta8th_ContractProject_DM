using System.Collections;
using System.Collections.Generic;
using Michsky.UI.Heat;
using Photon.Pun;
using Photon.Realtime;
using ProjectDM.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MultiRoomPanel : Panel
{
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
    private Animator TrackSelectButtonAnimator;

    [SerializeField]
    private ButtonManager ReadyButton;

    [SerializeField]
    private Animator ReadyButtonAnimator;

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

    private NetworkSystem networkSystem;
    private TrackData localSelectedTrack;

    private bool isTrackSelected = false;

    public override void Open()
    {
        base.Open();

        networkSystem = GameManager.Instance.NetworkSystem;

        if (networkSystem != null)
        {
            networkSystem.SetMultiRoomPanel(this);
            networkSystem.OnTrackUpdated += OnTrackUpdated;
            networkSystem.OnRemotePlayerJoined += OnRemotePlayerJoined;
            networkSystem.OnRemotePlayerLeft += OnRemotePlayerLeft;
            networkSystem.OnPlayerReadyStatusChanged += OnPlayerReadyStatusChanged;
        }

        TrackSelectButton.onClick.AddListener(OnTrackSelectButtonClicked);
        ReadyButton.onClick.AddListener(OnReadyButtonClicked);
        CloseButton.onClick.AddListener(OnCloseButtonClicked);
    }

    private void OnTrackSelectButtonClicked()
    {
        Close(true);
        StageUIManager.Instance.OpenPanel(PanelType.AlbumSelect);
    }

    private void OnReadyButtonClicked()
    {
        if (localSelectedTrack == null)
        {
            Debug.Log("Please select a track first");
            return;
        }

        networkSystem.OnReadyButtonClicked();

        ReadyButton.Interactable(false);
        TrackSelectButton.Interactable(false);
    }

    private void OnCloseButtonClicked()
    {
        if (networkSystem != null)
        {
            networkSystem.LeaveGame();
        }

        Close(true);
        StageUIManager.Instance.OpenPanel(PanelType.Title);
    }

    private void OnLocalTrackSelected(TrackData track, Difficulty difficulty)
    {
        print($"OnLocalTrackSelected: {track.trackName} , {difficulty}");
        localSelectedTrack = track;

        LocalPlayerTrackNameText.text = $"{track.artistName} - {track.trackName}";
        LocalTrackNameBox.SetActive(true);
        LocalTrackDifficultyBox.SetActive(true);

        for (int i = 0; i < LocalPlayerSelectedDifficulty.Length; i++)
        {
            LocalPlayerSelectedDifficulty[i].SetActive(i == (int)difficulty);
        }
        TrackSelectButtonAnimator.SetBool("subOpen", false);
        TrackSelectButton.gameObject.SetActive(false);
        ReadyButton.gameObject.SetActive(true);
        ReadyButtonAnimator.SetBool("subOpen", true);

        networkSystem.SelectTrack(track.id, difficulty);

        Open();
    }

    private void OnTrackUpdated(int actorNumber, TrackData track, Difficulty difficulty)
    {
        if (actorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
            return;

        OnRemotePlayerTrackSelected(track, difficulty);
    }

    public void OnRemotePlayerTrackSelected(TrackData track, Difficulty difficulty)
    {
        RemotePlayerTrackText.text = track.trackName;
        RemoteTrackNameBox.SetActive(true);
        RemoteTrackDifficultyBox.SetActive(true);
        RemotePlayerSelectingBox.SetActive(false);
        RemotePlayerOnReadyBox.SetActive(true);

        for (int i = 0; i < RemotePlayerSelectedDifficulty.Length; i++)
        {
            RemotePlayerSelectedDifficulty[i].SetActive(i == (int)difficulty);
        }
    }

    private void OnPlayerReadyStatusChanged(Player player)
    {
        if (player.ActorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
            return;

        OnRemotePlayerReady();
    }

    public void OnRemotePlayerReady()
    {
        RemotePlayerSelectingBox.SetActive(false);
        RemotePlayerOnReadyBox.SetActive(true);
    }

    public void InitializeHost()
    {
        LocalPlayerBox.SetBool("subOpen", true);
        WaitingRemotePlayerBox.SetBool("subOpen", true);
        RemotePlayerBox.SetBool("subOpen", false);
        LocalTrackNameBox.SetActive(false);
        LocalTrackDifficultyBox.SetActive(false);
        PlayerButtonBox.SetActive(false);
        TrackSelectButton.gameObject.SetActive(true);
    }

    public void InitializeClient()
    {
        LocalPlayerBox.SetBool("subOpen", true);
        RemotePlayerBox.SetBool("subOpen", true);
        PlayerButtonBox.SetActive(true);
        TrackSelectButton.gameObject.SetActive(true);
        TrackSelectButtonAnimator.SetBool("subOpen", true);
    }

    public void OnRemotePlayerJoined()
    {
        WaitingRemotePlayerBox.SetBool("subOpen", false);
        RemotePlayerBox.SetBool("subOpen", true);
        RemotePlayerSelectingBox.SetActive(true);
        PlayerButtonBox.SetActive(true);
        TrackSelectButton.gameObject.SetActive(true);
        TrackSelectButtonAnimator.SetBool("subOpen", true);
    }

    public void OnRemotePlayerLeft()
    {
        RemotePlayerBox.SetBool("subOpen", false);
        RemotePlayerSelectingBox.SetActive(false);
        RemotePlayerOnReadyBox.SetActive(false);
        RemoteTrackNameBox.SetActive(false);
        RemoteTrackDifficultyBox.SetActive(false);
        WaitingRemotePlayerBox.SetBool("subOpen", true);
        TrackSelectButtonAnimator.SetBool("subOpen", false);
        TrackSelectButton.gameObject.SetActive(false);
        ReadyButtonAnimator.SetBool("subOpen", false);
        ReadyButton.gameObject.SetActive(false);
    }

    public override void Close(bool objActive = true)
    {
        if (networkSystem != null)
        {
            networkSystem.OnTrackUpdated -= OnTrackUpdated;
            networkSystem.OnRemotePlayerJoined -= OnRemotePlayerJoined;
            networkSystem.OnRemotePlayerLeft -= OnRemotePlayerLeft;
            networkSystem.OnPlayerReadyStatusChanged -= OnPlayerReadyStatusChanged;
        }

        TrackSelectButton.onClick.RemoveAllListeners();
        ReadyButton.onClick.RemoveAllListeners();
        CloseButton.onClick.RemoveAllListeners();

        localSelectedTrack = null;

        base.Close(objActive);
    }
}
