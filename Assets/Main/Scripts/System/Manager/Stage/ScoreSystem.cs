using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ScoreSystem : MonoBehaviour, IInitializable
{
    private ScoreSettingData scoreSetingData;

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
    public int NoteHitCount { get; private set; } = 0;

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
        scoreSetingData = Resources.Load<ScoreSettingData>("SO/ScoreSettingData");

        ratingCount.Clear();

        if (scoreSetingData != null)
        {
            foreach (NoteRatings rating in Enum.GetValues(typeof(NoteRatings)))
            {
                ratingCount.Add(rating, 0);
            }
            for (int i = 0; i < scoreSetingData.engagementThreshold.Length; i++)
            {
                bandEngagementType.Add(scoreSetingData.engagementThreshold[i], (Engagement)i);
            }
            for (int i = 0; i < scoreSetingData.multiplierScore.Count; i++)
            {
                multiplierScore.Add(
                    scoreSetingData.multiplierScore[i].ratings,
                    scoreSetingData.multiplierScore[i].ratingScore
                );
            }
        }

        onBandEngagementChange?.Invoke(currentBandEngagement = Engagement.First);
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
            NoteHitCount++;
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
    }

    private int SetMultiplier()
    {
        for (int i = 0; i < scoreSetingData.comboMultiplier.Length; i++)
        {
            if (combo < scoreSetingData.comboMultiplier[i])
            {
                return i;
            }
        }
        return scoreSetingData.comboMultiplier.Length;
    }

    private int GetRatingScore(NoteRatings ratings)
    {
        if (multiplierScore.TryGetValue(ratings, out int score))
        {
            return score;
        }
        return 0;
    }

    //TODO: 콤보가 작아지면 디폴트 나가는현상 수정해야함
    private void SetSpectatorEngagement()
    {
        int totalNoteCount = 10; //GameManager.Instance.NoteMap.TotalNoteCount;
        SpectatorEventThreshold newThreshold =
            scoreSetingData.sectatorEventThreshold.LastOrDefault(Threshold =>
                NoteHitCount >= totalNoteCount * Threshold.noteThreshold
                && combo >= Threshold.comboThreshold
            ) ?? scoreSetingData.sectatorEventThreshold.First();

        if (currentSpectatorEngagement != newThreshold.engagement)
        {
            print($"관객 이벤트 발생: {newThreshold.engagement}");
            currentSpectatorEngagement = newThreshold.engagement;
            onSpectatorEngagementChange?.Invoke(currentSpectatorEngagement);
        }
    }

    private void SetBandEngagement()
    {
        Engagement newEngagement = bandEngagementType
            .Where(pair => combo >= pair.Key)
            .OrderByDescending(pair => pair.Key)
            .Select(pair => pair.Value)
            .DefaultIfEmpty(Engagement.First)
            .First();

        if (currentBandEngagement != newEngagement)
        {
            currentBandEngagement = newEngagement;
            onBandEngagementChange.Invoke(currentBandEngagement);
        }
    }
}
