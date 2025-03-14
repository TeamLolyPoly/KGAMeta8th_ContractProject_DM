using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : Singleton<GameManager>, IInitializable
{

    #region Runtime

    public float currentScore { get; private set; } = 0;

    public int combo { get; private set; } = 0;

    public int highCombo { get; private set; } = 0;

    public int currentBar { get; private set; } = 0;

    public int currentBeat { get; private set; } = 0;

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

    [SerializeField, Header("노트 맵")]
    private NoteMap noteMap;

    public NoteMap NoteMap => noteMap;

    private NoteSpawner noteSpawner;

    private GridGenerator gridGenerator;

    private ScoreSystem scoreSystem;
    public ScoreSystem ScoreSystem => scoreSystem;

    private AnimationSystem unitAnimationManager;
    public AnimationSystem UnitAnimationManager => unitAnimationManager;

    private bool isInitialized = false;

    public bool IsInitialized => isInitialized;

    public void Initialize()
    {
        GetSpawners();

        ResetGameState();
        isInitialized = true;
    }

    private void ResetGameState()
    {
        currentBar = 0;
        currentBeat = 0;
        isPlaying = false;
    }

    public void GetSpawners()
    {
        noteSpawner = new GameObject("NoteSpawner").AddComponent<NoteSpawner>();

        gridGenerator = new GameObject("GridGenerator").AddComponent<GridGenerator>();

        scoreSystem = new GameObject("ScoreSystem").AddComponent<ScoreSystem>();

        unitAnimationManager = new GameObject("unitAnimationManager").AddComponent<AnimationSystem>();

        unitAnimationManager.Initialize();

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

    private void OnGUI()
    {
        if (!isPlaying)
            return;

        double currentDspTime = AudioSettings.dspTime;
        GUILayout.BeginArea(new Rect(10, 10, 300, 150));
        GUILayout.Label($"현재 위치: 마디 {currentBar + 1}, 비트 {currentBeat + 1}");
        GUILayout.Label($"DSP 경과 시간: {(currentDspTime - startDspTime):F3}초");
        GUILayout.EndArea();
    }
}
