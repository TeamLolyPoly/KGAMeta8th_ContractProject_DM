using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using UnityEngine;

public class GridManager : Singleton<GridManager>
{
    [Header("그리드 설정")]
    [SerializeField] private int totalHorizontalCells = 5;  // 전체 가로 셀 (5)
    [SerializeField] private int verticalCells = 3;         // 세로 셀 (3)
    [SerializeField] private int handGridSize = 3;          // 각 손의 그리드 크기 (3x3)
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private float cellSpacing = 0.1f;
    [SerializeField] private float gridDistance = 15f;

    [Header("시각화")]
    [SerializeField] private Material leftHandMaterial;     // 왼손 영역 머티리얼
    [SerializeField] private Material rightHandMaterial;    // 오른손 영역 머티리얼
    [SerializeField] private Material overlapMaterial;      // 겹치는 영역 머티리얼

    private Transform sourceGrid;
    private Transform targetGrid;

    protected override void Awake()
    {
        base.Awake(); // Singleton 초기화
        CreateGrids();
    }

    void CreateGrids()
    {
        // 소스 그리드 생성 (플레이어 앞)
        GameObject sourceObj = new GameObject("SourceGrid");
        sourceGrid = sourceObj.transform;
        sourceGrid.parent = transform;
        sourceGrid.localPosition = new Vector3(0, 1.7f, 2f);
        CreateGrid(sourceGrid);

        // 타겟 그리드 생성
        GameObject targetObj = new GameObject("TargetGrid");
        targetGrid = targetObj.transform;
        targetGrid.parent = transform;
        targetGrid.localPosition = new Vector3(0, 1.7f, gridDistance);
        CreateGrid(targetGrid);
    }

    void CreateGrid(Transform gridParent)
    {
        float totalWidth = (totalHorizontalCells * cellSize) + ((totalHorizontalCells - 1) * cellSpacing);
        float totalHeight = (verticalCells * cellSize) + ((verticalCells - 1) * cellSpacing);
        float startX = -totalWidth / 2f;
        float startY = -totalHeight / 2f;

        for (int x = 0; x < totalHorizontalCells; x++)
        {
            for (int y = 0; y < verticalCells; y++)
            {
                CreateCell(gridParent, x, y, startX, startY);
            }
        }
    }

    void CreateCell(Transform parent, int x, int y, float startX, float startY)
    {
        GameObject cell = new GameObject($"Cell_{x}_{y}");
        cell.transform.parent = parent;

        float xPos = startX + (x * (cellSize + cellSpacing));
        float yPos = startY + (y * (cellSize + cellSpacing));
        cell.transform.localPosition = new Vector3(xPos, yPos, 0);

        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visual.transform.parent = cell.transform;
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localScale = new Vector3(cellSize, cellSize, 0.1f);

        // 영역에 따른 머티리얼 설정
        MeshRenderer renderer = visual.GetComponent<MeshRenderer>();
        if (x < handGridSize) // 왼손 영역
        {
            renderer.material = leftHandMaterial;
        }
        if (x >= totalHorizontalCells - handGridSize) // 오른손 영역
        {
            renderer.material = rightHandMaterial;
        }
        if (x >= handGridSize - 1 && x < totalHorizontalCells - handGridSize + 1) // 겹치는 영역
        {
            renderer.material = overlapMaterial;
        }

        // 셀 정보 저장
        CellInfo cellInfo = cell.AddComponent<CellInfo>();
        cellInfo.Initialize(x, y, IsLeftHand(x), IsRightHand(x));
    }

    public bool IsLeftHand(int x)
    {
        return x < handGridSize;
    }

    public bool IsRightHand(int x)
    {
        return x >= totalHorizontalCells - handGridSize;
    }

    public Vector3 GetCellPosition(Transform gridTransform, int x, int y)
    {
        float totalWidth = (totalHorizontalCells * cellSize) + ((totalHorizontalCells - 1) * cellSpacing);
        float totalHeight = (verticalCells * cellSize) + ((verticalCells - 1) * cellSpacing);
        float startX = -totalWidth / 2f;
        float startY = -totalHeight / 2f;

        float xPos = startX + (x * (cellSize + cellSpacing));
        float yPos = startY + (y * (cellSize + cellSpacing));

        return gridTransform.TransformPoint(new Vector3(xPos, yPos, 0));
    }
    
    //초기화
    public Transform SourceGrid => sourceGrid;
    public Transform TargetGrid => targetGrid;
    public Vector2Int GridSize => new Vector2Int(totalHorizontalCells, verticalCells);
    public int TotalHorizontalCells => totalHorizontalCells;
    public int VerticalCells => verticalCells;
    public int HandGridSize => handGridSize;
    public float CellSize => cellSize;
    public float GridDistance => gridDistance;
}
