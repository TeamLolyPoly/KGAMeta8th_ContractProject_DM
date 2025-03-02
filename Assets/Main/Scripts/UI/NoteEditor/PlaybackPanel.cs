using System;
using System.Collections;
using System.Collections.Generic;
using Michsky.UI.Heat;
using TMPro;
using UnityEngine;
using Dropdown = Michsky.UI.Heat.Dropdown;
using ProgressBar = Michsky.UI.Heat.ProgressBar;
using SFB;
using System.Threading.Tasks;

public class PlaybackPanel : MonoBehaviour, IInitializable
{
    public BoxButtonManager CurrentTrackInfo;
    public ButtonManager playButton;
    public ButtonManager LoadTrackButton;
    public TextMeshProUGUI currentTrackPlaybackTime;
    public Dropdown trackDropdown;
    public WaveformDisplay waveformDisplay;
    public BoxButtonManager SetAlbumArtButton;
    public ButtonManager DeleteTrackButton;
    public ProgressBar loadingIndicator;
    public bool IsInitialized { get; private set; }
    private Sprite defaultAlbumArt;
    private Sprite dropdownItemIcon;
    public GameObject LoadingPanel;

    private bool isPlaying = false;
    private List<TrackData> trackDataList = new List<TrackData>();

    public IEnumerator Start()
    {
        yield return new WaitUntil(() => AudioManager.Instance.IsInitialized);
        Initialize();
    }

