using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Michsky.UI.Heat;
using SFB;
using TMPro;
using UnityEngine;
using Dropdown = Michsky.UI.Heat.Dropdown;

namespace NoteEditor
{
    public class NoteEditorPanel : MonoBehaviour, IInitializable
    {
        public BoxButtonManager CurrentTrackInfo;
        public ButtonManager playButton;
        public ButtonManager LoadTrackButton;
        public TextMeshProUGUI currentTrackPlaybackTime;
        public Dropdown trackDropdown;
        public BoxButtonManager SetAlbumArtButton;
        public ButtonManager DeleteTrackButton;
        public InputFieldManager BPMInput;

        public bool IsInitialized { get; private set; }
        private Sprite defaultAlbumArt;
        private Sprite dropdownItemIcon;

        private bool isPlaying = false;
        private List<TrackData> trackDataList = new List<TrackData>();

        private bool isLoadingTrack = false;
        private bool isLoadingAlbumArt = false;

        private AudioDataManager audioDataManager;

        public IEnumerator Start()
        {
            yield return new WaitUntil(() => AudioManager.Instance.IsInitialized);

            Initialize();
        }

        public void Initialize()
        {
            audioDataManager = AudioDataManager.Instance;
            defaultAlbumArt = Resources.Load<Sprite>("Textures/AlbumArt");
            dropdownItemIcon = Resources.Load<Sprite>("Textures/DefaultAudioIcon");
            playButton.onClick.AddListener(OnPlayButtonClicked);
            LoadTrackButton.onClick.AddListener(OnLoadTrackButtonClicked);
            if (SetAlbumArtButton != null)
            {
                SetAlbumArtButton.onClick.AddListener(OnSetAlbumArtButtonClicked);
            }
            if (DeleteTrackButton != null)
            {
                DeleteTrackButton.onClick.AddListener(
                    async () => await OnDeleteTrackButtonClicked()
                );
            }

            if (BPMInput != null)
            {
                BPMInput.onSubmit.AddListener(() => OnBPMInputSubmit(BPMInput.inputText.text));
            }

            if (audioDataManager != null)
            {
                audioDataManager.OnTrackLoaded += OnTrackLoaded;
                audioDataManager.OnAlbumArtLoaded += OnAlbumArtLoaded;
            }

            InitializeTrackDropdown();
            IsInitialized = true;
        }

        private void OnDestroy()
        {
            if (Application.isPlaying)
            {
                if (audioDataManager != null)
                {
                    audioDataManager.OnTrackLoaded -= OnTrackLoaded;
                    audioDataManager.OnAlbumArtLoaded -= OnAlbumArtLoaded;
                }
            }
        }

        private void SetTrackBPM(int bpm)
        {
            if (bpm <= 0)
            {
                Debug.LogWarning("BPM은 0보다 커야 합니다.");
                return;
            }

            if (trackDataList.Count == 0 || trackDropdown.selectedItemIndex < 0)
            {
                Debug.LogWarning("선택된 트랙이 없습니다.");
                return;
            }

            AudioManager.Instance.CurrentBPM = bpm;
        }

        private void OnBPMInputSubmit(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                if (trackDataList.Count > 0 && trackDropdown.selectedItemIndex >= 0)
                {
                    TrackData selectedTrack = trackDataList[trackDropdown.selectedItemIndex];
                    BPMInput.inputText.text = selectedTrack.bpm.ToString();
                }
            }
            else if (int.TryParse(value, out int bpm))
            {
                SetTrackBPM(bpm);
            }
        }

        public void InitializeTrackDropdown()
        {
            trackDataList.Clear();
            List<TrackData> tracks = AudioManager.Instance.GetAllTrackInfo();
            Debug.Log($"트랙 데이터 로드: {tracks.Count}개 트랙 발견");
            trackDataList = tracks;

            trackDropdown.items.Clear();

            foreach (TrackData track in tracks)
            {
                Debug.Log(
                    $"트랙 추가: {track.trackName}, 앨범아트: {(track.AlbumArt != null ? "있음" : "없음")}"
                );
                try
                {
                    trackDropdown.CreateNewItem(track.trackName, dropdownItemIcon, false);
                }
                catch (Exception e)
                {
                    Debug.LogError($"드롭다운 아이템 추가 오류: {e.Message}");
                }
            }

            try
            {
                if (tracks.Count > 0)
                {
                    trackDropdown.Initialize();
                }
                else
                {
                    trackDropdown.CreateNewItem("No Track", dropdownItemIcon, false);
                    trackDropdown.Initialize();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"드롭다운 초기화 오류: {e.Message}");
            }

            trackDropdown.onValueChanged.AddListener(OnTrackDropdownValueChanged);

            if (tracks.Count > 0)
            {
                trackDropdown.selectedItemIndex = 0;
                AudioManager.Instance.SelectTrack(tracks[0]);
                CurrentTrackInfo.SetText(tracks[0].trackName);
                if (tracks[0].AlbumArt != null)
                {
                    CurrentTrackInfo.SetBackground(tracks[0].AlbumArt);
                }
                else
                {
                    CurrentTrackInfo.SetBackground(defaultAlbumArt);
                }
            }
            else
            {
                CurrentTrackInfo.SetText("No Track");
                CurrentTrackInfo.SetBackground(defaultAlbumArt);
            }
        }

