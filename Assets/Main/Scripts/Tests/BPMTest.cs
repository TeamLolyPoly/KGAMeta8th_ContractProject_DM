using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BPMTest : MonoBehaviour
{
    [Header("입력값")]
    [SerializeField]
    private float bpm = 120f;

    [SerializeField]
    private float distance = 15f;

    [SerializeField]
    private float speedMultiplier = 1f;

    [Header("계산 결과")]
    [SerializeField]
    private float noteSpeed;

    [SerializeField]
    private float timeToTarget;

    private void OnValidate()
    {
        CalculateSpeed();
    }

    private void CalculateSpeed()
    {
        //BPM을 초당 비트로 계산
        float beatsPerSecond = bpm / 60f;
        //속도 계산
        noteSpeed = distance * beatsPerSecond * speedMultiplier;
        //도달 시간 계산
        timeToTarget = distance / noteSpeed;

        Debug.Log($"BPM: {bpm}, 거리: {distance}");
        Debug.Log($"계산된 속도: {noteSpeed:F2} units/sec");
        Debug.Log($"도달 시간: {timeToTarget:F2} sec");
    }
}
