using Photon.Pun;
using UnityEngine;

public class PlayerSetup : MonoBehaviourPun
{
    [Header("XR Origin 포함 오브젝트")]
    public GameObject xrOrigin;

    [Header("로컬 전용 컴포넌트")]
    public MonoBehaviour[] localOnlyScripts;

    [Header("원격 전용 오브젝트")]
    public GameObject[] remoteOnlyObjects;

    private void Start()
    {
        if (photonView.IsMine)
        {
            xrOrigin.SetActive(true);

            foreach (var script in localOnlyScripts)
            {
                script.enabled = true;
            }

            foreach (var obj in remoteOnlyObjects)
            {
                obj.SetActive(false);
            }

            gameObject.name = "LocalPlayer";
        }
        else
        {
            xrOrigin.SetActive(false);

            foreach (var script in localOnlyScripts)
            {
                script.enabled = false;
            }

            foreach (var obj in remoteOnlyObjects)
            {
                obj.SetActive(true);
            }

            gameObject.name = "RemotePlayer";
        }
    }
}
