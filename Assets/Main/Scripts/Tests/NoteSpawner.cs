using UnityEngine;

public class NoteSpawner : MonoBehaviour
{
    [Header("노트 프리펩")]
    [SerializeField]
    private GameObject leftNotePrefab;

    [SerializeField]
    private GameObject rightNotePrefab;

    [Header("노트 생성 간격")]
    [SerializeField]
    private float spawnInterval = 2f;

    [SerializeField]
    private float noteSpeed = 10f;

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
        NoteData noteData = new NoteData();
        bool isLeftHand = Random.value > 0.5f; // 50% 확률로 왼쪽/오른쪽 결정

        if (isLeftHand)
        {
            noteData.noteType = NoteHitType.Hand; // 또는 왼쪽 노트용 타입
        }
        else
        {
            noteData.noteType = Random.value > 0.5f ? NoteHitType.Red : NoteHitType.Blue;
        }

        noteData.direction = (NoteDirection)Random.Range(1, 8);
        noteData.noteAxis = NoteAxis.PZ;
        noteData.moveSpeed = noteSpeed;

        int x = isLeftHand ? Random.Range(0, 3) : Random.Range(2, 5);
        int y = Random.Range(0, gridManager.VerticalCells);

        Vector3 startPos = gridManager.GetCellPosition(gridManager.SourceGrid, x, y);
        Vector3 targetPos = gridManager.GetCellPosition(gridManager.TargetGrid, x, y);

        noteData.target = targetPos;

        GameObject prefab = isLeftHand ? leftNotePrefab : rightNotePrefab;
        GameObject note = Instantiate(prefab, startPos, Quaternion.identity);

        if (isLeftHand)
        {
            LeftNote leftNote = note.GetComponent<LeftNote>();
            if (leftNote != null)
            {
                leftNote.Initialize(noteData);
            }
        }
        else
        {
            RightNote rightNote = note.GetComponent<RightNote>();
            if (rightNote != null)
            {
                rightNote.Initialize(noteData);
            }
        }
    }
}
