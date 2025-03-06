using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NoteEditor
{
    public class WaveformDisplay : MonoBehaviour, IPointerClickHandler, IInitializable
    {
        public RawImage waveformImage;
        public Image progressImage;
        public RectTransform waveformRect;
        public RectTransform playheadMarker;
        public Color playheadColor = Color.red;

        public float referencePixelsPerUnit = 200f;

        public float pixelsPerUnit = 200f;

        public bool useProgressOverlay = false;

        public Color waveformColor = new Color(1f, 0.6f, 0.2f);
        public Color progressColor = new Color(0.2f, 0.6f, 1f);

        public bool showBeatMarkers = false;
        public Color beatMarkerColor = Color.white;
        public Color downBeatMarkerColor = Color.yellow;

        public float bpm = 120f;

        public int beatsPerBar = 4;

        public GameObject beatMarkerPrefab;
        public GameObject downBeatMarkerPrefab;

        private Texture2D waveformTexture;
        private AudioClip currentClip;
        private Vector2 waveformSize;
        private bool isInitialized = false;
        private float[] beatMarkers;
        private List<GameObject> beatMarkerObjects = new List<GameObject>();

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

            // 캔버스 스케일러 설정
            CanvasScaler scaler = GetComponentInParent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.referencePixelsPerUnit = referencePixelsPerUnit;
                scaler.scaleFactor = 1f;
            }

            if (progressImage != null)
            {
                progressImage.color = progressColor;
                progressImage.type = Image.Type.Filled;
                progressImage.fillMethod = Image.FillMethod.Horizontal;
                progressImage.fillOrigin = (int)Image.OriginHorizontal.Left;
                progressImage.fillAmount = 0;

                progressImage.rectTransform.anchorMin = Vector2.zero;
                progressImage.rectTransform.anchorMax = Vector2.one;
                progressImage.rectTransform.offsetMin = Vector2.zero;
                progressImage.rectTransform.offsetMax = Vector2.zero;
            }

            if (playheadMarker != null)
            {
                playheadMarker.anchorMin = new Vector2(0, 0);
                playheadMarker.anchorMax = new Vector2(0, 1);
                playheadMarker.pivot = new Vector2(0f, 0.5f);
                playheadMarker.anchoredPosition = Vector2.zero;
                playheadMarker.sizeDelta = new Vector2(1f, 0);

                Image markerImage = playheadMarker.GetComponent<Image>();
                if (markerImage != null)
                {
                    markerImage.color = playheadColor;
                }
            }

            waveformSize = new Vector2(waveformRect.rect.width, waveformRect.rect.height);

            waveformImage.rectTransform.anchorMin = Vector2.zero;
            waveformImage.rectTransform.anchorMax = Vector2.one;
            waveformImage.rectTransform.offsetMin = Vector2.zero;
            waveformImage.rectTransform.offsetMax = Vector2.zero;

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
        }

        public void UpdateWaveform(AudioClip clip)
        {
            if (clip == null)
                return;

            currentClip = clip;

            waveformTexture = WaveformDisplayExtensions.CreateDualColorWaveformTexture(
                clip,
                waveformSize,
                waveformColor,
                pixelsPerUnit
            );

            if (waveformTexture != null)
            {
                waveformImage.texture = waveformTexture;
                waveformImage.texture.filterMode = FilterMode.Bilinear;
                waveformImage.texture.anisoLevel = 16;

                if (progressImage != null)
                {
                    progressImage.sprite = Sprite.Create(
                        waveformTexture,
                        new Rect(0, 0, waveformTexture.width, waveformTexture.height),
                        new Vector2(0.5f, 0.5f),
                        pixelsPerUnit
                    );
                    progressImage.fillAmount = 0;
                }

                if (showBeatMarkers)
                {
                    GenerateBeatMarkers();
                }
            }

            waveformImage.gameObject.SetActive(true);
            progressImage.gameObject.SetActive(true);
        }

        private void UpdatePlayheadPosition()
        {
            if (currentClip == null || playheadMarker == null)
                return;

            float currentTime = GetCurrentPlaybackTime();
            float totalDuration = GetTotalDuration();

            float normalizedPosition = Mathf.Clamp01(currentTime / totalDuration);

            UpdatePlayheadToPosition(normalizedPosition);
        }

        private void UpdateProgressOverlay()
        {
            if (currentClip == null || progressImage == null || !useProgressOverlay)
                return;

            float currentTime = GetCurrentPlaybackTime();
            float totalDuration = GetTotalDuration();

            float progress = Mathf.Clamp01(currentTime / totalDuration);
            progressImage.fillAmount = progress;
        }

        private float GetCurrentPlaybackTime()
        {
            if (AudioManager.Instance == null)
                return 0f;

            return AudioManager.Instance.currentPlaybackTime;
        }

        private float GetTotalDuration()
        {
            if (AudioManager.Instance == null || currentClip == null)
                return 1f;

            return currentClip != null
                ? currentClip.length
                : AudioManager.Instance.currentPlaybackDuration;
        }

        public void SetProgressOverlayVisible(bool visible)
        {
            useProgressOverlay = visible;
            if (progressImage != null)
            {
                progressImage.gameObject.SetActive(visible);
            }
        }

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

            float newTime = normalizedPosition * GetTotalDuration();
            AudioManager.Instance.ChangePlaybackPosition(newTime);

            if (progressImage != null && useProgressOverlay)
            {
                progressImage.fillAmount = normalizedPosition;
            }

            if (playheadMarker != null)
            {
                UpdatePlayheadToPosition(normalizedPosition);
            }
        }

        private void UpdatePlayheadToPosition(float normalizedPosition)
        {
            if (playheadMarker == null)
                return;

            float xPosition = normalizedPosition * waveformRect.rect.width;
            playheadMarker.anchoredPosition = new Vector2(xPosition, 0);
        }

        public void SetBeatMarkers(float[] markers)
        {
            beatMarkers = markers;
            showBeatMarkers = markers != null && markers.Length > 0;

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

                bool isDownBeat = i % beatsPerBar == 0;
                GameObject markerPrefab = isDownBeat ? downBeatMarkerPrefab : beatMarkerPrefab;

                if (markerPrefab != null)
                {
                    GameObject marker = Instantiate(markerPrefab, waveformRect);
                    RectTransform markerRect = marker.GetComponent<RectTransform>();

                    if (markerRect != null)
                    {
                        markerRect.anchorMin = new Vector2(0, 0);
                        markerRect.anchorMax = new Vector2(0, 1);
                        markerRect.pivot = new Vector2(0.5f, 0.5f);

                        float xPosition = normalizedPosition * waveformRect.rect.width;
                        markerRect.anchoredPosition = new Vector2(xPosition, 0);

                        markerRect.sizeDelta = new Vector2(0.1f, 0);

                        Image markerImage = marker.GetComponent<Image>();
                        if (markerImage != null)
                        {
                            markerImage.color = isDownBeat ? downBeatMarkerColor : beatMarkerColor;
                        }

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

        public void ClearWaveform()
        {
            if (waveformImage != null)
            {
                waveformImage.texture = null;
            }
        }
    }
}
