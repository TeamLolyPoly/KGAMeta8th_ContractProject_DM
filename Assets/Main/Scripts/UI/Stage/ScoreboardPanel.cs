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
    private float lastRate;

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
            }
        }

        float currentRate = CalculateAccuracy();
        if (lastRate != currentRate)
        {
            lastRate = currentRate;
            if (rateText != null)
            {
                rateText.text = $"{lastRate:F1}%";
            }
        }

        if (lastCombo != scoreSystem.combo)
        {
            lastCombo = scoreSystem.combo;
            if (comboText != null)
            {
                comboText.text = lastCombo.ToString();
            }
        }
    }

    private float CalculateAccuracy()
    {
        if (scoreSystem == null)
            return 0f;

        int totalNotes = scoreSystem.totalNoteCount;
        if (totalNotes == 0)
            return 0f;

        int hitNotes = scoreSystem.noteHitCount;
        return (float)hitNotes / totalNotes * 100f;
    }
}
