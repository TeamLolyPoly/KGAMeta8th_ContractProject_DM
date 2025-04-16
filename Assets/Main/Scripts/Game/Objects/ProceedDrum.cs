using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProceedDrum : MonoBehaviour
{
    public void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.TryGetComponent(out NoteInteractor noteInteractor))
        {
            noteInteractor.SendImpulse();
            GameManager.Instance.Single_BackToTitle();
        }
    }
}
