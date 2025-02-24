using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class TestNetworkManager : MonoBehaviourPunCallbacks
{
    public GameObject playerPrefab; //플레이어 프리팹 (Inspector에서 연결)

    void Start()
    {
        Debug.Log("Connecting to Photon Server...");
        PhotonNetwork.ConnectUsingSettings(); // Photon 서버 연결 시작
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to Master Server! Joining Lobby...");
        PhotonNetwork.JoinLobby(); //Master Server에 연결되면 로비 참가
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("Joined Lobby! Ready for matchmaking.");
    }

    public void CreateOrJoinRoom()
    {
        if (PhotonNetwork.IsConnectedAndReady && PhotonNetwork.InLobby)
        {
            Debug.Log("Trying to join a random room...");
            PhotonNetwork.JoinRandomRoom(); //랜덤 룸 참가
        }
        else
        {
            Debug.LogError("Cannot join room. Not connected to Master Server or not in lobby.");
        }
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log("No random room found, creating a new room.");
        PhotonNetwork.CreateRoom(null, new RoomOptions { MaxPlayers = 4 }); //랜덤 룸이 없으면 새로운 방 생성
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("Successfully joined room: " + PhotonNetwork.CurrentRoom.Name);
        SpawnPlayer(); //방 입장 후 플레이어 생성
    }

    void SpawnPlayer()
    {
        if (playerPrefab == null)
        {
            Debug.LogError("Player Prefab is not assigned in the Inspector!");
            return;
        }

        //랜덤한 위치에서 플레이어 생성
        Vector3 spawnPos = new Vector3(Random.Range(-5f, 5f), 0, Random.Range(-5f, 5f));
        spawnPos.y = 2.0f;
        PhotonNetwork.Instantiate(playerPrefab.name, spawnPos, Quaternion.identity);
    }
}
