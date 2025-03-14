using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoteGameManager : Singleton<NoteGameManager>, IInitializable
{
    #region Score Related

    #region Settings

    [SerializeField, Header("콤보 배율 기준")]
    private int[] comboMultiplier = { 100, 200, 300, 400, 500 };
    public int Multiplier { get; private set; } = 1;

    [SerializeField, Header("정확도 추가점수")]
    private int[] multiplierScore = { 20, 15, 10 };

    [SerializeField, Header("호응도 콤보 기준")]
    private int[] engagementThreshold = { 10, 30 };

    public Dictionary<NoteRatings, int> ratingComboCount { get; private set; } =
        new Dictionary<NoteRatings, int>();

    #endregion

    #region Runtime

    public float currentScore { get; private set; } = 0;

    public int combo { get; private set; } = 0;

    public int highCombo { get; private set; } = 0;

    public int currentBar { get; private set; } = 0;

    public int currentBeat { get; private set; } = 0;

    #endregion

    #endregion

    #region Game State Management

    [SerializeField, Header("게임 설정")]
    private float startDelay = 3f; // 게임 시작 전 대기 시간

    [SerializeField]
    private AudioSource musicSource;

    private double startDspTime;
    private bool isPlaying = false;
    private double nextBeatTime;

    // 게임 상태 이벤트
    public event Action<bool> OnGameStateChanged;

    // 비트 이벤트
    public event Action<int, int> OnBeatChanged;

    #endregion

    public event Action<int> onEngagementChange;

    private Coroutine engagementCoroutine;

    [SerializeField, Header("노트 맵")]
    private NoteMap noteMap;

    public NoteMap NoteMap => noteMap;

    private NoteSpawner noteSpawner;

    private GridGenerator gridGenerator;

    private bool isInitialized = false;

    public bool IsInitialized => isInitialized;

    public void Initialize()
    {
        GetSpawners();

        ResetGameState();

        if (engagementCoroutine != null)
        {
            StopCoroutine(engagementCoroutine);
        }

        engagementCoroutine = StartCoroutine(EngagementCoroutine());

        isInitialized = true;
    }

    private void ResetGameState()
    {
        currentScore = 0;
        combo = 0;
        highCombo = 0;
        Multiplier = 1;
        currentBar = 0;
        currentBeat = 0;
        isPlaying = false;

        ratingComboCount.Clear();

        foreach (NoteRatings rating in Enum.GetValues(typeof(NoteRatings)))
        {
            ratingComboCount.Add(rating, 0);
        }
    }

    public void GetSpawners()
    {
        noteSpawner = new GameObject("NoteSpawner").AddComponent<NoteSpawner>();

        gridGenerator = new GameObject("GridGenerator").AddComponent<GridGenerator>();

        noteSpawner.Initialize(gridGenerator, noteMap);
    }

    public void LoadNoteMap(NoteMap noteMap)
    {
        this.noteMap = noteMap;

        if (noteSpawner != null && noteSpawner.IsInitialized)
        {
            noteSpawner.Initialize(gridGenerator, noteMap);
        }
    }

    private void Start()
    {
        Initialize();
    }

    private void Update()
    {
        if (isPlaying)
        {
            double currentDspTime = AudioSettings.dspTime;
            float currentTime = (float)(currentDspTime - startDspTime);
            UpdateBarAndBeat(currentTime);
        }
    }

    private void UpdateBarAndBeat(float currentTime)
    {
        if (noteMap == null)
            return;

        float secondsPerBeat = 60f / noteMap.bpm;
        float totalBeats = currentTime / secondsPerBeat;

        int newBar = Mathf.FloorToInt(totalBeats / noteMap.beatsPerBar);
        int newBeat = Mathf.FloorToInt(totalBeats % noteMap.beatsPerBar);

        if (newBar != currentBar || newBeat != currentBeat)
        {
            currentBar = newBar;
            currentBeat = newBeat;
            OnBeatChanged?.Invoke(currentBar, currentBeat);
        }
    }

    #region Game Control

    public void StartGame()
    {
        if (noteMap == null)
        {
            Debug.LogError("노트맵이 설정되지 않았습니다!");
            return;
        }

        StartCoroutine(StageRoutine());
    }

    private IEnumerator StageRoutine()
    {
        Debug.Log($"게임 시작 준비... {startDelay}초 후 시작됩니다.");

        yield return new WaitForSeconds(startDelay);

        startDspTime = AudioSettings.dspTime;
        isPlaying = true;

        noteSpawner.StartSpawn(startDspTime);

        if (musicSource != null && musicSource.clip != null)
        {
            musicSource.Play();
        }

        OnGameStateChanged?.Invoke(true);

        Debug.Log("게임 시작!");
    }

    public void PauseGame()
    {
        if (!isPlaying)
            return;

        isPlaying = false;

        noteSpawner.StopSpawning();

        if (musicSource != null && musicSource.isPlaying)
        {
            musicSource.Pause();
        }

        OnGameStateChanged?.Invoke(false);

        Debug.Log("게임 일시정지");
    }

    public void ResumeGame()
    {
        if (isPlaying)
            return;

        //TODO : 게임 재개 로직

        isPlaying = true;

        Debug.Log("게임 재개");
    }

    public void StopGame()
    {
        if (!isPlaying)
            return;

        isPlaying = false;

        noteSpawner.StopSpawning();

        if (musicSource != null)
        {
            musicSource.Stop();
        }

        OnGameStateChanged?.Invoke(false);

        Debug.Log("게임 중지");
    }

    #endregion

    #region Score Management

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
            highCombo = combo;
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

    #endregion

    private IEnumerator EngagementCoroutine()
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

            yield return null;
        }
    }

    private void OnGUI()
    {
        if (!isPlaying)
            return;

        double currentDspTime = AudioSettings.dspTime;
        GUILayout.BeginArea(new Rect(10, 10, 300, 150));
        GUILayout.Label($"현재 위치: 마디 {currentBar + 1}, 비트 {currentBeat + 1}");
        GUILayout.Label($"DSP 경과 시간: {(currentDspTime - startDspTime):F3}초");
        GUILayout.Label($"점수: {currentScore:F0}");
        GUILayout.Label($"콤보: {combo} (최대: {highCombo})");
        GUILayout.Label($"배율: x{Multiplier}");
        GUILayout.EndArea();
    }
}
