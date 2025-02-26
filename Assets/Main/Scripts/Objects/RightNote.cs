using UnityEngine;

public class RightNote : Note
{
    [SerializeField, Header("타격 정확도 허용범위")]
    private float[] accuracyPoint = new float[2] { 0.34f, 0.67f };

    [SerializeField, Header("노트 정확도 점수배율")]
    private float[] accuracyScore = new float[2] { 0.8f, 0.5f };

    private Renderer noteRenderer;
    private float noteDistance;

    public override void Initialize(NoteData data)
    {
        base.Initialize(data);
        noteRenderer = GetComponent<Renderer>();
        if (noteRenderer != null)
        {
            SetNoteDisTance();
        }
        if (noteRenderer != null)
        {
            switch (noteData.noteType)
            {
                case HitType.Red:
                    noteRenderer.material.color = Color.red;
                    break;
                case HitType.Blue:
                    noteRenderer.material.color = Color.blue;
                    break;
            }
        }
        NoteDirectionChange();
        NoteHitDirectionChange();
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

    private void OnCollisionEnter(Collision other)
    {
        float hitdis = HitPoint(other);
        Vector3 hitPoint = other.contacts[0].normal;
        float range = Vector3.Angle(hitPoint, hitDirection);
        print($"법선벡터 X: {hitPoint.x} Y : {hitPoint.y} Z : {hitPoint.z}");
        print($"내 벡터 X: {hitDirection.x} Y : {hitDirection.y} Z : {hitDirection.z}");
        if (other.gameObject.TryGetComponent<Mace>(out Mace Mace))
        {
            if (Mace.maceType == noteData.noteType)
            {
                if (range <= directionalRange)
                {
                    print(HitScore(hitdis));
                }
                else
                {
                    print("이상한 방향을 타격함");
                }
            }
            else
            {
                print("Mace 타입이 다름");
            }
            Destroy(this.gameObject);
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

    //hit위치에서 중앙까지의 거리를 비교후 점수 리턴
    private float HitScore(float hitdis)
    {
        if (noteDistance * accuracyPoint[0] >= hitdis)
        {
            print($"Perfect noteDis :{noteDistance * accuracyPoint[0]} , Hitdis: {hitdis}");
            return NoteScore;
        }
        else if (noteDistance * accuracyPoint[1] > hitdis)
        {
            print($"Great noteDis :{noteDistance * accuracyPoint[1]} , Hitdis: {hitdis}");
            return NoteScore * accuracyScore[0];
        }
        else if (noteDistance >= hitdis)
        {
            print($"Good noteDis :{noteDistance} , Hitdis: {hitdis}");
            return NoteScore * accuracyScore[1];
        }
        else
        {
            print($"Miss noteDis :{noteDistance} , Hitdis: {hitdis}");
            return 0;
        }
    }
}
