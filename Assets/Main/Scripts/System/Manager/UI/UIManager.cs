using System;
using System.Collections.Generic;
using System.Linq;
using Michsky.UI.Heat;
using Ookii.Dialogs;
using ProjectDM.UI;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIManager : Singleton<UIManager>, IInitializable
{
    private bool isInitialized = false;
    public bool IsInitialized => isInitialized;

    public List<Panel> PanelPrefabs = new List<Panel>();
    public List<Panel> Panels = new List<Panel>();
    public Sprite defaultAlbumArt;
    public EditorSettingsPanel editorSettingsPanel;
    private ModalWindowManager popUpWindow;

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
        if (popUpWindow == null)
        {
            ModalWindowManager popUpWindowPrefab = Resources.Load<ModalWindowManager>(
                "Prefabs/UI/Panels/UI_Panel_PopUp"
            );
            popUpWindow = Instantiate(popUpWindowPrefab, transform);
        }
        if (defaultAlbumArt == null)
        {
            defaultAlbumArt = Resources.Load<Sprite>("Textures/DefaultAlbumArt");
        }
    }

    public void ToggleSettignsPanel()
    {
        if (editorSettingsPanel != null)
        {
            if (editorSettingsPanel.IsOpen)
            {
                editorSettingsPanel.Close();
            }
            else
            {
                editorSettingsPanel.Open();
            }
        }
        else
        {
            editorSettingsPanel = OpenPanel(PanelType.EditorSettings) as EditorSettingsPanel;
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

    public bool IsValidBPM(string value, out float bpmValue)
    {
        bpmValue = 0f;

        if (string.IsNullOrEmpty(value))
            return false;

        if (!float.TryParse(value, out bpmValue))
            return false;

        if (bpmValue <= 0 || bpmValue >= 500)
            return false;

        return true;
    }
}
