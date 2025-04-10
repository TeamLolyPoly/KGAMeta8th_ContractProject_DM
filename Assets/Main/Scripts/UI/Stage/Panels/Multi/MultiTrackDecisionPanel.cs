using Michsky.UI.Heat;
using Photon.Pun;
using ProjectDM.UI;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools.Constraints;

public class MultiTrackDecisionPanel : Panel
{
    public override PanelType PanelType => PanelType.Multi_TrackDecision;

    [SerializeField]
    private BoxButtonManager localTrackNameBox;

    [SerializeField]
    private BoxButtonManager remoteTrackNameBox;

    [SerializeField]
    private TextMeshProUGUI localTrackNameText;

    [SerializeField]
    private TextMeshProUGUI remoteTrackNameText;

    [SerializeField]
    private GameObject[] localTrackDifficultyBox;

    [SerializeField]
    private GameObject[] remoteTrackDifficultyBox;

    [SerializeField]
    private SlotMachineEffect slotEffect;

    public override void Open()
    {
        base.Open();
        TrackData localTrack = GameManager.Instance.NetworkSystem.GetTrackData(
            PhotonNetwork.LocalPlayer
        );
        Difficulty localDifficulty = GameManager.Instance.NetworkSystem.GetDifficulty(
            PhotonNetwork.LocalPlayer
        );
        localTrackNameBox.SetText($"{localTrack.trackName}");
        localTrackDifficultyBox[(int)localDifficulty].SetActive(true);
        localTrackNameText.text = $"{localTrack.trackName}";
        localTrackNameBox.SetBackground(localTrack.AlbumArt);
        foreach (var player in PhotonNetwork.PlayerList)
        {
            if (player.IsLocal)
                continue;

            TrackData remoteTrack = GameManager.Instance.NetworkSystem.GetTrackData(player);
            Difficulty remoteDifficulty = GameManager.Instance.NetworkSystem.GetDifficulty(player);
            remoteTrackNameBox.SetText($"{remoteTrack.trackName}");
            remoteTrackDifficultyBox[(int)remoteDifficulty].SetActive(true);
            remoteTrackNameText.text = $"{remoteTrack.trackName}";
            remoteTrackNameBox.SetBackground(remoteTrack.AlbumArt);
        }
    }

    public void StartSpinning(bool isLocalTrackSelected)
    {
        slotEffect.StartSpinningWithResult(isLocalTrackSelected);
    }
}
