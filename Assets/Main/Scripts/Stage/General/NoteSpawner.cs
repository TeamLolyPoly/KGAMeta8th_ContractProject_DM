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

    private Vector3 sourceCenter;
    private Vector3 targetCenter;
    private List<Vector3> sourcePoints = new List<Vector3>();
    private List<Vector3> targetPoints = new List<Vector3>();
    private float noteSpeed;
    private double startDspTime;
    private double nextSpawnTime;

    private GridGenerator gridGenerator;

    private Note shortNotePrefab;
    private LongNote longNotePrefab;
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
        longNotePrefab = Resources.Load<LongNote>("Prefabs/Stage/Note/LongNote");
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

    public void StartSpawn(double startTime, float preRollTime)
    {
        if (noteMap == null || noteMap.notes == null || noteMap.notes.Count == 0)
        {
            Debug.LogError("노트 맵이 비어있습니다!");
            return;
        }

        this.startDspTime = startTime;
        double currentDspTime = AudioSettings.dspTime;
        Debug.Log(
            $"노트 스폰 시작: 시작 시간={startTime:F3}, 현재 시간={currentDspTime:F3}, 프리롤={preRollTime:F3}초"
        );
        Debug.Log(
            $"BPM: {noteMap.bpm}, 마디당 비트: {noteMap.beatsPerBar}, 총 노트 수: {noteMap.notes.Count}"
        );

        StartCoroutine(SpawnNotesCoroutine(preRollTime));
    }

    public void StopSpawning()
    {
        StopAllCoroutines();
    }

    private IEnumerator SpawnNotesCoroutine(float preRollTime)
    {
        if (noteMap == null || noteMap.notes.Count == 0)
        {
            Debug.LogError("노트맵이 비어있습니다!");
            yield break;
        }

        // 노트 맵을 마디와 비트 순서로 정렬
        List<NoteData> sortedNotes = new List<NoteData>(noteMap.notes);
        sortedNotes.Sort(
            (a, b) =>
            {
                if (a.bar != b.bar)
                    return a.bar.CompareTo(b.bar);
                return a.beat.CompareTo(b.beat);
            }
        );

        // 기본 설정 계산
        float secondsPerBeat = 60f / noteMap.bpm;
        float noteTravelTime = gridGenerator.GridDistance / noteSpeed;

        Debug.Log(
            $"노트 스폰 준비: 노트 이동 시간={noteTravelTime:F3}초, 비트당 시간={secondsPerBeat:F3}초, 프리롤={preRollTime:F3}초"
        );

        // 음악 시작 절대 시간 (스케줄링 기준점)
        double musicStartTime = startDspTime + preRollTime;

        // 각 노트에 대한 생성 시간 계산 및 스폰
        foreach (NoteData note in sortedNotes)
        {
            // 노트의 목표 도착 시간 계산 (마디와 비트 기준)
            float targetHitTime = (note.bar * noteMap.beatsPerBar + note.beat) * secondsPerBeat;

            // 음악 시작 시간 기준 노트 도착 시간
            double absoluteHitTime = musicStartTime + targetHitTime;

            // 노트 생성 시간 (도착 시간 - 이동 시간)
            double spawnTime = absoluteHitTime - noteTravelTime;

            // 현재 시간부터 생성 시간까지 기다림
            double waitTime = spawnTime - AudioSettings.dspTime;

            if (waitTime > 0)
            {
                yield return new WaitForSecondsRealtime((float)waitTime);
            }

            // 노트 생성
            SpawnNote(note);

            // 짧은 대기 시간 추가 (노트 생성 간 간격)
            yield return new WaitForSecondsRealtime(0.01f);
        }

        Debug.Log($"모든 노트 생성 완료! 총 {sortedNotes.Count}개의 노트");
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

        switch (noteData.noteType)
        {
            case NoteType.Short:
                SpawnShortNote(noteData);
                break;
            case NoteType.Long:
                SpawnLongNote(noteData);
                break;
            default:
                Debug.LogError($"지원하지 않는 노트 타입: {noteData.noteType}");
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
            noteObj.SetNoteColor(noteData.noteColor);
        }

        if (noteObj != null && hitFXPrefab != null)
        {
            noteObj.SetHitFX(hitFXPrefab);
        }
    }

    private void SpawnLongNote(NoteData noteData)
    {
        if (
            noteData.GetType().GetField("StartIndex") != null
            || noteData.GetType().GetField("EndIndex") != null
        )
        {
            Debug.Log($"JSON에서 대문자 인덱스 속성이 발견되었습니다: StartIndex/EndIndex");
        }

        Debug.Log(
            $"롱노트 초기 정보 - 마디: {noteData.bar}, 박자: {noteData.beat}, "
                + $"startIndex: {noteData.startIndex}, endIndex: {noteData.endIndex}"
        );

        if (noteData.startIndex <= 0)
        {
            int gridX = (int)noteData.StartCell.x;
            int gridY = (int)noteData.StartCell.y;

            float normX = Mathf.Clamp(
                (gridX / (float)(gridGenerator.TotalHorizontalCells - 1)) * 2 - 1,
                -1,
                1
            );
            float normY = Mathf.Clamp(
                (gridY / (float)(gridGenerator.VerticalCells - 1)) * 2 - 1,
                -1,
                1
            );

            float angle = Mathf.Atan2(normY, normX) * Mathf.Rad2Deg;
            if (angle < 0)
                angle += 360f;

            noteData.startIndex = Mathf.RoundToInt(angle / (360f / segmentCount)) % segmentCount;
            Debug.Log($"시작 인덱스 계산됨: {noteData.startIndex}, 각도: {angle}");
        }

        if (noteData.startIndex < 0 || noteData.startIndex >= segmentCount)
        {
            Debug.LogError(
                $"잘못된 시작 인덱스: {noteData.startIndex}, 유효 범위: 0-{segmentCount - 1}"
            );
            return;
        }

        int calculatedArcLength;

        calculatedArcLength = noteData.CalculateArcLength(
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

        if (noteData.endIndex <= 0)
        {
            noteData.endIndex = (noteData.startIndex + calculatedArcLength) % segmentCount;
            Debug.Log($"끝 인덱스 계산됨: {noteData.endIndex}, 길이: {calculatedArcLength}");
        }
        else
        {
            Debug.Log($"기존 끝 인덱스 사용: {noteData.endIndex}");
        }

        if (noteData.endIndex < 0 || noteData.endIndex >= segmentCount)
        {
            Debug.LogWarning(
                $"끝 인덱스가 범위를 벗어났습니다. 값: {noteData.endIndex}, 유효 범위: 0-{segmentCount - 1}"
            );
            noteData.endIndex = (noteData.startIndex + calculatedArcLength) % segmentCount;
            Debug.Log($"끝 인덱스 재계산됨: {noteData.endIndex}");
        }

        int pathLength;
        if (noteData.isClockwise)
        {
            pathLength = (noteData.endIndex + segmentCount - noteData.startIndex) % segmentCount;
        }
        else
        {
            pathLength = (noteData.startIndex + segmentCount - noteData.endIndex) % segmentCount;
        }

        Debug.Log(
            $"롱노트 생성 - 마디: {noteData.bar}, 박자: {noteData.beat}, "
                + $"지속 시간: {noteData.durationBars}마디 {noteData.durationBeats}박자, "
                + $"계산된 인덱스: 시작 {noteData.startIndex}, 끝 {noteData.endIndex}, 경로 길이: {pathLength}"
        );

        StartCoroutine(
            SpawnSegments(noteData, noteData.startIndex, noteData.endIndex, false, pathLength)
        );

        if (noteData.isSymmetric)
        {
            int symmetricStart = (noteData.startIndex + segmentCount / 2) % segmentCount;
            int symmetricEnd = (noteData.endIndex + segmentCount / 2) % segmentCount;
            Debug.Log(
                $"대칭 롱노트: symmetricStart: {symmetricStart}, symmetricEnd: {symmetricEnd}"
            );
            StartCoroutine(SpawnSegments(noteData, symmetricStart, symmetricEnd, true, pathLength));
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
        if (arcLength <= 0)
        {
            Debug.LogError($"유효하지 않은 경로 길이: {arcLength}");
            yield break;
        }

        int currentIndex = startIndex;
        int maxIterations = segmentCount * 2;
        int iterations = 0;

        float secondsPerBeat = 60f / noteMap.bpm;
        float totalDurationSeconds =
            (noteData.durationBars * noteMap.beatsPerBar + noteData.durationBeats) * secondsPerBeat;

        if (totalDurationSeconds <= 0)
        {
            totalDurationSeconds = arcLength * segmentSpawnInterval;
            Debug.LogWarning("롱노트 지속 시간이 0입니다. 기본값으로 설정합니다.");
        }

        float segmentSpeed = noteSpeed;

        NoteColor segmentColor = isSymmetric ? NoteColor.Red : noteData.noteColor;
        if (segmentColor == NoteColor.None)
        {
            segmentColor = NoteColor.Blue;
        }

        Debug.Log(
            $"롱노트 세그먼트 생성 시작 - 시작 인덱스: {startIndex}, 끝 인덱스: {endIndex}, 길이: {arcLength}, 색상: {segmentColor}"
        );

        System.Func<int, int> clockwiseNext = (idx) => (idx + 1) % segmentCount;
        System.Func<int, int> counterClockwiseNext = (idx) =>
            (idx - 1 + segmentCount) % segmentCount;
        System.Func<int, int> nextIndex = noteData.isClockwise
            ? clockwiseNext
            : counterClockwiseNext;

        System.Func<int, bool> reachedEndIndex = (idx) => idx == endIndex;

        do
        {
            if (iterations++ > maxIterations)
            {
                Debug.LogError(
                    $"롱노트 생성 중단: 최대 반복 횟수({maxIterations})를 초과했습니다. "
                        + $"현재 인덱스: {currentIndex}, 목표 인덱스: {endIndex}"
                );
                yield break;
            }

            Vector3 sourcePos = sourcePoints[currentIndex];
            Vector3 targetPos = targetPoints[currentIndex];

            NoteData segmentData = new NoteData
            {
                noteType = NoteType.Long,
                noteColor = segmentColor,
                noteSpeed = segmentSpeed,
                direction = noteData.direction,
                noteAxis = noteData.noteAxis,
                bar = noteData.bar,
                beat = noteData.beat,
                durationBars = noteData.durationBars,
                durationBeats = noteData.durationBeats,
                isClockwise = noteData.isClockwise,
                isSymmetric = isSymmetric,
                gridGenerator = gridGenerator,
                startIndex = currentIndex,
            };

            try
            {
                LongNote segment = Instantiate(
                    longNotePrefab,
                    sourcePos,
                    Quaternion.Euler(90, 0, 0)
                );
                if (segment != null)
                {
                    segment.Initialize(segmentData);

                    segment.SetPositions(sourcePos, targetPos);

                    segment.SetNoteColor(segmentColor);

                    if (hitFXPrefab != null)
                    {
                        segment.SetHitFX(hitFXPrefab);
                    }

                    Debug.Log(
                        $"롱노트 세그먼트 생성 - 인덱스: {currentIndex}, 소스: {sourcePos}, 타겟: {targetPos}"
                    );
                }
                else
                {
                    Debug.LogError("롱노트 세그먼트 생성 실패: 프리팹 인스턴스가 null입니다.");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"롱노트 세그먼트 생성 중 예외 발생: {e.Message}");
            }

            if (reachedEndIndex(currentIndex))
                break;

            currentIndex = nextIndex(currentIndex);

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
