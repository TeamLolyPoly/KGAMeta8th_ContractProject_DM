using System.Collections;
using Michsky.UI.Heat;
using ProjectDM.UI;
using UnityEngine;

namespace NoteEditor
{
    public class LoadTrackPanel : Panel
    {
        public override PanelType PanelType => PanelType.LoadTrack;
        public RectTransform contentParent;
        public TrackButton trackButtonPrefab;
        public ButtonManager backButton;

        public override void Open()
        {
            base.Open();
            Initialize();
        }

        public void Initialize()
        {
            foreach (var track in EditorDataManager.Instance.GetAllTracks())
            {
                TrackButton trackButton = Instantiate(trackButtonPrefab, contentParent);
                trackButton.Initialize(track);
            }
            backButton.onClick.AddListener(Close);
        }

        public override void Close()
        {
            backButton.onClick.RemoveAllListeners();
            base.Close();
        }
    }
}
