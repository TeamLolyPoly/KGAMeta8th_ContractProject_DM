using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Michsky.UI.Heat;
using SFB;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Dropdown = Michsky.UI.Heat.Dropdown;

namespace NoteEditor
{
    public class NoteEditorPanel : MonoBehaviour, IInitializable
    {
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
        private Dropdown shortNoteTypeDropdown;

        [SerializeField]
        private Dropdown noteDirectionDropdown;

        [SerializeField]
        private Dropdown noteHitTypeDropdown;

        [SerializeField]
        private InputFieldManager beatsPerBarInput;

        [SerializeField]
        private TextMeshProUGUI statusText;

        [SerializeField]
        private TextMeshProUGUI selectedCellInfoText;

        public bool IsInitialized { get; private set; }
        private Sprite defaultAlbumArt;
        private Sprite dropdownItemIcon;
        private List<TrackData> trackDataList = new List<TrackData>();

        private bool isLoadingTrack = false;
        private bool isLoadingAlbumArt = false;

        private EditorDataManager audioDataManager;

        public void Initialize()
        {
            audioDataManager = EditorDataManager.Instance;
            defaultAlbumArt = Resources.Load<Sprite>("Textures/AlbumArt");
            dropdownItemIcon = Resources.Load<Sprite>("Textures/DefaultAudioIcon");
            LoadTrackButton.onClick.AddListener(OnLoadTrackButtonClicked);
            editor = EditorManager.Instance.noteEditor;
            if (SetAlbumArtButton != null)
            {
                SetAlbumArtButton.onClick.AddListener(OnSetAlbumArtButtonClicked);
            }
            if (DeleteTrackButton != null)
            {
                DeleteTrackButton.onClick.AddListener(
                    async () => await OnDeleteTrackButtonClicked()
                );
            }

            if (BPMInput != null)
            {
                BPMInput.onSubmit.AddListener(() => OnBPMInputSubmit(BPMInput.inputText.text));
            }

            if (beatsPerBarInput != null)
            {
                beatsPerBarInput.inputText.text = "4";
                beatsPerBarInput.onSubmit.AddListener(
                    () => OnBeatsPerBarChanged(beatsPerBarInput.inputText.text)
                );
            }

            if (saveButton != null)
            {
                saveButton.onClick.AddListener(OnSaveButtonClicked);
            }

            InitializeTrackDropdown();
            IsInitialized = true;
            Debug.Log("[NoteEditorPanel] 초기화 완료");
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

            if (beatsPerBarInput != null)
                beatsPerBarInput.onSubmit.RemoveAllListeners();

            if (saveButton != null)
                saveButton.onClick.RemoveAllListeners();

            if (trackDropdown != null)
                trackDropdown.onValueChanged.RemoveAllListeners();
        }

        private void SetTrackBPM(int bpm)
        {
            if (bpm <= 0)
            {
                Debug.LogWarning("BPM은 0보다 커야 합니다.");
                return;
            }

            if (trackDataList.Count == 0 || trackDropdown.selectedItemIndex < 0)
            {
                Debug.LogWarning("선택된 트랙이 없습니다.");
                return;
            }

            AudioManager.Instance.CurrentBPM = bpm;

            if (editor != null)
            {
                editor.UpdateBPM(bpm);
                UpdateStatusText($"BPM 변경됨: {bpm}");
            }
        }

        private void OnBPMInputSubmit(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                if (trackDataList.Count > 0 && trackDropdown.selectedItemIndex >= 0)
                {
                    TrackData selectedTrack = trackDataList[trackDropdown.selectedItemIndex];
                    BPMInput.inputText.text = selectedTrack.bpm.ToString();
                }
            }
            else if (int.TryParse(value, out int bpm))
            {
                SetTrackBPM(bpm);
            }
        }

        private void OnBeatsPerBarChanged(string value)
        {
            if (editor == null || !editor.IsInitialized)
                return;

            if (int.TryParse(value, out int beatsPerBar) && beatsPerBar > 0)
            {
                editor.UpdateBeatsPerBar(beatsPerBar);
                UpdateStatusText($"박자 수 변경됨: {beatsPerBar}");
            }
            else
            {
                beatsPerBarInput.inputText.text = editor.NoteMap.beatsPerBar.ToString();
                UpdateStatusText("유효한 박자 수를 입력하세요");
            }
        }

