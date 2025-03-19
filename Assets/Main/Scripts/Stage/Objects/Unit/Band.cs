using System.Collections;
using UnityEngine;

public class Band : Unit
{
    public BandType bandType;

    protected override void Initialize()
    {
        base.Initialize();

        unitAnimationManager.BandWalkAnimationChange(bandType, SetAnimationClip);
    }
}
