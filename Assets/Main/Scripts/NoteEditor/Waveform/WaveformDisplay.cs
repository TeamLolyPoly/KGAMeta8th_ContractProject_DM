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
        public RectTransform waveformRect;
        public RectTransform playheadMarker;
        public Color playheadColor = Color.red;

        public float referencePixelsPerUnit = 200f;

        public float pixelsPerUnit = 200f;

        public float railLength = 200f;

        private Color waveformColor = new Color(1f, 0.6f, 0.2f);
        private Color beatMarkerColor = Color.white;
        private Color downBeatMarkerColor = Color.yellow;

        public float bpm = 120f;

        public GameObject beatMarkerPrefab;
        public GameObject downBeatMarkerPrefab;

        private Texture2D waveformTexture;
        private AudioClip currentClip;
        private Vector2 waveformSize;
        private bool isInitialized = false;
        private List<GameObject> beatMarkerObjects = new List<GameObject>();

        public bool IsInitialized
        {
            get => isInitialized;
            private set => isInitialized = value;
        }

        public void SetColors(Color beatMarkerColor, Color downBeatMarkerColor, Color waveformColor)
        {
            this.beatMarkerColor = beatMarkerColor;
            this.downBeatMarkerColor = downBeatMarkerColor;
            this.waveformColor = waveformColor;
        }

        public void Initialize()
        {
            if (waveformRect == null)
                waveformRect = GetComponent<RectTransform>();

            if (waveformImage == null)
                waveformImage = GetComponent<RawImage>();

            CanvasScaler scaler = GetComponentInParent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.referencePixelsPerUnit = referencePixelsPerUnit;
                scaler.scaleFactor = 1f;
            }

            if (playheadMarker != null)
            {
                playheadMarker.anchorMin = new Vector2(0.5f, 0);
                playheadMarker.anchorMax = new Vector2(0.5f, 1);
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

            bpm = AudioManager.Instance.CurrentBPM;

            if (
                AudioManager.Instance.currentTrack != null
                && AudioManager.Instance.currentTrack.TrackAudio != null
            )
            {
                UpdateWaveform(AudioManager.Instance.currentTrack.TrackAudio);
            }

            IsInitialized = true;
        }

        public void OnBPMChanged(float newBPM)
        {
            bpm = newBPM;
            if (IsInitialized && currentClip != null)
            {
                GenerateBeatMarkers();
            }
        }

        public void OnBeatsPerBarChanged()
        {
            if (IsInitialized && currentClip != null)
            {
                GenerateBeatMarkers();
            }
        }

        public void OnTotalBarsChanged(float newTotalBars)
        {
            if (IsInitialized && currentClip != null)
            {
                GenerateBeatMarkers();
            }
        }

        private void Update()
        {
            if (!IsInitialized)
                return;

            UpdatePlayheadPosition();
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

                GenerateBeatMarkers();
            }

            waveformImage.gameObject.SetActive(true);
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

            if (playheadMarker != null)
            {
                UpdatePlayheadToPosition(normalizedPosition);
            }
        }

        private void UpdatePlayheadToPosition(float normalizedPosition)
        {
            if (playheadMarker == null)
                return;

            float waveformWidth = waveformRect.rect.width;
            float xPosition = (normalizedPosition * waveformWidth) - (waveformWidth / 2f);
            playheadMarker.anchoredPosition = new Vector2(xPosition, 0);
        }

        public void SetRailLength(float length)
        {
            railLength = length;
            if (IsInitialized && currentClip != null)
            {
                GenerateBeatMarkers();
            }
        }

        public void GenerateBeatMarkers()
        {
            ClearBeatMarkers();

            if (currentClip == null || bpm <= 0)
                return;

            int beatsPerBar = AudioManager.Instance.BeatsPerBar;
            float totalBars = AudioManager.Instance.TotalBars;
            float waveformWidth = waveformRect.rect.width;

            Debug.Log(
                $"[WaveformDisplay] 비트 마커 생성: totalBars={totalBars}, beatsPerBar={beatsPerBar}, waveformWidth={waveformWidth}"
            );

            float barWidth = waveformWidth / totalBars;

            for (int bar = 0; bar < Mathf.RoundToInt(totalBars); bar++)
            {
                float barStartPos = (bar / totalBars) * waveformWidth;
                float adjustedBarPos = barStartPos - (waveformWidth / 2f);

                CreateBeatMarker(adjustedBarPos, true);

                for (int beat = 1; beat < beatsPerBar; beat++)
                {
                    float beatPos = barStartPos + (beat * (barWidth / beatsPerBar));
                    float adjustedBeatPos = beatPos - (waveformWidth / 2f);

                    if (beatPos <= waveformWidth)
                    {
                        CreateBeatMarker(adjustedBeatPos, false);
                    }
                }
            }

            Debug.Log($"[WaveformDisplay] 비트 마커 생성 완료: {beatMarkerObjects.Count}개");
        }

        private void CreateBeatMarker(float position, bool isDownBeat)
        {
            GameObject markerPrefab = isDownBeat ? downBeatMarkerPrefab : beatMarkerPrefab;
            if (markerPrefab != null)
            {
                GameObject marker = Instantiate(markerPrefab, waveformRect);
                RectTransform markerRect = marker.GetComponent<RectTransform>();

                if (markerRect != null)
                {
                    markerRect.anchorMin = new Vector2(0.5f, 0);
                    markerRect.anchorMax = new Vector2(0.5f, 1);
                    markerRect.pivot = new Vector2(0.5f, 0.5f);
                    markerRect.anchoredPosition = new Vector2(position, 0);
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

        public void OnTrackChanged(TrackData track)
        {
            if (track == null)
                return;

            Debug.Log($"WaveformDisplay: Track changed to {track.trackName}");

            bpm = track.bpm;

            if (track.TrackAudio != null)
            {
                UpdateWaveform(track.TrackAudio);
            }
        }
    }
}
