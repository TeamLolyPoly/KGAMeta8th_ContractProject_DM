using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class XRPlayer : MonoBehaviour
{
    [SerializeField]
    private ActionBasedController leftController;

    [SerializeField]
    private ActionBasedController rightController;

    [SerializeField]
    private Renderer fadeRenderer;

    private ActionBasedContinuousMoveProvider continuousMoveProvider;
    private ActionBasedSnapTurnProvider snapTurnProvider;
    private GrabMoveProvider grabMoveProvider;
    public ActionBasedController LeftController => leftController;
    public ActionBasedController RightController => rightController;

    public void FadeIn(float duration)
    {
        StartCoroutine(Fade(0, 1, duration));
    }

    public void FadeOut(float duration)
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
