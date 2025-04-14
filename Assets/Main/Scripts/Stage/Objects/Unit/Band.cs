using System.Collections.Generic;
using UnityEngine;

public class Band : Unit
{
    public BandType bandType;
    
    protected override void Initialize()
    {
        base.Initialize();

        unitAnimationSystem.BandDefaultAnimationChange(this, SetAnimationClip);
    }
}
