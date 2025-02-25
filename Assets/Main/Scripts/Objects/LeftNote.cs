using UnityEngine;

public class LeftNote : Note
{
    public override void Initialize(NoteData data)
    {
        base.Initialize(data);
        NoteDirectionChange();
        NoteHitDirectionChange();
    }
    private void OnCollisionEnter(Collision other)
    {
        Vector3 hitPoint = other.contacts[0].normal;
        print($"법선벡터 X: {hitPoint.x} Y : {hitPoint.y} Z : {hitPoint.z}");
        print($"내 벡터 X: {hitDirection.x} Y : {hitDirection.y} Z : {hitDirection.z}");
        float range = Vector3.Angle(hitPoint, hitDirection);

        if (range <= directionalRange)
        {
            print($"타격 성공{NoteScore}");
        }
        else
        {
            print("이상한 방향을 타격함");
        }
        Destroy(this.gameObject);
    }
}
