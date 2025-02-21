using Photon.Pun;
using UnityEngine;

public class TestAction : MonoBehaviourPun
{
    [PunRPC] //�ٸ� Ŭ���̾�Ʈ�� ȣ���� �� �ִ� �Լ�
    public void TakeDamage(int damage)
    {
        Debug.Log("�÷��̾ " + damage + " ���ظ� ����");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            photonView.RPC("TakeDamage", RpcTarget.All, 10); //��� Ŭ���̾�Ʈ�� ���� ����
        }
    }
}
