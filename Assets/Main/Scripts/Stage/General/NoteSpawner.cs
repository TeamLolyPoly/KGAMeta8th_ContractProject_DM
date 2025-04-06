using System;
using System.Collections;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;
using UnityEngine;

public class NoteSpawner : MonoBehaviour
{
    [SerializeField]
    private float segmentSpawnInterval = 0.1f;

    [SerializeField]
    private float sourceRadius = 0.35f;

    [SerializeField]
    private float targetRadius = 0.35f;

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

        Debug.Log($"[NoteSpawner] 원형 경로 생성 완료: {segmentCount}개의 포인트");
    }

    private void CalculateNoteSpeed()
    {
        if (noteMap == null)
            return;

        float distance = gridGenerator.GridDistance;
        float secondsPerBeat = 60f / noteMap.bpm;
        float targetHitTime = secondsPerBeat * noteMap.beatsPerBar;
        noteSpeed = distance / targetHitTime;
    }

    public void StartSpawn(double startTime, float preRollTime)
    {
        if (noteMap == null || noteMap.notes == null || noteMap.notes.Count == 0)
        {
            Debug.LogError("[NoteSpawner] 노트 맵이 비어있습니다!");
            return;
        }

        startDspTime = startTime;

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
            Debug.LogError("[NoteSpawner] 노트맵이 비어있습니다!");
            yield break;
        }

        List<NoteData> sortedNotes = new List<NoteData>(noteMap.notes);
        sortedNotes.Sort(
            (a, b) =>
            {
                if (a.bar != b.bar)
                    return a.bar.CompareTo(b.bar);
                return a.beat.CompareTo(b.beat);
            }
        );

        float secondsPerBeat = 60f / noteMap.bpm;

        float noteTravelTime = gridGenerator.GridDistance / noteSpeed;

        double musicStartTime = startDspTime + preRollTime;

        foreach (NoteData note in sortedNotes)
        {
            float targetHitTime = (note.bar * noteMap.beatsPerBar + note.beat) * secondsPerBeat;

            double absoluteHitTime = musicStartTime + targetHitTime;

            double spawnTime = absoluteHitTime - noteTravelTime;

            double waitTime = spawnTime - AudioSettings.dspTime;

            if (waitTime > 0)
            {
                yield return new WaitForSecondsRealtime((float)waitTime);
            }

            SpawnNote(note);

            yield return new WaitForSecondsRealtime(0.01f);
        }
    }

    private void SpawnNote(NoteData noteData)
    {
        if (noteData == null)
        {
            Debug.LogError("[NoteSpawner] 노트 데이터가 null입니다!");
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
                Debug.LogError($"[NoteSpawner] 지원하지 않는 노트 타입: {noteData.noteType}");
                break;
        }
    }

    private void SpawnShortNote(NoteData noteData)
    {
        if (noteData.TargetCell == Vector2.zero)
        {
            noteData.TargetCell = noteData.StartCell;
        }

        ShortNote noteObj = PoolManager.Instance.Spawn<ShortNote>(
            shortNotePrefab.gameObject,
            noteData.GetStartPosition(),
            Quaternion.identity
        );

        noteObj.Initialize(noteData);

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
        if (noteData.noteType != NoteType.Long)
        {
            Debug.LogError("[NoteSpawner] 롱노트가 아닌 데이터로 롱노트 생성 시도");
            return;
        }

        if (sourcePoints.Count == 0 || targetPoints.Count == 0)
        {
            GenerateCirclePoints();
        }

        int startIndex = noteData.startIndex;
        int endIndex = noteData.endIndex;

        int arcLength = noteData.CalculateArcLength(
            segmentCount,
            noteMap.bpm,
            noteMap.beatsPerBar,
            segmentSpawnInterval
        );

        float secondsPerBeat = 60f / noteMap.bpm;
        float totalDurationSeconds =
            (noteData.durationBars * noteMap.beatsPerBar + noteData.durationBeats) * secondsPerBeat;

        if (endIndex < 0 || endIndex >= segmentCount)
        {
            if (noteData.isClockwise)
            {
                endIndex = (startIndex + arcLength) % segmentCount;
            }
            else
            {
                endIndex = (startIndex - arcLength + segmentCount) % segmentCount;
            }
        }

        StartCoroutine(
            SpawnSegments(noteData, startIndex, endIndex, false, arcLength, totalDurationSeconds)
        );

        if (noteData.isSymmetric)
        {
            int symmetricStartIndex = (startIndex + segmentCount / 2) % segmentCount;
            int symmetricEndIndex = (endIndex + segmentCount / 2) % segmentCount;

            StartCoroutine(
                SpawnSegments(
                    noteData,
                    symmetricStartIndex,
                    symmetricEndIndex,
                    true,
                    arcLength,
                    totalDurationSeconds
                )
            );
        }
    }

    private IEnumerator SpawnSegments(
        NoteData noteData,
        int startIndex,
        int endIndex,
        bool isSymmetric,
        int arcLength,
        float totalDurationSeconds
    )
    {
        if (arcLength <= 0)
        {
            Debug.LogError($"[NoteSpawner] 유효하지 않은 경로 길이: {arcLength}");
            yield break;
        }

        float segmentInterval = totalDurationSeconds / arcLength;

        segmentInterval = Mathf.Max(segmentInterval, 0.05f);

        NoteColor segmentColor = isSymmetric ? NoteColor.Red : noteData.noteColor;
        if (segmentColor == NoteColor.None)
        {
            segmentColor = NoteColor.Blue;
        }

        List<int> pathIndices = new List<int>();
        int currentIndex = startIndex;
        int direction = noteData.isClockwise ? 1 : -1;

        pathIndices.Add(currentIndex);

        for (int i = 0; i < segmentCount && pathIndices.Count < arcLength; i++)
        {
            currentIndex = (currentIndex + direction + segmentCount) % segmentCount;
            pathIndices.Add(currentIndex);

            if (currentIndex == endIndex)
                break;
        }

        if (pathIndices.Count < arcLength)
        {
            int remaining = arcLength - pathIndices.Count;

            for (int i = 0; i < remaining; i++)
            {
                pathIndices.Add(endIndex);
            }
        }
        else if (pathIndices.Count > arcLength)
        {
            pathIndices = pathIndices.GetRange(0, arcLength);
        }

        for (int i = 0; i < pathIndices.Count; i++)
        {
            int index = pathIndices[i];
            Vector3 sourcePos = sourcePoints[index];
            Vector3 targetPos = targetPoints[index];

            NoteData segmentData = new NoteData
            {
                noteType = NoteType.Long,
                noteColor = segmentColor,
                noteSpeed = noteSpeed,
                bar = noteData.bar,
                beat = noteData.beat,
                durationBars = noteData.durationBars,
                durationBeats = noteData.durationBeats,
                isClockwise = noteData.isClockwise,
                isSymmetric = isSymmetric,
                gridGenerator = gridGenerator,
                startIndex = index,
            };

            try
            {
                LongNote segment = PoolManager.Instance.Spawn<LongNote>(
                    longNotePrefab.gameObject,
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
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[NoteSpawner] 세그먼트 생성 중 오류 발생: {e.Message}");
            }

            yield return new WaitForSeconds(segmentInterval);
        }

        Debug.Log(
            $"[NoteSpawner] 세그먼트 생성 완료: 총 {pathIndices.Count}개, 대칭={isSymmetric}, 색상={segmentColor}"
        );
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
