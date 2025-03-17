using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "ScoreSettingData", menuName = "Project_DM/Data/Runtime/ScoreData")]
public class ScoreSettingData : ScriptableObject
{
    [Header("콤보 배율 기준")]
    [SerializeField]
    public int[] comboMultiplier;

    [Header("호응도 콤보 기준")]
    [SerializeField]
    public int[] engagementThreshold;

    [Header("노트 정확도 추가점수")]
    [SerializeField]
    public List<MultiplierScore> multiplierScore = new List<MultiplierScore>();

    private void OnValidate()
    {
        Array dataCount = Enum.GetValues(typeof(NoteRatings));

        if (multiplierScore.Count < dataCount.Length)
        {
            foreach (NoteRatings type in dataCount)
            {
                MultiplierScore data = new MultiplierScore();
                data.ratings = type;
                multiplierScore.Add(data);
            }
        }
        for (int i = 0; i < dataCount.Length; i++)
        {
            multiplierScore[i].ratings = (NoteRatings)i;
        }
        while (multiplierScore.Count > dataCount.Length)
        {
            multiplierScore.Remove(multiplierScore.Last());
        }
    }
}
