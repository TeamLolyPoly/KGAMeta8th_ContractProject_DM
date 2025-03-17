using System;
using System.Collections;
using System.Collections.Generic;
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
            GetResources();
            StartCoroutine(InitializeComponents());
        }

        private IEnumerator InitializeComponents()
        {
            yield return new WaitUntil(() => AudioManager.Instance.IsInitialized);
            yield return new WaitUntil(() => EditorDataManager.Instance.IsInitialized);

            RefreshTrackList();

            if (railController != null && !railController.IsInitialized)
            {
                railController.Initialize();
            }

            if (cellController != null && !cellController.IsInitialized)
            {
                cellController.Initialize();
            }

            if (noteEditor != null && !noteEditor.IsInitialized)
            {
                noteEditor.Initialize();
            }

            if (editorPanel != null && !editorPanel.IsInitialized)
            {
                editorPanel.Initialize();
            }

            editorCamera = Camera.main;

            if (editorCamera != null)
            {
                editorCamera.transform.position = new Vector3(0, 5, -5);
                editorCamera.transform.rotation = Quaternion.Euler(30, 0, 0);
            }

            SetupInputActions();
            SubscribeToEvents();

            // 초기 트랙 선택
            if (cachedTracks.Count > 0)
            {
                SelectTrack(cachedTracks[0]);
            }

            isInitialized = true;
            Debug.Log("[EditorManager] 초기화 완료");
        }

        public void GetResources()
        {
            railController = new GameObject("RailController").AddComponent<RailController>();
            cellController = new GameObject("CellController").AddComponent<CellController>();
            noteEditor = new GameObject("NoteEditor").AddComponent<NoteEditor>();
            waveformDisplay = new GameObject("WaveformDisplay").AddComponent<WaveformDisplay>();
            editorPanel = Instantiate(
                Resources.Load<NoteEditorPanel>("Prefabs/NoteEditor/UI_Panel_NoteEditor"),
                GameObject.Find("Canvas").transform
            );
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

        private void SubscribeToEvents()
        {
            Debug.Log("EditorManager: Subscribing to events");

            AudioManager.Instance.OnTrackChanged += OnTrackChangedHandler;
            AudioManager.Instance.OnBPMChanged += OnBPMChangedHandler;

            EditorDataManager.Instance.OnTrackAdded += OnTrackAddedHandler;
            EditorDataManager.Instance.OnTrackRemoved += OnTrackRemovedHandler;
            EditorDataManager.Instance.OnTrackUpdated += OnTrackUpdatedHandler;
            EditorDataManager.Instance.OnTrackLoaded += OnTrackLoadedHandler;
        }

        private void UnsubscribeFromEvents()
        {
            Debug.Log("EditorManager: Unsubscribing from events");

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.OnTrackChanged -= OnTrackChangedHandler;
                AudioManager.Instance.OnBPMChanged -= OnBPMChangedHandler;
            }

            if (EditorDataManager.Instance != null)
            {
                EditorDataManager.Instance.OnTrackAdded -= OnTrackAddedHandler;
                EditorDataManager.Instance.OnTrackRemoved -= OnTrackRemovedHandler;
                EditorDataManager.Instance.OnTrackUpdated -= OnTrackUpdatedHandler;
                EditorDataManager.Instance.OnTrackLoaded -= OnTrackLoadedHandler;
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
                StartCoroutine(LoadTrackAudioAndSelect(track.trackName));
                return;
            }

            SelectTrackInternal(track);
        }

        /// <summary>
        /// 트랙 오디오를 로드하고 선택하는 코루틴
        /// </summary>
        private IEnumerator LoadTrackAudioAndSelect(string trackName)
        {
            var loadTask = EditorDataManager.Instance.LoadTrackAudioAsync(trackName);

            while (!loadTask.IsCompleted)
            {
                yield return null;
            }

            if (loadTask.Result != null && loadTask.Result.TrackAudio != null)
            {
                SelectTrackInternal(loadTask.Result);
            }
            else
            {
                Debug.LogWarning($"트랙 '{trackName}'의 오디오 로드에 실패했습니다.");
            }
        }

        /// <summary>
        /// 트랙을 내부적으로 선택합니다.
        /// </summary>
        /// <param name="track">선택할 트랙</param>
        private void SelectTrackInternal(TrackData track)
        {
            if (track == null || track.TrackAudio == null)
            {
                Debug.LogWarning("선택한 트랙이 없거나 오디오가 로드되지 않았습니다.");
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

            currentTrackIndex = cachedTracks.IndexOf(track);
            if (currentTrackIndex < 0)
                currentTrackIndex = 0;

            // AudioManager에 트랙 설정
            AudioManager.Instance.SetTrack(track, track.TrackAudio);

            Debug.Log(
                $"SelectTrackInternal - Track: {track.trackName}, BPM: {track.bpm}, Audio Length: {track.TrackAudio.length}s"
            );
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

        private void OnTrackChangedHandler(TrackData track)
        {
            Debug.Log($"EditorManager: Track changed to {track.trackName}");

            if (waveformDisplay != null && waveformDisplay.IsInitialized)
            {
                waveformDisplay.OnTrackChanged(track);
            }

            if (railController != null && railController.IsInitialized)
            {
                railController.UpdateBeatSettings(track.bpm, AudioManager.Instance.BeatsPerBar);
                if (track.TrackAudio != null)
                {
                    railController.SetAudioClip(track.TrackAudio);
                }
            }

            if (noteEditor != null && noteEditor.IsInitialized)
            {
                noteEditor.OnTrackChanged(track);
            }

            if (cellController != null && cellController.IsInitialized)
            {
                cellController.Initialize();
            }

            if (editorPanel != null && editorPanel.IsInitialized)
            {
                editorPanel.OnTrackChangedHandler(track);
            }
        }

        private void OnTrackAddedHandler(TrackData track)
        {
            RefreshTrackList();

            if (noteEditor != null && noteEditor.IsInitialized)
            {
                noteEditor.OnTrackAddedHandler(track);
            }

            if (AudioManager.Instance.currentTrack == null && cachedTracks.Count > 0)
            {
                SelectTrack(track);
            }
        }

        private void OnTrackUpdatedHandler(TrackData track)
        {
            RefreshTrackList();

            if (noteEditor != null && noteEditor.IsInitialized)
            {
                noteEditor.OnTrackUpdatedHandler(track);
            }

            if (
                AudioManager.Instance.currentTrack != null
                && AudioManager.Instance.currentTrack.trackName == track.trackName
            )
            {
                if (
                    track.TrackAudio != null
                    && AudioManager.Instance.currentAudioSource.clip != track.TrackAudio
                )
                {
                    bool wasPlaying = AudioManager.Instance.IsPlaying;
                    float currentTime = AudioManager.Instance.currentPlaybackTime;

                    // 트랙 업데이트
                    AudioManager.Instance.SetTrack(track, track.TrackAudio);
                    AudioManager.Instance.currentPlaybackTime = currentTime;

                    if (wasPlaying)
                    {
                        AudioManager.Instance.PlayCurrentTrack();
                    }
                }

                // BPM이 변경되었으면 업데이트
                if (!Mathf.Approximately(AudioManager.Instance.CurrentBPM, track.bpm))
                {
                    AudioManager.Instance.CurrentBPM = track.bpm;
                }
            }
        }

        private void OnTrackRemovedHandler(TrackData track)
        {
            RefreshTrackList();

            if (noteEditor != null && noteEditor.IsInitialized)
            {
                noteEditor.OnTrackRemovedHandler(track);
            }

            if (AudioManager.Instance.currentTrack == track)
            {
                if (cachedTracks.Count > 0)
                {
                    SelectTrack(cachedTracks[0]);
                }
                else
                {
                    // 트랙이 없는 경우 AudioManager 초기화
                    AudioManager.Instance.currentTrack = null;
                    AudioManager.Instance.currentAudioSource.clip = null;
                    AudioManager.Instance.Stop();
                }
            }
        }

        private void OnTrackLoadedHandler(TrackData track)
        {
            Debug.Log($"EditorManager: Track loaded: {track.trackName}");

            if (noteEditor != null && noteEditor.IsInitialized)
            {
                noteEditor.OnTrackLoadedHandler(track);
            }
        }

        private void OnBPMChangedHandler(float newBpm)
        {
            Debug.Log($"EditorManager: BPM changed to {newBpm}");

            if (railController != null && railController.IsInitialized)
            {
                railController.UpdateBeatSettings(newBpm, AudioManager.Instance.BeatsPerBar);
            }

            if (noteEditor != null)
            {
                noteEditor.OnBPMChangedHandler(newBpm);
            }

            if (editorPanel != null && editorPanel.IsInitialized)
            {
                editorPanel.OnBPMChangedHandler(newBpm);
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
            UnsubscribeFromEvents();

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
