using UnityEngine;
using UnityEngine.UI;

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
}
