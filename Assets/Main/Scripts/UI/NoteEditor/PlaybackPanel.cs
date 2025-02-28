using System;
using System.Collections;
using System.Collections.Generic;
using Michsky.UI.Heat;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Dropdown = Michsky.UI.Heat.Dropdown;

public class PlaybackPanel : MonoBehaviour, IInitializable
{
    public Slider trackSlider;
    public BoxButtonManager CurrentTrackInfo;
    public ButtonManager playButton;
    public TextMeshProUGUI currentTrackPlaybackTime;
    public Dropdown trackDropdown;
    public WaveformDisplay waveformDisplay;
    public bool IsInitialized { get; private set; }

    private bool isPlaying = false;
    private List<TrackData> trackDataList = new List<TrackData>();

    public IEnumerator Start()
    {
        yield return new WaitUntil(() => AudioManager.Instance.IsInitialized);
        Initialize();
    }

    public void Initialize()
    {
        trackSlider.onValueChanged.AddListener(OnValueChanged);
        playButton.onClick.AddListener(OnPlayButtonClicked);
        InitializeTrackDropdown();
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
                trackDropdown.CreateNewItem(track.trackName, track.albumArt, false);
            }
            catch (Exception e)
            {
                Debug.LogError($"드롭다운 아이템 추가 오류: {e.Message}");
            }
        }

        try
        {
            trackDropdown.Initialize();
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
            CurrentTrackInfo.SetBackground(tracks[0].albumArt);

            if (waveformDisplay != null)
            {
                StartCoroutine(InitializeWaveform(tracks[0]));
            }
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
            CurrentTrackInfo.SetBackground(selectedTrack.albumArt);
            AudioManager.Instance.SelectTrack(selectedTrack);

            if (waveformDisplay != null && waveformDisplay.IsInitialized)
            {
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

        trackSlider.maxValue = AudioManager.Instance.currentPlaybackDuration;
        trackSlider.value = AudioManager.Instance.currentPlaybackTime;

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
        bool audioIsPlaying = AudioManager.Instance.currentAudioSource.isPlaying;

        if (isPlaying != audioIsPlaying)
        {
            isPlaying = audioIsPlaying;
        }
    }

    public void OnValueChanged(float value)
    {
        AudioManager.Instance.ChangePlaybackPosition(value);
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
}
