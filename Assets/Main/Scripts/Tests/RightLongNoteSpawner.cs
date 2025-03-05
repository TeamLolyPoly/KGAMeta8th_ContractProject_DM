using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class RightLongNoteSpawner : MonoBehaviour
{
    [SerializeField] private RightGridManager gridManager;
    [SerializeField] private GameObject longNotePrefab;
    [SerializeField] private float moveSpeed = 5f;

    private void Start()
    {
        if (gridManager == null)
        {
            gridManager = FindObjectOfType<RightGridManager>();
            if (gridManager == null)
            {
                Debug.LogError("RightGridManager를 찾을 수 없습니다!");
                enabled = false;
                return;
            }
        }

        // 시작할 때 테스트 노트 생성
        SpawnTestNote();
    }

    private void Update()
    {
        // 스페이스바를 누를 때마다 테스트 노트 생성
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            SpawnTestNote();
        }
    }

    private void SpawnTestNote()
    {
        // 테스트용: 다양한 셀 위치에서 롱노트 생성
        SpawnLongNote(new Vector2Int(0, 0), new Vector2Int(2, 2));

        // 추가 테스트 노트 (다른 위치)
        //SpawnLongNote(new Vector2Int(1, 0), new Vector2Int(1, 2));
        //SpawnLongNote(new Vector2Int(2, 0), new Vector2Int(0, 2));

        Debug.Log("롱노트 생성 시도");
    }

    public void SpawnLongNote(Vector2Int sourceCell, Vector2Int targetCell)
    {
        if (gridManager == null || longNotePrefab == null)
        {
            Debug.LogError("RightGridManager 또는 LongNotePrefab이 설정되지 않았습니다.");
            return;
        }

        if (!IsValidCell(sourceCell) || !IsValidCell(targetCell))
        {
            Debug.LogError($"잘못된 셀 위치: 시작({sourceCell}), 끝({targetCell})");
            return;
        }

        // 정확한 셀 위치 계산
        Vector3 sourcePos = gridManager.GetCellPosition(gridManager.SourceGrid, sourceCell.x, sourceCell.y);
        Vector3 targetPos = gridManager.GetCellPosition(gridManager.TargetGrid, targetCell.x, targetCell.y);

        // 디버그 로그 추가
        Debug.Log($"소스 위치: {sourcePos}, 타겟 위치: {targetPos}, 거리: {Vector3.Distance(sourcePos, targetPos)}");

        GameObject noteObj = Instantiate(longNotePrefab, sourcePos, Quaternion.identity);
        RightLongNote longNote = noteObj.GetComponent<RightLongNote>();

        if (longNote != null)
        {
            // 직선 경로로 초기화 (속도 증가)
            longNote.Initialize(sourcePos, targetPos, moveSpeed * 5f);
            Debug.Log($"롱노트 생성 완료: {sourceCell} -> {targetCell}, 속도: {moveSpeed * 5f}");
        }
    }

    private bool IsValidCell(Vector2Int cell)
    {
        Vector2Int gridSize = gridManager.GridSize;
        return cell.x >= 0 && cell.x < gridSize.x && cell.y >= 0 && cell.y < gridSize.y;
    }
}

