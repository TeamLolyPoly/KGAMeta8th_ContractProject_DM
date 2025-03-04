using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RhythmRailVisualizer : MonoBehaviour
{
    [Header("레일 설정")]
    [SerializeField] private int laneCount = 5;
    [SerializeField] private float railLength = 20f;
    [SerializeField] private float railWidth = 1f;
    [SerializeField] private float railSpacing = 0.1f;
    [SerializeField] private float perspectiveAngle = 50f;
    
    [Header("시각 효과")]
    [SerializeField] private Material railMaterial;
    [SerializeField] private Color railColor = new Color(0, 0.8f, 0.8f);
    [SerializeField] private Color lineColor = new Color(0, 1f, 0);
    [SerializeField] private int divisionCount = 10;
    
    [Header("배경")]
    [SerializeField] private Color backgroundColor = new Color(0.1f, 0, 0.2f);
    
    private GameObject[] lanes;
    private GameObject[] divisions;
    private GameObject judgeLine;
    private GameObject railContainer;
    
    private void Start()
    {
        // 배경색 설정
        Camera.main.backgroundColor = backgroundColor;
        
        // 카메라 각도 설정
        Camera.main.transform.rotation = Quaternion.Euler(perspectiveAngle, 0, 0);
        Camera.main.transform.position = new Vector3(0, 10, -5);
        
        CreateRail();
    }
    private void CreateRail()
    {
        // 레인 컨테이너
        GameObject railContainer = new GameObject("RailContainer");
        railContainer.transform.position = Vector3.zero;
        
        // 전체 레일 너비 계산
        float totalWidth = (laneCount * railWidth) + ((laneCount - 1) * railSpacing);
        float startX = -totalWidth / 2f + (railWidth / 2f);
        
        // 레인 생성
        lanes = new GameObject[laneCount];
        for (int i = 0; i < laneCount; i++)
        {
            float xPos = startX + (i * (railWidth + railSpacing));
            
            // 레인 오브젝트 생성
            GameObject lane = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lane.name = $"Lane_{i}";
            lane.transform.parent = railContainer.transform;
            lane.transform.localPosition = new Vector3(xPos, 0, railLength / 2f);
            lane.transform.localScale = new Vector3(railWidth, 0.1f, railLength);
            
            // 머티리얼 설정
            Renderer renderer = lane.GetComponent<Renderer>();
            if (railMaterial != null)
            {
                renderer.material = new Material(railMaterial);
            }
            renderer.material.color = railColor;
            
            lanes[i] = lane;
        }
        
        // 구분선 생성
        divisions = new GameObject[divisionCount];
        float divisionSpacing = railLength / divisionCount;
        
        for (int i = 0; i < divisionCount; i++)
        {
            float zPos = i * divisionSpacing;
            
            GameObject division = GameObject.CreatePrimitive(PrimitiveType.Cube);
            division.name = $"Division_{i}";
            division.transform.parent = railContainer.transform;
            division.transform.localPosition = new Vector3(0, 0.06f, zPos);
            division.transform.localScale = new Vector3(totalWidth + 0.2f, 0.01f, 0.05f);
            
            // 머티리얼 설정
            Renderer renderer = division.GetComponent<Renderer>();
            renderer.material.color = lineColor;
            
            divisions[i] = division;
        }
        
        // 판정선 생성
        GameObject judgeLine = GameObject.CreatePrimitive(PrimitiveType.Cube);
        judgeLine.name = "JudgeLine";
        judgeLine.transform.parent = railContainer.transform;
        judgeLine.transform.localPosition = new Vector3(0, 0.07f, 0);
        judgeLine.transform.localScale = new Vector3(totalWidth + 0.5f, 0.05f, 0.2f);
        
        // 판정선 머티리얼 설정
        Renderer judgeRenderer = judgeLine.GetComponent<Renderer>();
        judgeRenderer.material.color = new Color(0, 0.8f, 1f);
    }
}
