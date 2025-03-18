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

        private GridGenerator gridGenerator;

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

                gridGenerator = FindObjectOfType<GridGenerator>();
                if (gridGenerator == null)
                {
                    Debug.LogWarning(
                        "[NoteEditor] GridGenerator를 찾을 수 없습니다. 롱노트의 원형 인덱스 계산에 영향이 있을 수 있습니다."
                    );
                }

                noteMap = new NoteMap();
                noteMap.bpm = 120f;
                noteMap.beatsPerBar = 4;
                noteMap.notes = new List<NoteData>();

                SetActions();

                isInitialized = true;
                Debug.Log("[NoteEditor] 초기화 완료");

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

            Debug.Log($"[NoteEditor] 트랙 설정: {track.trackName}");

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

            Debug.Log($"[NoteEditor] 노트맵 로드 시작: {trackName}");
            var task = EditorDataManager.Instance.LoadNoteMapAsync(trackName);
            yield return new WaitUntil(() => task.IsCompleted);

            if (task.Result != null)
            {
                noteMap = task.Result;
                Debug.Log($"[NoteEditor] 노트맵 파일 로드 완료: {trackName}");

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
                Debug.Log($"[NoteEditor] 새 노트맵 생성: {trackName}");
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

            Debug.Log(
                $"[NoteEditor] 레일 및 셀 업데이트 시작: BPM = {noteMap.bpm}, BeatsPerBar = {noteMap.beatsPerBar}"
            );

            if (railController != null && railController.IsInitialized)
            {
                railController.SetupRail(noteMap.bpm, noteMap.beatsPerBar, track.TrackAudio);
            }

            if (cellController != null && cellController.IsInitialized)
            {
                cellController.Setup();
            }

            Debug.Log("[NoteEditor] 레일 및 셀 업데이트 완료");
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

            if (!isCreatingLongNote)
            {
                longNoteStartCell = cellController.SelectedCell;
                isCreatingLongNote = true;
                editorManager.editorPanel.UpdateStatusText(
                    $"롱노트 시작점 설정: 마디 {longNoteStartCell.bar}, 박자 {longNoteStartCell.beat}. 끝점을 선택하세요."
                );
            }
            else
            {
                Cell endCell = cellController.SelectedCell;
                CreateLongNote(longNoteStartCell, endCell);

                editorManager.editorPanel.UpdateStatusText(
                    $"롱노트 생성됨: 시작 마디 {longNoteStartCell.bar}, 박자 {longNoteStartCell.beat}, 끝 마디 {endCell.bar}, 박자 {endCell.beat}"
                );

                editorManager.editorPanel.UpdateSelectedCellInfo(endCell);

                SaveNoteMap();

                isCreatingLongNote = false;
                longNoteStartCell = null;

                // 롱노트 생성 후 대칭 노트 옵션 UI 표시
                editorManager.editorPanel.ToggleLongNoteUI(true);
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
                    noteAxis = NoteAxis.None,
                    StartCell = cell.cellPosition,
                    TargetCell = cell.cellPosition,
                    isLeftGrid = cell.cellPosition.x < 2,
                    noteSpeed = 1.0f,
                    bar = cell.bar,
                    beat = cell.beat,
                    startIndex = noteMap.notes.Count,
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

                if (durationBars < 0 || (durationBars == 0 && durationBeats < 0))
                {
                    editorManager.editorPanel.UpdateStatusText(
                        "<color=yellow>롱노트의 끝 지점이 시작 지점보다 앞에 있습니다. 위치를 바꿉니다.</color>"
                    );

                    Cell temp = startCell;
                    startCell = endCell;
                    endCell = temp;

                    durationBars = endCell.bar - startCell.bar;
                    durationBeats = endCell.beat - startCell.beat;
                }

                NoteData noteData = new NoteData
                {
                    noteType = NoteType.Long,
                    noteColor = NoteColor.None,
                    direction = NoteDirection.None,
                    noteAxis = NoteAxis.None,
                    StartCell = startCell.cellPosition,
                    TargetCell = endCell.cellPosition,
                    isLeftGrid = startCell.cellPosition.x < 2,
                    noteSpeed = 1.0f,
                    bar = startCell.bar,
                    beat = startCell.beat,
                    startIndex = 0,
                    endIndex = 0,
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
                    longNoteModel.Initialize(startCell, endCell, noteData);

                    longNoteModel.UpdateEndPoint(endCell);

                    startCell.longNoteModel = longNoteModel;
                }
                else
                {
                    Debug.LogWarning("[NoteEditor] LongNoteModel 프리팹을 찾을 수 없습니다.");
                }

                Debug.Log(
                    $"롱노트 생성 완료: 시작 마디 {startCell.bar}, 박자 {startCell.beat}, 지속 시간: {durationBars}마디 {durationBeats}박자"
                );
            }
            catch (Exception e)
            {
                Debug.LogError($"롱노트 생성 실패: {e.Message}");
            }
        }

        public void DeleteNote(Cell cell)
        {
            if (cell == null || cell.noteData == null || noteMap == null)
                return;

            try
            {
                noteMap.notes.Remove(cell.noteData);

                // 노트 모델 삭제
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

                Debug.Log($"노트 삭제 완료: 마디 {cell.bar}, 박자 {cell.beat}");

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
            cellController.SelectedCell.noteData.noteColor = noteColor;

            if (cellController.SelectedCell.noteData.noteType == NoteType.Short)
            {
                cellController.SelectedCell.noteModel.SetNoteColor(noteColor);
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

            // 시각적 업데이트
            if (cellController.SelectedCell.longNoteModel != null)
            {
                cellController.SelectedCell.longNoteModel.SetSymmetric(isSymmetric);
            }

            SaveNoteMap();

            Debug.Log($"롱노트 대칭 옵션 변경: {isSymmetric}");
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

            // 시각적 업데이트
            if (cellController.SelectedCell.longNoteModel != null)
            {
                cellController.SelectedCell.longNoteModel.SetClockwise(isClockwise);
            }

            SaveNoteMap();

            Debug.Log($"롱노트 회전 방향 변경: {(isClockwise ? "시계 방향" : "반시계 방향")}");
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
                        Debug.Log($"노트맵 저장 완료: {trackName}");
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
                                cell.longNoteModel = longNoteModel;
                                longNoteModel.Initialize(cell, targetCell, note);

                                // UpdateEndPoint를 사용하여 끝점 업데이트
                                longNoteModel.UpdateEndPoint(targetCell);

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

            Debug.Log($"[NoteEditor] 노트 적용 완료: {noteMap.notes.Count}개의 노트");
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

            Debug.Log($"NoteEditor: Track removed: {track.trackName}");

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
