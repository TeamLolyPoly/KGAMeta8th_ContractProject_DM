using Photon.Pun;
using UnityEngine;

public class SpawnTestStarter : MonoBehaviour
{
    public void OnStartButtonClicked()
    {
        SpawnTest.Instance.CheckXRDevice();

        // 포톤 방에 이미 들어가 있다면 씬 전환
        if (PhotonNetwork.IsConnectedAndReady)
        {
            PhotonNetwork.LoadLevel("XRTest2");
        }
        else
        {
            Debug.LogWarning("Photon에 연결되지 않았습니다.");
        }
    }
}
