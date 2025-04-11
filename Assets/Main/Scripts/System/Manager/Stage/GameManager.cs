using System;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
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
    public TrackData currentTrack;
    private NoteMap noteMap;
    private AudioSource musicSource;

    public NetworkSystem NetworkSystem => networkSystem;
    public PlayerSystem PlayerSystem => playerSystem;
    public NoteMap NoteMap => noteMap;
    public ScoreSystem ScoreSystem => scoreSystem;
    public AnimationSystem UnitAnimationSystem => unitAnimationManager;

    public ProceedDrum proceedDrum;

    private bool isInitialized = false;
    private bool isPlaying = false;
    private bool isInMultiStage = false;

    public bool IsInitialized => isInitialized;
    public bool IsPlaying => isPlaying;
    public bool IsInMultiStage => isInMultiStage;
    public bool isEditMode = false;

    private readonly Vector3 SINGLE_PLAYER_SPAWN_POSITION = new Vector3(0, 2.1f, 0.58f);
    private readonly Vector3 MASTER_PLAYER_SPAWN_POSITION = new Vector3(0, 2.1f, 0.58f);
    private readonly Vector3 CLIENT_PLAYER_SPAWN_POSITION = new Vector3(15f, 2.1f, -0.58f);

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

        stageUIManager.Initialize();

        if (isEditMode)
        {
            stageUIManager.EnableDebugMode();
        }

        PoolManager.Instance.Initialize();
        StageLoadingManager.Instance.Initialize();

        networkSystem = new GameObject("NetworkSystem").AddComponent<NetworkSystem>();
        networkSystem.gameObject.transform.SetParent(transform);

        playerSystem = new GameObject("PlayerSystem").AddComponent<PlayerSystem>();
        playerSystem.gameObject.transform.SetParent(transform);
        playerSystem.Initialize();
        yield return new WaitUntil(() => playerSystem.IsInitialized);

        musicSource = new GameObject("MusicSource").AddComponent<AudioSource>();
        musicSource.gameObject.transform.SetParent(transform);

        List<Func<IEnumerator>> initOperations = new List<Func<IEnumerator>>
        {
            DataManager.Instance.LoadTrackDataList,
            DataManager.Instance.LoadAlbumArtCoroutine,
        };

        Action AfterInit = () =>
        {
            playerSystem.SpawnPlayer(Vector3.zero, false);
            StageUIManager.Instance.OpenPanel(PanelType.Title);
        };

        StageLoadingManager.Instance.LoadScene("Test_Editor", initOperations, AfterInit);
    }

    public void TestStage(AudioClip audioClip, NoteMap noteMap)
    {
        PoolManager.Instance.Initialize();

        GameObject musicSourceObj = new GameObject("MusicSource");

        musicSource = musicSourceObj.AddComponent<AudioSource>();
        musicSource.gameObject.transform.SetParent(transform);

        this.noteMap = noteMap;
        musicSource.clip = audioClip;

        noteSpawner = new GameObject("NoteSpawner").AddComponent<NoteSpawner>();
        noteSpawner.transform.SetParent(transform);

        gridGenerator = new GameObject("GridGenerator").AddComponent<GridGenerator>();
        gridGenerator.transform.SetParent(transform);
        gridGenerator.Initialize();

        scoreSystem = new GameObject("ScoreSystem").AddComponent<ScoreSystem>();
        scoreSystem.transform.SetParent(transform);
        scoreSystem.Initialize();
        noteSpawner.Initialize(gridGenerator, noteMap);

        unitAnimationManager = new GameObject(
            "unitAnimationManager"
        ).AddComponent<AnimationSystem>();
        unitAnimationManager.transform.SetParent(transform);
        unitAnimationManager.Initialize();

        StartCoroutine(SingleStageRoutine());
    }

    public IEnumerator InitializeSingleStageRoutine()
    {
        float progress = 0f;
        float progressStep = 0.2f;

        DataManager.Instance.LoadTrackAudio(
            currentTrack.id,
            (audioClip) =>
            {
                musicSource.clip = audioClip;
            }
        );

        StageLoadingManager.Instance.SetLoadingText("게임 초기화 중...");

        progress += progressStep;
        yield return progress;
        yield return new WaitForSeconds(1f);

        noteSpawner = new GameObject("NoteSpawner").AddComponent<NoteSpawner>();
        noteSpawner.transform.SetParent(transform);

        StageLoadingManager.Instance.SetLoadingText("그리드 생성 중...");
        progress += progressStep;
        yield return progress;
        yield return new WaitForSeconds(1f);

        gridGenerator = new GameObject("GridGenerator").AddComponent<GridGenerator>();
        gridGenerator.transform.SetParent(transform);
        gridGenerator.Initialize();

        StageLoadingManager.Instance.SetLoadingText("점수 시스템 초기화 중...");
        progress += progressStep;
        yield return progress;
        yield return new WaitForSeconds(1f);

        scoreSystem = new GameObject("ScoreSystem").AddComponent<ScoreSystem>();
        scoreSystem.transform.SetParent(transform);
        scoreSystem.Initialize();

        StageLoadingManager.Instance.SetLoadingText("애니메이션 시스템 초기화 중...");
        progress += progressStep;
        yield return progress;
        yield return new WaitForSeconds(1f);

        unitAnimationManager = new GameObject(
            "unitAnimationManager"
        ).AddComponent<AnimationSystem>();
        unitAnimationManager.transform.SetParent(transform);
        unitAnimationManager.Initialize();

        progress += progressStep;
        yield return progress;
        yield return new WaitForSeconds(1f);
        noteSpawner.Initialize(gridGenerator, noteMap);
        StageUIManager.Instance.transform.position = new Vector3(0, 2, 0);
        StageLoadingManager.Instance.SetLoadingText("게임 시작 준비 중...");
        yield return new WaitForSeconds(1f);
    }

    public IEnumerator Cleanup()
    {
        float progress = 0f;
        float progressStep = 0.2f;

        yield return progress;
        yield return new WaitForSeconds(1f);
        StageLoadingManager.Instance.SetLoadingText("게임 초기화 중...");
        Destroy(noteSpawner.gameObject);

        progress += progressStep;
        yield return progress;
        yield return new WaitForSeconds(1f);

        Destroy(gridGenerator.gameObject);

        progress += progressStep;
        yield return progress;
        yield return new WaitForSeconds(1f);
        StageLoadingManager.Instance.SetLoadingText("점수 시스템 초기화 중...");

        Destroy(scoreSystem.gameObject);
        progress += progressStep;
        yield return progress;
        yield return new WaitForSeconds(1f);
        StageLoadingManager.Instance.SetLoadingText("애니메이션 시스템 초기화 중...");

        Destroy(unitAnimationManager.gameObject);

        progress += progressStep;
        yield return progress;
        yield return new WaitForSeconds(1f);
        StageLoadingManager.Instance.SetLoadingText("데이터 초기화중...");

        currentBar = 0;
        currentBeat = 0;
        musicSource.clip = null;
        noteSpawner = null;
        gridGenerator = null;
        scoreSystem = null;
        unitAnimationManager = null;

        progress += progressStep;
        yield return progress;
        yield return new WaitForSeconds(1f);
        StageLoadingManager.Instance.SetLoadingText("게임 초기화 완료");
    }

    public void BackToTitle()
    {
        StageUIManager.Instance.transform.position = new Vector3(0, 0, 0);
        StageLoadingManager.Instance.LoadScene(
            "Test_Editor",
            Cleanup,
            () =>
            {
                PlayerSystem.SpawnPlayer(Vector3.zero, false);
                StageUIManager.Instance.OpenPanel(PanelType.AlbumSelect);
            }
        );
    }

    private void Update()
    {
        if (isPlaying)
        {
            double currentDspTime = AudioSettings.dspTime;
            float currentTime = (float)(currentDspTime - startDspTime);
            UpdateBarAndBeat(currentTime);

            if (
                musicSource != null
                && musicSource.clip != null
                && musicSource.isPlaying
                && currentTime >= currentTrack.duration
            )
            {
                StopGame();
            }
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

    public void StartGame(TrackData track, NoteMap map)
    {
        currentTrack = track;

        noteMap = map;

        if (noteMap == null)
        {
            Debug.LogError("노트맵이 설정되지 않았습니다!");
            return;
        }

        StageLoadingManager.Instance.LoadScene(
            "Test_Stage",
            InitializeSingleStageRoutine,
            () =>
            {
                StartCoroutine(SingleStageRoutine());
            }
        );
    }

    private IEnumerator SingleStageRoutine()
    {
        gridGenerator.SetCellVisible(true);
        PlayerSystem.SpawnPlayer(SINGLE_PLAYER_SPAWN_POSITION, true);
        yield return new WaitUntil(() => PlayerSystem.IsSpawned);

        yield return new WaitForSeconds(startDelay);

        startDspTime = AudioSettings.dspTime;
        isPlaying = true;

        float secondsPerBeat = 60f / noteMap.bpm;
        float distance = gridGenerator.GridDistance;
        float targetHitTime = secondsPerBeat * noteMap.beatsPerBar;
        float noteSpeed = distance / targetHitTime;
        float noteTravelTime = distance / noteSpeed;

        float preRollTime = noteTravelTime;

        double musicStartTime = startDspTime + preRollTime;

        noteSpawner.StartSpawn(startDspTime, preRollTime);

        if (musicSource != null && musicSource.clip != null)
        {
            musicSource.PlayScheduled(musicStartTime);
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

        SingleResultPanel resultDetailPanel =
            StageUIManager.Instance.OpenPanel(PanelType.Single_Result) as SingleResultPanel;
        resultDetailPanel.Initialize(scoreSystem.GetScoreData());
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

    public void StartMultiplayer()
    {
        if (networkSystem == null)
        {
            Debug.LogError("[GameManager] NetworkSystem is not initialized");
            return;
        }

        networkSystem.StartMultiplayer();
    }

    public void StartMultiplayerGame(TrackData track, NoteMap map)
    {
        currentTrack = track;
        noteMap = map;

        if (noteMap == null)
        {
            Debug.LogError("[GameManager] 노트맵이 설정되지 않았습니다!");
            return;
        }

        List<Func<IEnumerator>> operations = new List<Func<IEnumerator>>
        {
            InitializeMultiplayerStageRoutine,
        };

        networkSystem.LoadSceneMaster(
            "Test_Stage",
            operations,
            () =>
            {
                StartCoroutine(MultiplayerStageRoutine());
            }
        );
    }

    public IEnumerator InitializeMultiplayerStageRoutine()
    {
        float progress = 0f;
        float progressStep = 0.2f;

        DataManager.Instance.LoadTrackAudio(
            currentTrack.id,
            (audioClip) =>
            {
                musicSource.clip = audioClip;
            }
        );

        StageLoadingManager.Instance.SetLoadingText("게임 초기화 중...");

        progress += progressStep;
        yield return progress;
        yield return new WaitForSeconds(0.5f);

        noteSpawner = new GameObject("NoteSpawner").AddComponent<NoteSpawner>();
        noteSpawner.transform.SetParent(transform);

        StageLoadingManager.Instance.SetLoadingText("그리드 생성 중...");
        progress += progressStep;
        yield return progress;
        yield return new WaitForSeconds(0.5f);

        gridGenerator = new GameObject("GridGenerator").AddComponent<GridGenerator>();
        gridGenerator.transform.SetParent(transform);
        gridGenerator.Initialize();

        StageLoadingManager.Instance.SetLoadingText("점수 시스템 초기화 중...");
        progress += progressStep;
        yield return progress;
        yield return new WaitForSeconds(0.5f);

        scoreSystem = new GameObject("ScoreSystem").AddComponent<ScoreSystem>();
        scoreSystem.transform.SetParent(transform);
        scoreSystem.Initialize();

        StageLoadingManager.Instance.SetLoadingText("애니메이션 시스템 초기화 중...");
        progress += progressStep;
        yield return progress;
        yield return new WaitForSeconds(0.5f);

        // unitAnimationManager = new GameObject(
        //     "unitAnimationManager"
        // ).AddComponent<AnimationSystem>();
        // unitAnimationManager.transform.SetParent(transform);
        // unitAnimationManager.Initialize();

        progress += progressStep;
        yield return progress;
        yield return new WaitForSeconds(0.5f);

        noteSpawner.Initialize(gridGenerator, noteMap);
        StageUIManager.Instance.transform.position = new Vector3(0, 2, 0);
        StageLoadingManager.Instance.SetLoadingText("게임 시작 준비 중...");
        yield return new WaitForSeconds(0.5f);
    }

    private IEnumerator MultiplayerStageRoutine()
    {
        gridGenerator.SetCellVisible(true);

        Vector3 spawnPosition = PhotonNetwork.IsMasterClient
            ? MASTER_PLAYER_SPAWN_POSITION
            : CLIENT_PLAYER_SPAWN_POSITION;
        PlayerSystem.SpawnPlayer(spawnPosition, true);

        while (!networkSystem.AreAllPlayersSpawned())
        {
            yield return null;
        }

        isInMultiStage = true;

        if (PhotonNetwork.IsMasterClient)
        {
            yield return new WaitForSeconds(startDelay);

            startDspTime = AudioSettings.dspTime;

            float secondsPerBeat = 60f / noteMap.bpm;
            float distance = gridGenerator.GridDistance;
            float targetHitTime = secondsPerBeat * noteMap.beatsPerBar;
            float noteSpeed = distance / targetHitTime;
            float noteTravelTime = distance / noteSpeed;

            float preRollTime = noteTravelTime;
            double musicStartTime = startDspTime + preRollTime;

            networkSystem.photonView.RPC(
                nameof(StartGamePlayback),
                RpcTarget.All,
                startDspTime,
                preRollTime
            );
        }
    }

    [PunRPC]
    public void StartGamePlayback(double startTime, float preRollTime)
    {
        startDspTime = startTime;
        isPlaying = true;

        noteSpawner.StartSpawn(startDspTime, preRollTime);

        if (musicSource != null && musicSource.clip != null)
        {
            double musicStartTime = startDspTime + preRollTime;
            musicSource.PlayScheduled(musicStartTime);
        }
        else
        {
            Debug.LogWarning(
                "[GameManager] 음악 소스나 클립이 설정되지 않았습니다. 노트만 생성됩니다."
            );
        }

        Debug.Log("[GameManager] 멀티플레이어 게임 시작!");
    }
}
