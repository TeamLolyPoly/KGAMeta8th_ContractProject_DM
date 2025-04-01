using System;
using System.Collections;
using UnityEngine;

namespace NoteEditor
{
    public class AudioManager : Singleton<AudioManager>, IInitializable
    {
        public AudioSource currentAudioSource;
        public bool IsInitialized { get; private set; }

        private bool isPlaying = false;
        private float volume = 1.0f;
        public float Volume => volume;

        public int TotalBeats
        {
            get
            {
                if (currentAudioSource == null)
                    return 0;

                if (totalBars <= 0)
                {
                    return 0;
                }

                print(
                    $"[AudioManager] TotalBeats: {Mathf.RoundToInt(totalBars * EditorManager.Instance.CurrentNoteMap.beatsPerBar)} totalBars: {totalBars} currentBeatsPerBar: {EditorManager.Instance.CurrentNoteMap.beatsPerBar}"
                );

                return Mathf.RoundToInt(
                    totalBars * EditorManager.Instance.CurrentNoteMap.beatsPerBar
                );
            }
        }

        private int totalBars = 0;

        public int TotalBars => totalBars;

        public float CurrentBPM
        {
            get => EditorManager.Instance.CurrentNoteMap.bpm;
            set
            {
                if (EditorManager.Instance.CurrentNoteMap != null)
                {
                    EditorManager.Instance.CurrentNoteMap.bpm = value;
                }

                UpdateTotalBars();
            }
        }

        public int BeatsPerBar
        {
            get => EditorManager.Instance.CurrentNoteMap.beatsPerBar;
            set
            {
                if (EditorManager.Instance.CurrentNoteMap.beatsPerBar != value)
                {
                    EditorManager.Instance.CurrentNoteMap.beatsPerBar = value;
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

        public void Initialize()
        {
            currentAudioSource = gameObject.AddComponent<AudioSource>();
            IsInitialized = true;
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
            if (EditorManager.Instance.CurrentNoteMap != null && currentAudioSource.clip != null)
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
        public void SetTrack(AudioClip clip)
        {
            if (clip == null)
                return;

            currentAudioSource.clip = clip;
            currentPlaybackTime = 0;

            CurrentBPM = EditorManager.Instance.CurrentNoteMap.bpm;
            BeatsPerBar = EditorManager.Instance.CurrentNoteMap.beatsPerBar;
        }

        /// <summary>
        /// 총 마디 수를 업데이트합니다.
        /// </summary>
        private void UpdateTotalBars()
        {
            if (currentAudioSource != null && currentAudioSource.clip != null)
            {
                float clipDuration = currentAudioSource.clip.length;
                float secondsPerBeat = 60f / CurrentBPM;
                float secondsPerBar = secondsPerBeat * 4;
                float rawTotalBars = clipDuration / secondsPerBar;
                totalBars = Mathf.FloorToInt(rawTotalBars);
                print(
                    $"[AudioManager] UpdateTotalBars\n"
                        + $" currentNoteMap.beatsPerBar: {EditorManager.Instance.CurrentNoteMap.beatsPerBar}\n"
                        + $" currentBPM: {CurrentBPM}\n"
                        + $" secondsPerBeat: {secondsPerBeat}\n"
                        + $" secondsPerBar: {secondsPerBar}\n"
                        + $" clipDuration: {clipDuration}\n"
                        + $" rawTotalBars: {rawTotalBars}\n"
                        + $" totalBars: {totalBars}\n"
                );
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
