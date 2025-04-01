using Michsky.UI.Heat;
using Ookii.Dialogs;
using ProjectDM.UI;
using SFB;
using TMPro;
using UnityEngine;

namespace NoteEditor
{
    public class NewTrackPanel : Panel
    {
        public override PanelType PanelType => PanelType.NewTrack;
        private bool isLoadingTrack = false;

        [SerializeField]
        private ButtonManager closeButton;

        [SerializeField]
        private ButtonManager proceedButton;

        [SerializeField]
        private BoxButtonManager loadTrackButton;

        [SerializeField]
        private TMP_InputField bpmInput;

        [SerializeField]
        private TMP_InputField trackNameInput;

        [SerializeField]
        private TMP_InputField artistNameInput;

        [SerializeField]
        private TMP_InputField albumNameInput;

        [SerializeField]
        private TMP_InputField yearInput;

        [SerializeField]
        private TMP_InputField genreInput;

        public override void Open()
        {
            base.Open();
            transform.SetAsLastSibling();
            closeButton.onClick.AddListener(OnBackButtonClick);
            loadTrackButton.onClick.AddListener(LoadTrack);
            proceedButton.onClick.AddListener(Proceed);
        }

        public override void Close(bool objActive = false)
        {
            loadTrackButton.SetText("새 트랙");
            loadTrackButton.SetBackground(UIManager.Instance.defaultAlbumArt);
            trackNameInput.text = "";
            artistNameInput.text = "";
            albumNameInput.text = "";
            yearInput.text = "";
            genreInput.text = "";
            closeButton.onClick.RemoveListener(OnBackButtonClick);
            loadTrackButton.onClick.RemoveListener(LoadTrack);
            proceedButton.onClick.RemoveListener(Proceed);
            base.Close(objActive);
        }

        private bool SaveMetaData()
        {
            string BPM = bpmInput.text;
            if (string.IsNullOrEmpty(BPM))
                return false;

            string trackName = trackNameInput.text;
            if (string.IsNullOrEmpty(trackName))
                return false;

            string artistName = artistNameInput.text;
            if (string.IsNullOrEmpty(artistName))
                return false;

            string albumName = albumNameInput.text;
            string year = yearInput.text;
            string genre = genreInput.text;

            EditorManager.Instance.UpdateTrackInfo(
                EditorManager.Instance.CurrentTrack,
                bpm: BPM,
                trackName: trackName,
                artistName: artistName,
                albumName: albumName != string.Empty ? albumName : null,
                year: year != string.Empty ? year : null,
                genre: genre != string.Empty ? genre : null
            );

            return true;
        }

        public void SetInfo(TrackData track)
        {
            loadTrackButton.SetText(track.trackName);
            if (track.AlbumArt != null)
            {
                loadTrackButton.SetBackground(track.AlbumArt);
            }
            trackNameInput.text = track.trackName;
            artistNameInput.text = track.artistName;
            albumNameInput.text = track.albumName;
            yearInput.text = track.year.ToString();
            genreInput.text = track.genre;
        }

        public void LoadTrack()
        {
            if (isLoadingTrack)
                return;

            ExtensionFilter[] extensions =
            {
                new ExtensionFilter("오디오 파일", "mp3", "wav", "ogg"),
            };

            print("[Loading] LoadTrack 호출됨");

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
                        EditorManager.Instance.LoadTrack(paths[0]);
                        isLoadingTrack = false;
                    }
                }
            );
        }

        public void Proceed()
        {
            if (SaveMetaData())
            {
                EditorManager.Instance.SelectTrack(
                    EditorManager.Instance.CurrentTrack,
                    EditorManager.Instance.CurrentNoteMapData
                );
                EditorDataManager.Instance.TmpTrack = null;
            }
            else if (EditorManager.Instance.CurrentTrack == null)
            {
                UIManager.Instance.OpenPopUp("오류", "오디오 파일을 로드하지 않았습니다");
            }
            else
            {
                UIManager.Instance.OpenPopUp("오류", "BPM , 트랙명 , 아티스트명은 필수 정보입니다");
            }
        }

        private void OnBackButtonClick()
        {
            if (EditorDataManager.Instance.TmpTrack != null)
            {
                EditorManager.Instance.RemoveTrack(EditorDataManager.Instance.TmpTrack);
                EditorDataManager.Instance.TmpTrack = null;
            }
            UIManager.Instance.OpenPanel(PanelType.EditorStart);
            Close(true);
        }
    }
}
