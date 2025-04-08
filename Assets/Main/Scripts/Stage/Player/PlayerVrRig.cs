using UnityEngine;

public class PlayerVrRig : MonoBehaviour
{
    [Range(0, 1)]
    public float turnSmoothness = 0.1f;
    public Vector3 CameraPositionOffset;
    public VrMap head;
    public VrMap leftHand;
    public VrMap rightHand;

    void LateUpdate()
    {
        Vector3 pos = head.vrTarget.position;
        pos.z += CameraPositionOffset.z;
        pos.y += -head.vrTarget.position.y + CameraPositionOffset.y;
        pos.x += CameraPositionOffset.x;

        transform.position = Vector3.Lerp(transform.position, pos, 0.05f);

        float yaw = head.vrTarget.eulerAngles.y;
        transform.rotation = Quaternion.Lerp(
            transform.rotation,
            Quaternion.Euler(transform.eulerAngles.x, yaw, transform.eulerAngles.z),
            turnSmoothness
        );

        head.Map();
        leftHand.Map();
        rightHand.Map();
    }
}
