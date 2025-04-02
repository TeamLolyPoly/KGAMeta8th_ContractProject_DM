using System.Collections;
using System.Collections.Generic;
using Michsky.UI.Heat;
using ProjectDM;
using ProjectDM.UI;
using UnityEngine;
using UnityEngine.UI;

public class ResultPanel : Panel
{
    public override PanelType PanelType => PanelType.Result;

    public override void Open()
    {
        base.Open();
    }

    public override void Close(bool objActive = false)
    {
        resultDetailButton.onClick.RemoveListener(OnClickResultDetail);
        homeButton.onClick.RemoveListener(OnHomeButtonClick);
        restartButton.onClick.RemoveListener(OnRestartButtonClick);
        base.Close(objActive);
    }

    [SerializeField]
    private ButtonManager restartButton;

    [SerializeField]
    private ButtonManager homeButton;

    [SerializeField]
    private BoxButtonManager resultDetailButton;

    private void Start()
    {
        resultDetailButton.onClick.AddListener(OnClickResultDetail);
        homeButton.onClick.AddListener(OnHomeButtonClick);
        restartButton.onClick.AddListener(OnRestartButtonClick);
    }

    private void OnClickResultDetail()
    {
        UIManager.Instance.OpenPanel(PanelType.ResultDetail);
    }

    private void OnHomeButtonClick()
    {
        UIManager.Instance.OpenPanel(PanelType.Title);
    }

    private void OnRestartButtonClick()
    {
        //todo: 리스타트시 게임 같은곡으로 재시작 (조건 만족시)
    }
}
