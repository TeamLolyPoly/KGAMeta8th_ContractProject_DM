using System.Collections.Generic;
using System.Linq;
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

    public float currentPlaybackTime
    {
        get => currentAudioSource.time;
        set => currentAudioSource.time = value;
    }

    public float currentPlaybackDuration
    {
        get => currentAudioSource.clip.length;
    }

    public void Start()
    {
        Initialize();
    }

    public void Initialize()
    {
        currentAudioSource = gameObject.AddComponent<AudioSource>();
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
        currentTrack = track;
        currentAudioSource.clip = track.trackAudio;

        int index = tracks.IndexOf(track);
        if (index != -1)
        {
            currentTrackIndex = index;
        }
    }

    public void LoadAllTracks()
    {
        List<AudioClip> audioClips = Resources.LoadAll<AudioClip>("Tracks").ToList();
        foreach (AudioClip clip in audioClips)
        {
            print(clip.name);
            TrackData track = new TrackData();
            track.trackName = clip.name;
            track.trackAudio = clip;
            track.albumArt = Resources.Load<Sprite>("Images/AlbumArts/" + clip.name);
            print(track.albumArt);
            tracks.Add(track);
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
}
