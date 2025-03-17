using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NoteEditor
{
    public class EditorManager : Singleton<EditorManager>, IInitializable
    {
        public RailController railController { get; private set; }
        public CellController cellController { get; private set; }
        public NoteEditor noteEditor { get; private set; }
        public NoteEditorPanel editorPanel { get; private set; }
        public WaveformDisplay waveformDisplay { get; private set; }

        private Camera editorCamera;
        private InputActionAsset editorControlActions;
        private List<TrackData> cachedTracks = new List<TrackData>();
        private int currentTrackIndex = 0;
        private string pendingAudioFilePath;
        private string pendingAlbumArtFilePath;
        private string currentSceneName;
        private bool isLoadingTrack = false;

        private readonly string[] audioLoadingTips =
        {
            "오디오 파일을 분석하는 중입니다...",
            "웨이브폼을 생성하는 중입니다...",
            "트랙 정보를 처리하는 중입니다...",
            "대용량 오디오 파일은 처리 시간이 더 오래 걸릴 수 있습니다.",
            "고품질 오디오 파일을 사용하면 더 정확한 웨이브폼을 볼 수 있습니다.",
        };

        private readonly string[] albumArtLoadingTips =
        {
            "이미지 파일을 처리하는 중입니다...",
            "앨범 아트를 최적화하는 중입니다...",
            "트랙 정보에 앨범 아트를 연결하는 중입니다...",
            "앨범 아트는 트랙 정보와 함께 저장됩니다.",
        };

        /// <summary>
        /// 현재 선택된 트랙을 반환합니다.
        /// </summary>
        public TrackData currentTrack
        {
            get
            {
                if (
                    cachedTracks.Count > 0
                    && currentTrackIndex >= 0
                    && currentTrackIndex < cachedTracks.Count
                )
                {
                    return cachedTracks[currentTrackIndex];
                }
                return null;
            }
        }

        [SerializeField]
        private float cameraSpeed = 5f;

        [SerializeField]
        private float zoomSpeed = 2f;

        private bool isInitialized = false;
        public bool IsInitialized => isInitialized;

        private void Start()
        {
            Initialize();
        }

        public void Initialize()
        {
            if (isInitialized)
            {
                GetResources();
                return;
            }

            GetResources();
            StartCoroutine(InitializeComponents());
        }

        private IEnumerator InitializeComponents()
        {
            yield return new WaitUntil(
                () => AudioManager.Instance != null && AudioManager.Instance.IsInitialized
            );
            yield return new WaitUntil(
                () => EditorDataManager.Instance != null && EditorDataManager.Instance.IsInitialized
            );

            Debug.Log("[EditorManager] 컴포넌트 초기화 시작");

            // 먼저 리소스 로드 및 기본 초기화
            if (railController != null && !railController.IsInitialized)
            {
                railController.Initialize();
                yield return new WaitUntil(() => railController.IsInitialized);
                Debug.Log("[EditorManager] RailController 초기화 완료");
            }

            if (cellController != null && !cellController.IsInitialized)
            {
                cellController.Initialize();
                yield return new WaitUntil(() => cellController.IsInitialized);
                Debug.Log("[EditorManager] CellController 초기화 완료");
            }

            if (noteEditor != null && !noteEditor.IsInitialized)
            {
                noteEditor.Initialize();
                yield return new WaitUntil(() => noteEditor.IsInitialized);
                Debug.Log("[EditorManager] NoteEditor 초기화 완료");
            }

            if (editorPanel != null && !editorPanel.IsInitialized)
            {
                editorPanel.Initialize();
                yield return new WaitUntil(() => editorPanel.IsInitialized);
                Debug.Log("[EditorManager] EditorPanel 초기화 완료");
            }

            editorCamera = Camera.main;

            if (editorCamera != null)
            {
                editorCamera.transform.position = new Vector3(0, 5, -5);
                editorCamera.transform.rotation = Quaternion.Euler(30, 0, 0);
            }

            SetupInputActions();
            RefreshTrackList();

            // 트랙 로드 및 선택
            if (cachedTracks.Count > 0)
            {
                SelectTrack(cachedTracks[0]);
            }

            isInitialized = true;
            Debug.Log("[EditorManager] 초기화 완료");
        }

        public void GetResources()
        {
            GameObject railObj = new GameObject("RailController");
            railObj.transform.SetParent(transform);
            railController = railObj.AddComponent<RailController>();

            GameObject cellObj = new GameObject("CellController");
            cellObj.transform.SetParent(this.transform);
            cellController = cellObj.AddComponent<CellController>();

            GameObject noteEditorObj = new GameObject("NoteEditor");
            noteEditorObj.transform.SetParent(transform);
            noteEditor = noteEditorObj.AddComponent<NoteEditor>();

            GameObject waveformObj = new GameObject("WaveformDisplay");
            waveformObj.transform.SetParent(transform);
            waveformDisplay = waveformObj.AddComponent<WaveformDisplay>();

            GameObject canvasObj = GameObject.Find("Canvas");
            if (canvasObj != null)
            {
                editorPanel = Instantiate(
                    Resources.Load<NoteEditorPanel>("Prefabs/NoteEditor/UI_Panel_NoteEditor"),
                    canvasObj.transform
                );
                canvasObj.transform.SetParent(transform);
            }
            else
            {
                Debug.LogError("Canvas를 찾을 수 없습니다.");
            }
        }

        private void SetupInputActions()
        {
            editorControlActions = Resources.Load<InputActionAsset>("Input/EditorControls");
            if (editorControlActions != null)
            {
                var actionMap = editorControlActions.FindActionMap("NoteEditor");
                if (actionMap != null)
                {
                    var playPauseAction = actionMap.FindAction("PlayPause");
                    if (playPauseAction != null)
                    {
                        playPauseAction.performed += ctx => AudioManager.Instance.TogglePlayPause();
                    }

                    var nextTrackAction = actionMap.FindAction("NextTrack");
                    if (nextTrackAction != null)
                    {
                        nextTrackAction.performed += ctx => NextTrack();
                    }

                    var prevTrackAction = actionMap.FindAction("PreviousTrack");
                    if (prevTrackAction != null)
                    {
                        prevTrackAction.performed += ctx => PreviousTrack();
                    }

                    var volumeUpAction = actionMap.FindAction("VolumeUp");
                    if (volumeUpAction != null)
                    {
                        volumeUpAction.performed += ctx =>
                            AudioManager.Instance.AdjustVolume(0.05f);
                    }

                    var volumeDownAction = actionMap.FindAction("VolumeDown");
                    if (volumeDownAction != null)
                    {
                        volumeDownAction.performed += ctx =>
                            AudioManager.Instance.AdjustVolume(-0.05f);
                    }

                    actionMap.Enable();
                }
                else
                {
                    Debug.LogError(
                        "NoteEditor 액션맵을 찾을 수 없습니다. InputActionAsset 설정을 확인하세요."
                    );
                }
            }
            else
            {
                Debug.LogWarning(
                    "editorControlActions이 할당되지 않았습니다. Resources/Input/EditorControls를 확인하세요."
                );
            }
        }

        private void OnEnable()
        {
            if (editorControlActions != null)
            {
                var actionMap = editorControlActions.FindActionMap("NoteEditor");
                if (actionMap != null)
                {
                    actionMap.Enable();
                }
            }
        }

        private void OnDisable()
        {
            if (editorControlActions != null)
            {
                var actionMap = editorControlActions.FindActionMap("NoteEditor");
                if (actionMap != null)
                {
                    actionMap.Disable();
                }
            }
        }

        /// <summary>
        /// 트랙 목록을 새로고침합니다.
        /// </summary>
        public void RefreshTrackList()
        {
            if (EditorDataManager.Instance != null)
            {
                cachedTracks = EditorDataManager.Instance.GetAllTracks();
            }
        }

        /// <summary>
        /// 모든 트랙 정보를 가져옵니다.
        /// </summary>
        /// <returns>트랙 데이터 목록</returns>
        public List<TrackData> GetAllTrackInfo()
        {
            RefreshTrackList();
            return cachedTracks;
        }

        /// <summary>
        /// 트랙을 선택합니다.
        /// </summary>
        /// <param name="track">선택할 트랙</param>
        public void SelectTrack(TrackData track)
        {
            if (track == null)
            {
                Debug.LogWarning("선택한 트랙이 없습니다.");
                return;
            }

            if (track.TrackAudio == null && EditorDataManager.Instance != null)
            {
                Debug.Log($"트랙 '{track.trackName}'의 오디오를 로드합니다.");
                StartCoroutine(LoadTrackAndSelect(track.trackName));
                return;
            }

            if (
                AudioManager.Instance.currentTrack != null
                && AudioManager.Instance.currentTrack.trackName == track.trackName
                && AudioManager.Instance.currentAudioSource.clip == track.TrackAudio
            )
            {
                Debug.Log($"트랙 '{track.trackName}'은 이미 선택되어 있습니다.");
                return;
            }

            if (track.TrackAudio == null)
            {
                Debug.LogWarning("선택한 트랙의 오디오가 로드되지 않았습니다.");
                return;
            }

            currentTrackIndex = cachedTracks.IndexOf(track);
            if (currentTrackIndex < 0)
                currentTrackIndex = 0;

            AudioManager.Instance.SetTrack(track, track.TrackAudio);

            noteEditor.SetTrack(track);

            if (editorPanel != null && editorPanel.IsInitialized)
            {
                editorPanel.ChangeTrack(track);
            }

            Debug.Log(
                $"트랙 선택됨: {track.trackName}, BPM: {track.bpm}, 길이: {track.TrackAudio.length}초"
            );
        }

        /// <summary>
        /// 트랙 오디오를 로드하고 선택하는 코루틴
        /// </summary>
        private IEnumerator LoadTrackAndSelect(string trackName)
        {
            var loadTask = EditorDataManager.Instance.LoadTrackAudioAsync(trackName);

            while (!loadTask.IsCompleted)
            {
                yield return null;
            }

            if (loadTask.Result != null && loadTask.Result.TrackAudio != null)
            {
                SelectTrack(loadTask.Result);
            }
            else
            {
                Debug.LogWarning($"트랙 '{trackName}'의 오디오 로드에 실패했습니다.");
            }
        }

        /// <summary>
        /// 다음 트랙으로 이동합니다.
        /// </summary>
        public void NextTrack()
        {
            if (cachedTracks.Count > 0)
            {
                currentTrackIndex = (currentTrackIndex + 1) % cachedTracks.Count;
                SelectTrack(cachedTracks[currentTrackIndex]);

                AudioManager.Instance.PlayCurrentTrack();
            }
        }

        /// <summary>
        /// 이전 트랙으로 이동합니다.
        /// </summary>
        public void PreviousTrack()
        {
            if (cachedTracks.Count > 0)
            {
                currentTrackIndex =
                    (currentTrackIndex - 1 + cachedTracks.Count) % cachedTracks.Count;
                SelectTrack(cachedTracks[currentTrackIndex]);

                AudioManager.Instance.PlayCurrentTrack();
            }
        }

        /// <summary>
        /// 트랙을 삭제합니다.
        /// </summary>
        /// <param name="track">삭제할 트랙</param>
        public async void RemoveTrack(TrackData track)
        {
            RefreshTrackList();

            await EditorDataManager.Instance.DeleteTrackAsync(track.trackName);

            if (noteEditor != null && noteEditor.IsInitialized)
            {
                noteEditor.RemoveTrack(track);
            }

            if (AudioManager.Instance.currentTrack == track)
            {
                if (cachedTracks.Count > 0)
                {
                    SelectTrack(cachedTracks[0]);
                }
                else
                {
                    AudioManager.Instance.currentTrack = null;
                    AudioManager.Instance.currentAudioSource.clip = null;
                    AudioManager.Instance.Stop();
                }
            }

            // UI 업데이트
            if (editorPanel != null && editorPanel.IsInitialized)
            {
                editorPanel.RefreshTrackList();
            }
        }

        /// <summary>
        /// 오디오 파일을 로드하는 메서드
        /// </summary>
        /// <param name="filePath">로드할 오디오 파일 경로</param>
        public void LoadAudioFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || isLoadingTrack)
                return;

            pendingAudioFilePath = filePath;
            currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            isLoadingTrack = true;

            LoadingManager.Instance.LoadScene(
                LoadingManager.Instance.loadingSceneName,
                () => StartCoroutine(LoadAudioFileProcess())
            );
        }

        /// <summary>
        /// 앨범 아트를 설정하는 메서드
        /// </summary>
        /// <param name="filePath">로드할 이미지 파일 경로</param>
        /// <param name="trackIndex">트랙 인덱스</param>
        public void SetAlbumArt(string filePath, int trackIndex)
        {
            if (string.IsNullOrEmpty(filePath))
                return;

            pendingAlbumArtFilePath = filePath;
            currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

            LoadingManager.Instance.LoadScene(
                LoadingManager.Instance.loadingSceneName,
                () => StartCoroutine(LoadAlbumArtProcess(trackIndex))
            );
        }

        /// <summary>
        /// 오디오 파일 로드 프로세스
        /// </summary>
        private IEnumerator LoadAudioFileProcess()
        {
            if (string.IsNullOrEmpty(pendingAudioFilePath))
            {
                LoadingManager.Instance.LoadScene(currentSceneName);
                isLoadingTrack = false;
                yield break;
            }

            LoadingUI loadingUI = FindObjectOfType<LoadingUI>();
            Action<float> updateProgress = LoadingManager.Instance.UpdateProgress;

            updateProgress(0.1f);
            loadingUI?.SetLoadingText("오디오 파일 로드 중...");

            if (loadingUI != null)
            {
                SetRandomTip(loadingUI, audioLoadingTips);
            }

            yield return new WaitForSeconds(0.2f);

            updateProgress(0.3f);
            loadingUI?.SetLoadingText("오디오 파일 분석 중...");

            var progress = new Progress<float>(p =>
            {
                float currentProgress = 0.3f + p * 0.6f;
                updateProgress(currentProgress);
            });

            var addTrackTask = EditorDataManager.Instance.AddTrackAsync(
                pendingAudioFilePath,
                progress
            );

            while (!addTrackTask.IsCompleted)
            {
                if (loadingUI != null && UnityEngine.Random.value < 0.05f)
                {
                    SetRandomTip(loadingUI, audioLoadingTips);
                }
                yield return null;
            }

            TrackData newTrack = addTrackTask.Result;

            if (newTrack == null)
            {
                Debug.LogError("트랙 추가 실패");
                pendingAudioFilePath = null;
                isLoadingTrack = false;
                LoadingManager.Instance.LoadScene(currentSceneName);
                yield break;
            }

            updateProgress(0.9f);
            loadingUI?.SetLoadingText("트랙 정보 저장 중...");
            yield return new WaitForSeconds(0.2f);

            updateProgress(1.0f);
            pendingAudioFilePath = null;
            isLoadingTrack = false;

            // 트랙 정보 저장
            string trackName = newTrack.trackName;

            // 씬 전환 후 트랙 선택을 위해 콜백 사용
            LoadingManager.Instance.LoadScene(
                currentSceneName,
                () =>
                {
                    // 씬 전환 후 트랙 목록 갱신 및 선택
                    RefreshTrackList();

                    // 새로 추가된 트랙 찾기
                    TrackData trackToSelect = cachedTracks.FirstOrDefault(t =>
                        t.trackName == trackName
                    );
                    if (trackToSelect != null)
                    {
                        SelectTrack(trackToSelect);
                    }

                    // UI 업데이트
                    if (editorPanel != null && editorPanel.IsInitialized)
                    {
                        editorPanel.RefreshTrackList();
                    }
                }
            );
        }

        /// <summary>
        /// 앨범 아트 로드 프로세스
        /// </summary>
        private IEnumerator LoadAlbumArtProcess(int trackIndex)
        {
            if (string.IsNullOrEmpty(pendingAlbumArtFilePath))
            {
                LoadingManager.Instance.LoadScene(currentSceneName);
                yield break;
            }

            LoadingUI loadingUI = FindObjectOfType<LoadingUI>();
            Action<float> updateProgress = LoadingManager.Instance.UpdateProgress;

            updateProgress(0.2f);
            loadingUI?.SetLoadingText("앨범 아트 로드 중...");

            if (loadingUI != null)
            {
                SetRandomTip(loadingUI, albumArtLoadingTips);
            }

            yield return new WaitForSeconds(0.2f);

            updateProgress(0.4f);
            loadingUI?.SetLoadingText("이미지 파일 처리 중...");

            if (trackIndex >= cachedTracks.Count)
            {
                Debug.LogError("선택된 트랙 인덱스가 유효하지 않습니다.");
                pendingAlbumArtFilePath = null;
                LoadingManager.Instance.LoadScene(currentSceneName);
                yield break;
            }

            TrackData selectedTrack = cachedTracks[trackIndex];
            string trackName = selectedTrack.trackName;

            var progress = new Progress<float>(p =>
            {
                float currentProgress = 0.4f + p * 0.5f;
                updateProgress(currentProgress);
            });

            var setAlbumArtTask = EditorDataManager.Instance.SetAlbumArtAsync(
                trackName,
                pendingAlbumArtFilePath,
                progress
            );

            while (!setAlbumArtTask.IsCompleted)
            {
                if (loadingUI != null && UnityEngine.Random.value < 0.05f)
                {
                    SetRandomTip(loadingUI, albumArtLoadingTips);
                }
                yield return null;
            }

            TrackData updatedTrack = setAlbumArtTask.Result;

            if (updatedTrack == null)
            {
                Debug.LogError("앨범 아트 로드 실패");
                pendingAlbumArtFilePath = null;
                LoadingManager.Instance.LoadScene(currentSceneName);
                yield break;
            }

            updateProgress(0.9f);
            loadingUI?.SetLoadingText("앨범 아트 적용 중...");
            yield return new WaitForSeconds(0.2f);

            updateProgress(1.0f);
            pendingAlbumArtFilePath = null;

            // 씬 전환 후 UI 업데이트를 위해 콜백 사용
            LoadingManager.Instance.LoadScene(
                currentSceneName,
                () =>
                {
                    // 트랙 목록 갱신
                    RefreshTrackList();

                    // UI 업데이트
                    if (editorPanel != null && editorPanel.IsInitialized)
                    {
                        editorPanel.RefreshTrackList();

                        // 현재 선택된 트랙이 업데이트된 트랙이라면 UI 갱신
                        TrackData currentSelectedTrack = cachedTracks.FirstOrDefault(t =>
                            t.trackName == trackName
                        );
                        if (
                            currentTrack != null
                            && currentTrack.trackName == trackName
                            && currentSelectedTrack != null
                        )
                        {
                            editorPanel.ChangeTrack(currentSelectedTrack);
                        }
                    }
                }
            );
        }

        /// <summary>
        /// 랜덤 팁을 설정합니다.
        /// </summary>
        private void SetRandomTip(LoadingUI loadingUI, string[] tips)
        {
            if (tips.Length > 0)
            {
                int randomIndex = UnityEngine.Random.Range(0, tips.Length);
                loadingUI.SetLoadingText(tips[randomIndex]);
            }
        }

        /// <summary>
        /// 트랙의 BPM을 설정합니다.
        /// </summary>
        /// <param name="trackName">트랙 이름</param>
        /// <param name="bpm">설정할 BPM 값</param>
        public async Task SetBPMAsync(string trackName, float bpm)
        {
            if (EditorDataManager.Instance != null)
            {
                await EditorDataManager.Instance.SetBPMAsync(trackName, bpm);

                // 현재 선택된 트랙의 BPM이 변경되었다면 노트 에디터에 알림
                if (currentTrack != null && currentTrack.trackName == trackName)
                {
                    noteEditor.UpdateBPM(bpm);
                }

                // 트랙 목록 갱신
                RefreshTrackList();
            }
        }

        private void Update()
        {
            if (!isInitialized || editorCamera == null)
                return;

            HandleCameraMovement();
            HandleCameraZoom();
        }

        private void HandleCameraMovement()
        {
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");

            Vector3 movement = new Vector3(horizontal, 0, vertical) * cameraSpeed * Time.deltaTime;
            editorCamera.transform.Translate(movement, Space.World);
        }

        private void HandleCameraZoom()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0)
            {
                Vector3 cameraPosition = editorCamera.transform.position;
                cameraPosition.y -= scroll * zoomSpeed;
                cameraPosition.y = Mathf.Clamp(cameraPosition.y, 2f, 10f);
                editorCamera.transform.position = cameraPosition;
            }
        }

        protected override void OnDestroy()
        {
            if (editorControlActions != null)
            {
                var actionMap = editorControlActions.FindActionMap("NoteEditor");
                if (actionMap != null)
                {
                    actionMap.Disable();
                }
            }

            base.OnDestroy();
        }
    }
}
