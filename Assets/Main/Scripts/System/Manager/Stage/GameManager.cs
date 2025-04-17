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
    private AnimationSystem unitAnimationSystem;
    public TrackData currentTrack;
    private NoteMap noteMap;
    private AudioSource musicSource;

    public NetworkSystem NetworkSystem => networkSystem;
    public PlayerSystem PlayerSystem => playerSystem;
    public NoteMap NoteMap => noteMap;
    public ScoreSystem ScoreSystem => scoreSystem;
    public AnimationSystem UnitAnimationSystem => unitAnimationSystem;
    public ProceedDrum proceedDrum;

    private bool isInitialized = false;
    private bool isPlaying = false;
    private bool isInMultiStage = false;

    public bool IsInitialized => isInitialized;
    public bool IsInMultiStage => isInMultiStage;
    public bool isEditMode = false;

    private readonly Vector3 SINGLE_PLAYER_SPAWN_POSITION = new Vector3(0, 2.1f, 0.58f);
    private readonly Vector3 MASTER_PLAYER_SPAWN_POSITION = new Vector3(0, 2.1f, 0.58f);
    private readonly Vector3 CLIENT_PLAYER_SPAWN_POSITION = new Vector3(15f, 2.1f, 0.58f);

    #region Common
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
        networkSystem.Initialize();

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

        StageLoadingManager.Instance.LoadScene("Main_Title", initOperations, AfterInit);
    }

    public IEnumerator Single_Cleanup()
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

        Destroy(unitAnimationSystem.gameObject);

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
        unitAnimationSystem = null;

        progress += progressStep;
        yield return progress;
        yield return new WaitForSeconds(1f);
        StageLoadingManager.Instance.SetLoadingText("게임 초기화 완료");
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
                if (isInMultiStage)
                {
                    StartCoroutine(Multi_StopGame());
                }
                else
                {
                    StopGame();
                }
            }
        }
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

    public void StartGamePlayback(double startTime, float preRollTime)
    {
        startDspTime = startTime;
        isPlaying = true;

        if (noteSpawner != null)
        {
            noteSpawner.StartSpawn(startDspTime, preRollTime);
        }

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

    #endregion

    #region Single Stage

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
            "Main_SingleStage",
            InitializeSingleStageRoutine,
            () =>
            {
                StartCoroutine(SingleStageRoutine());
            }
        );
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

        StageLoadingManager.Instance.SetLoadingText("애니메이션 시스템 초기화 중...");
        progress += progressStep;
        yield return progress;
        yield return new WaitForSeconds(1f);

        unitAnimationSystem = new GameObject(
            "unitAnimationManager"
        ).AddComponent<AnimationSystem>();
        unitAnimationSystem.transform.SetParent(transform);
        unitAnimationSystem.Initialize();
        scoreSystem.Initialize();

        progress += progressStep;
        yield return progress;
        yield return new WaitForSeconds(1f);
        noteSpawner.Initialize(gridGenerator, noteMap);
        StageUIManager.Instance.transform.position = new Vector3(0, 2, 0);
        StageLoadingManager.Instance.SetLoadingText("게임 시작 준비 중...");
        yield return new WaitForSeconds(1f);
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

        StartGamePlayback(startDspTime, preRollTime);
    }

    public void Single_BackToTitle()
    {
        StageUIManager.Instance.transform.position = new Vector3(0, 0, 0);
        StageLoadingManager.Instance.LoadScene(
            "Main_Title",
            Single_Cleanup,
            () =>
            {
                PlayerSystem.SpawnPlayer(Vector3.zero, false);
                StageUIManager.Instance.OpenPanel(PanelType.AlbumSelect);
            }
        );
    }
    #endregion

    #region Multiplayer
    public void SetMultiMode()
    {
        networkSystem.StartMultiplayer();
    }

    public void StartMultiPlayerStage(NoteMap noteMap, TrackData track)
    {
        currentTrack = track;
        this.noteMap = noteMap;

        StageLoadingManager.Instance.LoadScene(
            "Main_MultiStage",
            InitializeMultiplayerStageRoutine,
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
        gridGenerator.Initialize(true);

        StageLoadingManager.Instance.SetLoadingText("점수 시스템 초기화 중...");
        progress += progressStep;
        yield return progress;
        yield return new WaitForSeconds(0.5f);

        scoreSystem = new GameObject("ScoreSystem").AddComponent<ScoreSystem>();
        scoreSystem.transform.SetParent(transform);

        StageLoadingManager.Instance.SetLoadingText("애니메이션 시스템 초기화 중...");
        progress += progressStep;
        yield return progress;
        yield return new WaitForSeconds(0.5f);

        unitAnimationSystem = new GameObject(
            "unitAnimationManager"
        ).AddComponent<AnimationSystem>();
        unitAnimationSystem.transform.SetParent(transform);
        unitAnimationSystem.Initialize();
        scoreSystem.Initialize();

        progress += progressStep;
        yield return progress;
        yield return new WaitForSeconds(0.5f);

        noteSpawner.Initialize(gridGenerator, noteMap);
        StageLoadingManager.Instance.SetLoadingText("게임 시작 준비 중...");
        yield return new WaitForSeconds(0.5f);
    }

    public IEnumerator MultiplayerStageRoutine()
    {
        gridGenerator.SetCellVisible(true);

        isInMultiStage = true;

        Vector3 spawnPos = PhotonNetwork.IsMasterClient
            ? MASTER_PLAYER_SPAWN_POSITION
            : CLIENT_PLAYER_SPAWN_POSITION;

        if (PhotonNetwork.IsMasterClient)
        {
            StageUIManager.Instance.transform.position = new Vector3(0, 2, 0);
        }
        else
        {
            StageUIManager.Instance.transform.position = new Vector3(15, 2, 0);
        }

        networkSystem.SetIsInMultiStage(true);

        while (!networkSystem.AreAllPlayersInMultiStage())
        {
            yield return null;
        }

        PlayerSystem.SpawnPlayer(spawnPos, true);

        while (!networkSystem.AreAllPlayersSpawned())
        {
            yield return null;
        }

        yield return new WaitForSeconds(startDelay);

        startDspTime = AudioSettings.dspTime;

        float secondsPerBeat = 60f / noteMap.bpm;
        float distance = gridGenerator.GridDistance;
        float targetHitTime = secondsPerBeat * noteMap.beatsPerBar;
        float noteSpeed = distance / targetHitTime;
        float noteTravelTime = distance / noteSpeed;

        float preRollTime = noteTravelTime;

        StartGamePlayback(startDspTime, preRollTime);
    }

    public IEnumerator Multi_StopGame()
    {
        if (!isPlaying)
            yield break;

        isPlaying = false;

        noteSpawner.StopSpawning();

        gridGenerator.SetCellVisible(false);

        yield return new WaitForSeconds(3f);

        if (musicSource != null)
        {
            musicSource.Stop();
        }

        MultiResultPanel resultPanel =
            StageUIManager.Instance.OpenPanel(PanelType.Multi_Result) as MultiResultPanel;

        networkSystem.SetResultData(scoreSystem.GetScoreData());

        if (PhotonNetwork.IsMasterClient)
        {
            while (!networkSystem.IsStageDone())
            {
                yield return null;
            }

            if (networkSystem.DecideWinner())
            {
                resultPanel.Initialize(
                    networkSystem.GetResultData(PhotonNetwork.LocalPlayer),
                    networkSystem.GetResultData(PhotonNetwork.PlayerList[1]),
                    true
                );
            }
            else
            {
                resultPanel.Initialize(
                    networkSystem.GetResultData(PhotonNetwork.PlayerList[1]),
                    networkSystem.GetResultData(PhotonNetwork.LocalPlayer),
                    false
                );
            }
        }
        else
        {
            while (!networkSystem.IsStageDone())
            {
                yield return null;
            }

            bool isWinner =
                networkSystem.GetResultData(PhotonNetwork.LocalPlayer).Score
                > networkSystem.GetResultData(PhotonNetwork.PlayerListOthers[0]).Score;

            resultPanel.Initialize(
                isWinner
                    ? networkSystem.GetResultData(PhotonNetwork.LocalPlayer)
                    : networkSystem.GetResultData(PhotonNetwork.PlayerListOthers[0]),
                isWinner
                    ? networkSystem.GetResultData(PhotonNetwork.PlayerListOthers[0])
                    : networkSystem.GetResultData(PhotonNetwork.LocalPlayer),
                isWinner
            );
        }

        yield return new WaitForSeconds(3f);

        if (playerSystem != null && playerSystem.XRPlayer != null)
        {
            if (playerSystem.XRPlayer.LeftRayInteractor != null)
                playerSystem.XRPlayer.LeftRayInteractor.enabled = true;

            if (playerSystem.XRPlayer.RightRayInteractor != null)
                playerSystem.XRPlayer.RightRayInteractor.enabled = true;
        }
    }

    public void Multi_BackToTitle()
    {
        networkSystem.LeaveGame();
        isInMultiStage = false;
        StageUIManager.Instance.transform.position = new Vector3(0, 0, 0);
        StageLoadingManager.Instance.LoadScene(
            "Main_Title",
            Multi_Cleanup,
            () =>
            {
                PlayerSystem.SpawnPlayer(Vector3.zero, false);
                StageUIManager.Instance.OpenPanel(PanelType.Mode);
            }
        );
    }

    public IEnumerator Multi_Cleanup()
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

        Destroy(unitAnimationSystem.gameObject);

        progress += progressStep;
        yield return progress;
        yield return new WaitForSeconds(1f);
        StageLoadingManager.Instance.SetLoadingText("데이터 초기화중...");

        networkSystem.ClearPlayerProperties();
        yield return new WaitForSeconds(1f);
        StageLoadingManager.Instance.SetLoadingText("게임 초기화 중...");

        currentBar = 0;
        currentBeat = 0;
        musicSource.clip = null;
        noteSpawner = null;
        gridGenerator = null;
        scoreSystem = null;
        unitAnimationSystem = null;

        progress += progressStep;
        yield return progress;
        yield return new WaitForSeconds(1f);
        StageLoadingManager.Instance.SetLoadingText("게임 초기화 완료");
    }

    public void Multi_BackToRoom()
    {
        isInMultiStage = false;
        StageUIManager.Instance.transform.position = new Vector3(0, 0, 0);
        StageLoadingManager.Instance.LoadScene(
            "Main_Title",
            Multi_Cleanup,
            () =>
            {
                PlayerSystem.SpawnPlayer(Vector3.zero, false);
                StageUIManager.Instance.OpenPanel(PanelType.Multi_Room);
            }
        );
    }
    #endregion

    #region Debug
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

        unitAnimationSystem = new GameObject(
            "unitAnimationManager"
        ).AddComponent<AnimationSystem>();
        unitAnimationSystem.transform.SetParent(transform);
        unitAnimationSystem.Initialize();

        StartCoroutine(SingleStageRoutine());
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
    #endregion
}
