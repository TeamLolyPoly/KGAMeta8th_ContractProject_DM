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

        public float RailWidth => railWidth;
        public float RailSpacing => railSpacing;

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
        private float totalBars = 0f;
        public float TotalBars => totalBars;
        public float UnitsPerBar => unitsPerBar;
        public int BeatsPerBar => beatsPerBar;

        public void Initialize()
        {
            try
            {
                LoadResources();
                isInitialized = true;
                Debug.Log("[RailController] 초기화 완료 - 리소스 로드됨");
            }
            catch (Exception e)
            {
                Debug.LogError($"[RailController] 초기화 실패: {e.Message}");
                isInitialized = false;
            }
        }

        private void LoadResources()
        {
            railMaterial = Resources.Load<Material>("Materials/NoteEditor/Rail");
            barLineMaterial = Resources.Load<Material>("Materials/NoteEditor/BarLine");
            beatLineMaterial = Resources.Load<Material>("Materials/NoteEditor/BeatLine");
            waveformDisplayPrefab = Resources.Load<GameObject>(
                "Prefabs/NoteEditor/UI/WaveformDisplay"
            );
        }

        private void OnDisable()
        {
            Cleanup();
        }

        public void Cleanup()
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
        }

        public void CreateRail()
        {
            if (railContainer != null)
            {
                Destroy(railContainer);
                railContainer = null;
            }

            if (totalBars <= 0)
            {
                Debug.LogWarning(
                    "[RailController] totalBars가 유효하지 않아 레일을 생성할 수 없습니다."
                );
                return;
            }

            try
            {
                railContainer = new GameObject("RailContainer");
                railContainer.transform.position = Vector3.zero;
                railContainer.transform.parent = this.transform;

                float totalWidth = (laneCount * railWidth) + ((laneCount - 1) * railSpacing);
                float startX = -totalWidth / 2f + (railWidth / 2f);

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

                                for (int beat = 1; beat < beatsPerBar; beat++)
                                {
                                    float beatPos =
                                        barStartPos + (beat * (barLength / beatsPerBar));
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

                Debug.Log(
                    $"[RailController] 레일 생성 완료: 총 마디 수 = {totalBars}, 길이 = {railLength}"
                );
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to create rail: {e}");
                Cleanup();
                throw;
            }
        }

        public void CreateWaveformDisplay()
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

                Debug.Log("[RailController] 웨이브폼 디스플레이 생성 완료");
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

        public void SetupRail(float bpm, int beatsPerBar, AudioClip clip)
        {
            if (clip == null)
            {
                Debug.LogWarning("[RailController] 오디오 클립이 없어 레일을 설정할 수 없습니다.");
                return;
            }

            this.bpm = bpm;
            this.beatsPerBar = beatsPerBar;

            float totalSeconds = clip.length;
            float secondsPerBar = (60f / bpm) * beatsPerBar;
            totalBars = totalSeconds / secondsPerBar;

            float calculatedLength = totalBars * unitsPerBar;
            railLength = Mathf.Max(calculatedLength, minRailLength);

            Debug.Log(
                $"[RailController] 레일 설정: BPM = {bpm}, BeatsPerBar = {beatsPerBar}, TotalBars = {totalBars}, Length = {railLength}"
            );

            Cleanup();
            CreateRail();
            CreateWaveformDisplay();

            if (waveformDisplay != null)
            {
                StartCoroutine(WaitForWaveformInitialization(clip));
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

        public void UpdateWaveform(AudioClip clip)
        {
            if (waveformDisplay != null && clip != null)
            {
                waveformDisplay.UpdateWaveform(clip);
            }
        }

        public void UpdateBPM(float newBpm)
        {
            this.bpm = newBpm;

            if (waveformDisplay != null)
            {
                waveformDisplay.bpm = newBpm;
                waveformDisplay.OnBPMChanged(newBpm);
            }
        }

        public void UpdateBeatsPerBar(int newBeatsPerBar)
        {
            this.beatsPerBar = newBeatsPerBar;

            if (waveformDisplay != null)
            {
                waveformDisplay.beatsPerBar = newBeatsPerBar;
                waveformDisplay.OnBeatsPerBarChanged(newBeatsPerBar);
            }
        }
    }
}
