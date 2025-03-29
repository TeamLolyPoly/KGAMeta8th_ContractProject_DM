using Michsky.UI.Heat;
using UnityEngine;
using UnityEngine.UI;

namespace NoteEditor
{
    public class TrackButton : MonoBehaviour
    {
        [SerializeField]
        private BoxButtonManager loadButton;

        [SerializeField]
        private Image albumArt;

        private TrackData track;

        public void Initialize(TrackData track)
        {
            this.track = track;
            loadButton.buttonTitle = track.trackName;
            loadButton.UpdateUI();
            if (track.AlbumArt != null)
            {
                albumArt.sprite = track.AlbumArt;
            }
            loadButton.onClick.AddListener(LoadTrack);
        }

        private void LoadTrack()
        {
            EditorManager.Instance.SelectTrack(track);
            loadButton.onClick.RemoveAllListeners();
        }
    }
}
