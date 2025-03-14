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
    ResultDetail,
    Result,
    Option,
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
        // 옵션 패널은 다른 패널들을 닫지 않고 열립니다
        if (panelType == PanelType.Option)
        {
            Panel optionPanel = Panels.Find(p => p.PanelType == panelType);
            if (optionPanel != null)
            {
                optionPanel.Open();
            }
            return;
        }

        // 다른 패널들은 기존 방식대로 처리
        CloseAllPanels();

        Panel panel = Panels.Find(p => p.PanelType == panelType);
        if (panel != null)
        {
            panel.Open();
        }
        else
        {
            Debug.LogError($"Panel not found: {panelType}");
        }
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

    // 옵션 패널의 토글을 위한 메서드
    public void ToggleOptionPanel()
    {
        Panel optionPanel = Panels.Find(p => p.PanelType == PanelType.Option);
        if (optionPanel == null)
        {
            Debug.LogError("Option panel not found");
            return;
        }

        // 옵션 패널이 활성화되어 있으면 닫고, 아니면 엽니다
        if (optionPanel.gameObject.activeSelf)
        {
            optionPanel.Close();
        }
        else
        {
            optionPanel.Open();
        }
    }
}
