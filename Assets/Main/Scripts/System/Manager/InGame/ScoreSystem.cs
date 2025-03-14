using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScoreSystem : MonoBehaviour, IInitializable
{
    [SerializeField, Header("콤보 배율 기준")]
    private int[] comboMultiplier = { 100, 200, 300, 400, 500 };
    public int Multiplier { get; private set; } = 1;

    [SerializeField, Header("정확도 추가점수")]
    private int[] multiplierScore = { 20, 15, 10 };

    [SerializeField, Header("호응도 콤보 기준")]
    private int[] engagementThreshold = { 10, 30 };

    public Dictionary<NoteRatings, int> ratingComboCount { get; private set; } =
        new Dictionary<NoteRatings, int>();
    public float currentScore { get; private set; } = 0;

    public int combo { get; private set; } = 0;

    public int highCombo { get; private set; } = 0;

    public event Action<int> onEngagementChange;

    private bool isInitialized = false;

    public bool IsInitialized => isInitialized;

    public void Initialize()
    {
        isInitialized = true;

        foreach (NoteRatings rating in Enum.GetValues(typeof(NoteRatings)))
        {
            ratingComboCount.Add(rating, 0);
        }
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
        SetEngagement();
        int ratingScore = GetRatingScore(ratings);
        Multiplier = SetMultiplier();

        currentScore += (score * Multiplier) + ratingScore;

        print($"ratingScore: {ratingScore}");
        print($"currentScore: {currentScore}");
        print($"combo: {combo}");
        print($"combo: {combo} \ncurrentScore: {currentScore}");
    }

    private int SetMultiplier()
    {
        for (int i = 0; i < comboMultiplier.Length; i++)
        {
            if (combo > comboMultiplier[i])
            {
                return Multiplier = i + 1;
            }
        }
        return 1;
    }

    private int GetRatingScore(NoteRatings ratings)
    {
        switch (ratings)
        {
            case NoteRatings.Perfect:
                return multiplierScore[0];
            case NoteRatings.Great:
                return multiplierScore[1];
            case NoteRatings.Good:
                return multiplierScore[2];
            default:
                return 0;
        }
    }

    private void SetEngagement()
    {
        onEngagementChange?.Invoke(0);
        int currentengagement = 0;
        while (true)
        {
            if (combo < engagementThreshold[0] && currentengagement != 0)
            {
                onEngagementChange?.Invoke(0);
                currentengagement = 0;
            }
            if (combo > engagementThreshold[0] && currentengagement != 1)
            {
                onEngagementChange?.Invoke(1);
                currentengagement = 1;
            }
            if (combo > engagementThreshold[1] && currentengagement != 2)
            {
                onEngagementChange?.Invoke(2);
                currentengagement = 2;
            }
        }
    }
}
