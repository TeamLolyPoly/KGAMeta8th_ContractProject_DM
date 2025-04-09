using System.Collections.Generic;
using UnityEngine;

public class Band : Unit
{
    public BandType bandType;
    private Animator bandAnimator;
    private Engagement lastScore = (Engagement)(-1);

    protected override void Initialize()
    {
        base.Initialize();

        unitAnimationSystem.BandDefaultAnimationChange(this, SetAnimationClip);
        UpdateAnimationBasedOnScore();
    }

    private void Update()
    {
        if (bandAnimator == null)
            return;

        Engagement currentScore = GameManager.Instance.ScoreSystem.currentBandEngagement;
        if (currentScore != lastScore)
        {
            UpdateAnimationBasedOnScore();
            lastScore = currentScore;
        }
    }

    private void UpdateAnimationBasedOnScore()
    {
        if (bandAnimator == null)
            return;

        unitAnimationSystem.BandAnimationClipChange(
            GameManager.Instance.ScoreSystem.currentBandEngagement
        );
    }
}
