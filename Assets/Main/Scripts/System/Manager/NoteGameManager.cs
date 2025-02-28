using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NoteGameManager : Singleton<NoteGameManager>
{
    [Header("테스트")]
    public TextMeshProUGUI test;

    [SerializeField, Header("콤보 배율 기준")]
    private int[] comboMultiplier = { 10, 50, 100, 200, 300 };

    [SerializeField, Header("정확도 추가점수")]
    private int[] multiplierScore = { 20, 15, 10 };

    //현재 점수
    public float currentScore { get; private set; } = 0;

    //현재 콤보
    public int combo { get; private set; } = 0;

    //현재 배율
    public int Multiplier { get; private set; } = 1;

    //노트 점수계산함수
    public void SetScore(float score, NoteRatings ratings)
    {
        if (score <= 0 || ratings == NoteRatings.Miss)
        {
            Multiplier = 1;
            combo = 0;
            return;
        }
        combo++;
        int ratingScore = GetRatingScore(ratings);
        print(ratingScore);
        Multiplier = SetMultiplier();
        currentScore += (score * Multiplier) + ratingScore;
        test.text = $"{currentScore}";
    }

    //콤보별 배율 세팅함수
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

    //정확도 추가 점수
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
}
