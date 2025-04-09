using UnityEngine;
using Photon.Pun;

public class RemoteVrSync : MonoBehaviourPun, IPunObservable
{
    [Header("리모트 아바타 파츠")]
    public Transform headIK;
    public Transform leftHandIK;
    public Transform rightHandIK;

    private Vector3 headPos;
    private Quaternion headRot;
    private Vector3 leftPos;
    private Quaternion leftRot;
    private Vector3 rightPos;
    private Quaternion rightRot;

    void Update()
    {
        if (!photonView.IsMine)
        {
            headIK.position = Vector3.Lerp(headIK.position, headPos, Time.deltaTime * 10f);
            headIK.rotation = Quaternion.Slerp(headIK.rotation, headRot, Time.deltaTime * 10f);

            leftHandIK.position = Vector3.Lerp(leftHandIK.position, leftPos, Time.deltaTime * 10f);
            leftHandIK.rotation = Quaternion.Slerp(leftHandIK.rotation, leftRot, Time.deltaTime * 10f);

            rightHandIK.position = Vector3.Lerp(rightHandIK.position, rightPos, Time.deltaTime * 10f);
            rightHandIK.rotation = Quaternion.Slerp(rightHandIK.rotation, rightRot, Time.deltaTime * 10f);
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting && photonView.IsMine)
        {
            stream.SendNext(headIK.position);
            stream.SendNext(headIK.rotation);
            stream.SendNext(leftHandIK.position);
            stream.SendNext(leftHandIK.rotation);
            stream.SendNext(rightHandIK.position);
            stream.SendNext(rightHandIK.rotation);
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
