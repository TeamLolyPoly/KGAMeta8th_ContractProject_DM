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
    public Material laneMaterial;        // ���� ��Ƽ����

    void Start()
    {
        CreateLaneGrid();
    }

    void CreateLaneGrid()
    {
        float totalWidth = (horizontalLanes * laneWidth) + ((horizontalLanes - 1) * laneSpacing);
        float totalHeight = (verticalLanes * laneWidth) + ((verticalLanes - 1) * laneSpacing);

        float startX = -totalWidth / 2f;  // �߾� ����
        float startZ = -totalHeight / 2f;

        for (int row = 0; row < verticalLanes; row++)
        {
            for (int col = 0; col < horizontalLanes; col++)
            {
                // ���� ��ġ ���
                float xPos = startX + (col * (laneWidth + laneSpacing));
                float zPos = startZ + (row * (laneWidth + laneSpacing));

                // ���� ����
                GameObject lane = CreateSingleLane(new Vector3(xPos, 0, zPos));
                lane.name = $"Lane_{row}_{col}";
                lane.transform.parent = transform;
            }
        }
    }

    GameObject CreateSingleLane(Vector3 position)
    {
        GameObject lane = new GameObject();

        // ���� �ٴ� ����
        GameObject laneFloor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        laneFloor.transform.parent = lane.transform;
        laneFloor.transform.localPosition = position;
        laneFloor.transform.localScale = new Vector3(laneWidth, 0.1f, laneWidth); // ���簢�� ����

        // ��Ƽ���� ����
        MeshRenderer renderer = laneFloor.GetComponent<MeshRenderer>();
        renderer.material = laneMaterial;

        // ���� �ĺ��� ���� ������Ʈ �߰�
        LaneCell laneCell = lane.AddComponent<LaneCell>();

        return lane;
    }
}

// ���� ���� ���� �����ϴ� ������Ʈ
public class LaneCell : MonoBehaviour
{
    private bool isActive = false;
    private MeshRenderer meshRenderer;

    void Awake()
    {
        meshRenderer = GetComponentInChildren<MeshRenderer>();
    }

    public void Activate()
    {
        isActive = true;
        // Ȱ��ȭ ȿ�� (��: ���� ����, �߱� ��)
    }

    public void Deactivate()
    {
        isActive = false;
        // ��Ȱ��ȭ ȿ��
    }
}
