using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArcSegmentMover : MonoBehaviour
{
    private Vector3 startPosition;
    private Vector3 targetPosition;
    private float moveSpeed;
    private bool isInitialized = false;
    
    public void Initialize(Vector3 start, Vector3 target, float speed)
    {
        startPosition = start;
        targetPosition = target;
        moveSpeed = speed;
        isInitialized = true;
        
        // 이동 방향을 향하도록 회전
        transform.LookAt(targetPosition);
    }
    
    private void Update()
    {
        if (!isInitialized) return;
        
        // 목표를 향해 직선으로 이동
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            moveSpeed * Time.deltaTime
        );
        
        // 목표에 도달하면 파괴
        if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
        {
            Destroy(gameObject);
        }
    }
}
