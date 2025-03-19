using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class AnimationSystem : MonoBehaviour, IInitializable
{
    private AnimData AnimData;
    private Dictionary<BandType, BandAnimationData> bandAnimators =
        new Dictionary<BandType, BandAnimationData>();

    //Unit을 상속받지만 불필요한 연산을 막기위해 타입캐스팅을 미리하여서 관리
    private List<Band> Bands = new List<Band>();
    private List<Spectator> spectators = new List<Spectator>();
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
        GameManager.Instance.ScoreSystem.onSpectatorEngagementChange +=
            SpectatorAnimationClipChange;

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
                action(defaultClip, "default");
            }
        }
    }

    public void SpectatorDefaultAnimationChange(Action<AnimationClip, string> action)
    {
        int index = Random.Range(0, AnimData.spectatorAnimationData.RandomAnima.Count);

        AnimationClip defaultClip = AnimData.spectatorAnimationData.RandomAnima[index];
        if (defaultClip != null)
        {
            action(defaultClip, "default");
        }
    }

    public void BandAnimationClipChange(Engagement engagement)
    {
        foreach (Band band in Bands)
        {
            if (bandAnimators.TryGetValue(band.bandType, out BandAnimationData AnimationData))
            {
                if ((int)engagement >= AnimationData.animationClip.Length)
                    continue;
                AnimationClip animationClip = AnimationData?.animationClip[(int)engagement];

                if (animationClip == null)
                    continue;
                band.SetAnimationClip(animationClip, "Usual");
            }
        }
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
