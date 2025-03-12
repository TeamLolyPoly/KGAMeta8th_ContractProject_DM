using UnityEngine;

public class ArcSegmentMover : Note
{
    private bool isInitialized = false;
    private bool isHit = false;

    [Header("충돌 설정")]
    [SerializeField]
    private string[] targetTags = { "Mace" }; // "Mace" 태그와 충돌 감지

    [SerializeField]
    private GameObject hitEffectPrefab; // 충돌 시 생성할 이펙트

    public override void Initialize(NoteData data)
    {
        noteData = new NoteData()
        {
            startPosition = data.startPosition,
            targetPosition = data.targetPosition,
            noteSpeed = data.noteSpeed,
        };
        isInitialized = true;

        // 이동 방향을 향하도록 회전
        transform.LookAt(noteData.targetPosition);

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
        if (!isInitialized)
            return;

        // 목표를 향해 직선으로 이동
        // transform.position = Vector3.MoveTowards(
        //     transform.position,
        //     targetPosition,
        //     moveSpeed * Time.deltaTime
        // );

        double elapsedTime = AudioSettings.dspTime - spawnDspTime;
        float progress = (float)(
            elapsedTime
            * noteData.noteSpeed
            / Vector3.Distance(noteData.startPosition, noteData.targetPosition)
        );

        transform.position = Vector3.Lerp(
            noteData.startPosition,
            noteData.targetPosition,
            progress
        );

        // 목표에 도달하면 파괴
        if (Vector3.Distance(transform.position, noteData.targetPosition) < 0.01f)
        {
            Miss();
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // 이미 충돌했거나 초기화되지 않았으면 무시
        if (isHit || !isInitialized)
            return;

        // "Mace" 태그와 충돌했는지 확인
        if (other.CompareTag("Mace"))
        {
            HandleCollision(other);
        }
        //TODO: 다른 물체와 충돌해도 삭제판정이 필요함
        // else
        // {
        //     Miss();
        // }
    }

    private void HandleCollision(Collider other)
    {
        isHit = true;

        // 충돌 이펙트 생성
        if (hitEffectPrefab != null)
        {
            Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
        }
        //노트게임 매니저로 점수 전달
        NoteGameManager.Instance.SetScore(noteScore, NoteRatings.Success);
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
