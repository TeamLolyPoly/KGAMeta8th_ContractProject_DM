using System.Collections;
using Michsky.UI.Heat;
using ProjectDM.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MultiResultPanel : Panel
{
    public override PanelType PanelType => PanelType.Multi_Result;

    [Header("UI Controls")]
    public ButtonManager backToLobbyButton;

    [Header("Winner")]
    public TextMeshProUGUI winnerNameText;

    public TextMeshProUGUI winnerTotalScoreText;

    public TextMeshProUGUI winnerMostComboText;

    public TextMeshProUGUI winnerNoteHitCountText;

    public TextMeshProUGUI winnerMissCountText;

    public TextMeshProUGUI winnerGoodCountText;

    public TextMeshProUGUI winnerGreatCountText;

    public TextMeshProUGUI winnerPerfectCountText;

    public Image winnerRankImage;

    [Header("Loser")]
    public TextMeshProUGUI loserNameText;

    public TextMeshProUGUI loserTotalScoreText;

    public TextMeshProUGUI loserMostComboText;

    public TextMeshProUGUI loserNoteHitCountText;

    public TextMeshProUGUI loserMissCountText;

    public TextMeshProUGUI loserGoodCountText;

    public TextMeshProUGUI loserGreatCountText;

    public TextMeshProUGUI loserPerfectCountText;
    public Image loserRankImage;

    public override void Open()
    {
        base.Open();

        winnerRankImage.gameObject.SetActive(false);
        loserRankImage.gameObject.SetActive(false);

        if (backToLobbyButton != null)
        {
            backToLobbyButton.onClick.RemoveAllListeners();
            backToLobbyButton.onClick.AddListener(OnBackToLobbyClicked);
        }
    }

    public override void Close(bool objActive = true)
    {
        base.Close(objActive);

        if (backToLobbyButton != null)
        {
            backToLobbyButton.onClick.RemoveAllListeners();
        }
    }

    private void OnBackToRoomClicked()
    {
        if (backToLobbyButton != null)
        {
            backToLobbyButton.onClick.RemoveAllListeners();
            backToLobbyButton.enabled = false;
        }

        GameManager.Instance.Multi_BackToRoom();
    }

    private void OnBackToLobbyClicked()
    {
        if (backToLobbyButton != null)
        {
            backToLobbyButton.onClick.RemoveAllListeners();
            backToLobbyButton.enabled = false;
        }

        GameManager.Instance.Multi_BackToTitle();
    }

    public void Initialize(ScoreData winnerScoreData, ScoreData loserScoreData, bool isWinner)
    {
        if (isWinner)
        {
            winnerNameText.text = "플레이어";
            loserNameText.text = "상대방";
        }
        else
        {
            winnerNameText.text = "상대방";
            loserNameText.text = "플레이어";
        }

        StartCoroutine(AnimateScoreNumbers_Winner(winnerScoreData));
        StartCoroutine(AnimateScoreNumbers_Loser(loserScoreData));
    }

    private IEnumerator AnimateScoreNumbers_Winner(ScoreData scoreData)
    {
        float animationDuration = 5f;
        float elapsedTime = 0f;

        int startScore = 0;
        int startCombo = 0;
        int startNoteHit = 0;
        int startMiss = 0;
        int startGood = 0;
        int startGreat = 0;
        int startPerfect = 0;

        int targetScore = scoreData.Score;
        int targetCombo = scoreData.HighCombo;
        int targetNoteHit = scoreData.NoteHitCount;
        int targetMiss = scoreData.RatingCount[NoteRatings.Miss];
        int targetGood = scoreData.RatingCount[NoteRatings.Good];
        int targetGreat = scoreData.RatingCount[NoteRatings.Great];
        int targetPerfect = scoreData.RatingCount[NoteRatings.Perfect];

        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / animationDuration;

            float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);

            winnerTotalScoreText.text = Mathf
                .RoundToInt(Mathf.Lerp(startScore, targetScore, smoothProgress))
                .ToString();
            winnerMostComboText.text = Mathf
                .RoundToInt(Mathf.Lerp(startCombo, targetCombo, smoothProgress))
                .ToString();
            winnerNoteHitCountText.text = Mathf
                .RoundToInt(Mathf.Lerp(startNoteHit, targetNoteHit, smoothProgress))
                .ToString();
            winnerMissCountText.text = Mathf
                .RoundToInt(Mathf.Lerp(startMiss, targetMiss, smoothProgress))
                .ToString();
            winnerGoodCountText.text = Mathf
                .RoundToInt(Mathf.Lerp(startGood, targetGood, smoothProgress))
                .ToString();
            winnerGreatCountText.text = Mathf
                .RoundToInt(Mathf.Lerp(startGreat, targetGreat, smoothProgress))
                .ToString();
            winnerPerfectCountText.text = Mathf
                .RoundToInt(Mathf.Lerp(startPerfect, targetPerfect, smoothProgress))
                .ToString();

            yield return null;
        }

        winnerTotalScoreText.text = targetScore.ToString();
        winnerMostComboText.text = targetCombo.ToString();
        winnerNoteHitCountText.text = targetNoteHit.ToString();
        winnerMissCountText.text = targetMiss.ToString();
        winnerGoodCountText.text = targetGood.ToString();
        winnerGreatCountText.text = targetGreat.ToString();
        winnerPerfectCountText.text = targetPerfect.ToString();
        winnerRankImage.sprite = StageUIManager.Instance.GetRankImage(
            GameManager.Instance.ScoreSystem.GetGameRank(scoreData)
        );
        winnerRankImage.gameObject.SetActive(true);
    }

    private IEnumerator AnimateScoreNumbers_Loser(ScoreData scoreData)
    {
        float animationDuration = 5f;
        float elapsedTime = 0f;

        int startScore = 0;
        int startCombo = 0;
        int startNoteHit = 0;
        int startMiss = 0;
        int startGood = 0;
        int startGreat = 0;
        int startPerfect = 0;

        int targetScore = scoreData.Score;
        int targetCombo = scoreData.HighCombo;
        int targetNoteHit = scoreData.NoteHitCount;
        int targetMiss = scoreData.RatingCount[NoteRatings.Miss];
        int targetGood = scoreData.RatingCount[NoteRatings.Good];
        int targetGreat = scoreData.RatingCount[NoteRatings.Great];
        int targetPerfect = scoreData.RatingCount[NoteRatings.Perfect];

        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / animationDuration;

            float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);

            loserTotalScoreText.text = Mathf
                .RoundToInt(Mathf.Lerp(startScore, targetScore, smoothProgress))
                .ToString();
            loserMostComboText.text = Mathf
                .RoundToInt(Mathf.Lerp(startCombo, targetCombo, smoothProgress))
                .ToString();
            loserNoteHitCountText.text = Mathf
                .RoundToInt(Mathf.Lerp(startNoteHit, targetNoteHit, smoothProgress))
                .ToString();
            loserMissCountText.text = Mathf
                .RoundToInt(Mathf.Lerp(startMiss, targetMiss, smoothProgress))
                .ToString();
            loserGoodCountText.text = Mathf
                .RoundToInt(Mathf.Lerp(startGood, targetGood, smoothProgress))
                .ToString();
            loserGreatCountText.text = Mathf
                .RoundToInt(Mathf.Lerp(startGreat, targetGreat, smoothProgress))
                .ToString();
            loserPerfectCountText.text = Mathf
                .RoundToInt(Mathf.Lerp(startPerfect, targetPerfect, smoothProgress))
                .ToString();

            yield return null;
        }

        loserTotalScoreText.text = targetScore.ToString();
        loserMostComboText.text = targetCombo.ToString();
        loserNoteHitCountText.text = targetNoteHit.ToString();
        loserMissCountText.text = targetMiss.ToString();
        loserGoodCountText.text = targetGood.ToString();
        loserGreatCountText.text = targetGreat.ToString();
        loserPerfectCountText.text = targetPerfect.ToString();
        loserRankImage.sprite = StageUIManager.Instance.GetRankImage(
            GameManager.Instance.ScoreSystem.GetGameRank(scoreData)
        );
        loserRankImage.gameObject.SetActive(true);
    }
}
