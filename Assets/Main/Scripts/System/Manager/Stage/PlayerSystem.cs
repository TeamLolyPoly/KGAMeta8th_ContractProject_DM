using System.Collections;
using Photon.Pun;
using UnityEngine;

public class PlayerSystem : MonoBehaviourPunCallbacks, IInitializable
{
    public const string STAGE_PLAYER_PREFAB = "Prefabs/Stage/Player/StagePlayer";

    public const string LOBBY_PLAYER_PREFAB = "Prefabs/Stage/Player/LobbyPlayer";

    private XRPlayer stagePlayerPrefab;
    private XRPlayer lobbyPlayerPrefab;

    public XRPlayer XRPlayer { get; private set; }
    private bool isInitialized = false;
    public bool IsInitialized => isInitialized;

    private bool isSpawned = false;

    public bool IsSpawned => isSpawned;

    public float fadeTime = 3f;

    public void Initialize()
    {
        stagePlayerPrefab = Resources.Load<XRPlayer>(STAGE_PLAYER_PREFAB);
        lobbyPlayerPrefab = Resources.Load<XRPlayer>(LOBBY_PLAYER_PREFAB);
        isInitialized = true;
    }

    public void SpawnPlayer(Vector3 spawnPosition, bool isStage)
    {
        StartCoroutine(SpawnPlayerRoutine(spawnPosition, isStage));
    }

    private IEnumerator SpawnPlayerRoutine(Vector3 spawnPosition, bool isStage)
    {
        Debug.Log("[PlayerSystem] 플레이어 스폰");

        XRPlayer player = Instantiate(
            isStage ? stagePlayerPrefab : lobbyPlayerPrefab,
            spawnPosition,
            Quaternion.identity
        );

        XRPlayer = player;
        yield return new WaitForSeconds(2f);
        XRPlayer.FadeIn(fadeTime);
        yield return new WaitForSeconds(fadeTime);
        XRPlayer.LeftRayInteractor.enabled = true;
        XRPlayer.RightRayInteractor.enabled = true;
        isSpawned = true;
    }

    public void DespawnPlayer()
    {
        StartCoroutine(DespawnPlayerRoutine());
    }

    public IEnumerator DespawnPlayerRoutine()
    {
        XRPlayer.LeftRayInteractor.enabled = false;
        XRPlayer.RightRayInteractor.enabled = false;
        XRPlayer.FadeOut(fadeTime);
        yield return new WaitForSeconds(fadeTime + 2f);
        Destroy(XRPlayer.gameObject);
        XRPlayer = null;
        isSpawned = false;
    }
}
