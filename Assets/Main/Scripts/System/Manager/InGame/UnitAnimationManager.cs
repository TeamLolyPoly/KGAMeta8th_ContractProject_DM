using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UnitAnimationManager : Singleton<UnitAnimationManager>, IInitializable
{
    [SerializeField]
    private RuntimeAnimatorController unitAnimator;
    [SerializeField]
    private List<BandAnimationData> bandAnimationDatas = new List<BandAnimationData>();
    private Dictionary<Bandtype, BandAnimationData> bandAnimators = new Dictionary<Bandtype, BandAnimationData>();
    private List<Unit> units = new List<Unit>();
    private bool isInitialized = false;
    public bool IsInitialized => isInitialized;
    private void OnValidate()
    {

        Array dataCount = Enum.GetValues(typeof(Bandtype));

        if (bandAnimationDatas.Count < dataCount.Length)
        {
            foreach (Bandtype type in dataCount)
            {
                BandAnimationData data = new BandAnimationData();
                data.bandtype = type;
                bandAnimationDatas.Add(data);
            }
        }
        for (int i = 0; i < dataCount.Length; i++)
        {
            bandAnimationDatas[i].bandtype = (Bandtype)i;
        }
        while (bandAnimationDatas.Count > dataCount.Length)
        {
            bandAnimationDatas.Remove(bandAnimationDatas.Last());
        }
    }
    public void Initialize()
    {
        bandAnimators.Clear();

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

    //TODO: 애니메이션 클립명 변경 필요함
    public void AnimationClipChange(Engagement engagement)
    {
        foreach (Unit unit in units)
        {
            if (bandAnimators.TryGetValue(unit.bandtype, out BandAnimationData AnimationData))
            {
                if ((int)engagement + 1 <= AnimationData.animationClip.Length)
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
