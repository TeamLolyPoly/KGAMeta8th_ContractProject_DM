using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class TestNetworkManager : MonoBehaviourPunCallbacks
{
    public GameObject playerPrefab; //�÷��̾� ������ (Inspector���� ����)

    void Start()
    {
        Debug.Log("Connecting to Photon Server...");
        PhotonNetwork.ConnectUsingSettings(); // Photon ���� ���� ����
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to Master Server! Joining Lobby...");
        PhotonNetwork.JoinLobby(); //Master Server�� ����Ǹ� �κ� ����
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
            PhotonNetwork.JoinRandomRoom(); //���� �� ����
        }
        else
        {
            Debug.LogError("Cannot join room. Not connected to Master Server or not in lobby.");
        }
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log("No random room found, creating a new room.");
        PhotonNetwork.CreateRoom(null, new RoomOptions { MaxPlayers = 4 }); //���� ���� ������ ���ο� �� ����
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("Successfully joined room: " + PhotonNetwork.CurrentRoom.Name);
        SpawnPlayer(); //�� ���� �� �÷��̾� ����
    }

    void SpawnPlayer()
    {
        if (playerPrefab == null)
        {
            Debug.LogError("Player Prefab is not assigned in the Inspector!");
            return;
        }

        //������ ��ġ���� �÷��̾� ����
        Vector3 spawnPos = new Vector3(Random.Range(-5f, 5f), 0, Random.Range(-5f, 5f));
        spawnPos.y = 2.0f;
        PhotonNetwork.Instantiate(playerPrefab.name, spawnPos, Quaternion.identity);
    }
}
