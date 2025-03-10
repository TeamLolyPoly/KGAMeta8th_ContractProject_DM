using System.Collections.Generic;
using UnityEngine;

public class UnitAnimationManager : Singleton<UnitAnimationManager>
{
    [SerializeField]
    private RuntimeAnimatorController unitAnimator;

    [SerializeField]
    private BandAnimationData[] bandAnimationDatas;

    private Dictionary<Bandtype, BandAnimationData> bandAnimators =
        new Dictionary<Bandtype, BandAnimationData>();
    private List<Unit> units = new List<Unit>();

    protected override void Awake()
    {
        base.Awake();
        foreach (BandAnimationData bandAnimationData in bandAnimationDatas)
        {
            bandAnimators.Add(bandAnimationData.bandtype, bandAnimationData);
        }
    }

    private void Start()
    {
        NoteGameManager.instance.onEngagementChange += AnimationClipChange;
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
        targetAnimator.runtimeAnimatorController = new AnimatorOverrideController(unitAnimator);
    }

    public void AnimationClipChange(int engagement)
    {
        foreach (Unit unit in units)
        {
            unit.GetAnimator()["Walk"] = bandAnimators[unit.bandtype]?.animationClip[engagement];
        }
    }
}
