using Michsky.UI.Heat;
using ProjectDM.UI;
using UnityEngine;
using UnityEngine.UI;

public class DifficultPanel : Panel
{
    public override PanelType PanelType => PanelType.Difficult;

    public override void Open()
    {
        base.Open();
    }

    public override void Close(bool objActive = false)
    {
        backButton.onClick.RemoveListener(OnBackButtonClick);
        easyButton.onClick.RemoveListener(OnEasyButtonClick);
        normalButton.onClick.RemoveListener(OnNormalButtonClick);
        hardButton.onClick.RemoveListener(OnHardButtonClick);
        gameStartButton.onClick.RemoveListener(OnGameStartButtonClick);
        base.Close(objActive);
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
    private Button gameStartButton;

    public Difficulty userDifficulty = Difficulty.None;

    //프로퍼티 써서 캡슐 ㄱㄱ

    private void Start()
    {
        gameStartButton.interactable = false;
        backButton.onClick.AddListener(OnBackButtonClick);
        easyButton.onClick.AddListener(OnEasyButtonClick);
        normalButton.onClick.AddListener(OnNormalButtonClick);
        hardButton.onClick.AddListener(OnHardButtonClick);
        gameStartButton.onClick.AddListener(OnGameStartButtonClick);
    }

    private void OnEasyButtonClick()
    {
        SetDifficulty(Difficulty.Easy);
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

    private void OnBackButtonClick() { }

    private void SetDifficulty(Difficulty difficulty)
    {
        gameStartButton.interactable = true;
        userDifficulty = difficulty;
    }
}
