using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NoteEditor
{
    public class CellController : MonoBehaviour, IInitializable
    {
        private RailController railController;
        private int laneCount = 5;
        private int verticalCellCount = 3;
        private float cellSize = 0.5f;
        private float cellSpacing = 0.1f;
        private Material cellMaterial;
        private Material selectedCellMaterial;
        private GameObject cellObjectPrefab;

        private float railWidth = 1f;
        private float railSpacing = 0.1f;

        private Dictionary<string, Cell> cells = new Dictionary<string, Cell>();
        private Cell selectedCell;
        private GameObject cellContainer;
        private bool isInitialized = false;

        public bool IsInitialized => isInitialized;
        public Dictionary<string, Cell> Cells => cells;
        public Cell SelectedCell => selectedCell;

        public void Initialize()
        {
            try
            {
                LoadResources();
                isInitialized = true;
                Debug.Log("[CellController] 초기화 완료 - 리소스 로드됨");
            }
            catch (Exception e)
            {
                Debug.LogError($"[CellController] 초기화 실패: {e.Message}");
                isInitialized = false;
            }
        }

        private void LoadResources()
        {
            selectedCellMaterial = Resources.Load<Material>("Materials/NoteEditor/SelectedCell");
            cellMaterial = Resources.Load<Material>("Materials/NoteEditor/Cell");
            cellObjectPrefab = Resources.Load<GameObject>("Prefabs/NoteEditor/Model/CellModel");
        }

        public void Setup()
        {
            railController = EditorManager.Instance.railController;
            if (railController == null || !railController.IsInitialized)
            {
                Debug.LogWarning("[CellController] RailController가 초기화되지 않았습니다.");
                return;
            }

            railWidth = railController.RailWidth;
            railSpacing = railController.RailSpacing;

            if (railController.TotalBars <= 0)
            {
                Debug.LogWarning(
                    "[CellController] RailController의 TotalBars가 유효하지 않습니다."
                );
                return;
            }

            CleanupCells();
            CreateCellContainer();
            GenerateCells();

            Debug.Log("[CellController] 셀 설정 완료");
        }

        private void CreateCellContainer()
        {
            if (cellContainer != null)
            {
                Destroy(cellContainer);
            }

            cellContainer = new GameObject("CellContainer");
            cellContainer.transform.parent = transform;
            cellContainer.transform.localPosition = Vector3.zero;
        }

        public void GenerateCells()
        {
            if (railController == null || railController.TotalBars <= 0)
            {
                Debug.LogWarning(
                    "[CellController] RailController가 초기화되지 않았거나 TotalBars 값이 유효하지 않습니다. 셀 생성을 건너뜁니다."
                );
                return;
            }

            cells.Clear();
            selectedCell = null;

            float totalBars = railController.TotalBars;
            float unitsPerBar = railController.UnitsPerBar;

            if (totalBars <= 0 || unitsPerBar <= 0)
            {
                Debug.LogError($"Invalid values: TotalBars={totalBars}, UnitsPerBar={unitsPerBar}");
                return;
            }

            float railLength = totalBars * unitsPerBar;

            float totalWidth = (laneCount * railWidth) + ((laneCount - 1) * railSpacing);
            float startX = -totalWidth / 2f + (railWidth / 2f);

            for (int bar = 0; bar <= Mathf.CeilToInt(totalBars); bar++)
            {
                float barStartPos = (bar / totalBars) * railLength;

                int beatsPerBar = railController.BeatsPerBar;

                for (int beat = 0; beat < beatsPerBar; beat++)
                {
                    float beatPos = barStartPos + (beat * (unitsPerBar / beatsPerBar));

                    float nextBeatPos;
                    if (beat == beatsPerBar - 1)
                    {
                        nextBeatPos = barStartPos + unitsPerBar;
                    }
                    else
                    {
                        nextBeatPos = barStartPos + ((beat + 1) * (unitsPerBar / beatsPerBar));
                    }

                    float middleBeatPos = (beatPos + nextBeatPos) / 2f;

                    for (int lane = 0; lane < laneCount; lane++)
                    {
                        float xPos = startX + (lane * (railWidth + railSpacing));

                        for (int y = 0; y < verticalCellCount; y++)
                        {
                            float yPos =
                                (y - (verticalCellCount - 1) / 2f) * (cellSize + cellSpacing);

                            CreateCell(bar, beat, lane, y, new Vector3(xPos, yPos, middleBeatPos));
                        }
                    }
                }
            }

            Debug.Log($"[CellController] 셀 생성 완료: 총 {cells.Count}개의 셀 생성됨");
        }

        private void CreateCell(int bar, int beat, int lane, int y, Vector3 position)
        {
            string cellKey = $"{bar}_{beat}_{lane}_{y}";

            GameObject cellObj = new GameObject($"Cell_{cellKey}");
            cellObj.transform.parent = cellContainer.transform;

            Vector3 adjustedPosition = position;
            adjustedPosition.y = 1f + position.y;

            cellObj.transform.localPosition = adjustedPosition;
            cellObj.transform.localScale = new Vector3(cellSize, cellSize, cellSize);

            GameObject cellRenderer = Instantiate(cellObjectPrefab, cellObj.transform);
            cellRenderer.transform.localPosition = Vector3.zero;

            SphereCollider collider = cellObj.AddComponent<SphereCollider>();
            collider.isTrigger = true;

            Cell cell = cellObj.AddComponent<Cell>();
            cell.Initialize(bar, beat, new Vector2(lane, y));

            cell.cellRenderer = cellRenderer;

            cells.Add(cellKey, cell);
        }

        public void SelectCell(Cell cell)
        {
            if (cell == null)
                return;

            if (selectedCell != null)
            {
                selectedCell.cellRenderer.GetComponent<Renderer>().material = cellMaterial;
            }

            selectedCell = cell;

            EditorManager.Instance.editorPanel.UpdateSelectedCellInfo(cell);

            if (cell.noteData != null)
            {
                if (cell.noteData.noteType == NoteType.Short)
                {
                    EditorManager.Instance.editorPanel.ToggleShortNoteUI(true);
                    EditorManager.Instance.editorPanel.ToggleLongNoteUI(false);
                }
                else if (cell.noteData.noteType == NoteType.Long)
                {
                    EditorManager.Instance.editorPanel.ToggleShortNoteUI(false);
                    EditorManager.Instance.editorPanel.ToggleLongNoteUI(true);
                }
            }
            else
            {
                selectedCell.cellRenderer.GetComponent<Renderer>().material = selectedCellMaterial;
                EditorManager.Instance.editorPanel.ToggleShortNoteUI(false);
                EditorManager.Instance.editorPanel.ToggleLongNoteUI(false);
            }
        }

        public Cell GetCell(int bar, int beat, int lane, int y)
        {
            string cellKey = $"{bar}_{beat}_{lane}_{y}";
            if (cells.TryGetValue(cellKey, out Cell cell))
            {
                return cell;
            }
            return null;
        }

        private void CleanupCells()
        {
            cells.Clear();
            selectedCell = null;

            if (cellContainer != null)
            {
                Destroy(cellContainer);
                cellContainer = null;
            }
        }

        private void OnDisable()
        {
            CleanupCells();
        }

        public void Cleanup()
        {
            cells.Clear();
            selectedCell = null;

            if (cellContainer != null)
            {
                Destroy(cellContainer);
                cellContainer = null;
            }
        }
    }
}
