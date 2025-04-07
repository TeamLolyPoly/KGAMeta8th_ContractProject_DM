using System.Collections;
using Photon.Pun;
using UnityEngine;

public class PlayerSystem : MonoBehaviourPunCallbacks
{
    public const string STAGE_PLAYER_PREFAB = "Prefabs/Stage/Player/StagePlayer";

    public const string LOBBY_PLAYER_PREFAB = "Prefabs/Stage/Player/LobbyPlayer";

    public XRPlayer XRPlayer { get; private set; }
    public bool IsInitialized { get; private set; } = false;

    private bool isSpawned = false;

    public bool IsSpawned => isSpawned;

    public float fadeTime = 3f;

    public void SpawnPlayer(Vector3 spawnPosition, bool isStage)
    {
        StartCoroutine(SpawnPlayerRoutine(spawnPosition, isStage));
    }

    private IEnumerator SpawnPlayerRoutine(Vector3 spawnPosition, bool isStage)
    {
        Debug.Log("[PlayerSystem] 플레이어 스폰");

        GameObject player = PhotonNetwork.Instantiate(
            isStage ? STAGE_PLAYER_PREFAB : LOBBY_PLAYER_PREFAB,
            spawnPosition,
            Quaternion.identity
        );

        XRPlayer = player.GetComponent<XRPlayer>();
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
        PhotonNetwork.Destroy(XRPlayer.gameObject);
        XRPlayer = null;
        isSpawned = false;
    }
}