        private void OnTrackDropdownValueChanged(int selectedIndex)
        {
            if (selectedIndex >= 0 && selectedIndex < trackDataList.Count)
            {
                TrackData selectedTrack = trackDataList[selectedIndex];
                CurrentTrackInfo.SetText(selectedTrack.trackName);
                if (selectedTrack.AlbumArt != null)
                {
                    CurrentTrackInfo.SetBackground(selectedTrack.AlbumArt);
                }
                else
                {
                    CurrentTrackInfo.SetBackground(defaultAlbumArt);
                }
                AudioManager.Instance.SelectTrack(selectedTrack);

                if (BPMInput != null)
                {
                    BPMInput.inputText.text = selectedTrack.bpm.ToString();
                }
            }
        }

        private void Update()
        {
            if (!IsInitialized)
            {
                return;
            }

            if (
                AudioManager.Instance.currentAudioSource == null
                || AudioManager.Instance.currentAudioSource.clip == null
            )
            {
                currentTrackPlaybackTime.text = "00:00.00 | 00:00.00";
                return;
            }

            float currentTime = AudioManager.Instance.currentPlaybackTime;
            float totalTime = AudioManager.Instance.currentPlaybackDuration;

            string currentTimeFormatted = FormatTime(currentTime);
            string totalTimeFormatted = FormatTime(totalTime);

            currentTrackPlaybackTime.text = $"{currentTimeFormatted} | {totalTimeFormatted}";

            UpdatePlayButtonState();
        }

        private string FormatTime(float timeInSeconds)
        {
            int minutes = Mathf.FloorToInt(timeInSeconds / 60);
            float seconds = timeInSeconds % 60;

            return string.Format("{0:00}:{1:00.00}", minutes, seconds);
        }

        private void UpdatePlayButtonState()
        {
            if (AudioManager.Instance.currentAudioSource == null)
            {
                isPlaying = false;
                return;
            }

            bool audioIsPlaying = AudioManager.Instance.currentAudioSource.isPlaying;

            if (isPlaying != audioIsPlaying)
            {
                isPlaying = audioIsPlaying;
            }
        }

        public void OnPlayButtonClicked()
        {
            if (isPlaying)
            {
                AudioManager.Instance.Pause();
                isPlaying = false;
            }
            else
            {
                AudioManager.Instance.PlayCurrentTrack();
                isPlaying = true;
            }
        }

        public void OnLoadTrackButtonClicked()
        {
            if (isLoadingTrack || audioDataManager == null)
                return;

            ExtensionFilter[] extensions =
            {
                new ExtensionFilter("오디오 파일", "mp3", "wav", "ogg"),
            };

            StandaloneFileBrowser.OpenFilePanelAsync(
                "오디오 파일 선택",
                "",
                extensions,
                false,
                (string[] paths) =>
                {
                    if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
                    {
                        isLoadingTrack = true;
                        audioDataManager.LoadAudioFile(paths[0]);
                    }
                }
            );
        }

        /// <summary>
        /// 트랙 로드 완료 후 호출되는 콜백
        /// </summary>
        private void OnTrackLoaded(TrackData newTrack)
        {
            isLoadingTrack = false;

            if (newTrack == null)
                return;

            trackDataList = AudioManager.Instance.GetAllTrackInfo();

            UpdateTrackDropdown();

            int newTrackIndex = trackDataList.FindIndex(t => t.trackName == newTrack.trackName);
            if (newTrackIndex >= 0)
            {
                trackDropdown.selectedItemIndex = newTrackIndex;
                OnTrackDropdownValueChanged(newTrackIndex);
            }
        }

