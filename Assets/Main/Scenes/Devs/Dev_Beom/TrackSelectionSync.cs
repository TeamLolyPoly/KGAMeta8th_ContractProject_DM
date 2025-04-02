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

    private const string TRACK_DATA_KEY = "SelectedTrack";

    /// <summary>
    /// 트랙 정보를 JSON으로 변환해 CustomProperties에 저장
    /// </summary>
    public void SetSelectedTrack(TrackData track)
    {
        string json = JsonUtility.ToJson(track);
        Hashtable props = new Hashtable { { TRACK_DATA_KEY, json } };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    /// <summary>
    /// 특정 플레이어가 선택한 트랙을 가져옴
    /// </summary>
    public TrackData GetTrackData(Player player)
    {
        if (player.CustomProperties.TryGetValue(TRACK_DATA_KEY, out object jsonObj))
        {
            string json = jsonObj as string;
            if (!string.IsNullOrEmpty(json))
            {
                return JsonUtility.FromJson<TrackData>(json);
            }
        }
        return null;
    }

    /// <summary>
    /// 모든 플레이어의 선택된 트랙 데이터를 가져옴
    /// </summary>
    public void BroadcastAllTrackSelections()
    {
        foreach (var player in PhotonNetwork.PlayerList)
        {
            var track = GetTrackData(player);
            OnTrackUpdated?.Invoke(player.ActorNumber, track);
        }
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (changedProps.ContainsKey(TRACK_DATA_KEY))
        {
            var track = GetTrackData(targetPlayer);
            OnTrackUpdated?.Invoke(targetPlayer.ActorNumber, track);
        }
    }
}
