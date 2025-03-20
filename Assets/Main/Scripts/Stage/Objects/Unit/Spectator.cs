using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spectator : Unit
{
    protected override void Initialize()
    {
        base.Initialize();

        unitAnimationSystem.SpectatorDefaultAnimationChange(SetAnimationClip);

        animator.SetBool("Default", true);
    }
}
