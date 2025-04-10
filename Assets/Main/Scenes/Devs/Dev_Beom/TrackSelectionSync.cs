using System;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class TrackSelectionSync : MonoBehaviourPunCallbacks
{
    public static TrackSelectionSync Instance;

    public event Action<int, TrackData> OnTrackUpdated;

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

    private const string TRACK_GUID_KEY = "SelectedTrackGUID";

    public void SelectTrack(Guid trackGuid)
    {
        string guidString = trackGuid.ToString();

        Hashtable props = new Hashtable { { TRACK_GUID_KEY, guidString } };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);

        photonView.RPC(nameof(RPCSelectTrack), RpcTarget.Others, guidString);
    }

    [PunRPC]
    private void RPCSelectTrack(string guidStr, PhotonMessageInfo info)
    {
        if (Guid.TryParse(guidStr, out Guid trackGuid))
        {
            TrackData track = DataManager.Instance.TrackDataList.Find(t => t.id == trackGuid);
            if (track != null)
            {
                OnTrackUpdated?.Invoke(info.Sender.ActorNumber, track);
            }
            else
            {
                Debug.LogWarning($"[TrackSelectionSync] GUID에 해당하는 트랙을 찾을 수 없습니다: {trackGuid}");
            }
        }
    }

    public void BroadcastAllTrackSelections()
    {
        foreach (var player in PhotonNetwork.PlayerList)
        {
            var track = GetTrackData(player);
            OnTrackUpdated?.Invoke(player.ActorNumber, track);
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

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (changedProps.ContainsKey(TRACK_GUID_KEY))
        {
            var track = GetTrackData(targetPlayer);
            OnTrackUpdated?.Invoke(targetPlayer.ActorNumber, track);
        }
    }
}
