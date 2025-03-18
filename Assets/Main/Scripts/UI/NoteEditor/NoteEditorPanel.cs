using System;
using System.Collections;
using System.Collections.Generic;
using Michsky.UI.Heat;
using NoteEditor;
using ProjectDM.UI;
using SFB;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Dropdown = Michsky.UI.Heat.Dropdown;

namespace NoteEditor
{
    public class NoteEditorPanel : Panel, IInitializable
    {
        public override PanelType PanelType => PanelType.NoteEditor;
        public BoxButtonManager CurrentTrackInfo;
        public ButtonManager LoadTrackButton;
        public TextMeshProUGUI currentTrackPlaybackTime;
        public Dropdown trackDropdown;
        public BoxButtonManager SetAlbumArtButton;
        public ButtonManager DeleteTrackButton;
        public InputFieldManager BPMInput;
        private NoteEditor editor;

        [SerializeField]
        private ButtonManager saveButton;

        [SerializeField]
        private Dropdown noteColorDropdown;

        [SerializeField]
        private Dropdown noteDirectionDropdown;

        [SerializeField]
        private Dropdown beatsPerBarDropdown;

        [SerializeField]
        private TextMeshProUGUI statusText;

        [SerializeField]
        private TextMeshProUGUI selectedCellInfoText;

        [SerializeField]
        private CanvasGroup longNotePanel;

        [SerializeField]
        private Toggle symmetricToggle;

        [SerializeField]
        private Toggle clockwiseToggle;

        public bool IsInitialized { get; private set; }
        private Sprite defaultAlbumArt;
        private Sprite dropdownItemIcon;
        private List<TrackData> trackDataList = new List<TrackData>();

        private bool isLoadingTrack = false;
        private bool isLoadingAlbumArt = false;

        private EditorManager editorManager;

        public override void Open()
        {
            base.Open();
            Initialize();
        }

        public void Initialize()
        {
            editorManager = EditorManager.Instance;
            defaultAlbumArt = Resources.Load<Sprite>("Textures/AlbumArt");
            dropdownItemIcon = Resources.Load<Sprite>("Textures/DefaultAudioIcon");
            LoadTrackButton.onClick.AddListener(LoadTrack);
            editor = EditorManager.Instance.noteEditor;
            if (SetAlbumArtButton != null)
            {
                SetAlbumArtButton.onClick.AddListener(OnSetAlbumArtButtonClicked);
            }
            if (DeleteTrackButton != null)
            {
                DeleteTrackButton.onClick.AddListener(DeleteTrack);
            }

            if (BPMInput != null)
            {
                BPMInput.onSubmit.AddListener(() => OnBPMInputSubmit(BPMInput.inputText.text));
                BPMInput.inputText.text = "선택된 트랙 없음";
            }

            if (saveButton != null)
            {
                saveButton.onClick.AddListener(OnSaveButtonClicked);
            }

            InitializeTrackDropdown();
            InitializeAlbumArtButton();
            InitializeNoteUI();
            IsInitialized = true;
            Debug.Log("[NoteEditorPanel] 초기화 완료");

            // 롱노트 UI 이벤트 리스너 설정
            if (symmetricToggle != null)
            {
                symmetricToggle.onValueChanged.AddListener(OnSymmetricToggleChanged);
            }

            if (clockwiseToggle != null)
            {
                clockwiseToggle.onValueChanged.AddListener(OnClockwiseToggleChanged);
            }
        }

        private void InitializeBeatsPerBarDropdown()
        {
            beatsPerBarDropdown.items.Clear();
            foreach (int beatsPerBar in Enum.GetValues(typeof(BeatsPerBar)))
            {
                beatsPerBarDropdown.CreateNewItem(beatsPerBar.ToString(), true);
            }

            beatsPerBarDropdown.onValueChanged.AddListener(OnBeatsPerBarDropdownValueChanged);
        }

        private void OnBeatsPerBarDropdownValueChanged(int index)
        {
            switch (index)
            {
                case 0:
                    editor.UpdateBeatsPerBar(4);
                    break;
                case 1:
                    editor.UpdateBeatsPerBar(8);
                    break;
                case 2:
                    editor.UpdateBeatsPerBar(6);
                    break;
                case 3:
                    editor.UpdateBeatsPerBar(12);
                    break;
                case 4:
                    editor.UpdateBeatsPerBar(16);
                    break;
                case 5:
                    editor.UpdateBeatsPerBar(32);
                    break;
                default:
                    editor.UpdateBeatsPerBar(4);
                    break;
            }
        }

