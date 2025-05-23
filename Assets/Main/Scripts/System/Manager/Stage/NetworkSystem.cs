using System;
using System.Collections;
using System.Linq;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class NetworkSystem : MonoBehaviourPunCallbacks
{
    public static class RoomData
    {
        public const string FINAL_TRACK_GUID = "FinalTrackGUID";
        public const string FINAL_TRACK_DIFFICULTY = "FinalTrackDiff";
    }

    public static class MultiPlayerData
    {
        public const string IS_IN_MULTI_STAGE = "IsInMultiStage";
        public const string IS_SPAWNED = "IsSpawned";
        public const string IS_PLAYER_READY = "IsPlayerReady";
        public const string TRACK_GUID = "SelectedTrackGUID";
        public const string TRACK_DIFFICULTY = "SelectedTrackDiff";
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

    private bool isMultiPlayer = true;
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
        isMultiPlayer = true;
    }

    public void ClearPlayerProperties()
    {
        Hashtable playerProps = new Hashtable
        {
            { MultiPlayerData.TRACK_GUID, null },
            { MultiPlayerData.TRACK_DIFFICULTY, null },
            { MultiPlayerData.IS_PLAYER_READY, false },
            { MultiPlayerData.IS_SPAWNED, false },
            { MultiPlayerData.READY_TO_LOAD_GAME, false },
            { MultiPlayerData.IS_IN_MULTI_STAGE, false },
        };

        PhotonNetwork.LocalPlayer.SetCustomProperties(playerProps);

        if (PhotonNetwork.IsMasterClient)
        {
            Hashtable roomProps = new Hashtable
            {
                { RoomData.FINAL_TRACK_GUID, null },
                { RoomData.FINAL_TRACK_DIFFICULTY, null },
                { "SelectedPlayerID", null },
            };
            PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);
        }
    }

    public bool IsMasterClient()
    {
        return PhotonNetwork.IsMasterClient;
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
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
            return;
        }

        if (!isMultiPlayer || !PhotonNetwork.IsConnectedAndReady)
            return;

        PhotonNetwork.JoinRandomRoom();
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        PhotonNetwork.CreateRoom(null, new RoomOptions { MaxPlayers = 2 });
    }

    public override void OnJoinedRoom()
    {
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

        Hashtable playerProps = new Hashtable
        {
            { MultiPlayerData.TRACK_GUID, guidStr },
            { MultiPlayerData.TRACK_DIFFICULTY, diffInt },
        };
        PhotonNetwork.LocalPlayer.SetCustomProperties(playerProps);
    }

    public TrackData GetTrackData(Player player)
    {
        if (player.CustomProperties.TryGetValue(MultiPlayerData.TRACK_GUID, out object guidObj))
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
        if (
            player.CustomProperties.TryGetValue(
                MultiPlayerData.TRACK_DIFFICULTY,
                out object diffObj
            )
        )
        {
            if (diffObj is int diffInt)
            {
                return (Difficulty)diffInt;
            }
        }
        return Difficulty.Easy;
    }

    public void SetResultData(ScoreData data)
    {
        Hashtable resultProps = new Hashtable
        {
            { GameResultData.SCORE, data.Score },
            { GameResultData.HIGH_COMBO, data.HighCombo },
            { GameResultData.NOTE_HIT_COUNT, data.NoteHitCount },
            { GameResultData.TOTAL_NOTE_COUNT, data.totalNoteCount },
            { GameResultData.MISS_COUNT, data.RatingCount[NoteRatings.Miss] },
            { GameResultData.GOOD_COUNT, data.RatingCount[NoteRatings.Good] },
            { GameResultData.GREAT_COUNT, data.RatingCount[NoteRatings.Great] },
            { GameResultData.PERFECT_COUNT, data.RatingCount[NoteRatings.Perfect] },
        };

        PhotonNetwork.LocalPlayer.SetCustomProperties(resultProps);
    }

    public ScoreData GetResultData(Player player)
    {
        ScoreData data = new ScoreData(
            (int)player.CustomProperties[GameResultData.SCORE],
            (int)player.CustomProperties[GameResultData.HIGH_COMBO],
            (int)player.CustomProperties[GameResultData.NOTE_HIT_COUNT],
            (int)player.CustomProperties[GameResultData.TOTAL_NOTE_COUNT],
            (int)player.CustomProperties[GameResultData.MISS_COUNT],
            (int)player.CustomProperties[GameResultData.GOOD_COUNT],
            (int)player.CustomProperties[GameResultData.GREAT_COUNT],
            (int)player.CustomProperties[GameResultData.PERFECT_COUNT]
        );

        return data;
    }

    public bool DecideWinner()
    {
        var players = PhotonNetwork.PlayerList;

        ScoreData masterPlayerScoreData = GetResultData(PhotonNetwork.LocalPlayer);
        ScoreData otherPlayerScoreData = GetResultData(players[1]);

        if (masterPlayerScoreData.Score > otherPlayerScoreData.Score)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public bool IsStageDone()
    {
        return PhotonNetwork.PlayerList.All(p =>
            p.CustomProperties.ContainsKey(GameResultData.SCORE)
        );
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
            { RoomData.FINAL_TRACK_GUID, chosenTrack.id.ToString() },
            { RoomData.FINAL_TRACK_DIFFICULTY, (int)chosenDiff },
            { "SelectedPlayerID", chosen.ActorNumber },
        };

        PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        if (
            propertiesThatChanged.ContainsKey(RoomData.FINAL_TRACK_GUID)
            || propertiesThatChanged.ContainsKey(RoomData.FINAL_TRACK_DIFFICULTY)
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
            Hashtable props = new Hashtable { { MultiPlayerData.IS_PLAYER_READY, true } };
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        }
    }

    private bool AreAllPlayersReady()
    {
        return PhotonNetwork.CurrentRoom.PlayerCount == PhotonNetwork.PlayerList.Length
            && PhotonNetwork.PlayerList.All(p =>
                p.CustomProperties.ContainsKey(MultiPlayerData.IS_PLAYER_READY)
                && (bool)p.CustomProperties[MultiPlayerData.IS_PLAYER_READY] == true
            );
    }

    public void SetIsInMultiStage(bool isInMultiStage)
    {
        Hashtable props = new Hashtable { { MultiPlayerData.IS_IN_MULTI_STAGE, isInMultiStage } };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    public bool AreAllPlayersInMultiStage()
    {
        return PhotonNetwork.PlayerList.All(p =>
            p.CustomProperties.ContainsKey(MultiPlayerData.IS_IN_MULTI_STAGE)
            && (bool)p.CustomProperties[MultiPlayerData.IS_IN_MULTI_STAGE] == true
        );
    }

    [PunRPC]
    public void StartGame()
    {
        if (
            PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(
                RoomData.FINAL_TRACK_GUID,
                out object guidObj
            )
            && PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(
                RoomData.FINAL_TRACK_DIFFICULTY,
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
                }
            }
        }
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        if (isMultiPlayer)
        {
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
        if (isMultiPlayer)
        {
            isMultiPlayer = false;
            PhotonNetwork.LeaveRoom();
            PhotonNetwork.Disconnect();
        }
    }

    public void SetPlayerReadyToLoadGame()
    {
        Hashtable props = new Hashtable { { MultiPlayerData.READY_TO_LOAD_GAME, true } };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    public void SetPlayerSpawned()
    {
        Hashtable props = new Hashtable { { MultiPlayerData.IS_SPAWNED, true } };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    public bool AreAllPlayersReadyToStartGame()
    {
        return PhotonNetwork.PlayerList.All(p =>
            p.CustomProperties.ContainsKey(MultiPlayerData.READY_TO_LOAD_GAME)
            && (bool)p.CustomProperties[MultiPlayerData.READY_TO_LOAD_GAME] == true
        );
    }

    public bool AreAllPlayersSpawned()
    {
        return PhotonNetwork.PlayerList.All(p =>
            p.CustomProperties.ContainsKey(MultiPlayerData.IS_SPAWNED)
            && (bool)p.CustomProperties[MultiPlayerData.IS_SPAWNED]
        );
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (targetPlayer == null || changedProps == null)
            return;

        if (
            changedProps.ContainsKey(MultiPlayerData.TRACK_GUID)
            || changedProps.ContainsKey(MultiPlayerData.TRACK_DIFFICULTY)
        )
        {
            var track = GetTrackData(targetPlayer);
            var diff = GetDifficulty(targetPlayer);
            if (track != null)
            {
                OnTrackUpdated?.Invoke(targetPlayer.ActorNumber, track, diff);
            }
        }
        if (changedProps.ContainsKey(MultiPlayerData.IS_PLAYER_READY))
        {
            if (changedProps[MultiPlayerData.IS_PLAYER_READY] is bool isReady && isReady)
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
        if (changedProps.ContainsKey(MultiPlayerData.READY_TO_LOAD_GAME))
        {
            if (AreAllPlayersReadyToStartGame())
            {
                if (photonView != null && photonView.ViewID > 0)
                {
                    photonView.RPC(nameof(StartGame), RpcTarget.All);
                }
            }
        }
    }

    public void SetRemoteBandAnims(Engagement engagement, int num)
    {
        photonView.RPC(nameof(RPC_RemoteBandAnim), RpcTarget.Others, engagement, num);
    }

    [PunRPC]
    public void RPC_RemoteBandAnim(Engagement engagement, int num)
    {
        if (GameManager.Instance.UnitAnimationSystem != null)
        {
            GameManager.Instance.UnitAnimationSystem.RemoteBandAnimationClipChange(engagement, num);
        }
    }

    private void OnDestroy()
    {
        isMultiPlayer = false;
        PhotonNetwork.Disconnect();
    }
}
