using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ScoreSystem : MonoBehaviour, IInitializable
{
    private ScoreSettingData scoreSetingData;

    //각 정확도 기록 딕셔너리
    public Dictionary<NoteRatings, int> ratingComboCount { get; private set; } =
        new Dictionary<NoteRatings, int>();

    //정확도별 추가점수
    public Dictionary<NoteRatings, int> multiplierScore { get; private set; } =
        new Dictionary<NoteRatings, int>();

    //밴드 호응도 딕셔너리
    public Dictionary<int, Engagement> BandengagementType { get; private set; } =
        new Dictionary<int, Engagement>();
    private Engagement currentBandEngagement;

    //점수 배율
    public int Multiplier { get; private set; } = 1;

    //점수
    public float currentScore { get; private set; } = 0;

    //콤보
    public int combo { get; private set; } = 0;

    //최고 콤보
    public int highCombo { get; private set; } = 0;

    //밴드 호응도 이벤트
    public event Action<Engagement> onBandEngagementChange;

    private bool isInitialized = false;

    public bool IsInitialized => isInitialized;

    public void Initialize()
    {
        scoreSetingData = Resources.Load<ScoreSettingData>("SO/ScoreSettingData");

        ratingComboCount.Clear();

        if (scoreSetingData != null)
        {
            foreach (NoteRatings rating in Enum.GetValues(typeof(NoteRatings)))
            {
                ratingComboCount.Add(rating, 0);
            }
            for (int i = 0; i < scoreSetingData.engagementThreshold.Length; i++)
            {
                BandengagementType.Add(scoreSetingData.engagementThreshold[i], (Engagement)i);
            }
            for (int i = 0; i < scoreSetingData.multiplierScore.Count; i++)
            {
                multiplierScore.Add(
                    scoreSetingData.multiplierScore[i].ratings,
                    scoreSetingData.multiplierScore[i].ratingScore
                );
            }
        }

        onBandEngagementChange.Invoke(Engagement.First);
    }

    public void SetScore(float score, NoteRatings ratings)
    {
        ratingComboCount[ratings] += 1;
        if (score <= 0 || ratings == NoteRatings.Miss)
        {
            Multiplier = 1;
            combo = 0;
            print($"combo: {combo} \ncurrentScore: {currentScore}");
            return;
        }
        combo += 1;
        if (combo > highCombo)
        {
            highCombo = combo;
        }
        SetBandEngagement();
        int ratingScore = GetRatingScore(ratings);
        Multiplier = SetMultiplier();

        currentScore += (score * Multiplier) + ratingScore;

        print($"ratingScore: {ratingScore}");
        print($"currentScore: {currentScore}");
        print($"Multiplier: {Multiplier}");
        print($"combo: {combo}");
        print($"combo: {combo} \ncurrentScore: {currentScore}");
    }

    private int SetMultiplier()
    {
        for (int i = 0; i < scoreSetingData.comboMultiplier.Length; i++)
        {
            if (combo > scoreSetingData.comboMultiplier[i])
            {
                return i + 1;
            }
        }
        return 1;
    }

    private int GetRatingScore(NoteRatings ratings)
    {
        if (multiplierScore.TryGetValue(ratings, out int score))
        {
            return score;
        }
        return 0;
    }

    private void SetBandEngagement()
    {
        Engagement newEngagement = BandengagementType
            .Where(pair => combo > pair.Key)
            .OrderByDescending(pair => pair.Key)
            .Select(pair => pair.Value)
            .DefaultIfEmpty(Engagement.First)
            .First();

        if (currentBandEngagement != newEngagement)
        {
            onBandEngagementChange.Invoke(newEngagement);
            currentBandEngagement = newEngagement;
        }
    }
}
