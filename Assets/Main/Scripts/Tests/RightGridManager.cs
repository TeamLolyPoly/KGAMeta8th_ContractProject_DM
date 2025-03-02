using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RightGridManager : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField] private int horizontalCells = 3;
    [SerializeField] private int verticalCells = 3;
    [SerializeField] private float cellSize = 0.3f;
    [SerializeField] private float cellSpacing = 0.05f;
    [SerializeField] private float gridDistance = 15f;
    [SerializeField] private float gridHeight = 0.7f;

    [Header("Materials")]
    [SerializeField] private Material gridMaterial;

    private RightGrid sourceGrid;
    private RightGrid targetGrid;

    public RightGrid SourceGrid => sourceGrid;
    public RightGrid TargetGrid => targetGrid;
    public Vector2Int GridSize => new Vector2Int(horizontalCells, verticalCells);

    private void Awake()
    {
        CreateGrids();
    }

    private void CreateGrids()
    {
        // 소스 그리드 생성 (뒤쪽)
        GameObject sourceObj = new GameObject("SourceGrid");
        sourceObj.transform.parent = transform;
        sourceObj.transform.localPosition = new Vector3(0, gridHeight, gridDistance);
        sourceObj.transform.localRotation = Quaternion.identity;

        sourceGrid = sourceObj.AddComponent<RightGrid>();
        sourceGrid.Initialize(horizontalCells, verticalCells, cellSize, cellSpacing, gridMaterial);

        // 타겟 그리드 생성 (앞쪽)
        GameObject targetObj = new GameObject("TargetGrid");
        targetObj.transform.parent = transform;
        targetObj.transform.localPosition = new Vector3(0, gridHeight, -gridDistance);
        targetObj.transform.localRotation = Quaternion.identity;

        targetGrid = targetObj.AddComponent<RightGrid>();
        targetGrid.Initialize(horizontalCells, verticalCells, cellSize, cellSpacing, gridMaterial);

        Debug.Log($"그리드 생성 완료: 소스({sourceObj.transform.position}), 타겟({targetObj.transform.position})");
    }

    // 그리드 위치 getter
    public Vector3 GetSourceGridPosition() => sourceGrid.transform.position;
    public Vector3 GetTargetGridPosition() => targetGrid.transform.position;

    // 특정 셀의 월드 좌표 반환
    public Vector3 GetCellPosition(RightGrid grid, int x, int y)
    {
        if (grid == null)
        {
            Debug.LogError("그리드가 null입니다.");
            return Vector3.zero;
        }
        return grid.GetCellPosition(x, y);
    }
}
