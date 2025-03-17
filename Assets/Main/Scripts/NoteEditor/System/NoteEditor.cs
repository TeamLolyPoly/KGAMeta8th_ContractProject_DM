using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NoteEditor
{
    public class NoteEditor : MonoBehaviour, IInitializable
    {
        private RailController railController;
        private CellController cellController;

        private InputActionAsset audioControlActions;

        private NoteEditorPanel noteEditorPanel;
        private EditorManager editorManager;

        private NoteMap noteMap;
        private bool isInitialized = false;

        public bool IsInitialized => isInitialized;
        public NoteMap NoteMap => noteMap;

        private bool isCreatingLongNote = false;
        private Cell longNoteStartCell = null;

        public void Initialize()
        {
            try
            {
                editorManager = EditorManager.Instance;
                railController = editorManager.railController;
                cellController = editorManager.cellController;
                noteEditorPanel = editorManager.editorPanel;

                noteMap = new NoteMap();
                noteMap.bpm = 120f;
                noteMap.beatsPerBar = 4;
                noteMap.notes = new List<NoteData>();

                SetActions();

                isInitialized = true;
                Debug.Log("[NoteEditor] 초기화 완료");

                // 초기화 후 현재 트랙이 있으면 설정
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
            AutoSaveNoteMap();
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
                    actionMap.FindAction("CreateNote").performed += (ctx) => CreateNoteAction();
                    actionMap.FindAction("CreateLongNote").performed += (ctx) =>
                        CreateLongNoteAction();
                    actionMap.FindAction("DeleteNote").performed += (ctx) => DeleteNoteAction();
                }
            }
        }

        private void CreateNoteAction()
        {
            if (cellController != null && cellController.SelectedCell != null)
            {
                CreateNote(cellController.SelectedCell);

                noteEditorPanel.UpdateStatusText(
                    $"노트 생성됨: 마디 {cellController.SelectedCell.bar}, 박자 {cellController.SelectedCell.beat}"
                );
            }
            else
            {
                noteEditorPanel.UpdateStatusText("노트를 생성할 셀을 선택하세요");
            }
        }

        private void CreateLongNoteAction()
        {
            if (cellController == null || cellController.SelectedCell == null)
            {
                noteEditorPanel.UpdateStatusText("롱노트를 생성할 셀을 선택하세요");
                return;
            }

            if (!isCreatingLongNote)
            {
                longNoteStartCell = cellController.SelectedCell;
                isCreatingLongNote = true;
                noteEditorPanel.UpdateStatusText(
                    $"롱노트 시작점 설정: 마디 {longNoteStartCell.bar}, 박자 {longNoteStartCell.beat}. 끝점을 선택하세요."
                );
            }
            else
            {
                Cell endCell = cellController.SelectedCell;
                CreateLongNote(longNoteStartCell, endCell);

                noteEditorPanel.UpdateStatusText(
                    $"롱노트 생성됨: 시작 마디 {longNoteStartCell.bar}, 박자 {longNoteStartCell.beat}, 끝 마디 {endCell.bar}, 박자 {endCell.beat}"
                );

                isCreatingLongNote = false;
                longNoteStartCell = null;
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
                    noteEditorPanel.UpdateStatusText(
                        $"노트 삭제됨: 마디 {cellController.SelectedCell.bar}, 박자 {cellController.SelectedCell.beat}"
                    );
                }
                else
                {
                    noteEditorPanel.UpdateStatusText("선택한 셀에 노트가 없습니다");
                }
            }
            else
            {
                noteEditorPanel.UpdateStatusText("삭제할 노트가 있는 셀을 선택하세요");
            }
        }

        public void CreateNote(Cell cell)
        {
            if (cell == null || noteMap == null)
                return;

            try
            {
                NoteData noteData = new NoteData
                {
                    baseType = NoteBaseType.None,
                    noteType = NoteHitType.None,
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

                Debug.Log(
                    $"노트 생성 완료: 마디 {cell.bar}, 박자 {cell.beat}, 위치 {cell.cellPosition}"
                );
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
                    Debug.LogWarning(
                        "롱노트의 끝 지점이 시작 지점보다 앞에 있습니다. 위치를 바꿉니다."
                    );
                    Cell temp = startCell;
                    startCell = endCell;
                    endCell = temp;

                    durationBars = endCell.bar - startCell.bar;
                    durationBeats = endCell.beat - startCell.beat;
                }

                NoteData noteData = new NoteData
                {
                    baseType = NoteBaseType.Long,
                    noteType = NoteHitType.None,
                    direction = NoteDirection.None,
                    noteAxis = NoteAxis.None,
                    StartCell = startCell.cellPosition,
                    TargetCell = endCell.cellPosition,
                    isLeftGrid = startCell.cellPosition.x < 2,
                    noteSpeed = 1.0f,
                    bar = startCell.bar,
                    beat = startCell.beat,
                    startIndex = noteMap.notes.Count,
                    isSymmetric = false,
                    isClockwise = false,
                    durationBars = durationBars,
                    durationBeats = durationBeats,
                };

                noteMap.notes.Add(noteData);

                startCell.noteData = noteData;

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

                cell.noteData = null;

                Debug.Log($"노트 삭제 완료: 마디 {cell.bar}, 박자 {cell.beat}");
            }
            catch (Exception e)
            {
                Debug.LogError($"노트 삭제 실패: {e.Message}");
            }
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
                        noteEditorPanel?.UpdateStatusText($"노트맵 저장 완료: {trackName}");
                        Debug.Log($"노트맵 저장 완료: {trackName}");
                    }
                }
            }
            catch (Exception e)
            {
                noteEditorPanel?.UpdateStatusText($"노트맵 저장 실패: {e.Message}");
                Debug.LogError($"노트맵 저장 실패: {e.Message}");
            }
        }

        public void AutoSaveNoteMap()
        {
            if (noteMap == null || AudioManager.Instance.currentTrack == null)
                return;

            try
            {
                string trackName = AudioManager.Instance.currentTrack.trackName;

                AudioManager.Instance.currentTrack.noteMap = noteMap;

                if (EditorDataManager.Instance != null)
                {
                    _ = EditorDataManager.Instance.SaveNoteMapAsync(trackName, noteMap);
                    Debug.Log($"노트맵 자동 저장 완료: {trackName}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"노트맵 자동 저장 실패: {e.Message}");
            }
        }

        public void LoadNoteMap()
        {
            try
            {
                if (AudioManager.Instance.currentTrack != null)
                {
                    string trackName = AudioManager.Instance.currentTrack.trackName;

                    if (EditorDataManager.Instance != null)
                    {
                        StartCoroutine(LoadNoteMapCoroutine(trackName));
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"노트맵 로드 실패: {e.Message}");
            }
        }

        private IEnumerator LoadNoteMapCoroutine(string trackName)
        {
            var task = EditorDataManager.Instance.LoadNoteMapAsync(trackName);
            yield return new WaitUntil(() => task.IsCompleted);

            if (task.Result != null)
            {
                noteMap = task.Result;
                Debug.Log($"노트맵 로드 완료: {trackName}");
            }
            else
            {
                noteMap = new NoteMap
                {
                    bpm = AudioManager.Instance.CurrentBPM,
                    beatsPerBar = AudioManager.Instance.BeatsPerBar,
                    notes = new List<NoteData>(),
                };
                Debug.Log("새 노트맵을 생성했습니다.");
            }

            UpdateRailAndCells(AudioManager.Instance.currentTrack);
            ApplyNotesToCells();
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
                    cell.noteData = note;
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
        }

        public void UpdateBeatsPerBar(int newBeatsPerBar)
        {
            if (noteMap == null)
                return;

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
        }

        public void ChangeBPM(float newBpm)
        {
            if (noteMap == null)
                return;

            noteMap.bpm = newBpm;
            Debug.Log($"BPM 변경됨: {newBpm}");

            if (
                AudioManager.Instance.currentTrack != null
                && AudioManager.Instance.currentTrack.TrackAudio != null
            )
            {
                UpdateRailAndCells(AudioManager.Instance.currentTrack);
            }

            if (AudioManager.Instance.currentTrack != null)
            {
                AudioManager.Instance.currentTrack.noteMap = noteMap;

                if (EditorDataManager.Instance != null)
                {
                    string trackName = AudioManager.Instance.currentTrack.trackName;
                    _ = EditorDataManager.Instance.SaveNoteMapAsync(trackName, noteMap);
                    Debug.Log($"BPM 변경으로 노트맵 자동 저장: {trackName}, BPM: {newBpm}");
                }
            }
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
