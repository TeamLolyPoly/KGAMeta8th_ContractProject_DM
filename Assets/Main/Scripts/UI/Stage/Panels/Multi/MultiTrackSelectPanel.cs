using System.Collections.Generic;
using System.Linq;
using Michsky.UI.Heat;
using ProjectDM.UI;
using UnityEngine;
using UnityEngine.UI;

public class MultiTrackSelectPanel : Panel
{
    public override PanelType PanelType => PanelType.Multi_TrackSelect;

    private TrackData selectedTrack;
    private NoteMapData selectedNoteMapData;
    private NoteMap selectedNoteMap;

    [SerializeField]
    private ButtonManager closeButton;

    [SerializeField]
    private MultiTrackSelectButton trackButtonPrefab;

    [SerializeField]
    private Transform trackParent;

    [SerializeField]
    private Animator trackInfoPanel;

    [SerializeField]
    private ButtonManager difficulty_EasyButton;

    [SerializeField]
    private ButtonManager difficulty_NormalButton;

    [SerializeField]
    private ButtonManager difficulty_HardButton;

    [SerializeField]
    private Image easyButtonBG;

    [SerializeField]
    private Image normalButtonBG;

    [SerializeField]
    private Image hardButtonBG;

    [SerializeField]
    private ButtonManager selectButton;

    [SerializeField]
    private BoxButtonManager albumArtButton;

    [SerializeField]
    private Color disabledColor;

    private List<MultiTrackSelectButton> trackButtons = new List<MultiTrackSelectButton>();

    public override void Open()
    {
        base.Open();
        transform.SetAsLastSibling();
    }

    public void Initialize(List<TrackData> tracks)
    {
        foreach (var track in tracks)
        {
            MultiTrackSelectButton trackButton = Instantiate(trackButtonPrefab, trackParent);
            trackButton.Initialize(track, this);
            trackButtons.Add(trackButton);
        }
        closeButton.onClick.AddListener(OnCloseButtonClick);
    }

    private void OnCloseButtonClick()
    {
        Close(true);
        StageUIManager.Instance.OpenPanel(PanelType.AlbumSelect);
    }

    public void SelectTrack(TrackData track)
    {
        CleanUpListners();
        selectedTrack = track;
        easyButtonBG.color = disabledColor;
        normalButtonBG.color = disabledColor;
        hardButtonBG.color = disabledColor;
        difficulty_EasyButton.onClick.AddListener(() => SetNoteMapData(Difficulty.Easy));
        difficulty_NormalButton.onClick.AddListener(() => SetNoteMapData(Difficulty.Normal));
        difficulty_HardButton.onClick.AddListener(() => SetNoteMapData(Difficulty.Hard));
        selectButton.onClick.AddListener(OnStartButtonClick);
        selectButton.Interactable(false);

        CheckNoteMapData();

        if (selectedTrack.AlbumArt != null)
        {
            albumArtButton.SetBackground(selectedTrack.AlbumArt);
        }
        else
        {
            albumArtButton.SetBackground(StageUIManager.Instance.DefaultAlbumArt);
        }

        albumArtButton.buttonTitle = track.trackName;
        albumArtButton.UpdateUI();
        albumArtButton.SetInteractable(false);

        selectButton.Interactable(false);
        trackInfoPanel.SetBool("subOpen", true);
    }

    private void CheckNoteMapData()
    {
        if (selectedTrack.noteMapData.Any(n => n.difficulty == Difficulty.Easy) == true)
        {
            difficulty_EasyButton.Interactable(true);
        }
        else
        {
            difficulty_EasyButton.Interactable(false);
        }
        if (selectedTrack.noteMapData.Any(n => n.difficulty == Difficulty.Normal) == true)
        {
            difficulty_NormalButton.Interactable(true);
        }
        else
        {
            difficulty_NormalButton.Interactable(false);
        }
        if (selectedTrack.noteMapData.Any(n => n.difficulty == Difficulty.Hard) == true)
        {
            difficulty_HardButton.Interactable(true);
        }
        else
        {
            difficulty_HardButton.Interactable(false);
        }
    }

    public void SetNoteMapData(Difficulty difficulty)
    {
        selectedNoteMapData = selectedTrack.noteMapData.FirstOrDefault(n =>
            n.difficulty == difficulty
        );

        switch (difficulty)
        {
            case Difficulty.Easy:
            {
                easyButtonBG.color = Color.white;
                normalButtonBG.color = disabledColor;
                hardButtonBG.color = disabledColor;
                break;
            }
            case Difficulty.Normal:
            {
                easyButtonBG.color = disabledColor;
                normalButtonBG.color = Color.white;
                hardButtonBG.color = disabledColor;
                break;
            }
            case Difficulty.Hard:
            {
                easyButtonBG.color = disabledColor;
                normalButtonBG.color = disabledColor;
                hardButtonBG.color = Color.white;
                break;
            }
            default:
                break;
        }

        selectButton.Interactable(true);
    }

    private void OnStartButtonClick()
    {
        if (selectedNoteMapData != null)
        {
            selectedNoteMap = selectedNoteMapData.noteMap;
            GameManager.Instance.StartGame(selectedTrack, selectedNoteMap);
        }
    }

    private void CleanUpListners()
    {
        difficulty_EasyButton.onClick.RemoveAllListeners();
        difficulty_NormalButton.onClick.RemoveAllListeners();
        difficulty_HardButton.onClick.RemoveAllListeners();
        closeButton.onClick.RemoveAllListeners();
        selectButton.onClick.RemoveAllListeners();
    }

    public override void Close(bool objActive = true)
    {
        CleanUpListners();
        foreach (var trackButton in trackButtons)
        {
            Destroy(trackButton.gameObject);
        }
        trackButtons.Clear();
        selectedTrack = null;
        selectedNoteMapData = null;
        selectedNoteMap = null;
        trackInfoPanel.SetBool("subOpen", false);
        base.Close(objActive);
    }
}
