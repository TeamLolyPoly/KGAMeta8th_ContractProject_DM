using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TestNetworkManager : MonoBehaviourPunCallbacks
{
    public static TestNetworkManager Instance { get; private set; }

    public GameObject playerPrefab;
    private bool isMultiplayer = false;
    private Dictionary<int, bool> playerReadyStatus = new Dictionary<int, bool>();

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

    // void OnEnable()
    // {
    //     SceneManager.sceneLoaded += OnSceneLoaded;
    // }

    // void OnDisable()
    // {
    //     SceneManager.sceneLoaded -= OnSceneLoaded;
    // }

    void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("Joined Lobby! Ready for matchmaking.");
    }

    // 싱글 플레이 전용
    public void CreateSinglePlayerRoom()
    {
        isMultiplayer = false;
        RoomOptions options = new RoomOptions { MaxPlayers = 1 };
        PhotonNetwork.CreateRoom("SingleRoom_" + Random.Range(0, 10000), options);
    }

    // 멀티 플레이 전용
    public void CreateOrJoinMultiplayerRoom()
    {
        isMultiplayer = true;
        PhotonNetwork.JoinRandomRoom();
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        PhotonNetwork.CreateRoom(null, new RoomOptions { MaxPlayers = 2 });
    }

    public override void OnJoinedRoom()
    {
        playerReadyStatus.Clear();

        Debug.Log("Room joined: " + PhotonNetwork.CurrentRoom.Name);

        // 싱글 플레이는 바로 씬 전환
        if (!isMultiplayer)
        {
            PhotonNetwork.LoadLevel("GameScene");
            return;
        }

        // 멀티 플레이어가 2명 모이면 마스터가 씬 전환
        if (PhotonNetwork.CurrentRoom.PlayerCount == 2 && PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.AutomaticallySyncScene = true;
            PhotonNetwork.LoadLevel("GameScene");
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log("Player joined: " + newPlayer.NickName);
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        playerReadyStatus.Remove(otherPlayer.ActorNumber);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "GameScene" && PhotonNetwork.InRoom)
        {
            SpawnPlayer();

            if (isMultiplayer)
            {
                photonView.RPC("SetPlayerReady", RpcTarget.MasterClient);
            }
            else
            {
                // 싱글은 바로 시작
                // GameManager.Instance.StartGame();
            }
        }
    }

    private void SpawnPlayer()
    {
        if (playerPrefab == null)
        {
            Debug.LogError("Player Prefab is not assigned in the Inspector!");
            return;
        }

        Transform spawnPoint = GetSpawnPoint();
        PhotonNetwork.Instantiate(playerPrefab.name, spawnPoint.position, spawnPoint.rotation);
    }

    private Transform GetSpawnPoint()
    {
        // TODO: 스폰 포인트 시스템 구현
        // 간단한 예: Photon Player ID로 두 개 위치 고정
        // GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("SpawnPoint");

        // if (spawnPoints.Length >= PhotonNetwork.CurrentRoom.PlayerCount)
        // {
        //     int index = PhotonNetwork.LocalPlayer.ActorNumber % spawnPoints.Length;
        //     return spawnPoints[index].transform;
        // }
        return transform;
    }

    // 레디 버튼 이벤트용
    // public void OnReadyButtonClicked()
    // {
    //     if (PhotonNetwork.InRoom)
    //     {
    //         photonView.RPC("SetPlayerReady", RpcTarget.MasterClient);
    //     }
    // }

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
        // TODO : 플레이어 상태 체크 완료, 혹은 싱글 플레이에서 이후 게임 시작
        // GameManager.Instance.StartGame();
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning($"서버 연결이 끊어졌습니다: {cause}");
        // TODO: 재연결 로직 구현
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"방 생성 실패: {message}");
        // TODO: 재시도 로직 구현
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
}
