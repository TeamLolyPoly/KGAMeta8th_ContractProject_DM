using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class SyncTest : MonoBehaviourPunCallbacks
{
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

    public void CreateOrJoinMultiplayerRoom()
    {
        Debug.Log("[NetworkSystem] Multiplayer Start");

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
        PhotonNetwork.CreateRoom(null, new RoomOptions { MaxPlayers = 2 });
    }
    public override void OnJoinedRoom()
    {
        Debug.Log("[SyncTest] Room joined: " + PhotonNetwork.CurrentRoom.Name);

        Vector3 spawnPos = PhotonNetwork.IsMasterClient ? new Vector3(0, 0, 0) : new Vector3(3, 0, 0);
        PhotonNetwork.Instantiate("Player", spawnPos, Quaternion.identity);
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log("[SyncTest] Player joined: " + newPlayer.NickName);
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log("[SyncTest] Player left: " + otherPlayer.NickName);
    }
}