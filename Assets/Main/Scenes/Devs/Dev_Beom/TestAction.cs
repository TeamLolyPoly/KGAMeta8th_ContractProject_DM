using Photon.Pun;
using UnityEngine;

public class TestAction : MonoBehaviourPun
{
    [PunRPC] //다른 클라이언트가 호출할 수 있는 함수
    public void TakeDamage(int damage)
    {
        Debug.Log("플레이어가 " + damage + " 피해를 입음");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            photonView.RPC("TakeDamage", RpcTarget.All, 10); //모든 클라이언트에 피해 적용
        }
    }
}
