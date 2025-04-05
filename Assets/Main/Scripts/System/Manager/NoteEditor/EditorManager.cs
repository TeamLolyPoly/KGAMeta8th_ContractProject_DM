using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using SFB;
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
        public NoteMapData CurrentNoteMapData { get; set; }
        public NoteMap CurrentNoteMap { get; set; }
        private string pendingAudioFilePath;
        private string pendingAlbumArtFilePath;
        private string currentSceneName;

        private bool isInitialized = false;
        public bool IsInitialized => isInitialized;

        private void Start()
        {
            Initialize();
        }

        public void Initialize()
        {
            EditorLoadingManager.Instance.LoadScene(
                "Editor_Start",
                InitializeAsync,
                () =>
                {
                    EditorUIManager.Instance.OpenPanel(PanelType.EditorStart);
                }
            );
        }

        public void BackToMain()
        {
            EditorLoadingManager.Instance.LoadScene(
                "Editor_Start",
                Clear,
                () =>
                {
                    EditorUIManager.Instance.OpenPanel(PanelType.EditorStart);
                }
            );
        }

        public IEnumerator Clear()
        {
            var actionMap = editorControlActions.FindActionMap("NoteEditor");
            if (actionMap != null)
            {
                actionMap.Disable();
            }
            CurrentNoteMap = null;
            EditorLoadingManager.Instance.SetLoadingText("트랙 초기화 중...");
            CurrentTrack = null;
            CurrentNoteMapData = null;
            AudioManager.Instance.Stop();
            AudioManager.Instance.currentAudioSource.clip = null;
            yield return 0.2f;
            yield return new WaitForSeconds(0.3f);
            EditorLoadingManager.Instance.SetLoadingText("셀 초기화 중...");
            if (cellController != null)
            {
                cellController.Cleanup();
            }
            yield return 0.4f;
            yield return new WaitForSeconds(0.3f);
            EditorLoadingManager.Instance.SetLoadingText("레일 초기화 중...");
            if (railController != null)
            {
                railController.Cleanup();
            }
            yield return 0.6f;
            yield return new WaitForSeconds(0.3f);
            EditorLoadingManager.Instance.SetLoadingText("노트 에디터 초기화 중...");
            if (noteEditor != null)
            {
                noteEditor.Cleanup();
            }

            yield return 0.8f;
            yield return new WaitForSeconds(0.3f);
            yield return 1f;
            yield return new WaitForSeconds(0.5f);
        }

        public IEnumerator InitializeAsync()
        {
            yield return 0;
            SceneManager.sceneLoaded += (scene, mode) =>
            {
                currentSceneName = scene.name;
            };
            yield return new WaitForSeconds(0.3f);

            EditorLoadingManager.Instance.SetLoadingText("오디오 시스템 초기화 중...");
            AudioManager.Instance.Initialize();
            yield return 0.5f;
            yield return new WaitForSeconds(1f);

            EditorLoadingManager.Instance.SetLoadingText("데이터 시스템 초기화 중...");
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

        public async void SelectTrack(TrackData track, NoteMapData noteMapData)
        {
            if (track == null)
            {
                Debug.LogWarning("선택한 트랙이 없습니다.");
                return;
            }

            if (track.TrackAudio == null)
            {
                Debug.Log("트랙의 오디오가 로드되지 않아 로드 중...");
                await EditorDataManager.Instance.LoadTrackAudioAsync(track);
            }

            CurrentTrack = track;

            CurrentNoteMapData = noteMapData;

            CurrentNoteMap = noteMapData.noteMap;

            EditorLoadingManager.Instance.LoadScene(
                "Editor_Main",
                InitializeEditorAsync,
                () =>
                {
                    cameraController.Initialize();

                    editorPanel =
                        EditorUIManager.Instance.OpenPanel(PanelType.NoteEditor) as EditorPanel;

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

            EditorLoadingManager.Instance.SetLoadingText("컨트롤러 생성 중...");
            InstantiateControllers();
            yield return 0.1f;
            yield return new WaitForSeconds(1f);

            EditorLoadingManager.Instance.SetLoadingText("레일 초기화 중...");
            railController.Initialize();
            yield return 0.2f;
            yield return new WaitUntil(() => railController.IsInitialized);

            EditorLoadingManager.Instance.SetLoadingText("셀 초기화 중...");
            cellController.Initialize();
            yield return 0.3f;
            yield return new WaitUntil(() => cellController.IsInitialized);

            EditorLoadingManager.Instance.SetLoadingText("노트 에디터 초기화 중...");
            noteEditor.Initialize();
            yield return 0.4f;
            yield return new WaitUntil(() => noteEditor.IsInitialized);

            EditorLoadingManager.Instance.SetLoadingText("트랙 설정 중...");
            AudioManager.Instance.SetTrack(CurrentTrack.TrackAudio);
            yield return 0.5f;
            yield return new WaitForSeconds(1f);

            EditorLoadingManager.Instance.SetLoadingText("입력 설정 중...");
            SetupInputActions();
            yield return 0.7f;
            yield return new WaitForSeconds(1f);

            EditorLoadingManager.Instance.SetLoadingText("노트 에디터 트랙 설정 중...");
            noteEditor.SetTrack();
            yield return 0.8f;
            yield return new WaitForSeconds(1f);

            yield return 0.9f;
            yield return new WaitForSeconds(1f);

            yield return 1f;
            yield return new WaitForSeconds(1f);
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

                    var SettingsAction = actionMap.FindAction("Setting");
                    if (SettingsAction != null)
                    {
                        SettingsAction.performed += ctx =>
                            EditorUIManager.Instance.ToggleSettignsPanel();
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

        public void LoadTrack(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return;

            pendingAudioFilePath = filePath;
            ExtensionFilter[] extensions =
            {
                new ExtensionFilter("앨범아트 파일", "png", "jpg", "jpeg"),
            };

            StandaloneFileBrowser.OpenFilePanelAsync(
                "앨범아트 파일 선택",
                "",
                extensions,
                false,
                (string[] paths) =>
                {
                    if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
                    {
                        pendingAlbumArtFilePath = paths[0];
                    }
                }
            );

            EditorLoadingManager.Instance.LoadScene(
                currentSceneName,
                LoadTrackProcess,
                OnTrackLoaded
            );
        }

        public void SetAlbumArt(string filePath, TrackData track, Action onLoaded = null)
        {
            if (string.IsNullOrEmpty(filePath))
                return;

            pendingAlbumArtFilePath = filePath;

            EditorLoadingManager.Instance.LoadScene(
                currentSceneName,
                () => LoadAlbumArtProcess(track),
                onLoaded
            );
        }

        private IEnumerator LoadTrackProcess()
        {
            if (string.IsNullOrEmpty(pendingAudioFilePath))
            {
                EditorLoadingManager.Instance.LoadScene(currentSceneName);
                yield break;
            }

            yield return new WaitForSeconds(0.2f);

            IProgress<float> progress = new Progress<float>(p =>
            {
                EditorLoadingManager.Instance.UpdateProgress(p);
            });

            var addTrackTask = EditorDataManager.Instance.AddTrackAsync(
                pendingAudioFilePath,
                progress
            );

            while (!addTrackTask.IsCompleted)
            {
                yield return null;
            }

            CurrentTrack = addTrackTask.Result;

            CurrentNoteMapData = CurrentTrack.noteMapData.FirstOrDefault(n =>
                n.difficulty == Difficulty.Easy
            );

            CurrentNoteMap = CurrentNoteMapData.noteMap;

            if (pendingAlbumArtFilePath != null)
            {
                var albumArtTask = EditorDataManager.Instance.SetAlbumArtAsync(
                    CurrentTrack.trackName,
                    pendingAlbumArtFilePath
                );

                while (!albumArtTask.IsCompleted)
                {
                    yield return null;
                }

                CurrentTrack = albumArtTask.Result;
            }

            if (CurrentTrack == null)
            {
                pendingAudioFilePath = null;
                EditorLoadingManager.Instance.LoadScene(
                    currentSceneName,
                    () =>
                    {
                        EditorUIManager.Instance.OpenPopUp("오류", "트랙 추가 실패");
                    }
                );
                yield break;
            }

            yield return new WaitForSeconds(0.2f);

            pendingAudioFilePath = null;
            pendingAlbumArtFilePath = null;
        }

        private IEnumerator LoadAlbumArtProcess(TrackData track)
        {
            if (string.IsNullOrEmpty(pendingAlbumArtFilePath))
            {
                yield break;
            }

            Action<float> updateProgress = EditorLoadingManager.Instance.UpdateProgress;

            updateProgress(0.2f);
            EditorLoadingManager.Instance.SetLoadingText("앨범 아트 로드 중...");

            yield return new WaitForSeconds(0.2f);

            updateProgress(0.4f);
            EditorLoadingManager.Instance.SetLoadingText("이미지 파일 처리 중...");

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
                yield return null;
            }

            TrackData updatedTrack = setAlbumArtTask.Result;

            if (updatedTrack == null)
            {
                Debug.LogError("앨범 아트 로드 실패");
                pendingAlbumArtFilePath = null;
                yield break;
            }

            updateProgress(0.9f);
            EditorLoadingManager.Instance.SetLoadingText("앨범 아트 적용 중...");
            yield return new WaitForSeconds(0.2f);

            updateProgress(1.0f);
            pendingAlbumArtFilePath = null;
        }

        public void OnTrackLoaded()
        {
            if (currentSceneName == "Editor_Start")
            {
                EditorUIManager.Instance.OpenPanel(PanelType.EditorStart);
                EditorUIManager.Instance.ClosePanel(PanelType.EditorStart);
                EditorUIManager.Instance.OpenPanel(PanelType.NewTrack);
                NewTrackPanel newTrackPanel =
                    EditorUIManager.Instance.GetPanel(PanelType.NewTrack) as NewTrackPanel;
                newTrackPanel.SetInfo(CurrentTrack);
                EditorDataManager.Instance.TmpTrack = CurrentTrack;
            }
        }

        public async void RemoveTrack(TrackData track)
        {
            if (track != null)
                await EditorDataManager.Instance.DeleteTrackAsync(track);
        }

        public async void UpdateTrackInfo(
            TrackData track,
            string bpm = null,
            string trackName = null,
            string artistName = null,
            string albumName = null,
            string year = null,
            string genre = null
        )
        {
            if (track != null)
            {
                if (bpm != null)
                {
                    track.bpm = float.Parse(bpm);
                    foreach (var noteMapData in track.noteMapData)
                    {
                        noteMapData.noteMap.bpm = track.bpm;
                    }
                    SaveNoteMapAsync(track);
                }
                if (trackName != null)
                    track.trackName = trackName;
                if (artistName != null)
                    track.artistName = artistName;
                if (albumName != null)
                    track.albumName = albumName;
                if (year != null)
                    track.year = int.Parse(year);
                if (genre != null)
                    track.genre = genre;
                await EditorDataManager.Instance.UpdateSingleTrackAsync(track);
            }
        }

        public async Task SetBPMAsync(float bpm)
        {
            if (CurrentTrack != null)
            {
                await EditorDataManager.Instance.SetBPMAsync(CurrentTrack, bpm);

                AudioManager.Instance.CurrentBPM = bpm;

                noteEditor.UpdateBPM(bpm);
            }
        }

        public async void SaveNoteMapAsync(TrackData track = null)
        {
            if (track != null)
            {
                await EditorDataManager.Instance.SaveNoteMapAsync(track);
            }
            else if (CurrentTrack != null)
            {
                await EditorDataManager.Instance.SaveNoteMapAsync(CurrentTrack);
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
