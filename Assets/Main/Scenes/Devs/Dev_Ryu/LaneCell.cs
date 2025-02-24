using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaneCell : MonoBehaviour
{
    private bool isActive = false;
    private MeshRenderer meshRenderer;
    public bool isSourceLane; // �ҽ� �������� ����

    void Awake()
    {
        meshRenderer = GetComponentInChildren<MeshRenderer>();
        isSourceLane = transform.parent.name == "SourceGrid";
    }

    public void Activate()
    {
        isActive = true;
        // Ȱ��ȭ ȿ��
    }

    public void Deactivate()
    {
        isActive = false;
        // ��Ȱ��ȭ ȿ��
    }
}
