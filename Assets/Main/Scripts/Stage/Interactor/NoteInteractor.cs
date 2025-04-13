using Photon.Pun;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class NoteInteractor : MonoBehaviour
{
    public NoteColor noteColor;
    private XRBaseController controller;
    private PhotonView photonView;

    [SerializeField]
    private GameObject hitFXPrefab;

    private void Awake()
    {
        controller = GetComponentInParent<XRBaseController>();
        photonView = GetComponentInParent<PhotonView>();
    }

    public void SendImpulse()
    {
        controller.SendHapticImpulse(1.0f, 0.1f);
    }

    public void TriggerHitEffect(Vector3 position)
    {
        PlayHitEffect(position);

        if (photonView.IsMine)
        {
            photonView.RPC(nameof(RPC_PlayHitEffect), RpcTarget.Others, position);
        }
    }

    [PunRPC]
    private void RPC_PlayHitEffect(Vector3 position)
    {
        PlayHitEffect(position);
    }

    private void PlayHitEffect(Vector3 position)
    {
        if (hitFXPrefab != null)
        {
            var effect = PoolManager.Instance.Spawn<ParticleSystem>(
                hitFXPrefab,
                position,
                Quaternion.identity
            );
            effect.Play();
            PoolManager.Instance.Despawn(effect, 2.0f);
        }
    }
}
