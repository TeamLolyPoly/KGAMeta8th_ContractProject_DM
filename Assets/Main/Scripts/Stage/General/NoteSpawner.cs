using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoteSpawner : MonoBehaviour
{
    [SerializeField]
    private float segmentSpawnInterval = 0.1f;

    [SerializeField]
    private float sourceRadius = 5f;

    [SerializeField]
    private float targetRadius = 5f;

    [SerializeField]
    private int segmentCount = 36;

    [SerializeField]
    private Vector3 circleOffset = Vector3.zero;

    private Vector3 sourceCenter;
    private Vector3 targetCenter;
    private List<Vector3> sourcePoints = new List<Vector3>();
    private List<Vector3> targetPoints = new List<Vector3>();
    private float noteSpeed;
    private double startDspTime;
    private double nextSpawnTime;

    private GridGenerator gridGenerator;

    private Note leftNotePrefab;
    private Note rightNotePrefab;
    private Note longNotePrefab;
    private Note segmentPrefab;
    private ParticleSystem hitFXPrefab;
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
        leftNotePrefab = Resources.Load<Note>("Prefabs/Stage/Note/LeftNote");
        rightNotePrefab = Resources.Load<Note>("Prefabs/Stage/Note/RightNote");
        longNotePrefab = Resources.Load<Note>("Prefabs/Stage/Note/LongNote");
        segmentPrefab = Resources.Load<Note>("Prefabs/Stage/Note/Segment");
        hitFXPrefab = Resources.Load<ParticleSystem>("Prefabs/Stage/Effects/HitFX");
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

        Debug.Log($"=== 노트 생성 시작 ===");
        Debug.Log($"BPM: {noteMap.bpm}");
        Debug.Log($"박자: {noteMap.beatsPerBar}/4");
        Debug.Log($"비트 간격: {60f / noteMap.bpm:F3}초");
        Debug.Log($"마디 길이: {(60f / noteMap.bpm) * noteMap.beatsPerBar:F3}초");
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
        Vector3 startPos = gridGenerator.GetCellPosition(
            gridGenerator.sourceOrigin,
            (int)noteData.StartCell.x,
            (int)noteData.StartCell.y
        );

        Vector3 targetPos = gridGenerator.GetCellPosition(
            gridGenerator.targetOrigin,
            (int)noteData.StartCell.x,
            (int)noteData.StartCell.y
        );

        NoteData spawnData = new NoteData
        {
            baseType = noteData.baseType,
            noteType = noteData.noteType,
            noteSpeed = noteSpeed,
            startPosition = startPos,
            targetPosition = targetPos,
            direction = noteData.direction,
            noteAxis = noteData.noteAxis,
            StartCell = noteData.StartCell,
            isLeftGrid = noteData.isLeftGrid,
            bar = noteData.bar,
            beat = noteData.beat,
        };

        Note note = spawnData.isLeftGrid ? leftNotePrefab : rightNotePrefab;
        Note noteObj = Instantiate(note, startPos, Quaternion.identity);

        noteObj.Initialize(spawnData);

        double spawnTime = AudioSettings.dspTime - startDspTime;
        Debug.Log(
            $"단노트 생성 - 시간: {spawnTime:F3}, 마디: {spawnData.bar}, 비트: {spawnData.beat}, "
                + $"위치: {(spawnData.isLeftGrid ? "왼쪽" : "오른쪽")} ({spawnData.StartCell.x}, {spawnData.StartCell.y})"
        );
    }

    private void SpawnLongNote(NoteData noteData)
    {
        if (noteData.startIndex < 0 || noteData.startIndex >= segmentCount)
        {
            Debug.LogError($"잘못된 시작 인덱스: {noteData.startIndex}");
            return;
        }

        int endIndex = (noteData.startIndex + noteData.arcLength) % segmentCount;
        StartCoroutine(SpawnSegments(noteData, noteData.startIndex, endIndex, false));

        if (noteData.isSymmetric)
        {
            int symmetricStart = (noteData.startIndex + segmentCount / 2) % segmentCount;
            int symmetricEnd = (endIndex + segmentCount / 2) % segmentCount;
            StartCoroutine(SpawnSegments(noteData, symmetricStart, symmetricEnd, true));
        }
    }

    private IEnumerator SpawnSegments(
        NoteData noteData,
        int startIndex,
        int endIndex,
        bool isSymmetric
    )
    {
        Note prefabToUse = isSymmetric ? segmentPrefab : longNotePrefab;
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

            Note segment = Instantiate(prefabToUse, sourcePos, Quaternion.identity);

            Segment mover = segment.GetComponent<Segment>();
            if (mover == null)
            {
                mover = segment.gameObject.AddComponent<Segment>();
            }

            mover.Initialize(segmentData);

            if (hitFXPrefab != null)
            {
                mover.SetHitFX(hitFXPrefab);
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
