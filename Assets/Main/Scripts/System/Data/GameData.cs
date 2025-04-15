using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

#region Runtime
[Serializable]
public class NoteData
{
    public NoteType noteType = NoteType.None;
    public NoteColor noteColor;
    public NoteDirection direction;
    public NoteAxis noteAxis = NoteAxis.PZ;

    [JsonIgnore]
    public Vector2Int StartCell = Vector2Int.zero;

    [JsonIgnore]
    public Vector2Int TargetCell = Vector2Int.zero;

    public int StartCellX
    {
        get { return StartCell.x; }
        set { StartCell = new Vector2Int(value, StartCell.y); }
    }

    public int StartCellY
    {
        get { return StartCell.y; }
        set { StartCell = new Vector2Int(StartCell.x, value); }
    }

    public int TargetCellX
    {
        get { return TargetCell.x; }
        set { TargetCell = new Vector2Int(value, TargetCell.y); }
    }

    public int TargetCellY
    {
        get { return TargetCell.y; }
        set { TargetCell = new Vector2Int(TargetCell.x, value); }
    }

    public int bar;
    public int beat;
    public int startIndex;
    public int endIndex;
    public bool isSymmetric;
    public bool isClockwise;

    public int durationBars;
    public int durationBeats;

    [NonSerialized]
    public float noteSpeed;

    [NonSerialized]
    public GridGenerator gridGenerator;

    [NonSerialized]
    public bool useSecondGrid = false;

    [NonSerialized]
    public bool isInteractable = true;

    public Vector3 GetStartPosition()
    {
        if (gridGenerator == null)
            return Vector3.zero;

        Transform sourceOrigin = useSecondGrid
            ? gridGenerator.secondSourceOrigin
            : gridGenerator.sourceOrigin;

        return gridGenerator.GetCellPosition(sourceOrigin, (int)StartCell.x, (int)StartCell.y);
    }

    public Vector3 GetTargetPosition()
    {
        if (gridGenerator == null)
            return Vector3.zero;

        Transform targetOrigin = useSecondGrid
            ? gridGenerator.secondTargetOrigin
            : gridGenerator.targetOrigin;

        return gridGenerator.GetCellPosition(targetOrigin, (int)TargetCell.x, (int)TargetCell.y);
    }

    public int CalculateArcLength(int segmentCount, float bpm, int beatsPerBar, float spawnInterval)
    {
        if (noteType != NoteType.Long)
            return 0;

        float secondsPerBeat = 60f / bpm;

        float totalDurationSeconds = (durationBars * beatsPerBar + durationBeats) * secondsPerBeat;

        if (totalDurationSeconds <= 0)
        {
            Debug.LogWarning(
                $"롱노트 지속 시간이 0 또는 음수입니다: {durationBars}마디 {durationBeats}비트"
            );
            return 1;
        }

        int calculatedArcLength = Mathf.RoundToInt(totalDurationSeconds / spawnInterval);

        calculatedArcLength = Mathf.Max(1, calculatedArcLength);
        calculatedArcLength = Mathf.Min(calculatedArcLength, segmentCount - 1);

        return calculatedArcLength;
    }
}

[Serializable]
public class NoteMap
{
    public List<NoteData> notes = new List<NoteData>();
    public float bpm = 120f;
    public int beatsPerBar = 4;

    [JsonIgnore]
    public int TotalNoteCount
    {
        get
        {
            int count = 0;
            foreach (var note in notes)
            {
                if (note.noteType == NoteType.Long)
                {
                    float secondsPerBeat = 60f / bpm;
                    float totalDurationSeconds =
                        (note.durationBars * beatsPerBar + note.durationBeats) * secondsPerBeat;
                    float spawnInterval = 0.1f;
                    int segmentCount = 72;

                    int calculatedArcLength = Mathf.RoundToInt(
                        totalDurationSeconds / spawnInterval
                    );
                    calculatedArcLength = Mathf.Max(1, calculatedArcLength);
                    calculatedArcLength = Mathf.Min(calculatedArcLength, segmentCount - 1);

                    count += calculatedArcLength;
                }
                else
                {
                    count++;
                }
            }
            return count;
        }
    }
}

[Serializable]
public class NoteMapData
{
    public Difficulty difficulty;
    public NoteMap noteMap;
}

