using System.Collections;
using System.Collections.Generic;
using ProjectDM.UI;
using UnityEngine;

public enum PanelType
{
    Main,
    Mode,
    Option,
    Music,
}

public class UIManager : Singleton<UIManager>, IInitializable
{
    private bool isInitialized = false;
    public bool IsInitialized => isInitialized;

    public List<Panel> PanelPrefabs = new List<Panel>();

    private List<Panel> Panels = new List<Panel>();

    private void Start()
    {
        Debug.Log("UIManager Start");
        Initialize();
    }

    public void Initialize()
    {
        //초기화 할때 필요한 과정 넣기
        //패널의 참조나 , 리소스에서 로드하는 것등등
        CloseAllPanels(); // 모든 패널을 닫고 시작
        Debug.Log("모든 패널 닫기 완료 ");
        // 메인 패널 열기
        OpenPanel(PanelType.Main);
        Debug.Log("메인 패널 열기 완료 ");

        isInitialized = true;
    }

    public void OpenPanel(PanelType panelType)
    {
        Panel panel = Panels.Find(p => p.PanelType == panelType);
        if (panel == null)
        {
            panel = Instantiate(PanelPrefabs.Find(p => p.PanelType == panelType));
            Panels.Add(panel);
        }

        panel.Open();
    }

    public void ClosePanel(PanelType panelType)
    {
        Panel panel = Panels.Find(p => p.PanelType == panelType);
        if (panel == null)
        {
            Debug.LogError($"PanelType {panelType} not found");
            return;
        }
        panel.Close();
    }

    public void CloseAllPanels()
    {
        foreach (var panel in Panels)
        {
            panel.Close();
        }
    }
}
