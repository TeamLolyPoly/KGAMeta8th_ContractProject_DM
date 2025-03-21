using System.Collections.Generic;
using System.Linq;
using ProjectDM.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIManager : Singleton<UIManager>, IInitializable
{
    private bool isInitialized = false;
    public bool IsInitialized => isInitialized;

    public List<Panel> PanelPrefabs = new List<Panel>();
    public List<Panel> Panels = new List<Panel>();

    private void Start()
    {
        Initialize();
    }

    public void Initialize()
    {
        LoadResources();
        InitializeComponents();
        isInitialized = true;
    }

    private void InitializeComponents()
    {
        if (GetComponentInChildren<EventSystem>() == null)
        {
            GameObject eventSystem = Instantiate(
                Resources.Load<GameObject>("Prefabs/Utils/EventSystem")
            );
            eventSystem.transform.SetParent(transform);
        }
        if (GetComponentInChildren<Canvas>() == null)
        {
            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }
        if (GetComponentInChildren<CanvasScaler>() == null)
        {
            CanvasScaler canvasScaler = gameObject.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080);
        }
        if (GetComponentInChildren<GraphicRaycaster>() == null)
        {
            gameObject.AddComponent<GraphicRaycaster>();
        }
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
                instance.name = prefab.name;
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
}
