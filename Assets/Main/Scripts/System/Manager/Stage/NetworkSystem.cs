using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using Photon.Realtime;
using ProjectDM.UI;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class NetworkSystem : MonoBehaviourPunCallbacks
{
    public static class Keys
    {
        public const string IsSpawned = "IsSpawned";
    }

    private bool isPlaying = true;
    private Dictionary<int, bool> playerReadyStatus = new Dictionary<int, bool>();

    private MultiStatusPanel multiStatusPanel;
    private MultiWaitingPanel multiWaitingPanel;

    public bool IsInitialized { get; private set; } = false;

    public void StartMultiplayer()
    {
        multiStatusPanel =
            StageUIManager.Instance.OpenPanel(PanelType.MultiStatus) as MultiStatusPanel;
        multiStatusPanel.Close(true);
        multiWaitingPanel =
            StageUIManager.Instance.OpenPanel(PanelType.MultiWaiting) as MultiWaitingPanel;
        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.ConnectUsingSettings();
        IsInitialized = true;
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("[NetworkSystem] | Joined Lobby");
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

        if (!isPlaying || !PhotonNetwork.IsConnectedAndReady)
            return;

        PhotonNetwork.JoinRandomRoom();
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        PhotonNetwork.CreateRoom(null, new RoomOptions { MaxPlayers = 2 });
    }

    public override void OnLeftRoom()
    {
        if (!isPlaying || !PhotonNetwork.IsConnectedAndReady)
            return;

        PhotonNetwork.JoinRandomRoom();
    }

    public override void OnJoinedRoom()
    {
        playerReadyStatus.Clear();

        Debug.Log("[NetworkSystem] Room joined: " + PhotonNetwork.CurrentRoom.Name);
        if (PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(multiWaitingPanel.OnSearchFailed());
        }
        else
        {
            StartCoroutine(multiWaitingPanel.OnRoomFound());
        }
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

    public void SetPlayerSpawned()
    {
        Hashtable props = new Hashtable { { Keys.IsSpawned, true } };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    public bool AreAllPlayersSpawned()
    {
        return PhotonNetwork.PlayerList.All(p =>
            p.CustomProperties.ContainsKey(Keys.IsSpawned)
            && (bool)p.CustomProperties[Keys.IsSpawned]
        );
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (changedProps.ContainsKey(Keys.IsSpawned))
        {
            Debug.Log($"[NetworkSystem] Player {targetPlayer.ActorNumber} spawn status updated.");

            if (AreAllPlayersSpawned())
            {
                Debug.Log("[NetworkSystem] 모든 플레이어가 스폰 완료했습니다.");
                GameManager.Instance.AllPlayersSpawned();
            }
        }
    }

    private void OnDestroy()
    {
        isPlaying = false;
        PhotonNetwork.Disconnect();
    }
}
