using System;
using System.Collections;
using Photon.Pun;
using UnityEngine;

public class PlayerSystem : MonoBehaviourPunCallbacks, IInitializable
{
    public const string PLAYER_PREFAB = "Prefabs/Stage/Player/Player";

    public XRPlayer XRPlayer;
    public XRPlayer remotePlayer;
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
                Debug.Log(
                    $"[PlayerSystem] 멀티플레이어 생성 시작 - 위치: {spawnPosition}, 방 인원: {PhotonNetwork.CurrentRoom.PlayerCount}"
                );
                XRPlayer multiPlayer = PhotonNetwork
                    .Instantiate(PLAYER_PREFAB, spawnPosition, Quaternion.identity)
                    .GetComponent<XRPlayer>();

                Debug.Log(
                    $"[PlayerSystem] 플레이어 생성됨 - ID: {multiPlayer.photonView.ViewID}, IsMine: {multiPlayer.photonView.IsMine}"
                );

                if (multiPlayer.photonView.IsMine)
                {
                    multiPlayer.FadeIn(fadeTime);
                    yield return new WaitForSeconds(fadeTime);
                }

                Debug.Log("[PlayerSystem] 플레이어 대기 시작...");
                yield return new WaitUntil(() => XRPlayer != null);
                Debug.Log(
                    $"[PlayerSystem] 로컬 플레이어 설정됨: {(XRPlayer != null ? XRPlayer.name : "null")}"
                );

                yield return new WaitUntil(() => remotePlayer != null);
                Debug.Log(
                    $"[PlayerSystem] 원격 플레이어 설정됨: {(remotePlayer != null ? remotePlayer.name : "null")}"
                );

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

        yield return new WaitForSeconds(0.5f);

        isSpawned = true;
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
        catch (Exception e)
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
                    remotePlayer = null;
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
        catch (Exception e)
        {
            Debug.LogWarning("플레이어 게임오브젝트 제거 중 오류 발생: " + e.Message);
        }

        XRPlayer = null;
        isSpawned = false;
    }

    public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
    {
        base.OnPlayerEnteredRoom(newPlayer);
        Debug.Log(
            $"[PlayerSystem] 새 플레이어 입장: {newPlayer.NickName}, ActorNumber: {newPlayer.ActorNumber}"
        );
    }

    public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
    {
        base.OnPlayerLeftRoom(otherPlayer);
        Debug.Log(
            $"[PlayerSystem] 플레이어 퇴장: {otherPlayer.NickName}, ActorNumber: {otherPlayer.ActorNumber}"
        );
    }
}
