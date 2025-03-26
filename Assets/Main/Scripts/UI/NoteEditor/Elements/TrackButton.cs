using Michsky.UI.Heat;
using UnityEngine;
using UnityEngine.UI;

namespace NoteEditor
{
    public class TrackButton : MonoBehaviour
    {
        ShopButtonManager trackButton;

        [SerializeField]
        private Image albumArt;

        public void Initialize(TrackData track)
        {
            trackButton = GetComponent<ShopButtonManager>();
            trackButton.buttonTitle = track.trackName;
            trackButton.buttonDescription = $"BPM : {track.bpm}";
            if (track.AlbumArt != null)
            {
                albumArt.sprite = track.AlbumArt;
            }
            trackButton.UpdateUI();
        }
    }
}
