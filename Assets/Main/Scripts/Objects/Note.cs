using System.Collections.Generic;
using ExitGames.Client.Photon.StructWrapping;
using UnityEngine;

public class Note : MonoBehaviour
{
    [SerializeField, Header("블럭 타격 오차범위")]
    protected float directionalRange = 10f;

    [SerializeField, Header("노트 점수")]
    protected int NoteScore = 100;

    [SerializeField, Header("타격 정확도 허용범위")]
    protected float[] accuracyPoint = { 0.34f, 0.67f };

    [SerializeField, Header("노트 플레이어 방향 기울기")]
    protected float noteLookAtAngle = 40f;

    // [SerializeField, Header("노트 정확도 점수배율")]
    // protected float[] accuracyScore = { 0.8f, 0.5f };
    protected bool isMoving = true;
    protected NoteData noteData;
    protected Transform noteTrans;
    protected Vector3 noteDownDirection,
        noteUpDirection;
    protected float noteDistance;
    private Collider noteCollider;
    private float EnterAngle,
        ExitAngle;
    private float hitdis;
    private Transform TargetTrans;

    public virtual void Initialize(NoteData data)
    {
        TargetTrans = GetComponentInParent<Transform>();
        if (TargetTrans != null)
        {
            TargetTrans = GetComponent<Transform>();
        }
        noteTrans = GetComponent<Transform>();
        noteCollider = GetComponent<Collider>();
        noteData = new NoteData()
        {
            baseType = data.baseType,
            noteAxis = data.noteAxis,
            direction = data.direction,
            target = data.target,
            moveSpeed = data.moveSpeed,
            noteType = data.noteType,
        };
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
        if (isMoving)
        {
            TargetTrans.position = Vector3.MoveTowards(
                transform.position,
                noteData.target,
                noteData.moveSpeed * Time.deltaTime
            );
            if (Vector3.Distance(TargetTrans.position, noteData.target) < 0.1f)
            {
                try
                {
                    Miss();
                }
                catch (System.NullReferenceException)
                {
                    Debug.LogWarning("NullReferenceException 발생. 노트를 직접 파괴합니다.");
                    Destroy(gameObject);
                }
            }
            // TODO: NoteGameManager 관련 문제 해결하기
            // 1. NoteGameManager.Instance가 null인 이유 확인
            // 2. NoteGameManager의 test 변수 초기화 확인
            // 3. 씬에 NoteGameManager 오브젝트 추가 확인
            // 4. 예외 처리 대신 근본적인 해결책 적용

            //if (Vector3.Distance(transform.position, noteData.target) < 0.1f)
            //{
            //    Miss();
            //    Destroy(gameObject);
            //}
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.black;
        Gizmos.DrawLine(transform.localPosition, transform.position + noteDownDirection);
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.localPosition, transform.position + noteUpDirection);
    }

    //TODO: 노트 기울기 만들어야함함
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
        Destroy(TargetTrans.gameObject);
    }

    protected void Miss()
    {
        NoteGameManager.Instance.SetScore(0, NoteRatings.Miss);
        Destroy(TargetTrans.gameObject);
    }

    //노트가 허용하는 Hit거리를 구함
    private void SetNoteDisTance()
    {
        float sizeY = noteCollider.bounds.size.y;
        print($"Y: {sizeY}");
        Vector3 dis = transform.position;
        dis.y += sizeY / 2;
        noteDistance = Vector3.Distance(transform.position, dis);
        print($"노트 길이: {noteDistance}");
    }

    private void OnCollisionEnter(Collision other)
    {
        Vector3 hitPoint = other.contacts[0].normal;
        hitdis = HitPoint(other);
        EnterAngle = Vector3.Angle(hitPoint, noteDownDirection);
        Debug.DrawRay(transform.position, hitPoint, Color.blue, 0.5f);

        print($"법선벡터 X: {hitPoint.x} Y : {hitPoint.y} Z : {hitPoint.z}");
        print(
            $"내 벡터 X: {noteDownDirection.x} Y : {noteDownDirection.y} Z : {noteDownDirection.z} range: {EnterAngle}"
        );
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
        if (other.gameObject.TryGetComponent(out HitObject hitObject))
        {
            if (hitObject.hitObjectType == noteData.noteType)
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

    //맞은 기준으로 노트의 중앙에서 부터의 거리를 구함
    private float HitPoint(Collision other)
    {
        Vector3 hitPoint = other.GetContact(0).point;
        Vector3 notePos = transform.position - (-transform.up * noteDistance);
        print($"notepos {notePos} hitPoint{hitPoint}");
        return Vector3.Distance(hitPoint, notePos);
    }
}
