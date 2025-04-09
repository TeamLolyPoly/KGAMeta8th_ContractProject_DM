using System;
using System.Collections;
using System.Collections.Generic;
using Michsky.UI.Heat;
using ProjectDM.UI;
using TMPro;
using UnityEngine;

public class ResultDetailPanel : Panel
{
    public override PanelType PanelType => PanelType.ResultDetail;

    public TextMeshProUGUI totalScoreText;

    public TextMeshProUGUI comboText;

    public TextMeshProUGUI noteHitCountText;

    public TextMeshProUGUI missCountText;

    public TextMeshProUGUI goodCountText;

    public TextMeshProUGUI greatCountText;

    public TextMeshProUGUI perfectCountText;

    public override void Open()
    {
        base.Open();
    }

    public void Initialize(ScoreData scoreData)
    {
        StartCoroutine(AnimateScoreNumbers(scoreData));
    }

    private IEnumerator AnimateScoreNumbers(ScoreData scoreData)
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

            totalScoreText.text = Mathf
                .RoundToInt(Mathf.Lerp(startScore, targetScore, smoothProgress))
                .ToString();
            comboText.text = Mathf
                .RoundToInt(Mathf.Lerp(startCombo, targetCombo, smoothProgress))
                .ToString();
            noteHitCountText.text = Mathf
                .RoundToInt(Mathf.Lerp(startNoteHit, targetNoteHit, smoothProgress))
                .ToString();
            missCountText.text = Mathf
                .RoundToInt(Mathf.Lerp(startMiss, targetMiss, smoothProgress))
                .ToString();
            goodCountText.text = Mathf
                .RoundToInt(Mathf.Lerp(startGood, targetGood, smoothProgress))
                .ToString();
            greatCountText.text = Mathf
                .RoundToInt(Mathf.Lerp(startGreat, targetGreat, smoothProgress))
                .ToString();
            perfectCountText.text = Mathf
                .RoundToInt(Mathf.Lerp(startPerfect, targetPerfect, smoothProgress))
                .ToString();

            yield return null;
        }

        totalScoreText.text = targetScore.ToString();
        comboText.text = targetCombo.ToString();
        noteHitCountText.text = targetNoteHit.ToString();
        missCountText.text = targetMiss.ToString();
        goodCountText.text = targetGood.ToString();
        greatCountText.text = targetGreat.ToString();
        perfectCountText.text = targetPerfect.ToString();

        Instantiate(GameManager.Instance.proceedDrum);
    }
}