        public void OnSetAlbumArtButtonClicked()
        {
            if (
                isLoadingAlbumArt
                || audioDataManager == null
                || trackDataList.Count == 0
                || trackDropdown.selectedItemIndex < 0
            )
            {
                Debug.LogWarning("선택된 트랙이 없습니다.");
                return;
            }

            ExtensionFilter[] extensions =
            {
                new ExtensionFilter("이미지 파일", "png", "jpg", "jpeg"),
            };

            StandaloneFileBrowser.OpenFilePanelAsync(
                "앨범 아트 선택",
                "",
                extensions,
                false,
                (string[] paths) =>
                {
                    if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
                    {
                        isLoadingAlbumArt = true;
                        audioDataManager.SetAlbumArt(paths[0], trackDropdown.selectedItemIndex);
                    }
                }
            );
        }

        /// <summary>
        /// 앨범 아트 로드 완료 후 호출되는 콜백
        /// </summary>
        private void OnAlbumArtLoaded(string trackName, Sprite albumArt)
        {
            isLoadingAlbumArt = false;

            if (string.IsNullOrEmpty(trackName) || albumArt == null)
                return;

            trackDataList = AudioManager.Instance.GetAllTrackInfo();

            UpdateTrackDropdown();

            int trackIndex = trackDataList.FindIndex(t => t.trackName == trackName);
            if (trackIndex >= 0)
            {
                trackDropdown.selectedItemIndex = trackIndex;
                OnTrackDropdownValueChanged(trackIndex);

                CurrentTrackInfo.SetBackground(albumArt);
            }
        }

        public async Task OnDeleteTrackButtonClicked()
        {
            if (trackDataList.Count == 0 || trackDropdown.selectedItemIndex < 0)
            {
                Debug.LogWarning("선택된 트랙이 없습니다.");
                return;
            }

            int selectedIndex = trackDropdown.selectedItemIndex;
            if (selectedIndex >= 0 && selectedIndex < trackDataList.Count)
            {
                TrackData selectedTrack = trackDataList[selectedIndex];
                string trackName = selectedTrack.trackName;

                await AudioDataManager.Instance.DeleteTrackAsync(trackName);

                trackDropdown.RemoveItem(trackName, true);

                trackDataList = AudioManager.Instance.GetAllTrackInfo();
                Debug.Log($"삭제 후 남은 트랙 수: {trackDataList.Count}");

                if (trackDataList.Count == 0)
                {
                    trackDropdown.items.Clear();
                    trackDropdown.CreateNewItem("No Track", dropdownItemIcon, false);
                    trackDropdown.Initialize();

                    CurrentTrackInfo.SetText("No Track");
                    CurrentTrackInfo.SetBackground(defaultAlbumArt);
                }
                else
                {
                    int newSelectedIndex;

                    if (selectedIndex < trackDataList.Count)
                    {
                        newSelectedIndex = selectedIndex;
                    }
                    else
                    {
                        newSelectedIndex = trackDataList.Count - 1;
                    }

                    trackDropdown.selectedItemIndex = newSelectedIndex;

                    OnTrackDropdownValueChanged(newSelectedIndex);
                }

                Debug.Log($"트랙 삭제됨: {trackName}");
            }
        }

        private void UpdateTrackDropdown()
        {
            trackDropdown.items.Clear();

            trackDataList = AudioManager.Instance.GetAllTrackInfo();

            if (trackDataList.Count == 0)
            {
                try
                {
                    trackDropdown.CreateNewItem("No Track", dropdownItemIcon, false);
                    trackDropdown.Initialize();
                    trackDropdown.selectedItemIndex = 0;

                    CurrentTrackInfo.SetText("No Track");
                    CurrentTrackInfo.SetBackground(defaultAlbumArt);

                    if (trackDropdown.isOn)
                    {
                        trackDropdown.Animate();
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"빈 드롭다운 초기화 오류: {e.Message}");
                }
                return;
            }

            foreach (TrackData track in trackDataList)
            {
                try
                {
                    trackDropdown.CreateNewItem(track.trackName, dropdownItemIcon, false);
                }
                catch (Exception e)
                {
                    Debug.LogError($"드롭다운 아이템 추가 오류: {e.Message}");
                }
            }

            try
            {
                trackDropdown.Initialize();

                if (trackDataList.Count > 0)
                {
                    trackDropdown.selectedItemIndex = 0;
                    OnTrackDropdownValueChanged(0);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"드롭다운 초기화 오류: {e.Message}");
            }
        }
    }
}
