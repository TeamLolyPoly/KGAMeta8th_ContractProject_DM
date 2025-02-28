using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RightGridManager : MonoBehaviour
{
    [Header("Grid Reference")]
    [SerializeField] private RightGrid sourceGrid;
    [SerializeField] private RightGrid targetGrid;

    [Header("Grid Settings")]
    [SerializeField] private float gridDistance = 15f;

    private void Start()
    {
        InitializeGrids();
    }
    private void InitializeGrids()
    {
        if (sourceGrid == null || targetGrid == null)
        {
            Debug.LogError("Grid Reference가 설정되지 않았습니다.");
            return;
        }

        //소스 그리드는 뒤쪽에 위치
        sourceGrid.transform.position = new Vector3(0, 0.7f, gridDistance);
        sourceGrid.transform.rotation = Quaternion.identity;

        //타겟 그리드는 앞쪽에 위치
        targetGrid.transform.position = new Vector3(0, 0.7f, -gridDistance);
        targetGrid.transform.rotation = Quaternion.identity;
  
    }
    // 그리드 위치 getter
    public Vector3 GetSourceGridPosition() => sourceGrid.transform.position;
    public Vector3 GetTargetGridPosition() => targetGrid.transform.position;

    public RightGrid GetSourceGrid() => sourceGrid;
    public RightGrid GetTargetGrid() => targetGrid;   
}
