using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RightGrid : MonoBehaviour
{
    private int horizontalCells;
    private int verticalCells;
    private float cellSize;
    private float cellSpacing;
    private Material gridMaterial;
    private List<GameObject> cells = new List<GameObject>();

    public void Initialize(int hCells, int vCells, float size, float spacing, Material material)
    {
        horizontalCells = hCells;
        verticalCells = vCells;
        cellSize = size;
        cellSpacing = spacing;
        gridMaterial = material;

        CreateGrid();
    }

    // 그리드 생성
    private void CreateGrid()
    {
        // 전체 그리드의 크기 계산
        float totalWidth = (horizontalCells * cellSize) + ((horizontalCells - 1) * cellSpacing);
        float totalHeight = (verticalCells * cellSize) + ((verticalCells - 1) * cellSpacing);
        float startX = -totalWidth / 2f;  // 중앙 정렬을 위한 시작 위치
        float startY = -totalHeight / 2f;

        // 그리드 셀 생성
        for (int x = 0; x < horizontalCells; x++)
        {
            for (int y = 0; y < verticalCells; y++)
            {
                CreateCell(x, y, startX, startY);
            }
        }

        Debug.Log($"그리드 생성 완료: {horizontalCells}x{verticalCells}, 위치: {transform.position}");
    }

    // 개별 셀 생성
    private void CreateCell(int x, int y, float startX, float startY)
    {
        // 셀 게임오브젝트 생성
        GameObject cell = new GameObject($"Cell_{x}_{y}");
        cell.transform.parent = transform;

        // 셀 위치 계산
        float xPos = startX + (x * (cellSize + cellSpacing));
        float yPos = startY + (y * (cellSize + cellSpacing));
        cell.transform.localPosition = new Vector3(xPos, yPos, 0);

        // 셀의 시각적 요소 생성
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visual.transform.parent = cell.transform;
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localScale = new Vector3(cellSize, cellSize, 0.1f);

        // 머티리얼 적용
        if (gridMaterial != null)
        {
            MeshRenderer renderer = visual.GetComponent<MeshRenderer>();
            renderer.material = gridMaterial;
        }

        // 셀 정보 컴포넌트 추가
        CellInfo cellInfo = cell.AddComponent<CellInfo>();
        cellInfo.Initialize(x, y, false, true);

        cells.Add(cell);
    }

    // 특정 셀의 월드 좌표 반환
    public Vector3 GetCellPosition(int x, int y)
    {
        if (x < 0 || x >= horizontalCells || y < 0 || y >= verticalCells)
        {
            Debug.LogError($"잘못된 셀 인덱스: ({x}, {y})");
            return transform.position;
        }

        float totalWidth = (horizontalCells * cellSize) + ((horizontalCells - 1) * cellSpacing);
        float totalHeight = (verticalCells * cellSize) + ((verticalCells - 1) * cellSpacing);
        float startX = -totalWidth / 2f;
        float startY = -totalHeight / 2f;

        float xPos = startX + (x * (cellSize + cellSpacing));
        float yPos = startY + (y * (cellSize + cellSpacing));

        return transform.TransformPoint(new Vector3(xPos, yPos, 0));
    }

    // 그리드 크기 반환
    public Vector2Int GridSize => new Vector2Int(horizontalCells, verticalCells);

    // 셀 크기 반환
    public float CellSize => cellSize;
}
