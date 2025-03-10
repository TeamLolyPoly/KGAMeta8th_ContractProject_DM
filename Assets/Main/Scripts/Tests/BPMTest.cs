using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BPMTest : MonoBehaviour
{
    [Header("입력값")]
    [SerializeField]
    private float bpm = 128f;

    [SerializeField]
    private float distance = 15f;

    [SerializeField]
    private float speedMultiplier = 1f;

    [Header("계산 결과")]
    [SerializeField]
    private float noteSpeed;

    [SerializeField]
    private float timeToTarget;

    private double dspStartTime;

    private void Start()
    {
        // dspTime을 시작 기준점으로 설정
        dspStartTime = AudioSettings.dspTime;
        CalculateSpeed();
    }

    private void OnValidate()
    {
        CalculateSpeed();
    }
    private void CalculateSpeed()
    {
        float beatsPerSecond = bpm / 60f;

        noteSpeed = distance * beatsPerSecond * speedMultiplier;
        timeToTarget = distance / noteSpeed;

        Debug.Log($"BPM: {bpm}, 거리: {distance}");
        Debug.Log($"계산된 속도: {noteSpeed:F2} units/sec");
        Debug.Log($"도달 시간: {timeToTarget:F2} sec");
    }

    private void Update()
    {
        // dspTime을 사용하여 노트 이동을 더 정확하게 테스트
        double elapsedTime = AudioSettings.dspTime - dspStartTime;

        if (elapsedTime >= timeToTarget)
        {
            Debug.Log($"[🎵] 노트 도착! 경과 시간: {elapsedTime:F2} sec");
            dspStartTime = AudioSettings.dspTime; // 타이밍 재설정
        }
    }
}
