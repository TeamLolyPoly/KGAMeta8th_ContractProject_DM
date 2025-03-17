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
            GetResources();
            railController = EditorManager.Instance.railController;

            if (railController != null)
            {
                railWidth = 1f;
                railSpacing = 0.1f;
            }

            if (isInitialized)
            {
                CleanupCells();
            }

            try
            {
                CreateCellContainer();
                GenerateCells();
                isInitialized = true;
            }
            catch (Exception e)
            {
                Debug.LogError($"셀 생성 실패: {e.Message}");
                CleanupCells();
            }
        }

        private void GetResources()
        {
            cellMaterial = Resources.Load<Material>("Materials/NoteEditor/Cell");
            selectedCellMaterial = Resources.Load<Material>("Materials/NoteEditor/SelectedCell");
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

        private void GenerateCells()
        {
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

                int beatsPerBar =
                    AudioManager.Instance != null ? AudioManager.Instance.BeatsPerBar : 4;

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
        }

        private void CreateCell(int bar, int beat, int lane, int y, Vector3 position)
        {
            string cellKey = $"{bar}_{beat}_{lane}_{y}";

            GameObject cellObj = new GameObject($"Cell_{cellKey}");
            cellObj.transform.parent = cellContainer.transform;

            Vector3 adjustedPosition = position;
            adjustedPosition.y = 1f + position.y;

            cellObj.transform.localPosition = adjustedPosition;

            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.transform.parent = cellObj.transform;
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale = new Vector3(cellSize, cellSize, cellSize);

            MeshRenderer renderer = (MeshRenderer)visual.GetComponent<Renderer>();
            if (cellMaterial != null)
            {
                renderer.material = cellMaterial;
            }

            Cell cell = cellObj.AddComponent<Cell>();
            cell.Initialize(bar, beat, new Vector2(lane, y));

            SphereCollider collider = visual.GetComponent<SphereCollider>();
            if (collider != null)
            {
                collider.isTrigger = true;
            }

            cells.Add(cellKey, cell);
        }

        public void SelectCell(Cell cell)
        {
            if (selectedCell != null)
            {
                MeshRenderer renderer = selectedCell.GetComponentInChildren<MeshRenderer>();
                if (renderer != null && cellMaterial != null)
                {
                    renderer.material = cellMaterial;
                }
            }

            selectedCell = cell;

            if (selectedCell != null)
            {
                MeshRenderer renderer = selectedCell.GetComponentInChildren<MeshRenderer>();
                if (renderer != null && selectedCellMaterial != null)
                {
                    renderer.material = selectedCellMaterial;
                }
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

            isInitialized = false;
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

            isInitialized = false;
        }
    }
}
