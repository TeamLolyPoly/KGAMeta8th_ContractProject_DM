using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace NoteEditor
{
    public class EditorManager : Singleton<EditorManager>, IInitializable
    {
        public RailController railController { get; private set; }
        public CellController cellController { get; private set; }
        public NoteEditor noteEditor { get; private set; }
        public EditorPanel editorPanel { get; private set; }
        public CameraController cameraController { get; private set; }
        private InputActionAsset editorControlActions;
        public TrackData CurrentTrack { get; private set; }
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

        private bool isInitialized = false;
        public bool IsInitialized => isInitialized;

        private void Start()
        {
            Initialize();
        }

        public void Initialize()
        {
            LoadingManager.Instance.LoadScene(
                "Editor_Start",
                InitializeAsync,
                () =>
                {
                    UIManager.Instance.OpenPanel(PanelType.EditorStart);
                }
            );
        }

        public IEnumerator InitializeAsync()
        {
            yield return 0;
            yield return new WaitForSeconds(0.3f);

            LoadingManager.Instance.SetLoadingText("오디오 시스템 초기화 중...");
            AudioManager.Instance.Initialize();
            yield return 0.5f;
            yield return new WaitForSeconds(1f);

            LoadingManager.Instance.SetLoadingText("데이터 시스템 초기화 중...");
            EditorDataManager.Instance.Initialize();
            yield return 0.8f;
            yield return new WaitForSeconds(1f);

            isInitialized = true;
            yield return 1f;
            yield return new WaitForSeconds(0.5f);
        }

        public void InstantiateControllers()
        {
            GameObject railObj = new GameObject("RailController");
            railObj.transform.SetParent(transform);
            railController = railObj.AddComponent<RailController>();

            GameObject cellObj = new GameObject("CellController");
            cellObj.transform.SetParent(transform);
            cellController = cellObj.AddComponent<CellController>();

            GameObject noteEditorObj = new GameObject("NoteEditor");
            noteEditorObj.transform.SetParent(transform);
            noteEditor = noteEditorObj.AddComponent<NoteEditor>();

            GameObject cameraObj = new GameObject("CameraController");
            cameraObj.transform.SetParent(transform);
            cameraController = cameraObj.AddComponent<CameraController>();
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

        public async void SelectTrack(TrackData track)
        {
            if (track == null)
            {
                Debug.LogWarning("선택한 트랙이 없습니다.");
                return;
            }

            if (track.TrackAudio == null)
            {
                Debug.Log("트랙의 오디오가 로드되지 않아 로드 중...");
                await EditorDataManager.Instance.LoadTrackAudioAsync(track.trackName);
            }

            CurrentTrack = track;

            LoadingManager.Instance.LoadScene(
                "Editor_Main",
                InitializeEditorAsync,
                () =>
                {
                    cameraController.Initialize();

                    UIManager.Instance.CloseAllPanels();

                    editorPanel = UIManager.Instance.OpenPanel(PanelType.NoteEditor) as EditorPanel;

                    Debug.Log(
                        $"트랙 선택됨: {CurrentTrack.trackName}, BPM: {CurrentTrack.bpm}, 길이: {CurrentTrack.TrackAudio.length}초"
                    );
                }
            );
        }

        private IEnumerator InitializeEditorAsync()
        {
            yield return 0;
            yield return new WaitForSeconds(0.3f);

            LoadingManager.Instance.SetLoadingText("컨트롤러 생성 중...");
            InstantiateControllers();
            yield return 0.1f;
            yield return new WaitForSeconds(1f);

            LoadingManager.Instance.SetLoadingText("레일 초기화 중...");
            railController.Initialize();
            yield return 0.2f;
            yield return new WaitUntil(() => railController.IsInitialized);

            LoadingManager.Instance.SetLoadingText("셀 초기화 중...");
            cellController.Initialize();
            yield return 0.3f;
            yield return new WaitUntil(() => cellController.IsInitialized);

            LoadingManager.Instance.SetLoadingText("노트 에디터 초기화 중...");
            noteEditor.Initialize();
            yield return 0.4f;
            yield return new WaitUntil(() => noteEditor.IsInitialized);

            LoadingManager.Instance.SetLoadingText("트랙 설정 중...");
            AudioManager.Instance.SetTrack(CurrentTrack, CurrentTrack.TrackAudio);
            yield return 0.5f;
            yield return new WaitForSeconds(1f);

            LoadingManager.Instance.SetLoadingText("입력 설정 중...");
            SetupInputActions();
            yield return 0.7f;
            yield return new WaitForSeconds(1f);

            LoadingManager.Instance.SetLoadingText("노트 에디터 트랙 설정 중...");
            noteEditor.SetTrack(CurrentTrack);
            yield return 0.8f;
            yield return new WaitForSeconds(1f);

            yield return 0.9f;
            yield return new WaitForSeconds(1f);

            yield return 1f;
            yield return new WaitForSeconds(1f);
        }

        public async void RemoveTrack(TrackData track)
        {
            await EditorDataManager.Instance.DeleteTrackAsync(track.trackName);
        }

        public void LoadTrack(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || isLoadingTrack)
                return;

            pendingAudioFilePath = filePath;
            currentSceneName = SceneManager.GetActiveScene().name;
            isLoadingTrack = true;

            LoadingManager.Instance.LoadScene(
                LoadingManager.LOADING_SCENE_NAME,
                () => StartCoroutine(LoadAudioFileProcess())
            );
        }

        public void SetAlbumArt(string filePath, TrackData track)
        {
            if (string.IsNullOrEmpty(filePath))
                return;

            pendingAlbumArtFilePath = filePath;
            currentSceneName = SceneManager.GetActiveScene().name;

            LoadingManager.Instance.LoadScene(
                LoadingManager.LOADING_SCENE_NAME,
                () => StartCoroutine(LoadAlbumArtProcess(track))
            );
        }

        private IEnumerator LoadAudioFileProcess()
        {
            if (string.IsNullOrEmpty(pendingAudioFilePath))
            {
                LoadingManager.Instance.LoadScene(currentSceneName);
                isLoadingTrack = false;
                yield break;
            }

            LoadingPanel loadingUI = FindObjectOfType<LoadingPanel>();
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

            string trackName = newTrack.trackName;

            LoadingManager.Instance.LoadScene(currentSceneName);
        }

        private IEnumerator LoadAlbumArtProcess(TrackData track)
        {
            if (string.IsNullOrEmpty(pendingAlbumArtFilePath))
            {
                LoadingManager.Instance.LoadScene(currentSceneName);
                yield break;
            }

            LoadingPanel loadingUI = FindObjectOfType<LoadingPanel>();
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

            string trackName = track.trackName;

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

            LoadingManager.Instance.LoadScene(currentSceneName);
        }

        private void SetRandomTip(LoadingPanel loadingUI, string[] tips)
        {
            if (tips.Length > 0)
            {
                int randomIndex = UnityEngine.Random.Range(0, tips.Length);
                loadingUI.SetLoadingText(tips[randomIndex]);
            }
        }

        public async Task SetBPMAsync(TrackData track, float bpm)
        {
            if (EditorDataManager.Instance != null)
            {
                await EditorDataManager.Instance.SetBPMAsync(track.trackName, bpm);

                if (CurrentTrack != null && CurrentTrack.trackName == track.trackName)
                {
                    noteEditor.UpdateBPM(bpm);
                }
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
