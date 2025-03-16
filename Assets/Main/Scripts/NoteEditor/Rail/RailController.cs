using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NoteEditor
{
    public class RailController : MonoBehaviour, IInitializable
    {
        private int laneCount = 5;
        private float unitsPerBar = 10f;
        private float minRailLength = 20f;
        private float railWidth = 1f;
        private float railSpacing = 0.1f;

        private Material railMaterial;
        private Material barLineMaterial;
        private Material beatLineMaterial;
        private GameObject waveformDisplayPrefab;
        private float waveformOffset = 0.2f;
        private float waveformWidth = 1.5f;
        private Color waveformColor = new Color(1f, 0.6f, 0.2f);
        private Color beatMarkerColor = Color.white;
        private Color downBeatMarkerColor = Color.yellow;

        private GameObject[] lanes;
        private GameObject[] divisions;
        private GameObject startLine;
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

        public void Initialize()
        {
            GetResources();
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
                    Cleanup();
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
                        Cleanup();
                    }
                }
            }
            isInitialized = true;
        }

        public void GetResources()
        {
            railMaterial = Resources.Load<Material>("Materials/NoteEditor/Rail");
            barLineMaterial = Resources.Load<Material>("Materials/NoteEditor/BarLine");
            beatLineMaterial = Resources.Load<Material>("Materials/NoteEditor/BeatLine");
            waveformDisplayPrefab = Resources.Load<GameObject>(
                "Prefabs/NoteEditor/WaveformDisplay"
            );
        }

        private void ClearAllListeners()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.OnTrackChanged -= OnTrackChanged;
                AudioManager.Instance.OnBPMChanged -= OnBPMChanged;
            }
        }

        private void OnDisable()
        {
            Cleanup();
        }

        private void Cleanup()
        {
            if (this == null)
                return;

            StopAllCoroutines();
            ClearAllListeners();

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

            if (startLine != null)
            {
                Destroy(startLine);
                startLine = null;
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

                float totalBars = AudioManager.Instance.TotalBars;
                float barLength = railLength / totalBars;

                lanes = new GameObject[laneCount];
                List<GameObject> divisionsList = new List<GameObject>();

                GameObject lanesContainer = new GameObject("LanesContainer");
                lanesContainer.transform.parent = railContainer.transform;
                lanesContainer.transform.localPosition = Vector3.zero;

                GameObject linesContainer = new GameObject("LinesContainer");
                linesContainer.transform.parent = railContainer.transform;
                linesContainer.transform.localPosition = Vector3.zero;

                for (int lane = 0; lane < laneCount; lane++)
                {
                    float xPos = startX + (lane * (railWidth + railSpacing));

                    GameObject laneContainer = new GameObject($"Lane_{lane}");
                    laneContainer.transform.parent = lanesContainer.transform;
                    laneContainer.transform.localPosition = Vector3.zero;

                    for (int bar = 0; bar <= Mathf.CeilToInt(totalBars); bar++)
                    {
                        float barStartPos = (bar / totalBars) * railLength;

                        if (barStartPos <= railLength)
                        {
                            GameObject barContainer = new GameObject($"Bar_{bar}");
                            barContainer.transform.parent = laneContainer.transform;
                            barContainer.transform.localPosition = Vector3.zero;

                            GameObject laneSegment = GameObject.CreatePrimitive(PrimitiveType.Cube);
                            laneSegment.name = "RailSegment";
                            laneSegment.transform.parent = barContainer.transform;

                            float segmentLength = barLength;
                            float segmentPos = barStartPos + (segmentLength / 2f);

                            laneSegment.transform.localPosition = new Vector3(xPos, 0, segmentPos);
                            laneSegment.transform.localScale = new Vector3(
                                railWidth,
                                0.1f,
                                segmentLength
                            );

                            Renderer renderer = laneSegment.GetComponent<Renderer>();
                            if (railMaterial != null)
                            {
                                renderer.material = railMaterial;
                            }

                            if (bar == 0)
                                lanes[lane] = laneSegment;

                            if (lane == 0)
                            {
                                GameObject barLine = GameObject.CreatePrimitive(PrimitiveType.Cube);
                                barLine.name = $"BarLine_{bar}";
                                barLine.transform.position = new Vector3(0, 0.06f, barStartPos);
                                barLine.transform.localScale = new Vector3(
                                    totalWidth + 0.4f,
                                    0.03f,
                                    0.05f
                                );

                                Renderer barRenderer = barLine.GetComponent<Renderer>();
                                barRenderer.material = barLineMaterial;
                                divisionsList.Add(barLine);

                                for (int beat = 1; beat < AudioManager.Instance.BeatsPerBar; beat++)
                                {
                                    float beatPos =
                                        barStartPos
                                        + (beat * (barLength / AudioManager.Instance.BeatsPerBar));
                                    GameObject beatLine = GameObject.CreatePrimitive(
                                        PrimitiveType.Cube
                                    );
                                    beatLine.name = $"BeatLine_{beat}";
                                    beatLine.transform.position = new Vector3(0, 0.06f, beatPos);
                                    beatLine.transform.localScale = new Vector3(
                                        totalWidth + 0.2f,
                                        0.01f,
                                        0.05f
                                    );

                                    Renderer beatRenderer = beatLine.GetComponent<Renderer>();
                                    beatRenderer.material = beatLineMaterial;
                                    divisionsList.Add(beatLine);

                                    beatLine.transform.parent = barLine.transform;
                                }

                                barLine.transform.parent = linesContainer.transform;
                            }
                        }
                    }
                }

                divisions = divisionsList.ToArray();

                startLine = GameObject.CreatePrimitive(PrimitiveType.Cube);
                startLine.name = "StartLine";
                startLine.transform.parent = linesContainer.transform;
                startLine.transform.localPosition = new Vector3(0, 0.07f, 0);
                startLine.transform.localScale = new Vector3(totalWidth + 0.5f, 0.05f, 0.2f);

                Renderer startRenderer = startLine.GetComponent<Renderer>();
                startRenderer.material.color = new Color(0, 0.8f, 1f);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to create rail: {e}");
                Cleanup();
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
                    railLength / 2f
                );
                canvasObj.transform.localRotation = Quaternion.Euler(90, 270, 0);

                waveformCanvas = canvasObj.AddComponent<Canvas>();
                waveformCanvas.renderMode = RenderMode.WorldSpace;
                waveformCanvas.worldCamera = Camera.main;

                RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
                canvasRect.sizeDelta = new Vector2(railLength, waveformWidth);

                canvasRect.pivot = new Vector2(0.5f, 0.5f);
                canvasRect.anchorMin = new Vector2(0.5f, 0.5f);
                canvasRect.anchorMax = new Vector2(0.5f, 0.5f);

                CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.dynamicPixelsPerUnit = 100f;
                scaler.referencePixelsPerUnit = 100f;

                canvasObj.AddComponent<GraphicRaycaster>();

                if (waveformDisplayPrefab != null)
                {
                    WaveformDisplay waveformObj = Instantiate(
                            waveformDisplayPrefab,
                            canvasObj.transform
                        )
                        .GetComponent<WaveformDisplay>();
                    waveformObj.name = "WaveformDisplay";
                    waveformDisplay = waveformObj;

                    if (waveformDisplay != null)
                    {
                        RectTransform waveformRect = waveformObj.GetComponent<RectTransform>();
                        waveformRect.anchorMin = Vector2.zero;
                        waveformRect.anchorMax = Vector2.one;
                        waveformRect.offsetMin = Vector2.zero;
                        waveformRect.offsetMax = Vector2.zero;

                        waveformDisplay.bpm = bpm;
                        waveformDisplay.beatsPerBar = beatsPerBar;
                        waveformDisplay.SetColors(
                            beatMarkerColor,
                            downBeatMarkerColor,
                            waveformColor
                        );
                        waveformDisplay.SetRailLength(railLength);

                        waveformDisplay.Initialize();
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
                    Cleanup();
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
                Cleanup();
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

                    Cleanup();
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

            if (track.TrackAudio != null)
            {
                SetAudioClip(track.TrackAudio);
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
