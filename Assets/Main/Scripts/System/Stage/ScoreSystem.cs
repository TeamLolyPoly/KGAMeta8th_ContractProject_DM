using System;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using UnityEngine;

public class ScoreSystem : MonoBehaviour, IInitializable
{
    private ScoreSettingData scoreSettingData;

    //정확도별 추가점수
    public Dictionary<NoteRatings, int> multiplierScore { get; private set; } =
        new Dictionary<NoteRatings, int>();

    //밴드 호응도 딕셔너리
    public Dictionary<int, Engagement> bandEngagementType { get; private set; } =
        new Dictionary<int, Engagement>();

    //밴드 호응도 변경 인원
    public Dictionary<Engagement, int> bandActiveMember { get; private set; } =
        new Dictionary<Engagement, int>();

    //현재 밴드 호응도
    public Engagement currentBandEngagement { get; private set; }

    //현재 관객 호응도
    public Engagement currentSpectatorEngagement { get; private set; }

    //점수 배율
    public int multiplier { get; private set; } = 1;

    //정확도 기록 딕셔너리
    public Dictionary<NoteRatings, int> ratingCount { get; private set; } =
        new Dictionary<NoteRatings, int>();

    //점수
    public float currentScore { get; private set; } = 0;

    //콤보
    public int combo { get; private set; } = 0;

    //최고 콤보
    public int highCombo { get; private set; } = 0;

    //Miss제외한 노트 Hit 총 횟수
    public int noteHitCount { get; private set; } = 0;

    //노래 총 노트개수
    public int totalNoteCount { get; private set; } = 0;

    //밴드 호응도 이벤트
    public event Action<Engagement, int> onBandEngagementChange;

    //관객 호응도 이벤트
    public event Action<Engagement> onSpectatorEngagementChange;

    private bool isInitialized = false;

    public bool IsInitialized => isInitialized;

    // 최근 판정 결과 저장
    private NoteRatings lastRating = NoteRatings.None;
    public NoteRatings LastRating => lastRating;

    public void Initialize()
    {
        scoreSettingData = Resources.Load<ScoreSettingData>("SO/ScoreSettingData");

        totalNoteCount = GameManager.Instance.NoteMap.TotalNoteCount;

        ratingCount.Clear();

        if (scoreSettingData != null)
        {
            foreach (NoteRatings rating in Enum.GetValues(typeof(NoteRatings)))
            {
                ratingCount.Add(rating, 0);
            }
            for (int i = 0; i < scoreSettingData.engagementThreshold.Count; i++)
            {
                bandEngagementType.Add(scoreSettingData.engagementThreshold[i], (Engagement)i);
                bandActiveMember.Add((Engagement)i, scoreSettingData.engagementMemberThreshold[i]);
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

        isInitialized = true;
    }

    public void SetScore(float score, NoteRatings ratings)
    {
        ratingCount[ratings] += 1;
        lastRating = ratings;

        if (ratings == NoteRatings.Miss)
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
            multiplier = SetMultiplier();

            currentScore += (score * multiplier) + ratingScore;
        }
        SetBandEngagement();
        SetSpectatorEngagement();
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
        SpectatorEventThreshold newThreshold =
            scoreSettingData.sectatorEventThreshold.LastOrDefault(threshold =>
                CheckEngagement(threshold, totalNoteCount)
            ) ?? scoreSettingData.sectatorEventThreshold.First();

        if (currentSpectatorEngagement != newThreshold.engagement)
        {
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
            currentBandEngagement = newEngagement;
            onBandEngagementChange?.Invoke(
                currentBandEngagement,
                bandActiveMember[currentBandEngagement]
            );
            if (PhotonNetwork.InRoom)
            {
                GameManager.Instance.NetworkSystem.SetRemoteBandAnims(
                    currentBandEngagement,
                    bandActiveMember[currentBandEngagement]
                );
            }
        }
    }

    public ResultRank GetGameRank()
    {
        ratingCount.TryGetValue(NoteRatings.Miss, out int missValue);

        ratingCount.TryGetValue(NoteRatings.Good, out int goodValue);

        (int miss, int good) rating = (missValue, goodValue);

        return rating switch
        {
            var (miss, good) when miss == 0 && good == 0 && noteHitCount == totalNoteCount =>
                ResultRank.Splus,
            var (miss, good) when miss == 0 && good > 0 && noteHitCount == totalNoteCount =>
                ResultRank.S,
            var (miss, good) when miss < totalNoteCount * 0.05 && good >= 0 => ResultRank.A,
            var (miss, good) when miss <= totalNoteCount * 0.5 && good >= 0 => ResultRank.B,
            var (miss, good) when miss > totalNoteCount * 0.5 && good >= 0 => ResultRank.C,
            _ => ResultRank.C,
        };
    }

    public ResultRank GetGameRank(ScoreData scoreData)
    {
        int missValue = scoreData.RatingCount[NoteRatings.Miss];
        int goodValue = scoreData.RatingCount[NoteRatings.Good];
        int hitCount = scoreData.NoteHitCount;

        (int miss, int good) rating = (missValue, goodValue);

        return rating switch
        {
            var (miss, good) when miss == 0 && good == 0 && hitCount == totalNoteCount =>
                ResultRank.Splus,
            var (miss, good) when miss == 0 && good > 0 && hitCount == totalNoteCount =>
                ResultRank.S,
            var (miss, good) when miss < totalNoteCount * 0.05 && good >= 0 => ResultRank.A,
            var (miss, good) when miss <= totalNoteCount * 0.5 && good >= 0 => ResultRank.B,
            var (miss, good) when miss > totalNoteCount * 0.5 && good >= 0 => ResultRank.C,
            _ => ResultRank.C,
        };
    }

    public ScoreData GetScoreData()
    {
        return new ScoreData(
            (int)currentScore,
            highCombo,
            noteHitCount,
            totalNoteCount,
            ratingCount[NoteRatings.Miss],
            ratingCount[NoteRatings.Good],
            ratingCount[NoteRatings.Great],
            ratingCount[NoteRatings.Perfect]
        );
    }

    private bool IsPlus(int num) => num >= 0;

    private bool IsCombo(int num)
    {
        if (IsPlus(num))
            return combo >= num;
        else
            return combo <= num;
    }

    private void CleanUp()
    {
        onBandEngagementChange = null;
        onSpectatorEngagementChange = null;

        Destroy(this.gameObject);
    }

    private void OnDestroy()
    {
        CleanUp();
    }
}
