using System.Collections;
using Michsky.UI.Heat;
using ProjectDM.UI;
using TMPro;
using UnityEngine;

public class StageLoadingPanel : Panel
{
    public override PanelType PanelType => PanelType.Loading;

    [SerializeField]
    private ProgressBar progressBar;

    [SerializeField]
    private RectTransform barRect;

    [SerializeField]
    private TextMeshProUGUI loadingText;

    [SerializeField]
    private RectTransform loadingIcon;

    [SerializeField]
    private RectTransform handleParticle;

    [SerializeField]
    private float particleOffset = 20f;

    private bool isFirstText = true;
    private float currentProgress = 0f;
    private float targetProgress = 0f;
    private const float SMOOTH_SPEED = 5f;
    private Vector2 particleStartPos;
    private float progressBarWidth;

    public override void Open()
    {
        animator.SetBool("isOpen", false);

        base.Open();

        currentProgress = 0f;
        targetProgress = 0f;
        progressBar.currentValue = currentProgress;
        progressBar.UpdateUI();

        if (StageLoadingManager.Instance != null)
        {
            StageLoadingManager.Instance.OnProgressUpdated += UpdateProgress;
        }

        InitializeParticlePosition();
        StartCoroutine(CycleLoadingIcon());
    }

    private void InitializeParticlePosition()
    {
        if (handleParticle != null && progressBar != null && progressBar.barImage != null)
        {
            RectTransform barRect = progressBar.barImage.rectTransform;
            progressBarWidth = barRect.rect.width;
            particleStartPos = handleParticle.anchoredPosition;
        }
    }

    public override void Close(bool objActive = true)
    {
        if (StageLoadingManager.Instance != null)
        {
            StageLoadingManager.Instance.OnProgressUpdated -= UpdateProgress;
        }

        StopCoroutine(CycleLoadingIcon());

        base.Close(objActive);
    }

    private void Update()
    {
        if (currentProgress != targetProgress)
        {
            currentProgress = Mathf.Lerp(
                currentProgress,
                targetProgress,
                Time.deltaTime * SMOOTH_SPEED
            );

            if (Mathf.Abs(currentProgress - targetProgress) < 0.01f)
            {
                currentProgress = targetProgress;
            }

            if (progressBar != null)
            {
                progressBar.currentValue = currentProgress * 100f;
                progressBar.UpdateUI();
                UpdateParticlePosition();
            }
        }
    }

    private void UpdateParticlePosition()
    {
        if (handleParticle != null && progressBar != null)
        {
            float progress = currentProgress;

            float xPos = particleStartPos.x + (progressBarWidth * progress) + particleOffset;
            handleParticle.anchoredPosition = new Vector2(xPos, particleStartPos.y);
        }
    }

    public void UpdateProgress(float progress)
    {
        targetProgress = progress;
    }

    public void SetLoadingText(string text)
    {
        if (loadingText != null)
        {
            loadingText.text = text;
        }
        if (isFirstText)
        {
            animator.SetBool("isOpen", true);
            isFirstText = false;
        }
    }

    private IEnumerator CycleLoadingIcon()
    {
        while (true)
        {
            if (loadingIcon != null)
            {
                loadingIcon.Rotate(Vector3.forward, 360 * Time.deltaTime);
            }

            yield return null;
        }
    }
}
