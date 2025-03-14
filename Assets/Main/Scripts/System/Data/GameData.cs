using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

#region Runtime
[Serializable]
public class NoteData
{
    public NoteBaseType baseType = NoteBaseType.None;
    public NoteHitType noteType;
    public NoteDirection direction;
    public NoteAxis noteAxis = NoteAxis.PZ;
    public Vector2 StartCell;
    public Vector2 TargetCell;
    public bool isLeftGrid;
    public float noteSpeed;
    public int bar;
    public int beat;
    public int startIndex;
    public bool isSymmetric;
    public bool isClockwise;

    // 롱노트 지속 시간 관련 필드
    public int durationBars; // 롱노트가 지속되는 마디 수
    public int durationBeats; // 롱노트가 지속되는 박자 수

    [NonSerialized]
    public GridGenerator gridGenerator;

    public Vector3 GetStartPosition()
    {
        if (gridGenerator == null)
            return Vector3.zero;

        return gridGenerator.GetCellPosition(
            gridGenerator.sourceOrigin,
            (int)StartCell.x,
            (int)StartCell.y
        );
    }

    public Vector3 GetTargetPosition()
    {
        if (gridGenerator == null)
            return Vector3.zero;

        return gridGenerator.GetCellPosition(
            gridGenerator.targetOrigin,
            (int)TargetCell.x,
            (int)TargetCell.y
        );
    }

    public int CalculateArcLength(int segmentCount, float bpm, int beatsPerBar, float spawnInterval)
    {
        if (baseType != NoteBaseType.Long)
            return 0;

        float secondsPerBeat = 60f / bpm;
        float totalDurationSeconds = (durationBars * beatsPerBar + durationBeats) * secondsPerBeat;

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
}
#endregion

#region NoteEditor
[Serializable]
public class TrackData
{
    public string trackName;
    public string artistName;
    public string albumName;
    public int year;
    public string genre;
    public float duration;
    public float bpm = 120f;

    [JsonIgnore]
    private Sprite _albumArt;

    [JsonIgnore]
    private AudioClip _audioClip;

    [JsonIgnore]
    public Sprite AlbumArt
    {
        get { return _albumArt; }
        set { _albumArt = value; }
    }

    [JsonIgnore]
    public AudioClip TrackAudio
    {
        get { return _audioClip; }
        set { _audioClip = value; }
    }
}

[Serializable]
public class BandAnimationData
{
    public Bandtype bandtype;
    public AnimationClip[] animationClip;
}

[Serializable]
public class MultiplierScore
{
    public NoteRatings ratings;
    public int ratingScore = 0;
}
#endregion
