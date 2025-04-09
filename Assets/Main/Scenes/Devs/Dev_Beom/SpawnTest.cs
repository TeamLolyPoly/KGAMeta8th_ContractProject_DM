using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR;
using UnityEngine.XR.Management;

public class SpawnTest : MonoBehaviourPunCallbacks
{
    public static SpawnTest Instance { get; private set; }

    public GameObject xrPlayerPrefab;
    public GameObject fallbackPlayerPrefab; // XR 없을 때 사용될 일반 플레이어 프리팹

    private bool xrDeviceAvailable = false;

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
            PhotonNetwork.ConnectUsingSettings();
        }

        CheckXRDevice(); // XR 연결 확인
        SceneManager.sceneLoaded += OnSceneLoaded;
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

        // 기존 플레이어들을 RemotePlayer로 변경
        GameObject[] existingPlayers = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject player in existingPlayers)
        {
            if (player.GetComponent<PhotonView>().IsMine)
            {
                continue; // 내 플레이어는 건너뛰기
            }

            // RemotePlayer로 변경
            player.tag = "RemotePlayer";
            Debug.Log($"[SpawnTest] 플레이어 {player.name}를 RemotePlayer로 변경");
        }

        if (SceneManager.GetActiveScene().name == "XRTest2")
        {
            StartCoroutine(InitXRAndSpawn());
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "XRTest2" && PhotonNetwork.InRoom)
        {
            StartCoroutine(InitXRAndSpawn());
        }
    }

    /// <summary>
    /// 연결된 XR 디바이스가 있는지 체크
    /// </summary>
    public void CheckXRDevice()
    {
        List<InputDevice> devices = new List<InputDevice>();
        InputDevices.GetDevices(devices);

        xrDeviceAvailable = devices.Count > 0;

        if (xrDeviceAvailable)
        {
            Debug.Log($"[SpawnTest] XR 디바이스 연결됨 ({devices.Count}개)");
        }
        else
        {
            Debug.LogWarning("[SpawnTest] XR 디바이스 없음 - 일반 모드로 실행됩니다.");
        }
    }

    /// <summary>
    /// XR 있는 경우 XR 로더 초기화, 없으면 바로 기본 스폰
    /// </summary>
    private IEnumerator InitXRAndSpawn()
    {
        if (xrDeviceAvailable)
        {
            XRManagerSettings xrManager = XRGeneralSettings.Instance?.Manager;

            if (xrManager == null)
            {
                Debug.LogError("[SpawnTest] XR Manager가 null입니다.");
                yield break;
            }

            if (xrManager.isInitializationComplete)
            {
                xrManager.DeinitializeLoader();
                yield return null;
            }

            yield return xrManager.InitializeLoader();

            if (xrManager.activeLoader == null)
            {
                Debug.LogError("[SpawnTest] XR Loader 초기화 실패");
                yield break;
            }

            xrManager.StartSubsystems();
            Debug.Log("[SpawnTest] XR 시스템 초기화 완료");

            yield return new WaitForSeconds(0.5f);
            SpawnXRPlayer();
        }
        else
        {
            yield return new WaitForSeconds(0.5f);
            SpawnFallbackPlayer(); // 일반 모드 플레이어 스폰
        }
    }

    public void SpawnXRPlayer()
    {
        if (xrPlayerPrefab == null)
        {
            Debug.LogError("[SpawnTest] XR Player 프리팹이 지정되지 않았습니다.");
            return;
        }

        Vector3 spawnPos = new Vector3(0, 1, 0);
        PhotonNetwork.Instantiate(xrPlayerPrefab.name, spawnPos, Quaternion.identity);
        Debug.Log("[SpawnTest] XR 플레이어 스폰 완료");
    }

    public void SpawnFallbackPlayer()
    {
        if (fallbackPlayerPrefab == null)
        {
            Debug.LogError("[SpawnTest] 일반 플레이어 프리팹이 지정되지 않았습니다.");
            return;
        }

        Vector3 spawnPos = new Vector3(0, 1, 0);
        PhotonNetwork.Instantiate(fallbackPlayerPrefab.name, spawnPos, Quaternion.identity);
        Debug.Log("[SpawnTest] 일반 플레이어 스폰 완료 (XR 없음)");
    }
}
