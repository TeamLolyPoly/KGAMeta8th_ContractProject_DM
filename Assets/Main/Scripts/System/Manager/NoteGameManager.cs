using UnityEngine;
using System;
using Random = UnityEngine.Random;
using System.Collections;

public class NoteGameManager : Singleton<NoteGameManager>, IInitializable
{
    //TODO: 관객 호응도 옵저버 패턴 구현 및 노래별 총점수 필요 노래별 총점수는 에디터에서 관리도 가능능

    [SerializeField, Header("콤보 배율 기준")]
    private int[] comboMultiplier = { 100, 200, 300, 400, 500 };

    [SerializeField, Header("정확도 추가점수")]
    private int[] multiplierScore = { 20, 15, 10 };

    [SerializeField, Header("호응도 콤보 기준")]
    private int[] engagementThreshold = { 10, 30 };

    //현재 점수
    public float currentScore { get; private set; } = 0;
    //현재 콤보
    public int combo { get; private set; } = 0;
    //현재 배율
    public int Multiplier { get; private set; } = 1;

    private bool isInitialized = false;

    public bool IsInitialized => isInitialized;

    //호응도 변화시 호출할 이벤트
    public event Action<int> onEngagementChange;

    private Coroutine engagementCoroutine;
    //게임 시작 전 초기화 함수
    public void Initialize()
    {
        currentScore = 0;
        combo = 0;
        Multiplier = 1;

        if (engagementCoroutine != null)
        {
            StopCoroutine(engagementCoroutine);
        }
        engagementCoroutine = StartCoroutine(SetEngagementCoroutine());

        isInitialized = true;
    }
    private void Start()
    {
        Initialize();
    }

    // 노트 타입 설정 함수
    public void SetupNoteTypeData(NoteData noteData, bool isLeftGrid)
    {
        if (isLeftGrid)
        {
            noteData.baseType = NoteBaseType.Short;
            noteData.noteType = NoteHitType.Hand;
        }
        else
        {
            // 오른쪽 그리드는 Short/Long 모두 가능하고 Red/Blue만 가능
            noteData.noteType = Random.value > 0.5f ? NoteHitType.Red : NoteHitType.Blue;
        }
    }

    //노트 점수계산함수
    public void SetScore(float score, NoteRatings ratings)
    {
        if (score <= 0 || ratings == NoteRatings.Miss)
        {
            Multiplier = 1;
            combo = 0;
            print($"combo: {combo} \ncurrentScore: {currentScore}");
            return;
        }
        combo += 1;
        int ratingScore = GetRatingScore(ratings);
        Multiplier = SetMultiplier();

        currentScore += (score * Multiplier) + ratingScore;

        print($"ratingScore: {ratingScore}");
        print($"currentScore: {currentScore}");
        print($"combo: {combo}");
        print($"combo: {combo} \ncurrentScore: {currentScore}");
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

    private IEnumerator SetEngagementCoroutine()
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
