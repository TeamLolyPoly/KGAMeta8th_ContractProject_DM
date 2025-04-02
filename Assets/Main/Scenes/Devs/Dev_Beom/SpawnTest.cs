using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR;

public class SpawnTest : MonoBehaviourPunCallbacks
{
    public static SpawnTest Instance { get; private set; }

    public GameObject xrPlayerPrefab;

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

    private void Start()
    {
        if (!PhotonNetwork.IsConnected)
        {
            Debug.Log("[SpawnTest] Photon에 연결 중...");
            PhotonNetwork.ConnectUsingSettings();
        }
        else
        {
            Debug.Log("[SpawnTest] 이미 Photon에 연결됨");
        }

        CheckXRDevice();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("[SpawnTest] Photon 마스터 서버에 연결됨");

        if (!PhotonNetwork.InRoom)
        {
            RoomOptions roomOptions = new RoomOptions { MaxPlayers = 4 };
            PhotonNetwork.JoinOrCreateRoom("TestRoom", roomOptions, null); // <- 여기서 TypedLobby 제거
        }
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("[SpawnTest] Photon 방에 입장 완료");

        // 만약 씬이 이미 XRTest2라면 바로 스폰
        if (SceneManager.GetActiveScene().name == "XRTest2")
        {
            SpawnXRPlayer();
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>
    /// XR 디바이스 연결 여부 확인 (XRTest 씬에서 호출됨)
    /// </summary>
    public void CheckXRDevice()
    {
        List<InputDevice> devices = new List<InputDevice>();
        InputDevices.GetDevices(devices);

        if (devices.Count == 0)
        {
            Debug.LogWarning("[SpawnTest] XR 디바이스가 연결되지 않았습니다.");
        }
        else
        {
            Debug.Log($"[SpawnTest] XR 디바이스 수: {devices.Count}");
            foreach (var device in devices)
            {
                Debug.Log($"  - {device.name} ({device.characteristics})");
            }
        }
    }

    /// <summary>
    /// XRTest2 씬에 진입하면 자동으로 스폰 수행
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "XRTest2" && PhotonNetwork.InRoom)
        {
            StartCoroutine(SpawnXRPlayer());
        }
    }

    public IEnumerator SpawnXRPlayer()
    {
        if (xrPlayerPrefab == null)
        {
            Debug.LogError("[SpawnTest] XR Player Prefab이 지정되지 않았습니다.");
            yield return null;
        }

        yield return new WaitForSeconds(2f);

        Vector3 spawnPos = new Vector3(0, 1, 0);
        PhotonNetwork.Instantiate(xrPlayerPrefab.name, spawnPos, Quaternion.identity);
        Debug.Log($"[SpawnTest] XR Player 스폰 완료 at {spawnPos}");
    }
}
