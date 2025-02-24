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
        NoteData noteData = CreateRandomNoteData();
        bool isLeftHand = noteData.noteType == HitType.None;

        int x = isLeftHand ? Random.Range(0, 3) : Random.Range(2, 5);
        int y = Random.Range(0, gridManager.VerticalCells);

        Vector3 startPos = gridManager.GetCellPosition(gridManager.SourceGrid, x, y);
        Vector3 targetPos = gridManager.GetCellPosition(gridManager.TargetGrid, x, y);

        GameObject prefab = isLeftHand ? leftNotePrefab : rightNotePrefab;
        GameObject note = Instantiate(prefab, startPos, Quaternion.identity);

        if (isLeftHand)
        {
            LeftNote leftNote = note.GetComponent<LeftNote>();
            if (leftNote != null)
            {
                leftNote.Initialize(targetPos, noteSpeed);
                leftNote.SetNoteData(noteData);
            }
        }
        else
        {
            RightNote rightNote = note.GetComponent<RightNote>();
            if (rightNote != null)
            {
                rightNote.Initialize(targetPos, noteSpeed);
                rightNote.SetNoteData(noteData);
            }
        }
    }
    private NoteData CreateRandomNoteData()
    {
        NoteData data = new NoteData();

        // �����ϰ� �޼�/������ ��Ʈ ����
        if (Random.value > 0.5f)
        {
            // LeftNote ������
            data.noteType = HitType.None;  // None�� LeftNote�� �ǹ�
            data.direction = (NoteDirection)Random.Range(0, 8);
            data.noteAxis = (NoteAxis)Random.Range(0, 4);
        }
        else
        {
            // RightNote ������
            data.noteType = Random.value > 0.5f ? HitType.Red : HitType.Blue;
            data.direction = NoteDirection.None;
            data.noteAxis = NoteAxis.None;
        }

        return data;
    }
}
