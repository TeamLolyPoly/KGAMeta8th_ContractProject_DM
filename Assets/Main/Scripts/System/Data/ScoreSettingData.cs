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

    [Header("밴드 호응도 콤보 기준")]
    [SerializeField]
    public int[] engagementThreshold;
    [SerializeField, Header("관객 이벤트 활성화 조건")]
    public List<SpectatorEventThreshold> sectatorEventThreshold = new List<SpectatorEventThreshold>();

    [Header("노트 정확도 추가점수")]
    [SerializeField]
    public List<MultiplierScore> multiplierScore = new List<MultiplierScore>();

    private void OnValidate()
    {
        Array NoteRatingCount = Enum.GetValues(typeof(NoteRatings));
        Array EngagementCount = Enum.GetValues(typeof(Engagement));

        if (multiplierScore.Count < NoteRatingCount.Length)
        {
            foreach (NoteRatings type in NoteRatingCount)
            {
                MultiplierScore data = new MultiplierScore();
                data.ratings = type;
                multiplierScore.Add(data);
            }
        }
        while (multiplierScore.Count > NoteRatingCount.Length)
        {
            multiplierScore.Remove(multiplierScore.Last());
        }
        while (sectatorEventThreshold.Count > EngagementCount.Length)
        {
            sectatorEventThreshold.Remove(sectatorEventThreshold.Last());
        }

        for (int i = 0; i < NoteRatingCount.Length; i++)
        {
            multiplierScore[i].ratings = (NoteRatings)i;
        }
        for (int i = 0; i < sectatorEventThreshold.Count; i++)
        {
            sectatorEventThreshold[i].engagement = (Engagement)i;
        }
    }

    // 어렵다
    // private void SyncListWithEnum<T, TEnum>(List<T> list, Func<TEnum, T> createElement)
    //  where T : class
    //  where TEnum : Enum
    // {
    //     var enumValues = Enum.GetValues(typeof(TEnum));

    //     // 리스트 초기화
    //     list.Clear();

    //     // enum 값들로 리스트 채우기
    //     foreach (TEnum enumValue in enumValues)
    //     {
    //         list.Add(createElement(enumValue));
    //     }

    //     while (list.Count > enumValues.Length)
    //     {
    //         list.RemoveAt(list.Count - 1);
    //     }
    // }
}
