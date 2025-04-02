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
    private List<Band> bands = new List<Band>();
    private List<Spectator> spectators = new List<Spectator>();

    //디폴트 호응도
    private Engagement? defaultEngagement;

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
            bands.Add(band);
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
            bands.Remove(band);
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

        foreach (Band band in bands)
        {
            if (bandAnimators.TryGetValue(band.bandType, out BandAnimationData animationData))
            {
                if ((int)defaultEngagement >= animationData.animationClip.Length)
                    continue;
                AnimationClip animationClip = animationData.animationClip[(int)defaultEngagement];

                if (animationClip == null)
                    continue;
                band.SetAnimationClip(animationClip, "Usual");
            }
        }

        List<int> ints = new List<int>();
        if (numberOfUnits != 0)
        {
            for (int i = 0; i < bands.Count; i++)
            {
                ints.Add(i);
            }

            for (int i = 0; i < numberOfUnits; i++)
            {
                int a = ints[Random.Range(0, ints.Count)];
                ints.RemoveAt(a);

                if (bandAnimators.TryGetValue(bands[a].bandType, out BandAnimationData animationData))
                {
                    if ((int)engagement >= animationData.animationClip.Length)
                        continue;
                    AnimationClip animationClip = animationData.animationClip[(int)engagement];

                    if (animationClip == null)
                        continue;
                    bands[a].SetAnimationClip(animationClip, "Usual");
                }
            }
        }
        else
        {
            foreach (Band band in bands)
            {
                if (bandAnimators.TryGetValue(band.bandType, out BandAnimationData animationData))
                {
                    if ((int)engagement >= animationData.animationClip.Length)
                        continue;
                    AnimationClip animationClip = animationData.animationClip[(int)engagement];

                    if (animationClip == null)
                        continue;
                    band.SetAnimationClip(animationClip, "Usual");
                }
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
