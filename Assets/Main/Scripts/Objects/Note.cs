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

    public virtual void Initialize(Vector3 target, float moveSpeed)
    {
        targetPosition = target;
        speed = moveSpeed;
    }

    public virtual void SetNoteData(NoteData data)
    {
        noteData = data;
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
