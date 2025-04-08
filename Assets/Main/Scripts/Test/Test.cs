using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

public class Test : MonoBehaviour
{
    private NoteMap noteMap;

    [SerializeField]
    private AudioClip audioClip;

    void Start()
    {
        string json = Resources.Load<TextAsset>("Json/TestMap").text;
        noteMap = JsonConvert.DeserializeObject<NoteMap>(json);
        GameManager.Instance.TestStage(audioClip, noteMap);
    }
}
