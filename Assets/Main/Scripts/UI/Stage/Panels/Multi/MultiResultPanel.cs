using System.Collections;
using System.Collections.Generic;
using ProjectDM.UI;
using UnityEngine;

public class MultiResultPanel : Panel
{
    public override PanelType PanelType => PanelType.Multi_Result;

    public override void Open()
    {
        base.Open();
    }

    public override void Close(bool objActive = true)
    {
        base.Close(objActive);
    }
}