        private void InitializeNoteUI()
        {
            noteDirectionDropdown.items.Clear();
            noteColorDropdown.items.Clear();
            foreach (NoteColor noteColor in Enum.GetValues(typeof(NoteColor)))
            {
                noteColorDropdown.CreateNewItem(noteColor.ToString(), true);
            }

            foreach (NoteDirection noteDirection in Enum.GetValues(typeof(NoteDirection)))
            {
                noteDirectionDropdown.CreateNewItem(noteDirection.ToString(), true);
            }

            if (noteColorDropdown != null)
            {
                noteColorDropdown.onValueChanged.AddListener(OnNoteColorDropdownValueChanged);
            }

            if (noteDirectionDropdown != null)
            {
                noteDirectionDropdown.onValueChanged.AddListener(
                    OnNoteDirectionDropdownValueChanged
                );
            }

            ToggleShortNoteUI(false);
        }

        private void OnNoteColorDropdownValueChanged(int index)
        {
            if (!editor.UpdateNoteColor(index))
            {
                UpdateStatusText("<color=red>노트 색상 변경 실패</color>");
            }
            else
            {
                UpdateStatusText("<color=green>노트 색상 변경 완료</color>");
            }
        }

        private void OnNoteDirectionDropdownValueChanged(int index)
        {
            if (!editor.UpdateNoteDirection(index))
            {
                UpdateStatusText("<color=red>노트 방향 변경 실패</color>");
            }
            else
            {
                UpdateStatusText("<color=green>노트 방향 변경 완료</color>");
            }
        }

        private void OnDestroy()
        {
            if (LoadTrackButton != null)
                LoadTrackButton.onClick.RemoveAllListeners();

            if (SetAlbumArtButton != null)
                SetAlbumArtButton.onClick.RemoveAllListeners();

            if (DeleteTrackButton != null)
                DeleteTrackButton.onClick.RemoveAllListeners();

            if (BPMInput != null)
                BPMInput.onSubmit.RemoveAllListeners();

            if (saveButton != null)
                saveButton.onClick.RemoveAllListeners();

            if (trackDropdown != null)
                trackDropdown.onValueChanged.RemoveAllListeners();
        }

        private void OnBPMInputSubmit(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                Debug.LogWarning("BPM을 입력하세요.");
                return;
            }
            if (!int.TryParse(value, out int bpm))
            {
                Debug.LogWarning("유효한 숫자를 입력하세요.");
                return;
            }
            if (bpm <= 0)
            {
                UpdateStatusText("<color=red>BPM은 0보다 커야 합니다.</color>");
                return;
            }

            if (trackDataList.Count == 0 || trackDropdown.selectedItemIndex < 0)
            {
                UpdateStatusText("<color=red>선택된 트랙이 없습니다.</color>");
                return;
            }

            AudioManager.Instance.CurrentBPM = bpm;

            if (editor != null)
            {
                editor.UpdateBPM(bpm);
                UpdateStatusText($"<color=green>BPM 변경됨: {bpm}</color>");
            }

            if (editorManager.currentTrack != null)
            {
                _ = editorManager.SetBPMAsync(editorManager.currentTrack.trackName, bpm);
            }
        }

