using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitManager : Singleton<UnitManager>
{
    [SerializeField]
    private RuntimeAnimatorController Animator;

    [SerializeField]
    private AnimationClip[] audienceAnimationClip;

    [SerializeField]
    private AnimationClip[] bandAnimationClip;
    private List<Unit> units = new List<Unit>();

    public void AddUnit(Unit unit)
    {
        units.Add(unit);
    }

    public void RemoveUnit(Unit unit)
    {
        units.Remove(unit);
    }

    public void AttachAnimation(Animator unitAnimator, Unit unit)
    {
        unitAnimator.runtimeAnimatorController = new AnimatorOverrideController(Animator);
    }

    public void AnimationClipChange(int engagement)
    {
        bool bandClipCheck = false;
        bool audienceClipCheck = false;
        if (audienceAnimationClip[engagement])
            audienceClipCheck = true;
        if (bandAnimationClip[engagement])
            bandClipCheck = true;
        foreach (Unit unit in units)
        {
            if (unit is Audience && audienceClipCheck)
            {
                unit.GetAnimator()["Walk"] = audienceAnimationClip[engagement];
            }
            else if (unit is Band && bandClipCheck)
            {
                unit.GetAnimator()["Walk"] = bandAnimationClip[engagement];
            }
        }
    }
}
