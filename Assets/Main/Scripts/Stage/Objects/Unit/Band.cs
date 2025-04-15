public class Band : Unit
{
    public BandType bandType;

    protected override void Initialize()
    {
        base.Initialize();
        unitAnimationSystem.BandDefaultAnimationChange(this, SetAnimationClip);
        unitAnimationSystem.ChangeAnimation(this, GameManager.Instance.ScoreSystem.currentBandEngagement);
    }
}
