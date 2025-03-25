using System;
using System.Collections.Generic;
using System.Linq;
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

    //밴드 호응도 이벤트
    public event Action<Engagement> onBandEngagementChange;

    //관객 호응도 이벤트
    public event Action<Engagement> onSpectatorEngagementChange;

    public int totalNoteCount { get; private set; } = 0;

    private bool isInitialized = false;

    public bool IsInitialized => isInitialized;

    //테스트용 코드
    // void Update()
    // {
    //     if (Input.GetKeyDown(KeyCode.Q))
    //     {
    //         SetScore(100, NoteRatings.Perfect);
    //     }
    //     if (Input.GetKeyDown(KeyCode.W))
    //     {
    //         SetScore(100, NoteRatings.Good);
    //     }
    //     if (Input.GetKeyDown(KeyCode.E))
    //     {
    //         SetScore(0, NoteRatings.Miss);
    //     }
    //     if (Input.GetKeyDown(KeyCode.R))
    //     {
    //         print(
    //             $"\nGood: {ratingCount[NoteRatings.Good]}"
    //         + $"\nMiss: {ratingCount[NoteRatings.Miss]}"
    //                 + $"\n게임랭크: {GetGameRank()}"
    //         );
    //     }
    // }

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
            print($"ratingScore: {ratingScore}");
            multiplier = SetMultiplier();

            currentScore += (score * multiplier) + ratingScore;
        }
        SetBandEngagement();
        SetSpectatorEngagement();

        print(
            $"=============노트 타격===============\n"
                + $"현재총점수: {currentScore}"
                + $"\n랭크: {ratings}"
                + $"\n노트점수: {score}"
                + $"\n최고 콤보: {highCombo}"
                + $"\n점수 배율{multiplier}"
                + $"\n현재 콤보: {combo}"
                + $"\n노트파괴수: {noteHitCount}"
        );
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
        int totalNoteCount = GameManager.Instance.NoteMap.TotalNoteCount;

        print($"totalNoteCount: {totalNoteCount}");
        SpectatorEventThreshold newThreshold =
            scoreSettingData.sectatorEventThreshold.LastOrDefault(threshold =>
                CheckEngagement(threshold, totalNoteCount)
            ) ?? scoreSettingData.sectatorEventThreshold.First();

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

    public string GetGameRank()
    {
        int totalNoteCount = GameManager.Instance.NoteMap.TotalNoteCount;

        ratingCount.TryGetValue(NoteRatings.Miss, out int missValue);

        ratingCount.TryGetValue(NoteRatings.Good, out int goodValue);

        (int miss, int good) rating = (missValue, goodValue);

        return rating switch
        {
            var (miss, good) when miss == 0 && good == 0 => "S+",
            var (miss, good) when miss == 0 && good > 0 => "S",
            var (miss, good) when miss < totalNoteCount * 0.05 && good >= 0 => "A",
            var (miss, good) when miss <= totalNoteCount * 0.5 && good >= 0 => "B",
            var (miss, good) when miss > totalNoteCount * 0.5 && good >= 0 => "C",
            _ => "C",
        };
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
