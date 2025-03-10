using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class NoteData
{
    //기본 노트 타입은 숏으로 설정
    public NoteBaseType baseType = NoteBaseType.Short;
    public NoteHitType noteType;
    public NoteDirection direction;
    public NoteAxis noteAxis;
    public Vector3 target;
    public float moveSpeed;
}

[Serializable]
public class ArcNoteData
{
    //기본 패턴
    public int startIndex;
    public int arcLength;
    public bool isSymmetric;
    public bool isClockwise;

    //원형 그리드 설정
    public float sourceRadius;
    public float targetRadius;
    public float moveSpeed;
    public float spawnInterval;

    //노트 타입
    public NoteHitType noteType;
}

[Serializable]
public class ArcNoteList
{
    public List<ArcNoteData> patterns = new List<ArcNoteData>();
}

[Serializable]
public class GridNoteData
{
    // 기본 정보
    public bool isLeftGrid; // 왼쪽/오른쪽 그리드 구분
    public int gridX; // 그리드 X 위치 (0-2: 왼쪽, 2-5: 오른쪽)
    public int gridY; // 그리드 Y 위치

    // 노트 설정
    public NoteBaseType baseType = NoteBaseType.Short;
    public NoteHitType noteType; // Hand(왼쪽) 또는 Red/Blue(오른쪽)
    public NoteDirection direction;
    public NoteAxis noteAxis;
    public float moveSpeed;
}

[Serializable]
public class GridNoteList
{
    public List<GridNoteData> patterns = new List<GridNoteData>();
}

public class TrackData
{
    public string trackName;
    public Sprite albumArt;
    public AudioClip trackAudio;
    public float bpm = 120f;
}

[Serializable]
public class TrackMetadata
{
    public string trackName;
    public string artistName;
    public string albumName;
    public int year;
    public string genre;
    public float duration;
    public float bpm = 120f;
}

[Serializable]
public class TrackMetadataList
{
    public List<TrackMetadata> tracks;
}
