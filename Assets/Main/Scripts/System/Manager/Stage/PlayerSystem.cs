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
            XRPlayer multiPlayer = PhotonNetwork
                .Instantiate(PLAYER_PREFAB, spawnPosition, Quaternion.identity)
                .GetComponent<XRPlayer>();
            if (photonView.IsMine)
            {
                multiPlayer.Initialize(isStage);
                multiPlayer.FadeIn(fadeTime);
                yield return new WaitForSeconds(fadeTime);
                XRPlayer = multiPlayer;
                gameObject.name = "LocalPlayer";
            }
            else
            {
                multiPlayer.Initialize(isStage);
                gameObject.name = "RemotePlayer";
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

            gameObject.name = "LocalPlayer";

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
            PhotonNetwork.Destroy(XRPlayer.gameObject);
        }
        else
        {
            Destroy(XRPlayer.gameObject);
        }
        XRPlayer = null;
        isSpawned = false;
    }
}
