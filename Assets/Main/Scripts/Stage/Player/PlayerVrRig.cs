using System;
using Photon.Pun;
using UnityEngine;

public class PlayerVrRig : MonoBehaviourPun, IPunObservable
{
    [Header("로컬/리모트 VR 대상")]
    public VrMap head;
    public VrMap leftHand;
    public VrMap rightHand;

    [Header("로컬 위치 조정")]
    public Vector3 cameraOffset;
    public float turnSmoothness = 0.1f;

    // 리모트용 변수
    private Vector3 headPos,
        leftPos,
        rightPos;
    private Quaternion headRot,
        leftRot,
        rightRot;

    void LateUpdate()
    {
        if (photonView.IsMine)
        {
            // 내 XR Origin을 기준으로 동작
            Vector3 targetPos = head.vrTarget.position + cameraOffset;
            targetPos.y += -1.45f;

            transform.position = Vector3.Lerp(transform.position, targetPos, 0.05f);

            float yaw = head.vrTarget.eulerAngles.y;
            transform.rotation = Quaternion.Lerp(
                transform.rotation,
                Quaternion.Euler(0, yaw, 0),
                turnSmoothness
            );

            head.Map();
            leftHand.Map();
            rightHand.Map();
        }
        else
        {
            // 리모트 플레이어 위치 업데이트
            head.ikTarget.position = Vector3.Lerp(
                head.ikTarget.position,
                headPos,
                Time.deltaTime * 25f
            );
            head.ikTarget.rotation = Quaternion.Slerp(
                head.ikTarget.rotation,
                headRot,
                Time.deltaTime * 25f
            );

            leftHand.ikTarget.position = Vector3.Lerp(
                leftHand.ikTarget.position,
                leftPos,
                Time.deltaTime * 25f
            );
            leftHand.ikTarget.rotation = Quaternion.Slerp(
                leftHand.ikTarget.rotation,
                leftRot,
                Time.deltaTime * 25f
            );

            rightHand.ikTarget.position = Vector3.Lerp(
                rightHand.ikTarget.position,
                rightPos,
                Time.deltaTime * 25f
            );
            rightHand.ikTarget.rotation = Quaternion.Slerp(
                rightHand.ikTarget.rotation,
                rightRot,
                Time.deltaTime * 25f
            );
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting && photonView.IsMine)
        {
            stream.SendNext(head.ikTarget.position);
            stream.SendNext(head.ikTarget.rotation);

            stream.SendNext(leftHand.ikTarget.position);
            stream.SendNext(leftHand.ikTarget.rotation);

            stream.SendNext(rightHand.ikTarget.position);
            stream.SendNext(rightHand.ikTarget.rotation);
        }
        else
        {
            headPos = (Vector3)stream.ReceiveNext();
            headRot = (Quaternion)stream.ReceiveNext();

            leftPos = (Vector3)stream.ReceiveNext();
            leftRot = (Quaternion)stream.ReceiveNext();

            rightPos = (Vector3)stream.ReceiveNext();
            rightRot = (Quaternion)stream.ReceiveNext();
        }
    }
}
