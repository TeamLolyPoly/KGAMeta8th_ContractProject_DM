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

        /// <summary>
        /// 입력 액션을 설정합니다.
        /// </summary>
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
                        nextTrackAction.performed += ctx => AudioManager.Instance.NextTrack();
                    }

                    var prevTrackAction = actionMap.FindAction("PreviousTrack");
                    if (prevTrackAction != null)
                    {
                        prevTrackAction.performed += ctx => AudioManager.Instance.PreviousTrack();
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

            // AudioManager 이벤트 구독
            AudioManager.Instance.OnTrackChanged += OnTrackChangedHandler;
            AudioManager.Instance.OnBPMChanged += OnBPMChangedHandler;
            AudioManager.Instance.OnTotalBarsChanged += OnTotalBarsChangedHandler;

            // EditorDataManager 이벤트 구독
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
                AudioManager.Instance.OnTotalBarsChanged -= OnTotalBarsChangedHandler;
            }

            if (EditorDataManager.Instance != null)
            {
                EditorDataManager.Instance.OnTrackAdded -= OnTrackAddedHandler;
                EditorDataManager.Instance.OnTrackRemoved -= OnTrackRemovedHandler;
                EditorDataManager.Instance.OnTrackUpdated -= OnTrackUpdatedHandler;
                EditorDataManager.Instance.OnTrackLoaded -= OnTrackLoadedHandler;
            }
        }

        private void OnTrackChangedHandler(TrackData track)
        {
            Debug.Log($"EditorManager: Track changed to {track.trackName}");

            // 각 컴포넌트에 트랙 변경 알림
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

            // 기타 필요한 컴포넌트들에 트랙 변경 알림
            if (cellController != null && cellController.IsInitialized)
            {
                cellController.Initialize();
            }

            // NoteEditorPanel에 트랙 변경 알림
            if (editorPanel != null && editorPanel.IsInitialized)
            {
                editorPanel.OnTrackChangedHandler(track);
            }
        }

        private void OnTrackAddedHandler(TrackData track)
        {
            if (noteEditor != null && noteEditor.IsInitialized)
            {
                noteEditor.OnTrackAddedHandler(track);
            }
        }

        private void OnTrackUpdatedHandler(TrackData track)
        {
            if (noteEditor != null && noteEditor.IsInitialized)
            {
                noteEditor.OnTrackUpdatedHandler(track);
            }
        }

        private void OnTrackRemovedHandler(TrackData track)
        {
            if (noteEditor != null && noteEditor.IsInitialized)
            {
                noteEditor.OnTrackRemovedHandler(track);
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

        private void OnTotalBarsChangedHandler(float totalBars)
        {
            Debug.Log($"EditorManager: Total bars changed to {totalBars}");

            if (noteEditor != null && noteEditor.IsInitialized)
            {
                noteEditor.UpdateTotalBars(totalBars);
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
