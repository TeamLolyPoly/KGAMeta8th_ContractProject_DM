using System.Collections;
using Michsky.UI.Heat;
using Photon.Pun;
using ProjectDM.UI;
using TMPro;
using UnityEngine;

public class MultiTrackDecisionPanel : Panel
{
    public override PanelType PanelType => PanelType.Multi_TrackDecision;

    [SerializeField]
    private BoxButtonManager localTrackNameBox;

    [SerializeField]
    private BoxButtonManager remoteTrackNameBox;

    [SerializeField]
    private GameObject[] localTrackDifficultyBox;

    [SerializeField]
    private GameObject[] remoteTrackDifficultyBox;

    [SerializeField]
    private SlotMachineEffect slotEffect;

    public override void Open()
    {
        TrackData localTrack = GameManager.Instance.NetworkSystem.GetTrackData(
            PhotonNetwork.LocalPlayer
        );
        Difficulty localDifficulty = GameManager.Instance.NetworkSystem.GetDifficulty(
            PhotonNetwork.LocalPlayer
        );
        localTrackNameBox.SetText($"{localTrack.trackName}");
        localTrackDifficultyBox[(int)localDifficulty].SetActive(true);
        localTrackNameBox.SetBackground(localTrack.AlbumArt);
        foreach (var player in PhotonNetwork.PlayerList)
        {
            if (player.IsLocal)
                continue;

            TrackData remoteTrack = GameManager.Instance.NetworkSystem.GetTrackData(player);
            Difficulty remoteDifficulty = GameManager.Instance.NetworkSystem.GetDifficulty(player);
            remoteTrackNameBox.SetText($"{remoteTrack.trackName}");
            remoteTrackDifficultyBox[(int)remoteDifficulty].SetActive(true);
            remoteTrackNameBox.SetBackground(remoteTrack.AlbumArt);
        }
        base.Open();
    }

    public IEnumerator StartSpinning(bool isLocalTrackSelected)
    {
        slotEffect.StartSpinningWithResult(isLocalTrackSelected);
        yield return new WaitUntil(() => slotEffect.IsFinished);
        GameManager.Instance.NetworkSystem.SetPlayerReadyToLoadGame();
    }
}
