using System.Collections;
using Photon.Pun;
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
                Debug.Log(
                    $"[XRPlayer] This is MY player! Setting up local player components. IsStage={isStage}"
                );

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
                    Debug.Log($"[XRPlayer] Disabled remote-only object: {obj.name}");
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
                Debug.Log(
                    $"[XRPlayer] Local player initialized at position {transform.position}, name set to: {gameObject.name}"
                );
            }
            else
            {
                Debug.Log(
                    $"[XRPlayer] This is a REMOTE player! Setting up remote player components."
                );

                xrOrigin.SetActive(false);

                foreach (var script in localOnlyScripts)
                {
                    script.enabled = false;
                    Debug.Log($"[XRPlayer] Disabled local-only script: {script.GetType().Name}");
                }

                foreach (var obj in localOnlyObjects)
                {
                    obj.SetActive(false);
                    Debug.Log($"[XRPlayer] Disabled local-only object: {obj.name}");
                }

                foreach (var obj in remoteOnlyObjects)
                {
                    obj.SetActive(true);
                    Debug.Log($"[XRPlayer] Enabled remote-only object: {obj.name}");
                }

                gameObject.name = "RemotePlayer";
                GameManager.Instance.PlayerSystem.remotePlayer = this;
                Debug.Log(
                    $"[XRPlayer] Remote player initialized at position {transform.position}, name set to: {gameObject.name}"
                );
            }
        }
        else
        {
            Debug.Log(
                $"[XRPlayer] Offline mode. Setting up single player components. IsStage={isStage}"
            );

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
            Debug.Log(
                $"[XRPlayer] Offline player initialized at position {transform.position}, name set to: {gameObject.name}"
            );
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

    void OnDestroy()
    {
        if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
        {
            if (GameManager.Instance != null && GameManager.Instance.IsInMultiStage)
            {
                if (photonView.IsMine)
                {
                    if (LeftController != null)
                    {
                        LeftController.uiPressAction.action.performed -= leftHaptic;
                    }

                    if (RightController != null)
                    {
                        RightController.uiPressAction.action.performed -= rightHaptic;
                    }
                }
            }
        }
        else
        {
            if (LeftController != null)
            {
                LeftController.uiPressAction.action.performed -= leftHaptic;
            }

            if (RightController != null)
            {
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
