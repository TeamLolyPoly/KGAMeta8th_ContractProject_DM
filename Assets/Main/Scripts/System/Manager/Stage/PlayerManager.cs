using System.Collections;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerSystem : MonoBehaviourPunCallbacks
{
    public float YSpawnOffset = 0.5f;

    public const string STAGE_PLAYER_PREFAB = "Prefabs/Stage/Player/StagePlayer";

    public const string LOBBY_PLAYER_PREFAB = "Prefabs/Stage/Player/LobbyPlayer";

    public XRPlayer XRPlayer { get; private set; }
    public bool IsInitialized { get; private set; } = false;

    private bool isSpawned = false;

    public IEnumerator InitializeRoutine()
    {
        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.ConnectUsingSettings();
        }

        SceneManager.sceneLoaded += OnSceneLoaded;

        yield return new WaitUntil(() => PhotonNetwork.InRoom);
        StartCoroutine(SpawnPlayer(Vector3.zero, false));
        yield return new WaitUntil(() => isSpawned);
        IsInitialized = true;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("[SpawnTest] Photon 마스터 서버 연결 완료");

        if (!PhotonNetwork.InRoom)
        {
            RoomOptions roomOptions = new RoomOptions { MaxPlayers = 4 };
            PhotonNetwork.JoinOrCreateRoom("TestRoom", roomOptions, null);
        }
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("[SpawnTest] 방 입장 완료");
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (PhotonNetwork.InRoom)
        {
            StartCoroutine(SpawnPlayer(Vector3.zero, false));
        }
    }

    public IEnumerator SpawnPlayer(Vector3 spawnPosition, bool isStage)
    {
        yield return new WaitForSeconds(3f);
        GameObject player = PhotonNetwork.Instantiate(
            isStage ? STAGE_PLAYER_PREFAB : LOBBY_PLAYER_PREFAB,
            spawnPosition + new Vector3(0, YSpawnOffset, 0),
            Quaternion.identity
        );
        XRPlayer = player.GetComponent<XRPlayer>();
        XRPlayer.FadeIn(3f);
        isSpawned = true;
    }
}
