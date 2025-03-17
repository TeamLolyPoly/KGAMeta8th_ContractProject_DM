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

        private NoteMap noteMap;
        private bool isInitialized = false;

        public bool IsInitialized => isInitialized;
        public NoteMap NoteMap => noteMap;

        private bool isCreatingLongNote = false;
        private Cell longNoteStartCell = null;

        public void Initialize()
        {
            railController = EditorManager.Instance.railController;
            cellController = EditorManager.Instance.cellController;
            noteEditorPanel = EditorManager.Instance.editorPanel;
            try
            {
                noteMap = new NoteMap();
                noteMap.bpm = 120f;
                noteMap.beatsPerBar = 4;
                noteMap.notes = new List<NoteData>();

                SetActions();

                if (AudioManager.Instance != null && AudioManager.Instance.currentTrack != null)
                {
                    OnTrackChangedHandler(AudioManager.Instance.currentTrack);
                }

                isInitialized = true;
                Debug.Log("[NoteEditor] 초기화 완료");
            }
            catch (Exception e)
            {
                Debug.LogError($"[NoteEditor] 초기화 실패: {e.Message}");
                isInitialized = false;
            }
        }

        public void OnTrackAddedHandler(TrackData track)
        {
            if (track == null)
                return;

            if (
                AudioManager.Instance.currentTrack != null
                && AudioManager.Instance.currentTrack.trackName == track.trackName
            )
            {
                if (track.noteMap != null)
                {
                    noteMap = track.noteMap;
                    Debug.Log($"트랙 추가 시 노트맵 로드: {track.trackName}");
                }
                else
                {
                    noteMap = new NoteMap
                    {
                        bpm = track.bpm,
                        beatsPerBar = AudioManager.Instance.BeatsPerBar,
                        notes = new List<NoteData>(),
                    };
                    Debug.Log($"트랙 추가 시 새 노트맵 생성: {track.trackName}");
                }

                if (railController != null)
                {
                    railController.UpdateBeatSettings(noteMap.bpm, noteMap.beatsPerBar);
                }

                if (cellController != null)
                {
                    cellController.Initialize();
                }

                ApplyNotesToCells();
            }
        }

        public void OnTrackUpdatedHandler(TrackData track)
        {
            if (track == null)
                return;

            if (
                AudioManager.Instance.currentTrack != null
                && AudioManager.Instance.currentTrack.trackName == track.trackName
            )
            {
                if (track.noteMap != null && track.noteMap != noteMap)
                {
                    noteMap = track.noteMap;
                    Debug.Log($"트랙 업데이트 시 노트맵 로드: {track.trackName}");

                    if (railController != null)
                    {
                        railController.UpdateBeatSettings(noteMap.bpm, noteMap.beatsPerBar);
                    }

                    if (cellController != null)
                    {
                        cellController.Initialize();
                    }

                    ApplyNotesToCells();
                }
                else if (track.bpm != noteMap.bpm)
                {
                    noteMap.bpm = track.bpm;

                    if (railController != null)
                    {
                        railController.UpdateBeatSettings(noteMap.bpm, noteMap.beatsPerBar);
                    }
                }
            }
        }

        public void OnTrackLoadedHandler(TrackData track)
        {
            if (track == null)
                return;

            if (
                AudioManager.Instance.currentTrack != null
                && AudioManager.Instance.currentTrack.trackName == track.trackName
            )
            {
                if (track.noteMap != null)
                {
                    noteMap = track.noteMap;
                    Debug.Log($"트랙 로드 시 노트맵 로드: {track.trackName}");
                }
                else
                {
                    LoadNoteMap();
                }
            }
        }

        public void OnTrackChangedHandler(TrackData track)
        {
            if (track == null)
                return;

            Debug.Log($"트랙 변경됨: {track.trackName}");

            AutoSaveNoteMap();

            if (track.noteMap != null)
            {
                noteMap = track.noteMap;
                Debug.Log($"트랙 변경 시 노트맵 로드: {track.trackName}");

                if (railController != null)
                {
                    railController.UpdateBeatSettings(noteMap.bpm, noteMap.beatsPerBar);
                }

                if (cellController != null)
                {
                    cellController.Initialize();
                }

                ApplyNotesToCells();
            }
            else
            {
                StartCoroutine(LoadNoteMapForTrackCoroutine(track.trackName));
            }
        }

        private IEnumerator LoadNoteMapForTrackCoroutine(string trackName)
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
                Debug.Log($"트랙 변경 시 노트맵 파일 로드 완료: {trackName}");

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
                Debug.Log($"트랙 변경 시 새 노트맵 생성: {trackName}");
            }

            if (railController != null)
            {
                railController.UpdateBeatSettings(noteMap.bpm, noteMap.beatsPerBar);
            }

            if (cellController != null)
            {
                cellController.Initialize();
            }

            ApplyNotesToCells();
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

            if (railController != null)
            {
                railController.UpdateBeatSettings(noteMap.bpm, noteMap.beatsPerBar);
            }

            if (cellController != null)
            {
                cellController.Initialize();
            }

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
        }

        public void UpdateBPM(float newBpm)
        {
            if (noteMap == null)
                return;

            noteMap.bpm = newBpm;

            if (railController != null)
            {
                railController.UpdateBeatSettings(newBpm, noteMap.beatsPerBar);
            }
        }

        public void UpdateBeatsPerBar(int newBeatsPerBar)
        {
            if (noteMap == null)
                return;

            noteMap.beatsPerBar = newBeatsPerBar;

            if (railController != null)
            {
                railController.UpdateBeatSettings(noteMap.bpm, newBeatsPerBar);
            }
        }

        public void OnBPMChangedHandler(float newBpm)
        {
            if (noteMap == null)
                return;

            noteMap.bpm = newBpm;
            Debug.Log($"BPM 변경됨: {newBpm}");

            if (railController != null)
            {
                railController.UpdateBeatSettings(noteMap.bpm, noteMap.beatsPerBar);
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

        public void OnTrackChanged(TrackData track)
        {
            if (track == null)
                return;

            Debug.Log($"NoteEditor: Track changed to {track.trackName}");

            // 기존 OnTrackChangedHandler 메서드 호출
            OnTrackChangedHandler(track);
        }

        public void OnTrackRemovedHandler(TrackData track)
        {
            if (track == null)
                return;

            Debug.Log($"NoteEditor: Track removed: {track.trackName}");

            // 현재 편집 중인 트랙이 제거된 경우 노트맵 초기화
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
                    cellController.Initialize();
                }
            }
        }
    }
}
