using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 레일과 웨이브폼을 생성하고 관리하는 클래스
/// </summary>
public class RailGenerator : MonoBehaviour
{
    [Header("레일 설정")]
    [SerializeField]
    private int laneCount = 5;

    [SerializeField]
    private float railLength = 20f;

    [SerializeField]
    private float railWidth = 1f;

    [SerializeField]
    private float railSpacing = 0.1f;

    [Header("시각 효과")]
    [SerializeField]
    private Material railMaterial;

    [SerializeField]
    private Color railColor = new Color(0, 0.8f, 0.8f);

    [SerializeField]
    private Color lineColor = new Color(0, 1f, 0);

    [SerializeField]
    private int divisionCount = 10;

    [Header("웨이브폼 디스플레이")]
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

    /// <summary>
    /// 초기화 여부
    /// </summary>
    private bool isInitialized = false;
    public bool IsInitialized => isInitialized;

    private void Start()
    {
        if (!isInitialized)
        {
            Initialize();
        }
    }

    /// <summary>
    /// 기본 설정값으로 초기화합니다.
    /// </summary>
    public void Initialize()
    {
        CreateRail();
        CreateWaveformDisplay();
        isInitialized = true;
    }

    /// <summary>
    /// 레일 컨테이너와 레인, 디비전, 저지라인 등을 생성합니다.
    /// </summary>
    private void CreateRail()
    {
        // 기존 컨테이너가 있으면 제거
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

    /// <summary>
    /// 웨이브폼 디스플레이를 생성합니다.
    /// </summary>
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
            }
        }
    }

    /// <summary>
    /// 웨이브폼 캔버스의 위치를 업데이트합니다.
    /// </summary>
    private void UpdateWaveformCanvasPosition()
    {
        if (waveformCanvas != null && railContainer != null)
        {
            // 레일의 총 너비 계산
            float totalWidth = (laneCount * railWidth) + ((laneCount - 1) * railSpacing);

            // 레일 오른쪽 끝 위치 계산
            float rightEdgeX = totalWidth / 2f + 0.5f; // 약간의 여백 추가

            // 캔버스 위치 업데이트 - 레일 컨테이너 기준 로컬 좌표
            waveformCanvas.transform.localPosition = new Vector3(
                rightEdgeX - totalWidth / 2f, // 로컬 좌표계에서 조정
                waveformOffset,
                0
            );
        }
    }

    /// <summary>
    /// 레일의 길이를 설정합니다.
    /// </summary>
    /// <param name="length">새 레일 길이</param>
    public void SetRailLength(float length)
    {
        if (length <= 0)
            return;

        railLength = length;

        // 레일 재생성
        if (railContainer != null)
        {
            // 웨이브폼 캔버스 임시 저장
            Transform waveformCanvasTransform = null;
            if (waveformCanvas != null)
            {
                waveformCanvasTransform = waveformCanvas.transform;
                waveformCanvasTransform.parent = transform; // 임시로 부모 변경
            }

            Destroy(railContainer);

            CreateRail();

            // 웨이브폼 캔버스가 있었다면 다시 레일 컨테이너의 자식으로 설정
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

                // 웨이브폼 캔버스 위치 업데이트
                UpdateWaveformCanvasPosition();
            }
            else
            {
                // 웨이브폼 캔버스가 없었다면 새로 생성
                CreateWaveformDisplay();
            }
        }
        else
        {
            CreateRail();
            CreateWaveformDisplay();
        }
    }

    /// <summary>
    /// 비트 마커 표시 여부를 설정합니다.
    /// </summary>
    /// <param name="show">표시 여부</param>
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

    /// <summary>
    /// 웨이브폼 디스플레이 컴포넌트를 반환합니다.
    /// </summary>
    public WaveformDisplay GetWaveformDisplay()
    {
        return waveformDisplay;
    }

    /// <summary>
    /// 오디오 클립을 설정하고 웨이브폼을 업데이트합니다.
    /// </summary>
    /// <param name="clip">오디오 클립</param>
    public void SetAudioClip(AudioClip clip)
    {
        if (waveformDisplay != null && clip != null)
        {
            StartCoroutine(WaitForWaveformInitialization(clip));
        }
    }

    /// <summary>
    /// 웨이브폼 초기화를 기다리고 오디오 클립을 설정합니다.
    /// </summary>
    private IEnumerator WaitForWaveformInitialization(AudioClip clip)
    {
        // 웨이브폼 디스플레이가 초기화될 때까지 대기
        yield return new WaitUntil(() => waveformDisplay != null && waveformDisplay.IsInitialized);

        // 오디오 클립 설정 및 웨이브폼 업데이트
        waveformDisplay.UpdateWaveform(clip);
    }

    /// <summary>
    /// BPM 및 박자 설정을 업데이트합니다.
    /// </summary>
    /// <param name="bpm">BPM</param>
    /// <param name="beatsPerBar">마디당 박자 수</param>
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
}
