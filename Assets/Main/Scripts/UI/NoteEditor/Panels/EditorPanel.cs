using System;
using System.Collections.Generic;
using Michsky.UI.Heat;
using ProjectDM.UI;
using SFB;
using TMPro;
using UnityEngine;
using Dropdown = Michsky.UI.Heat.Dropdown;

namespace NoteEditor
{
    public class EditorPanel : Panel, IInitializable
    {
        public override PanelType PanelType => PanelType.NoteEditor;
        public BoxButtonManager CurrentTrackInfo;
        public TextMeshProUGUI currentTrackPlaybackTime;
        public BoxButtonManager SetAlbumArtButton;
        public InputFieldManager BPMInput;
        private NoteEditor editor;

        [SerializeField]
        private Dropdown noteColorDropdown;

        [SerializeField]
        private Dropdown noteDirectionDropdown;

        [SerializeField]
        private Animator noteColorAnimator;

        [SerializeField]
        private Animator noteDirectionAnimator;

        [SerializeField]
        private Dropdown beatsPerBarDropdown;

        [SerializeField]
        private TextMeshProUGUI statusText;

        [SerializeField]
        private TextMeshProUGUI selectedCellInfoText;

        [SerializeField]
        private Animator longNoteGroupAnimator;

        [SerializeField]
        private SettingsElement symmetricToggle;

        [SerializeField]
        private SettingsElement clockwiseToggle;

        [SerializeField]
        private ButtonManager CameraToggleButton;

        public bool IsInitialized { get; private set; }

        private bool isLoadingAlbumArt = false;

        private UnityEngine.Events.UnityAction<bool> symmetricToggleHandler;
        private UnityEngine.Events.UnityAction<bool> clockwiseToggleHandler;

        public override void Open()
        {
            base.Open();
            Initialize();
        }

        public void Initialize()
        {
            editor = EditorManager.Instance.noteEditor;
            if (SetAlbumArtButton != null)
            {
                SetAlbumArtButton.onClick.AddListener(OnSetAlbumArtButtonClicked);
            }

            if (BPMInput != null)
            {
                BPMInput.onSubmit.AddListener(() => OnBPMInputSubmit(BPMInput.inputText.text));
                BPMInput.inputText.text = EditorManager.Instance.CurrentTrack.bpm.ToString();
            }

            InitializeAlbumArtButton();
            InitializeNoteUI();
            InitializeBeatsPerBarDropdown();
            currentTrackPlaybackTime.text =
                $"00:00:00 / {FormatTime(AudioManager.Instance.currentPlaybackDuration)}";
            IsInitialized = true;
            Debug.Log("[NoteEditorPanel] 초기화 완료");
        }

