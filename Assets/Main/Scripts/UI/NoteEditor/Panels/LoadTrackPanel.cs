using System.Collections.Generic;
using System.Linq;
using Michsky.UI.Heat;
using ProjectDM.UI;
using SFB;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NoteEditor
{
    public class LoadTrackPanel : Panel
    {
        public override PanelType PanelType => PanelType.LoadTrack;
        public RectTransform contentParent;
        public TrackButton trackButtonPrefab;
        public ButtonManager closeButton;
        private List<TrackButton> trackButtons = new List<TrackButton>();
        private TrackData selectedTrack;
        private NoteMapData selectedNoteMapData;
        public ButtonManager proceedButton;
        public ButtonManager deleteButton;
        public Animator trackInfoPanelAnimator;
        public TMP_InputField bpmInput;
        public TMP_InputField trackNameInput;
        public TMP_InputField artistNameInput;
        public TMP_InputField albumNameInput;
        public TMP_InputField yearInput;
        public TMP_InputField genreInput;
        public BoxButtonManager loadAlbumArtButton;
        public ButtonManager difficulty_EasyButton;
        public ButtonManager difficulty_NormalButton;
        public ButtonManager difficulty_HardButton;
        public Image easyButtonBG;
        public Image normalButtonBG;
        public Image hardButtonBG;
        public Color disabledColor;

        private bool isLoadingAlbumArt = false;

        public override void Open()
        {
            base.Open();
            transform.SetAsLastSibling();
            Initialize();
        }

        public void Initialize()
        {
            foreach (var trackButton in trackButtons)
            {
                Destroy(trackButton.gameObject);
            }
            trackButtons.Clear();
            foreach (var track in EditorDataManager.Instance.Tracks)
            {
                TrackButton trackButton = Instantiate(trackButtonPrefab, contentParent);
                trackButton.Initialize(track, this);
                trackButtons.Add(trackButton);
            }
            closeButton.onClick.AddListener(OnBackButtonClick);
            bpmInput.contentType = TMP_InputField.ContentType.DecimalNumber;
            yearInput.contentType = TMP_InputField.ContentType.IntegerNumber;
            bpmInput.characterLimit = 5;
            yearInput.characterLimit = 4;
            trackNameInput.characterLimit = 70;
            artistNameInput.characterLimit = 70;
            albumNameInput.characterLimit = 70;
            genreInput.characterLimit = 70;
            bpmInput.text = "";
            trackNameInput.text = "";
            artistNameInput.text = "";
            albumNameInput.text = "";
            yearInput.text = "";
            genreInput.text = "";
        }

        private void OnBPMInputChanged(string value)
        {
            if (EditorUIManager.Instance.IsValidBPM(value, out float bpmValue))
            {
                selectedTrack.bpm = bpmValue;
                EditorManager.Instance.UpdateTrackInfo(selectedTrack, bpm: value);
            }
            else
            {
                bpmInput.text = selectedTrack.bpm.ToString();
            }
        }

        private void OnTrackNameInputChanged(string value)
        {
            selectedTrack.trackName = value;
            EditorManager.Instance.UpdateTrackInfo(selectedTrack, trackName: value);
        }

        private void OnArtistNameInputChanged(string value)
        {
            selectedTrack.artistName = value;
            EditorManager.Instance.UpdateTrackInfo(selectedTrack, artistName: value);
        }

        private void OnAlbumNameInputChanged(string value)
        {
            selectedTrack.albumName = value;
            EditorManager.Instance.UpdateTrackInfo(selectedTrack, albumName: value);
        }

        private void OnYearInputChanged(string value)
        {
            selectedTrack.year = int.Parse(value);
            EditorManager.Instance.UpdateTrackInfo(selectedTrack, year: value);
        }

        private void OnGenreInputChanged(string value)
        {
            selectedTrack.genre = value;
            EditorManager.Instance.UpdateTrackInfo(selectedTrack, genre: value);
        }

        private void OnBackButtonClick()
        {
            Close(true);
            EditorUIManager.Instance.OpenPanel(PanelType.EditorStart);
        }

        private void OnLoadAlbumArtButtonClick()
        {
            if (isLoadingAlbumArt)
                return;

            ExtensionFilter[] extensions =
            {
                new ExtensionFilter("이미지 파일", "png", "jpg", "jpeg"),
            };

            TrackData currentTrack = selectedTrack;

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
                            currentTrack,
                            () =>
                            {
                                EditorUIManager.Instance.OpenPanel(PanelType.EditorStart);
                                EditorUIManager.Instance.ClosePanel(PanelType.EditorStart);
                                EditorUIManager.Instance.OpenPanel(PanelType.LoadTrack);

                                SelectTrack(currentTrack);
                            }
                        );
                        isLoadingAlbumArt = false;
                    }
                }
            );
        }

        public void SelectTrack(TrackData track)
        {
            CleanUpListners();
            selectedTrack = track;
            bpmInput.text = selectedTrack.bpm.ToString();
            trackNameInput.text = selectedTrack.trackName;
            artistNameInput.text = selectedTrack.artistName;
            albumNameInput.text = selectedTrack.albumName;
            yearInput.text = selectedTrack.year.ToString();
            genreInput.text = selectedTrack.genre;
            easyButtonBG.color = disabledColor;
            normalButtonBG.color = disabledColor;
            hardButtonBG.color = disabledColor;
            loadAlbumArtButton.onClick.AddListener(OnLoadAlbumArtButtonClick);
            bpmInput.onValueChanged.AddListener(OnBPMInputChanged);
            trackNameInput.onValueChanged.AddListener(OnTrackNameInputChanged);
            artistNameInput.onValueChanged.AddListener(OnArtistNameInputChanged);
            albumNameInput.onValueChanged.AddListener(OnAlbumNameInputChanged);
            yearInput.onValueChanged.AddListener(OnYearInputChanged);
            genreInput.onValueChanged.AddListener(OnGenreInputChanged);
            difficulty_EasyButton.onClick.AddListener(() => SetNoteMapData(Difficulty.Easy));
            difficulty_NormalButton.onClick.AddListener(() => SetNoteMapData(Difficulty.Normal));
            difficulty_HardButton.onClick.AddListener(() => SetNoteMapData(Difficulty.Hard));
            proceedButton.onClick.AddListener(OnProceedButtonClick);
            deleteButton.onClick.AddListener(OnDeleteButtonClick);
            proceedButton.Interactable(false);

            if (selectedTrack.AlbumArt != null)
            {
                loadAlbumArtButton.SetBackground(selectedTrack.AlbumArt);
            }
            else
            {
                loadAlbumArtButton.SetBackground(EditorUIManager.Instance.defaultAlbumArt);
            }
            loadAlbumArtButton.buttonTitle = track.trackName;
            loadAlbumArtButton.UpdateUI();

            proceedButton.Interactable(false);
            trackInfoPanelAnimator.SetBool("isOpen", true);
        }

        public void SetNoteMapData(Difficulty difficulty)
        {
            selectedNoteMapData = selectedTrack.noteMapData.FirstOrDefault(n =>
                n.difficulty == difficulty
            );

            switch (difficulty)
            {
                case Difficulty.Easy:
                {
                    easyButtonBG.color = Color.white;
                    normalButtonBG.color = disabledColor;
                    hardButtonBG.color = disabledColor;
                    break;
                }
                case Difficulty.Normal:
                {
                    easyButtonBG.color = disabledColor;
                    normalButtonBG.color = Color.white;
                    hardButtonBG.color = disabledColor;
                    break;
                }
                case Difficulty.Hard:
                {
                    easyButtonBG.color = disabledColor;
                    normalButtonBG.color = disabledColor;
                    hardButtonBG.color = Color.white;
                    break;
                }
                default:
                    break;
            }

            if (selectedNoteMapData == null)
            {
                selectedNoteMapData = new NoteMapData() { difficulty = difficulty };
                switch (difficulty)
                {
                    case Difficulty.Normal:
                        selectedNoteMapData.noteMap = selectedTrack
                            .noteMapData.FirstOrDefault(n => n.difficulty == Difficulty.Easy)
                            .noteMap;
                        break;
                    case Difficulty.Hard:
                        selectedNoteMapData.noteMap = selectedTrack
                            .noteMapData.FirstOrDefault(n => n.difficulty == Difficulty.Normal)
                            .noteMap;
                        break;
                    default:
                        selectedNoteMapData.noteMap = selectedTrack
                            .noteMapData.FirstOrDefault(n => n.difficulty == Difficulty.Easy)
                            .noteMap;
                        break;
                }
                selectedTrack.noteMapData.Add(selectedNoteMapData);
                EditorManager.Instance.SaveNoteMapAsync(selectedTrack);
            }

            proceedButton.Interactable(true);
        }

        private void OnProceedButtonClick()
        {
            if (selectedNoteMapData != null)
            {
                EditorManager.Instance.SelectTrack(selectedTrack, selectedNoteMapData);
            }
        }

        private void OnDeleteButtonClick()
        {
            EditorUIManager.Instance.OpenPopUp(
                "삭제",
                "트랙에 관련된 모든 파일이 삭제됩니다. 삭제하시겠습니까?",
                () =>
                {
                    if (selectedTrack != null)
                    {
                        TrackButton trackButton = trackButtons.Find(button =>
                            button.Track == selectedTrack
                        );
                        trackButtons.Remove(trackButton);
                        if (trackButton != null)
                        {
                            Destroy(trackButton.gameObject);
                        }
                        trackInfoPanelAnimator.SetBool("isOpen", false);
                        EditorManager.Instance.RemoveTrack(selectedTrack);
                    }
                }
            );
        }

        public override void Close(bool objActive = true)
        {
            CleanUpListners();
            closeButton.onClick.RemoveAllListeners();
            loadAlbumArtButton.SetBackground(EditorUIManager.Instance.defaultAlbumArt);
            loadAlbumArtButton.buttonTitle = null;
            loadAlbumArtButton.UpdateUI();
            bpmInput.text = "";
            trackNameInput.text = "";
            artistNameInput.text = "";
            albumNameInput.text = "";
            yearInput.text = "";
            genreInput.text = "";
            selectedTrack = null;
            trackInfoPanelAnimator.SetBool("isOpen", false);
            base.Close(objActive);
        }

        private void CleanUpListners()
        {
            loadAlbumArtButton.onClick.RemoveListener(OnLoadAlbumArtButtonClick);
            proceedButton.onClick.RemoveAllListeners();
            trackNameInput.onValueChanged.RemoveAllListeners();
            artistNameInput.onValueChanged.RemoveAllListeners();
            albumNameInput.onValueChanged.RemoveAllListeners();
            yearInput.onValueChanged.RemoveAllListeners();
            genreInput.onValueChanged.RemoveAllListeners();
            deleteButton.onClick.RemoveAllListeners();
            bpmInput.onValueChanged.RemoveAllListeners();
            difficulty_EasyButton.onClick.RemoveAllListeners();
            difficulty_NormalButton.onClick.RemoveAllListeners();
            difficulty_HardButton.onClick.RemoveAllListeners();
        }
    }
}
