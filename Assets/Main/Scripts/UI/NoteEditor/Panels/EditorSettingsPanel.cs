using System.Collections;
using System.Collections.Generic;
using Michsky.UI.Heat;
using NoteEditor;
using ProjectDM.UI;
using UnityEngine;
using UnityEngine.UI;

public class EditorSettingsPanel : Panel
{
    public override PanelType PanelType => PanelType.EditorSettings;

    [SerializeField]
    private SliderManager volumeSlider;

    [SerializeField]
    private ButtonManager backToMainButton;

    public override void Open()
    {
        volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        volumeSlider.mainSlider.value = AudioManager.Instance.Volume;
        backToMainButton.onClick.AddListener(OnBackToMainButtonClicked);
        base.Open();
    }

    private void OnVolumeChanged(float value)
    {
        AudioManager.Instance.SetVolume(value);
    }

    public override void Close(bool objActive = true)
    {
        volumeSlider.onValueChanged.RemoveListener(OnVolumeChanged);
        base.Close(objActive);
    }

    private void OnBackToMainButtonClicked()
    {
        EditorManager.Instance.BackToMain();
    }
}
