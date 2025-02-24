using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaneManager : MonoBehaviour
{
    public int horizontalLanes = 5;     // ���� ���� ��
    public int verticalLanes = 3;       // ���� ���� ��
    public float laneWidth = 1f;        // ���� �ʺ�
    public float laneLength = 20f;      // ���� ����
    public float laneSpacing = 0.1f;    // ���� ����
    public float gridDistance = 10f;    // �� �׸��� ������ �Ÿ�
    public Material laneMaterial;        // ���� ��Ƽ����

    private GameObject sourceGrid;
    private GameObject targetGrid;

    void Start()
    {
        CreateGrids();
    }

    void CreateGrids()
    {
        // �ҽ� �׸��� ���� (�÷��̾� ��)
        sourceGrid = new GameObject("SourceGrid");
        sourceGrid.transform.parent = transform;
        sourceGrid.transform.localPosition = new Vector3(0, 0, 0);
        sourceGrid.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        CreateLaneGrid(sourceGrid);

        // Ÿ�� �׸��� ���� (��ǥ ����)
        targetGrid = new GameObject("TargetGrid");
        targetGrid.transform.parent = transform;
        targetGrid.transform.localPosition = new Vector3(0, 0, gridDistance);
        targetGrid.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        CreateLaneGrid(targetGrid);
    }
    void CreateLaneGrid(GameObject gridParent)
    {
        float totalWidth = (horizontalLanes * laneWidth) + ((horizontalLanes - 1) * laneSpacing);
        float totalHeight = (verticalLanes * laneWidth) + ((verticalLanes - 1) * laneSpacing);

        float startX = -totalWidth / 2f;
        float startY = 0f;  // �ٴڿ������� ����

        // �׸��� ��ü�� 90�� ȸ������ �������� ����
        transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        for (int col = 0; col < horizontalLanes; col++)
        {
            for (int row = 0; row < verticalLanes; row++)
            {
                float xPos = startX + (col * (laneWidth + laneSpacing));
                float yPos = startY + (row * (laneWidth + laneSpacing));

                GameObject lane = CreateSingleLane(new Vector3(xPos, yPos, 0));
                lane.name = $"Lane_{col}_{row}";
                lane.transform.parent = transform;
            }
        }
    }

    GameObject CreateSingleLane(Vector3 position)
    {
        GameObject lane = new GameObject();

        GameObject laneFloor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        laneFloor.transform.parent = lane.transform;
        laneFloor.transform.localPosition = position;
        laneFloor.transform.localScale = new Vector3(laneWidth, laneWidth, 0.1f);

        MeshRenderer renderer = laneFloor.GetComponent<MeshRenderer>();
        renderer.material = laneMaterial;

        LaneCell laneCell = lane.AddComponent<LaneCell>();

        return lane;
    }
}
