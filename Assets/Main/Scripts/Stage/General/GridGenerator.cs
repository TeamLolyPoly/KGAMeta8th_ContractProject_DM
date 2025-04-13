using UnityEngine;

public class GridGenerator : MonoBehaviour
{
    [SerializeField]
    private int totalHorizontalCells = 5;

    [SerializeField]
    private int verticalCells = 3;

    [SerializeField]
    private int leftGridSize = 3;

    [SerializeField]
    private float cellSize = 0.35f;

    [SerializeField]
    private float cellSpacing = 0.05f;

    [SerializeField]
    private float gridDistance = 30f;

    [SerializeField]
    private float multiPlayerGridOffset = 15f;

    private Material LeftGridMaterial;
    private Material RightGridMaterial;
    private Material OverlapGridMaterial;

    public Transform sourceOrigin { get; private set; }
    public Transform targetOrigin { get; private set; }

    public Transform secondSourceOrigin { get; private set; }
    public Transform secondTargetOrigin { get; private set; }

    public int TotalHorizontalCells => totalHorizontalCells;
    public int VerticalCells => verticalCells;
    public int LeftGridSize => leftGridSize;
    public float CellSize => cellSize;
    public float GridDistance => gridDistance;

    private bool isInitialized = false;
    private bool isMultiplayerMode = false;
    public bool IsInitialized => isInitialized;
    public bool IsMultiplayerMode => isMultiplayerMode;

    public void Initialize(bool multiplayerMode = false)
    {
        LeftGridMaterial = Resources.Load<Material>("Materials/Stage/LeftGrid");
        RightGridMaterial = Resources.Load<Material>("Materials/Stage/RightGrid");
        OverlapGridMaterial = Resources.Load<Material>("Materials/Stage/OverlapGrid");

        isMultiplayerMode = multiplayerMode;

        if (multiplayerMode)
        {
            CreateMultiplayerGrids();
        }
        else
        {
            CreateGrids(new Vector3(0, 3.3f, gridDistance), new Vector3(0, 3.3f, 0));
        }

        isInitialized = true;
    }

    private void CleanUp()
    {
        Destroy(sourceOrigin.gameObject);
        Destroy(targetOrigin.gameObject);

        if (isMultiplayerMode)
        {
            Destroy(secondSourceOrigin.gameObject);
            Destroy(secondTargetOrigin.gameObject);
        }
    }

    private void CreateMultiplayerGrids()
    {
        CreateGrids(new Vector3(0, 3.3f, gridDistance), new Vector3(0, 3.3f, 0));

        GameObject secondSourceObj = new GameObject("SecondSourceGrid");
        secondSourceOrigin = secondSourceObj.transform;
        secondSourceOrigin.parent = transform;
        secondSourceOrigin.localPosition = new Vector3(multiPlayerGridOffset, 3.3f, gridDistance);
        secondSourceOrigin.localRotation = Quaternion.Euler(0, 0, 0);

        GameObject secondTargetObj = new GameObject("SecondTargetGrid");
        secondTargetOrigin = secondTargetObj.transform;
        secondTargetOrigin.parent = transform;
        secondTargetOrigin.localPosition = new Vector3(multiPlayerGridOffset, 3.3f, 0);
        secondTargetOrigin.localRotation = Quaternion.Euler(0, 0, 0);
        CreateGrids(secondSourceOrigin.position, secondTargetOrigin.position);
    }

    private void CreateGrids(Vector3 sourcePosition, Vector3 targetPosition)
    {
        GameObject sourceObj = new GameObject("SourceGrid");
        sourceOrigin = sourceObj.transform;
        sourceOrigin.parent = transform;
        sourceOrigin.localPosition = sourcePosition;
        sourceOrigin.localRotation = Quaternion.Euler(0, 0, 0);
        CreateGrid(sourceOrigin);

        GameObject targetObj = new GameObject("TargetGrid");
        targetOrigin = targetObj.transform;
        targetOrigin.parent = transform;
        targetOrigin.localPosition = targetPosition;
        targetOrigin.localRotation = Quaternion.Euler(0, 0, 0);
        CreateGrid(targetOrigin);

        SetCellVisible(false);
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
        if (x < leftGridSize)
        {
            renderer.material = LeftGridMaterial;
        }
        if (x >= totalHorizontalCells - leftGridSize)
        {
            renderer.material = RightGridMaterial;
        }
        if (x >= leftGridSize - 1 && x < totalHorizontalCells - leftGridSize + 1)
        {
            renderer.material = OverlapGridMaterial;
        }

        Cell cellInfo = cell.AddComponent<Cell>();
        cellInfo.Initialize(x, y, IsLeftHand(x), IsRightHand(x));
    }

    public void SetCellVisible(bool visible)
    {
        targetOrigin.gameObject.SetActive(visible);
        sourceOrigin.gameObject.SetActive(visible);

        if (isMultiplayerMode)
        {
            secondTargetOrigin.gameObject.SetActive(visible);
            secondSourceOrigin.gameObject.SetActive(visible);
        }
    }

    public bool IsLeftHand(int x)
    {
        return x < leftGridSize;
    }

    public bool IsRightHand(int x)
    {
        return x >= totalHorizontalCells - leftGridSize;
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
        int centerX = isLeftHand ? LeftGridSize / 2 : TotalHorizontalCells - LeftGridSize / 2 - 1;
        int centerY = VerticalCells / 2;

        return GetCellPosition(gridTransform, centerX, centerY);
    }

    private void OnDestroy()
    {
        CleanUp();
    }
}
