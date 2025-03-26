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

        NoteRatings currentRating = GetCurrentRating();
        if (lastRating != currentRating)
        {
            lastRating = currentRating;
            if (rateText != null)
            {
                rateText.text = lastRating.ToString();
                Debug.Log($"정확도 업데이트: {lastRating}");
            }
        }

        if (lastCombo != scoreSystem.combo)
        {
            lastCombo = scoreSystem.combo;
            if (comboText != null)
            {
                comboText.text = lastCombo.ToString();
                Debug.Log($"콤보 업데이트: {lastCombo}");
            }
        }
    }

    private NoteRatings GetCurrentRating()
    {
        if (scoreSystem == null)
            return NoteRatings.Miss;

        return scoreSystem.LastRating;
    }
}