        public void InitializeTrackDropdown()
        {
            try
            {
                if (trackDropdown != null)
                {
                    trackDropdown.items.Clear();
                    trackDropdown.selectedItemIndex = 0;

                    trackDataList = editorManager.GetAllTrackInfo();

                    if (trackDataList.Count > 0)
                    {
                        foreach (var track in trackDataList)
                        {
                            trackDropdown.CreateNewItem(
                                track.trackName,
                                track.AlbumArt != null ? track.AlbumArt : dropdownItemIcon,
                                false
                            );
                        }

                        trackDropdown.onValueChanged.AddListener(OnTrackDropdownValueChanged);
                        trackDropdown.Initialize();

                        if (editorManager.currentTrack != null)
                        {
                            int index = trackDataList.FindIndex(t =>
                                t.trackName == editorManager.currentTrack.trackName
                            );
                            if (index >= 0)
                            {
                                trackDropdown.selectedItemIndex = index;
                            }
                        }
                    }
                    else
                    {
                        trackDropdown.CreateNewItem("No Tracks", dropdownItemIcon, true);
                        trackDropdown.SetDropdownIndex(0);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"드롭다운 초기화 오류: {e.Message}");
            }
        }

        private void InitializeAlbumArtButton()
        {
            if (SetAlbumArtButton != null)
            {
                if (trackDataList.Count == 0)
                {
                    SetAlbumArtButton.buttonIcon = defaultAlbumArt;
                    SetAlbumArtButton.buttonDescription = "앨범 아트 선택";
                    SetAlbumArtButton.SetText("로드된 트랙 없음");
                }
                else
                {
                    SetAlbumArtButton.buttonIcon = trackDataList[
                        trackDropdown.selectedItemIndex
                    ].AlbumArt;
                    SetAlbumArtButton.buttonDescription = "앨범 아트 선택";
                    SetAlbumArtButton.SetText(
                        trackDataList[trackDropdown.selectedItemIndex].trackName
                    );
                }
            }
        }

        private void OnTrackDropdownValueChanged(int index)
        {
            if (index < 0 || index >= trackDataList.Count)
                return;

            TrackData selectedTrack = trackDataList[index];

            if (selectedTrack == null)
                return;
            else
            {
                trackDropdown.SetDropdownIndex(index);
                editorManager.SelectTrack(selectedTrack);
            }
        }

        private void Update()
        {
            if (!IsInitialized)
            {
                return;
            }

            if (
                AudioManager.Instance != null
                && AudioManager.Instance.currentAudioSource != null
                && currentTrackPlaybackTime != null
            )
            {
                float currentTime = AudioManager.Instance.currentPlaybackTime;
                float duration = AudioManager.Instance.currentPlaybackDuration;

                string currentTimeStr = FormatTime(currentTime);
                string durationStr = FormatTime(duration);

                currentTrackPlaybackTime.text = $"{currentTimeStr} / {durationStr}";
            }
        }

        private string FormatTime(float timeInSeconds)
        {
            int minutes = Mathf.FloorToInt(timeInSeconds / 60);
            int seconds = Mathf.FloorToInt(timeInSeconds % 60);
            int milliseconds = Mathf.FloorToInt((timeInSeconds * 1000) % 1000);
            return $"{minutes:00}:{seconds:00}:{milliseconds:00}";
        }

        public void LoadTrack()
        {
            if (isLoadingTrack)
                return;

            ExtensionFilter[] extensions =
            {
                new ExtensionFilter("오디오 파일", "mp3", "wav", "ogg"),
            };

            StandaloneFileBrowser.OpenFilePanelAsync(
                "오디오 파일 선택",
                "",
                extensions,
                false,
                (string[] paths) =>
                {
                    if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
                    {
                        isLoadingTrack = true;
                        editorManager.LoadAudioFile(paths[0]);
                        isLoadingTrack = false;
                    }
                }
            );
        }

        public void OnSetAlbumArtButtonClicked()
        {
            if (
                isLoadingAlbumArt
                || trackDataList.Count == 0
                || trackDropdown.selectedItemIndex < 0
            )
            {
                Debug.LogWarning("선택된 트랙이 없습니다.");
                return;
            }

            ExtensionFilter[] extensions =
            {
                new ExtensionFilter("이미지 파일", "png", "jpg", "jpeg"),
            };

            StandaloneFileBrowser.OpenFilePanelAsync(
                "앨범 아트 선택",
                "",
                extensions,
                false,
                (string[] paths) =>
                {
                    if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
                    {
                        isLoadingAlbumArt = true;
                        editorManager.SetAlbumArt(paths[0], trackDropdown.selectedItemIndex);
                        isLoadingAlbumArt = false;
                    }
                }
            );
        }

        public void DeleteTrack()
        {
            if (trackDataList.Count == 0 || trackDropdown.selectedItemIndex < 0)
            {
                Debug.LogWarning("선택된 트랙이 없습니다.");
                return;
            }

            int selectedIndex = trackDropdown.selectedItemIndex;
            if (selectedIndex >= 0 && selectedIndex < trackDataList.Count)
            {
                TrackData selectedTrack = trackDataList[selectedIndex];
                editorManager.RemoveTrack(selectedTrack);
            }

            RefreshTrackList();
        }

        private void OnSaveButtonClicked()
        {
            if (editor == null || !editor.IsInitialized)
                return;

            editor.SaveNoteMap();
            UpdateStatusText($"{AudioManager.Instance.currentTrack.trackName} 노트맵 저장 완료");
        }

        public void UpdateStatusText(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
        }

        public void UpdateSelectedCellInfo(Cell cell)
        {
            if (selectedCellInfoText != null)
            {
                if (cell != null)
                {
                    string noteInfo =
                        cell.noteData != null
                            ? $"노트 타입: {cell.noteData.noteType}"
                            : "노트 없음";

                    selectedCellInfoText.text =
                        $"선택된 셀: 마디 {cell.bar}, 박자 {cell.beat}, 위치 ({cell.cellPosition.x}, {cell.cellPosition.y})\n{noteInfo}";
                }
                else
                {
                    selectedCellInfoText.text = "선택된 셀 없음";
                }
            }
        }

        public void ChangeTrack(TrackData track)
        {
            if (track == null)
                return;

            Debug.Log($"NoteEditorPanel: Track changed to {track.trackName}");

            if (CurrentTrackInfo != null)
            {
                CurrentTrackInfo.SetText(track.trackName);
                CurrentTrackInfo.buttonDescription = $"BPM: {track.bpm}";

                if (track.AlbumArt != null)
                {
                    CurrentTrackInfo.buttonIcon = track.AlbumArt;
                }
                else
                {
                    CurrentTrackInfo.buttonIcon = defaultAlbumArt;
                }
            }

            if (BPMInput != null)
            {
                BPMInput.inputText.text = track.bpm.ToString();
            }

            int index = trackDataList.FindIndex(t => t.trackName == track.trackName);
            if (index >= 0 && trackDropdown != null)
            {
                trackDropdown.SetDropdownIndex(index);
            }

            UpdateStatusText($"<color=green>트랙 변경됨: {track.trackName}</color>");
        }

        public void RefreshTrackList()
        {
            trackDataList = editorManager.GetAllTrackInfo();
            InitializeTrackDropdown();
            InitializeAlbumArtButton();
            InitializeInputFields();
        }

        public void ToggleShortNoteUI(bool isVisible)
        {
            noteDirectionDropdown.gameObject.SetActive(isVisible);
            noteColorDropdown.gameObject.SetActive(isVisible);
        }

        private void InitializeInputFields()
        {
            if (trackDataList.Count > 0)
            {
                TrackData track = trackDataList[trackDropdown.selectedItemIndex];
                if (BPMInput != null)
                {
                    BPMInput.inputText.text = track.bpm.ToString();
                }
            }
            else
            {
                BPMInput.inputText.text = "트랙 없음";
            }
        }

        private void OnSymmetricToggleChanged(bool isOn)
        {
            if (EditorManager.Instance != null && EditorManager.Instance.noteEditor != null)
            {
                EditorManager.Instance.noteEditor.UpdateNoteSymmetric(isOn);
            }
        }

        private void OnClockwiseToggleChanged(bool isOn)
        {
            if (EditorManager.Instance != null && EditorManager.Instance.noteEditor != null)
            {
                EditorManager.Instance.noteEditor.UpdateNoteClockwise(isOn);
            }
        }

        public void ToggleLongNoteUI(bool isVisible)
        {
            if (longNotePanel != null)
            {
                longNotePanel.alpha = isVisible ? 1 : 0;
                longNotePanel.interactable = isVisible;
                longNotePanel.blocksRaycasts = isVisible;
            }

            if (
                isVisible
                && EditorManager.Instance.cellController.SelectedCell != null
                && EditorManager.Instance.cellController.SelectedCell.noteData != null
            )
            {
                var noteData = EditorManager.Instance.cellController.SelectedCell.noteData;
                if (symmetricToggle != null)
                {
                    symmetricToggle.isOn = noteData.isSymmetric;
                }
                if (clockwiseToggle != null)
                {
                    clockwiseToggle.isOn = noteData.isClockwise;
                }
            }
        }
    }
}
