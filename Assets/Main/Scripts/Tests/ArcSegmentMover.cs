using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArcSegmentMover : MonoBehaviour
{
    private Vector3 startPosition;
    private Vector3 targetPosition;
    private float moveSpeed;
    private bool isInitialized = false;
    private bool isHit = false;

    private double spawnDspTime; // dspTime을 기준으로 생성 시간 저장

    [Header("충돌 설정")]
    [SerializeField] private string[] targetTags = { "Mace" }; // "Mace" 태그와 충돌 감지
    [SerializeField] private GameObject hitEffectPrefab; // 충돌 시 생성할 이펙트
    public void Initialize(Vector3 start, Vector3 target, float speed)
    {
        startPosition = start;
        targetPosition = target;
        moveSpeed = speed;
        isInitialized = true;

        // 이동 방향을 향하도록 회전
        transform.LookAt(targetPosition);

        spawnDspTime = AudioSettings.dspTime; // dspTime을 기준으로 이동
        // 콜라이더가 없으면 추가
        if (!GetComponent<Collider>())
        {
            SphereCollider collider = gameObject.AddComponent<SphereCollider>();
            collider.isTrigger = true;
            collider.radius = 0.5f; // 적절한 크기로 조정
        }
    }
    private void Update()
    {
        if (!isInitialized) return;

        // 목표를 향해 직선으로 이동
        // transform.position = Vector3.MoveTowards(
        //     transform.position,
        //     targetPosition,
        //     moveSpeed * Time.deltaTime
        // );

        double elapsedTime = AudioSettings.dspTime - spawnDspTime;
        float progress = (float)(elapsedTime * moveSpeed / Vector3.Distance(startPosition, targetPosition));

        transform.position = Vector3.Lerp(startPosition, targetPosition, progress);

        // 목표에 도달하면 파괴
        if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
        {
            Destroy(gameObject);
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        // 이미 충돌했거나 초기화되지 않았으면 무시
        if (isHit || !isInitialized) return;

        // "Mace" 태그와 충돌했는지 확인
        if (other.CompareTag("Mace"))
        {
            HandleCollision(other);
        }
    }

    private void HandleCollision(Collider other)
    {
        isHit = true;

        // 충돌 이펙트 생성
        if (hitEffectPrefab != null)
        {
            Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
        }
        // 점수 추가 또는 게임 이벤트 발생 (필요한 경우)
        // 충돌 이벤트 발생 (필요한 경우)
        // 오브젝트 파괴
        Destroy(gameObject);
    }

    // 필요한 경우 충돌 이펙트 설정 메서드 추가
    public void SetHitEffect(GameObject effectPrefab)
    {
        hitEffectPrefab = effectPrefab;
    }
}
