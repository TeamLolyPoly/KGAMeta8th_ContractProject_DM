using Photon.Pun;
using UnityEngine;

public class TestPlayerController : MonoBehaviourPun
{
    public float moveSpeed = 5f;

    void Update()
    {
        if (photonView.IsMine) //본인의 플레이어만 이동 가능
        {
            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");
            transform.Translate(new Vector3(h, 0, v) * moveSpeed * Time.deltaTime);
        }
    }
}
