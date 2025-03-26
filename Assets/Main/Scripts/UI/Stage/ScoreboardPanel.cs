using System.Collections;
using System.Collections.Generic;
using ProjectDM.UI;
using TMPro;
using UnityEngine;

public class ScoreboardPanel : Panel
{
    public override PanelType PanelType => PanelType.Scoreboard;

    [SerializeField]
    private TextMeshProUGUI scoreText;

    [SerializeField]
    private TextMeshProUGUI rateText;

    [SerializeField]
    private TextMeshProUGUI comboText;

    private ScoreSystem scoreSystem;
    private float lastScore;
    private int lastCombo;
    private NoteRatings lastRating;

    private void Start()
    {
        scoreSystem = GameManager.Instance.ScoreSystem;

        // 초기값 설정
        lastScore = 0;
        lastCombo = 0;
        lastRating = scoreSystem.LastRating;

        // UI 업데이트
        if (scoreText != null)
        {
            scoreText.text = "0";
        }
        if (comboText != null)
        {
            comboText.text = "0";
        }
        if (rateText != null)
        {
            rateText.text = lastRating.ToString();
        }
    }

    private void Update()
    {
        if (scoreSystem == null)
            return;

        // 값이 변경된 경우에만 업데이트
        if (lastScore != scoreSystem.currentScore)
        {
            lastScore = scoreSystem.currentScore;
            if (scoreText != null)
            {
                scoreText.text = lastScore.ToString("N0");
                Debug.Log($"점수 업데이트: {lastScore:N0}");
            }
        }

        if (lastRating != scoreSystem.LastRating)
        {
            lastRating = scoreSystem.LastRating;
            if (rateText != null)
            {
                if (lastRating == NoteRatings.Success)
                {
                    rateText.text = "Perfect";
                }
                else
                {
                    rateText.text = lastRating.ToString();
                }
                Debug.Log($"정확도 업데이트: {lastRating}");
            }
        }

        if (lastCombo != scoreSystem.combo)
        {
            lastCombo = scoreSystem.combo;
            if (comboText != null)
            {
                if (lastCombo > 0)
                {
                    comboText.text = lastCombo.ToString();
                }
                else
                {
                    comboText.text = "0";
                }
                Debug.Log($"콤보 업데이트: {lastCombo}");
            }
        }
    }
}
