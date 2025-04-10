using System.Collections;
using System.Collections.Generic;
using ProjectDM.UI;
using UnityEngine;

public class MultiRoomPanel : Panel
{
    public override PanelType PanelType => PanelType.MultiRoom;

    public override void Open()
    {
        base.Open();
    }

    public override void Close(bool objActive = true)
    {
        base.Close(objActive);
    }
}
