using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum NoteDirection
{
    East,
    West,
    South,
    North,
    Northeast,
    Northwest,
    Southeast,
    Southwest,

}
public enum NoteAxis
{
    PZ,
    MZ,
    PX,
    MX,

}

public class TestHitNote : Note
{

    [SerializeField] private NoteDirection direction;
    [SerializeField] private NoteAxis axis = NoteAxis.PZ;
    private Transform noteTrans;
    private Vector3 hitDirection;
    [SerializeField, Header("블럭 타격 오차범위")]
    private float directionalRange = 10f;
    void Awake()
    {
        noteTrans = GetComponent<Transform>();
    }

    void OnValidate()
    {
        if (noteTrans == null)
        {
            noteTrans = GetComponent<Transform>();
        }
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
            print("타격 실패");
        }
    }
    //노트에 direction값과 axis값을 보고 hit허용방향 회전각을 조정함
    private void NoteDirectionChange()
    {
        float rotationZ = 0f;
        switch (direction)
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
        noteTrans.rotation = Quaternion.Euler(noteTrans.rotation.eulerAngles.x, noteTrans.rotation.eulerAngles.y, rotationZ);
    }
    //노트에 axis값을 보고 보는 방향을 회전시킴 
    private void NoteHitDirectionChange()
    {
        float rotationY = 0f;
        switch (axis)
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
        noteTrans.rotation = Quaternion.Euler(noteTrans.rotation.eulerAngles.x, rotationY, noteTrans.rotation.eulerAngles.z);
    }
    //노트의 axis값에 달라지는 hitDirection값을 보정함
    private Vector3 SetHitDirection(Vector3 dir)
    {
        switch (axis)
        {
            case NoteAxis.PZ:
                return dir;
            case NoteAxis.MZ:
                return dir = -dir;
            case NoteAxis.PX:
                return new Vector3(dir.z, dir.y, -dir.x);
            case NoteAxis.MX:
                return new Vector3(dir.z, dir.y, dir.x);
            default:
                return dir;
        }
    }

}
