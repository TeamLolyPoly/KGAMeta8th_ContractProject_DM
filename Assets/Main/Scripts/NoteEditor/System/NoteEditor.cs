using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NoteEditor
{
    public class NoteEditor : MonoBehaviour, IInitializable
    {
        private RailController railController;
        private CellController cellController;

        private InputActionAsset audioControlActions;

        private EditorManager editorManager;

        private NoteMap noteMap;
        private bool isInitialized = false;

        public bool IsInitialized => isInitialized;

        private bool isCreatingLongNote = false;
        private Cell longNoteStartCell = null;

        private ShortNoteModel shortNoteModelPrefab;
        private LongNoteModel longNoteModelPrefab;

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

                noteMap = new NoteMap();
                noteMap.bpm = 120f;
                noteMap.beatsPerBar = 4;
                noteMap.notes = new List<NoteData>();

                SetActions();

                isInitialized = true;

                if (AudioManager.Instance != null && AudioManager.Instance.currentTrack != null)
                {
                    SetTrack(AudioManager.Instance.currentTrack);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[NoteEditor] 초기화 실패: {e.Message}");
                isInitialized = false;
            }
        }

        public void SetTrack(TrackData track)
        {
            if (track == null)
                return;

            if (
                AudioManager.Instance.currentTrack != null
                && AudioManager.Instance.currentTrack.trackName == track.trackName
            )
            {
                StartCoroutine(LoadNoteMapRoutine(track.trackName));
            }
            else if (track.bpm != noteMap.bpm)
            {
                noteMap.bpm = track.bpm;
                UpdateRailAndCells(track);
            }
        }

        private IEnumerator LoadNoteMapRoutine(string trackName)
        {
            if (EditorDataManager.Instance == null)
            {
                yield break;
            }

            var task = EditorDataManager.Instance.LoadNoteMapAsync(trackName);
            yield return new WaitUntil(() => task.IsCompleted);

            if (task.Result != null)
            {
                noteMap = task.Result;

                if (
                    AudioManager.Instance.currentTrack != null
                    && AudioManager.Instance.currentTrack.trackName == trackName
                )
                {
                    AudioManager.Instance.currentTrack.noteMap = noteMap;
                }
            }
            else
            {
                noteMap = new NoteMap
                {
                    bpm = AudioManager.Instance.CurrentBPM,
                    beatsPerBar = AudioManager.Instance.BeatsPerBar,
                    notes = new List<NoteData>(),
                };
            }

            UpdateRailAndCells(AudioManager.Instance.currentTrack);
            ApplyNotesToCells();
            SaveNoteMap();
        }

        private void UpdateRailAndCells(TrackData track)
        {
            if (track == null || track.TrackAudio == null)
            {
                Debug.LogWarning(
                    "[NoteEditor] 트랙 또는 오디오가 없어 레일과 셀을 업데이트할 수 없습니다."
                );
                return;
            }

            if (railController != null && railController.IsInitialized)
            {
                railController.SetupRail(noteMap.bpm, noteMap.beatsPerBar, track.TrackAudio);
            }

            if (cellController != null && cellController.IsInitialized)
            {
                cellController.Setup();
            }
        }

        private void SetActions()
        {
            audioControlActions = Resources.Load<InputActionAsset>("Input/EditorControls");
            if (audioControlActions != null)
            {
                var actionMap = audioControlActions.FindActionMap("NoteEditor");
                if (actionMap != null)
                {
                    actionMap.FindAction("CreateShortNote").performed += (ctx) =>
                        CreateShortNoteAction();
                    actionMap.FindAction("CreateLongNote").performed += (ctx) =>
                        CreateLongNoteAction();
                    actionMap.FindAction("DeleteNote").performed += (ctx) => DeleteNoteAction();
                }
            }
        }

        private void CreateShortNoteAction()
        {
            if (cellController != null && cellController.SelectedCell != null)
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

                SaveNoteMap();

                editorManager.editorPanel.ToggleShortNoteUI(true);
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

            if (selectedCell.noteData != null && selectedCell.noteData.noteType != NoteType.Long)
            {
                editorManager.editorPanel.UpdateStatusText(
                    "<color=red>롱노트는 비어있는 셀 또는 다른 롱노트가 없는 셀에서만 시작할 수 있습니다.</color>"
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

                SaveNoteMap();

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

        private void DeleteNoteAction()
        {
            if (cellController == null || cellController.SelectedCell == null)
                return;

            if (cellController != null && cellController.SelectedCell != null)
            {
                if (cellController.SelectedCell.noteData != null)
                {
                    DeleteNote(cellController.SelectedCell);

                    editorManager.editorPanel.UpdateStatusText(
                        $"<color=yellow>노트 삭제됨: 마디 {cellController.SelectedCell.bar}, 박자 {cellController.SelectedCell.beat}</color>"
                    );

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

        public void CreateShortNote(Cell cell)
        {
            if (cell == null || noteMap == null)
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

                noteMap.notes.Add(noteData);

                cell.noteData = noteData;
            }
            catch (Exception e)
            {
                Debug.LogError($"노트 생성 실패: {e.Message}");
            }
        }

        public void CreateLongNote(Cell startCell, Cell endCell)
        {
            if (startCell == null || endCell == null || noteMap == null)
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

                noteMap.notes.Add(noteData);
                startCell.noteData = noteData;

                if (longNoteModelPrefab != null)
                {
                    LongNoteModel longNoteModel = Instantiate(
                        longNoteModelPrefab,
                        startCell.transform
                    );

                    if (longNoteModel.gameObject.activeSelf == false)
                    {
                        longNoteModel.gameObject.SetActive(true);
                    }

                    longNoteModel.Initialize(startCell, endCell, noteData);
                    startCell.longNoteModel = longNoteModel;

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

        public void DeleteNote(Cell cell)
        {
            if (cell == null || cell.noteData == null || noteMap == null)
                return;

            try
            {
                noteMap.notes.Remove(cell.noteData);

                if (cell.noteData.noteType == NoteType.Short && cell.noteModel != null)
                {
                    Destroy(cell.noteModel.gameObject);
                    cell.noteModel = null;
                }
                else if (cell.noteData.noteType == NoteType.Long && cell.longNoteModel != null)
                {
                    Destroy(cell.longNoteModel.gameObject);
                    cell.longNoteModel = null;
                }

                cell.noteData = null;
                cell.cellRenderer.SetActive(true);

                SaveNoteMap();
            }
            catch (Exception e)
            {
                Debug.LogError($"노트 삭제 실패: {e.Message}");
            }
        }

        public bool UpdateNoteColor(int index)
        {
            if (noteMap == null)
                return false;

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
            else if (cellController.SelectedCell.noteData.noteType == NoteType.Long)
            {
                if (cellController.SelectedCell.longNoteModel != null)
                {
                    cellController.SelectedCell.longNoteModel.ApplyRodColors();
                }
            }

            SaveNoteMap();
            return true;
        }

        public bool UpdateNoteDirection(int index)
        {
            if (noteMap == null)
                return false;

            if (cellController.SelectedCell == null)
                return false;

            cellController.SelectedCell.noteData.direction = (NoteDirection)index;
            cellController.SelectedCell.noteModel.SetNoteDirection((NoteDirection)index);
            SaveNoteMap();
            return true;
        }

        public bool UpdateNoteSymmetric(bool isSymmetric)
        {
            if (noteMap == null)
                return false;

            if (cellController.SelectedCell == null || cellController.SelectedCell.noteData == null)
                return false;

            if (cellController.SelectedCell.noteData.noteType != NoteType.Long)
                return false;

            cellController.SelectedCell.noteData.isSymmetric = isSymmetric;

            if (cellController.SelectedCell.longNoteModel != null)
            {
                cellController.SelectedCell.longNoteModel.SetSymmetric(isSymmetric);
            }

            SaveNoteMap();
            return true;
        }

        public bool UpdateNoteClockwise(bool isClockwise)
        {
            if (noteMap == null)
                return false;

            if (cellController.SelectedCell == null || cellController.SelectedCell.noteData == null)
                return false;

            if (cellController.SelectedCell.noteData.noteType != NoteType.Long)
                return false;

            cellController.SelectedCell.noteData.isClockwise = isClockwise;

            if (cellController.SelectedCell.longNoteModel != null)
            {
                cellController.SelectedCell.longNoteModel.SetClockwise(isClockwise);
            }

            SaveNoteMap();
            return true;
        }

        public void SaveNoteMap()
        {
            if (noteMap == null)
                return;

            try
            {
                if (AudioManager.Instance.currentTrack != null)
                {
                    string trackName = AudioManager.Instance.currentTrack.trackName;

                    AudioManager.Instance.currentTrack.noteMap = noteMap;

                    if (EditorDataManager.Instance != null)
                    {
                        _ = EditorDataManager.Instance.SaveNoteMapAsync(trackName, noteMap);
                        editorManager.editorPanel.UpdateStatusText(
                            $"노트맵 저장 완료: {trackName}"
                        );
                    }
                }
            }
            catch (Exception e)
            {
                editorManager.editorPanel.UpdateStatusText($"노트맵 저장 실패: {e.Message}");
                Debug.LogError($"노트맵 저장 실패: {e.Message}");
            }
        }

        private void ApplyNotesToCells()
        {
            if (noteMap == null || cellController == null)
                return;

            foreach (var note in noteMap.notes)
            {
                int lane = (int)note.StartCell.x;
                int y = (int)note.StartCell.y;
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

                        int targetLane = (int)note.TargetCell.x;
                        int targetY = (int)note.TargetCell.y;
                        Cell targetCell = null;

                        int endBar = note.bar + note.durationBars;
                        int endBeat = note.beat + note.durationBeats;

                        if (endBeat >= railController.BeatsPerBar)
                        {
                            endBar += endBeat / railController.BeatsPerBar;
                            endBeat = endBeat % railController.BeatsPerBar;
                        }

                        targetCell = cellController.GetCell(endBar, endBeat, targetLane, targetY);

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
            if (noteMap == null)
                return;

            noteMap.bpm = newBpm;

            if (railController != null && railController.IsInitialized)
            {
                railController.UpdateBPM(newBpm);
            }

            if (
                AudioManager.Instance.currentTrack != null
                && AudioManager.Instance.currentTrack.TrackAudio != null
            )
            {
                UpdateRailAndCells(AudioManager.Instance.currentTrack);
            }

            SaveNoteMap();
        }

        public bool UpdateBeatsPerBar(int newBeatsPerBar)
        {
            if (noteMap == null)
                return false;

            noteMap.beatsPerBar = newBeatsPerBar;

            if (railController != null && railController.IsInitialized)
            {
                railController.UpdateBeatsPerBar(newBeatsPerBar);
            }

            if (
                AudioManager.Instance.currentTrack != null
                && AudioManager.Instance.currentTrack.TrackAudio != null
            )
            {
                UpdateRailAndCells(AudioManager.Instance.currentTrack);
            }

            SaveNoteMap();

            return true;
        }

        public void RemoveTrack(TrackData track)
        {
            if (track == null)
                return;

            if (
                AudioManager.Instance.currentTrack == null
                || AudioManager.Instance.currentTrack.trackName == track.trackName
            )
            {
                noteMap = new NoteMap
                {
                    bpm = 120f,
                    beatsPerBar = 4,
                    notes = new List<NoteData>(),
                };

                if (cellController != null)
                {
                    cellController.Cleanup();
                }

                if (railController != null)
                {
                    railController.Cleanup();
                }
            }
        }
    }
}
