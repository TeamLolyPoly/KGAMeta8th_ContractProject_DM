using System;
using System.Linq;
using System.Collections.Generic;
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
    public Vector3 startPosition;
    public Vector3 targetPosition;
    public Vector2 StartCell;
    public Vector2 TargetCell;
    public bool isLeftGrid;
    public float noteSpeed;
    public int bar;
    public int beat;
    public int startIndex;
    public int arcLength;
    public bool isSymmetric;
    public bool isClockwise;
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
#endregion

[CreateAssetMenu(fileName = "AnimData", menuName = "Project_DM/Data/Runtime/AnimData")]
public class AnimData : ScriptableObject
{
    [SerializeField]
    public RuntimeAnimatorController unitAnimator;
    [SerializeField]
    public List<BandAnimationData> bandAnimationDatas = new List<BandAnimationData>();

    private void OnValidate()
    {
        Array dataCount = Enum.GetValues(typeof(Bandtype));

        if (bandAnimationDatas.Count < dataCount.Length)
        {
            foreach (Bandtype type in dataCount)
            {
                BandAnimationData data = new BandAnimationData();
                data.bandtype = type;
                bandAnimationDatas.Add(data);
            }
        }
        for (int i = 0; i < dataCount.Length; i++)
        {
            bandAnimationDatas[i].bandtype = (Bandtype)i;
        }
        while (bandAnimationDatas.Count > dataCount.Length)
        {
            bandAnimationDatas.Remove(bandAnimationDatas.Last());
        }
    }

}
