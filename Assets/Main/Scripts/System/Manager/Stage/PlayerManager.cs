using System;
using Photon.Pun;
using UnityEngine;

public class PlayerSystem : MonoBehaviour
{
    public float YSpawnOffset;

    public const string STAGE_PLAYER_PREFAB = "StagePlayer";

    public const string LOBBY_PLAYER_PREFAB = "LobbyPlayer";

    public XRPlayer XRPlayer { get; private set; }

    public void SpawnPlayer(Vector3 spawnPosition, bool isStage)
    {
        GameObject player = PhotonNetwork.Instantiate(
            isStage ? STAGE_PLAYER_PREFAB : LOBBY_PLAYER_PREFAB,
            spawnPosition + new Vector3(0, YSpawnOffset, 0),
            Quaternion.identity
        );
        XRPlayer = player.GetComponent<XRPlayer>();
    }
}
