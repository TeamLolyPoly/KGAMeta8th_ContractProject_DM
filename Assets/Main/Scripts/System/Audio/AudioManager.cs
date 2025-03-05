using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

public class AudioManager : Singleton<AudioManager>, IInitializable
{
    public TrackData currentTrack;
    public AudioSource currentAudioSource;
    public List<TrackData> tracks = new List<TrackData>();
    public bool IsInitialized { get; private set; }
    private InputActionAsset audioControlActions;
    private int currentTrackIndex = 0;
    private InputAction playPauseAction;
    private bool isPlaying = false;

    private string tracksPath;

    // BPM 관련 이벤트 및 속성
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
            }
        }
    }

    public float currentPlaybackTime
    {
        get => currentAudioSource != null ? currentAudioSource.time : 0f;
        set
        {
            if (currentAudioSource != null)
                currentAudioSource.time = value;
        }
    }

    public float currentPlaybackDuration
    {
        get => currentAudioSource.clip != null ? currentAudioSource.clip.length : 0f;
    }

    public void Start()
    {
        Initialize();
    }

    public void Initialize()
    {
        currentAudioSource = gameObject.AddComponent<AudioSource>();

        tracksPath = Path.Combine(Application.persistentDataPath, "Tracks");
        if (!Directory.Exists(tracksPath))
        {
            Directory.CreateDirectory(tracksPath);
        }

        LoadTrackMetadata();
        LoadAllTracks();
        SetupInputActions();
        IsInitialized = true;
    }

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

    private void OnDisable()
    {
        if (audioControlActions != null)
        {
            var actionMap = audioControlActions.FindActionMap("AudioPlayer");
            if (actionMap != null)
            {
                actionMap.Disable();
            }
        }
    }

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

    public void SelectTrack(TrackData track)
    {
        if (track == null || track.trackAudio == null)
        {
            Debug.LogWarning("선택한 트랙이 없거나 오디오가 로드되지 않았습니다.");
            return;
        }

        currentTrack = track;
        currentAudioSource.clip = track.trackAudio;
        currentPlaybackTime = 0;

        CurrentBPM = track.bpm;

        OnTrackChanged?.Invoke(track);
    }

    public void LoadAllTracks()
    {
        if (!Directory.Exists(tracksPath))
            return;

        string[] files = Directory.GetFiles(tracksPath, "*.wav");
        foreach (string file in files)
        {
            string fileName = Path.GetFileNameWithoutExtension(file);

            if (tracks.Any(t => t.trackName == fileName))
            {
                Debug.Log($"트랙 '{fileName}'은 이미 메타데이터에서 로드되었습니다.");
                continue;
            }

            StartCoroutine(LoadTrackFromFile(file));
        }
    }

    public List<TrackData> GetAllTrackInfo()
    {
        return tracks;
    }

    public void PlayCurrentTrack()
    {
        if (currentTrack != null && currentAudioSource.clip != null)
        {
            currentAudioSource.Play();
            isPlaying = true;
        }
        else
        {
            Debug.LogWarning("재생할 트랙이 선택되지 않았습니다.");
        }
    }

    public void ChangePlaybackPosition(float time)
    {
        currentAudioSource.time = time;
    }

    public void Pause()
    {
        currentAudioSource.Pause();
        isPlaying = false;
    }

    public void Resume()
    {
        currentAudioSource.UnPause();
        isPlaying = true;
    }

    public void Stop()
    {
        currentAudioSource.Stop();
        isPlaying = false;
    }

    public void SetVolume(float volume)
    {
        currentAudioSource.volume = volume;
    }

    public void NextTrack()
    {
        if (tracks.Count > 0)
        {
            currentTrackIndex = (currentTrackIndex + 1) % tracks.Count;
            SelectTrack(tracks[currentTrackIndex]);

            PlayCurrentTrack();
        }
    }

    public void PreviousTrack()
    {
        if (tracks.Count > 0)
        {
            currentTrackIndex = (currentTrackIndex - 1 + tracks.Count) % tracks.Count;
            SelectTrack(tracks[currentTrackIndex]);

            PlayCurrentTrack();
        }
    }

    public void AdjustVolume(float delta)
    {
        float newVolume = Mathf.Clamp01(currentAudioSource.volume + delta);
        SetVolume(newVolume);
    }

    public void SaveTrack(AudioClip clip, string trackName)
    {
        if (clip == null)
            return;

        string filePath = Path.Combine(tracksPath, trackName + ".wav");

        WavHelper.Save(filePath, clip);

        Debug.Log($"트랙 저장됨: {filePath}");
    }

    private async Task<bool> LoadTrackFromFileAsync(string filePath)
    {
        try
        {
            var result = await ResourceIO.LoadAudioFileAsync(filePath);

            if (result.clip == null)
            {
                Debug.LogError($"트랙 로드 실패: {filePath}");
                return false;
            }

            Sprite albumArt = LoadAlbumArt(result.fileName);

            TrackData track = new TrackData
            {
                trackName = result.fileName,
                trackAudio = result.clip,
                albumArt = albumArt,
            };

            TrackData existingTrack = tracks.FirstOrDefault(t => t.trackName == track.trackName);
            if (existingTrack == null)
            {
                tracks.Add(track);
                Debug.Log($"트랙 로드됨: {result.fileName}");
            }

            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"트랙 로드 중 예외 발생: {ex.Message}");
            return false;
        }
    }

    private System.Collections.IEnumerator LoadTrackFromFile(string filePath)
    {
        // 비동기 메서드를 코루틴으로 래핑
        Task<bool> loadTask = LoadTrackFromFileAsync(filePath);

        // 비동기 작업이 완료될 때까지 대기
        while (!loadTask.IsCompleted)
        {
            yield return null;
        }

        if (loadTask.Exception != null)
        {
            Debug.LogError($"트랙 로드 중 예외 발생: {loadTask.Exception.Message}");
        }
    }

    private async Task<bool> LoadAudioForTrackAsync(TrackData track, string filePath)
    {
        try
        {
            var result = await ResourceIO.LoadAudioFileAsync(filePath);

            if (result.clip == null)
            {
                Debug.LogError($"트랙 오디오 로드 실패: {filePath}");
                return false;
            }

            track.trackAudio = result.clip;

            Debug.Log($"트랙 오디오 로드됨: {track.trackName}");

            SelectTrack(track);
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"트랙 오디오 로드 중 예외 발생: {ex.Message}");
            return false;
        }
    }

    private System.Collections.IEnumerator LoadAudioForTrack(TrackData track, string filePath)
    {
        // 비동기 메서드를 코루틴으로 래핑
        Task<bool> loadTask = LoadAudioForTrackAsync(track, filePath);

        // 비동기 작업이 완료될 때까지 대기
        while (!loadTask.IsCompleted)
        {
            yield return null;
        }

        if (loadTask.Exception != null)
        {
            Debug.LogError($"트랙 오디오 로드 중 예외 발생: {loadTask.Exception.Message}");
        }
    }

    public void AddTrack(TrackData track)
    {
        if (track != null && track.trackAudio != null)
        {
            TrackData existingTrack = tracks.FirstOrDefault(t => t.trackName == track.trackName);
            if (existingTrack != null)
            {
                int index = tracks.IndexOf(existingTrack);
                tracks[index] = track;
                Debug.Log($"트랙 업데이트: {track.trackName}");

                SaveTrack(track.trackAudio, track.trackName);

                if (track.albumArt != null)
                {
                    SaveAlbumArt(track.albumArt, track.trackName);
                }

                SaveTrackMetadata();
            }
            else
            {
                tracks.Add(track);
                Debug.Log($"새 트랙 추가: {track.trackName}");

                SaveTrack(track.trackAudio, track.trackName);

                if (track.albumArt != null)
                {
                    SaveAlbumArt(track.albumArt, track.trackName);
                }

                SaveTrackMetadata();
            }

            if (currentTrack == null)
            {
                SelectTrack(track);
            }
        }
        else
        {
            Debug.LogError("유효하지 않은 트랙 데이터입니다.");
        }
    }

    public void ClearAllTracks()
    {
        if (Directory.Exists(tracksPath))
        {
            try
            {
                // 오디오 파일 삭제
                string[] audioFiles = Directory.GetFiles(tracksPath, "*.wav");
                foreach (string file in audioFiles)
                {
                    File.Delete(file);
                }
                Debug.Log("모든 오디오 파일이 삭제되었습니다.");

                // 앨범 아트 폴더 삭제
                string albumArtPath = Path.Combine(tracksPath, "AlbumArts");
                if (Directory.Exists(albumArtPath))
                {
                    string[] artFiles = Directory.GetFiles(albumArtPath, "*.png");
                    foreach (string file in artFiles)
                    {
                        File.Delete(file);
                    }
                    Debug.Log("모든 앨범 아트 파일이 삭제되었습니다.");
                }

                // 메타데이터 파일 삭제
                string metadataPath = Path.Combine(tracksPath, "track_metadata.json");
                if (File.Exists(metadataPath))
                {
                    File.Delete(metadataPath);
                    Debug.Log("메타데이터 파일이 삭제되었습니다.");
                }

                // 트랙 리스트 초기화
                tracks.Clear();
                currentTrack = null;
                currentAudioSource.clip = null;
                currentTrackIndex = 0;

                Debug.Log("모든 트랙이 삭제되었습니다.");
            }
            catch (Exception e)
            {
                Debug.LogError($"트랙 삭제 중 오류 발생: {e.Message}");
            }
        }
    }

    public void DeleteTrack(string trackName)
    {
        TrackData trackToRemove = tracks.FirstOrDefault(t => t.trackName == trackName);
        if (trackToRemove != null)
        {
            // 리스트에서 트랙 제거
            tracks.Remove(trackToRemove);

            // 오디오 파일 삭제
            string filePath = Path.Combine(tracksPath, trackName + ".wav");
            if (File.Exists(filePath))
            {
                try
                {
                    File.Delete(filePath);
                    Debug.Log($"트랙 파일 삭제됨: {filePath}");
                }
                catch (Exception e)
                {
                    Debug.LogError($"트랙 파일 삭제 중 오류 발생: {e.Message}");
                }
            }

            // 앨범 아트 파일 삭제
            string albumArtPath = Path.Combine(tracksPath, "AlbumArts", trackName + ".png");
            if (File.Exists(albumArtPath))
            {
                try
                {
                    File.Delete(albumArtPath);
                    Debug.Log($"앨범 아트 파일 삭제됨: {albumArtPath}");
                }
                catch (Exception e)
                {
                    Debug.LogError($"앨범 아트 파일 삭제 중 오류 발생: {e.Message}");
                }
            }

            // 현재 트랙이 삭제된 경우 다른 트랙으로 전환
            if (currentTrack == trackToRemove)
            {
                if (tracks.Count > 0)
                {
                    SelectTrack(tracks[0]);
                }
                else
                {
                    currentTrack = null;
                    currentAudioSource.clip = null;
                }
            }

            // 메타데이터 파일 업데이트
            SaveTrackMetadata();

            Debug.Log($"트랙 삭제됨: {trackName}");
        }
        else
        {
            Debug.LogWarning($"삭제할 트랙을 찾을 수 없음: {trackName}");
        }
    }

    public void SaveAlbumArt(Sprite albumArt, string trackName)
    {
        if (albumArt == null)
            return;

        try
        {
            string albumArtPath = Path.Combine(tracksPath, "AlbumArts");
            if (!Directory.Exists(albumArtPath))
            {
                Directory.CreateDirectory(albumArtPath);
            }

            string filePath = Path.Combine(albumArtPath, trackName + ".png");

            Texture2D texture = albumArt.texture;
            byte[] bytes = texture.EncodeToPNG();
            File.WriteAllBytes(filePath, bytes);

            Debug.Log($"앨범 아트 저장됨: {filePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"앨범 아트 저장 중 오류 발생: {e.Message}");
        }
    }

    public Sprite LoadAlbumArt(string trackName)
    {
        string albumArtPath = Path.Combine(tracksPath, "AlbumArts", trackName + ".png");
        if (File.Exists(albumArtPath))
        {
            try
            {
                byte[] bytes = File.ReadAllBytes(albumArtPath);
                Texture2D texture = new Texture2D(2, 2);
                texture.LoadImage(bytes);

                Sprite sprite = Sprite.Create(
                    texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f)
                );
                return sprite;
            }
            catch (Exception e)
            {
                Debug.LogError($"앨범 아트 로드 중 오류 발생: {e.Message}");
            }
        }

        return null;
    }

    public void SaveTrackMetadata()
    {
        try
        {
            List<TrackMetadata> metadataList = new List<TrackMetadata>();

            foreach (var track in tracks)
            {
                TrackMetadata metadata = new TrackMetadata
                {
                    trackName = track.trackName,
                    filePath = track.filePath,
                    albumArtPath = track.albumArtPath,
                    bpm = track.bpm,
                };

                // 현재 트랙이면 현재 BPM 값 사용
                if (track == currentTrack)
                {
                    metadata.bpm = CurrentBPM;
                }

                metadataList.Add(metadata);
            }

            // 메타데이터 저장
            string json = JsonUtility.ToJson(new TrackMetadataList { tracks = metadataList }, true);
            string metadataPath = Path.Combine(tracksPath, "metadata.json");
            File.WriteAllText(metadataPath, json);

            Debug.Log("트랙 메타데이터가 저장되었습니다.");
        }
        catch (Exception e)
        {
            Debug.LogError($"트랙 메타데이터 저장 중 오류 발생: {e.Message}");
        }
    }

    private void LoadTrackMetadata()
    {
        string metadataPath = Path.Combine(tracksPath, "metadata.json");
        if (File.Exists(metadataPath))
        {
            try
            {
                string json = File.ReadAllText(metadataPath);
                TrackMetadataList metadataList = JsonUtility.FromJson<TrackMetadataList>(json);

                if (metadataList != null && metadataList.tracks != null)
                {
                    Debug.Log(
                        $"메타데이터에서 {metadataList.tracks.Count}개의 트랙 정보를 로드했습니다."
                    );

                    foreach (var metadata in metadataList.tracks)
                    {
                        TrackData track = new TrackData
                        {
                            trackName = metadata.trackName,
                            filePath = metadata.filePath,
                            albumArtPath = metadata.albumArtPath,
                            bpm = metadata.bpm,
                        };

                        // 앨범 아트 로드
                        if (!string.IsNullOrEmpty(metadata.albumArtPath))
                        {
                            track.albumArt = LoadAlbumArt(metadata.trackName);
                        }

                        // 중복 트랙 방지
                        if (!tracks.Any(t => t.trackName == track.trackName))
                        {
                            tracks.Add(track);
                            Debug.Log($"메타데이터에서 트랙 추가: {track.trackName}");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"트랙 메타데이터 로드 중 오류 발생: {e.Message}");
            }
        }
        else
        {
            Debug.Log("메타데이터 파일이 없습니다. 새로 생성됩니다.");
        }
    }

    // 트랙 변경 이벤트
    public event Action<TrackData> OnTrackChanged;

    /// <summary>
    /// 현재 트랙의 BPM을 설정합니다.
    /// </summary>
    /// <param name="bpm">설정할 BPM 값</param>
    public void SetBPM(float bpm)
    {
        if (bpm <= 0)
        {
            Debug.LogWarning("BPM은 0보다 커야 합니다.");
            return;
        }

        CurrentBPM = bpm;

        // 현재 트랙이 있으면 BPM 저장
        if (currentTrack != null)
        {
            currentTrack.bpm = bpm;

            // 메타데이터 저장
            SaveTrackMetadata();
        }
    }

    /// <summary>
    /// 현재 트랙의 BPM을 가져옵니다.
    /// </summary>
    /// <returns>현재 BPM 값</returns>
    public float GetBPM()
    {
        return CurrentBPM;
    }
}
