using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RightLongNote : MonoBehaviour
{
    [Header("롱노트 설정")]
    [SerializeField] private GameObject segmentPrefab;
    [SerializeField] private Material noteMaterial;

    [Header("박자 설정")]
    [SerializeField] private int measureCount = 1;
    [SerializeField] private int beatsPerMeasure = 4;
    [SerializeField] private int segmentsPerBeat = 4;
    [SerializeField] private float segmentSpawnInterval = 0.1f;

    private Vector3 startPosition;
    private Vector3 endPosition;
    private List<GameObject> segments = new List<GameObject>();
    private float moveSpeed = 5f;
    private bool isInitialized = false;
    private int totalSegments;
    private int spawnedSegments = 0;
    private int destroyedSegments = 0;
    private float nextSpawnTime = 0f;

    // 롱노트 초기화
    public void Initialize(Vector3 start, Vector3 end, float speed)
    {
        startPosition = start;
        endPosition = end;
        moveSpeed = speed;
        
        totalSegments = measureCount * beatsPerMeasure * segmentsPerBeat;
        
        // 이 오브젝트는 관리자 역할만 하고 보이지 않게 설정
        if (TryGetComponent<MeshRenderer>(out var renderer))
        {
            renderer.enabled = false;
        }
        
        isInitialized = true;
        nextSpawnTime = Time.time;
        
        Debug.Log($"롱노트 초기화: 총 세그먼트 {totalSegments}");
    }

    private void Update()
    {
        if (!isInitialized) return;

        // 세그먼트 생성
        if (Time.time >= nextSpawnTime && spawnedSegments < totalSegments)
        {
            SpawnSegment();
            nextSpawnTime = Time.time + segmentSpawnInterval;
        }

        // 세그먼트 이동 및 파괴
        MoveAndDestroySegments();
        
        // 모든 세그먼트가 생성되고 파괴되었는지 확인
        if (spawnedSegments >= totalSegments && destroyedSegments >= totalSegments)
        {
            Debug.Log("모든 세그먼트 처리 완료, 롱노트 제거");
            Destroy(gameObject);
        }
    }

    private void SpawnSegment()
    {
        GameObject segment = Instantiate(segmentPrefab, startPosition, Quaternion.identity);
        segment.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
        
        if (segment.TryGetComponent<MeshRenderer>(out var renderer))
        {
            renderer.material = noteMaterial;
        }
        
        segments.Add(segment);
        spawnedSegments++;
    }

    private void MoveAndDestroySegments()
    {
        for (int i = 0; i < segments.Count; i++)
        {
            if (segments[i] == null) continue;
            
            // 세그먼트 이동
            segments[i].transform.position = Vector3.MoveTowards(
                segments[i].transform.position,
                endPosition,
                moveSpeed * Time.deltaTime
            );
            
            // 목표 도달 시 세그먼트 파괴
            if (Vector3.Distance(segments[i].transform.position, endPosition) < 0.01f)
            {
                Destroy(segments[i]);
                segments[i] = null;
                destroyedSegments++;
            }
        }
    }

    private void OnDestroy()
    {
        // 남은 세그먼트 정리
        foreach (var segment in segments)
        {
            if (segment != null)
            {
                Destroy(segment);
            }
        }
    }
}
