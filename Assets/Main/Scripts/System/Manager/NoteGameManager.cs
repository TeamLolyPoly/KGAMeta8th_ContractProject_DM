using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoteGameManager : Singleton<NoteGameManager>
{
    public float currentScore { get; private set; } = 0;
    public int combo { get; set; } = 0;

    public SetScore(float score)
    {

    }
}
