using System.Collections;
using System.Collections.Generic;
using ProjectDM.UI;
using TMPro;
using UnityEngine;

public class Scoreboard : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI scoreText;

    [SerializeField]
    private TextMeshProUGUI ratingText;

    [SerializeField]
    private TextMeshProUGUI comboText;

    private ScoreSystem scoreSystem;
    private float lastScore;
    private int lastCombo;
    private NoteRatings lastRating;

    private void Awake()
    {
        StartCoroutine(WaitForGameManager());
    }

    private IEnumerator WaitForGameManager()
    {
        yield return new WaitUntil(
            () => GameManager.Instance != null && GameManager.Instance.ScoreSystem != null
        );

        scoreSystem = GameManager.Instance.ScoreSystem;

        lastScore = 0;
        lastCombo = 0;
        lastRating = scoreSystem.LastRating;

        if (scoreText != null)
        {
            scoreText.text = "0";
        }
        if (comboText != null)
        {
            comboText.text = "0";
        }
        if (ratingText != null)
        {
            ratingText.text = lastRating.ToString();
        }
    }

    private void Update()
    {
        if (scoreSystem == null)
            return;

        if (lastScore != scoreSystem.currentScore)
        {
            lastScore = scoreSystem.currentScore;
            if (scoreText != null)
            {
                scoreText.text = lastScore.ToString("N0");
            }
        }

        if (lastRating != scoreSystem.LastRating)
        {
            lastRating = scoreSystem.LastRating;
            if (ratingText != null)
            {
                if (lastRating == NoteRatings.Success)
                {
                    ratingText.text = "Perfect";
                }
                else
                {
                    ratingText.text = lastRating.ToString();
                }
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
            }
        }
    }
}
