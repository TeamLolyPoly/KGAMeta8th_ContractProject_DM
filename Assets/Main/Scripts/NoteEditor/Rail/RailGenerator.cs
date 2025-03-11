using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NoteEditor
{
    public class RailGenerator : MonoBehaviour, IInitializable
    {
        [SerializeField]
        private int laneCount = 5;

        [SerializeField]
        private float unitsPerBar = 10f;

        [SerializeField]
        private float minRailLength = 20f;

        [SerializeField]
        private float railWidth = 1f;

        [SerializeField]
        private float railSpacing = 0.1f;

        [SerializeField]
        private Material railMaterial;

        [SerializeField]
        private Color railColor = new Color(0, 0.8f, 0.8f);

        [SerializeField]
        private Color lineColor = new Color(0, 1f, 0);

        [SerializeField]
        private Color barLineColor = new Color(1f, 0, 0);

        [SerializeField]
        private GameObject waveformDisplayPrefab;

        [SerializeField]
        private float waveformOffset = 0.2f;

        [SerializeField]
        private float waveformWidth = 5f;

        [SerializeField]
        private Color waveformColor = new Color(1f, 0.6f, 0.2f);

        private GameObject[] lanes;
        private GameObject[] divisions;
        private GameObject judgeLine;
        private GameObject railContainer;
        private WaveformDisplay waveformDisplay;
        private Canvas waveformCanvas;
        private bool isInitialized = false;
        public bool IsInitialized => isInitialized;

        private float bpm = 120f;
        private int beatsPerBar = 4;

        private float railLength = 20f;

        private AudioClip currentAudioClip;
        private float totalBars;

        public float TotalBars => totalBars;
        public float UnitsPerBar => unitsPerBar;
        public AudioClip CurrentAudioClip => currentAudioClip;

        private IEnumerator Start()
        {
            yield return new WaitUntil(
                () =>
                    AudioManager.Instance != null
                    && AudioManager.Instance.IsInitialized
                    && AudioDataManager.Instance != null
                    && AudioDataManager.Instance.IsInitialized
            );

            Initialize();
        }

        public void Initialize()
        {
            AudioManager.Instance.OnTrackChanged += OnTrackChanged;
            AudioManager.Instance.OnBPMChanged += OnBPMChanged;

            if (AudioManager.Instance.currentTrack != null)
            {
                OnTrackChanged(AudioManager.Instance.currentTrack);
            }
            else
            {
                if (isInitialized)
                {
                    CleanupObjects();
                }
                else
                {
                    try
                    {
                        CreateRail();
                        CreateWaveformDisplay();
                        isInitialized = true;
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Failed to initialize RailGenerator: {e}");
                        CleanupObjects();
                    }
                }
            }
            isInitialized = true;
        }

        private void OnDestroy()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.OnTrackChanged -= OnTrackChanged;
                AudioManager.Instance.OnBPMChanged -= OnBPMChanged;
            }
            CleanupObjects();
        }

        private void OnDisable()
        {
            CleanupObjects();
            StopAllCoroutines();
        }

        private void CleanupObjects()
        {
            if (this == null)
                return;

            StopAllCoroutines();

            if (lanes != null)
            {
                foreach (var lane in lanes)
                {
                    if (lane != null)
                    {
                        Destroy(lane);
                    }
                }
                lanes = null;
            }

            if (divisions != null)
            {
                foreach (var division in divisions)
                {
                    if (division != null)
                    {
                        Destroy(division);
                    }
                }
                divisions = null;
            }

            if (judgeLine != null)
            {
                Destroy(judgeLine);
                judgeLine = null;
            }

            if (waveformDisplay != null)
            {
                if (waveformDisplay.gameObject != null)
                {
                    Destroy(waveformDisplay.gameObject);
                }
                waveformDisplay = null;
            }

            if (waveformCanvas != null)
            {
                if (waveformCanvas.gameObject != null)
                {
                    Destroy(waveformCanvas.gameObject);
                }
                waveformCanvas = null;
            }

            if (railContainer != null)
            {
                Destroy(railContainer);
                railContainer = null;
            }

            isInitialized = false;
        }

        private void CreateRail()
        {
            if (railContainer != null)
            {
                Destroy(railContainer);
                railContainer = null;
            }

            try
            {
                railContainer = new GameObject("RailContainer");
                railContainer.transform.position = Vector3.zero;
                railContainer.transform.parent = this.transform;

                float totalWidth = (laneCount * railWidth) + ((laneCount - 1) * railSpacing);
                float startX = -totalWidth / 2f + (railWidth / 2f);

                lanes = new GameObject[laneCount];
                for (int i = 0; i < laneCount; i++)
                {
                    float xPos = startX + (i * (railWidth + railSpacing));

                    GameObject lane = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    lane.name = $"Lane_{i}";
                    lane.transform.parent = railContainer.transform;
                    lane.transform.localPosition = new Vector3(xPos, 0, railLength / 2f);
                    lane.transform.localScale = new Vector3(railWidth, 0.1f, railLength);

                    Renderer renderer = lane.GetComponent<Renderer>();
                    if (railMaterial != null)
                    {
                        renderer.material = new Material(railMaterial);
                    }
                    renderer.material.color = railColor;

                    lanes[i] = lane;
                }

                float secondsPerBeat = 60f / bpm;
                float secondsPerBar = secondsPerBeat * beatsPerBar;
                float totalSeconds = currentAudioClip != null ? currentAudioClip.length : 0f;

                float exactBars = totalSeconds / secondsPerBar;

                List<GameObject> divisionsList = new List<GameObject>();

                int totalBars = Mathf.CeilToInt(exactBars);
                for (int bar = 0; bar <= totalBars; bar++)
                {
                    float barStartPos = (bar / exactBars) * railLength;

                    if (barStartPos <= railLength)
                    {
                        GameObject barLine = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        barLine.name = $"BarLine_{bar}";
                        barLine.transform.parent = railContainer.transform;
                        barLine.transform.localPosition = new Vector3(0, 0.06f, barStartPos);
                        barLine.transform.localScale = new Vector3(totalWidth + 0.4f, 0.03f, 0.05f);

                        Renderer barRenderer = barLine.GetComponent<Renderer>();
                        barRenderer.material.color = barLineColor;
                        divisionsList.Add(barLine);

                        for (int beat = 1; beat < beatsPerBar; beat++)
                        {
                            float beatPos =
                                barStartPos + (beat * (railLength / (exactBars * beatsPerBar)));
                            if (beatPos <= railLength)
                            {
                                GameObject beatLine = GameObject.CreatePrimitive(
                                    PrimitiveType.Cube
                                );
                                beatLine.name = $"BeatLine_{bar}_{beat}";
                                beatLine.transform.parent = railContainer.transform;
                                beatLine.transform.localPosition = new Vector3(0, 0.06f, beatPos);
                                beatLine.transform.localScale = new Vector3(
                                    totalWidth + 0.2f,
                                    0.01f,
                                    0.05f
                                );

                                Renderer beatRenderer = beatLine.GetComponent<Renderer>();
                                beatRenderer.material.color = lineColor;
                                divisionsList.Add(beatLine);
                            }
                        }
                    }
                }

                divisions = divisionsList.ToArray();

                judgeLine = GameObject.CreatePrimitive(PrimitiveType.Cube);
                judgeLine.name = "JudgeLine";
                judgeLine.transform.parent = railContainer.transform;
                judgeLine.transform.localPosition = new Vector3(0, 0.07f, 0);
                judgeLine.transform.localScale = new Vector3(totalWidth + 0.5f, 0.05f, 0.2f);

                Renderer judgeRenderer = judgeLine.GetComponent<Renderer>();
                judgeRenderer.material.color = new Color(0, 0.8f, 1f);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to create rail: {e}");
                CleanupObjects();
                throw;
            }
        }

        private void CreateWaveformDisplay()
        {
            if (railContainer == null)
                return;

            if (waveformCanvas != null)
            {
                Destroy(waveformCanvas.gameObject);
                waveformCanvas = null;
            }

            try
            {
                float totalWidth = (laneCount * railWidth) + ((laneCount - 1) * railSpacing);
                float rightEdgeX = totalWidth / 2f - 1f;

                GameObject canvasObj = new GameObject("WaveformCanvas");
                canvasObj.transform.parent = railContainer.transform;
                canvasObj.transform.localPosition = new Vector3(
                    rightEdgeX - totalWidth,
                    waveformOffset,
                    0
                );
                canvasObj.transform.localRotation = Quaternion.Euler(90, 90, 0);

                waveformCanvas = canvasObj.AddComponent<Canvas>();
                waveformCanvas.renderMode = RenderMode.WorldSpace;
                waveformCanvas.worldCamera = Camera.main;

                RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
                canvasRect.sizeDelta = new Vector2(railLength, waveformWidth);
                canvasRect.pivot = new Vector2(1f, 0.5f);

                CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.dynamicPixelsPerUnit = 100f;
                scaler.referencePixelsPerUnit = 100f;

                canvasObj.AddComponent<GraphicRaycaster>();

                if (waveformDisplayPrefab != null)
                {
                    GameObject waveformObj = Instantiate(
                        waveformDisplayPrefab,
                        canvasObj.transform
                    );
                    waveformDisplay = waveformObj.GetComponent<WaveformDisplay>();

                    if (waveformDisplay != null)
                    {
                        RectTransform waveformRect = waveformObj.GetComponent<RectTransform>();
                        waveformRect.anchorMin = Vector2.zero;
                        waveformRect.anchorMax = Vector2.one;
                        waveformRect.offsetMin = Vector2.zero;
                        waveformRect.offsetMax = Vector2.zero;

                        waveformDisplay.waveformColor = waveformColor;
                        waveformDisplay.showBeatMarkers = true;
                        waveformDisplay.bpm = bpm;
                        waveformDisplay.beatsPerBar = beatsPerBar;
                        waveformDisplay.SetRailLength(railLength);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to create waveform display: {e}");
                if (waveformCanvas != null)
                {
                    Destroy(waveformCanvas.gameObject);
                    waveformCanvas = null;
                }
                throw;
            }
        }

        public void SetAudioClip(AudioClip clip)
        {
            if (clip == null)
                return;

            try
            {
                currentAudioClip = clip;

                float totalSeconds = clip.length;
                float secondsPerBar = (60f / bpm) * beatsPerBar;
                totalBars = totalSeconds / secondsPerBar;

                float calculatedLength = totalBars * unitsPerBar;
                float newLength = Mathf.Max(calculatedLength, minRailLength);

                if (!Mathf.Approximately(railLength, newLength))
                {
                    railLength = newLength;
                    CleanupObjects();
                    CreateRail();
                    CreateWaveformDisplay();
                }

                if (waveformDisplay != null && waveformDisplay.gameObject != null)
                {
                    StartCoroutine(WaitForWaveformInitialization(clip));
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to set audio clip: {e}");
                CleanupObjects();
            }
        }

        private IEnumerator WaitForWaveformInitialization(AudioClip clip)
        {
            if (clip == null)
                yield break;

            yield return new WaitUntil(
                () =>
                    waveformDisplay != null
                    && waveformDisplay.gameObject != null
                    && waveformDisplay.IsInitialized
            );

            if (waveformDisplay != null && waveformDisplay.gameObject != null)
            {
                waveformDisplay.UpdateWaveform(clip);
            }
        }

        public void UpdateBeatSettings(float bpm, int beatsPerBar)
        {
            Debug.Log(
                $"UpdateBeatSettings 시작 - 현재 BPM: {this.bpm}, 새 BPM: {bpm}, 현재 railLength: {railLength}"
            );

            this.bpm = bpm;
            this.beatsPerBar = beatsPerBar;

            if (currentAudioClip != null)
            {
                float totalSeconds = currentAudioClip.length;
                float secondsPerBar = (60f / bpm) * beatsPerBar;
                totalBars = totalSeconds / secondsPerBar;

                float calculatedLength = totalBars * unitsPerBar;
                float newLength = Mathf.Max(calculatedLength, minRailLength);

                Debug.Log(
                    $"UpdateBeatSettings 계산 - Audio Length: {totalSeconds}s, Bars: {totalBars}, Calculated Length: {calculatedLength}, New Length: {newLength}"
                );

                if (!Mathf.Approximately(railLength, newLength))
                {
                    railLength = newLength;
                    Debug.Log($"railLength 업데이트됨: {railLength}");

                    CleanupObjects();
                    CreateRail();
                    CreateWaveformDisplay();

                    if (waveformDisplay != null && currentAudioClip != null)
                    {
                        waveformDisplay.UpdateWaveform(currentAudioClip);
                    }
                }
            }
            else
            {
                Debug.LogWarning("UpdateBeatSettings called but currentAudioClip is null");
                railLength = minRailLength;
            }

            if (waveformDisplay != null)
            {
                waveformDisplay.bpm = bpm;
                waveformDisplay.beatsPerBar = beatsPerBar;
                waveformDisplay.SetRailLength(railLength);
            }

            Debug.Log(
                $"UpdateBeatSettings 완료: BPM = {bpm}, BeatsPerBar = {beatsPerBar}, RailLength = {railLength}"
            );
        }

        public void UpdateWaveform(AudioClip clip)
        {
            if (waveformDisplay != null)
            {
                waveformDisplay.UpdateWaveform(clip);
            }
        }

        private void OnTrackChanged(TrackData track)
        {
            if (track == null)
                return;

            bpm = track.bpm;
            beatsPerBar = 4;

            if (track.trackAudio != null)
            {
                SetAudioClip(track.trackAudio);
            }
        }

        private void OnBPMChanged(float newBpm)
        {
            if (Mathf.Approximately(bpm, newBpm))
                return;

            UpdateBeatSettings(newBpm, beatsPerBar);
        }
    }
}
