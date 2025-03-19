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

    private void Update()
    {
        if (!isInitialized)
            return;

        // 경과 시간 계산 (음악 시작 기준)
        double elapsedTime = AudioSettings.dspTime - spawnDspTime;

        // 이동 거리 계산
        float totalDistance = Vector3.Distance(startPosition, targetPosition);
        float currentDistance = noteData.noteSpeed * (float)elapsedTime;

        // 진행률 계산 (0~1 사이)
        float progress = Mathf.Clamp01(currentDistance / totalDistance);

        // 노트 위치 업데이트
        transform.position = Vector3.Lerp(startPosition, targetPosition, progress);

        // 목표 위치에 도달했을 때 Miss 처리
        if (progress >= 1.0f && !isHit)
        {
            Miss();
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
            ratings = NoteRatings.Perfect;
        }
        else if (noteDistance * accuracyPoint[1] > hitdis)
        {
            ratings = NoteRatings.Great;
        }
        else
        {
            ratings = NoteRatings.Good;
        }
        scoreSystem.SetScore(Score, ratings);
        Destroy(gameObject);
    }

    private void SetNoteDisTance()
    {
        float sizeY = noteCollider.bounds.size.y;
        Vector3 dis = transform.position;
        dis.y += sizeY / 2;
        noteDistance = Vector3.Distance(transform.position, dis);
    }

    protected override void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.TryGetComponent(out NoteInteractor noteInteractor))
        {
            if (noteInteractor.noteColor == noteData.noteColor)
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

        Debug.DrawRay(transform.position, ExitPoint, Color.red, 0.5f);
        if (other.gameObject.TryGetComponent(out NoteInteractor noteInteractor))
        {
            if (noteInteractor.noteColor == noteData.noteColor)
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
