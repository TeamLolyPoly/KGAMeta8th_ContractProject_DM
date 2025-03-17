using System.Collections.Generic;
using System.Linq;
using Michsky.UI.Heat;
using ProjectDM.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManager : Singleton<UIManager>, IInitializable
{
    private bool isInitialized = false;
    public bool IsInitialized => isInitialized;

    public List<Panel> PanelPrefabs = new List<Panel>();

    private List<Panel> Panels = new List<Panel>();

    private void Start()
    {
        Initialize();
    }

    public void Initialize()
    {
        LoadResources();

        if (SceneManager.GetActiveScene().name != "NoteEditor")
        {
            CloseAllPanels();
            Debug.Log("모든 패널 닫기 완료");

            OpenPanel(PanelType.Title);
            Debug.Log("Title 패널 열기 완료");
        }
        else
        {
            InitializeComponents();
        }

        isInitialized = true;
    }

    private void InitializeComponents()
    {
        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject eventSystem = Instantiate(
                Resources.Load<GameObject>("Prefabs/Utils/EventSystem")
            );
            eventSystem.transform.SetParent(transform);
        }

        Canvas cv = gameObject.AddComponent<Canvas>();
        cv.renderMode = RenderMode.ScreenSpaceOverlay;
        GraphicRaycaster gr = gameObject.AddComponent<GraphicRaycaster>();
        gr.enabled = true;
        gr.ignoreReversedGraphics = true;
        gr.blockingObjects = GraphicRaycaster.BlockingObjects.None;
        gr.blockingMask = -1;
        CanvasScaler cs = gameObject.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        cs.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        cs.matchWidthOrHeight = 0.5f;
        cs.referencePixelsPerUnit = 100;
        gameObject.AddComponent<CanvasManager>();
    }

    private void LoadResources()
    {
        if (PanelPrefabs.Count == 0)
        {
            PanelPrefabs = Resources.LoadAll<Panel>("Prefabs/UI/Panels/Stage").ToList();
            PanelPrefabs.AddRange(Resources.LoadAll<Panel>("Prefabs/UI/Panels/NoteEditor"));
        }
    }

    public Panel OpenPanel(PanelType panelType)
    {
        Panel panel = Panels.Find(p => p.PanelType == panelType);
        if (panel != null)
        {
            panel.Open();
            return panel;
        }
        else
        {
            Panel prefab = PanelPrefabs.Find(p => p.PanelType == panelType);
            if (prefab != null)
            {
                Panel instance = Instantiate(prefab, transform);
                Panels.Add(instance);
                instance.Open();
                return instance;
            }
        }
        Debug.LogWarning(
            $"패널 {panelType} 프리팹 할당되어있지 않거나 리소스 경로에 존재하지 않습니다"
        );
        return null;
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

    public void ToggleOptionPanel()
    {
        Panel optionPanel = Panels.Find(p => p.PanelType == PanelType.Option);
        if (optionPanel == null)
        {
            Debug.LogError("Option panel not found");
            return;
        }

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
