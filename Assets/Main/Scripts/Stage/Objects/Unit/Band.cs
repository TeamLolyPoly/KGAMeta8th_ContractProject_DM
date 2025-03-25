
public class Band : Unit
{
    public BandType bandType;
    protected override void Initialize()
    {
        base.Initialize();

        unitAnimationSystem.BandDefaultAnimationChange(this, SetAnimationClip);

        unitAnimationSystem.BandAnimationClipChange(GameManager.Instance.ScoreSystem.currentBandEngagement);

    }
}
