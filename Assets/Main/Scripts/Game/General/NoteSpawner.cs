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
    private Vector3 secondSourceCenter;
    private Vector3 secondTargetCenter;

    private List<Vector3> sourcePoints = new List<Vector3>();
    private List<Vector3> targetPoints = new List<Vector3>();
    private List<Vector3> secondSourcePoints = new List<Vector3>();
    private List<Vector3> secondTargetPoints = new List<Vector3>();

    private float noteSpeed;
    private double startDspTime;

    private GridGenerator gridGenerator;
    private bool isMultiplayerMode = false;

    private Note shortNotePrefab;
    private LongNote longNotePrefab;
    private GameObject hitFXPrefab;
    private NoteMap noteMap;

    private bool isInitialized = false;
    public bool IsInitialized => isInitialized;

    public void Initialize(GridGenerator gridGen, NoteMap noteMap)
    {
        if (!isInitialized)
        {
            SetPrefabs();
            isInitialized = true;
        }

        SetGrids(gridGen, noteMap);
        GenerateCirclePoints();
        CalculateNoteSpeed();
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
        isMultiplayerMode = gridGenerator.IsMultiplayerMode;

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

        if (isMultiplayerMode)
        {
            Vector3 secondSourceGridCenter = gridGenerator.GetHandGridCenter(
                gridGenerator.secondSourceOrigin,
                false
            );
            Vector3 secondTargetGridCenter = gridGenerator.GetHandGridCenter(
                gridGenerator.secondTargetOrigin,
                false
            );

            secondSourceCenter = secondSourceGridCenter + circleOffset;
            secondTargetCenter = secondTargetGridCenter + circleOffset;
        }
    }

    private void GenerateCirclePoints()
    {
        sourcePoints.Clear();
        targetPoints.Clear();
        secondSourcePoints.Clear();
        secondTargetPoints.Clear();

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

            if (isMultiplayerMode)
            {
                secondSourcePoints.Add(
                    new Vector3(
                        secondSourceCenter.x + sourceRadius * x,
                        secondSourceCenter.y + sourceRadius * y,
                        secondSourceCenter.z
                    )
                );

                secondTargetPoints.Add(
                    new Vector3(
                        secondTargetCenter.x + targetRadius * x,
                        secondTargetCenter.y + targetRadius * y,
                        secondTargetCenter.z
                    )
                );
            }
        }
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

        int count = sortedNotes.Count;

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
                if (isMultiplayerMode)
                {
                    SpawnShortNote(noteData, true);
                }
                break;
            case NoteType.Long:
                SpawnLongNote(noteData);
                if (isMultiplayerMode)
                {
                    SpawnLongNote(noteData, true);
                }
                break;
            default:
                Debug.LogError($"[NoteSpawner] 지원하지 않는 노트 타입: {noteData.noteType}");
                break;
        }
    }

    private void SpawnShortNote(NoteData noteData, bool isSecondGrid = false)
    {
        if (noteData.TargetCell == Vector2.zero)
        {
            noteData.TargetCell = noteData.StartCell;
        }

        NoteData clonedData = new NoteData();
        clonedData.noteType = noteData.noteType;
        clonedData.StartCell = noteData.StartCell;
        clonedData.TargetCell = noteData.TargetCell;
        clonedData.noteColor = noteData.noteColor;
        clonedData.noteSpeed = noteData.noteSpeed;
        clonedData.bar = noteData.bar;
        clonedData.beat = noteData.beat;
        clonedData.gridGenerator = noteData.gridGenerator;
        clonedData.direction = noteData.direction;
        clonedData.noteAxis = noteData.noteAxis;

        if (isSecondGrid)
        {
            clonedData.useSecondGrid = true;
            if (PhotonNetwork.IsMasterClient)
            {
                clonedData.isInteractable = false;
                noteData.isInteractable = true;
            }
            else
            {
                clonedData.isInteractable = true;
                noteData.isInteractable = false;
            }
        }

        ShortNote noteObj = PoolManager.Instance.Spawn<ShortNote>(
            shortNotePrefab.gameObject,
            isSecondGrid ? clonedData.GetStartPosition() : noteData.GetStartPosition(),
            Quaternion.identity
        );

        if (noteObj != null)
        {
            noteObj.Initialize(isSecondGrid ? clonedData : noteData);
            noteObj.SetNoteColor(noteData.noteColor);

            if (hitFXPrefab != null)
            {
                noteObj.SetHitFX(hitFXPrefab);
            }
        }
    }

    private void SpawnLongNote(NoteData noteData, bool isSecondGrid = false)
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

        if (isSecondGrid)
        {
            StartCoroutine(
                SpawnSegments(
                    noteData,
                    startIndex,
                    endIndex,
                    false,
                    arcLength,
                    totalDurationSeconds,
                    true
                )
            );
        }
        else
        {
            StartCoroutine(
                SpawnSegments(
                    noteData,
                    startIndex,
                    endIndex,
                    false,
                    arcLength,
                    totalDurationSeconds
                )
            );
        }

        if (noteData.isSymmetric)
        {
            int symmetricStartIndex = (startIndex + segmentCount / 2) % segmentCount;
            int symmetricEndIndex = (endIndex + segmentCount / 2) % segmentCount;

            if (isSecondGrid)
            {
                StartCoroutine(
                    SpawnSegments(
                        noteData,
                        symmetricStartIndex,
                        symmetricEndIndex,
                        true,
                        arcLength,
                        totalDurationSeconds,
                        true
                    )
                );
            }
            else
            {
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
    }

    private IEnumerator SpawnSegments(
        NoteData noteData,
        int startIndex,
        int endIndex,
        bool isSymmetric,
        int arcLength,
        float totalDurationSeconds,
        bool isSecondGrid = false
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
            Vector3 sourcePos,
                targetPos;

            NoteData segmentData;

            if (isSecondGrid)
            {
                sourcePos = secondSourcePoints[index];
                targetPos = secondTargetPoints[index];
                segmentData = new NoteData
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
                if (isMultiplayerMode)
                {
                    if (PhotonNetwork.IsMasterClient)
                    {
                        segmentData.isInteractable = false;
                    }
                    else
                    {
                        segmentData.isInteractable = true;
                    }
                }
            }
            else
            {
                sourcePos = sourcePoints[index];
                targetPos = targetPoints[index];
                segmentData = new NoteData
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
                if (isMultiplayerMode)
                {
                    if (PhotonNetwork.IsMasterClient)
                    {
                        segmentData.isInteractable = true;
                    }
                    else
                    {
                        segmentData.isInteractable = false;
                    }
                }
            }

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
            catch (Exception e)
            {
                Debug.LogError($"[NoteSpawner] 세그먼트 생성 중 오류 발생: {e.Message}");
            }

            yield return new WaitForSeconds(segmentInterval);
        }
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying)
            return;

        Gizmos.color = Color.green;
        DrawCircle(sourceCenter, sourceRadius);

        Gizmos.color = Color.red;
        DrawCircle(targetCenter, targetRadius);

        if (isMultiplayerMode)
        {
            Gizmos.color = Color.green;
            DrawCircle(secondSourceCenter, sourceRadius);

            Gizmos.color = Color.red;
            DrawCircle(secondTargetCenter, targetRadius);
        }

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

            if (isMultiplayerMode && secondSourcePoints != null && secondTargetPoints != null)
            {
                Gizmos.color = Color.yellow;
                foreach (Vector3 point in secondSourcePoints)
                {
                    Gizmos.DrawSphere(point, 0.1f);
                }

                Gizmos.color = Color.cyan;
                foreach (Vector3 point in secondTargetPoints)
                {
                    Gizmos.DrawSphere(point, 0.1f);
                }
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
