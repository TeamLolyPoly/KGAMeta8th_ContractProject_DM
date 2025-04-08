using System;
using System.Collections.Generic;
using System.Linq;
using Michsky.UI.Heat;
using ProjectDM.UI;
using UnityEngine;
using UnityEngine.Android;

public class StageUIManager : Singleton<StageUIManager>, IInitializable
{
    private bool isInitialized = false;
    public bool IsInitialized => isInitialized;

    private List<Panel> PanelPrefabs = new List<Panel>();

    private List<Panel> panels = new List<Panel>();
    public List<Panel> Panels => panels;

    [SerializeField]
    private Sprite defaultAlbumArt;

    public Sprite DefaultAlbumArt => defaultAlbumArt;

    [SerializeField]
    private ModalWindowManager popUpWindow;

    [SerializeField]
    private Animator logSystemAnimator;

    [SerializeField]
    private Canvas mainCanvas;

    public Canvas MainCanvas => mainCanvas;

    [SerializeField]
    private Canvas editorCanvas;

    public Canvas EditorCanvas => editorCanvas;

    [SerializeField]
    public List<Animator> debugPanels = new List<Animator>();

    [SerializeField]
    private ButtonManager debugButton;

    [SerializeField]
    private LogSystem logSystem;

    public LogSystem LogSystem => logSystem;

    public void Initialize()
    {
        LoadResources();
        logSystem.Initialize();
        foreach (var panel in debugPanels)
        {
            panel.SetBool("subOpen", false);
        }

        isInitialized = true;
    }

    private void LoadResources()
    {
        if (PanelPrefabs.Count == 0)
        {
            PanelPrefabs = Resources.LoadAll<Panel>("Prefabs/UI/Panels/Stage").ToList();
        }
        if (popUpWindow == null)
        {
            ModalWindowManager popUpWindowPrefab = Resources.Load<ModalWindowManager>(
                "Prefabs/UI/Panels/General/UI_Panel_PopUp"
            );
            popUpWindow = Instantiate(popUpWindowPrefab, transform);
        }
        if (defaultAlbumArt == null)
        {
            defaultAlbumArt = Resources.Load<Sprite>("Textures/DefaultAlbumArt");
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
                Panel instance = Instantiate(prefab, mainCanvas.transform);
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

    public void ClosePanel(PanelType panelType, bool objActive = true)
    {
        Panel panel = Panels.Find(p => p.PanelType == panelType);
        if (panel == null)
        {
            Debug.LogError($"PanelType {panelType} not found");
            return;
        }
        panel.Close(objActive);
    }

    public void OpenPopUp(string title, string description, Action OnConfirm = null)
    {
        popUpWindow.windowTitle.text = title;
        popUpWindow.windowDescription.text = description;
        popUpWindow.showCancelButton = false;
        popUpWindow.closeOnConfirm = true;
        popUpWindow.transform.SetAsLastSibling();
        popUpWindow.OpenWindow();
        popUpWindow.onConfirm.AddListener(() => OnConfirm?.Invoke());
    }

    public Panel GetPanel(PanelType panelType)
    {
        return Panels.Find(p => p.PanelType == panelType);
    }

    public void CloseAllPanels()
    {
        foreach (var panel in Panels)
        {
            panel.Close(false);
        }
        if (popUpWindow.isOn)
        {
            popUpWindow.CloseWindow();
        }
    }

    public void EnableDebugMode()
    {
        debugButton.gameObject.SetActive(true);
        debugButton.onClick.AddListener(ToggleDebugUI);
    }

    public void ToggleDebugUI()
    {
        foreach (var panel in debugPanels)
        {
            panel.SetBool("subOpen", !panel.GetBool("subOpen"));
        }
    }
}
