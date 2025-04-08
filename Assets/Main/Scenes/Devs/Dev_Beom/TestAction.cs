using Photon.Pun;
using UnityEngine;

public class TestAction : MonoBehaviourPun
{
    [PunRPC]
    public void TakeDamage(int damage)
    {
        Debug.Log("플레이어가 " + damage + " 데미지를 입었습니다.");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            photonView.RPC("TakeDamage", RpcTarget.All, 10);
        }
    }
}
