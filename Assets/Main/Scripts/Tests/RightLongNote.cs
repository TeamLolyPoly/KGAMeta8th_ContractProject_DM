
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RightLongNote : MonoBehaviour
{
    [Header("롱노트 설정")]
    [SerializeField] private GameObject segmentPrefab;   // 세그먼트 프리팹
    [SerializeField] private Material noteMaterial;      // 노트 머티리얼

    [Header("박자 설정")]
    [SerializeField] private int measureCount = 1;       // 마디 수
    [SerializeField] private int beatsPerMeasure = 4;    // 한 마디당 박자 수
    [SerializeField] private int segmentsPerBeat = 4;    // 한 박자당 세그먼트 수

    private Vector3 startPosition;                        // 시작 위치 (시작 셀)
    private Vector3 endPosition;                          // 목표 위치 (도착 셀)
    private List<GameObject> segments = new List<GameObject>();
    private float moveSpeed = 5f;
    private bool isInitialized = false;

    // 롱노트 초기화
    public void Initialize(Vector3 start, Vector3 end, float speed)
    {
        startPosition = start;
        endPosition = end;
        moveSpeed = speed;
        transform.position = startPosition;

        CreateSegments();
        isInitialized = true;
    }

    private void CreateSegments()
    {
        // 전체 세그먼트 수 계산
        int totalSegments = measureCount * beatsPerMeasure * segmentsPerBeat;

        // 시작점과 끝점 사이의 거리
        Vector3 direction = (endPosition - startPosition);
        float totalDistance = direction.magnitude;
        Vector3 normalizedDirection = direction.normalized;

        // 세그먼트 간격 계산
        float segmentSpacing = totalDistance / totalSegments;

        // 첫 번째 세그먼트는 현재 오브젝트
        segments.Add(gameObject);
        if (TryGetComponent<MeshRenderer>(out var renderer))
        {
            renderer.material = noteMaterial;
        }

        // 나머지 세그먼트 생성
        for (int i = 1; i < totalSegments; i++)
        {
            Vector3 segmentPosition = startPosition + (normalizedDirection * (segmentSpacing * i));
            GameObject segment = Instantiate(segmentPrefab, segmentPosition, Quaternion.identity);

            // 세그먼트 크기와 회전 설정
            segment.transform.forward = normalizedDirection;
            segment.transform.localScale = new Vector3(0.2f, 0.2f, segmentSpacing);

            if (segment.TryGetComponent<MeshRenderer>(out var segRenderer))
            {
                segRenderer.material = noteMaterial;
            }

            segments.Add(segment);
        }

        Debug.Log($"롱노트 생성 완료 - 총 세그먼트: {totalSegments}");
    }

    private void Update()
    {
        if (!isInitialized) return;

        // 첫 번째 세그먼트(헤드) 이동
        float step = moveSpeed * Time.deltaTime;
        transform.position = Vector3.MoveTowards(transform.position, endPosition, step);

        // 목표 도달 시 노트 제거
        if (Vector3.Distance(transform.position, endPosition) < 0.01f)
        {
            DestroyAllSegments();
        }
    }

    private void DestroyAllSegments()
    {
        foreach (var segment in segments)
        {
            if (segment != null && segment != gameObject)
            {
                Destroy(segment);
            }
        }
        segments.Clear();
        Destroy(gameObject);
    }
}
