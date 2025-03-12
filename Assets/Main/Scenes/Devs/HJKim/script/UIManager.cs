using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ProjectDM.UI;
using UnityEngine;

public enum PanelType
{
    Title,
    Mode,
    Album,
    Music,
    Difficult,
    Option,
    Result,
    ResultDetail,
}

public class UIManager : Singleton<UIManager>, IInitializable
{
    [SerializeField]
    private Canvas mainCanvas;
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
        // Canvas 아래의 모든 패널 찾기
        Panels = mainCanvas.GetComponentsInChildren<Panel>(true).ToList();
        Debug.Log($"Found {Panels.Count} panels in Canvas");

        // 모든 패널 닫기
        CloseAllPanels();
        Debug.Log("모든 패널 닫기 완료");

        // Title 패널만 열기
        OpenPanel(PanelType.Title);
        Debug.Log("Title 패널 열기 완료");

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
