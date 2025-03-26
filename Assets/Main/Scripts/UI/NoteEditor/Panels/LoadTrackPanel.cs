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
        public List<TrackButton> trackButtons = new List<TrackButton>();

        private IEnumerator Start()
        {
            yield return new WaitUntil(() => EditorDataManager.Instance.IsInitialized);
            animator.SetBool("isOpen", true);
            Initialize();
        }

        public override void Open()
        {
            base.Open();
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
            backButton.onClick.AddListener(Close);
        }

        public override void Close()
        {
            backButton.onClick.RemoveAllListeners();
            base.Close();
            UIManager.Instance.OpenPanel(PanelType.EditorStart);
        }
    }
}
