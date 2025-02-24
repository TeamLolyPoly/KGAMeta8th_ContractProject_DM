using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum HitType
{
    Red,
    Bule,
}

public class TestMaceNote : Note
{
    [SerializeField] private HitType noteType;

    [SerializeField, Header("타격 정확도 허용범위")]
    private float[] accuracyPoint = new float[2] { 0.34f, 0.67f };
    [SerializeField, Header("노트 정확도 점수배율")]
    private float[] accuracyScore = new float[2] { 0.8f, 0.5f };
    private Transform noteTrans;
    private Renderer noteRenderer;
    private float noteDistance;

    void Awake()
    {
        noteRenderer = GetComponent<Renderer>();
        noteTrans = GetComponent<Transform>();
        SetNoteDisTance();
    }

    //노트가 허용하는 Hit거리를 구함
    private void SetNoteDisTance()
    {
        float sizeX = noteRenderer.bounds.size.x;
        float sizeY = noteRenderer.bounds.size.y;
        print($"x: {sizeX} Y: {sizeY}");
        Vector3 dis = noteTrans.position;
        dis.x += sizeX / 2;
        noteDistance = Vector3.Distance(noteTrans.position, dis);
        print($"노트 길이: {noteDistance}");
    }

    //시각적으로 타입이 다른걸 보이게할려고 만듬 삭제해도 상관없음
    private void OnValidate()
    {
        if (noteRenderer == null)
        {
            noteRenderer = GetComponent<Renderer>();
        }
        switch (noteType)
        {
            case HitType.Red:
                noteRenderer.sharedMaterial.color = Color.red;
                break;
            case HitType.Bule:
                noteRenderer.sharedMaterial.color = Color.blue;
                break;
        }
    }

    private void OnCollisionEnter(Collision other)
    {
        float hitdis = HitPoint(other);
        print(hitdis);
        if (other.gameObject.TryGetComponent<TestMace>(out TestMace Mace))
        {
            if (Mace.maceType == noteType)
            {
                print(HitScore(hitdis));
            }
        }
    }

    //맞은 기준으로 노트의 중앙에서 부터의 거리를 구함
    private float HitPoint(Collision other)
    {
        Vector3 hitPoint = other.GetContact(0).point;
        Vector3 notePos = noteTrans.transform.position;
        notePos.z -= noteRenderer.bounds.size.z / 2;
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
