using UnityEngine;

public class LongNote : Note
{
    public override void Initialize(NoteData data)
    {
        base.Initialize(data);

        // 디버그 로깅 추가
        Debug.Log(
            $"롱노트 초기화 - 마디: {data.bar}, 비트: {data.beat}, 인덱스: {data.startIndex}, "
                + $"소스위치: {data.GetStartPosition()}, 타겟위치: {data.GetTargetPosition()}"
        );
    }

    public void SetPositions(Vector3 start, Vector3 target)
    {
        startPosition = start;
        targetPosition = target;
        transform.position = start;
        transform.rotation = Quaternion.Euler(90f, 0, 0);

        Debug.Log(
            $"롱노트 세그먼트 위치 설정: 시작={start}, 타겟={target}, 거리={Vector3.Distance(start, target):F2}"
        );
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

        if (progress >= 1.0f && !isHit)
        {
            Miss();
        }
    }
}
