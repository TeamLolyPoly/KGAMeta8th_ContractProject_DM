using System.Collections;
using ProjectDM.UI;
using UnityEngine;

public class MultiWaitingPanel : Panel
{
    public override PanelType PanelType => PanelType.Multi_Waiting;

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
        MultiRoomPanel multiRoomPanel =
            StageUIManager.Instance.OpenPanel(PanelType.Multi_Room) as MultiRoomPanel;
        multiRoomPanel.InitializeClient();
    }

    public IEnumerator OnSearchFailed()
    {
        waitingBox.SetBool("isOpen", false);
        failedBox.SetBool("isOpen", true);
        yield return new WaitForSeconds(1f);
        Close(false);
        MultiRoomPanel multiRoomPanel =
            StageUIManager.Instance.OpenPanel(PanelType.Multi_Room) as MultiRoomPanel;
        multiRoomPanel.InitializeHost();
    }

    public override void Close(bool objActive = true)
    {
        base.Close(objActive);
        waitingBox.SetBool("isOpen", false);
        findBox.SetBool("isOpen", false);
        failedBox.SetBool("isOpen", false);
    }
}
