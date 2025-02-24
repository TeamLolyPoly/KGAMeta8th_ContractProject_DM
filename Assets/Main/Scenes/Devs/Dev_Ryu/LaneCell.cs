using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaneCell : MonoBehaviour
{
    private bool isActive = false;
    private MeshRenderer meshRenderer;
    public bool isSourceLane; // 소스 레인인지 여부

    void Awake()
    {
        meshRenderer = GetComponentInChildren<MeshRenderer>();
        isSourceLane = transform.parent.name == "SourceGrid";
    }

    public void Activate()
    {
        isActive = true;
        // 활성화 효과
    }

    public void Deactivate()
    {
        isActive = false;
        // 비활성화 효과
    }
}
