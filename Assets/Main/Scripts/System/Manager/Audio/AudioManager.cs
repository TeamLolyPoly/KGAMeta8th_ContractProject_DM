using System.Collections;
using UnityEngine;

namespace NoteEditor
{
    public class AudioManager : Singleton<AudioManager>, IInitializable
    {
        public TrackData currentTrack;
        public AudioSource currentAudioSource;
        public bool IsInitialized { get; private set; }

        private bool isPlaying = false;
        private float volume = 1.0f;

        private float currentBPM = 120f;
        private int currentBeatsPerBar = 4;
        private float totalBars = 0;

        public float TotalBars => totalBars;

        public float CurrentBPM
        {
            get => currentBPM;
            set
            {
                if (!Mathf.Approximately(currentBPM, value))
                {
                    currentBPM = value;

                    if (currentTrack != null)
                    {
                        currentTrack.bpm = value;
                    }

                    UpdateTotalBars();
                }
            }
        }

        public int BeatsPerBar
        {
            get => currentBeatsPerBar;
            set
            {
                if (currentBeatsPerBar != value)
                {
                    currentBeatsPerBar = value;
                    UpdateTotalBars();
                }
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
        /// 매니저를 초기화합니다.
        /// </summary>
        public void Initialize()
        {
            currentAudioSource = gameObject.AddComponent<AudioSource>();
            IsInitialized = true;
            Debug.Log("[AudioManager] 초기화 완료");
        }

        /// <summary>
        /// 재생/일시정지를 토글합니다.
        /// </summary>
        public void TogglePlayPause()
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
        /// 볼륨을 조정합니다.
        /// </summary>
        /// <param name="delta">볼륨 변화량</param>
        public void AdjustVolume(float delta)
        {
            volume = Mathf.Clamp01(volume + delta);
            if (currentAudioSource != null)
            {
                currentAudioSource.volume = volume;
            }
            Debug.Log($"Volume adjusted to: {volume}");
        }

        /// <summary>
        /// 트랙을 설정합니다. (EditorManager에서 호출)
        /// </summary>
        public void SetTrack(TrackData track, AudioClip clip)
        {
            if (track == null || clip == null)
                return;

            currentTrack = track;
            currentAudioSource.clip = clip;
            currentPlaybackTime = 0;

            CurrentBPM = track.bpm;
            BeatsPerBar = 4;

            UpdateTotalBars();
        }

        /// <summary>
        /// 총 마디 수를 업데이트합니다.
        /// </summary>
        private void UpdateTotalBars()
        {
            if (currentAudioSource != null && currentAudioSource.clip != null)
            {
                float clipDuration = currentAudioSource.clip.length;
                float secondsPerBeat = 60f / currentBPM;
                float secondsPerBar = secondsPerBeat * currentBeatsPerBar;
                totalBars = clipDuration / secondsPerBar;
            }
        }

        protected override void OnDestroy()
        {
            if (currentAudioSource != null)
            {
                currentAudioSource.Stop();
                currentAudioSource.clip = null;
            }

            base.OnDestroy();
        }
    }
}