    public void Initialize()
    {
        defaultAlbumArt = Resources.Load<Sprite>("Textures/AlbumArt");
        print(defaultAlbumArt);
        dropdownItemIcon = Resources.Load<Sprite>("Textures/DefaultAudioIcon");
        playButton.onClick.AddListener(OnPlayButtonClicked);
        LoadTrackButton.onClick.AddListener(OnLoadTrackButtonClicked);
        if (SetAlbumArtButton != null)
        {
            SetAlbumArtButton.onClick.AddListener(OnSetAlbumArtButtonClicked);
        }
        if (DeleteTrackButton != null)
        {
            DeleteTrackButton.onClick.AddListener(OnDeleteTrackButtonClicked);
        }
        InitializeTrackDropdown();
        LoadingPanel.SetActive(false);
        IsInitialized = true;
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
                $"트랙 추가: {track.trackName}, 앨범아트: {(track.albumArt != null ? "있음" : "없음")}"
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
            if (tracks[0].albumArt != null)
            {
                CurrentTrackInfo.SetBackground(tracks[0].albumArt);
            }
            else
            {
                CurrentTrackInfo.SetBackground(defaultAlbumArt);
            }

            if (waveformDisplay != null)
            {
                StartCoroutine(InitializeWaveform(tracks[0]));
            }
        }
        else
        {
            CurrentTrackInfo.SetText("No Track");
            CurrentTrackInfo.SetBackground(defaultAlbumArt);
        }
    }

    private IEnumerator InitializeWaveform(TrackData track)
    {
        yield return new WaitUntil(() => waveformDisplay.IsInitialized);

        waveformDisplay.UpdateWaveform(track.trackAudio);
    }

    private void OnTrackDropdownValueChanged(int selectedIndex)
    {
        if (selectedIndex >= 0 && selectedIndex < trackDataList.Count)
        {
            TrackData selectedTrack = trackDataList[selectedIndex];
            CurrentTrackInfo.SetText(selectedTrack.trackName);
            if (selectedTrack.albumArt != null)
            {
                CurrentTrackInfo.SetBackground(selectedTrack.albumArt);
            }
            else
            {
                CurrentTrackInfo.SetBackground(defaultAlbumArt);
            }
            AudioManager.Instance.SelectTrack(selectedTrack);

            if (waveformDisplay != null && waveformDisplay.IsInitialized)
            {
                waveformDisplay.gameObject.SetActive(true);
                waveformDisplay.UpdateWaveform(selectedTrack.trackAudio);
            }
        }
    }

    private void Update()
    {
        if (!IsInitialized)
        {
            return;
        }

        if (AudioManager.Instance.currentAudioSource == null ||
            AudioManager.Instance.currentAudioSource.clip == null)
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
        ExtensionFilter[] extensions = {
            new ExtensionFilter("오디오 파일", "mp3", "wav", "ogg")
        };

        StandaloneFileBrowser.OpenFilePanelAsync("오디오 파일 선택", "", extensions, false, async (string[] paths) =>
        {
            if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
            {
                if (loadingIndicator != null)
                {
                    LoadingPanel.SetActive(true);
                    loadingIndicator.SetValue(0);
                }

                await LoadAudioFileAsync(paths[0]);

                if (loadingIndicator != null)
                {
                    loadingIndicator.SetValue(100);
                    await Task.Delay(300);
                    LoadingPanel.SetActive(false);
                }
            }
        });
    }

    private async Task LoadAudioFileAsync(string filePath)
    {
        try
        {
            LoadingPanel.SetActive(true);
            if (loadingIndicator != null) loadingIndicator.SetValue(10);

            var result = await ResourceIO.LoadAudioFileAsync(filePath);

            if (loadingIndicator != null) loadingIndicator.SetValue(50);

            if (result.clip == null)
            {
                Debug.LogError("오디오 파일 로드 실패");
                return;
            }

            TrackData newTrack = new TrackData
            {
                trackName = result.fileName,
                trackAudio = result.clip,
                albumArt = null
            };

            AudioManager.Instance.AddTrack(newTrack);

            if (loadingIndicator != null) loadingIndicator.SetValue(75);

            trackDataList = AudioManager.Instance.GetAllTrackInfo();

            if (waveformDisplay != null && waveformDisplay.IsInitialized)
            {
                waveformDisplay.gameObject.SetActive(true);
                waveformDisplay.UpdateWaveform(newTrack.trackAudio);
            }

            await AddTrackToDropdown(newTrack);

            if (loadingIndicator != null) loadingIndicator.SetValue(90);

            LoadingPanel.SetActive(false);
        }
        catch (Exception ex)
        {
            Debug.LogError($"오디오 로드 중 예외 발생: {ex.Message}");
        }
    }

    private async Task AddTrackToDropdown(TrackData newTrack)
    {
        int existingIndex = trackDropdown.items.FindIndex(item => item.itemName == newTrack.trackName);
        if (existingIndex >= 0)
        {
            trackDropdown.selectedItemIndex = existingIndex;
            OnTrackDropdownValueChanged(existingIndex);

            if (waveformDisplay != null && waveformDisplay.IsInitialized)
            {
                waveformDisplay.gameObject.SetActive(true);
                waveformDisplay.UpdateWaveform(newTrack.trackAudio);
            }

            Debug.Log($"이미 존재하는 트랙 선택: {newTrack.trackName}, 인덱스: {existingIndex}");
            return;
        }

        try
        {
            trackDropdown.CreateNewItem(newTrack.trackName, dropdownItemIcon, true);

            int newTrackIndex = trackDataList.FindIndex(t => t.trackName == newTrack.trackName);
            if (newTrackIndex >= 0)
            {
                await Task.Delay(50);

                trackDropdown.selectedItemIndex = newTrackIndex;
                OnTrackDropdownValueChanged(newTrackIndex);
                CurrentTrackInfo.SetBackground(defaultAlbumArt);

                if (waveformDisplay != null && waveformDisplay.IsInitialized)
                {
                    waveformDisplay.gameObject.SetActive(true);
                    waveformDisplay.UpdateWaveform(newTrack.trackAudio);
                }

                Debug.Log($"새 트랙 선택됨: {newTrack.trackName}, 인덱스: {newTrackIndex}");
            }
            else
            {
                Debug.LogWarning($"새 트랙을 찾을 수 없음: {newTrack.trackName}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"드롭다운 업데이트 오류: {e.Message}");

            UpdateTrackDropdown();
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

                if (waveformDisplay != null && waveformDisplay.IsInitialized)
                {
                    waveformDisplay.ClearWaveform();
                    waveformDisplay.gameObject.SetActive(false);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"빈 드롭다운 초기화 오류: {e.Message}");
            }
            return;
        }
        else
        {
            if (waveformDisplay != null && waveformDisplay.IsInitialized)
            {
                waveformDisplay.gameObject.SetActive(true);
            }
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

    public void OnSetAlbumArtButtonClicked()
    {
        if (trackDataList.Count == 0 || trackDropdown.selectedItemIndex < 0)
        {
            Debug.LogWarning("선택된 트랙이 없습니다.");
            return;
        }

        ExtensionFilter[] extensions = {
            new ExtensionFilter("이미지 파일", "png", "jpg", "jpeg")
        };

        StandaloneFileBrowser.OpenFilePanelAsync("앨범 아트 선택", "", extensions, false, async (string[] paths) =>
        {
            if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
            {
                if (loadingIndicator != null)
                {
                    LoadingPanel.SetActive(true);
                    loadingIndicator.SetValue(0);
                }

                await LoadAlbumArtAsync(paths[0]);

                if (loadingIndicator != null)
                {
                    loadingIndicator.SetValue(100);
                    await Task.Delay(300);
                    LoadingPanel.SetActive(false);
                }
            }
        });
    }

    private async Task LoadAlbumArtAsync(string filePath)
    {
        try
        {
            LoadingPanel.SetActive(true);
            if (loadingIndicator != null) loadingIndicator.SetValue(20);

            Sprite albumArt = await ResourceIO.LoadAlbumArtAsync(filePath);

            if (loadingIndicator != null) loadingIndicator.SetValue(60);

            if (albumArt == null)
            {
                Debug.LogError("앨범 아트 로드 실패");
                return;
            }

            int selectedIndex = trackDropdown.selectedItemIndex;
            if (selectedIndex >= 0 && selectedIndex < trackDataList.Count)
            {
                TrackData selectedTrack = trackDataList[selectedIndex];

                selectedTrack.albumArt = albumArt;

                CurrentTrackInfo.SetBackground(albumArt);

                if (loadingIndicator != null) loadingIndicator.SetValue(80);

                UpdateTrackDropdown();
                trackDropdown.selectedItemIndex = selectedIndex;

                AudioManager.Instance.AddTrack(selectedTrack);

                Debug.Log($"앨범 아트 설정됨: {selectedTrack.trackName}");
            }
            LoadingPanel.SetActive(false);
        }
        catch (Exception ex)
        {
            Debug.LogError($"앨범 아트 로드 중 예외 발생: {ex.Message}");
        }
    }

    public void OnDeleteTrackButtonClicked()
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

            AudioManager.Instance.DeleteTrack(trackName);

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
                if (waveformDisplay != null && waveformDisplay.IsInitialized)
                {
                    waveformDisplay.ClearWaveform();
                    waveformDisplay.gameObject.SetActive(false);
                }
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
}