[Serializable]
public class JsonNoteData
{
    public int noteType;
    public int noteColor;
    public int direction;
    public int noteAxis;
    public int bar;
    public int beat;
    public int startIndex;
    public int endIndex;
    public bool isSymmetric;
    public bool isClockwise;
    public int durationBars;
    public int durationBeats;
    public int StartCellX;
    public int StartCellY;
    public int TargetCellX;
    public int TargetCellY;
}

[Serializable]
public class JsonNoteMap
{
    public List<JsonNoteData> notes;
    public float bpm;
    public int beatsPerBar;
}

[Serializable]
public class TrackDataList
{
    public List<TrackData> tracks;
}

[Serializable]
public class RankData
{
    public string nickName;
    public Sprite profileImage;
    public int score;
    public int rank;
}

[Serializable]
public class NoteMapDataList
{
    public List<JsonNoteMapWrapper> noteMaps;
}

[Serializable]
public class JsonNoteMapWrapper
{
    public int difficulty;
    public JsonNoteMapContent noteMap;
}

[Serializable]
public class JsonNoteMapContent
{
    public List<JsonNoteData> notes;
    public float bpm;
    public int beatsPerBar;
}

#endregion

#region NoteEditor
[Serializable]
public class TrackData
{
    public Guid id;
    public string trackName;
    public string artistName;
    public string albumName;
    public int year;
    public string genre;
    public float duration;
    public float bpm = 120f;

    [JsonIgnore]
    public List<NoteMapData> noteMapData = new List<NoteMapData>();

    [JsonIgnore]
    public Sprite AlbumArt { get; set; }

    [JsonIgnore]
    public AudioClip TrackAudio { get; set; }
}

[Serializable]
public class BandAnimationData
{
    public BandType bandType;

    [Header("밴드 walk 애니메이션")]
    public AnimationClip MoveClip;
    public AnimationClip[] animationClip;
}

[Serializable]
public class SpectatorAnimData
{
    [Header("관객 디폴트 애니메이션")]
    public List<AnimationClip> RandomAnima = new List<AnimationClip>();

    [Header("관객 호응도 애니메이션")]
    public List<AnimationClip> engagementClip = new List<AnimationClip>();
}

[Serializable]
public class SpectatorEventThreshold
{
    public Engagement engagement;

    [Range(0f, 1f), Header("노트 퍼센트")]
    public float noteThreshold;

    [Header("기준 콤보 횟수")]
    public int comboThreshold;
}

[Serializable]
public class MultiplierScore
{
    public NoteRatings ratings;
    public int ratingScore = 0;
}
#endregion
[Serializable]
public class VrMap
{
    public bool isTransformPoint = true;
    public Transform vrTarget;
    public Transform ikTarget;
    public Vector3 trackingPositionOffset;
    public Vector3 trackingRotationOffset;

    public void Map()
    {
        if (isTransformPoint)
        {
            ikTarget.position = vrTarget.TransformPoint(trackingPositionOffset);
        }
        else
        {
            ikTarget.position = vrTarget.position + trackingPositionOffset;
        }
        if (Mathf.Abs(vrTarget.rotation.x) < 89f)
            ikTarget.rotation = vrTarget.rotation * Quaternion.Euler(trackingRotationOffset);
    }
}

public class ScoreData
{
    public int Score = 0;
    public int HighCombo = 0;
    public int NoteHitCount = 0;
    public int totalNoteCount = 0;
    public Dictionary<NoteRatings, int> RatingCount = new Dictionary<NoteRatings, int>();

    public ScoreData(
        int totalScore,
        int highCombo,
        int noteHitCount,
        int totalNoteCount,
        int missCount,
        int goodCount,
        int greatCount,
        int perfectCount
    )
    {
        this.Score = totalScore;
        this.HighCombo = highCombo;
        this.NoteHitCount = noteHitCount;
        this.totalNoteCount = totalNoteCount;
        RatingCount.Add(NoteRatings.Miss, missCount);
        RatingCount.Add(NoteRatings.Good, goodCount);
        RatingCount.Add(NoteRatings.Great, greatCount);
        RatingCount.Add(NoteRatings.Perfect, perfectCount);
    }
}
