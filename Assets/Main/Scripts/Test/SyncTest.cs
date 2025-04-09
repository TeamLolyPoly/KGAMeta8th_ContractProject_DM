using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class SyncTest : MonoBehaviourPunCallbacks
{
    public readonly string LOCAL_PLAYER_PREFAB_NAME = "Prefabs/Stage/Player/MultiPlayer";

    [Header("스폰 위치")]
    public Vector3 masterSpawnPos = new Vector3(0, 0, 0);
    public Vector3 clientSpawnPos = new Vector3(3, 0, 0);

    private void Awake()
    {
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("[SyncTest] | Connected to Master");
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("[SyncTest] | Joined Lobby");
        CreateOrJoinMultiplayerRoom();
    }

    private void CreateOrJoinMultiplayerRoom()
    {
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
            return;
        }

        if (!PhotonNetwork.IsConnectedAndReady)
            return;

        PhotonNetwork.JoinRandomRoom();
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log("[SyncTest] | Failed to join random room, creating new room...");
        PhotonNetwork.CreateRoom(null, new RoomOptions { MaxPlayers = 2 });
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("[SyncTest] | Room joined: " + PhotonNetwork.CurrentRoom.Name);

        Vector3 spawnPos = PhotonNetwork.IsMasterClient ? masterSpawnPos : clientSpawnPos;

        PhotonNetwork.Instantiate(LOCAL_PLAYER_PREFAB_NAME, spawnPos, Quaternion.identity);

        SpawnRemotePlayersForOthers();
    }

    private void SpawnRemotePlayersForOthers()
    {
        foreach (var player in PhotonNetwork.PlayerListOthers)
        {
            Debug.Log($"[SyncTest] Already existing remote player: {player.NickName}");
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"[SyncTest] Player joined: {newPlayer.NickName}");
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log("[SyncTest] Player left: " + otherPlayer.NickName);
    }
}
