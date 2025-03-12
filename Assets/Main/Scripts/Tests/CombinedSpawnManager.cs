using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CombinedSpawnManager : MonoBehaviour
{
    [Header("노트 프리팹 설정")]
    [SerializeField]
    private GameObject leftNotePrefab;

    [SerializeField]
    private GameObject rightNotePrefab;

    [SerializeField]
    private GameObject primarySegmentPrefab;

    [SerializeField]
    private GameObject symmetricSegmentPrefab;

    [Header("노트 기본 설정")]
    [SerializeField]
    private float segmentSpawnInterval = 0.1f;

    [Header("원형 노트 설정")]
    [SerializeField]
    private float sourceRadius = 5f;

    [SerializeField]
    private float targetRadius = 5f;

    [SerializeField]
    private int segmentCount = 36;

    [SerializeField]
    private Vector3 circleOffset = Vector3.zero;

    [Header("이펙트 설정")]
    [SerializeField]
    private ParticleSystem hitEffectPrefab;

    // 내부 변수
    private GridManager gridManager;
    private Vector3 sourceCenter;
    private Vector3 targetCenter;
    private List<Vector3> sourcePoints = new List<Vector3>();
    private List<Vector3> targetPoints = new List<Vector3>();
    private float noteSpeed;

    private double startDspTime;
    private bool isPlaying = false;
    private int currentBar;
    private int currentBeat;
    private double nextSpawnTime;

    // 노트 데이터 관리
    private NoteList currentNoteList;
    private NoteData currentNoteData;

    #region 초기화 및 기본 동작
    private void Start()
    {
        gridManager = GridManager.Instance;
        if (gridManager == null)
        {
            Debug.LogError("GridManager를 찾을 수 없습니다!");
            return;
        }

        InitializeCirclePositions();
    }

    private void Update()
    {
        if (isPlaying)
        {
            double currentDspTime = AudioSettings.dspTime;
            float currentTime = (float)(currentDspTime - startDspTime);
            UpdateBarAndBeat(currentTime);
        }

        // 테스트용 스페이스바 입력 처리
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            if (!isPlaying)
            {
                CreateTestNoteList();
            }
            else
            {
                StopSpawning();
            }
        }
    }

    private void InitializeCirclePositions()
    {
        int rightGridCenterX = gridManager.TotalHorizontalCells - gridManager.HandGridSize / 2 - 1;
        int gridCenterY = gridManager.VerticalCells / 2;

        Vector3 sourceGridCenter = gridManager.GetCellPosition(
            gridManager.SourceGrid,
            rightGridCenterX,
            gridCenterY
        );
        Vector3 targetGridCenter = gridManager.GetCellPosition(
            gridManager.TargetGrid,
            rightGridCenterX,
            gridCenterY
        );

        sourceCenter = sourceGridCenter + circleOffset;
        targetCenter = targetGridCenter + circleOffset;

        GenerateCirclePoints();
    }
    #endregion

    #region BPM 시스템
    private void CalculateNoteSpeed()
    {
        if (currentNoteList == null)
            return;

        float distance = gridManager.GridDistance;
        float secondsPerBeat = 60f / currentNoteList.bpm;
        float targetHitTime = secondsPerBeat * currentNoteList.beatsPerBar; // 1마디 동안 이동
        noteSpeed = distance / targetHitTime;

        Debug.Log($"=== 노트 속도 계산 ===");
        Debug.Log($"BPM: {currentNoteList.bpm}");
        Debug.Log($"박자: {currentNoteList.beatsPerBar}/4");
        Debug.Log($"비트당 시간: {secondsPerBeat:F3}초");
        Debug.Log($"이동 거리: {distance:F2} 유닛");
        Debug.Log($"목표 도달 시간: {targetHitTime:F2}초");
        Debug.Log($"계산된 노트 속도: {noteSpeed:F2} 유닛/초");
    }

    private void UpdateBarAndBeat(float currentTime)
    {
        if (currentNoteList == null)
            return;

        float secondsPerBeat = 60f / currentNoteList.bpm;
        float totalBeats = currentTime / secondsPerBeat;
        currentBar = Mathf.FloorToInt(totalBeats / currentNoteList.beatsPerBar);
        currentBeat = Mathf.FloorToInt(totalBeats % currentNoteList.beatsPerBar);
    }
    #endregion

    #region 노트 생성 시스템
    private void SpawnNote(NoteData noteData)
    {
        if (noteData == null)
        {
            Debug.LogError("노트 데이터가 null입니다!");
            return;
        }

        currentNoteData = noteData;
        noteData.noteSpeed = noteSpeed;

        switch (noteData.baseType)
        {
            case NoteBaseType.Short:
                SpawnGridNote(noteData);
                break;
            case NoteBaseType.Long:
                SpawnArcLongNote(noteData);
                break;
            default:
                Debug.LogError($"지원하지 않는 노트 타입: {noteData.baseType}");
                break;
        }
    }

    private void SpawnGridNote(NoteData noteData)
    {
        Vector3 startPos = gridManager.GetCellPosition(
            gridManager.SourceGrid,
            (int)noteData.gridpos.x,
            (int)noteData.gridpos.y
        );
        Vector3 targetPos = gridManager.GetCellPosition(
            gridManager.TargetGrid,
            (int)noteData.gridpos.x,
            (int)noteData.gridpos.y
        );

        // 노트 데이터 복사본 생성
        NoteData spawnData = new NoteData
        {
            baseType = noteData.baseType,
            noteType = noteData.noteType,
            noteSpeed = noteSpeed,
            startPosition = startPos,
            targetPosition = targetPos,
            direction = noteData.direction,
            noteAxis = noteData.noteAxis,
            gridpos = noteData.gridpos,
            isLeftGrid = noteData.isLeftGrid,
            bar = noteData.bar,
            beat = noteData.beat,
        };

        GameObject prefab = spawnData.isLeftGrid ? leftNotePrefab : rightNotePrefab;
        GameObject note = Instantiate(prefab, startPos, Quaternion.identity);

        if (spawnData.isLeftGrid)
        {
            if (note.TryGetComponent<LeftNote>(out var leftNote))
            {
                leftNote.Initialize(spawnData);
            }
        }
        else
        {
            if (note.TryGetComponent<RightNote>(out var rightNote))
            {
                rightNote.Initialize(spawnData);
            }
        }

        double spawnTime = AudioSettings.dspTime - startDspTime;
        Debug.Log(
            $"단노트 생성 - 시간: {spawnTime:F3}, 마디: {spawnData.bar}, 비트: {spawnData.beat}, "
                + $"위치: {(spawnData.isLeftGrid ? "왼쪽" : "오른쪽")} ({spawnData.gridpos.x}, {spawnData.gridpos.y})"
        );
    }

    private void SpawnArcLongNote(NoteData noteData)
    {
        if (noteData.startIndex < 0 || noteData.startIndex >= segmentCount)
        {
            Debug.LogError($"잘못된 시작 인덱스: {noteData.startIndex}");
            return;
        }

        int endIndex = (noteData.startIndex + noteData.arcLength) % segmentCount;
        StartCoroutine(SpawnArcSegments(noteData, noteData.startIndex, endIndex, false));

        if (noteData.isSymmetric)
        {
            int symmetricStart = (noteData.startIndex + segmentCount / 2) % segmentCount;
            int symmetricEnd = (endIndex + segmentCount / 2) % segmentCount;
            StartCoroutine(SpawnArcSegments(noteData, symmetricStart, symmetricEnd, true));
        }
    }

    private IEnumerator SpawnArcSegments(
        NoteData noteData,
        int startIndex,
        int endIndex,
        bool isSymmetric
    )
    {
        GameObject prefabToUse = isSymmetric ? symmetricSegmentPrefab : primarySegmentPrefab;
        int currentIndex = startIndex;
        int maxIterations = segmentCount * 2;
        int iterations = 0;

        do
        {
            if (iterations++ > maxIterations)
            {
                Debug.LogError(
                    $"롱노트 생성 중단: 최대 반복 횟수({maxIterations})를 초과했습니다."
                );
                yield break;
            }

            Vector3 sourcePos = sourcePoints[currentIndex];
            Vector3 targetPos = targetPoints[currentIndex];

            // 세그먼트 데이터 복사본 생성
            NoteData segmentData = new NoteData
            {
                baseType = NoteBaseType.Long,
                noteType = noteData.noteType,
                noteSpeed = noteSpeed,
                startPosition = sourcePos,
                targetPosition = targetPos,
                direction = noteData.direction,
                noteAxis = noteData.noteAxis,
                bar = noteData.bar,
                beat = noteData.beat,
                isClockwise = noteData.isClockwise,
                isSymmetric = isSymmetric,
            };

            GameObject segment = Instantiate(prefabToUse, sourcePos, Quaternion.identity);

            // LongNoteSegment 컴포넌트가 없으면 추가
            LongNoteSegment mover = segment.GetComponent<LongNoteSegment>();
            if (mover == null)
            {
                mover = segment.AddComponent<LongNoteSegment>();
            }

            mover.Initialize(segmentData);

            if (hitEffectPrefab != null)
            {
                mover.SetHitEffect(hitEffectPrefab.gameObject);
            }

            if (currentIndex == endIndex)
                break;

            currentIndex = noteData.isClockwise
                ? (currentIndex + 1) % segmentCount
                : (currentIndex - 1 + segmentCount) % segmentCount;

            yield return new WaitForSeconds(segmentSpawnInterval);
        } while (true);
    }
    #endregion

    #region 노트 리스트 관리
    public void StartSpawning(NoteList noteList)
    {
        if (noteList == null || noteList.patterns.Count == 0)
        {
            Debug.LogError("노트 패턴이 없습니다!");
            return;
        }

        currentNoteList = noteList;
        CalculateNoteSpeed();

        startDspTime = AudioSettings.dspTime;
        nextSpawnTime = startDspTime;

        StartCoroutine(SpawnNotesCoroutine(noteList));
        isPlaying = true;

        Debug.Log($"=== 노트 생성 시작 ===");
        Debug.Log($"BPM: {noteList.bpm}");
        Debug.Log($"박자: {noteList.beatsPerBar}/4");
        Debug.Log($"비트 간격: {60f / noteList.bpm:F3}초");
        Debug.Log($"마디 길이: {(60f / noteList.bpm) * noteList.beatsPerBar:F3}초");
    }

    public void StopSpawning()
    {
        isPlaying = false;
        StopAllCoroutines();
    }

    private IEnumerator SpawnNotesCoroutine(NoteList noteList)
    {
        float secondsPerBeat = 60f / noteList.bpm; // 미리 계산

        foreach (var noteData in noteList.patterns)
        {
            double waitTime = nextSpawnTime - AudioSettings.dspTime;
            if (waitTime > 0)
                yield return new WaitForSeconds((float)waitTime);

            SpawnNote(noteData);
            nextSpawnTime += secondsPerBeat; // 미리 계산된 값 사용
        }
    }

    private void CreateTestNoteList()
    {
        var noteList = new NoteList
        {
            bpm = 120f,
            beatsPerBar = 4, // 4/4박자
        };

        // 테스트용 단노트 추가
        noteList.patterns.Add(
            new NoteData
            {
                baseType = NoteBaseType.Short,
                isLeftGrid = true,
                gridpos = new Vector2(1, 1),
                bar = 0,
                beat = 0,
            }
        );

        // 테스트용 롱노트 추가 (길이 제한)
        noteList.patterns.Add(
            new NoteData
            {
                baseType = NoteBaseType.Long,
                startIndex = 0,
                arcLength = Mathf.Min(10, segmentCount / 4), // 최대 길이 제한
                isSymmetric = true,
                isClockwise = true,
                bar = 1,
                beat = 0,
            }
        );

        StartSpawning(noteList);
    }
    #endregion

    #region 원형 포인트 생성
    private void GenerateCirclePoints()
    {
        sourcePoints.Clear();
        targetPoints.Clear();

        float angleStep = 360f / segmentCount;

        for (int i = 0; i < segmentCount; i++)
        {
            float angle = i * angleStep;
            float radians = angle * Mathf.Deg2Rad;

            float x = Mathf.Cos(radians);
            float y = Mathf.Sin(radians);

            sourcePoints.Add(
                new Vector3(
                    sourceCenter.x + sourceRadius * x,
                    sourceCenter.y + sourceRadius * y,
                    sourceCenter.z
                )
            );

            targetPoints.Add(
                new Vector3(
                    targetCenter.x + targetRadius * x,
                    targetCenter.y + targetRadius * y,
                    targetCenter.z
                )
            );
        }

        Debug.Log($"원형 경로 생성 완료: {segmentCount}개의 포인트");
    }
    #endregion

    #region 디버그
    private void OnGUI()
    {
        if (!isPlaying)
            return;

        double currentDspTime = AudioSettings.dspTime;
        GUILayout.BeginArea(new Rect(10, 10, 300, 100));
        GUILayout.Label($"현재 위치: 마디 {currentBar + 1}, 비트 {currentBeat + 1}");
        GUILayout.Label($"DSP 경과 시간: {(currentDspTime - startDspTime):F3}초");
        GUILayout.EndArea();
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying)
            return;

        Gizmos.color = Color.green;
        DrawCircle(sourceCenter, sourceRadius);

        Gizmos.color = Color.red;
        DrawCircle(targetCenter, targetRadius);

        if (sourcePoints != null && targetPoints != null)
        {
            Gizmos.color = Color.yellow;
            foreach (Vector3 point in sourcePoints)
            {
                Gizmos.DrawSphere(point, 0.1f);
            }

            Gizmos.color = Color.cyan;
            foreach (Vector3 point in targetPoints)
            {
                Gizmos.DrawSphere(point, 0.1f);
            }
        }
    }

    private void DrawCircle(Vector3 center, float radius)
    {
        int detail = 36;
        float angleStep = 360f / detail;
        Vector3 prevPoint = center + new Vector3(radius, 0, 0);

        for (int i = 1; i <= detail; i++)
        {
            float angle = i * angleStep;
            float radians = angle * Mathf.Deg2Rad;
            Vector3 point =
                center + new Vector3(radius * Mathf.Cos(radians), radius * Mathf.Sin(radians), 0);

            Gizmos.DrawLine(prevPoint, point);
            prevPoint = point;
        }
    }
    #endregion
}
