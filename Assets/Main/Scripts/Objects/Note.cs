using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Note : MonoBehaviour
{
    [SerializeField, Header("노트 점수")]
    protected float NoteScore = 100;
    protected Vector3 targetPosition;
    protected float speed;
    protected bool isMoving = true;
    protected NoteData noteData;

    public virtual void Initialize(NoteData data)
    {
        noteData = new NoteData()
        {
            noteAxis = data.noteAxis,
            direction = data.direction,
            target = data.target,
            moveSpeed = data.moveSpeed,
        };
    }

    protected virtual void Update()
    {
        if (isMoving)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPosition,
                speed * Time.deltaTime
            );

            if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
            {
                Destroy(gameObject);
            }
        }
    }
}
