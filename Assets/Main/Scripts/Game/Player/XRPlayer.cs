using System.Collections;
using Photon.Pun;
using Photon.Pun.UtilityScripts;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

public class XRPlayer : MonoBehaviourPun
{
    [SerializeField]
    private ActionBasedController leftController;

    [SerializeField]
    private ActionBasedController rightController;

    [SerializeField]
    private XRRayInteractor leftRayInteractor;

    [SerializeField]
    private XRRayInteractor rightRayInteractor;

    [SerializeField]
    private Renderer fadeRenderer;

    public ActionBasedController LeftController => leftController;
    public ActionBasedController RightController => rightController;

    public XRRayInteractor LeftRayInteractor => leftRayInteractor;
    public XRRayInteractor RightRayInteractor => rightRayInteractor;

    public GameObject xrOrigin;

    public MonoBehaviour[] localOnlyScripts;
    public GameObject[] localOnlyObjects;

    public GameObject[] remoteOnlyObjects;

    private void Awake()
    {
        if (GameManager.Instance.IsInMultiStage)
        {
            Initialize(true);
        }
    }

    public void Initialize(bool isStage)
    {
        if (
            PhotonNetwork.IsConnected
            && PhotonNetwork.InRoom
            && GameManager.Instance.IsInMultiStage
        )
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

                if (!isStage)
                {
                    LeftRayInteractor.enabled = true;
                    RightRayInteractor.enabled = true;
                }
                else
                {
                    LeftRayInteractor.enabled = false;
                    RightRayInteractor.enabled = false;
                }
                if (LeftController != null)
                {
                    LeftController.uiPressAction.action.performed += leftHaptic;
                }

                if (RightController != null)
                {
                    RightController.uiPressAction.action.performed += rightHaptic;
                }

                gameObject.name = "LocalPlayer";
                GameManager.Instance.PlayerSystem.XRPlayer = this;
            }
            else
            {
                Destroy(xrOrigin);

                xrOrigin = null;

                foreach (var script in localOnlyScripts)
                {
                    script.enabled = false;
                }

                foreach (var obj in localOnlyObjects)
                {
                    obj.SetActive(false);
                }

                foreach (var obj in remoteOnlyObjects)
                {
                    obj.SetActive(true);
                }

                gameObject.name = "RemotePlayer";
                GameManager.Instance.PlayerSystem.remotePlayer = this;
            }
        }
        else
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

            if (!isStage)
            {
                LeftRayInteractor.enabled = true;
                RightRayInteractor.enabled = true;
            }
            else
            {
                LeftRayInteractor.enabled = false;
                RightRayInteractor.enabled = false;
            }
            if (LeftController != null)
            {
                LeftController.uiPressAction.action.performed += leftHaptic;
            }

            if (RightController != null)
            {
                RightController.uiPressAction.action.performed += rightHaptic;
            }

            gameObject.name = "LocalPlayer";
        }
    }

    void leftHaptic(InputAction.CallbackContext ctx)
    {
        leftController.SendHapticImpulse(0.7f, 0.2f);
    }

    void rightHaptic(InputAction.CallbackContext ctx)
    {
        rightController.SendHapticImpulse(0.7f, 0.2f);
    }

    private void OnDestroy()
    {
        if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
        {
            if (GameManager.Instance != null && GameManager.Instance.IsInMultiStage)
            {
                if (photonView.IsMine)
                {
                    Debug.Log("[XRPlayer] 로컬 플레이어 Destroy");
                    if (LeftController != null)
                    {
                        LeftRayInteractor.enableUIInteraction = false;
                        LeftController.uiPressAction.action.performed -= leftHaptic;
                    }

                    if (RightController != null)
                    {
                        RightRayInteractor.enableUIInteraction = false;
                        RightController.uiPressAction.action.performed -= rightHaptic;
                    }
                    GameManager.Instance.PlayerSystem.XRPlayer = null;
                }
                else
                {
                    Debug.Log("[XRPlayer] 원격 플레이어 Destroy");
                }
            }
        }
        else
        {
            if (LeftController != null)
            {
                LeftRayInteractor.enableUIInteraction = false;
                LeftController.uiPressAction.action.performed -= leftHaptic;
            }

            if (RightController != null)
            {
                RightRayInteractor.enableUIInteraction = false;
                RightController.uiPressAction.action.performed -= rightHaptic;
            }
        }
    }

    public void FadeOut(float duration)
    {
        StartCoroutine(Fade(0, 1, duration));
    }

    public void FadeIn(float duration)
    {
        StartCoroutine(Fade(1, 0, duration));
    }

    private IEnumerator Fade(float alphaIn, float alphaOut, float duration)
    {
        float time = 0;
        while (time < duration)
        {
            fadeRenderer.material.color = new Color(
                0,
                0,
                0,
                Mathf.Lerp(alphaIn, alphaOut, time / duration)
            );
            time += Time.deltaTime;
            yield return null;
        }
    }
}
