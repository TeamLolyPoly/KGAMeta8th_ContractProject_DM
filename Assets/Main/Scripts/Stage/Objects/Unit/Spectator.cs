using System.Collections;
using UnityEngine;

public class Spectator : Unit
{
    protected override void Initialize()
    {
        base.Initialize();

        unitAnimationSystem.SpectatorDefaultAnimationChange(SetAnimationClip);

        unitAnimationSystem.SpectatorAnimationClipChange(
            GameManager.Instance.ScoreSystem.currentSpectatorEngagement
        );

        StartCoroutine(AnimationPlayRoutine());
    }

    public IEnumerator AnimationPlayRoutine()
    {
        while (true)
        {
            isAnimating = false;
            animator.SetBool("Default", true);

            yield return new WaitForSeconds(0.1f);
            yield return new WaitUntil(() => isAnimating);

            isAnimating = false;
            animator.SetBool("Default", false);

            yield return new WaitForSeconds(0.1f);
            yield return new WaitUntil(() => isAnimating);
        }
    }
}
