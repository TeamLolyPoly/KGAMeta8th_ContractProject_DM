using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class NetworkSystem : MonoBehaviourPunCallbacks
{
    public static class Keys
    {
        public const string IS_SPAWNED = "IsSpawned";
        public const string TRACK_GUID = "SelectedTrackGUID";
        public const string TRACK_DIFFICULTY = "SelectedTrackDiff";
        public const string FINAL_TRACK_GUID = "FinalTrackGUID";
        public const string FINAL_TRACK_DIFFICULTY = "FinalTrackDiff";
    }

    private bool isPlaying = true;
    public bool IsInitialized { get; private set; } = false;
    private Dictionary<int, bool> playerReadyStatus = new Dictionary<int, bool>();
    private MultiWaitingPanel multiWaitingPanel;
    private MultiRoomPanel multiRoomPanel;
    public event Action<int, TrackData, Difficulty> OnTrackUpdated;
    public event Action<TrackData, Difficulty> OnFinalTrackSelected;
    public event Action<Player> OnPlayerReadyStatusChanged;
    public event Action OnRemotePlayerJoined;
    public event Action OnRemotePlayerLeft;

    public void StartMultiplayer()
    {
        multiWaitingPanel =
            StageUIManager.Instance.OpenPanel(PanelType.Multi_Waiting) as MultiWaitingPanel;
        PhotonNetwork.ConnectUsingSettings();
        IsInitialized = true;
    }

    public override void OnJoinedLobby()
    {
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
        OnRemotePlayerJoined?.Invoke();

        if (PhotonNetwork.IsMasterClient && multiRoomPanel != null)
        {
            multiRoomPanel.OnRemotePlayerJoined();
        }
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        playerReadyStatus.Remove(otherPlayer.ActorNumber);
        Debug.Log("[NetworkSystem] Player left: " + otherPlayer.NickName);

        OnRemotePlayerLeft?.Invoke();

        if (PhotonNetwork.IsMasterClient && multiRoomPanel != null)
        {
            multiRoomPanel.OnRemotePlayerLeft();
        }
    }

    public void SelectTrack(Guid trackGuid, Difficulty difficulty)
    {
        string guidStr = trackGuid.ToString();
        int diffInt = (int)difficulty;

        Hashtable props = new Hashtable
        {
            { Keys.TRACK_GUID, guidStr },
            { Keys.TRACK_DIFFICULTY, diffInt },
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
        if (player.CustomProperties.TryGetValue(Keys.TRACK_GUID, out object guidObj))
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
        if (player.CustomProperties.TryGetValue(Keys.TRACK_DIFFICULTY, out object diffObj))
        {
            if (diffObj is int diffInt)
            {
                return (Difficulty)diffInt;
            }
        }
        return Difficulty.Easy;
    }

    public void DecideFinalTrack()
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

        bool isLocalTrackSelected = chosen == PhotonNetwork.LocalPlayer;

        if (chosenTrack == null)
            return;

        Hashtable roomProps = new Hashtable
        {
            { Keys.FINAL_TRACK_GUID, chosenTrack.id.ToString() },
            { Keys.FINAL_TRACK_DIFFICULTY, (int)chosenDiff },
        };

        PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);

        photonView.RPC(nameof(NotfyFinalTrack), RpcTarget.All, isLocalTrackSelected);
    }

    [PunRPC]
    private void NotfyFinalTrack(bool isLocalTrackSelected)
    {
        if (isLocalTrackSelected)
        {
            MultiTrackDecisionPanel multiTrackDecisionPanel =
                StageUIManager.Instance.OpenPanel(PanelType.Multi_TrackDecision)
                as MultiTrackDecisionPanel;
            multiTrackDecisionPanel.StartSpinning(isLocalTrackSelected);
        }
    }

    public void OnReadyButtonClicked()
    {
        if (PhotonNetwork.InRoom)
        {
            photonView.RPC(
                nameof(RPCPlayerReady),
                RpcTarget.Others,
                PhotonNetwork.LocalPlayer.ActorNumber
            );
        }
    }

    [PunRPC]
    private void RPCPlayerReady(int actorNumber)
    {
        Player player = PhotonNetwork.CurrentRoom.GetPlayer(actorNumber);
        if (player != null)
        {
            playerReadyStatus[actorNumber] = true;
            OnPlayerReadyStatusChanged?.Invoke(player);

            if (multiRoomPanel != null)
            {
                multiRoomPanel.OnRemotePlayerReady();
            }

            if (PhotonNetwork.IsMasterClient && AreAllPlayersReady())
            {
                DecideFinalTrack();
                photonView.RPC(nameof(StartGame), RpcTarget.All);
            }
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
        if (
            PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(
                Keys.FINAL_TRACK_GUID,
                out object guidObj
            )
            && PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(
                Keys.FINAL_TRACK_DIFFICULTY,
                out object diffObj
            )
        )
        {
            string guidStr = (string)guidObj;
            int diffInt = (int)diffObj;

            if (Guid.TryParse(guidStr, out Guid guid))
            {
                TrackData track = DataManager.Instance.TrackDataList.Find(t => t.id == guid);
                Difficulty diff = (Difficulty)diffInt;

                if (track != null)
                {
                    NoteMapData noteMapData = track.noteMapData.FirstOrDefault(n =>
                        n.difficulty == diff
                    );
                    if (noteMapData != null)
                    {
                        NoteMap noteMap = noteMapData.noteMap;
                        GameManager.Instance.StartMultiplayerGame(track, noteMap);
                    }
                }
            }
        }
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning($"[NetworkSystem] Server disconnected: {cause}");
        StageUIManager.Instance.CloseAllPanels();
        StageUIManager.Instance.OpenPopUp(
            "네트워크 오류",
            "서버와 연결이 끊어졌습니다. 다시 시도해주세요.",
            () =>
            {
                StageUIManager.Instance.OpenPanel(PanelType.Mode);
            }
        );
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"[NetworkSystem] Room creation failed: {message}");
        StageUIManager.Instance.CloseAllPanels();
        StageUIManager.Instance.OpenPopUp(
            "네트워크 오류",
            "방 생성에 실패했습니다. 다시 시도해주세요.",
            () =>
            {
                StageUIManager.Instance.OpenPanel(PanelType.Mode);
            }
        );
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

            photonView.RPC(
                nameof(LoadSceneClient),
                RpcTarget.Others,
                sceneName,
                operations,
                onComplete
            );
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
        Hashtable props = new Hashtable { { Keys.IS_SPAWNED, true } };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    public bool AreAllPlayersSpawned()
    {
        return PhotonNetwork.PlayerList.All(p =>
            p.CustomProperties.ContainsKey(Keys.IS_SPAWNED)
            && (bool)p.CustomProperties[Keys.IS_SPAWNED]
        );
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (
            changedProps.ContainsKey(Keys.TRACK_GUID)
            || changedProps.ContainsKey(Keys.TRACK_DIFFICULTY)
        )
        {
            var track = GetTrackData(targetPlayer);
            var diff = GetDifficulty(targetPlayer);
            if (track != null)
            {
                OnTrackUpdated?.Invoke(targetPlayer.ActorNumber, track, diff);
            }
        }
    }

    private void OnDestroy()
    {
        isPlaying = false;
        PhotonNetwork.Disconnect();
    }
}
