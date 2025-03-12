using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CombinedSpawnManager : MonoBehaviour
{
    [Header("그리드 노트 설정")]
    [SerializeField]
    private GameObject leftNotePrefab;

    [SerializeField]
    private GameObject rightNotePrefab;

    [SerializeField]
    private float gridNoteSpeed = 10f;

    [SerializeField]
    private float gridSpawnInterval = 2f;

    [Header("원형 노트 설정")]
    [SerializeField]
    private float sourceRadius = 5f;

    [SerializeField]
    private float targetRadius = 5f;

    [SerializeField]
    private GameObject primarySegmentPrefab;

    [SerializeField]
    private GameObject symmetricSegmentPrefab;

    [SerializeField]
    private float arcMoveSpeed = 5f;

    [SerializeField]
    private int segmentCount = 36;

    [SerializeField]
    private float segmentSpawnInterval = 0.1f;

    [SerializeField]
    private bool createSymmetric = true;

    [Header("원형 위치 설정")]
    [SerializeField]
    private float circleZOffset = 0f;

    [SerializeField]
    private float circleYOffset = 0f;

    [Header("이펙트 설정")]
    [SerializeField]
    private GameObject hitEffectPrefab;

    [Header("BPM 설정")]
    [SerializeField]
    private float bpm = 128f;

    [SerializeField]
    private AudioSource musicSource;

    [SerializeField]
    private AudioClip testMusic;

    [SerializeField]
    private AudioSource metronomeSource;

    [SerializeField]
    private AudioClip metronomeClip;

    [Header("타이밍 정보")]
    [SerializeField]
    private float targetHitTime = 7.5f;

    [SerializeField]
    private float secondsPerBeat;

    [SerializeField]
    private float secondsPerBar;

    [SerializeField]
    private int currentBar;

    [SerializeField]
    private int currentBeat;

    [Header("단노트 BPM 설정")]
    [SerializeField]
    private bool useGridBPM = false;

    [SerializeField]
    private int totalBars = 32;

    private double nextGridSpawnTime;
    private int currentGridBar = 0;
    private int currentGridBeat = 0;

    private double startDspTime;
    private bool isPlaying = false;
    private const int BEATS_PER_BAR = 4;

    private GridManager gridManager;
    private Vector3 sourceCenter;
    private Vector3 targetCenter;
    private List<Vector3> sourcePoints = new List<Vector3>();
    private List<Vector3> targetPoints = new List<Vector3>();
    private float gridTimer;

    #region 초기화 및 기본 동작
    /// <summary>
    /// 컴포넌트 초기화 및 기본 설정을 수행합니다.
    /// </summary>
    private void Start()
    {
        gridManager = GridManager.Instance;

        if (gridManager == null)
        {
            Debug.LogError("GridManager를 찾을 수 없습니다!");
            return;
        }

        // 그리드의 오른쪽 3x3 영역 중앙을 기준으로 원형 위치 계산
        int rightGridCenterX = gridManager.TotalHorizontalCells - gridManager.HandGridSize / 2 - 1;
        int gridCenterY = gridManager.VerticalCells / 2;

        // 소스 그리드의 오른쪽 영역 중앙 위치 계산
        Vector3 sourceGridCenter = gridManager.GetCellPosition(
            gridManager.SourceGrid,
            rightGridCenterX,
            gridCenterY
        );

        // 타겟 그리드의 오른쪽 영역 중앙 위치 계산
        Vector3 targetGridCenter = gridManager.GetCellPosition(
            gridManager.TargetGrid,
            rightGridCenterX,
            gridCenterY
        );

        // 원형 중심점 설정
        sourceCenter = new Vector3(
            sourceGridCenter.x,
            sourceGridCenter.y + circleYOffset,
            sourceGridCenter.z + circleZOffset
        );

        targetCenter = new Vector3(
            targetGridCenter.x,
            targetGridCenter.y + circleYOffset,
            targetGridCenter.z + circleZOffset
        );

        // 원형 포인트 생성
        GenerateCirclePoints();

        // 프리팹 확인
        if (primarySegmentPrefab == null)
        {
            Debug.LogWarning("기본 세그먼트 프리팹이 설정되지 않았습니다. 기본 큐브를 사용합니다.");
            primarySegmentPrefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            primarySegmentPrefab.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
            DestroyImmediate(primarySegmentPrefab.GetComponent<Collider>());
        }

        if (symmetricSegmentPrefab == null)
        {
            Debug.LogWarning("대칭 세그먼트 프리팹이 설정되지 않았습니다. 기본 구체를 사용합니다.");
            symmetricSegmentPrefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            symmetricSegmentPrefab.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
            DestroyImmediate(symmetricSegmentPrefab.GetComponent<Collider>());
        }
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
        }

        if (testMusic != null)
        {
            musicSource.clip = testMusic;
        }

        CalculateBPMParameters();
    }

    /// <summary>
    /// 매 프레임마다 실행되며 노트 생성과 BPM 관련 업데이트를 처리합니다.
    /// </summary>
    private void Update()
    {
        if (isPlaying)
        {
            double currentDspTime = AudioSettings.dspTime;
            float currentTime = (float)(currentDspTime - startDspTime);
            UpdateBarAndBeat(currentTime);

            // BPM 기반 단노트 생성 (DSP 시간 사용)
            if (useGridBPM && currentGridBar < totalBars)
            {
                if (currentDspTime >= nextGridSpawnTime)
                {
                    SpawnGridNote();

                    // 다음 비트 시간 계산 (DSP 시간 기준)
                    nextGridSpawnTime += secondsPerBeat;

                    // 비트/마디 업데이트
                    currentGridBeat++;
                    if (currentGridBeat >= BEATS_PER_BAR)
                    {
                        currentGridBeat = 0;
                        currentGridBar++;
                    }

                    Debug.Log(
                        $"단노트 생성 - 마디: {currentGridBar + 1}, 비트: {currentGridBeat + 1}, DSP Time: {currentDspTime:F3}"
                    );
                }
            }
            else
            {
                // 기존 타이머 기반 생성
                gridTimer += Time.deltaTime;
                if (gridTimer >= gridSpawnInterval)
                {
                    SpawnGridNote();
                    gridTimer = 0f;
                }
            }
        }

        // 스페이스바 입력 처리 (기존 코드 + BPM 테스트 통합)
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            if (!isPlaying)
            {
                StartBPMTest(); // BPM 테스트 시작 (음악 재생 포함)
            }
            else
            {
                StopBPMTest(); // BPM 테스트 중지
            }
            SpawnRandomArcLongNote(); // 기존 기능 유지
        }
    }
    #endregion

    #region BPM 및 타이밍 관리
    /// <summary>
    /// BPM 관련 매개변수를 계산하고 초기화합니다.
    /// </summary>
    private void CalculateBPMParameters()
    {
        secondsPerBeat = 60f / bpm;
        secondsPerBar = secondsPerBeat * BEATS_PER_BAR;

        Debug.Log($"=== BPM 설정 정보 ===");
        Debug.Log($"BPM: {bpm}");
        Debug.Log($"비트 길이: {secondsPerBeat:F3}초");
        Debug.Log($"마디 길이: {secondsPerBar:F3}초");
        Debug.Log($"목표 도달 시간: {targetHitTime:F3}초 (4마디 첫박)");
        Debug.Log($"노트 속도: {arcMoveSpeed:F2} units/sec");
    }

    /// <summary>
    /// 현재 진행 중인 마디와 비트를 업데이트합니다.
    /// </summary>
    private void UpdateBarAndBeat(float currentTime)
    {
        float totalBeats = currentTime / secondsPerBeat;
        currentBar = Mathf.FloorToInt(totalBeats / BEATS_PER_BAR);
        currentBeat = Mathf.FloorToInt(totalBeats % BEATS_PER_BAR);
    }

    /// <summary>
    /// BPM 테스트를 시작하고 음악을 재생합니다.
    /// </summary>
    public void StartBPMTest()
    {
        startDspTime = AudioSettings.dspTime;

        // 0.1초 지연으로 시작 (오디오 시스템 초기화 시간 확보)
        double scheduleTime = startDspTime + 0.1;

        // 메인 음악 재생
        musicSource.PlayScheduled(scheduleTime);

        // 첫 노트 생성 시간 설정
        nextGridSpawnTime = scheduleTime;
        currentGridBar = 0;
        currentGridBeat = 0;

        isPlaying = true;
        Debug.Log($"BPM 테스트 시작 - DSP Start Time: {startDspTime:F3}");
    }

    /// <summary>
    /// BPM 테스트를 중지하고 음악 재생을 멈춥니다.
    /// </summary>
    public void StopBPMTest()
    {
        musicSource.Stop();
        isPlaying = false;
        Debug.Log("BPM 테스트 종료");
    }
    #endregion

    #region 그리드 노트 생성 및 관리
    /// <summary>
    /// BPM에 맞춰 그리드 노트를 생성합니다.
    /// </summary>
    private void SpawnGridNote()
    {
        // 1. 기본 노트 데이터 생성
        NoteData noteData = new NoteData();
        bool isLeftHand = Random.value > 0.5f; // 50% 확률로 왼쪽/오른쪽 결정

         // 2. NoteGameManager를 통해 노트 타입 설정
        NoteGameManager.Instance.SetupNoteTypeData(noteData, isLeftHand);

        // 3. 노트 기본 속성 설정
        noteData.direction = (NoteDirection)Random.Range(1, 8);
        noteData.noteAxis = NoteAxis.PZ;
        noteData.noteSpeed = gridNoteSpeed;

        // 4. 노트 위치 설정        
        int x = isLeftHand ? Random.Range(0, 3) : Random.Range(2, 5);
        int y = Random.Range(0, gridManager.VerticalCells);
        
         // 5. 시작/목표 위치 계산
        Vector3 startPos = gridManager.GetCellPosition(gridManager.SourceGrid, x, y);
        Vector3 targetPos = gridManager.GetCellPosition(gridManager.TargetGrid, x, y);

        noteData.targetPosition = targetPos;

        // 6. 노트 생성 및 초기화
        GameObject prefab = isLeftHand ? leftNotePrefab : rightNotePrefab;
        GameObject note = Instantiate(prefab, startPos, Quaternion.identity);

         // 7. 노트 컴포넌트 초기화
        if (isLeftHand)
        {
            LeftNote leftNote = note.GetComponent<LeftNote>();
            if (leftNote != null)
            {
                leftNote.Initialize(noteData);
            }
        }
        else
        {
            RightNote rightNote = note.GetComponent<RightNote>();
            if (rightNote != null)
            {
                rightNote.Initialize(noteData);
            }
        }

        // 메트로놈 소리 재생 (노트 생성과 동시에)
        if (metronomeSource != null && metronomeClip != null)
        {
            metronomeSource.PlayOneShot(metronomeClip);
        }

        double spawnTime = AudioSettings.dspTime - startDspTime;
        Debug.Log(
            $"노트 생성 - 시간: {spawnTime:F3}, 마디: {currentGridBar + 1}, 비트: {currentGridBeat + 1}"
        );
    }

    /// <summary>
    /// 그리드 노트 패턴을 JSON 파일로 저장합니다.
    /// </summary>
    public void TestSaveGridNotePattern()
    {
        var NoteList = new NoteList();

        // 테스트용 왼쪽 그리드 노트
        var leftGridNote = new NoteData
        {
            isLeftGrid = true,
            gridpos = new Vector2(1, 1),
            baseType = NoteBaseType.Short,
            noteType = NoteHitType.Hand,
            direction = NoteDirection.North,
            noteAxis = NoteAxis.PZ,
            noteSpeed = gridNoteSpeed,
        };

        // 테스트용 오른쪽 그리드 노트
        var rightGridNote = new NoteData
        {
            isLeftGrid = false,
            gridpos = new Vector2(3, 2),
            baseType = NoteBaseType.Short,
            noteType = NoteHitType.Red,
            direction = NoteDirection.South,
            noteAxis = NoteAxis.PZ,
            noteSpeed = gridNoteSpeed,
        };

        NoteList.patterns.Add(leftGridNote);
        NoteList.patterns.Add(rightGridNote);

        string json = JsonUtility.ToJson(NoteList, true);
        string path = Application.dataPath + "/Resources/TestGridNotePatterns.json";
        System.IO.File.WriteAllText(path, json);

        Debug.Log($"Saved Grid Note Patterns: \n{json}");
    }

    /// <summary>
    /// JSON 파일에서 그리드 노트 패턴을 로드합니다.
    /// </summary>

    public void TestLoadGridNotePattern()
    {
        string path = Application.dataPath + "/Resources/TestGridNotePatterns.json";
        if (!System.IO.File.Exists(path))
        {
            Debug.LogError("Test pattern file not found!");
            return;
        }

        string json = System.IO.File.ReadAllText(path);
        var loadedPatterns = JsonUtility.FromJson<NoteList>(json);

        Debug.Log($"Loaded {loadedPatterns.patterns.Count} grid note patterns");

        foreach (var pattern in loadedPatterns.patterns)
        {
            Debug.Log(
                $"Grid Note Pattern:"
                    + $"\nGrid Position: {(pattern.isLeftGrid ? "Left" : "Right")} ({pattern.gridpos.x}, {pattern.gridpos.y})"
                    + $"\nBase Type: {pattern.baseType}"
                    + $"\nNote Type: {pattern.noteType}"
                    + $"\nDirection: {pattern.direction}"
                    + $"\nAxis: {pattern.noteAxis}"
                    + $"\nMove Speed: {pattern.noteSpeed}"
            );

            // 선택적: 로드된 패턴으로 실제 노트 생성
            SpawnGridNoteFromData(pattern);
        }
    }

    /// <summary>
    /// 저장된 데이터를 기반으로 그리드 노트를 생성합니다.
    /// </summary>

    private void SpawnGridNoteFromData(NoteData data)
    {
        Vector3 startPos = gridManager.GetCellPosition(
            gridManager.SourceGrid,
            (int)data.gridpos.x,
            (int)data.gridpos.y
        );
        Vector3 targetPos = gridManager.GetCellPosition(
            gridManager.TargetGrid,
            (int)data.gridpos.x,
            (int)data.gridpos.y
        );

        NoteData noteData = new NoteData
        {
            baseType = data.baseType,
            noteType = data.noteType,
            direction = data.direction,
            noteAxis = data.noteAxis,
            targetPosition = targetPos,
            noteSpeed = data.noteSpeed,
        };

        GameObject prefab = data.isLeftGrid ? leftNotePrefab : rightNotePrefab;
        GameObject note = Instantiate(prefab, startPos, Quaternion.identity);

        if (data.isLeftGrid)
        {
            if (note.TryGetComponent<LeftNote>(out var leftNote))
            {
                leftNote.Initialize(noteData);
            }
        }
        else
        {
            if (note.TryGetComponent<RightNote>(out var rightNote))
            {
                rightNote.Initialize(noteData);
            }
        }
    }
    #endregion

    #region 원형 롱노트 생성 및 관리
    /// <summary>
    /// 원형 경로의 포인트들을 생성합니다.
    /// </summary>
    private void GenerateCirclePoints()
    {
        sourcePoints.Clear();
        targetPoints.Clear();

        float angleStep = 360f / segmentCount;

        // 소스 원형 포인트 생성 (XY 평면에 세로로 배치)
        for (int i = 0; i < segmentCount; i++)
        {
            float angle = i * angleStep;
            float radians = angle * Mathf.Deg2Rad;

            // XY 평면에서 원을 생성 (세로 원)
            float x = sourceCenter.x + sourceRadius * Mathf.Cos(radians);
            float y = sourceCenter.y + sourceRadius * Mathf.Sin(radians);
            float z = sourceCenter.z;

            sourcePoints.Add(new Vector3(x, y, z));
        }

        // 타겟 원형 포인트 생성 (XY 평면에 세로로 배치)
        for (int i = 0; i < segmentCount; i++)
        {
            float angle = i * angleStep;
            float radians = angle * Mathf.Deg2Rad;

            // XY 평면에서 원을 생성 (세로 원)
            float x = targetCenter.x + targetRadius * Mathf.Cos(radians);
            float y = targetCenter.y + targetRadius * Mathf.Sin(radians);
            float z = targetCenter.z;

            targetPoints.Add(new Vector3(x, y, z));
        }

        Debug.Log(
            $"원형 경로 생성 완료: 소스 포인트 {sourcePoints.Count}개, 타겟 포인트 {targetPoints.Count}개"
        );
    }

    /// <summary>
    /// 랜덤한 위치에 원형 롱노트를 생성합니다.
    /// </summary>
    private void SpawnRandomArcLongNote()
    {
        // 랜덤 시작 인덱스와 호의 길이 선택
        int startIdx = Random.Range(0, segmentCount);
        int arcLength = Random.Range(5, 15); // 호의 길이 (세그먼트 수)

        SpawnArcLongNote(startIdx, arcLength);
    }

    /// <summary>
    /// 지정된 위치에 원형 롱노트를 생성합니다.
    /// </summary>
    /// <param name="startIndex">시작 인덱스</param>
    /// <param name="arcLength">호의 길이</param>

    public void SpawnArcLongNote(int startIndex, int arcLength)
    {
        if (startIndex < 0 || startIndex >= segmentCount)
        {
            Debug.LogError($"잘못된 시작 인덱스: {startIndex}");
            return;
        }

        // 호의 끝 인덱스 계산 (원형 배열이므로 모듈로 연산)
        int endIndex = (startIndex + arcLength) % segmentCount;

        // 첫 번째 호 생성 (기본 프리팹 사용)
        StartCoroutine(SpawnArcSegments(startIndex, endIndex, false));

        // 대칭 호 생성 (옵션이 활성화된 경우, 대칭 프리팹 사용)
        if (createSymmetric)
        {
            // 대칭 시작점과 끝점 계산 (원의 반대편)
            int symmetricStart = (startIndex + segmentCount / 2) % segmentCount;
            int symmetricEnd = (endIndex + segmentCount / 2) % segmentCount;

            StartCoroutine(SpawnArcSegments(symmetricStart, symmetricEnd, true));

            Debug.Log($"대칭 호 롱노트 생성: 시작 {symmetricStart}, 끝 {symmetricEnd}");
        }

        Debug.Log($"호 롱노트 생성: 시작 {startIndex}, 끝 {endIndex}, 길이 {arcLength}");
    }

    /// <summary>
    /// 원형 롱노트의 세그먼트들을 순차적으로 생성합니다.
    /// </summary>
    /// <param name="startIndex">시작 인덱스</param>
    /// <param name="endIndex">끝 인덱스</param>
    /// <param name="isSymmetric">대칭 여부</param>

    private IEnumerator SpawnArcSegments(int startIndex, int endIndex, bool isSymmetric)
    {
         // 1. 기본 설정
        // 시계 방향으로 이동할지 결정
        bool clockwise = true;
        int currentIndex = startIndex;
        int segmentsSpawned = 0;

        // 사용할 프리팹 선택
        GameObject prefabToUse = isSymmetric ? symmetricSegmentPrefab : primarySegmentPrefab;

        // 시작 시간 기록
        double spawnStartTime = AudioSettings.dspTime;

        // 2. 호의 모든 세그먼트를 순차적으로 생성
        while (true)
        {
            // 3. 현재 세그먼트의 위치 계산
            // 현재 인덱스의 소스 및 타겟 위치
            Vector3 sourcePos = sourcePoints[currentIndex];
            Vector3 targetPos = targetPoints[currentIndex];

              // 4. 노트 데이터 설정
            NoteData noteData = new NoteData()
            {
                baseType = NoteBaseType.Long, // 원형 노트는 항상 롱노트
                noteSpeed = arcMoveSpeed,
                startPosition = sourcePos,
                targetPosition = targetPos,
                direction = NoteDirection.North, // 또는 상황에 맞는 방향
            };

            // 5. 노트 타입 설정
            NoteGameManager.Instance.SetupNoteTypeData(noteData, false); // false = 오른쪽

            // 6. 세그먼트 생성 및 초기화
            GameObject segment = Instantiate(prefabToUse, sourcePos, Quaternion.identity);
            // 세그먼트 이동 컴포넌트 추가
            ArcSegmentMover mover = segment.AddComponent<ArcSegmentMover>();
            mover.Initialize(noteData);

            // 7. 노트 컴포넌트 초기화
            if (segment.TryGetComponent<Note>(out Note note))
            {
                note.Initialize(noteData);
            }

            //충돌 이펙트 설정
            if (hitEffectPrefab != null)
            {
                mover.SetHitEffect(hitEffectPrefab);
            }

            segmentsSpawned++;

            // 끝 인덱스에 도달했는지 확인
            if (currentIndex == endIndex)
            {
                break;
            }

            // 다음 인덱스로 이동 (시계 또는 반시계 방향)
            if (clockwise)
            {
                currentIndex = (currentIndex + 1) % segmentCount;
            }
            else
            {
                currentIndex = (currentIndex - 1 + segmentCount) % segmentCount;
            }

            // BPM에 맞춰 생성 간격 조절
            yield return new WaitForSeconds(segmentSpawnInterval);
        }

        string symmetricText = isSymmetric ? "대칭 " : "";
        Debug.Log($"{symmetricText}호 롱노트 생성 완료: {segmentsSpawned}개 세그먼트");
    }

    /// <summary>
    /// 원형 롱노트 패턴을 JSON 파일로 저장합니다.
    /// </summary>

    public void TestSaveArcNotePattern()
    {
        var NoteList = new NoteList();

        // 테스트용 패턴 데이터 생성
        var testPattern = new NoteData
        {
            startIndex = 0,
            arcLength = 10,
            isSymmetric = true,
            isClockwise = true,
            sourceRadius = sourceRadius,
            targetRadius = targetRadius,
            noteSpeed = arcMoveSpeed,
            spawnInterval = segmentSpawnInterval,
            noteType = NoteHitType.Red,
        };

        NoteList.patterns.Add(testPattern);

        string json = JsonUtility.ToJson(NoteList, true);
        string path = Application.dataPath + "/Resources/TestArcNotePatterns.json";
        System.IO.File.WriteAllText(path, json);

        Debug.Log($"Saved Arc Note Patterns: \n{json}");
    }

    /// <summary>
    /// JSON 파일에서 원형 롱노트 패턴을 로드합니다.
    /// </summary>

    public void TestLoadArcNotePattern()
    {
        string path = Application.dataPath + "/Resources/TestArcNotePatterns.json";
        if (!System.IO.File.Exists(path))
        {
            Debug.LogError("Test pattern file not found!");
            return;
        }

        string json = System.IO.File.ReadAllText(path);
        var loadedPatterns = JsonUtility.FromJson<NoteList>(json);

        Debug.Log($"Loaded {loadedPatterns.patterns.Count} patterns");

        foreach (var pattern in loadedPatterns.patterns)
        {
            SpawnArcLongNote(pattern.startIndex, pattern.arcLength);
        }
    }
    #endregion

    #region 디버그
    /// <summary>
    /// 현재 진행 상태를 화면에 표시합니다.
    /// </summary>
    private void OnGUI()
    {
        if (!isPlaying)
            return;

        double currentDspTime = AudioSettings.dspTime;
        GUILayout.BeginArea(new Rect(10, 10, 300, 150));
        GUILayout.Label($"현재 위치: 마디 {currentBar + 1}, 비트 {currentBeat + 1}");
        GUILayout.Label($"DSP 경과 시간: {(currentDspTime - startDspTime):F3}초");
        if (useGridBPM)
        {
            GUILayout.Label(
                $"단노트 진행: {currentGridBar + 1}/{totalBars} 마디, {currentGridBeat + 1}/4 비트"
            );
            GUILayout.Label($"다음 노트 시간: {(nextGridSpawnTime - currentDspTime):F3}초 후");
        }
        GUILayout.EndArea();
    }

    /// <summary>
    /// 디버그용 시각화를 수행합니다.
    /// </summary>
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying)
            return;

        // 소스 원형 그리기
        Gizmos.color = Color.green;
        DrawCircle(sourceCenter, sourceRadius, segmentCount, true);

        // 타겟 원형 그리기
        Gizmos.color = Color.red;
        DrawCircle(targetCenter, targetRadius, segmentCount, true);

        // 플레이 모드에서 실제 포인트 표시
        if (Application.isPlaying)
        {
            Gizmos.color = Color.yellow;
            for (int i = 0; i < sourcePoints.Count; i++)
            {
                Gizmos.DrawSphere(sourcePoints[i], 0.1f);
            }

            Gizmos.color = Color.cyan;
            for (int i = 0; i < targetPoints.Count; i++)
            {
                Gizmos.DrawSphere(targetPoints[i], 0.1f);
            }
        }
    }

    /// <summary>
    /// 원을 그리는 디버그 기즈모를 그립니다.
    /// </summary>
    /// <param name="center">원의 중심점</param>
    /// <param name="radius">원의 반지름</param>
    /// <param name="segments">원을 구성하는 세그먼트 수</param>
    /// <param name="vertical">수직 원 여부</param>

    private void DrawCircle(Vector3 center, float radius, int segments, bool vertical)
    {
        float angleStep = 360f / segments;
        Vector3 prevPoint;

        if (vertical)
        {
            // XY 평면에 세로 원 그리기
            prevPoint = new Vector3(
                center.x + radius * Mathf.Cos(0),
                center.y + radius * Mathf.Sin(0),
                center.z
            );
        }
        else
        {
            // XZ 평면에 가로 원 그리기 (기존 방식)
            prevPoint = new Vector3(
                center.x + radius * Mathf.Sin(0),
                center.y,
                center.z + radius * Mathf.Cos(0)
            );
        }

        for (int i = 1; i <= segments; i++)
        {
            float angle = i * angleStep;
            float radians = angle * Mathf.Deg2Rad;
            Vector3 point;

            if (vertical)
            {
                // XY 평면에 세로 원 그리기
                point = new Vector3(
                    center.x + radius * Mathf.Cos(radians),
                    center.y + radius * Mathf.Sin(radians),
                    center.z
                );
            }
            else
            {
                // XZ 평면에 가로 원 그리기 (기존 방식)
                point = new Vector3(
                    center.x + radius * Mathf.Sin(radians),
                    center.y,
                    center.z + radius * Mathf.Cos(radians)
                );
            }

            Gizmos.DrawLine(prevPoint, point);
            prevPoint = point;
        }

        Gizmos.DrawSphere(center, 0.3f);
    }
    #endregion
}
