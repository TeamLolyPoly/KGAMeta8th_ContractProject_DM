using System.Collections;
using System.Xml;
using Photon.Pun;
using UnityEngine;

public class PlayerSystem : MonoBehaviourPunCallbacks, IInitializable
{
    public const string PLAYER_PREFAB = "Prefabs/Stage/Player/Player";

    public XRPlayer XRPlayer { get; private set; }
    private bool isInitialized = false;
    public bool IsInitialized => isInitialized;

    private bool isSpawned = false;
    public bool IsSpawned => isSpawned;
    public float fadeTime = 2f;

    public void Initialize()
    {
        isInitialized = true;
    }

    public void SpawnPlayer(Vector3 spawnPosition, bool isStage)
    {
        StartCoroutine(SpawnPlayerRoutine(spawnPosition, isStage));
    }

    private IEnumerator SpawnPlayerRoutine(Vector3 spawnPosition, bool isStage)
    {
        if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
        {
            if (GameManager.Instance.IsInMultiStage)
            {
                GameObject playerObj = PhotonNetwork.Instantiate(
                    PLAYER_PREFAB,
                    spawnPosition,
                    Quaternion.identity
                );

                XRPlayer multiPlayer = playerObj.GetComponent<XRPlayer>();

                multiPlayer.FadeIn(fadeTime);
                yield return new WaitForSeconds(fadeTime);
                XRPlayer = multiPlayer;

                GameManager.Instance.NetworkSystem.SetPlayerSpawned();
            }
            else
            {
                XRPlayer localPlayer = Instantiate(
                    Resources.Load<XRPlayer>(PLAYER_PREFAB),
                    spawnPosition,
                    Quaternion.identity
                );

                localPlayer.gameObject.name = "LocalPlayer";

                localPlayer.Initialize(isStage);
                localPlayer.FadeIn(fadeTime);
                yield return new WaitForSeconds(fadeTime);

                XRPlayer = localPlayer;
            }
        }
        else
        {
            Debug.Log($"[PlayerSystem] Creating offline player via Instantiate");

            XRPlayer localPlayer = Instantiate(
                Resources.Load<XRPlayer>(PLAYER_PREFAB),
                spawnPosition,
                Quaternion.identity
            );

            localPlayer.gameObject.name = "LocalPlayer";
            Debug.Log($"[PlayerSystem] Created local player (offline) at {spawnPosition}");

            localPlayer.Initialize(isStage);
            localPlayer.FadeIn(fadeTime);
            yield return new WaitForSeconds(fadeTime);

            XRPlayer = localPlayer;
        }

        yield return new WaitForSeconds(0.5f);

        isSpawned = true;
        Debug.Log($"[PlayerSystem] Player spawn complete, isSpawned set to {isSpawned}");
    }

    public void DespawnPlayer()
    {
        StartCoroutine(DespawnPlayerRoutine());
    }

    public IEnumerator DespawnPlayerRoutine()
    {
        if (XRPlayer == null || !isSpawned)
        {
            isSpawned = false;
            yield break;
        }

        try
        {
            if (XRPlayer.LeftRayInteractor != null)
            {
                XRPlayer.LeftRayInteractor.enabled = false;
            }

            if (XRPlayer.RightRayInteractor != null)
            {
                XRPlayer.RightRayInteractor.enabled = false;
            }

            XRPlayer.FadeOut(fadeTime);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("플레이어 페이드아웃 중 오류 발생: " + e.Message);
        }

        yield return new WaitForSeconds(fadeTime + 0.5f);

        try
        {
            if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
            {
                if (
                    GameManager.Instance.IsInMultiStage
                    && XRPlayer != null
                    && XRPlayer.gameObject != null
                )
                {
                    PhotonNetwork.Destroy(XRPlayer.gameObject);
                }
                else if (XRPlayer != null && XRPlayer.gameObject != null)
                {
                    Destroy(XRPlayer.gameObject);
                }
            }
            else if (XRPlayer != null && XRPlayer.gameObject != null)
            {
                Destroy(XRPlayer.gameObject);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("플레이어 게임오브젝트 제거 중 오류 발생: " + e.Message);
        }

        XRPlayer = null;
        isSpawned = false;
    }
}
