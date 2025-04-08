using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "AnimData", menuName = "Project_DM/Data/Runtime/AnimData")]
public class AnimData : ScriptableObject
{
    [SerializeField]
    public RuntimeAnimatorController UnitAnimator;

    [SerializeField]
    public List<BandAnimationData> bandAnimationDatas = new List<BandAnimationData>();
    public SpectatorAnimData spectatorAnimationData;

    private void OnValidate()
    {
        Array dataCount = Enum.GetValues(typeof(BandType));

        if (bandAnimationDatas.Count < dataCount.Length)
        {
            foreach (BandType type in dataCount)
            {
                BandAnimationData data = new BandAnimationData();
                data.bandType = type;
                bandAnimationDatas.Add(data);
            }
        }
        for (int i = 0; i < dataCount.Length; i++)
        {
            bandAnimationDatas[i].bandType = (BandType)i;
        }
        while (bandAnimationDatas.Count > dataCount.Length)
        {
            bandAnimationDatas.Remove(bandAnimationDatas.Last());
        }
    }
}
