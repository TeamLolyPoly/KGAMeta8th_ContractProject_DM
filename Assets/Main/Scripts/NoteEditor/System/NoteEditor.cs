using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NoteEditor
{
    public class NoteEditor : MonoBehaviour, IInitializable
    {
        private RailController railController;
        private CellController cellController;
        private InputActionAsset editorControlActions;
        private EditorManager editorManager;
        private bool isInitialized = false;
        public bool IsInitialized => isInitialized;
        private bool isCreatingLongNote = false;
        private Cell longNoteStartCell = null;

        private ShortNoteModel shortNoteModelPrefab;
        private LongNoteModel longNoteModelPrefab;

        // 이벤트 핸들러를 저장할 변수들
        private System.Action<InputAction.CallbackContext> createShortNoteHandler;
        private System.Action<InputAction.CallbackContext> createLongNoteHandler;
        private System.Action<InputAction.CallbackContext> deleteNoteHandler;

        public void Initialize()
        {
            try
            {
                shortNoteModelPrefab = Resources.Load<ShortNoteModel>(
                    "Prefabs/NoteEditor/Model/ShortNoteModel"
                );
                longNoteModelPrefab = Resources.Load<LongNoteModel>(
                    "Prefabs/NoteEditor/Model/LongNoteModel"
                );
                editorManager = EditorManager.Instance;
                railController = editorManager.railController;
                cellController = editorManager.cellController;

                SetActions();

                isInitialized = true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[NoteEditor] 초기화 실패: {e.Message}");
                isInitialized = false;
            }
        }

        public void SetTrack()
        {
            UpdateRailAndCells();

            ApplyNotesToCells();
        }

        private void UpdateRailAndCells()
        {
            if (AudioManager.Instance.currentAudioSource.clip == null)
            {
                Debug.LogWarning(
                    "[NoteEditor] 트랙 또는 오디오가 없어 레일과 셀을 업데이트할 수 없습니다."
                );
                return;
            }

            if (railController != null && railController.IsInitialized)
            {
                railController.SetupRail();
            }

            if (cellController != null && cellController.IsInitialized)
            {
                cellController.Setup();
            }
        }

        public void Cleanup()
        {
            if (editorControlActions != null)
            {
                var actionMap = editorControlActions.FindActionMap("NoteEditor");
                if (actionMap != null)
                {
                    if (createShortNoteHandler != null)
                        actionMap.FindAction("CreateShortNote").performed -= createShortNoteHandler;

                    if (createLongNoteHandler != null)
                        actionMap.FindAction("CreateLongNote").performed -= createLongNoteHandler;

                    if (deleteNoteHandler != null)
                        actionMap.FindAction("DeleteNote").performed -= deleteNoteHandler;
                }
            }

            createShortNoteHandler = null;
            createLongNoteHandler = null;
            deleteNoteHandler = null;
        }

        private void SetActions()
        {
            editorControlActions = Resources.Load<InputActionAsset>("Input/EditorControls");
            if (editorControlActions != null)
            {
                var actionMap = editorControlActions.FindActionMap("NoteEditor");
                if (actionMap != null)
                {
                    createShortNoteHandler = (ctx) => CreateShortNoteAction();
                    createLongNoteHandler = (ctx) => CreateLongNoteAction();
                    deleteNoteHandler = (ctx) => DeleteNoteAction();

                    actionMap.FindAction("CreateShortNote").performed += createShortNoteHandler;
                    actionMap.FindAction("CreateLongNote").performed += createLongNoteHandler;
                    actionMap.FindAction("DeleteNote").performed += deleteNoteHandler;
                }
            }
        }

        private void CreateShortNoteAction()
        {
            if (
                cellController != null
                && cellController.SelectedCell != null
                && cellController.SelectedCell.isOccupied == false
            )
            {
                CreateShortNote(cellController.SelectedCell);

                ShortNoteModel noteModel = Instantiate(
                    shortNoteModelPrefab,
                    cellController.SelectedCell.transform
                );
                cellController.SelectedCell.noteModel = noteModel;

                cellController.SelectedCell.cellRenderer.SetActive(false);

                editorManager.editorPanel.UpdateStatusText(
                    $"노트 생성됨: 마디 {cellController.SelectedCell.bar}, 박자 {cellController.SelectedCell.beat}"
                );

                editorManager.editorPanel.UpdateSelectedCellInfo(cellController.SelectedCell);

                editorManager.SaveNoteMapAsync();

                editorManager.editorPanel.ToggleShortNoteUI(true);
            }
            else if (cellController.SelectedCell.isOccupied)
            {
                editorManager.editorPanel.UpdateStatusText(
                    "<color=red>이미 노트가 있는 셀입니다.</color>"
                );
            }
            else
            {
                editorManager.editorPanel.UpdateStatusText("노트를 생성할 셀을 선택하세요");
            }
        }

        private void CreateLongNoteAction()
        {
            if (cellController == null || cellController.SelectedCell == null)
            {
                editorManager.editorPanel.UpdateStatusText("롱노트를 생성할 셀을 선택하세요");
                return;
            }

            Cell selectedCell = cellController.SelectedCell;

            if (
                selectedCell.cellPosition.x < 2
                || selectedCell.cellPosition.x > 4
                || selectedCell.cellPosition.y < 0
                || selectedCell.cellPosition.y > 2
            )
            {
                editorManager.editorPanel.UpdateStatusText(
                    "<color=red>롱노트는 오른쪽 3x3 그리드 내에서만 생성할 수 있습니다 (x: 2-4, y: 0-2)</color>"
                );
                return;
            }

            if (selectedCell.isOccupied)
            {
                editorManager.editorPanel.UpdateStatusText(
                    "<color=red>비어있지 않은 셀입니다</color>"
                );
                return;
            }

            if (isCreatingLongNote)
            {
                Cell endCell = selectedCell;

                CreateLongNote(longNoteStartCell, endCell);

                editorManager.editorPanel.UpdateStatusText(
                    $"롱노트 생성됨: 시작 마디 {longNoteStartCell.bar}, 박자 {longNoteStartCell.beat}, 끝 마디 {endCell.bar}, 박자 {endCell.beat}"
                );

                editorManager.editorPanel.UpdateSelectedCellInfo(endCell);

                editorManager.SaveNoteMapAsync();

                isCreatingLongNote = false;
                longNoteStartCell = null;

                editorManager.editorPanel.ToggleLongNoteUI(true);
            }
            else
            {
                longNoteStartCell = selectedCell;
                isCreatingLongNote = true;
                editorManager.editorPanel.UpdateStatusText(
                    "<color=yellow>롱노트 시작점이 설정되었습니다. 끝점을 선택해주세요.</color>"
                );
            }
        }

        public void CreateShortNote(Cell cell)
        {
            if (cell == null)
                return;

            try
            {
                NoteData noteData = new NoteData
                {
                    noteType = NoteType.Short,
                    noteColor = NoteColor.None,
                    direction = NoteDirection.None,
                    noteAxis = NoteAxis.PZ,
                    StartCell = cell.cellPosition,
                    TargetCell = cell.cellPosition,
                    bar = cell.bar,
                    beat = cell.beat,
                    isSymmetric = false,
                    isClockwise = false,
                };

                EditorManager.Instance.CurrentNoteMap.notes.Add(noteData);

                cell.noteData = noteData;
                cell.isOccupied = true;
            }
            catch (Exception e)
            {
                Debug.LogError($"노트 생성 실패: {e.Message}");
            }
        }

        public void CreateLongNote(Cell startCell, Cell endCell)
        {
            if (startCell == null || endCell == null)
                return;

            try
            {
                int durationBars = endCell.bar - startCell.bar;
                int durationBeats = endCell.beat - startCell.beat;

                if (durationBars == 0 && durationBeats == 0)
                {
                    editorManager.editorPanel.UpdateStatusText(
                        "<color=red>롱노트는 서로 다른 박자 위치에서만 생성할 수 있습니다.</color>"
                    );
                    return;
                }

                bool swapped = false;
                if (durationBars < 0 || (durationBars == 0 && durationBeats < 0))
                {
                    editorManager.editorPanel.UpdateStatusText(
                        "<color=yellow>롱노트의 끝 지점이 시작 지점보다 앞에 있습니다. 위치를 바꿉니다.</color>"
                    );

                    Cell temp = startCell;
                    startCell = endCell;
                    endCell = temp;
                    swapped = true;

                    durationBars = endCell.bar - startCell.bar;
                    durationBeats = endCell.beat - startCell.beat;
                }

                if (durationBeats < 0)
                {
                    durationBars--;
                    durationBeats += AudioManager.Instance.BeatsPerBar;
                }

                bool isStartCellInGrid = !(
                    startCell.cellPosition.x < 2
                    || startCell.cellPosition.x > 4
                    || startCell.cellPosition.y < 0
                    || startCell.cellPosition.y > 2
                );

                if (!isStartCellInGrid)
                {
                    if (swapped)
                    {
                        editorManager.editorPanel.UpdateStatusText(
                            "<color=red>롱노트 시작점이 3x3 그리드 밖에 있습니다. 시작점과 끝점을 바꿨을 때도 시작점이 그리드 안에 있어야 합니다.</color>"
                        );
                    }
                    else
                    {
                        editorManager.editorPanel.UpdateStatusText(
                            "<color=red>롱노트는 오른쪽 3x3 그리드 내에서만 생성할 수 있습니다 (x: 2-4, y: 0-2)</color>"
                        );
                    }
                    return;
                }

                int startIndex = CalculateCellIndex(startCell.cellPosition);
                int endIndex = CalculateCellIndex(endCell.cellPosition);

                NoteData noteData = new NoteData
                {
                    noteType = NoteType.Long,
                    noteColor = NoteColor.None,
                    direction = NoteDirection.None,
                    noteAxis = NoteAxis.PZ,
                    StartCell = startCell.cellPosition,
                    TargetCell = endCell.cellPosition,
                    bar = startCell.bar,
                    beat = startCell.beat,
                    startIndex = startIndex,
                    endIndex = endIndex,
                    isSymmetric = false,
                    isClockwise = true,
                    durationBars = durationBars,
                    durationBeats = durationBeats,
                };

                EditorManager.Instance.CurrentNoteMap.notes.Add(noteData);
                startCell.noteData = noteData;

                if (longNoteModelPrefab != null)
                {
                    LongNoteModel longNoteModel = Instantiate(
                        longNoteModelPrefab,
                        startCell.transform
                    );

                    longNoteModel.Initialize(startCell, endCell, noteData);
                    startCell.isOccupied = true;
                    endCell.isOccupied = true;

                    startCell.longNoteModel = longNoteModel;
                    endCell.longNoteModel = longNoteModel;
                    startCell.cellRenderer.SetActive(false);
                }
                else
                {
                    Debug.LogWarning("[NoteEditor] LongNoteModel 프리팹을 찾을 수 없습니다.");
                }

                print(
                    $"=========롱노트 생성됨=======\n"
                        + $"시작 마디 {startCell.bar}, 박자 {startCell.beat}\n"
                        + $"끝 마디 {endCell.bar}, 박자 {endCell.beat}\n"
                        + $"롱노트 길이: {durationBars}마디 {durationBeats}박자\n"
                        + $"롱노트 시작 인덱스: {startIndex}, 끝 인덱스: {endIndex}\n"
                );
            }
            catch (Exception e)
            {
                Debug.LogError($"롱노트 생성 실패: {e.Message}");
                Debug.LogError($"[DEBUG] 예외 발생 - 스택 트레이스: {e.StackTrace}");
            }
        }

        private int CalculateCellIndex(Vector2Int cellPosition)
        {
            int gridX = cellPosition.x;
            int gridY = cellPosition.y;

            float normX = Mathf.Clamp((gridX / (float)(5 - 1)) * 2 - 1, -1, 1);
            float normY = Mathf.Clamp((gridY / (float)(3 - 1)) * 2 - 1, -1, 1);

            float angle = Mathf.Atan2(normY, normX) * Mathf.Rad2Deg;
            if (angle < 0)
                angle += 360f;

            int segmentCount = 72;

            int index = Mathf.RoundToInt(angle / (360f / segmentCount)) % segmentCount;

            return index;
        }

        private void DeleteNoteAction()
        {
            if (cellController == null || cellController.SelectedCell == null)
                return;

            if (cellController != null && cellController.SelectedCell != null)
            {
                if (cellController.SelectedCell.noteData != null)
                {
                    DeleteNote(cellController.SelectedCell);

                    editorManager.editorPanel.UpdateSelectedCellInfo(cellController.SelectedCell);

                    editorManager.editorPanel.ToggleShortNoteUI(false);
                    editorManager.editorPanel.ToggleLongNoteUI(false);
                }
                else
                {
                    editorManager.editorPanel.UpdateStatusText(
                        "<color=red>선택한 셀에 노트가 없습니다</color>"
                    );
                }
            }
            else
            {
                editorManager.editorPanel.UpdateStatusText(
                    "<color=yellow>삭제할 노트가 있는 셀을 선택하세요</color>"
                );
            }
        }

        public void DeleteNote(Cell cell)
        {
            if (cell == null || cell.noteData == null)
                return;

            try
            {
                EditorManager.Instance.CurrentNoteMap.notes.Remove(cell.noteData);

                if (cell.noteData.noteType == NoteType.Short && cell.noteModel != null)
                {
                    DeleteShortNote(cell);
                }
                else if (cell.noteData.noteType == NoteType.Long && cell.longNoteModel != null)
                {
                    DeleteLongNote(cell);
                }

                cell.noteData = null;
                cell.cellRenderer.SetActive(true);

                editorManager.SaveNoteMapAsync();
            }
            catch (Exception e)
            {
                Debug.LogError($"노트 삭제 실패: {e.Message}");
            }
        }

        private void DeleteLongNote(Cell cell)
        {
            Cell endCell = cell.longNoteModel.endCell;
            bool isSymmetric = cell.noteData.isSymmetric;
            GameObject symmetricObject = null;

            if (isSymmetric && cell.longNoteModel.symmetricObject != null)
            {
                symmetricObject = cell.longNoteModel.symmetricObject;
            }

            UpdateSymmetricNote(
                cell.longNoteModel.startCell,
                cell.longNoteModel.endCell,
                cell.noteData,
                false
            );

            Destroy(cell.longNoteModel.gameObject);
            cell.longNoteModel = null;
            cell.isOccupied = false;

            if (endCell != null)
            {
                endCell.isOccupied = false;
            }

            print($"isSymmetric: {isSymmetric}, symmetricObject: {symmetricObject}");
        }

        private void DeleteShortNote(Cell cell)
        {
            Destroy(cell.noteModel.gameObject);
            cell.noteModel = null;
            cell.isOccupied = false;
            editorManager.editorPanel.UpdateStatusText(
                $"<color=yellow>노트 삭제됨: 마디 {cell.bar}, 박자 {cell.beat}</color>"
            );
        }

        public bool UpdateNoteColor(int index)
        {
            if (cellController.SelectedCell == null)
                return false;

            NoteColor noteColor = (NoteColor)index;

            if (cellController.SelectedCell.noteData.noteType == NoteType.Short)
            {
                cellController.SelectedCell.noteData.noteColor = noteColor;

                if (cellController.SelectedCell.noteModel != null)
                {
                    cellController.SelectedCell.noteModel.SetNoteColor(noteColor);
                }
            }

            editorManager.SaveNoteMapAsync();
            return true;
        }

        public bool UpdateNoteDirection(int index)
        {
            if (cellController.SelectedCell == null)
                return false;

            cellController.SelectedCell.noteData.direction = (NoteDirection)index;
            cellController.SelectedCell.noteModel.SetNoteDirection((NoteDirection)index);
            editorManager.SaveNoteMapAsync();
            return true;
        }

        public bool UpdateNoteSymmetric(bool isSymmetric)
        {
            if (cellController.SelectedCell == null || cellController.SelectedCell.noteData == null)
                return false;

            if (cellController.SelectedCell.noteData.noteType != NoteType.Long)
                return false;

            cellController.SelectedCell.noteData.isSymmetric = isSymmetric;

            if (cellController.SelectedCell.longNoteModel != null)
            {
                cellController.SelectedCell.longNoteModel.SetSymmetric(isSymmetric);
                LongNoteModel longNoteModel = cellController.SelectedCell.longNoteModel;

                longNoteModel.startCell.noteData.isSymmetric = isSymmetric;

                GameObject symmetricModel = UpdateSymmetricNote(
                    longNoteModel.startCell,
                    longNoteModel.endCell,
                    cellController.SelectedCell.noteData,
                    isSymmetric
                );
                longNoteModel.symmetricObject = symmetricModel;
            }

            editorManager.SaveNoteMapAsync();
            return true;
        }

        public GameObject UpdateSymmetricNote(
            Cell originalStart,
            Cell orignialTarget,
            NoteData noteData,
            bool createSymmetric
        )
        {
            if (originalStart == null || orignialTarget == null || noteData == null)
                return null;

            string symmetricVisualName = $"SymmetricNote_{originalStart.name}";
            GameObject existingSymmetricModel = originalStart.longNoteModel.symmetricObject;
            if (existingSymmetricModel != null)
            {
                Destroy(existingSymmetricModel);
            }

            if (createSymmetric)
            {
                if (longNoteModelPrefab != null)
                {
                    Vector2Int centerGrid = new Vector2Int(3, 1);

                    Vector2Int symStartPos = new Vector2Int(
                        2 * centerGrid.x - originalStart.cellPosition.x,
                        2 * centerGrid.y - originalStart.cellPosition.y
                    );

                    Vector2Int symEndPos = new Vector2Int(
                        2 * centerGrid.x - orignialTarget.cellPosition.x,
                        2 * centerGrid.y - orignialTarget.cellPosition.y
                    );

                    Cell symmetricStartCell = GetCellByPosition(symStartPos);
                    Cell symmetricEndCell = GetCellByPosition(symEndPos);

                    if (symmetricStartCell == null || symmetricEndCell == null)
                    {
                        Debug.LogWarning(
                            $"대칭 노트를 위한 셀을 찾을 수 없습니다. "
                                + $"시작점: ({symStartPos.x}, {symStartPos.y}), "
                                + $"끝점: ({symEndPos.x}, {symEndPos.y})"
                        );
                        return null;
                    }

                    int durationBars = noteData.durationBars;
                    int durationBeats = noteData.durationBeats;

                    int endBar = symmetricStartCell.bar + durationBars;
                    int endBeat = symmetricStartCell.beat + durationBeats;

                    if (endBeat >= AudioManager.Instance.BeatsPerBar)
                    {
                        endBar += endBeat / AudioManager.Instance.BeatsPerBar;
                        endBeat = endBeat % AudioManager.Instance.BeatsPerBar;
                    }

                    symmetricEndCell = cellController.GetCell(
                        endBar,
                        endBeat,
                        symEndPos.x,
                        symEndPos.y
                    );

                    if (symmetricEndCell == null)
                    {
                        Debug.LogWarning(
                            $"대칭 노트의 끝점 셀을 찾을 수 없습니다: 마디 {endBar}, 박자 {endBeat}, 위치 ({symEndPos.x}, {symEndPos.y})"
                        );
                        return null;
                    }

                    LongNoteModel symmetricNoteModel = Instantiate(
                        longNoteModelPrefab,
                        symmetricStartCell.transform
                    );

                    symmetricEndCell.isOccupied = true;
                    symmetricEndCell.cellRenderer.SetActive(false);
                    symmetricStartCell.isOccupied = true;
                    symmetricStartCell.cellRenderer.SetActive(false);

                    symmetricNoteModel.gameObject.name = symmetricVisualName;

                    symmetricNoteModel.Initialize(symmetricStartCell, symmetricEndCell);

                    return symmetricNoteModel.gameObject;
                }
                else
                {
                    Debug.LogWarning("롱노트 모델 프리팹이 없어 대칭 노트를 생성할 수 없습니다.");
                    return null;
                }
            }

            return null;
        }

        private Cell GetCellByPosition(Vector2Int position)
        {
            foreach (var cell in cellController.Cells.Values)
            {
                if (cell.cellPosition == position)
                    return cell;
            }
            return null;
        }

        public bool UpdateNoteClockwise(bool isClockwise)
        {
            if (cellController.SelectedCell == null || cellController.SelectedCell.noteData == null)
                return false;

            if (cellController.SelectedCell.noteData.noteType != NoteType.Long)
                return false;

            cellController.SelectedCell.noteData.isClockwise = isClockwise;

            if (cellController.SelectedCell.longNoteModel != null)
            {
                cellController.SelectedCell.longNoteModel.SetClockwise(isClockwise);
            }

            editorManager.SaveNoteMapAsync();
            return true;
        }

        private void ApplyNotesToCells()
        {
            foreach (var note in EditorManager.Instance.CurrentNoteMap.notes)
            {
                int lane = note.StartCell.x;
                int y = note.StartCell.y;
                Cell cell = cellController.GetCell(note.bar, note.beat, lane, y);

                if (cell != null)
                {
                    if (note.noteType == NoteType.Short)
                    {
                        cell.noteData = note;
                        ShortNoteModel noteModel = Instantiate(
                            shortNoteModelPrefab,
                            cell.transform
                        );
                        cell.noteModel = noteModel;
                        noteModel.SetNoteColor(note.noteColor);
                        noteModel.SetNoteDirection(note.direction);
                        cell.cellRenderer.SetActive(false);
                    }
                    else if (note.noteType == NoteType.Long)
                    {
                        if (
                            note.StartCell.x < 2
                            || note.StartCell.x > 4
                            || note.StartCell.y < 0
                            || note.StartCell.y > 2
                        )
                        {
                            Debug.LogWarning(
                                $"[NoteEditor] 3x3 그리드 외부에 있는 롱노트는 로드하지 않습니다: 마디 {note.bar}, 박자 {note.beat}"
                            );
                            continue;
                        }

                        cell.noteData = note;

                        int targetLane = note.TargetCell.x;
                        int targetY = note.TargetCell.y;

                        int endBar = note.bar + note.durationBars;
                        int endBeat = note.beat + note.durationBeats;

                        if (endBeat >= AudioManager.Instance.BeatsPerBar)
                        {
                            endBar += endBeat / AudioManager.Instance.BeatsPerBar;
                            endBeat = endBeat % AudioManager.Instance.BeatsPerBar;
                        }

                        Cell targetCell = cellController.GetCell(
                            endBar,
                            endBeat,
                            targetLane,
                            targetY
                        );

                        if (targetCell != null)
                        {
                            if (longNoteModelPrefab != null)
                            {
                                LongNoteModel longNoteModel = Instantiate(
                                    longNoteModelPrefab,
                                    cell.transform
                                );

                                if (longNoteModel.gameObject.activeSelf == false)
                                {
                                    longNoteModel.gameObject.SetActive(true);
                                }

                                cell.longNoteModel = longNoteModel;
                                longNoteModel.Initialize(cell, targetCell, note);

                                longNoteModel.SetSymmetric(note.isSymmetric);
                                longNoteModel.SetClockwise(note.isClockwise);

                                cell.cellRenderer.SetActive(false);
                            }
                        }
                        else
                        {
                            Debug.LogWarning(
                                $"롱노트의 타겟 셀을 찾을 수 없습니다: 마디 {endBar}, 박자 {endBeat}, 레인 {targetLane}, Y {targetY}"
                            );
                        }
                    }
                }
            }
        }

        public void UpdateBPM(float newBpm)
        {
            EditorManager.Instance.CurrentNoteMap.bpm = newBpm;

            AudioManager.Instance.CurrentBPM = newBpm;

            if (railController != null && railController.IsInitialized)
            {
                railController.UpdateBPM();
            }

            if (cellController != null && cellController.IsInitialized)
            {
                cellController.Setup();
            }
        }

        public bool UpdateBeatsPerBar(int newBeatsPerBar)
        {
            if (AudioManager.Instance.currentAudioSource.clip == null)
                return false;

            EditorManager.Instance.CurrentNoteMap.beatsPerBar = newBeatsPerBar;

            AudioManager.Instance.BeatsPerBar = newBeatsPerBar;

            railController.SetupRail();

            cellController.Setup();

            EditorManager.Instance.CurrentNoteMap = new NoteMap()
            {
                bpm = EditorManager.Instance.CurrentNoteMap.bpm,
                beatsPerBar = newBeatsPerBar,
            };

            EditorManager.Instance.CurrentNoteMapData.noteMap = EditorManager
                .Instance
                .CurrentNoteMap;

            EditorManager
                .Instance.CurrentTrack.noteMapData.Find(
                    (noteMapData) =>
                        noteMapData.difficulty
                        == EditorManager.Instance.CurrentNoteMapData.difficulty
                )
                .noteMap = EditorManager.Instance.CurrentNoteMap;

            editorManager.SaveNoteMapAsync();

            return true;
        }
    }
}
