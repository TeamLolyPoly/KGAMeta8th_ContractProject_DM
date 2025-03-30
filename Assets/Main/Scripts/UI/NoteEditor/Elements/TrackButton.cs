using Michsky.UI.Heat;
using UnityEngine;
using UnityEngine.UI;

namespace NoteEditor
{
    public class TrackButton : MonoBehaviour
    {
        private LoadTrackPanel loadTrackPanel;

        [SerializeField]
        private ShopButtonManager trackButton;

        [SerializeField]
        private ButtonManager selectButton;

        [SerializeField]
        private Image[] albumArt;

        private TrackData track;

        public void Initialize(TrackData track, LoadTrackPanel loadTrackPanel)
        {
            this.track = track;
            this.loadTrackPanel = loadTrackPanel;
            trackButton.buttonTitle = track.trackName;
            trackButton.UpdateUI();
            if (track.AlbumArt != null)
            {
                foreach (var art in albumArt)
                {
                    art.sprite = track.AlbumArt;
                }
            }
            selectButton.onClick.AddListener(OnSelectTrack);
        }

        private void OnSelectTrack()
        {
            loadTrackPanel.SelectTrack(track);
        }
    }
}
