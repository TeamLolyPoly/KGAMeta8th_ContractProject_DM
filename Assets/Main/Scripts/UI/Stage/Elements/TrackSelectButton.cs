using System.Collections;
using System.Collections.Generic;
using Michsky.UI.Heat;
using UnityEngine;
using UnityEngine.UI;

public class TrackSelectButton : MonoBehaviour
{
    private TrackSelectPanel trackSelectPanel;

    [SerializeField]
    private ShopButtonManager trackButton;

    [SerializeField]
    private ButtonManager selectButton;

    [SerializeField]
    private Image[] albumArt;

    private TrackData track;

    public TrackData Track => track;

    public void Initialize(TrackData track, TrackSelectPanel trackSelectPanel)
    {
        this.track = track;
        this.trackSelectPanel = trackSelectPanel;
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
        trackSelectPanel.SelectTrack(track);
    }
}
