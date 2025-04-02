using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class NoteInteractor : MonoBehaviour
{
    public NoteColor noteColor;
    private XRBaseController controller;

    private void Awake()
    {
        controller = GetComponentInParent<XRBaseController>();
    }

    public void SendImpulse()
    {
        controller.SendHapticImpulse(1.0f, 0.1f);
    }
}
