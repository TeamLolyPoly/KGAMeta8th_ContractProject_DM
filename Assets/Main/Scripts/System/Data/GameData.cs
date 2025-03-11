using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using NoteEditor;
using UnityEngine;

[Serializable]
public class NoteData
{
    //기본 노트 타입은 None으로 설정
    public NoteBaseType baseType = NoteBaseType.None;
    public NoteHitType noteType; // Hand(왼쪽) 또는 Red/Blue(오른쪽)
    public NoteDirection direction;
    public NoteAxis noteAxis = NoteAxis.PZ;
    public Vector3 startPosition;
    public Vector3 targetPosition;
    public Vector2 gridpos; // 그리드 x, y 위치
    public bool isLeftGrid; // 왼쪽/오른쪽 그리드 구분
    public float noteSpeed; // 노트 이동 속도
    public int bar; // 박자
    public int beat; // 비트

    //기본 패턴
    public int startIndex;
    public int arcLength;
    public bool isSymmetric;
    public bool isClockwise;

    //원형 그리드 설정
    public float sourceRadius;
    public float targetRadius;
    public float spawnInterval;
}

[Serializable]
public class NoteList
{
    public List<NoteData> patterns = new List<NoteData>();
}

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
    public Sprite albumArt
    {
        get
        {
            if (
                _albumArt == null
                && !string.IsNullOrEmpty(trackName)
                && AudioDataManager.Instance != null
            )
            {
                _albumArt = AudioDataManager.Instance.GetAlbumArt(trackName);
            }
            return _albumArt;
        }
        set { _albumArt = value; }
    }

    [JsonIgnore]
    public AudioClip trackAudio
    {
        get
        {
            if (
                _audioClip == null
                && !string.IsNullOrEmpty(trackName)
                && AudioDataManager.Instance != null
            )
            {
                _audioClip = AudioDataManager.Instance.GetAudioClip(trackName);
            }
            return _audioClip;
        }
        set { _audioClip = value; }
    }
}

[Serializable]
public class BandAnimationData
{
    public Bandtype bandtype;
    public AnimationClip[] animationClip;
}
