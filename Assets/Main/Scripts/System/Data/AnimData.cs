using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "AnimData", menuName = "Project_DM/Data/Runtime/AnimData")]
public class AnimData : ScriptableObject
{
    [SerializeField]
    public RuntimeAnimatorController unitAnimator;

    [SerializeField]
    public List<BandAnimationData> bandAnimationDatas = new List<BandAnimationData>();

    private void OnValidate()
    {
        Array dataCount = Enum.GetValues(typeof(Bandtype));

        if (bandAnimationDatas.Count < dataCount.Length)
        {
            foreach (Bandtype type in dataCount)
            {
                BandAnimationData data = new BandAnimationData();
                data.bandtype = type;
                bandAnimationDatas.Add(data);
            }
        }
        for (int i = 0; i < dataCount.Length; i++)
        {
            bandAnimationDatas[i].bandtype = (Bandtype)i;
        }
        while (bandAnimationDatas.Count > dataCount.Length)
        {
            bandAnimationDatas.Remove(bandAnimationDatas.Last());
        }
    }
}
