using System;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class TrackSelectionSync : MonoBehaviourPunCallbacks
{
    public static TrackSelectionSync Instance;
    public event Action<int, TrackData, Difficulty> OnTrackUpdated;
    public event Action<TrackData, Difficulty> OnFinalTrackSelected;

    private const string TRACK_GUID_KEY = "SelectedTrackGUID";
    private const string TRACK_DIFF_KEY = "SelectedTrackDiff";
    private const string FINAL_TRACK_GUID_KEY = "FinalTrackGUID";
    private const string FINAL_TRACK_DIFF_KEY = "FinalTrackDiff";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SelectTrack(Guid trackGuid, Difficulty difficulty)
    {
        string guidStr = trackGuid.ToString();
        int diffInt = (int)difficulty;

        Hashtable props = new Hashtable
        {
            { TRACK_GUID_KEY, guidStr },
            { TRACK_DIFF_KEY, diffInt },
        };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);

        photonView.RPC(nameof(RPCSelectTrack), RpcTarget.Others, guidStr, diffInt);
    }

    [PunRPC]
    private void RPCSelectTrack(string guidStr, int diffInt, PhotonMessageInfo info)
    {
        if (Guid.TryParse(guidStr, out Guid trackGuid))
        {
            TrackData track = DataManager.Instance.TrackDataList.Find(t => t.id == trackGuid);
            if (track != null)
            {
                Difficulty difficulty = (Difficulty)diffInt;
                OnTrackUpdated?.Invoke(info.Sender.ActorNumber, track, difficulty);
            }
        }
    }

    public TrackData GetTrackData(Player player)
    {
        if (player.CustomProperties.TryGetValue(TRACK_GUID_KEY, out object guidObj))
        {
            if (guidObj is string guidStr && Guid.TryParse(guidStr, out Guid trackGuid))
            {
                return DataManager.Instance.TrackDataList.Find(t => t.id == trackGuid);
            }
        }
        return null;
    }

    public Difficulty GetDifficulty(Player player)
    {
        if (player.CustomProperties.TryGetValue(TRACK_DIFF_KEY, out object diffObj))
        {
            if (diffObj is int diffInt)
            {
                return (Difficulty)diffInt;
            }
        }
        return Difficulty.Easy;
    }

    public void DecideRandomFinalTrack()
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        var players = PhotonNetwork.PlayerList;
        if (players.Length < 2)
            return;

        int randomIndex = UnityEngine.Random.Range(0, players.Length);
        Player chosen = players[randomIndex];

        TrackData chosenTrack = GetTrackData(chosen);
        Difficulty chosenDiff = GetDifficulty(chosen);

        if (chosenTrack == null)
            return;

        Hashtable roomProps = new Hashtable
        {
            { FINAL_TRACK_GUID_KEY, chosenTrack.id.ToString() },
            { FINAL_TRACK_DIFF_KEY, (int)chosenDiff },
        };

        PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (changedProps.ContainsKey(TRACK_GUID_KEY) || changedProps.ContainsKey(TRACK_DIFF_KEY))
        {
            var track = GetTrackData(targetPlayer);
            var diff = GetDifficulty(targetPlayer);
            if (track != null)
                OnTrackUpdated?.Invoke(targetPlayer.ActorNumber, track, diff);
        }
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        if (
            propertiesThatChanged.ContainsKey(FINAL_TRACK_GUID_KEY)
            && propertiesThatChanged.ContainsKey(FINAL_TRACK_DIFF_KEY)
        )
        {
            string guidStr = (string)propertiesThatChanged[FINAL_TRACK_GUID_KEY];
            int diffInt = (int)propertiesThatChanged[FINAL_TRACK_DIFF_KEY];

            if (Guid.TryParse(guidStr, out Guid guid))
            {
                TrackData track = DataManager.Instance.TrackDataList.Find(t => t.id == guid);
                Difficulty diff = (Difficulty)diffInt;
                OnFinalTrackSelected?.Invoke(track, diff);
            }
        }
    }
}
