using Photon.Pun.Demo.PunBasics;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoteSpawner : MonoBehaviour
{
    [Header("��Ʈ ������")]
    [SerializeField] private GameObject leftNotePrefab;    // ���⼺ ��Ʈ(LeftNote)
    [SerializeField] private GameObject rightNotePrefab;   // ��Ȯ�� ��Ʈ(RightNote)

    [Header("���� ����")]
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
        // �޼�/������ ���� ����
        bool isLeftHand = Random.value > 0.5f;

        // ��ġ ����
        int x = isLeftHand ? Random.Range(0, 3) : Random.Range(2, 5);
        int y = Random.Range(0, gridManager.VerticalCells);

        // ��Ʈ ���� �� �ʱ�ȭ
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
