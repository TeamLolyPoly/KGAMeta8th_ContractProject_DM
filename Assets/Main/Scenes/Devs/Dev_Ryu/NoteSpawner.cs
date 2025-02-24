using Photon.Pun.Demo.PunBasics;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoteSpawner : MonoBehaviour
{
    [Header("노트 프리팹")]
    [SerializeField] private GameObject leftNotePrefab;    // 방향성 노트(LeftNote)
    [SerializeField] private GameObject rightNotePrefab;   // 정확도 노트(RightNote)

    [Header("스폰 설정")]
    [SerializeField] private float spawnInterval = 2f;
    [SerializeField] private float noteSpeed = 10f;

    private float timer;
    private GridManager gridManager;
    void Start()
    {
        gridManager = GridManager.Instance;
    }
    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            SpawnRandomNote();
            timer = 0f;
        }
    }

    void SpawnRandomNote()
    {
        // 왼손/오른손 랜덤 선택
        bool isLeftHand = Random.value > 0.5f;

        // 위치 선택
        int x = isLeftHand ? Random.Range(0, 3) : Random.Range(2, 5);
        int y = Random.Range(0, gridManager.VerticalCells);

        // 노트 생성 및 초기화
        GameObject prefab = isLeftHand ? leftNotePrefab : rightNotePrefab;
        Vector3 startPos = gridManager.GetCellPosition(gridManager.SourceGrid, x, y);
        Vector3 targetPos = gridManager.GetCellPosition(gridManager.TargetGrid, x, y);

        GameObject note = Instantiate(prefab, startPos, Quaternion.identity);
        Note noteComponent = note.GetComponent<Note>();

        if (noteComponent != null)
        {
            noteComponent.Initialize(targetPos, noteSpeed);
        }
    }
}
