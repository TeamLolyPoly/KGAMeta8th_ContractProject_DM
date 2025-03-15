using UnityEngine;

public class GridGenerator : MonoBehaviour, IInitializable
{
    [SerializeField]
    private int totalHorizontalCells = 5;

    [SerializeField]
    private int verticalCells = 3;

    [SerializeField]
    private int handGridSize = 3;

    [SerializeField]
    private float cellSize = 0.5f;

    [SerializeField]
    private float cellSpacing = 0.05f;

    [SerializeField]
    private float gridDistance = 15f;

    private Material LeftGridMaterial;
    private Material RightGridMaterial;
    private Material OverlapGridMaterial;

    public Transform sourceOrigin { get; private set; }
    public Transform targetOrigin { get; private set; }

    public int TotalHorizontalCells => totalHorizontalCells;
    public int VerticalCells => verticalCells;
    public int HandGridSize => handGridSize;
    public float CellSize => cellSize;
    public float GridDistance => gridDistance;

    private bool isInitialized = false;
    public bool IsInitialized => isInitialized;

    public void Initialize()
    {
        LeftGridMaterial = Resources.Load<Material>("Materials/Stage/LeftGrid");
        RightGridMaterial = Resources.Load<Material>("Materials/Stage/RightGrid");
        OverlapGridMaterial = Resources.Load<Material>("Materials/Stage/OverlapGrid");
        CreateGrids();
        isInitialized = true;
    }

    private void CreateGrids()
    {
        GameObject sourceObj = new GameObject("SourceGrid");
        sourceOrigin = sourceObj.transform;
        sourceOrigin.parent = transform;
        sourceOrigin.localPosition = new Vector3(0, 1.7f, gridDistance);
        sourceOrigin.localRotation = Quaternion.Euler(0, 0, 0);
        CreateGrid(sourceOrigin);

        GameObject targetObj = new GameObject("TargetGrid");
        targetOrigin = targetObj.transform;
        targetOrigin.parent = transform;
        targetOrigin.localPosition = new Vector3(0, 1.7f, 0);
        targetOrigin.localRotation = Quaternion.Euler(0, 0, 0);
        CreateGrid(targetOrigin);
    }

    private void CreateGrid(Transform gridParent)
    {
        float totalWidth =
            (totalHorizontalCells * cellSize) + ((totalHorizontalCells - 1) * cellSpacing);
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

    private void CreateCell(Transform parent, int x, int y, float startX, float startY)
    {
        GameObject cell = new GameObject($"Cell[{x}|{y}]");
        cell.transform.parent = parent;

        float xPos = startX + (x * (cellSize + cellSpacing));
        float yPos = startY + (y * (cellSize + cellSpacing));
        cell.transform.localPosition = new Vector3(xPos, yPos, 0);

        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        visual.transform.parent = cell.transform;
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localScale = new Vector3(cellSize, cellSize, cellSize);

        MeshRenderer renderer = visual.GetComponent<MeshRenderer>();
        if (x < handGridSize)
        {
            renderer.material = LeftGridMaterial;
        }
        if (x >= totalHorizontalCells - handGridSize)
        {
            renderer.material = RightGridMaterial;
        }
        if (x >= handGridSize - 1 && x < totalHorizontalCells - handGridSize + 1)
        {
            renderer.material = OverlapGridMaterial;
        }

        Cell cellInfo = cell.AddComponent<Cell>();
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
        float totalWidth =
            (totalHorizontalCells * cellSize) + ((totalHorizontalCells - 1) * cellSpacing);
        float totalHeight = (verticalCells * cellSize) + ((verticalCells - 1) * cellSpacing);
        float startX = -totalWidth / 2f;
        float startY = -totalHeight / 2f;

        float xPos = startX + (x * (cellSize + cellSpacing));
        float yPos = startY + (y * (cellSize + cellSpacing));

        return gridTransform.TransformPoint(new Vector3(xPos, yPos, 0));
    }

    public Vector3 GetGridCenter(Transform gridTransform)
    {
        return gridTransform.position;
    }

    public Vector3 GetHandGridCenter(Transform gridTransform, bool isLeftHand)
    {
        int centerX = isLeftHand ? HandGridSize / 2 : TotalHorizontalCells - HandGridSize / 2 - 1;
        int centerY = VerticalCells / 2;

        return GetCellPosition(gridTransform, centerX, centerY);
    }
}
