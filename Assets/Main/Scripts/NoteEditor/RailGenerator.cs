using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace NoteEditor
{
    public class RailGenerator : MonoBehaviour, IInitializable
    {
        [SerializeField]
        private int laneCount = 5;

        [SerializeField]
        private float railLength = 20f;

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
        private int divisionCount = 10;

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

        private void Start()
        {
            if (!isInitialized)
            {
                Initialize();
            }
        }

        public void Initialize()
        {
            CreateRail();
            CreateWaveformDisplay();
            isInitialized = true;
        }

        private void CreateRail()
        {
            if (railContainer != null)
            {
                Destroy(railContainer);
            }

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

            divisions = new GameObject[divisionCount];
            float divisionSpacing = railLength / divisionCount;

            for (int i = 0; i < divisionCount; i++)
            {
                float zPos = i * divisionSpacing;

                GameObject division = GameObject.CreatePrimitive(PrimitiveType.Cube);
                division.name = $"Division_{i}";
                division.transform.parent = railContainer.transform;
                division.transform.localPosition = new Vector3(0, 0.06f, zPos);
                division.transform.localScale = new Vector3(totalWidth + 0.2f, 0.01f, 0.05f);

                Renderer renderer = division.GetComponent<Renderer>();
                renderer.material.color = lineColor;

                divisions[i] = division;
            }

            judgeLine = GameObject.CreatePrimitive(PrimitiveType.Cube);
            judgeLine.name = "JudgeLine";
            judgeLine.transform.parent = railContainer.transform;
            judgeLine.transform.localPosition = new Vector3(0, 0.07f, 0);
            judgeLine.transform.localScale = new Vector3(totalWidth + 0.5f, 0.05f, 0.2f);

            Renderer judgeRenderer = judgeLine.GetComponent<Renderer>();
            judgeRenderer.material.color = new Color(0, 0.8f, 1f);
        }

        private void CreateWaveformDisplay()
        {
            if (railContainer == null)
                return;

            if (waveformCanvas != null)
            {
                Destroy(waveformCanvas.gameObject);
                waveformCanvas = null;
                waveformDisplay = null;
            }

            float totalWidth = (laneCount * railWidth) + ((laneCount - 1) * railSpacing);
            float rightEdgeX = totalWidth / 2f - 1f;

            GameObject canvasObj = new GameObject("WaveformCanvas");
            canvasObj.transform.parent = railContainer.transform;
            canvasObj.transform.localPosition = new Vector3(rightEdgeX - totalWidth, waveformOffset, 0);
            canvasObj.transform.localRotation = Quaternion.Euler(90, 90, 0);

            waveformCanvas = canvasObj.AddComponent<Canvas>();
            waveformCanvas.renderMode = RenderMode.WorldSpace;
            waveformCanvas.worldCamera = Camera.main;

            RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(railLength, waveformWidth);
            canvasRect.pivot = new Vector2(1f, 0.5f);

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 100f;

            canvasObj.AddComponent<GraphicRaycaster>();

            if (waveformDisplayPrefab != null)
            {
                GameObject waveformObj = Instantiate(waveformDisplayPrefab, canvasObj.transform);
                waveformDisplay = waveformObj.GetComponent<WaveformDisplay>();

                if (waveformDisplay != null)
                {
                    waveformDisplay.waveformColor = waveformColor;
                    waveformDisplay.showBeatMarkers = true;
                    waveformDisplay.bpm = AudioManager.Instance.GetBPM();
                }
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

            if (railContainer != null)
            {
                Transform waveformCanvasTransform = null;
                if (waveformCanvas != null)
                {
                    waveformCanvasTransform = waveformCanvas.transform;
                    waveformCanvasTransform.parent = transform;
                }

                Destroy(railContainer);

                CreateRail();

                if (waveformCanvasTransform != null)
                {
                    waveformCanvasTransform.parent = railContainer.transform;

                    RectTransform canvasRect = waveformCanvas.GetComponent<RectTransform>();
                    if (canvasRect != null)
                    {
                        canvasRect.sizeDelta = new Vector2(railLength, waveformWidth);

                        if (
                            waveformDisplay != null
                            && waveformDisplay.IsInitialized
                            && AudioManager.Instance != null
                            && AudioManager.Instance.currentTrack != null
                        )
                        {
                            waveformDisplay.UpdateWaveform(
                                AudioManager.Instance.currentTrack.trackAudio
                            );
                        }
                    }

                    UpdateWaveformCanvasPosition();
                }
                else
                {
                    CreateWaveformDisplay();
                }
            }
            else
            {
                CreateRail();
                CreateWaveformDisplay();
            }
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
            if (waveformDisplay != null && clip != null)
            {
                StartCoroutine(WaitForWaveformInitialization(clip));
            }
        }

        private IEnumerator WaitForWaveformInitialization(AudioClip clip)
        {
            yield return new WaitUntil(() => waveformDisplay != null && waveformDisplay.IsInitialized);

            waveformDisplay.UpdateWaveform(clip);
        }

        public void UpdateBeatSettings(float bpm, int beatsPerBar)
        {
            if (waveformDisplay != null)
            {
                waveformDisplay.bpm = bpm;
                waveformDisplay.beatsPerBar = beatsPerBar;
                if (waveformDisplay.IsInitialized)
                {
                    waveformDisplay.GenerateBeatMarkers();
                }
            }
        }

        public void UpdateWaveform(AudioClip clip)
        {
            if (waveformDisplay != null)
            {
                waveformDisplay.UpdateWaveform(clip);
            }
        }
    }
}