        public void InitializeTrackDropdown()
        {
            trackDataList.Clear();
            List<TrackData> tracks = AudioManager.Instance.GetAllTrackInfo();
            Debug.Log($"트랙 데이터 로드: {tracks.Count}개 트랙 발견");
            trackDataList = tracks;

            trackDropdown.items.Clear();

            foreach (TrackData track in tracks)
            {
                Debug.Log(
                    $"트랙 추가: {track.trackName}, 앨범아트: {(track.AlbumArt != null ? "있음" : "없음")}"
                );
                try
                {
                    trackDropdown.CreateNewItem(track.trackName, dropdownItemIcon, false);
                }
                catch (Exception e)
                {
                    Debug.LogError($"드롭다운 아이템 추가 오류: {e.Message}");
                }
            }

            try
            {
                if (tracks.Count > 0)
                {
                    trackDropdown.Initialize();
                }
                else
                {
                    trackDropdown.CreateNewItem("No Track", dropdownItemIcon, false);
                    trackDropdown.Initialize();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"드롭다운 초기화 오류: {e.Message}");
            }

            trackDropdown.onValueChanged.AddListener(OnTrackDropdownValueChanged);

            if (tracks.Count > 0)
            {
                trackDropdown.selectedItemIndex = 0;
                AudioManager.Instance.SelectTrack(tracks[0]);
                CurrentTrackInfo.SetText(tracks[0].trackName);
                if (tracks[0].AlbumArt != null)
                {
                    CurrentTrackInfo.SetBackground(tracks[0].AlbumArt);
                }
                else
                {
                    CurrentTrackInfo.SetBackground(defaultAlbumArt);
                }
            }
            else
            {
                CurrentTrackInfo.SetText("No Track");
                CurrentTrackInfo.SetBackground(defaultAlbumArt);
            }
        }

        private void OnTrackDropdownValueChanged(int selectedIndex)
        {
            if (selectedIndex >= 0 && selectedIndex < trackDataList.Count)
            {
                TrackData selectedTrack = trackDataList[selectedIndex];
                CurrentTrackInfo.SetText(selectedTrack.trackName);
                if (selectedTrack.AlbumArt != null)
                {
                    CurrentTrackInfo.SetBackground(selectedTrack.AlbumArt);
                }
                else
                {
                    CurrentTrackInfo.SetBackground(defaultAlbumArt);
                }
                AudioManager.Instance.SelectTrack(selectedTrack);

                if (BPMInput != null)
                {
                    BPMInput.inputText.text = selectedTrack.bpm.ToString();

                    if (editor != null)
                    {
                        editor.UpdateBPM(selectedTrack.bpm);
                    }
                }
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

                // 시간을 분:초 형식으로 표시
                string currentTimeStr = FormatTime(currentTime);
                string durationStr = FormatTime(duration);

                currentTrackPlaybackTime.text = $"{currentTimeStr} / {durationStr}";
            }

            if (editor != null && editor.IsInitialized)
            {
                CellController cellGenerator = FindObjectOfType<CellController>();
                if (cellGenerator != null)
                {
                    UpdateSelectedCellInfo(cellGenerator.SelectedCell);
                }
            }
        }

        private string FormatTime(float timeInSeconds)
        {
            int minutes = Mathf.FloorToInt(timeInSeconds / 60);
            int seconds = Mathf.FloorToInt(timeInSeconds % 60);
            return $"{minutes:00}:{seconds:00}";
        }

