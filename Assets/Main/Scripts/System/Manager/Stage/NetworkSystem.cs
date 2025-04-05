using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using Random = UnityEngine.Random;

public class NetworkSystem : MonoBehaviourPunCallbacks, IInitializable
{
    private bool isMultiplayer = false;
    private bool isPlaying = true;
    private Dictionary<int, bool> playerReadyStatus = new Dictionary<int, bool>();

    public bool IsInitialized { get; private set; } = false;

    public void Initialize()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.ConnectUsingSettings();
        IsInitialized = true;
    }

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("[NetworkSystem] | Joined Lobby | Single Player Start");
        CreateSinglePlayerRoom();
    }

    public void CreateSinglePlayerRoom()
    {
        isMultiplayer = false;
        RoomOptions options = new RoomOptions { MaxPlayers = 1 };
        PhotonNetwork.CreateRoom("SingleRoom_" + Random.Range(0, 10000), options);
    }

    public void CreateOrJoinMultiplayerRoom()
    {
        isMultiplayer = true;

        Debug.Log("[NetworkSystem] Multiplayer Start");

        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
            return;
        }
    }

    public override void OnLeftRoom()
    {
        if (!isPlaying || !PhotonNetwork.IsConnectedAndReady)
            return;

        if (isMultiplayer)
        {
            PhotonNetwork.JoinRandomRoom();
        }
        else
        {
            PhotonNetwork.CreateRoom(
                "SingleRoom_" + Random.Range(0, 10000),
                new RoomOptions { MaxPlayers = 1 }
            );
        }
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        PhotonNetwork.CreateRoom(null, new RoomOptions { MaxPlayers = 2 });
    }

    public override void OnJoinedRoom()
    {
        playerReadyStatus.Clear();

        Debug.Log("[NetworkSystem] Room joined: " + PhotonNetwork.CurrentRoom.Name);

        // if (!isMultiplayer)
        // {
        //     LoadMultiplayerScene("GameScene");
        //     return;
        // }

        // if (PhotonNetwork.CurrentRoom.PlayerCount == 2 && PhotonNetwork.IsMasterClient)
        // {
        //     PhotonNetwork.AutomaticallySyncScene = true;
        //     LoadMultiplayerScene("GameScene");
        // }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log("[NetworkSystem] Player joined: " + newPlayer.NickName);
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        playerReadyStatus.Remove(otherPlayer.ActorNumber);
        Debug.Log("[NetworkSystem] Player left: " + otherPlayer.NickName);
    }

    private Transform GetSpawnPoint()
    {
        // 간단한 예: Photon Player ID로 두 개 위치 고정
        // GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("SpawnPoint");

        // if (spawnPoints.Length >= PhotonNetwork.CurrentRoom.PlayerCount)
        // {
        //     int index = PhotonNetwork.LocalPlayer.ActorNumber % spawnPoints.Length;
        //     return spawnPoints[index].transform;
        // }
        return transform;
    }

    public void OnReadyButtonClicked()
    {
        if (PhotonNetwork.InRoom)
        {
            photonView.RPC("SetPlayerReady", RpcTarget.MasterClient);
        }
    }

    [PunRPC]
    public void SetPlayerReady()
    {
        int playerId = PhotonNetwork.LocalPlayer.ActorNumber;
        playerReadyStatus[playerId] = true;

        if (PhotonNetwork.IsMasterClient && AreAllPlayersReady())
        {
            photonView.RPC("StartGame", RpcTarget.All);
        }
    }

    private bool AreAllPlayersReady()
    {
        return playerReadyStatus.Count == PhotonNetwork.CurrentRoom.PlayerCount
            && playerReadyStatus.All(status => status.Value);
    }

    [PunRPC]
    public void StartGame()
    {
        // GameManager.Instance.StartGame();
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning($"[NetworkSystem] Server disconnected: {cause}");
        StartCoroutine(ReconnectRoutine());
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"[NetworkSystem] Room creation failed: {message}");
    }

    private IEnumerator ReconnectRoutine()
    {
        int maxAttempts = 10;
        int attempt = 0;
        while (attempt < maxAttempts || PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Reconnect();
            attempt++;
            yield return new WaitForSeconds(5f);
        }
    }

    public void LeaveGame()
    {
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
        }
        else
        {
            PhotonNetwork.Disconnect();
        }
    }

    public void LoadSceneMaster(
        string sceneName,
        List<Func<IEnumerator>> operations,
        Action onComplete = null
    )
    {
        if (PhotonNetwork.IsMasterClient)
        {
            StageLoadingManager.Instance.LoadScene(
                sceneName,
                operations,
                () =>
                {
                    onComplete?.Invoke();
                }
            );

            photonView.RPC("LoadSceneClient", RpcTarget.Others, sceneName, operations, onComplete);
        }
    }

    [PunRPC]
    private void LoadSceneClient(
        string sceneName,
        List<Func<IEnumerator>> operations,
        Action onComplete
    )
    {
        StageLoadingManager.Instance.LoadScene(sceneName, operations, onComplete);
    }

    private void OnDestroy()
    {
        isPlaying = false;
        PhotonNetwork.Disconnect();
    }
}
