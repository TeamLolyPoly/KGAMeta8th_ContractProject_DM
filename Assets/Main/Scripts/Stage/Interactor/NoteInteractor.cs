using Photon.Pun;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class NoteInteractor : MonoBehaviourPunCallbacks
{
    public NoteColor noteColor;
    private XRBaseController controller;

    private void Awake()
    {
        controller = GetComponentInParent<XRBaseController>();
    }

    public void SendImpulse()
    {
        if (controller != null)
        {
            controller.SendHapticImpulse(1.0f, 0.1f);
        }
    }
}
