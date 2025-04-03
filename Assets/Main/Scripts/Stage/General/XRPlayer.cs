using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class XRPlayer : MonoBehaviour
{
    [SerializeField]
    private ActionBasedController leftController;

    [SerializeField]
    private ActionBasedController rightController;

    private ActionBasedContinuousMoveProvider continuousMoveProvider;
    private ActionBasedSnapTurnProvider snapTurnProvider;
    private GrabMoveProvider grabMoveProvider;
    public ActionBasedController LeftController => leftController;
    public ActionBasedController RightController => rightController;

    private void Awake()
    {
        continuousMoveProvider = GetComponent<ActionBasedContinuousMoveProvider>();
        if (continuousMoveProvider != null)
        {
            continuousMoveProvider.enabled = false;
        }
        snapTurnProvider = GetComponent<ActionBasedSnapTurnProvider>();
        if (snapTurnProvider != null)
        {
            snapTurnProvider.enabled = false;
        }
        grabMoveProvider = GetComponent<GrabMoveProvider>();
        if (grabMoveProvider != null)
        {
            grabMoveProvider.enabled = false;
        }
    }
}