        public void OnLoadTrackButtonClicked()
        {
            if (isLoadingTrack || audioDataManager == null)
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
                        audioDataManager.LoadAudioFile(paths[0]);
                    }
                }
            );
        }

        private void OnTrackLoaded(TrackData newTrack)
        {
            isLoadingTrack = false;

            if (newTrack == null)
                return;

            trackDataList = AudioManager.Instance.GetAllTrackInfo();

            UpdateTrackDropdown();

            int newTrackIndex = trackDataList.FindIndex(t => t.trackName == newTrack.trackName);
            if (newTrackIndex >= 0)
            {
                trackDropdown.selectedItemIndex = newTrackIndex;
                OnTrackDropdownValueChanged(newTrackIndex);

                if (newTrack.noteMap == null)
                {
                    LoadNoteMapForTrack(newTrack.trackName);
                }
                else
                {
                    UpdateStatusText($"트랙 로드됨: {newTrack.trackName}, 노트맵 로드 완료");

                    if (beatsPerBarInput != null && newTrack.noteMap != null)
                    {
                        beatsPerBarInput.inputText.text = newTrack.noteMap.beatsPerBar.ToString();
                    }
                }
            }
        }

        public void OnSetAlbumArtButtonClicked()
        {
            if (
                isLoadingAlbumArt
                || audioDataManager == null
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
                        audioDataManager.SetAlbumArt(paths[0], trackDropdown.selectedItemIndex);
                    }
                }
            );
        }

        private void OnAlbumArtLoaded(string trackName, Sprite albumArt)
        {
            isLoadingAlbumArt = false;

            if (string.IsNullOrEmpty(trackName) || albumArt == null)
                return;

            trackDataList = AudioManager.Instance.GetAllTrackInfo();

            UpdateTrackDropdown();

            int trackIndex = trackDataList.FindIndex(t => t.trackName == trackName);
            if (trackIndex >= 0)
            {
                trackDropdown.selectedItemIndex = trackIndex;
                OnTrackDropdownValueChanged(trackIndex);

                CurrentTrackInfo.SetBackground(albumArt);
            }
        }

        public async Task OnDeleteTrackButtonClicked()
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
                string trackName = selectedTrack.trackName;

                await EditorDataManager.Instance.DeleteTrackAsync(trackName);

                trackDropdown.RemoveItem(trackName, true);

                trackDataList = AudioManager.Instance.GetAllTrackInfo();
                Debug.Log($"삭제 후 남은 트랙 수: {trackDataList.Count}");

                if (trackDataList.Count == 0)
                {
                    trackDropdown.items.Clear();
                    trackDropdown.CreateNewItem("No Track", dropdownItemIcon, false);
                    trackDropdown.Initialize();

                    CurrentTrackInfo.SetText("No Track");
                    CurrentTrackInfo.SetBackground(defaultAlbumArt);
                }
                else
                {
                    int newSelectedIndex;

                    if (selectedIndex < trackDataList.Count)
                    {
                        newSelectedIndex = selectedIndex;
                    }
                    else
                    {
                        newSelectedIndex = trackDataList.Count - 1;
                    }

                    trackDropdown.selectedItemIndex = newSelectedIndex;

                    OnTrackDropdownValueChanged(newSelectedIndex);
                }

                Debug.Log($"트랙 삭제됨: {trackName}");
            }
        }

        private void UpdateTrackDropdown()
        {
            trackDropdown.items.Clear();

            trackDataList = AudioManager.Instance.GetAllTrackInfo();

            if (trackDataList.Count == 0)
            {
                try
                {
                    trackDropdown.CreateNewItem("No Track", dropdownItemIcon, false);
                    trackDropdown.Initialize();
                    trackDropdown.selectedItemIndex = 0;

                    CurrentTrackInfo.SetText("No Track");
                    CurrentTrackInfo.SetBackground(defaultAlbumArt);

                    if (trackDropdown.isOn)
                    {
                        trackDropdown.Animate();
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"빈 드롭다운 초기화 오류: {e.Message}");
                }
                return;
            }

            foreach (TrackData track in trackDataList)
            {
                try
                {
                    trackDropdown.CreateNewItem(track.trackName, dropdownItemIcon, false);
                }
                catch (Exception e)
                {
                    Debug.LogError($"드롭다운 아이템 추가 오류: {e.Message}");
                }
            }

            try
            {
                trackDropdown.Initialize();

                if (trackDataList.Count > 0)
                {
                    trackDropdown.selectedItemIndex = 0;
                    OnTrackDropdownValueChanged(0);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"드롭다운 초기화 오류: {e.Message}");
            }
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
                            ? $"노트 타입: {cell.noteData.baseType}"
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

        public void OnTrackChangedHandler(TrackData track)
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

            // BPM 입력 필드 업데이트
            if (BPMInput != null)
            {
                BPMInput.inputText.text = track.bpm.ToString();
            }

            // 박자 수 입력 필드 업데이트
            if (beatsPerBarInput != null && track.noteMap != null)
            {
                beatsPerBarInput.inputText.text = track.noteMap.beatsPerBar.ToString();
            }
            else if (beatsPerBarInput != null)
            {
                beatsPerBarInput.inputText.text = AudioManager.Instance.BeatsPerBar.ToString();
            }

            UpdateTrackDropdown();

            if (trackDropdown != null)
            {
                int index = trackDataList.FindIndex(t => t.trackName == track.trackName);
                if (index >= 0)
                {
                    trackDropdown.selectedItemIndex = index;
                }
            }

            UpdateStatusText($"트랙 변경됨: {track.trackName}");
        }

        private void LoadNoteMapForTrack(string trackName)
        {
            if (string.IsNullOrEmpty(trackName) || audioDataManager == null)
                return;

            UpdateStatusText($"노트맵 로드 중: {trackName}");

            string filePath = AudioPathProvider.GetNoteMapPath(trackName);

            if (File.Exists(filePath))
            {
                StartCoroutine(LoadNoteMapCoroutine(trackName));
            }
            else
            {
                UpdateStatusText($"노트맵 파일 없음: {trackName}, 새 노트맵 생성");
            }
        }

        private IEnumerator LoadNoteMapCoroutine(string trackName)
        {
            var task = audioDataManager.LoadNoteMapAsync(trackName);
            yield return new WaitUntil(() => task.IsCompleted);

            if (task.Result != null)
            {
                UpdateStatusText($"노트맵 로드 완료: {trackName}");

                if (
                    beatsPerBarInput != null
                    && AudioManager.Instance.currentTrack != null
                    && AudioManager.Instance.currentTrack.noteMap != null
                )
                {
                    beatsPerBarInput.inputText.text =
                        AudioManager.Instance.currentTrack.noteMap.beatsPerBar.ToString();
                }
            }
            else
            {
                UpdateStatusText($"노트맵 로드 실패: {trackName}, 새 노트맵 생성");
            }
        }

        public void OnBPMChangedHandler(float newBpm)
        {
            if (BPMInput != null)
            {
                BPMInput.inputText.text = newBpm.ToString();
            }

            if (CurrentTrackInfo != null && AudioManager.Instance.currentTrack != null)
            {
                CurrentTrackInfo.buttonDescription = $"BPM: {newBpm}";
            }

            UpdateStatusText($"BPM 변경됨: {newBpm}");
        }
    }
}
