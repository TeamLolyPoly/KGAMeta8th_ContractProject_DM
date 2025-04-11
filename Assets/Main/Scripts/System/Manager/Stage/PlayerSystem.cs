using System.Collections;
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
        Debug.Log(
            $"[PlayerSystem] SpawnPlayerRoutine called at position {spawnPosition}, isStage={isStage}, IsConnected={PhotonNetwork.IsConnected}, IsInRoom={PhotonNetwork.InRoom}, IsMasterClient={PhotonNetwork.IsMasterClient}"
        );

        if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
        {
            if (GameManager.Instance.IsInMultiStage)
            {
                Debug.Log(
                    $"[PlayerSystem] Creating multiplayer player via PhotonNetwork.Instantiate"
                );

                GameObject playerObj = PhotonNetwork.Instantiate(
                    PLAYER_PREFAB,
                    spawnPosition,
                    Quaternion.identity
                );

                XRPlayer multiPlayer = playerObj.GetComponent<XRPlayer>();

                multiPlayer.FadeIn(fadeTime);
                yield return new WaitForSeconds(fadeTime);
                XRPlayer = multiPlayer;

                if (PhotonNetwork.IsMasterClient)
                {
                    Vector3 masterPos = new Vector3(0, 2.1f, 0.58f);
                    playerObj.transform.position = masterPos;
                    Debug.Log($"[PlayerSystem] Set master position to {masterPos}");
                }
                else
                {
                    Vector3 clientPos = new Vector3(15f, 2.1f, -0.58f);
                    playerObj.transform.position = clientPos;
                    Debug.Log($"[PlayerSystem] Set client position to {clientPos}");
                }

                GameManager.Instance.NetworkSystem.SetPlayerSpawned();
            }
            else
            {
                Debug.Log($"[PlayerSystem] Creating single player in non-multi mode");

                XRPlayer localPlayer = Instantiate(
                    Resources.Load<XRPlayer>(PLAYER_PREFAB),
                    spawnPosition,
                    Quaternion.identity
                );

                localPlayer.gameObject.name = "LocalPlayer";
                Debug.Log($"[PlayerSystem] Created local player (non-multi) at {spawnPosition}");

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
        XRPlayer.LeftRayInteractor.enabled = false;
        XRPlayer.RightRayInteractor.enabled = false;
        XRPlayer.FadeOut(fadeTime);
        yield return new WaitForSeconds(fadeTime + 2f);
        if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
        {
            if (GameManager.Instance.IsInMultiStage)
            {
                PhotonNetwork.Destroy(XRPlayer.gameObject);
            }
            else
            {
                Destroy(XRPlayer.gameObject);
            }
        }
        else
        {
            Destroy(XRPlayer.gameObject);
        }
        XRPlayer = null;
        isSpawned = false;
    }
}
