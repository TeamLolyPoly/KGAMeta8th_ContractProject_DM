using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ScoreSystem : MonoBehaviour, IInitializable
{
    private ScoreSettingData scoreSettingData;

    //정확도 기록 딕셔너리
    public Dictionary<NoteRatings, int> ratingCount { get; private set; } =
        new Dictionary<NoteRatings, int>();

    //정확도별 추가점수
    public Dictionary<NoteRatings, int> multiplierScore { get; private set; } =
        new Dictionary<NoteRatings, int>();

    //밴드 호응도 딕셔너리
    public Dictionary<int, Engagement> bandEngagementType { get; private set; } =
        new Dictionary<int, Engagement>();
    private Engagement currentBandEngagement;
    private Engagement currentSpectatorEngagement;

    //점수 배율
    public int multiplier { get; private set; } = 1;

    //점수
    public float currentScore { get; private set; } = 0;

    //콤보
    public int combo { get; private set; } = 0;

    //최고 콤보
    public int highCombo { get; private set; } = 0;

    //Miss제외한 노트 Hit 총 횟수
    public int noteHitCount { get; private set; } = 0;

    //밴드 호응도 이벤트
    public event Action<Engagement> onBandEngagementChange;
    //관객 호응도 이벤트
    public event Action<Engagement> onSpectatorEngagementChange;

    private bool isInitialized = false;

    public bool IsInitialized => isInitialized;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            SetScore(100, NoteRatings.Perfect);
        }
        if (Input.GetKeyDown(KeyCode.W))
        {
            SetScore(0, NoteRatings.Miss);
        }
    }
    public void Initialize()
    {
        scoreSettingData = Resources.Load<ScoreSettingData>("SO/ScoreSettingData");

        ratingCount.Clear();

        if (scoreSettingData != null)
        {
            foreach (NoteRatings rating in Enum.GetValues(typeof(NoteRatings)))
            {
                ratingCount.Add(rating, 0);
            }
            for (int i = 0; i < scoreSettingData.engagementThreshold.Length; i++)
            {
                bandEngagementType.Add(scoreSettingData.engagementThreshold[i], (Engagement)i);
            }
            for (int i = 0; i < scoreSettingData.multiplierScore.Count; i++)
            {
                multiplierScore.Add(
                    scoreSettingData.multiplierScore[i].ratings,
                    scoreSettingData.multiplierScore[i].ratingScore
                );
            }
        }

        SetBandEngagement();
        onSpectatorEngagementChange?.Invoke(currentSpectatorEngagement = Engagement.First);
    }

    public void SetScore(float score, NoteRatings ratings)
    {
        ratingCount[ratings] += 1;
        if (score <= 0 || ratings == NoteRatings.Miss)
        {
            combo = 0 < combo ? 0 : combo - 1;
            multiplier = 1;
        }
        else
        {
            combo = 0 <= combo ? combo + 1 : 1;
            noteHitCount++;
            if (combo > highCombo)
            {
                highCombo = combo;
            }
            int ratingScore = GetRatingScore(ratings);
            print($"ratingScore: {ratingScore}");
            multiplier = SetMultiplier();

            currentScore += (score * multiplier) + ratingScore;
        }
        SetBandEngagement();
        SetSpectatorEngagement();

        print($"currentScore: {currentScore}");
        print($"Multiplier: {multiplier}");
        print($"combo: {combo}");
        print($"higtcombo: {highCombo}");
        print($"NoteHitCount: {noteHitCount}");
    }

    private int SetMultiplier()
    {
        for (int i = 0; i < scoreSettingData.comboMultiplier.Length; i++)
        {
            if (combo < scoreSettingData.comboMultiplier[i])
            {
                return i;
            }
        }
        return scoreSettingData.comboMultiplier.Length;
    }

    private int GetRatingScore(NoteRatings ratings)
    {
        if (multiplierScore.TryGetValue(ratings, out int score))
        {
            return score;
        }
        return 0;
    }

    private void SetSpectatorEngagement()
    {
        int totalNoteCount = 10;//GameManager.Instance.NoteMap.TotalNoteCount;
        SpectatorEventThreshold newThreshold = scoreSettingData.sectatorEventThreshold
        .LastOrDefault(threshold => CheckEngagement(threshold, totalNoteCount))
        ?? scoreSettingData.sectatorEventThreshold.First();
        print($"combo: {combo}\n NoteHitCount: {noteHitCount}\n newThreshold: {newThreshold.comboThreshold} , {newThreshold.noteThreshold} \n totalNoteCount: {totalNoteCount * newThreshold.noteThreshold}");

        if (currentSpectatorEngagement != newThreshold.engagement)

        {
            print($"관객 이벤트 발생: {newThreshold.engagement}");
            currentSpectatorEngagement = newThreshold.engagement;
            onSpectatorEngagementChange?.Invoke(currentSpectatorEngagement);
        }
    }
    private bool CheckEngagement(SpectatorEventThreshold threshold, int totalNoteCount)
    {
        bool isOverCount = noteHitCount >= totalNoteCount * threshold.noteThreshold;
        bool isOverCombo = combo >= threshold.comboThreshold || threshold.comboThreshold <= 0;
        return isOverCount && isOverCombo;
    }

    private void SetBandEngagement()
    {
        var selectedEngagement = bandEngagementType.Where(pair => IsCombo(pair.Key));

        var sortedEngagement = selectedEngagement.OrderByDescending(pair => Math.Abs(pair.Key));

        Engagement newEngagement = sortedEngagement
            .Select(pair => pair.Value)
            .DefaultIfEmpty(bandEngagementType[0])
            .First();

        if (currentBandEngagement != newEngagement)
        {
            print($"밴드 이벤트 발생: {newEngagement}");
            currentBandEngagement = newEngagement;
            onBandEngagementChange.Invoke(currentBandEngagement);
        }
    }

    private bool IsPlus(int num) => num >= 0;
    private bool IsCombo(int num)
    {
        if (IsPlus(num))
            return combo >= num;
        else
            return combo <= num;
    }
}
