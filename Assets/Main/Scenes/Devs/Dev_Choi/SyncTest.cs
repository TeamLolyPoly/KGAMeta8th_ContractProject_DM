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
        CreateOrJoinMultiplayerRoom();
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
        if (PhotonNetwork.LocalPlayer.IsMasterClient)
        {
            PhotonNetwork.Instantiate("Player", transform.position, transform.rotation);
        }
        if (!PhotonNetwork.LocalPlayer.IsMasterClient)
        {
            PhotonNetwork.Instantiate("Player", new Vector3(5, 0, 0), transform.rotation);
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log("[SyncTest] Player joined: " + newPlayer.NickName);
        PhotonNetwork.Instantiate("RemotePlayer", transform.position, transform.rotation);
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log("[SyncTest] Player left: " + otherPlayer.NickName);
    }
}
