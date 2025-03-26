using System;
using Michsky.UI.Heat;
using UnityEngine;
using UnityEngine.UI;

namespace NoteEditor
{
    public class TrackButton : MonoBehaviour
    {
        private ShopButtonManager trackButton;

        [SerializeField]
        private ButtonManager loadButton;

        [SerializeField]
        private Image albumArt;

        private TrackData track;

        public void Initialize(TrackData track)
        {
            this.track = track;
            trackButton = GetComponent<ShopButtonManager>();
            trackButton.buttonTitle = track.trackName;
            trackButton.buttonDescription = $"BPM : {track.bpm}";
            if (track.AlbumArt != null)
            {
                albumArt.sprite = track.AlbumArt;
            }
            trackButton.UpdateUI();
            loadButton.onClick.AddListener(LoadTrack);
        }

        private void LoadTrack()
        {
            EditorManager.Instance.SelectTrack(track);
        }

        public void OnDestroy()
        {
            trackButton.onClick.RemoveAllListeners();
        }
    }
}
