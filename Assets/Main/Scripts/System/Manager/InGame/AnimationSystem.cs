using System.Collections.Generic;
using UnityEngine;

public class AnimationSystem : MonoBehaviour, IInitializable
{
    private AnimData animData;
    private Dictionary<Bandtype, BandAnimationData> bandAnimators =
        new Dictionary<Bandtype, BandAnimationData>();
    private List<Unit> units = new List<Unit>();
    private bool isInitialized = false;
    public bool IsInitialized => isInitialized;

    public void Initialize()
    {
        animData = Resources.Load<AnimData>("SO/AnimData");

        bandAnimators.Clear();

        if (animData != null)
        {
            foreach (BandAnimationData bandAnimationData in animData.bandAnimationDatas)
            {
                bandAnimators.Add(bandAnimationData.bandtype, bandAnimationData);
            }
        }

        GameManager.Instance.ScoreSystem.onBandEngagementChange += AnimationClipChange;

        isInitialized = true;
    }

    public void AddUnit(Unit unit)
    {
        units.Add(unit);
    }

    public void RemoveUnit(Unit unit)
    {
        units.Remove(unit);
    }

    public void AttachAnimation(Animator targetAnimator)
    {
        targetAnimator.runtimeAnimatorController = new AnimatorOverrideController(
            animData.unitAnimator
        );
    }

    public void AnimationClipChange(Engagement engagement)
    {
        foreach (Unit unit in units)
        {
            if (bandAnimators.TryGetValue(unit.bandtype, out BandAnimationData AnimationData))
            {
                if ((int)engagement < AnimationData.animationClip.Length)
                {
                    AnimationClip animationClip = AnimationData?.animationClip[(int)engagement];

                    if (animationClip != null)
                    {
                        unit.SetAnimationClip(animationClip);
                    }
                }
            }
        }
    }
}
