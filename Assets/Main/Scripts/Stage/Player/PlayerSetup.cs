using Photon.Pun;
using UnityEngine;

public class PlayerSetup : MonoBehaviourPun
{
    [Header("XR Origin 포함 오브젝트")]
    public GameObject xrOrigin;

    public MonoBehaviour[] localOnlyScripts;
    public GameObject[] localOnlyObjects;

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

            foreach (var obj in localOnlyObjects)
            {
                obj.SetActive(true);
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
