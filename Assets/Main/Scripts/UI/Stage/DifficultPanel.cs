using Michsky.UI.Heat;
using ProjectDM;
using ProjectDM.UI;
using UnityEditor.ShaderGraph.Serialization;
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
    private BoxButtonManager backButton;

    //todo:랭킹 스크롤 작업 해야함

    [SerializeField]
    private ButtonManager easyButton;

    [SerializeField]
    private ButtonManager normalButton;

    [SerializeField]
    private ButtonManager hardButton;

    [SerializeField]
    private ButtonManager gameStartButton;

    public Difficulty userDifficulty = Difficulty.None;

    //프로퍼티 써서 캡슐 ㄱㄱ

    private void Start()
    {
        gameStartButton.isInteractable = false;
        backButton.onClick.AddListener(OnBackButtonClick);
        easyButton.onClick.AddListener(OnEasyButtonClick);
        normalButton.onClick.AddListener(OnNormalButtonClick);
        hardButton.onClick.AddListener(OnHardButtonClick);
        gameStartButton.onClick.AddListener(OnGameStartButtonClick);
    }

    private void OnEasyButtonClick()
    {
        SetDifficulty(Difficulty.Easy);
        // TODO: 난이도 설정
    }

    private void OnNormalButtonClick()
    {
        SetDifficulty(Difficulty.Normal);
        // TODO: 난이도 설정
    }

    private void OnHardButtonClick()
    {
        // TODO: 난이도 설정

        SetDifficulty(Difficulty.Hard);
    }

    private void OnGameStartButtonClick()
    {
        Debug.Log("게임 시작 버튼이 눌렸습니다.");
        // TODO: 게임 시작 로직
    }

    private void OnBackButtonClick()
    {
        UIManager.Instance.OpenPanel(PanelType.Music);
        UIManager.Instance.ClosePanel(PanelType.Difficult);
    }

    //todo: 난입도 버튼을 누른 상태여야 게임 스타트 버튼이 눌릴수있게 구현해야함.

    private void SetDifficulty(Difficulty difficulty)
    {
        gameStartButton.isInteractable = true;
        userDifficulty = difficulty;
    }
}
