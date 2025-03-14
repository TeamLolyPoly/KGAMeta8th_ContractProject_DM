using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoteSpawner : MonoBehaviour
{
    [SerializeField]
    private float segmentSpawnInterval = 0.1f;

    [SerializeField]
    private float sourceRadius = 0.5f;

    [SerializeField]
    private float targetRadius = 0.5f;

    [SerializeField]
    private int segmentCount = 72;

    [SerializeField]
    private Vector3 circleOffset = Vector3.zero;

    public Color leftColor = Color.yellow;
    public Color rightColor = Color.magenta;

    private Vector3 sourceCenter;
    private Vector3 targetCenter;
    private List<Vector3> sourcePoints = new List<Vector3>();
    private List<Vector3> targetPoints = new List<Vector3>();
    private float noteSpeed;
    private double startDspTime;
    private double nextSpawnTime;

    private GridGenerator gridGenerator;

    private Note shortNotePrefab;
    private Note longNotePrefab;
    private GameObject hitFXPrefab;
    private NoteMap noteMap;

    private bool isInitialized = false;
    public bool IsInitialized => isInitialized;

    public void Initialize(GridGenerator gridGen, NoteMap noteMap)
    {
        SetPrefabs();
        SetGrids(gridGen, noteMap);
        GenerateCirclePoints();
        CalculateNoteSpeed();
        isInitialized = true;
    }

    private void SetPrefabs()
    {
        shortNotePrefab = Resources.Load<Note>("Prefabs/Stage/Note/ShortNote");
        longNotePrefab = Resources.Load<Note>("Prefabs/Stage/Note/LongNote");
        hitFXPrefab = Resources.Load<GameObject>("Prefabs/Stage/Effects/HitFX");
    }

    private void SetGrids(GridGenerator gridGen, NoteMap noteMap)
    {
        gridGenerator = gridGen;
        this.noteMap = noteMap;

        Vector3 sourceGridCenter = gridGenerator.GetHandGridCenter(
            gridGenerator.sourceOrigin,
            false
        );
        Vector3 targetGridCenter = gridGenerator.GetHandGridCenter(
            gridGenerator.targetOrigin,
            false
        );

        sourceCenter = sourceGridCenter + circleOffset;
        targetCenter = targetGridCenter + circleOffset;
    }

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

    private void CalculateNoteSpeed()
    {
        if (noteMap == null)
            return;

        float distance = gridGenerator.GridDistance;
        float secondsPerBeat = 60f / noteMap.bpm;
        float targetHitTime = secondsPerBeat * noteMap.beatsPerBar;
        noteSpeed = distance / targetHitTime;

        Debug.Log(
            $"=== 노트 속도 계산 ===\n"
                + $"BPM: {noteMap.bpm}\n"
                + $"박자: {noteMap.beatsPerBar}/4\n"
                + $"비트당 시간: {secondsPerBeat:F3}초\n"
                + $"이동 거리: {distance:F2} 유닛\n"
                + $"목표 도달 시간: {targetHitTime:F2}초\n"
                + $"계산된 노트 속도: {noteSpeed:F2} 유닛/초"
        );
    }

    public void StartSpawn(double startTime)
    {
        if (noteMap == null || noteMap.notes.Count == 0)
        {
            Debug.LogError("노트 패턴이 없습니다!");
            return;
        }

        startDspTime = startTime;
        nextSpawnTime = startDspTime;

        StartCoroutine(SpawnNotesCoroutine());

        Debug.Log(
            $"=== 노트 생성 시작 === \n"
                + $"BPM: {noteMap.bpm}\n"
                + $"박자: {noteMap.beatsPerBar}/4\n"
                + $"비트 간격: {60f / noteMap.bpm:F3}초\n"
                + $"마디 길이: {(60f / noteMap.bpm) * noteMap.beatsPerBar:F3}초"
        );
    }

    public void StopSpawning()
    {
        StopAllCoroutines();
    }

    private IEnumerator SpawnNotesCoroutine()
    {
        float secondsPerBeat = 60f / noteMap.bpm;

        foreach (var noteData in noteMap.notes)
        {
            double waitTime = nextSpawnTime - AudioSettings.dspTime;
            if (waitTime > 0)
                yield return new WaitForSeconds((float)waitTime);

            SpawnNote(noteData);
            nextSpawnTime += secondsPerBeat;
        }
    }

    private void SpawnNote(NoteData noteData)
    {
        if (noteData == null)
        {
            Debug.LogError("노트 데이터가 null입니다!");
            return;
        }

        noteData.noteSpeed = noteSpeed;
        noteData.gridGenerator = gridGenerator;

        switch (noteData.baseType)
        {
            case NoteBaseType.Short:
                SpawnShortNote(noteData);
                break;
            case NoteBaseType.Long:
                SpawnLongNote(noteData);
                break;
            default:
                Debug.LogError($"지원하지 않는 노트 타입: {noteData.baseType}");
                break;
        }
    }

    private void SpawnShortNote(NoteData noteData)
    {
        if (noteData.TargetCell == Vector2.zero)
        {
            noteData.TargetCell = noteData.StartCell;
        }

        Note noteObj = Note.CreateNote(noteData, shortNotePrefab, longNotePrefab);

        if (noteObj != null)
        {
            noteObj.SetNoteColor(noteData.isLeftGrid ? leftColor : rightColor);
        }

        if (noteObj != null && hitFXPrefab != null)
        {
            noteObj.SetHitFX(hitFXPrefab);
        }

        double spawnTime = AudioSettings.dspTime - startDspTime;
        Debug.Log(
            $"단노트 생성 - 시간: {spawnTime:F3}, 마디: {noteData.bar}, 비트: {noteData.beat}, "
                + $"위치: {(noteData.isLeftGrid ? "왼쪽" : "오른쪽")} ({noteData.StartCell.x}, {noteData.StartCell.y})"
        );
    }

    private void SpawnLongNote(NoteData noteData)
    {
        if (noteData.startIndex < 0 || noteData.startIndex >= segmentCount)
        {
            Debug.LogError($"잘못된 시작 인덱스: {noteData.startIndex}");
            return;
        }

        int calculatedArcLength = noteData.CalculateArcLength(
            segmentCount,
            noteMap.bpm,
            noteMap.beatsPerBar,
            segmentSpawnInterval
        );

        if (calculatedArcLength <= 0)
        {
            Debug.LogWarning($"롱노트 길이가 너무 짧습니다. 최소 길이(1)로 설정합니다.");
            calculatedArcLength = 1;
        }

        int endIndex = (noteData.startIndex + calculatedArcLength) % segmentCount;

        Debug.Log(
            $"롱노트 생성 - 마디: {noteData.bar}, 박자: {noteData.beat}, "
                + $"지속 시간: {noteData.durationBars}마디 {noteData.durationBeats}박자, "
                + $"계산된 길이: {calculatedArcLength}"
        );

        StartCoroutine(
            SpawnSegments(noteData, noteData.startIndex, endIndex, false, calculatedArcLength)
        );

        if (noteData.isSymmetric)
        {
            int symmetricStart = (noteData.startIndex + segmentCount / 2) % segmentCount;
            int symmetricEnd = (endIndex + segmentCount / 2) % segmentCount;
            StartCoroutine(
                SpawnSegments(noteData, symmetricStart, symmetricEnd, true, calculatedArcLength)
            );
        }
    }

    private IEnumerator SpawnSegments(
        NoteData noteData,
        int startIndex,
        int endIndex,
        bool isSymmetric,
        int arcLength
    )
    {
        int currentIndex = startIndex;
        int maxIterations = segmentCount * 2;
        int iterations = 0;

        // 롱노트의 총 지속 시간 계산
        float secondsPerBeat = 60f / noteMap.bpm;
        float totalDurationSeconds =
            (noteData.durationBars * noteMap.beatsPerBar + noteData.durationBeats) * secondsPerBeat;

        // 지속 시간이 0이면 기본값 설정
        if (totalDurationSeconds <= 0)
        {
            totalDurationSeconds = arcLength * segmentSpawnInterval;
            Debug.LogWarning("롱노트 지속 시간이 0입니다. 기본값으로 설정합니다.");
        }

        // 각 세그먼트의 속도 계산 (전체 거리를 총 지속 시간으로 나눔)
        float segmentSpeed = noteSpeed;
        if (totalDurationSeconds > 0)
        {
            // 원형 경로의 총 거리 계산 (대략적인 계산)
            float totalDistance = Vector3.Distance(
                sourcePoints[startIndex],
                targetPoints[startIndex]
            );
            segmentSpeed = totalDistance / totalDurationSeconds;
        }

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

            NoteData segmentData = new NoteData
            {
                baseType = NoteBaseType.Long,
                noteType = noteData.noteType,
                noteSpeed = segmentSpeed, // 계산된 속도 사용
                direction = noteData.direction,
                noteAxis = noteData.noteAxis,
                bar = noteData.bar,
                beat = noteData.beat,
                durationBars = noteData.durationBars,
                durationBeats = noteData.durationBeats,
                isClockwise = noteData.isClockwise,
                isSymmetric = isSymmetric,
                gridGenerator = gridGenerator,
            };

            Note segment = Instantiate(longNotePrefab, sourcePos, Quaternion.Euler(90, 0, 0));

            if (isSymmetric)
            {
                segment.SetNoteColor(Color.red);
            }

            if (segment is LongNote longNote)
            {
                longNote.Initialize(segmentData);
                longNote.SetPositions(sourcePos, targetPos);
            }

            if (hitFXPrefab != null)
            {
                segment.SetHitFX(hitFXPrefab);
            }

            if (currentIndex == endIndex)
                break;

            currentIndex = noteData.isClockwise
                ? (currentIndex + 1) % segmentCount
                : (currentIndex - 1 + segmentCount) % segmentCount;

            yield return new WaitForSeconds(segmentSpawnInterval);
        } while (true);
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
}
