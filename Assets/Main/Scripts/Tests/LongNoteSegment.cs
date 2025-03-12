using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LongNoteSegment : Note
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
            baseType = data.baseType,
            noteType = data.noteType,
            noteAxis = data.noteAxis,
            direction = data.direction,
            startPosition = data.startPosition,
            targetPosition = data.targetPosition,
            noteSpeed = data.noteSpeed,
            isClockwise = data.isClockwise,
            isSymmetric = data.isSymmetric,
            bar = data.bar,
            beat = data.beat,
        };

        isInitialized = true;
        transform.LookAt(noteData.targetPosition);
        spawnDspTime = AudioSettings.dspTime;

        if (!GetComponent<Collider>())
        {
            SphereCollider collider = gameObject.AddComponent<SphereCollider>();
            collider.isTrigger = true;
            collider.radius = 0.5f;
        }
    }

    private void Update()
    {
        if (!isInitialized)
            return;

        double elapsedTime = AudioSettings.dspTime - spawnDspTime;
        float totalDistance = Vector3.Distance(noteData.startPosition, noteData.targetPosition);
        float currentDistance = noteData.noteSpeed * (float)elapsedTime;
        float progress = Mathf.Clamp01(currentDistance / totalDistance);

        transform.position = Vector3.Lerp(
            noteData.startPosition,
            noteData.targetPosition,
            progress
        );

        // 목표에 도달하면 파괴
        if (progress >= 1f)
        {
            if (NoteGameManager.Instance != null)
            {
                NoteGameManager.Instance.SetScore(0, NoteRatings.Miss);
            }
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isHit || !isInitialized)
            return;

        if (other.CompareTag("Mace"))
        {
            HandleCollision(other);
        }
    }

    private void HandleCollision(Collider other)
    {
        isHit = true;

        if (hitEffectPrefab != null)
        {
            Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
        }

        if (NoteGameManager.Instance != null)
        {
            NoteGameManager.Instance.SetScore(noteScore, NoteRatings.Success);
        }

        Destroy(gameObject);
    }

    // 필요한 경우 충돌 이펙트 설정 메서드 추가
    public void SetHitEffect(GameObject effectPrefab)
    {
        hitEffectPrefab = effectPrefab;
    }
}
