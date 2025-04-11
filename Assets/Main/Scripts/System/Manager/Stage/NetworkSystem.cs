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
    public static class LobbyData
    {
        public const string IS_SPAWNED = "IsSpawned";
        public const string IS_PLAYER_READY = "IsPlayerReady";
        public const string TRACK_GUID = "SelectedTrackGUID";
        public const string TRACK_DIFFICULTY = "SelectedTrackDiff";
        public const string FINAL_TRACK_GUID = "FinalTrackGUID";
        public const string FINAL_TRACK_DIFFICULTY = "FinalTrackDiff";
        public const string READY_TO_LOAD_GAME = "ReadyToLoadGame";
    }

    public static class GameResultData
    {
        public const string SCORE = "Score";
        public const string HIGH_COMBO = "HighCombo";
        public const string NOTE_HIT_COUNT = "NoteHitCount";
        public const string TOTAL_NOTE_COUNT = "TotalNoteCount";
        public const string MISS_COUNT = "MissCount";
        public const string GOOD_COUNT = "GoodCount";
        public const string GREAT_COUNT = "GreatCount";
        public const string PERFECT_COUNT = "PerfectCount";
    }

    private bool isPlaying = true;
    public bool IsInitialized { get; private set; } = false;
    private MultiWaitingPanel multiWaitingPanel;
    private MultiRoomPanel multiRoomPanel;
    public event Action<int, TrackData, Difficulty> OnTrackUpdated;
    public event Action<Player> OnPlayerReadyStatusChanged;
    public event Action OnRemotePlayerJoined;
    public event Action OnRemotePlayerLeft;

    public void Initialize()
    {
        if (photonView == null)
        {
            PhotonView view = gameObject.AddComponent<PhotonView>();
            view.ViewID = 1;
            view.Synchronization = ViewSynchronization.Off;
            view.OwnershipTransfer = OwnershipOption.Fixed;
        }
    }

    public void StartMultiplayer()
    {
        multiWaitingPanel =
            StageUIManager.Instance.OpenPanel(PanelType.Multi_Waiting) as MultiWaitingPanel;

        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.ConnectUsingSettings();
        }
        else
        {
            PhotonNetwork.Disconnect();
            StartCoroutine(ReconnectAfterDisconnect());
        }

        IsInitialized = true;
    }

    private IEnumerator ReconnectAfterDisconnect()
    {
        while (PhotonNetwork.IsConnected)
        {
            yield return null;
        }

        yield return new WaitForSeconds(0.5f);

        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        CreateOrJoinMultiplayerRoom();
    }

    public void SetMultiRoomPanel(MultiRoomPanel panel)
    {
        multiRoomPanel = panel;
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
            { LobbyData.TRACK_GUID, guidStr },
            { LobbyData.TRACK_DIFFICULTY, diffInt },
        };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    public TrackData GetTrackData(Player player)
    {
        if (player.CustomProperties.TryGetValue(LobbyData.TRACK_GUID, out object guidObj))
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
        if (player.CustomProperties.TryGetValue(LobbyData.TRACK_DIFFICULTY, out object diffObj))
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

        if (chosenTrack == null)
            return;

        Hashtable roomProps = new Hashtable
        {
            { LobbyData.FINAL_TRACK_GUID, chosenTrack.id.ToString() },
            { LobbyData.FINAL_TRACK_DIFFICULTY, (int)chosenDiff },
            { "SelectedPlayerID", chosen.ActorNumber },
        };

        PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        if (
            propertiesThatChanged.ContainsKey(LobbyData.FINAL_TRACK_GUID)
            || propertiesThatChanged.ContainsKey(LobbyData.FINAL_TRACK_DIFFICULTY)
        )
        {
            int selectedPlayerID = 0;
            if (propertiesThatChanged.ContainsKey("SelectedPlayerID"))
            {
                selectedPlayerID = (int)propertiesThatChanged["SelectedPlayerID"];
            }
            else if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("SelectedPlayerID"))
            {
                selectedPlayerID = (int)
                    PhotonNetwork.CurrentRoom.CustomProperties["SelectedPlayerID"];
            }

            bool isLocalTrackSelected = selectedPlayerID == PhotonNetwork.LocalPlayer.ActorNumber;

            StageUIManager.Instance.CloseAllPanels();

            MultiTrackDecisionPanel multiTrackDecisionPanel =
                StageUIManager.Instance.OpenPanel(PanelType.Multi_TrackDecision)
                as MultiTrackDecisionPanel;

            StartCoroutine(multiTrackDecisionPanel.StartSpinning(isLocalTrackSelected));
        }
    }

    public void OnReadyButtonClicked()
    {
        if (PhotonNetwork.InRoom)
        {
            Hashtable props = new Hashtable { { LobbyData.IS_PLAYER_READY, true } };
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        }
    }

    private bool AreAllPlayersReady()
    {
        return PhotonNetwork.CurrentRoom.PlayerCount == PhotonNetwork.PlayerList.Length
            && PhotonNetwork.PlayerList.All(p =>
                p.CustomProperties.ContainsKey(LobbyData.IS_PLAYER_READY)
                && (bool)p.CustomProperties[LobbyData.IS_PLAYER_READY] == true
            );
    }

    [PunRPC]
    public void StartGame()
    {
        Debug.Log("[NetworkSystem] StartGame RPC execution started");

        if (
            PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(
                LobbyData.FINAL_TRACK_GUID,
                out object guidObj
            )
            && PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(
                LobbyData.FINAL_TRACK_DIFFICULTY,
                out object diffObj
            )
        )
        {
            string guidStr = (string)guidObj;
            int diffInt = (int)diffObj;

            Debug.Log($"[NetworkSystem] Track GUID: {guidStr}, Difficulty: {diffInt}");

            if (Guid.TryParse(guidStr, out Guid guid))
            {
                TrackData track = DataManager.Instance.TrackDataList.Find(t => t.id == guid);
                Difficulty diff = (Difficulty)diffInt;

                Debug.Log(
                    $"[NetworkSystem] Found track: {(track != null ? track.trackName : "null")}"
                );

                if (track != null)
                {
                    NoteMapData noteMapData = track.noteMapData.FirstOrDefault(n =>
                        n.difficulty == diff
                    );

                    Debug.Log(
                        $"[NetworkSystem] NoteMapData: {(noteMapData != null ? "Valid" : "null")} for difficulty {diff}"
                    );

                    if (noteMapData != null)
                    {
                        NoteMap noteMap = noteMapData.noteMap;

                        Debug.Log(
                            $"[NetworkSystem] NoteMap: {(noteMap != null ? "Valid" : "null")}, BPM: {(noteMap != null ? noteMap.bpm.ToString() : "unknown")}"
                        );

                        if (noteMap != null)
                        {
                            GameManager.Instance.StartMultiPlayerStage(noteMap, track);
                        }
                        else
                        {
                            Debug.LogError(
                                "[NetworkSystem] noteMap is null, cannot start multiplayer stage"
                            );
                        }
                    }
                    else
                    {
                        Debug.LogError(
                            $"[NetworkSystem] No NoteMapData found for track {track.trackName} at difficulty {diff}"
                        );
                    }
                }
                else
                {
                    Debug.LogError($"[NetworkSystem] Track not found for GUID {guid}");
                }
            }
            else
            {
                Debug.LogError($"[NetworkSystem] Failed to parse GUID: {guidStr}");
            }
        }
        else
        {
            Debug.LogError("[NetworkSystem] Room properties missing final track info");
        }
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        if (isPlaying)
        {
            Debug.LogWarning($"[NetworkSystem] Server disconnected: {cause}");
            if (StageUIManager.Instance != null)
            {
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
        }
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
        if (isPlaying)
        {
            isPlaying = false;
            if (PhotonNetwork.InRoom)
            {
                PhotonNetwork.LeaveRoom();
            }
            else
            {
                PhotonNetwork.Disconnect();
            }
        }
    }

    public void SetPlayerReadyToLoadGame()
    {
        Hashtable props = new Hashtable { { LobbyData.READY_TO_LOAD_GAME, true } };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    public void SetPlayerSpawned()
    {
        Hashtable props = new Hashtable { { LobbyData.IS_SPAWNED, true } };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    public bool AreAllPlayersReadyToStartGame()
    {
        return PhotonNetwork.PlayerList.All(p =>
            p.CustomProperties.ContainsKey(LobbyData.READY_TO_LOAD_GAME)
            && (bool)p.CustomProperties[LobbyData.READY_TO_LOAD_GAME] == true
        );
    }

    public bool AreAllPlayersSpawned()
    {
        return PhotonNetwork.PlayerList.All(p =>
            p.CustomProperties.ContainsKey(LobbyData.IS_SPAWNED)
            && (bool)p.CustomProperties[LobbyData.IS_SPAWNED]
        );
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (targetPlayer == null || changedProps == null)
            return;

        if (
            changedProps.ContainsKey(LobbyData.TRACK_GUID)
            || changedProps.ContainsKey(LobbyData.TRACK_DIFFICULTY)
        )
        {
            var track = GetTrackData(targetPlayer);
            var diff = GetDifficulty(targetPlayer);
            if (track != null)
            {
                OnTrackUpdated?.Invoke(targetPlayer.ActorNumber, track, diff);
            }
        }
        if (changedProps.ContainsKey(LobbyData.IS_PLAYER_READY))
        {
            if (changedProps[LobbyData.IS_PLAYER_READY] is bool isReady && isReady)
            {
                OnPlayerReadyStatusChanged?.Invoke(targetPlayer);

                if (multiRoomPanel != null && targetPlayer != PhotonNetwork.LocalPlayer)
                {
                    multiRoomPanel.OnRemotePlayerReady();
                }
            }

            if (AreAllPlayersReady() && PhotonNetwork.IsMasterClient)
            {
                DecideFinalTrack();
            }
        }
        if (changedProps.ContainsKey(LobbyData.READY_TO_LOAD_GAME))
        {
            if (AreAllPlayersReadyToStartGame())
            {
                if (photonView != null && photonView.ViewID > 0)
                {
                    Debug.Log("[NetworkSystem] StartGame RPC called");
                    photonView.RPC(nameof(StartGame), RpcTarget.All);
                }
                else
                {
                    Debug.LogError(
                        "[NetworkSystem] Invalid PhotonView when trying to call StartGame RPC. ViewID: "
                            + (photonView != null ? photonView.ViewID.ToString() : "null")
                    );
                }
            }
        }
    }

    [PunRPC]
    public void StartGamePlayback(double startTime, float preRollTime)
    {
        Debug.Log("[NetworkSystem] StartGamePlayback RPC called");
        GameManager.Instance.StartGamePlayback(startTime, preRollTime);
    }

    private void OnDestroy()
    {
        isPlaying = false;
        PhotonNetwork.Disconnect();
    }
}
