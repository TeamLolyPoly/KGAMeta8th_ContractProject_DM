using UnityEngine;

public class ShortNote : Note
{
    [SerializeField, Header("블럭 타격 오차범위")]
    protected float directionalRange = 50f;

    [SerializeField, Header("타격 정확도 허용범위")]
    protected float[] accuracyPoint = { 0.34f, 0.67f };

    [SerializeField, Header("노트 플레이어 방향 기울기")]
    protected float noteLookAtAngle = 40f;
    protected Transform noteTrans;
    protected Vector3 noteDownDirection;
    protected Vector3 noteUpDirection;
    protected float noteDistance;
    private float EnterAngle;
    private float ExitAngle;
    private float hitdis;

    private Collider noteCollider;
    protected bool isMoving = true;

    public override void Initialize(NoteData data)
    {
        base.Initialize(data);

        noteTrans = transform;
        noteCollider = GetComponent<Collider>();

        if (noteCollider != null)
        {
            SetNoteDisTance();
        }

        NoteDirectionChange();
        NoteHitDirectionChange();
        noteDownDirection = -transform.up;
        noteUpDirection = transform.up;
        print($"Down: {noteDownDirection} Up: {noteUpDirection}");
    }

    protected virtual void Update()
    {
        if (isMoving && isInitialized)
        {
            double elapsedTime = AudioSettings.dspTime - spawnDspTime;
            float totalDistance = Vector3.Distance(startPosition, targetPosition);
            float currentDistance = noteData.noteSpeed * (float)elapsedTime;
            float progress = Mathf.Clamp01(currentDistance / totalDistance);

            transform.position = Vector3.Lerp(startPosition, targetPosition, progress);

            if (progress >= 1f)
            {
                if (GameManager.Instance != null)
                {
                    scoreSystem.SetScore(0, NoteRatings.Miss);
                }
                Destroy(gameObject);
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.localPosition, transform.position + noteDownDirection);
        Gizmos.color = Color.black;
        Gizmos.DrawLine(transform.localPosition, transform.position + noteUpDirection);
    }

    protected void NoteDirectionChange()
    {
        float rotationZ = 0f;
        float rotationY = 0f;
        float rotationX = 0f;
        switch (noteData.direction)
        {
            case NoteDirection.East:
                rotationZ = 90f;
                rotationY = -noteLookAtAngle;
                break;
            case NoteDirection.West:
                rotationZ = -90f;
                rotationY = noteLookAtAngle;
                break;
            case NoteDirection.South:
                rotationZ = 0f;
                rotationX = -noteLookAtAngle;
                break;
            case NoteDirection.North:
                rotationZ = 180f;
                rotationX = noteLookAtAngle;
                break;
            case NoteDirection.Northeast:
                rotationZ = 135f;
                rotationX = noteLookAtAngle;
                break;
            case NoteDirection.Northwest:
                rotationZ = -135f;
                rotationX = noteLookAtAngle;
                break;
            case NoteDirection.Southeast:
                rotationZ = 45f;
                rotationX = -noteLookAtAngle;
                break;
            case NoteDirection.Southwest:
                rotationZ = -45f;
                rotationX = -noteLookAtAngle;
                break;
        }
        noteTrans.rotation = Quaternion.Euler(
            noteTrans.rotation.eulerAngles.x + rotationX,
            noteTrans.rotation.eulerAngles.y + rotationY,
            noteTrans.rotation.eulerAngles.z + rotationZ
        );
    }

    protected void NoteHitDirectionChange()
    {
        float rotationY = 0f;
        switch (noteData.noteAxis)
        {
            case NoteAxis.PZ:
                rotationY = 0f;
                break;
            case NoteAxis.MZ:
                rotationY = 180f;
                break;
            case NoteAxis.PX:
                rotationY = 90f;
                break;
            case NoteAxis.MX:
                rotationY = -90f;
                break;
        }
        noteTrans.rotation = Quaternion.Euler(
            noteTrans.rotation.eulerAngles.x,
            noteTrans.rotation.eulerAngles.y + rotationY,
            noteTrans.rotation.eulerAngles.z
        );
    }

    protected void HitScore(float hitdis)
    {
        NoteRatings ratings;
        int Score = noteScore;
        if (noteDistance * accuracyPoint[0] >= hitdis)
        {
            print($"Perfect noteDis :{noteDistance * accuracyPoint[0]} , Hitdis: {hitdis}");
            ratings = NoteRatings.Perfect;
        }
        else if (noteDistance * accuracyPoint[1] > hitdis)
        {
            print($"Great noteDis :{noteDistance * accuracyPoint[1]} , Hitdis: {hitdis}");
            ratings = NoteRatings.Great;
        }
        else
        {
            print($"Good noteDis :{noteDistance} , Hitdis: {hitdis}");
            ratings = NoteRatings.Good;
        }
        scoreSystem.SetScore(Score, ratings);
        Destroy(gameObject);
    }

    private void SetNoteDisTance()
    {
        float sizeY = noteCollider.bounds.size.y;
        print($"Y: {sizeY}");
        Vector3 dis = transform.position;
        dis.y += sizeY / 2;
        noteDistance = Vector3.Distance(transform.position, dis);
        print($"노트 길이: {noteDistance}");
    }

    protected override void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.TryGetComponent(out NoteInteractor noteInteractor))
        {
            if (noteInteractor.noteType == noteData.noteType)
            {
                Vector3 hitPoint = other.contacts[0].normal;
                hitdis = HitPoint(other);
                EnterAngle = Vector3.Angle(hitPoint, noteDownDirection);
                Debug.DrawRay(transform.position, hitPoint, Color.blue, 0.5f);

                Instantiate(hitFX, transform.position, Quaternion.identity);

                print($"Enter 법선벡터 X: {hitPoint.x} Y : {hitPoint.y} Z : {hitPoint.z}");
                print(
                    $"Enter 내 벡터 X: {noteDownDirection.x} Y : {noteDownDirection.y} Z : {noteDownDirection.z} EnterAngle: {EnterAngle}"
                );
            }
        }
    }

    private void OnCollisionExit(Collision other)
    {
        Vector3 ExitPoint = (transform.position - other.transform.position).normalized;
        ExitAngle = Vector3.Angle(ExitPoint, noteUpDirection);

        print($"Exit 법선벡터 X: {ExitPoint.x} Y : {ExitPoint.y} Z : {ExitPoint.z}");
        print(
            $"Exit 내 벡터 X:{noteUpDirection.x} Y : {noteUpDirection.y} Z : {noteUpDirection.z} ExitAngle: {ExitAngle}"
        );
        Debug.DrawRay(transform.position, ExitPoint, Color.red, 0.5f);
        if (other.gameObject.TryGetComponent(out NoteInteractor noteInteractor))
        {
            if (noteInteractor.noteType == noteData.noteType)
            {
                if (EnterAngle <= directionalRange && ExitAngle <= directionalRange)
                {
                    HitScore(hitdis);
                }
                else
                {
                    Miss();
                    print("이상한 방향을 타격함");
                }
            }
            else
            {
                Miss();
                print("HitObject 타입이 다름");
            }
            Miss();
        }
    }

    private float HitPoint(Collision other)
    {
        Vector3 hitPoint = other.GetContact(0).point;
        Vector3 notePos = transform.position - (-transform.up * noteDistance);
        print($"notepos {notePos} hitPoint{hitPoint}");
        return Vector3.Distance(hitPoint, notePos);
    }
}
