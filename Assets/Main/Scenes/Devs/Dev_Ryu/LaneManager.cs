using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaneManager : MonoBehaviour
{
    public int horizontalLanes = 5;     // 가로 레인 수
    public int verticalLanes = 3;       // 세로 레인 수
    public float laneWidth = 1f;        // 레인 너비
    public float laneLength = 20f;      // 레인 길이
    public float laneSpacing = 0.1f;    // 레인 간격
    public float gridDistance = 10f;    // 두 그리드 사이의 거리
    public Material laneMaterial;        // 레인 머티리얼

    private GameObject sourceGrid;
    private GameObject targetGrid;

    void Start()
    {
        CreateGrids();
    }

    void CreateGrids()
    {
        // 소스 그리드 생성 (플레이어 쪽)
        sourceGrid = new GameObject("SourceGrid");
        sourceGrid.transform.parent = transform;
        sourceGrid.transform.localPosition = new Vector3(0, 0, 0);
        sourceGrid.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        CreateLaneGrid(sourceGrid);

        // 타겟 그리드 생성 (목표 지점)
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
        float startY = 0f;  // 바닥에서부터 시작

        // 그리드 전체를 90도 회전시켜 수직으로 세움
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
