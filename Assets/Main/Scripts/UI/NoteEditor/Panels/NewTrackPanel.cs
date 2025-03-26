using System.Collections;
using Michsky.UI.Heat;
using ProjectDM.UI;
using SFB;

namespace NoteEditor
{
    public class NewTrackPanel : Panel
    {
        public override PanelType PanelType => PanelType.NewTrack;
        private bool isLoadingTrack = false;
        public PanelButton BackButton;

        public override void Open()
        {
            base.Open();
            BackButton.onClick.AddListener(OnBackButtonClick);
        }

        public override void Close()
        {
            BackButton.onClick.RemoveListener(OnBackButtonClick);
            base.Close();
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
                        EditorManager.Instance.LoadAudioFile(paths[0]);
                        isLoadingTrack = false;
                    }
                }
            );
        }

        private void OnBackButtonClick()
        {
            UIManager.Instance.OpenPanel(PanelType.EditorStart);
            Close();
        }
    }
}
