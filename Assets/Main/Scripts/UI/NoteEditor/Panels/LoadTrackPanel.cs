using System.Collections;
using System.Collections.Generic;
using Michsky.UI.Heat;
using ProjectDM.UI;
using SFB;
using TMPro;
using UnityEngine;

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
        public ButtonManager proceedButton;
        public ButtonManager deleteButton;
        public Animator trackInfoPanelAnimator;
        public TMP_InputField bpmInputField;
        public TMP_InputField trackNameInputField;
        public TMP_InputField artistNameInputField;
        public TMP_InputField albumNameInputField;
        public TMP_InputField yearInputField;
        public TMP_InputField genreInputField;
        public BoxButtonManager loadAlbumArtButton;

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
            proceedButton.onClick.AddListener(OnProceedButtonClick);
            deleteButton.onClick.AddListener(OnDeleteButtonClick);
        }

        private void OnBPMInputChanged(string value)
        {
            selectedTrack.bpm = float.Parse(value);
            EditorManager.Instance.UpdateTrackInfo(selectedTrack, bpm: value);
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
            UIManager.Instance.OpenPanel(PanelType.EditorStart);
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
                                UIManager.Instance.OpenPanel(PanelType.EditorStart);
                                UIManager.Instance.ClosePanel(PanelType.EditorStart);
                                UIManager.Instance.OpenPanel(PanelType.LoadTrack);

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
            selectedTrack = track;
            bpmInputField.text = selectedTrack.bpm.ToString();
            trackNameInputField.text = selectedTrack.trackName;
            artistNameInputField.text = selectedTrack.artistName;
            albumNameInputField.text = selectedTrack.albumName;
            yearInputField.text = selectedTrack.year.ToString();
            genreInputField.text = selectedTrack.genre;
            if (selectedTrack.AlbumArt != null)
            {
                loadAlbumArtButton.SetBackground(selectedTrack.AlbumArt);
            }
            else
            {
                loadAlbumArtButton.SetBackground(UIManager.Instance.defaultAlbumArt);
            }
            loadAlbumArtButton.buttonTitle = track.trackName;
            loadAlbumArtButton.UpdateUI();
            loadAlbumArtButton.onClick.AddListener(OnLoadAlbumArtButtonClick);
            bpmInputField.onValueChanged.AddListener(OnBPMInputChanged);
            trackNameInputField.onValueChanged.AddListener(OnTrackNameInputChanged);
            artistNameInputField.onValueChanged.AddListener(OnArtistNameInputChanged);
            albumNameInputField.onValueChanged.AddListener(OnAlbumNameInputChanged);
            yearInputField.onValueChanged.AddListener(OnYearInputChanged);
            genreInputField.onValueChanged.AddListener(OnGenreInputChanged);
            trackInfoPanelAnimator.SetBool("isOpen", true);
        }

        private void OnProceedButtonClick()
        {
            EditorManager.Instance.SelectTrack(selectedTrack);
        }

        private void OnDeleteButtonClick()
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

        public override void Close(bool objActive = false)
        {
            loadAlbumArtButton.onClick.RemoveListener(OnLoadAlbumArtButtonClick);
            closeButton.onClick.RemoveAllListeners();
            proceedButton.onClick.RemoveAllListeners();
            trackNameInputField.onValueChanged.RemoveAllListeners();
            artistNameInputField.onValueChanged.RemoveAllListeners();
            albumNameInputField.onValueChanged.RemoveAllListeners();
            yearInputField.onValueChanged.RemoveAllListeners();
            genreInputField.onValueChanged.RemoveAllListeners();
            deleteButton.onClick.RemoveAllListeners();
            bpmInputField.onValueChanged.RemoveAllListeners();
            loadAlbumArtButton.SetBackground(null);
            loadAlbumArtButton.buttonTitle = null;
            loadAlbumArtButton.UpdateUI();
            bpmInputField.text = "";
            trackNameInputField.text = "";
            artistNameInputField.text = "";
            albumNameInputField.text = "";
            yearInputField.text = "";
            genreInputField.text = "";
            selectedTrack = null;
            trackInfoPanelAnimator.SetBool("isOpen", false);
            base.Close(objActive);
        }
    }
}
