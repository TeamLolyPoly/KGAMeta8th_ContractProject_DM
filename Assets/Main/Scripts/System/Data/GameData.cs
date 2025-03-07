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

public class TrackData
{
    public string trackName;
    public Sprite albumArt;
    public AudioClip trackAudio;
}

// 트랙 메타데이터를 저장하기 위한 클래스
[Serializable]
public class TrackMetadata
{
    public string trackName;
    public string albumArtPath;
    public string artistName;
    public string albumName;
    public int year;
    public string genre;
    public float duration;
    public string filePath;
}

// 메타데이터 리스트를 JSON으로 직렬화하기 위한 래퍼 클래스
[Serializable]
public class TrackMetadataList
{
    public List<TrackMetadata> tracks;
}
