using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class WaveformDisplay : MonoBehaviour, IPointerClickHandler, IInitializable
{
    [Header("웨이브폼 설정")]
    public RawImage waveformImage;
    public RectTransform waveformRect;
    public RectTransform playheadMarker;
    public Color playheadColor = Color.red;
    public float waveformHeight = 100f;

    [Header("재생 진행 표시")]
    public Image progressOverlay; // 재생 진행 상태를 표시할 이미지
    public Color progressColor = new Color(0.2f, 0.6f, 1f, 0.5f); // 진행 색상 (파란색 반투명)

    [Header("웨이브폼 색상")]
    public Color backgroundColor = new Color(0.25f, 0.25f, 0.25f); // 배경 색상
    public Color unplayedColor = new Color(1f, 0.6f, 0.2f); // 재생 전 웨이브폼 색상 (주황색)
    public Color playedColor = new Color(0.2f, 0.6f, 1f); // 재생 후 웨이브폼 색상 (파란색)
    public bool useDualColorWaveform = true; // 이중 색상 웨이브폼 사용 여부

    [Header("마커 설정")]
    public bool showBeatMarkers = false;
    public Color beatMarkerColor = Color.white;
    public Color downBeatMarkerColor = Color.yellow;

    [Tooltip("BPM 값 (분당 비트 수)")]
    public float bpm = 120f;

    [Tooltip("박자 (예: 4/4 박자의 경우 4)")]
    public int beatsPerBar = 4;

    [Header("마커 프리팹")]
    public GameObject beatMarkerPrefab;
    public GameObject downBeatMarkerPrefab;

    private Texture2D waveformTexture;
    private AudioClip currentClip;
    private Vector2 waveformSize;
    private bool isInitialized = false;
    private float[] beatMarkers;
    private int downBeat = 4;
    private List<GameObject> beatMarkerObjects = new List<GameObject>();
    private float lastProgress = 0f;
    private bool isDualColorTextureCreated = false;

    public bool IsInitialized
    {
        get => isInitialized;
        private set => isInitialized = value;
    }

    private void Start()
    {
        StartCoroutine(InitializeRoutine());
    }

    private IEnumerator InitializeRoutine()
    {
        yield return new WaitUntil(() => AudioManager.Instance.IsInitialized);
        Initialize();
    }

    public void Initialize()
    {
        if (waveformRect == null)
            waveformRect = GetComponent<RectTransform>();

        if (waveformImage == null)
            waveformImage = GetComponent<RawImage>();

        if (progressOverlay != null)
        {
            progressOverlay.color = progressColor;
            progressOverlay.fillMethod = Image.FillMethod.Horizontal;
            progressOverlay.fillOrigin = (int)Image.OriginHorizontal.Left;
            progressOverlay.type = Image.Type.Filled;
            progressOverlay.fillAmount = 0f;
        }

        waveformSize = new Vector2(waveformRect.rect.width, waveformHeight);

        if (
            AudioManager.Instance.currentTrack != null
            && AudioManager.Instance.currentTrack.trackAudio != null
        )
        {
            UpdateWaveform(AudioManager.Instance.currentTrack.trackAudio);
        }

        IsInitialized = true;
    }

    private void Update()
    {
        if (!IsInitialized)
            return;

        if (
            AudioManager.Instance.currentTrack != null
            && AudioManager.Instance.currentTrack.trackAudio != null
            && currentClip != AudioManager.Instance.currentTrack.trackAudio
        )
        {
            UpdateWaveform(AudioManager.Instance.currentTrack.trackAudio);
        }

        UpdatePlayheadPosition();

        UpdateProgressOverlay();

        if (useDualColorWaveform && isDualColorTextureCreated)
        {
            UpdateDualColorWaveform();
        }
    }

    public void UpdateWaveform(AudioClip clip)
    {
        if (clip == null)
            return;

        currentClip = clip;

        if (useDualColorWaveform)
        {
            waveformTexture = WaveformDisplayExtensions.CreateDualColorWaveformTexture(
                clip,
                waveformSize,
                backgroundColor,
                unplayedColor,
                playedColor
            );
            isDualColorTextureCreated = true;
            lastProgress = 0f;
        }
        else
        {
            waveformTexture = Waveform.GetWaveformTexture(clip, waveformSize);
            isDualColorTextureCreated = false;
        }

        if (waveformTexture != null)
        {
            waveformImage.texture = waveformTexture;
            waveformImage.SetNativeSize();

            RectTransform imageRect = waveformImage.rectTransform;
            imageRect.sizeDelta = new Vector2(waveformRect.rect.width, waveformHeight);

            if (progressOverlay != null)
            {
                progressOverlay.fillAmount = 0f;
            }

            if (showBeatMarkers)
            {
                GenerateBeatMarkers();
            }
        }
    }

    private void UpdatePlayheadPosition()
    {
        if (currentClip == null || playheadMarker == null)
            return;

        float currentTime = AudioManager.Instance.currentPlaybackTime;
        float totalDuration = AudioManager.Instance.currentPlaybackDuration;

        float normalizedPosition = currentTime / totalDuration;
        float xPosition =
            normalizedPosition * waveformRect.rect.width - waveformRect.rect.width / 2;

        playheadMarker.anchoredPosition = new Vector2(xPosition, 0);
    }

    private void UpdateProgressOverlay()
    {
        if (currentClip == null || progressOverlay == null)
            return;

        float currentTime = AudioManager.Instance.currentPlaybackTime;
        float totalDuration = AudioManager.Instance.currentPlaybackDuration;

        float progress = currentTime / totalDuration;

        progressOverlay.fillAmount = progress;
    }

    private void UpdateDualColorWaveform()
    {
        if (currentClip == null || waveformTexture == null)
            return;

        float currentTime = AudioManager.Instance.currentPlaybackTime;
        float totalDuration = AudioManager.Instance.currentPlaybackDuration;

        float progress = currentTime / totalDuration;

        if (Mathf.Abs(progress - lastProgress) > 0.01f)
        {
            WaveformDisplayExtensions.UpdateWaveformProgress(
                waveformTexture,
                progress,
                waveformSize,
                backgroundColor,
                playedColor
            );

            lastProgress = progress;
        }
    }

    // 웨이브폼 클릭 처리
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!IsInitialized || currentClip == null)
            return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            waveformRect,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint
        );

        float normalizedPosition = Mathf.Clamp01(
            (localPoint.x + waveformRect.rect.width / 2) / waveformRect.rect.width
        );

        float newTime = normalizedPosition * AudioManager.Instance.currentPlaybackDuration;
        AudioManager.Instance.ChangePlaybackPosition(newTime);

        if (progressOverlay != null)
        {
            progressOverlay.fillAmount = normalizedPosition;
        }

        if (useDualColorWaveform && isDualColorTextureCreated)
        {
            WaveformDisplayExtensions.UpdateWaveformProgress(
                waveformTexture,
                normalizedPosition,
                waveformSize,
                backgroundColor,
                playedColor
            );

            lastProgress = normalizedPosition;
        }
    }

    public void SetBeatMarkers(float[] markers, int downBeatValue = 4)
    {
        beatMarkers = markers;
        downBeat = downBeatValue;
        showBeatMarkers = (markers != null && markers.Length > 0);

        if (showBeatMarkers && IsInitialized && currentClip != null)
        {
            GenerateBeatMarkers();
        }
    }

    public void GenerateBeatMarkers()
    {
        ClearBeatMarkers();

        if (currentClip == null || bpm <= 0)
            return;

        float clipDuration = currentClip.length;
        float secondsPerBeat = 60f / bpm;
        int totalBeats = Mathf.FloorToInt(clipDuration / secondsPerBeat);

        beatMarkers = new float[totalBeats];

        for (int i = 0; i < totalBeats; i++)
        {
            float beatTime = i * secondsPerBeat;
            float normalizedPosition = beatTime / clipDuration;
            beatMarkers[i] = normalizedPosition;

            bool isDownBeat = (i % beatsPerBar == 0);
            GameObject markerPrefab = isDownBeat ? downBeatMarkerPrefab : beatMarkerPrefab;

            if (markerPrefab != null)
            {
                GameObject marker = Instantiate(markerPrefab, waveformRect);
                RectTransform markerRect = marker.GetComponent<RectTransform>();

                if (markerRect != null)
                {
                    float xPosition =
                        normalizedPosition * waveformRect.rect.width - waveformRect.rect.width / 2;
                    markerRect.anchoredPosition = new Vector2(xPosition, 0);

                    markerRect.sizeDelta = new Vector2(markerRect.sizeDelta.x, waveformHeight);

                    beatMarkerObjects.Add(marker);
                }
            }
        }
    }

    private void ClearBeatMarkers()
    {
        foreach (GameObject marker in beatMarkerObjects)
        {
            if (marker != null)
            {
                Destroy(marker);
            }
        }

        beatMarkerObjects.Clear();
    }

    private void OnDestroy()
    {
        ClearBeatMarkers();
    }
}
