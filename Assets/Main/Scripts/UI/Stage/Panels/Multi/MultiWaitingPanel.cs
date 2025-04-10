using System.Collections;
using ProjectDM.UI;
using UnityEngine;

public class MultiWaitingPanel : Panel
{
    public override PanelType PanelType => PanelType.MultiWaiting;

    public Animator failedBox;
    public Animator waitingBox;
    public Animator findBox;

    public override void Open()
    {
        base.Open();
        waitingBox.SetBool("isOpen", true);
    }

    public IEnumerator OnRoomFound()
    {
        waitingBox.SetBool("isOpen", false);
        findBox.SetBool("isOpen", true);
        yield return new WaitForSeconds(1f);
        Close(false);
        StageUIManager.Instance.OpenPanel(PanelType.MultiStatus);
        StageUIManager.Instance.OpenPanel(PanelType.MultiRoom);
    }

    public IEnumerator OnSearchFailed()
    {
        waitingBox.SetBool("isOpen", false);
        failedBox.SetBool("isOpen", true);
        yield return new WaitForSeconds(1f);
        Close(false);
        StageUIManager.Instance.OpenPanel(PanelType.MultiStatus);
        StageUIManager.Instance.OpenPanel(PanelType.MultiRoom);
    }

    public override void Close(bool objActive = true)
    {
        base.Close(objActive);
        waitingBox.SetBool("isOpen", false);
        findBox.SetBool("isOpen", false);
        failedBox.SetBool("isOpen", false);
    }
}
