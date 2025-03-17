using Michsky.UI.Heat;
using ProjectDM;
using ProjectDM.UI;
using UnityEngine;

public class DifficultPanel : Panel
{
    public override PanelType PanelType => PanelType.Difficult;

    public override void Open()
    {
        base.Open();
    }

    public override void Close()
    {
        backButton.onClick.RemoveListener(OnBackButtonClick);
        easyButton.onClick.RemoveListener(OnEasyButtonClick);
        normalButton.onClick.RemoveListener(OnNormalButtonClick);
        hardButton.onClick.RemoveListener(OnHardButtonClick);
        gameStartButton.onClick.RemoveListener(OnGameStartButtonClick);
        base.Close();
    }

    [SerializeField]
    private PanelButton backButton;

    //todo:랭킹 스크롤 작업 해야함

    [SerializeField]
    private ButtonManager easyButton;

    [SerializeField]
    private ButtonManager normalButton;

    [SerializeField]
    private ButtonManager hardButton;

    [SerializeField]
    private ButtonManager gameStartButton;

    private void Start()
    {
        backButton.onClick.AddListener(OnBackButtonClick);
        easyButton.onClick.AddListener(OnEasyButtonClick);
        normalButton.onClick.AddListener(OnNormalButtonClick);
        hardButton.onClick.AddListener(OnHardButtonClick);
        gameStartButton.onClick.AddListener(OnGameStartButtonClick);
    }

    private void OnEasyButtonClick()
    {
        // TODO: 난이도 설정
    }

    private void OnNormalButtonClick()
    {
        // TODO: 난이도 설정
    }

    private void OnHardButtonClick()
    {
        // TODO: 난이도 설정
    }

    private void OnGameStartButtonClick()
    {
        // TODO: 게임 시작 로직
    }

    private void OnBackButtonClick()
    {
        UIManager.Instance.OpenPanel(PanelType.Music);
    }

    //todo: 난입도 버튼을 누른 상태여야 게임 스타트 버튼이 눌릴수있게 구현해야함..
}
