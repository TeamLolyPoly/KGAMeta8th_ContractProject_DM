using System.Collections;
using System.Collections.Generic;
using Michsky.UI.Heat;
using ProjectDM.UI;
using UnityEngine;

public class OptionPanel : Panel
{
    public override PanelType PanelType => PanelType.Option;

    [SerializeField]
    private BoxButtonManager closeButton;

    public override void Open()
    {
        closeButton.onClick.AddListener(OnCloseButtonClick);
        base.Open();
    }

    public override void Close(bool objActive = false)
    {
        closeButton.onClick.RemoveListener(OnCloseButtonClick);
        base.Close(objActive);
    }

    private void OnCloseButtonClick()
    {
        UIManager.Instance.ClosePanel(PanelType.Option);
    }
}
