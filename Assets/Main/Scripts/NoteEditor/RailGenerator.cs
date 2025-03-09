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
        private float unitsPerBar = 10f; // 한 마디당 유닛 길이

        [SerializeField]
        private float minRailLength = 20f; // 최소 레일 길이

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
        private float totalBars; // 전체 마디 수

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

            // 현재 트랙이 있다면 초기 설정
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
                    catch (System.Exception e)
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
                        DestroyImmediate(lane);
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
                        DestroyImmediate(division);
                    }
                }
                divisions = null;
            }

            if (judgeLine != null)
            {
                DestroyImmediate(judgeLine);
                judgeLine = null;
            }

            if (waveformDisplay != null)
            {
                if (waveformDisplay.gameObject != null)
                {
                    DestroyImmediate(waveformDisplay.gameObject);
                }
                waveformDisplay = null;
            }

            if (waveformCanvas != null)
            {
                if (waveformCanvas.gameObject != null)
                {
                    DestroyImmediate(waveformCanvas.gameObject);
                }
                waveformCanvas = null;
            }

            if (railContainer != null)
            {
                DestroyImmediate(railContainer);
                railContainer = null;
            }

            isInitialized = false;
        }

        private void CreateRail()
        {
            if (railContainer != null)
            {
                DestroyImmediate(railContainer);
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

                // BPM 기반 마디와 비트 라인 생성
                float secondsPerBeat = 60f / bpm;
                float secondsPerBar = secondsPerBeat * beatsPerBar;
                float totalSeconds = currentAudioClip != null ? currentAudioClip.length : 0f;

                // 정확한 마디 수 계산
                float exactBars = totalSeconds / secondsPerBar;

                List<GameObject> divisionsList = new List<GameObject>();

                // 마디와 비트 라인 생성
                int totalBars = Mathf.CeilToInt(exactBars);
                for (int bar = 0; bar <= totalBars; bar++)
                {
                    float barStartPos = (bar / exactBars) * railLength;

                    if (barStartPos <= railLength)
                    {
                        // 마디 라인 생성
                        GameObject barLine = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        barLine.name = $"BarLine_{bar}";
                        barLine.transform.parent = railContainer.transform;
                        barLine.transform.localPosition = new Vector3(0, 0.06f, barStartPos);
                        barLine.transform.localScale = new Vector3(totalWidth + 0.4f, 0.03f, 0.05f);

                        Renderer barRenderer = barLine.GetComponent<Renderer>();
                        barRenderer.material.color = barLineColor;
                        divisionsList.Add(barLine);

                        // 해당 마디 내의 비트 라인 생성
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
            catch (System.Exception e)
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
                DestroyImmediate(waveformCanvas.gameObject);
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
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to create waveform display: {e}");
                if (waveformCanvas != null)
                {
                    DestroyImmediate(waveformCanvas.gameObject);
                    waveformCanvas = null;
                }
                throw;
            }
        }

        private void UpdateWaveformCanvasPosition()
        {
            if (waveformCanvas != null && railContainer != null)
            {
                float totalWidth = (laneCount * railWidth) + ((laneCount - 1) * railSpacing);

                float rightEdgeX = totalWidth / 2f - 1f;

                waveformCanvas.transform.localPosition = new Vector3(
                    rightEdgeX - totalWidth,
                    waveformOffset,
                    0
                );
            }
        }

        public void SetRailLength(float length)
        {
            if (length <= 0)
                return;

            railLength = length;
            CleanupObjects();
            CreateRail();
            CreateWaveformDisplay();
        }

        public void SetShowBeatMarkers(bool show)
        {
            if (waveformDisplay != null)
            {
                waveformDisplay.showBeatMarkers = show;
                if (waveformDisplay.IsInitialized && show)
                {
                    waveformDisplay.GenerateBeatMarkers();
                }
            }
        }

        public WaveformDisplay GetWaveformDisplay()
        {
            return waveformDisplay;
        }

        public void SetAudioClip(AudioClip clip)
        {
            if (clip == null)
                return;

            try
            {
                currentAudioClip = clip;

                // 오디오 길이를 마디 단위로 계산
                float totalSeconds = clip.length;
                float secondsPerBar = (60f / bpm) * beatsPerBar;
                totalBars = totalSeconds / secondsPerBar;

                // 레일 길이 재계산
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
            catch (System.Exception e)
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
            this.bpm = bpm;
            this.beatsPerBar = beatsPerBar;

            if (currentAudioClip != null)
            {
                float totalSeconds = currentAudioClip.length;
                float secondsPerBar = (60f / bpm) * beatsPerBar;
                totalBars = totalSeconds / secondsPerBar;

                // 마디 수를 올림하지 않고 정확한 값 사용
                float calculatedLength = totalBars * unitsPerBar;
                float newLength = Mathf.Max(calculatedLength, minRailLength);

                railLength = newLength;
            }

            if (waveformDisplay != null)
            {
                waveformDisplay.bpm = bpm;
                waveformDisplay.beatsPerBar = beatsPerBar;
                waveformDisplay.SetRailLength(railLength);
            }

            CleanupObjects();
            CreateRail();
            CreateWaveformDisplay();

            if (waveformDisplay != null && currentAudioClip != null)
            {
                waveformDisplay.UpdateWaveform(currentAudioClip);
            }
        }

        public void UpdateWaveform(AudioClip clip)
        {
            if (waveformDisplay != null)
            {
                waveformDisplay.UpdateWaveform(clip);
            }
        }

        public void SetUnitsPerBar(float units)
        {
            if (units <= 0)
                return;

            unitsPerBar = units;

            // 현재 트랙이 있다면 레일 재생성
            if (AudioManager.Instance.currentTrack != null)
            {
                OnTrackChanged(AudioManager.Instance.currentTrack);
            }
        }

        private void OnTrackChanged(TrackData track)
        {
            if (track == null)
                return;

            bpm = track.bpm;
            beatsPerBar = 4; // 기본값으로 4/4 박자 사용

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

        // 현재 오디오 클립의 정보를 가져오는 메서드 추가
        public (float totalSeconds, float barsCount) GetCurrentAudioInfo()
        {
            if (currentAudioClip == null)
                return (0f, 0f);

            float totalSeconds = currentAudioClip.length;
            return (totalSeconds, totalBars);
        }
    }
}
