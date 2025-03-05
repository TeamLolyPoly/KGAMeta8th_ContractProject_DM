using System;
using System.Collections.Generic;
using UnityEngine;

public class NoteData
{
    public NoteHitType noteType;
    public NoteDirection direction;
    public NoteAxis noteAxis;
    public Vector3 target;
    public float moveSpeed;
}

public class TrackData
{
    public string trackName;
    public Sprite albumArt;
    public AudioClip trackAudio;
    public string filePath;
    public string albumArtPath;
    public float bpm = 120f;
}

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
    public float bpm = 120f;
}

[Serializable]
public class TrackMetadataList
{
    public List<TrackMetadata> tracks;
}
