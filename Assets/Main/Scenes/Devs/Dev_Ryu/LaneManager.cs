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
    public Material laneMaterial;        // 레인 머티리얼

    void Start()
    {
        CreateLaneGrid();
    }

    void CreateLaneGrid()
    {
        float totalWidth = (horizontalLanes * laneWidth) + ((horizontalLanes - 1) * laneSpacing);
        float totalHeight = (verticalLanes * laneWidth) + ((verticalLanes - 1) * laneSpacing);

        float startX = -totalWidth / 2f;  // 중앙 정렬
        float startZ = -totalHeight / 2f;

        for (int row = 0; row < verticalLanes; row++)
        {
            for (int col = 0; col < horizontalLanes; col++)
            {
                // 레인 위치 계산
                float xPos = startX + (col * (laneWidth + laneSpacing));
                float zPos = startZ + (row * (laneWidth + laneSpacing));

                // 레인 생성
                GameObject lane = CreateSingleLane(new Vector3(xPos, 0, zPos));
                lane.name = $"Lane_{row}_{col}";
                lane.transform.parent = transform;
            }
        }
    }

    GameObject CreateSingleLane(Vector3 position)
    {
        GameObject lane = new GameObject();

        // 레인 바닥 생성
        GameObject laneFloor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        laneFloor.transform.parent = lane.transform;
        laneFloor.transform.localPosition = position;
        laneFloor.transform.localScale = new Vector3(laneWidth, 0.1f, laneWidth); // 정사각형 레인

        // 머티리얼 적용
        MeshRenderer renderer = laneFloor.GetComponent<MeshRenderer>();
        renderer.material = laneMaterial;

        // 레인 식별을 위한 컴포넌트 추가
        LaneCell laneCell = lane.AddComponent<LaneCell>();

        return lane;
    }
}

// 개별 레인 셀을 관리하는 컴포넌트
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
        // 활성화 효과 (예: 색상 변경, 발광 등)
    }

    public void Deactivate()
    {
        isActive = false;
        // 비활성화 효과
    }
}
