using UnityEngine;

public class PlayerVrRig : MonoBehaviour
{
    [Range(0, 1)]
    public float turnSmoothness = 0.1f;
    public VrMap head;
    public VrMap leftHand;
    public VrMap rightHand;

    public Vector3 headBodyPositionOffset;
    public float headBodyYawOffset;

    void LateUpdate()
    {
        transform.position = head.ikTarget.position + headBodyPositionOffset;
        float yaw = head.vrTarget.eulerAngles.y;
        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(transform.eulerAngles.x, yaw, transform.eulerAngles.z), turnSmoothness);

        head.Map();
        leftHand.Map();
        rightHand.Map();
    }
}
