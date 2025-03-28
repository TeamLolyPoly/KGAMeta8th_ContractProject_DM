using System.Collections;
using System.Collections.Generic;
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
        private List<TrackButton> trackButtons = new List<TrackButton>();

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
                trackButton.Initialize(track);
                trackButtons.Add(trackButton);
            }
            backButton.onClick.AddListener(OnBackButtonClick);
        }

        private void OnBackButtonClick()
        {
            Close(true);
            UIManager.Instance.OpenPanel(PanelType.EditorStart);
        }

        public override void Close(bool objActive = false)
        {
            base.Close(objActive);
            backButton.onClick.RemoveAllListeners();
        }
    }
}
