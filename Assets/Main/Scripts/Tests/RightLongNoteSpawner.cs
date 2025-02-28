using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RightLongNoteSpawner : MonoBehaviour
{
    [SerializeField] private RightGrid rightGrid;
    [SerializeField] private GameObject longNotePrefab;
    [SerializeField] private float moveSpeed = 5f;

    // 특정 셀에서 다른 셀로 이동하는 롱노트 생성
    public void SpawnLongNote(Vector2Int startCell, Vector2Int endCell)
    {
        if (rightGrid == null || longNotePrefab == null) return;
        if (!IsValidCell(startCell) || !IsValidCell(endCell)) return;

        Vector3 startPos = rightGrid.GetCellPosition(startCell.x, startCell.y);
        Vector3 endPos = rightGrid.GetCellPosition(endCell.x, endCell.y);

        GameObject noteObj = Instantiate(longNotePrefab, startPos, Quaternion.identity);
        RightLongNote longNote = noteObj.GetComponent<RightLongNote>();

        if (longNote != null)
        {
            longNote.Initialize(startPos, endPos, moveSpeed);
        }
    }

    private bool IsValidCell(Vector2Int cell)
    {
        return cell.x >= 0 && cell.x < 3 && cell.y >= 0 && cell.y < 3;
    }
}
