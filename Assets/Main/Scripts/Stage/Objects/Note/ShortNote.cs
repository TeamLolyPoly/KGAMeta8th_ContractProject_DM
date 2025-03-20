using UnityEngine;

public class ShortNote : Note
{
    [SerializeField, Header("블럭 타격 오차범위")]
    protected float directionalRange = 50f;

    [SerializeField, Header("타격 정확도 허용범위")]
    protected float[] accuracyPoint = { 0.34f, 0.67f };

    [SerializeField, Header("노트 플레이어 방향 기울기")]
    protected float noteLookAtAngle = 40f;
    public Renderer topRenderer;
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

        noteCollider = GetComponent<Collider>();

        if (noteCollider != null)
        {
            SetNoteDisTance();
        }

        NoteDirectionChange();
        NoteHitDirectionChange();
        noteDownDirection = -transform.up;
        noteUpDirection = transform.up;
    }

    public override void SetNoteColor(NoteColor color)
    {
        noteData.noteColor = color;

        if (Rim == null)
        {
            Debug.LogWarning($"노트의 Rim이 null입니다. 노트 타입: {noteData?.noteType}");
            return;
        }

        Renderer rimRenderer = Rim.GetComponent<Renderer>();
        if (rimRenderer == null)
        {
            Debug.LogWarning("Rim에 Renderer 컴포넌트가 없습니다.");
            return;
        }

        switch (color)
        {
            case NoteColor.Red:
                rimRenderer.material.color = Color.red;
                topRenderer.material.color = Color.red;
                break;
            case NoteColor.Blue:
                rimRenderer.material.color = Color.blue;
                topRenderer.material.color = Color.blue;
                break;
            case NoteColor.Yellow:
                rimRenderer.material.color = Color.yellow;
                topRenderer.material.color = Color.yellow;
                break;
            default:
                rimRenderer.material.color = Color.white;
                topRenderer.material.color = Color.white;
                break;
        }
    }

    private void Update()
    {
        if (!isInitialized)
            return;

        double elapsedTime = AudioSettings.dspTime - spawnDspTime;

        float totalDistance = Vector3.Distance(startPosition, targetPosition);
        float currentDistance = noteData.noteSpeed * (float)elapsedTime;

        float progress = Mathf.Clamp01(currentDistance / totalDistance);

        transform.position = Vector3.Lerp(startPosition, targetPosition, progress);

        if (progress >= 1f && !isHit)
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

        transform.rotation = Quaternion.Euler(rotationX, rotationY, rotationZ);
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
        transform.rotation = Quaternion.Euler(
            transform.rotation.eulerAngles.x,
            transform.rotation.eulerAngles.y + rotationY,
            transform.rotation.eulerAngles.z
        );
    }

    protected void HitScore(float hitdis)
    {
        NoteRatings ratings;
        int score = noteScore;
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
        scoreSystem.SetScore(score, ratings);
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
            }
        }
    }

    private void OnCollisionExit(Collision other)
    {
        if (other.gameObject.TryGetComponent(out NoteInteractor noteInteractor))
        {
            if (noteInteractor.noteColor == noteData.noteColor)
            {
                Vector3 ExitPoint = (transform.position - other.transform.position).normalized;
                ExitAngle = Vector3.Angle(ExitPoint, noteUpDirection);

                Debug.DrawRay(transform.position, ExitPoint, Color.red, 0.5f);

                if (EnterAngle <= directionalRange && ExitAngle <= directionalRange)
                {
                    HitScore(hitdis);
                }
                else
                {
                    print("이상한 방향을 타격함");
                    Miss();
                }
            }
            else
            {
                print("HitObject 타입이 다름");
                Miss();
            }
        }
    }

    private float HitPoint(Collision other)
    {
        Vector3 hitPoint = other.GetContact(0).point;
        Vector3 notePos = transform.position - (-transform.up * noteDistance);
        return Vector3.Distance(hitPoint, notePos);
    }
}