        private void InitializeBeatsPerBarDropdown()
        {
            if (beatsPerBarDropdown == null)
                return;
            if (!IsInitialized)
            {
                foreach (BeatsPerBar beatsPerBar in Enum.GetValues(typeof(BeatsPerBar)))
                {
                    beatsPerBarDropdown.CreateNewItem(beatsPerBar.ToString(), true);
                }
            }

            switch (AudioManager.Instance.BeatsPerBar)
            {
                case 4:
                    beatsPerBarDropdown.SetDropdownIndex(0);
                    break;
                case 8:
                    beatsPerBarDropdown.SetDropdownIndex(1);
                    break;
                case 6:
                    beatsPerBarDropdown.SetDropdownIndex(2);
                    break;
                case 12:
                    beatsPerBarDropdown.SetDropdownIndex(3);
                    break;
                case 16:
                    beatsPerBarDropdown.SetDropdownIndex(4);
                    break;
                case 32:
                    beatsPerBarDropdown.SetDropdownIndex(5);
                    break;
                default:
                    beatsPerBarDropdown.SetDropdownIndex(0);
                    break;
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

            if (symmetricToggle != null)
            {
                symmetricToggleHandler = OnSymmetricToggleChanged;
                symmetricToggle
                    .GetComponentInChildren<SwitchManager>()
                    .onValueChanged.AddListener(symmetricToggleHandler);
            }

            if (clockwiseToggle != null)
            {
                clockwiseToggleHandler = OnClockwiseToggleChanged;
                clockwiseToggle
                    .GetComponentInChildren<SwitchManager>()
                    .onValueChanged.AddListener(clockwiseToggleHandler);
            }

            ToggleLongNoteUI(false);
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

        public override void Close(bool objActive = true)
        {
            Cleanup();
            base.Close(objActive);
        }

        private void Cleanup()
        {
            if (BPMInput != null)
            {
                BPMInput.onSubmit.RemoveAllListeners();
            }

            if (noteColorDropdown != null)
            {
                noteColorDropdown.onValueChanged.RemoveAllListeners();
            }

            if (noteDirectionDropdown != null)
            {
                noteDirectionDropdown.onValueChanged.RemoveAllListeners();
            }

            if (beatsPerBarDropdown != null)
            {
                beatsPerBarDropdown.onValueChanged.RemoveAllListeners();
            }

            if (symmetricToggle != null)
            {
                symmetricToggle
                    .GetComponentInChildren<SwitchManager>()
                    .onValueChanged.RemoveListener(symmetricToggleHandler);
            }

            if (clockwiseToggle != null)
            {
                clockwiseToggle
                    .GetComponentInChildren<SwitchManager>()
                    .onValueChanged.RemoveListener(clockwiseToggleHandler);
            }

            if (CameraToggleButton != null)
            {
                CameraToggleButton.onClick.RemoveAllListeners();
            }

            if (SetAlbumArtButton != null)
            {
                SetAlbumArtButton.onClick.RemoveAllListeners();
            }

            if (CurrentTrackInfo != null)
            {
                CurrentTrackInfo.onClick.RemoveAllListeners();
            }

            if (statusText != null)
            {
                statusText.text = "";
            }

            if (currentTrackPlaybackTime != null)
            {
                currentTrackPlaybackTime.text = "";
            }

            if (selectedCellInfoText != null)
            {
                selectedCellInfoText.text = "";
            }

            if (longNoteGroupAnimator != null)
            {
                longNoteGroupAnimator.SetBool("isOpen", false);
            }

            if (noteColorAnimator != null)
            {
                noteColorAnimator.SetBool("isOpen", false);
            }

            if (noteDirectionAnimator != null)
            {
                noteDirectionAnimator.SetBool("isOpen", false);
            }
            if (beatsPerBarDropdown != null)
            {
                beatsPerBarDropdown.onValueChanged.RemoveAllListeners();
            }
        }

        private async void OnBPMInputSubmit(string value)
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

            AudioManager.Instance.CurrentBPM = bpm;

            if (editor != null)
            {
                await EditorManager.Instance.SetBPMAsync(bpm);
                UpdateStatusText($"<color=green>BPM 변경됨: {bpm}</color>");
            }
        }

        private void InitializeAlbumArtButton()
        {
            if (SetAlbumArtButton != null)
            {
                SetAlbumArtButton.buttonIcon = EditorManager.Instance.CurrentTrack.AlbumArt;
                SetAlbumArtButton.buttonDescription = "앨범 아트 선택";
                SetAlbumArtButton.SetText(EditorManager.Instance.CurrentTrack.trackName);
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

        public void OnSetAlbumArtButtonClicked()
        {
            if (!isLoadingAlbumArt)
            {
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
                            EditorManager.Instance.SetAlbumArt(
                                paths[0],
                                EditorManager.Instance.CurrentTrack
                            );
                            isLoadingAlbumArt = false;
                        }
                    }
                );
            }
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

        public void UpdateNoteInfo(Cell cell)
        {
            if (cell.noteData != null)
            {
                if (cell.noteData.noteType == NoteType.Short)
                {
                    EditorManager.Instance.editorPanel.ToggleShortNoteUI(true);
                    noteDirectionDropdown.SetDropdownIndex((int)cell.noteData.direction);
                    noteColorDropdown.SetDropdownIndex((int)cell.noteData.noteColor);
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
                EditorManager.Instance.editorPanel.ToggleShortNoteUI(false);
                EditorManager.Instance.editorPanel.ToggleLongNoteUI(false);
            }
        }

        public void ToggleShortNoteUI(bool isVisible)
        {
            noteDirectionAnimator.SetBool("isOpen", isVisible);
            noteColorAnimator.SetBool("isOpen", isVisible);
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
            longNoteGroupAnimator.SetBool("isOpen", isVisible);

            if (
                isVisible
                && EditorManager.Instance.cellController.SelectedCell != null
                && EditorManager.Instance.cellController.SelectedCell.noteData != null
            )
            {
                var noteData = EditorManager.Instance.cellController.SelectedCell.noteData;

                SwitchManager symmetricSwitch =
                    symmetricToggle.GetComponentInChildren<SwitchManager>();
                SwitchManager clockwiseSwitch =
                    clockwiseToggle.GetComponentInChildren<SwitchManager>();

                if (symmetricSwitch != null)
                {
                    symmetricSwitch.onValueChanged.RemoveListener(symmetricToggleHandler);
                    if (noteData.isSymmetric)
                        symmetricSwitch.SetOn();
                    else
                        symmetricSwitch.SetOff();
                    symmetricSwitch.onValueChanged.AddListener(symmetricToggleHandler);
                }
                if (clockwiseSwitch != null)
                {
                    clockwiseSwitch.onValueChanged.RemoveListener(clockwiseToggleHandler);
                    if (noteData.isClockwise)
                        clockwiseSwitch.SetOn();
                    else
                        clockwiseSwitch.SetOff();
                    clockwiseSwitch.onValueChanged.AddListener(clockwiseToggleHandler);
                }
            }
        }
    }
}
