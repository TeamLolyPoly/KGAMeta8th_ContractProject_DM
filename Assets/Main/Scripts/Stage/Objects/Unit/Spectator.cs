
public class Spectator : Unit
{
    protected override void Initialize()
    {
        base.Initialize();

        unitAnimationSystem.SpectatorDefaultAnimationChange(SetAnimationClip);

        animator.SetBool("Default", true);
    }
}
