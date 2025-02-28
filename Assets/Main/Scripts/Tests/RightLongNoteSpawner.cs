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
        // 테스트용: (0,0)에서 (2,2)로 이동하는 롱노트
        SpawnLongNote(new Vector2Int(0, 0), new Vector2Int(2, 2));
    
        Debug.Log("롱노트 생성 시도");
    }

    public void SpawnLongNote(Vector2Int sourceCell, Vector2Int targetCell)
    {
        if (gridManager == null || longNotePrefab == null)
        {
            Debug.LogError("RightGrid 또는 LongNotePrefab이 설정되지 않았습니다.");
            return;
        }

        if (!IsValidCell(sourceCell) || !IsValidCell(targetCell))
        {
            Debug.LogError($"잘못된 셀 위치: 시작({sourceCell}), 끝({targetCell})");
            return;
        }
        
        RightGrid sourceGrid = gridManager.GetSourceGrid();
        RightGrid targetGrid = gridManager.GetTargetGrid();

        if (sourceGrid == null || targetGrid == null)
        {
            Debug.LogError("소스 그리드 또는 타겟 그리드가 없습니다.");
            return;
        }
        
        // 정확한 셀 위치 계산
        Vector3 sourcePos = sourceGrid.GetCellPosition(sourceCell.x, sourceCell.y);
        Vector3 targetPos = targetGrid.GetCellPosition(targetCell.x, targetCell.y);
        
        GameObject noteObj = Instantiate(longNotePrefab, sourcePos, Quaternion.identity);
        RightLongNote longNote = noteObj.GetComponent<RightLongNote>();

        if (longNote != null)
        {
            longNote.Initialize(sourcePos, targetPos, moveSpeed);
            Debug.Log($"롱노트 생성 완료: {sourceCell} -> {targetCell}");
        }
    }

    private bool IsValidCell(Vector2Int cell)
    {
        return cell.x >= 0 && cell.x < 3 && cell.y >= 0 && cell.y < 3;
    }
}

