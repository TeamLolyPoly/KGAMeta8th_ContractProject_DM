using System;
using System.Collections;
using Newtonsoft.Json;
using UnityEngine;

public class GameManager : Singleton<GameManager>, IInitializable
{
    public int currentBar { get; private set; } = 0;
    public int currentBeat { get; private set; } = 0;
    private float startDelay = 1f;
    private double startDspTime;
    public event Action<bool> OnGameStateChanged;
    public event Action<int, int> OnBeatChanged;

    [SerializeField]
    private AudioSource musicSource;
    private NoteMap noteMap;
    public NoteMap NoteMap => noteMap;
    private NoteSpawner noteSpawner;
    private GridGenerator gridGenerator;
    private ScoreSystem scoreSystem;
    public ScoreSystem ScoreSystem => scoreSystem;
    private AnimationSystem unitAnimationManager;
    public AnimationSystem UnitAnimationSystem => unitAnimationManager;
    private bool isInitialized = false;
    public bool IsInitialized => isInitialized;
    private bool isPlaying = false;

    private void Start()
    {
        Initialize();
    }

    public void Initialize()
    {
        ResetGameState();

        InitializeSystem();

        Test();

        isInitialized = true;
    }

    public void Test()
    {
        string TestNoteMap = Resources.Load<TextAsset>("JSON/TestMap").text;
        NoteMap noteMap = JsonConvert.DeserializeObject<NoteMap>(TestNoteMap);
        LoadNoteMap(noteMap);
        StartGame();
    }

    private void ResetGameState()
    {
        currentBar = 0;
        currentBeat = 0;
        isPlaying = false;
    }

    public void InitializeSystem()
    {
        // UIManager 생성
        if (UIManager.Instance == null)
        {
            GameObject uiManagerObj = new GameObject("UIManager");
            uiManagerObj.AddComponent<UIManager>();
        }
        noteSpawner = new GameObject("NoteSpawner").AddComponent<NoteSpawner>();

        gridGenerator = new GameObject("GridGenerator").AddComponent<GridGenerator>();

        gridGenerator.Initialize();

        scoreSystem = new GameObject("ScoreSystem").AddComponent<ScoreSystem>();

        unitAnimationManager = new GameObject(
            "unitAnimationManager"
        ).AddComponent<AnimationSystem>();

        unitAnimationManager.Initialize();

        scoreSystem.Initialize();
        noteSpawner.Initialize(gridGenerator, noteMap);
    }

    public void LoadNoteMap(NoteMap noteMap)
    {
        this.noteMap = noteMap;

        noteSpawner.Initialize(gridGenerator, noteMap);
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

    public void StartGame()
    {
        if (noteMap == null)
        {
            Debug.LogError("노트맵이 설정되지 않았습니다!");
            return;
        }

        GameObject RenderCanvas = GameObject.Find("RenderCanvas");
        if (RenderCanvas == null)
        {
            Debug.LogError("RenderCanvas 찾을 수 없습니다!");
            return;
        }
        Transform rendererObject = RenderCanvas.transform.Find("Renderer");
        if (rendererObject == null)
        {
            Debug.LogError("Renderer 찾을 수 없습니다!");
            return;
        }

        GameObject scoreboard = Resources.Load<GameObject>(
            "Prefabs/UI/Panels/Stage/UI_Panel_ScorePanel"
        );
        if (scoreboard != null)
        {
            GameObject scoreboardInstance = Instantiate(scoreboard, rendererObject);
            Debug.Log("ScoreboardPanel이 성공적으로 생성되었습니다.");
        }
        else
        {
            Debug.LogError("ScoreboardPanel 프리팹을 찾을 수 없습니다!");
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

        float secondsPerBeat = 60f / noteMap.bpm;
        float distance = gridGenerator.GridDistance;
        float targetHitTime = secondsPerBeat * noteMap.beatsPerBar;
        float noteSpeed = distance / targetHitTime;
        float noteTravelTime = distance / noteSpeed;

        Debug.Log(
            $"노트 속도 계산: BPM={noteMap.bpm}, 거리={distance:F2}, 이동 시간={noteTravelTime:F3}초"
        );

        float preRollTime = noteTravelTime;

        double musicStartTime = startDspTime + preRollTime;

        Debug.Log($"노트 스폰 시작: DSP 시간={startDspTime:F3}, 프리롤={preRollTime:F3}초");
        noteSpawner.StartSpawn(startDspTime, preRollTime);

        if (musicSource != null && musicSource.clip != null)
        {
            musicSource.PlayScheduled(musicStartTime);
            Debug.Log(
                $"음악 시작 예약: DSP 시간 {musicStartTime:F3}, 현재 시간: {AudioSettings.dspTime:F3}, 간격: {musicStartTime - AudioSettings.dspTime:F3}초"
            );
            Debug.Log($"비트당 시간: {secondsPerBeat:F3}초 (BPM {noteMap.bpm})");
        }
        else
        {
            Debug.LogWarning("음악 소스나 클립이 설정되지 않았습니다. 노트만 생성됩니다.");
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

    private void OnGUI()
    {
        if (!isPlaying)
            return;

        double currentDspTime = AudioSettings.dspTime;
        GUILayout.BeginArea(new Rect(10, 10, 300, 150));
        GUILayout.Label($"현재 위치: 마디 {currentBar + 1}, 비트 {currentBeat + 1}");
        GUILayout.Label($"DSP 경과 시간: {(currentDspTime - startDspTime):F3}초");
        GUILayout.Label($"현재 점수: {scoreSystem.currentScore}");
        GUILayout.Label($"콤보: {scoreSystem.combo}");
        GUILayout.Label($"최고 콤보: {scoreSystem.highCombo}");
        GUILayout.EndArea();
    }
}
