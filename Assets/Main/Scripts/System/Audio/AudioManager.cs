using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NoteEditor
{
    /// <summary>
    /// 오디오 재생 및 제어를 담당하는 매니저 클래스
    /// </summary>
    public class AudioManager : Singleton<AudioManager>, IInitializable
    {
        public TrackData currentTrack;
        public AudioSource currentAudioSource;
        public bool IsInitialized { get; private set; }

        private InputActionAsset audioControlActions;
        private int currentTrackIndex = 0;
        private InputAction playPauseAction;
        private bool isPlaying = false;
        private List<TrackData> cachedTracks = new List<TrackData>();

        public event Action<float> OnBPMChanged;
        private float currentBPM = 120f;
        public float CurrentBPM
        {
            get => currentBPM;
            set
            {
                if (currentBPM != value)
                {
                    currentBPM = value;
                    OnBPMChanged?.Invoke(currentBPM);

                    if (currentTrack != null && AudioDataManager.Instance != null)
                    {
                        currentTrack.bpm = value;
                        _ = UpdateTrackBPMAsync(currentTrack.trackName, value);
                    }
                }
            }
        }

        private async Task UpdateTrackBPMAsync(string trackName, float bpm)
        {
            try
            {
                await AudioDataManager.Instance.SetBPMAsync(trackName, bpm);
            }
            catch (Exception ex)
            {
                Debug.LogError($"BPM 업데이트 중 오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// 현재 재생 시간
        /// </summary>
        public float currentPlaybackTime
        {
            get => currentAudioSource != null ? currentAudioSource.time : 0f;
            set
            {
                if (currentAudioSource != null)
                    currentAudioSource.time = value;
            }
        }

        /// <summary>
        /// 현재 트랙의 총 재생 시간
        /// </summary>
        public float currentPlaybackDuration
        {
            get => currentAudioSource.clip != null ? currentAudioSource.clip.length : 0f;
        }

        /// <summary>
        /// 현재 재생 중인지 여부
        /// </summary>
        public bool IsPlaying => isPlaying;

        /// <summary>
        /// 트랙 변경 이벤트
        /// </summary>
        public event Action<TrackData> OnTrackChanged;

        public IEnumerator Start()
        {
            yield return new WaitUntil(() => AudioDataManager.Instance.IsInitialized);
            Initialize();
        }

        /// <summary>
        /// 매니저를 초기화합니다.
        /// </summary>
        public void Initialize()
        {
            SetupInputActions();

            if (AudioDataManager.Instance != null)
            {
                RefreshTrackList();

                AudioDataManager.Instance.OnTrackAdded += OnTrackAddedHandler;
                AudioDataManager.Instance.OnTrackRemoved += OnTrackRemovedHandler;
                AudioDataManager.Instance.OnTrackUpdated += OnTrackUpdatedHandler;
            }
            currentAudioSource = gameObject.AddComponent<AudioSource>();

            IsInitialized = true;
        }

        private void ClearAllListeners()
        {
            if (AudioDataManager.Instance != null)
            {
                AudioDataManager.Instance.OnTrackAdded -= OnTrackAddedHandler;
                AudioDataManager.Instance.OnTrackRemoved -= OnTrackRemovedHandler;
                AudioDataManager.Instance.OnTrackUpdated -= OnTrackUpdatedHandler;
            }
        }

        private void OnDisable()
        {
            ClearAllListeners();

            if (audioControlActions != null)
            {
                var actionMap = audioControlActions.FindActionMap("AudioPlayer");
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
            if (AudioDataManager.Instance != null)
            {
                cachedTracks = AudioDataManager.Instance.GetAllTracks();
            }
        }

        /// <summary>
        /// 트랙 추가 이벤트 핸들러
        /// </summary>
        private void OnTrackAddedHandler(TrackData track)
        {
            RefreshTrackList();

            if (currentTrack == null && cachedTracks.Count > 0)
            {
                SelectTrack(track);
            }
        }

        /// <summary>
        /// 트랙 제거 이벤트 핸들러
        /// </summary>
        private void OnTrackRemovedHandler(TrackData track)
        {
            RefreshTrackList();

            if (currentTrack == track)
            {
                if (cachedTracks.Count > 0)
                {
                    SelectTrack(cachedTracks[0]);
                }
                else
                {
                    currentTrack = null;
                    currentAudioSource.clip = null;
                    Stop();
                }
            }
        }

        /// <summary>
        /// 트랙 업데이트 이벤트 핸들러
        /// </summary>
        private void OnTrackUpdatedHandler(TrackData track)
        {
            RefreshTrackList();

            if (currentTrack != null && currentTrack.trackName == track.trackName)
            {
                currentTrack = track;

                if (track.TrackAudio != null && currentAudioSource.clip != track.TrackAudio)
                {
                    bool wasPlaying = isPlaying;
                    float currentTime = currentPlaybackTime;

                    currentAudioSource.clip = track.TrackAudio;
                    currentPlaybackTime = currentTime;

                    if (wasPlaying)
                    {
                        PlayCurrentTrack();
                    }
                }

                if (currentBPM != track.bpm)
                {
                    CurrentBPM = track.bpm;
                }
            }
        }

        /// <summary>
        /// 입력 액션을 설정합니다.
        /// </summary>
        private void SetupInputActions()
        {
            audioControlActions = Resources.Load<InputActionAsset>("Input/AudioControls");
            if (audioControlActions != null)
            {
                var actionMap = audioControlActions.FindActionMap("AudioPlayer");
                if (actionMap != null)
                {
                    playPauseAction = actionMap.FindAction("PlayPause");
                    if (playPauseAction != null)
                    {
                        playPauseAction.performed += ctx => TogglePlayPause();
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
                        volumeUpAction.performed += ctx => AdjustVolume(0.05f);
                    }

                    var volumeDownAction = actionMap.FindAction("VolumeDown");
                    if (volumeDownAction != null)
                    {
                        volumeDownAction.performed += ctx => AdjustVolume(-0.05f);
                    }

                    actionMap.Enable();
                }
                else
                {
                    Debug.LogError(
                        "AudioPlayer 액션맵을 찾을 수 없습니다. InputActionAsset 설정을 확인하세요."
                    );
                }
            }
            else
            {
                Debug.LogWarning(
                    "audioControlActions이 할당되지 않았습니다. Inspector에서 할당해주세요."
                );
            }
        }

        private void OnEnable()
        {
            if (audioControlActions != null)
            {
                var actionMap = audioControlActions.FindActionMap("AudioPlayer");
                if (actionMap != null)
                {
                    actionMap.Enable();
                }
            }
        }

        /// <summary>
        /// 재생/일시정지를 토글합니다.
        /// </summary>
        private void TogglePlayPause()
        {
            if (isPlaying)
            {
                Pause();
            }
            else
            {
                PlayCurrentTrack();
            }
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

            if (track.TrackAudio == null && AudioDataManager.Instance != null)
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
            var loadTask = AudioDataManager.Instance.LoadTrackAudioAsync(trackName);

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

            currentTrack = track;
            currentAudioSource.clip = track.TrackAudio;
            currentPlaybackTime = 0;

            currentTrackIndex = cachedTracks.IndexOf(track);
            if (currentTrackIndex < 0)
                currentTrackIndex = 0;

            CurrentBPM = track.bpm;

            var railGenerator = FindObjectOfType<RailGenerator>();
            if (railGenerator != null)
            {
                Debug.Log(
                    $"SelectTrackInternal - Track: {track.trackName}, BPM: {track.bpm}, Audio Length: {track.TrackAudio.length}s"
                );
                railGenerator.UpdateBeatSettings(track.bpm, 4);
                railGenerator.UpdateWaveform(track.TrackAudio);
            }

            OnTrackChanged?.Invoke(track);
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
        /// 현재 트랙을 재생합니다.
        /// </summary>
        public void PlayCurrentTrack()
        {
            if (currentTrack != null && currentAudioSource.clip != null)
            {
                double startTime = AudioSettings.dspTime;
                currentAudioSource.PlayScheduled(startTime);
                isPlaying = true;
            }
            else
            {
                Debug.LogWarning("재생할 트랙이 선택되지 않았습니다.");
            }
        }

        /// <summary>
        /// 재생 위치를 변경합니다.
        /// </summary>
        /// <param name="time">변경할 시간(초)</param>
        public void ChangePlaybackPosition(float time)
        {
            currentAudioSource.time = time;
        }

        /// <summary>
        /// 재생을 일시정지합니다.
        /// </summary>
        public void Pause()
        {
            currentAudioSource.Pause();
            isPlaying = false;
        }

        /// <summary>
        /// 일시정지된 재생을 재개합니다.
        /// </summary>
        public void Resume()
        {
            currentAudioSource.UnPause();
            isPlaying = true;
        }

        /// <summary>
        /// 재생을 중지합니다.
        /// </summary>
        public void Stop()
        {
            currentAudioSource.Stop();
            isPlaying = false;
        }

        /// <summary>
        /// 볼륨을 설정합니다.
        /// </summary>
        /// <param name="volume">설정할 볼륨(0-1)</param>
        public void SetVolume(float volume)
        {
            currentAudioSource.volume = volume;
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

                PlayCurrentTrack();
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

                PlayCurrentTrack();
            }
        }

        /// <summary>
        /// 볼륨을 조정합니다.
        /// </summary>
        /// <param name="delta">볼륨 변화량</param>
        public void AdjustVolume(float delta)
        {
            float newVolume = Mathf.Clamp01(currentAudioSource.volume + delta);
            SetVolume(newVolume);
        }

        protected override void OnDestroy()
        {
            if (currentAudioSource != null)
            {
                currentAudioSource.Stop();
                currentAudioSource.clip = null;
            }

            if (AudioDataManager.Instance != null)
            {
                AudioDataManager.Instance.OnTrackAdded -= OnTrackAddedHandler;
                AudioDataManager.Instance.OnTrackRemoved -= OnTrackRemovedHandler;
                AudioDataManager.Instance.OnTrackUpdated -= OnTrackUpdatedHandler;
            }

            if (audioControlActions != null)
            {
                var actionMap = audioControlActions.FindActionMap("AudioPlayer");
                if (actionMap != null)
                {
                    actionMap.Disable();
                }
            }

            base.OnDestroy();
        }
    }
}
