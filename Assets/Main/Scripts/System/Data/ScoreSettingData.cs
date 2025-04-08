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
    public List<int> engagementThreshold = new List<int>();

    [Header("밴드 호응도 변경 인원")]
    public List<int> engagementMemberThreshold = new List<int>();

    [SerializeField, Header("관객 이벤트 활성화 조건")]
    public List<SpectatorEventThreshold> sectatorEventThreshold =
        new List<SpectatorEventThreshold>();

    [Header("노트 정확도 추가점수")]
    [SerializeField]
    public List<MultiplierScore> multiplierScore = new List<MultiplierScore>();

    private void OnValidate()
    {
        int engagementLength = Enum.GetValues(typeof(Engagement)).Length;

        LimitMaxSize(sectatorEventThreshold, engagementLength);
        LimitMaxSize(engagementThreshold, engagementLength);

        SyncListSize<MultiplierScore, NoteRatings>(multiplierScore, () => new MultiplierScore());
        SyncListSize(engagementMemberThreshold, engagementThreshold.Count, () => 0);

        SetEnumValues(multiplierScore, i => multiplierScore[i].ratings = (NoteRatings)i);
        SetEnumValues(
            sectatorEventThreshold,
            i => sectatorEventThreshold[i].engagement = (Engagement)i
        );
    }

    private void SyncListSize<T, TEnum>(List<T> values, Func<T> createDefault)
        where TEnum : Enum
    {
        int Length = Enum.GetValues(typeof(TEnum)).Length;
        SyncListSize(values, Length, createDefault);
    }

    private void SyncListSize<T>(List<T> values, int targetCount, Func<T> createDefault)
    {
        while (values.Count < targetCount)
        {
            values.Add(createDefault());
        }

        while (values.Count > targetCount)
        {
            values.RemoveAt(values.Count - 1);
        }
    }

    private void LimitMaxSize<T>(List<T> values, int maxCount)
    {
        while (values.Count > maxCount)
        {
            values.RemoveAt(values.Count - 1);
        }
    }

    private void SetEnumValues<T>(List<T> values, Action<int> action)
    {
        for (int i = 0; i < values.Count; i++)
        {
            action(i);
        }
    }
}
