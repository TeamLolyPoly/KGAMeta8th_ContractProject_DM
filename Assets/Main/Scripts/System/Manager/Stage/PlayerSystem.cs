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
                PhotonView playerPhotonView = playerObj.GetComponent<PhotonView>();

                if (playerPhotonView.IsMine)
                {
                    multiPlayer.Initialize(isStage);
                    multiPlayer.FadeIn(fadeTime);
                    yield return new WaitForSeconds(fadeTime);
                    XRPlayer = multiPlayer;
                    playerObj.name = "LocalPlayer";
                }
                else
                {
                    multiPlayer.Initialize(isStage);
                    playerObj.name = "RemotePlayer";
                }
            }
            else
            {
                XRPlayer localPlayer = Instantiate(
                    Resources.Load<XRPlayer>(PLAYER_PREFAB),
                    spawnPosition,
                    Quaternion.identity
                );

                localPlayer.Initialize(isStage);
                localPlayer.FadeIn(fadeTime);
                yield return new WaitForSeconds(fadeTime);

                localPlayer.gameObject.name = "LocalPlayer";

                XRPlayer = localPlayer;
            }
            GameManager.Instance.NetworkSystem.SetPlayerSpawned();
        }
        else
        {
            XRPlayer localPlayer = Instantiate(
                Resources.Load<XRPlayer>(PLAYER_PREFAB),
                spawnPosition,
                Quaternion.identity
            );

            localPlayer.Initialize(isStage);
            localPlayer.FadeIn(fadeTime);
            yield return new WaitForSeconds(fadeTime);

            localPlayer.gameObject.name = "LocalPlayer";

            XRPlayer = localPlayer;
        }

        yield return new WaitForSeconds(0.5f);

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
