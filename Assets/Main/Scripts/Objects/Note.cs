using UnityEngine;

public class Note : MonoBehaviour
{
    [SerializeField, Header("블럭 타격 오차범위")]
    protected float directionalRange = 10f;

    [SerializeField, Header("노트 점수")]
    protected int NoteScore = 100;

    [SerializeField, Header("타격 정확도 허용범위")]
    protected float[] accuracyPoint = { 0.34f, 0.67f };

    [SerializeField, Header("노트 정확도 점수배율")]
    protected float[] accuracyScore = { 0.8f, 0.5f };
    protected bool isMoving = true;
    protected NoteData noteData;
    protected Transform noteTrans;
    protected Vector3 hitDirection;
    protected float noteDistance;
    private Renderer noteRenderer;

    public virtual void Initialize(NoteData data)
    {
        noteTrans = GetComponent<Transform>();
        noteRenderer = GetComponent<Renderer>();
        noteData = new NoteData()
        {
            noteAxis = data.noteAxis,
            direction = data.direction,
            target = data.target,
            moveSpeed = data.moveSpeed,
            noteType = data.noteType,
        };
        if (noteRenderer != null)
        {
            SetNoteDisTance();

            switch (noteData.noteType)
            {
                case NoteHitType.Red:
                    noteRenderer.material.color = Color.red;
                    break;
                case NoteHitType.Blue:
                    noteRenderer.material.color = Color.blue;
                    break;
            }
        }
        NoteDirectionChange();
        NoteHitDirectionChange();
    }

    protected virtual void Update()
    {
        if (isMoving)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                noteData.target,
                noteData.moveSpeed * Time.deltaTime
            );

            if (Vector3.Distance(transform.position, noteData.target) < 0.1f)
            {
                Miss();
                Destroy(gameObject);
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.black;
        Gizmos.DrawLine(transform.localPosition, transform.position - -transform.up - hitDirection);
    }

    protected void NoteDirectionChange()
    {
        float rotationZ = 0f;
        switch (noteData.direction)
        {
            case NoteDirection.East:
                rotationZ = 90f;
                hitDirection = SetHitDirection(new Vector3(1, 0, 0));
                break;
            case NoteDirection.West:
                rotationZ = -90f;
                hitDirection = SetHitDirection(new Vector3(-1, 0, 0));
                break;
            case NoteDirection.South:
                rotationZ = 0f;
                hitDirection = SetHitDirection(new Vector3(0, -1, 0));
                break;
            case NoteDirection.North:
                rotationZ = 180f;
                hitDirection = SetHitDirection(new Vector3(0, 1, 0));
                break;
            case NoteDirection.Northeast:
                rotationZ = 135f;
                hitDirection = SetHitDirection(new Vector3(1, 1, 0).normalized);
                break;
            case NoteDirection.Northwest:
                rotationZ = -135f;
                hitDirection = SetHitDirection(new Vector3(-1, 1, 0).normalized);
                break;
            case NoteDirection.Southeast:
                rotationZ = 45f;
                hitDirection = SetHitDirection(new Vector3(1, -1, 0).normalized);
                break;
            case NoteDirection.Southwest:
                rotationZ = -45f;
                hitDirection = SetHitDirection(new Vector3(-1, -1, 0).normalized);
                break;
        }
        noteTrans.rotation = Quaternion.Euler(
            noteTrans.rotation.eulerAngles.x,
            noteTrans.rotation.eulerAngles.y,
            rotationZ
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
            rotationY,
            noteTrans.rotation.eulerAngles.z
        );
    }

    protected Vector3 SetHitDirection(Vector3 dir)
    {
        switch (noteData.noteAxis)
        {
            case NoteAxis.PZ:
                return dir;
            case NoteAxis.MZ:
                return -dir;
            case NoteAxis.PX:
                return new Vector3(dir.z, dir.y, -dir.x);
            case NoteAxis.MX:
                return new Vector3(dir.z, dir.y, dir.x);
            default:
                return dir;
        }
    }

    //TODO: dis 널널하게
    //hit위치에서 중앙까지의 거리를 비교후 점수 계산
    protected void HitScore(float hitdis)
    {
        NoteRatings ratings;
        int Score = NoteScore;
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
        NoteGameManager.Instance.SetScore(Score, ratings);
        Destroy(this.gameObject);
    }

    protected void Miss()
    {
        NoteGameManager.Instance.SetScore(0, NoteRatings.Miss);
        Destroy(this.gameObject);
    }

    //노트가 허용하는 Hit거리를 구함
    private void SetNoteDisTance()
    {
        float sizeY = noteRenderer.bounds.size.y;
        print($"Y: {sizeY}");
        Vector3 dis = transform.position;
        dis.y += sizeY / 2;
        noteDistance = Vector3.Distance(transform.position, dis);
        print($"노트 길이: {noteDistance}");
    }

    //TODO: 판정 방식 수정 HitScore수정 해야함
    private void OnCollisionEnter(Collision other)
    {
        float hitdis = HitPoint(other);
        Vector3 hitPoint = other.contacts[0].normal;
        Debug.DrawRay(transform.position, hitPoint, Color.blue, 0.5f);
        float range = Vector3.Angle(hitPoint, hitDirection);
        print($"법선벡터 X: {hitPoint.x} Y : {hitPoint.y} Z : {hitPoint.z}");
        print(
            $"내 벡터 X: {hitDirection.x} Y : {hitDirection.y} Z : {hitDirection.z} range: {range}"
        );
        if (other.gameObject.TryGetComponent(out HitObject hitObject))
        {
            if (hitObject.hitObjectType == noteData.noteType)
            {
                if (range <= directionalRange)
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

    //맞은 기준으로 노트의 중앙에서 부터의 거리를 구함
    private float HitPoint(Collision other)
    {
        Vector3 hitPoint = other.GetContact(0).point;
        Vector3 notePos = transform.position - (-transform.up * noteDistance);
        print($"notepos {notePos} hitPoint{hitPoint}");
        return Vector3.Distance(hitPoint, notePos);
    }
}
