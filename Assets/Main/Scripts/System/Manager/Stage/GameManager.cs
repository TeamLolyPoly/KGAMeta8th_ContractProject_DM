using System;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class GameManager : Singleton<GameManager>, IInitializable
{
    public int currentBar { get; private set; } = 0;
    public int currentBeat { get; private set; } = 0;
    private float startDelay = 1f;
    private double startDspTime;

    private NetworkSystem networkSystem;
    private PlayerSystem playerSystem;
    private GridGenerator gridGenerator;
    private ScoreSystem scoreSystem;
    private NoteSpawner noteSpawner;
    private AnimationSystem unitAnimationManager;
    private NoteMap noteMap;
    private AudioSource musicSource;

    public NetworkSystem NetworkSystem => networkSystem;
    public PlayerSystem PlayerSystem => playerSystem;
    public NoteMap NoteMap => noteMap;
    public ScoreSystem ScoreSystem => scoreSystem;
    public AnimationSystem UnitAnimationSystem => unitAnimationManager;

    private bool isInitialized = false;
    private bool isPlaying = false;

    public bool IsInitialized => isInitialized;
    public bool IsPlaying => isPlaying;

    public bool isEditMode = false;

    private void Start()
    {
        Initialize();
    }

    public void Initialize()
    {
        StartCoroutine(InitializeRoutine());
    }

    private IEnumerator InitializeRoutine()
    {
        StageUIManager stageUIManager = Instantiate(
            Resources.Load<StageUIManager>("Prefabs/Stage/System/StageUIManager")
        );
        if (isEditMode)
        {
            stageUIManager.EnableDebugMode();
        }

        PoolManager.Instance.Initialize();
        StageLoadingManager.Instance.Initialize();

        networkSystem = new GameObject("NetworkSystem").AddComponent<NetworkSystem>();
        networkSystem.Initialize();
        networkSystem.gameObject.transform.SetParent(transform);
        yield return new WaitUntil(() => PhotonNetwork.InRoom);

        playerSystem = new GameObject("PlayerSystem").AddComponent<PlayerSystem>();
        playerSystem.gameObject.transform.SetParent(transform);

        List<Func<IEnumerator>> initOperations = new List<Func<IEnumerator>>
        {
            DataManager.Instance.LoadTrackDataList,
            DataManager.Instance.LoadTrackAudioCoroutine,
        };

        Action AfterInit = () =>
        {
            playerSystem.SpawnPlayer(Vector3.zero, false);
            StageUIManager.Instance.OpenPanel(PanelType.Title);
        };

        StageLoadingManager.Instance.LoadScene("Test_Editor", initOperations, AfterInit);

        musicSource = new GameObject("MusicSource").AddComponent<AudioSource>();
        musicSource.gameObject.transform.SetParent(transform);
    }

    public void InitializeStage()
    {
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

    public void StartGame()
    {
        StartCoroutine(StageRoutine());
    }

    public void Cleanup()
    {
        Destroy(noteSpawner.gameObject);
        Destroy(gridGenerator.gameObject);
        Destroy(scoreSystem.gameObject);
        Destroy(unitAnimationManager.gameObject);

        currentBar = 0;
        currentBeat = 0;
        musicSource.clip = null;
        noteSpawner = null;
        gridGenerator = null;
        scoreSystem = null;
        unitAnimationManager = null;
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
        }
    }

    public void StartGame(NoteMap map)
    {
        noteMap = map;

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

        InitializeStage();

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
            $"[GameManager] 노트 속도 계산: BPM={noteMap.bpm}, 거리={distance:F2}, 이동 시간={noteTravelTime:F3}초"
        );

        float preRollTime = noteTravelTime;

        double musicStartTime = startDspTime + preRollTime;

        Debug.Log(
            $"[GameManager] 노트 스폰 시작: DSP 시간={startDspTime:F3}, 프리롤={preRollTime:F3}초"
        );
        noteSpawner.StartSpawn(startDspTime, preRollTime);

        if (musicSource != null && musicSource.clip != null)
        {
            musicSource.PlayScheduled(musicStartTime);
            Debug.Log(
                $"[GameManager] 음악 시작 예약: DSP 시간 {musicStartTime:F3}, 현재 시간: {AudioSettings.dspTime:F3}, 간격: {musicStartTime - AudioSettings.dspTime:F3}초"
            );
            Debug.Log($"[GameManager] 비트당 시간: {secondsPerBeat:F3}초 (BPM {noteMap.bpm})");
        }
        else
        {
            Debug.LogWarning(
                "[GameManager] 음악 소스나 클립이 설정되지 않았습니다. 노트만 생성됩니다."
            );
        }

        Debug.Log("[GameManager] 게임 시작!");
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

        Debug.Log("[GameManager] 게임 중지");
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
