using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class NoteRPC : MonoBehaviour, IPoolable
{
    private PhotonView PV;

    private void Awake()
    {
        PV = GetComponent<PhotonView>();
    }

    public void OnSpawnFromPool()
    {
        if (PhotonNetwork.IsConnected && PV.IsMine)
        {
            PV.RPC("RPC_SyncNoteState", RpcTarget.All, transform.position, transform.rotation);
        }
    }

    public void OnReturnToPool()
    {
        if (PhotonNetwork.IsConnected && PV.IsMine)
        {
            PV.RPC("RPC_HideNote", RpcTarget.All);
        }
    }

    [PunRPC]
    private void RPC_SyncNoteState(Vector3 position, Quaternion rotation)
    {
        transform.position = position;
        transform.rotation = rotation;
        gameObject.SetActive(true);
    }

    [PunRPC]
    private void RPC_HideNote()
    {
        gameObject.SetActive(false);
    }
}
