using System.Collections.Generic;
using UnityEngine;

public class UnitAnimationManager : Singleton<UnitAnimationManager>, IInitializable
{
    [SerializeField]
    private RuntimeAnimatorController unitAnimator;

    [SerializeField]
    private List<BandAnimationData> bandAnimationDatas = new List<BandAnimationData>();

    private Dictionary<Bandtype, BandAnimationData> bandAnimators =
        new Dictionary<Bandtype, BandAnimationData>();
    private List<Unit> units = new List<Unit>();

    private bool isInitialized = false;

    public bool IsInitialized => isInitialized;

    public void Initialize()
    {
        List<BandAnimationData> RemoveDatas = new List<BandAnimationData>();

        foreach (BandAnimationData bandAnimationData in bandAnimationDatas)
        {
            if (bandAnimators.ContainsKey(bandAnimationData.bandtype))
            {
                RemoveDatas.Add(bandAnimationData);
                continue;
            }
            bandAnimators.Add(bandAnimationData.bandtype, bandAnimationData);
        }

        foreach (BandAnimationData RemoveData in RemoveDatas)
        {
            bandAnimationDatas.Remove(RemoveData);
        }
        NoteGameManager.Instance.onEngagementChange += AnimationClipChange;

        isInitialized = true;

    }

    private void Start()
    {
        Initialize();
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
