using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.UI;

public class XRPlayer : MonoBehaviour
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

    public void Start()
    {
        LeftController.uiPressAction.action.performed += (ctx) =>
        {
            leftController.SendHapticImpulse(0.7f, 0.2f);
        };
        RightController.uiPressAction.action.performed += (ctx) =>
        {
            rightController.SendHapticImpulse(0.7f, 0.2f);
        };
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
