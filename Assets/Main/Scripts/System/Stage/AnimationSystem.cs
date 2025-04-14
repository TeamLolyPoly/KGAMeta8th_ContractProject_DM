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

    //씬위에 올라와있는 밴드와 관객
    private List<Band> bands = new List<Band>();
    private List<Band> remoteBands = new List<Band>();
    private List<Spectator> spectators = new List<Spectator>();

    //디폴트 호응도
    private Engagement? defaultEngagement;

    private bool isInitialized = false;
    public bool IsInitialized => isInitialized;

    public IEnumerator InitializeRoutine()
    {
        yield return new WaitUntil(
            () =>
                GameManager.Instance.ScoreSystem != null
                && GameManager.Instance.ScoreSystem.IsInitialized
        );
        Initialize();
    }

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
            bands.Add(band);
        }
        else if (unit is Spectator spectator)
        {
            spectators.Add(spectator);
        }
    }

    public void AddRemoteUnit(Unit unit)
    {
        if (unit is Band band)
        {
            remoteBands.Add(band);
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
        print($"defaultEngagement : {defaultEngagement.Value}");
        if (bands == null || bands.Count == 0)
            return;

        foreach (Band band in bands)
        {
            ChangeAnimation(band, defaultEngagement.Value);
        }

        //var indices = Enumerable.Range(0, bands.Count).ToList();
        List<int> randomBand = new List<int>();
        if (numberOfUnits > 0)
        {
            for (int i = 0; i < bands.Count; i++)
            {
                randomBand.Add(i);
            }

            for (int i = 0; i < numberOfUnits && randomBand.Count > 0; i++)
            {
                int randomIndex = Random.Range(0, randomBand.Count);
                int selectedBandIndex = randomBand[randomIndex];
                randomBand.RemoveAt(randomIndex);
                ChangeAnimation(bands[selectedBandIndex], engagement);
            }
        }
        else if (numberOfUnits == 0)
        {
            foreach (Band band in bands)
            {
                ChangeAnimation(band, engagement);
            }
        }
    }

    public void RemoteBandAnimationClipChange(Engagement engagement, int numberOfUnits = 0)
    {
        if (defaultEngagement == null)
        {
            defaultEngagement = engagement;
        }
        print($"defaultEngagement : {defaultEngagement.Value}");
        if (bands == null || bands.Count == 0)
            return;

        foreach (Band band in remoteBands)
        {
            ChangeAnimation(band, defaultEngagement.Value);
        }

        //var indices = Enumerable.Range(0, bands.Count).ToList();
        List<int> randomBand = new List<int>();
        if (numberOfUnits > 0)
        {
            for (int i = 0; i < remoteBands.Count; i++)
            {
                randomBand.Add(i);
            }

            for (int i = 0; i < numberOfUnits && randomBand.Count > 0; i++)
            {
                int randomIndex = Random.Range(0, randomBand.Count);
                int selectedBandIndex = randomBand[randomIndex];
                randomBand.RemoveAt(randomIndex);
                ChangeAnimation(remoteBands[selectedBandIndex], engagement);
            }
        }
        else if (numberOfUnits == 0)
        {
            foreach (Band band in remoteBands)
            {
                ChangeAnimation(band, engagement);
            }
        }
    }

    public void ChangeAnimation(Band band, Engagement engagement)
    {
        if (!bandAnimators.TryGetValue(band.bandType, out BandAnimationData animationData))
            return;

        if ((int)engagement >= animationData.animationClip.Length)
            return;

        AnimationClip animationClip = animationData.animationClip[(int)engagement];
        if (animationClip == null)
            return;

        band.SetAnimationClip(animationClip, "Usual");
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

    private void CleanUp()
    {
        if (GameManager.Instance != null && GameManager.Instance.ScoreSystem != null)
        {
            GameManager.Instance.ScoreSystem.onBandEngagementChange -= BandAnimationClipChange;
            GameManager.Instance.ScoreSystem.onSpectatorEngagementChange -=
                SpectatorAnimationClipChange;
        }
    }

    private void OnDestroy()
    {
        CleanUp();
    }
}
