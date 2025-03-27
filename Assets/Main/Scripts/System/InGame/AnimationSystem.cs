using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class AnimationSystem : MonoBehaviour, IInitializable
{
    private AnimData AnimData;
    private Dictionary<BandType, BandAnimationData> bandAnimators =
        new Dictionary<BandType, BandAnimationData>();

    //씬위에 올라와있는 밴드와 관객
    private List<Band> Bands = new List<Band>();
    private List<Spectator> spectators = new List<Spectator>();

    //이전에 클립이 변경된 밴드
    private HashSet<Band> changeBands = new HashSet<Band>();
    private Engagement? defaultEngagement;
    private int currentnumberOfUnits = 0;
    private bool isInitialized = false;
    public bool IsInitialized => isInitialized;

    public void Initialize()
    {
        AnimData = Resources.Load<AnimData>("SO/BandAnimData");

        bandAnimators.Clear();

        if (AnimData != null)
        {
            foreach (BandAnimationData bandAnimationData in AnimData.bandAnimationDatas)
            {
                bandAnimators.Add(bandAnimationData.bandType, bandAnimationData);
            }
        }

        GameManager.Instance.ScoreSystem.onBandEngagementChange += BandAnimationClipChange;
        GameManager.Instance.ScoreSystem.onSpectatorEngagementChange += SpectatorAnimationClipChange;

        isInitialized = true;
    }

    public void AddUnit(Unit unit)
    {
        if (unit is Band band)
        {
            Bands.Add(band);
        }
        else if (unit is Spectator spectator)
        {
            spectators.Add(spectator);
        }
    }

    public void RemoveUnit(Unit unit)
    {
        if (unit is Band band)
        {
            Bands.Remove(band);
        }
        else if (unit is Spectator spectator)
        {
            spectators.Remove(spectator);
        }
    }

    public void AttachAnimation(Animator targetAnimator)
    {
        targetAnimator.runtimeAnimatorController = new AnimatorOverrideController(
            AnimData.UnitAnimator
        );
    }

    public void BandDefaultAnimationChange(Band band, Action<AnimationClip, string> action)
    {
        if (bandAnimators.TryGetValue(band.bandType, out BandAnimationData AnimationData))
        {
            AnimationClip defaultClip = AnimationData?.MoveClip;
            if (defaultClip != null)
            {
                action(defaultClip, "Default");
            }
        }
    }

    public void SpectatorDefaultAnimationChange(Action<AnimationClip, string> action)
    {
        int index = Random.Range(0, AnimData.spectatorAnimationData.RandomAnima.Count);

        AnimationClip defaultClip = AnimData.spectatorAnimationData.RandomAnima[index];
        if (defaultClip != null)
        {
            action(defaultClip, "Default");
        }
    }

    /// <summary>
    /// 밴드 전체변경은 numberOfUnits 0 으로 실행
    /// </summary>
    /// <param name="engagement"></param>
    /// <param name="numberOfUnits"></param>
    public void BandAnimationClipChange(Engagement engagement, int numberOfUnits = 0)
    {
        if (defaultEngagement == null)
        {
            defaultEngagement = engagement;
        }
        else if (defaultEngagement == engagement)
        {
            currentnumberOfUnits = 0;
            changeBands.Clear();
        }
        if (numberOfUnits == 0)
        {
            numberOfUnits = 100000000;
        }
        else
        {
            int a = numberOfUnits - currentnumberOfUnits;
            currentnumberOfUnits = numberOfUnits;
            numberOfUnits = a;
        }

        HashSet<int> aa = new HashSet<int>();

        numberOfUnits = Mathf.Min(numberOfUnits, Bands.Count);

        while (aa.Count < numberOfUnits)
        {
            aa.Add(Random.Range(0, numberOfUnits));
        }
        foreach (int i in aa)
        {
            if (changeBands.Contains(Bands[i]))
                continue;

            if (bandAnimators.TryGetValue(Bands[i].bandType, out BandAnimationData animationData))
            {
                if ((int)engagement >= animationData.animationClip.Length)
                    continue;
                AnimationClip animationClip = animationData.animationClip[(int)engagement];

                if (animationClip == null)
                    continue;
                Bands[i].SetAnimationClip(animationClip, "Usual");

                changeBands.Add(Bands[i]);
            }
        }
        // foreach (Band band in Bands)
        // {
        //     if (changeBands.Contains(band))
        //         continue;

        //     if (bandAnimators.TryGetValue(band.bandType, out BandAnimationData animationData))
        //     {
        //         if ((int)engagement >= animationData.animationClip.Length)
        //             continue;
        //         AnimationClip animationClip = animationData.animationClip[(int)engagement];

        //         if (animationClip == null)
        //             continue;
        //         band.SetAnimationClip(animationClip, "Usual");
        //         changeBands.Add(band);
        //     }
        // }
    }

    public void SpectatorAnimationClipChange(Engagement engagement)
    {
        foreach (Spectator spectator in spectators)
        {
            if ((int)engagement >= AnimData.spectatorAnimationData.engagementClip.Count)
                continue;

            AnimationClip animationClip = AnimData
                ?.spectatorAnimationData
                .engagementClip[(int)engagement];

            if (animationClip == null)
                continue;
            spectator.SetAnimationClip(animationClip, "Usual");
        }
    }
}
