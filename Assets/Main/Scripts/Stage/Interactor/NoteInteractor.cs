using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using Photon.Pun;

public class NoteInteractor : MonoBehaviour
{
    public NoteColor noteColor;
    private XRBaseController controller;
    private PhotonView photonView;

    private void Awake()
    {
        controller = GetComponentInParent<XRBaseController>();
        photonView = GetComponentInParent<PhotonView>();
    }

    public void SendImpulse()
    {
        controller.SendHapticImpulse(1.0f, 0.1f);
    }
}
