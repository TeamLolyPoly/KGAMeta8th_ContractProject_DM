using System.Collections;
using Michsky.UI.Heat;
using ProjectDM.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoadingPanel : Panel
{
    public override PanelType PanelType => PanelType.Loading;

    [Header("참조")]
    [SerializeField]
    private ProgressBar progressBar;

    [SerializeField]
    private TextMeshProUGUI loadingText;

    [SerializeField]
    private CanvasGroup canvasGroup;

    [SerializeField]
    private RectTransform loadingIcon;

    [SerializeField]
    private Image backgroundImage;

    [SerializeField]
    private Sprite[] backgroundImages;

    [SerializeField]
    private bool randomizeBackground = true;

    private bool isFirstText = true;
    private float currentProgress = 0f;
    private float targetProgress = 0f;
    private const float SMOOTH_SPEED = 5f;

    public override void Open()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (progressBar != null)
        {
            progressBar.minValue = 0;
            progressBar.maxValue = 100;
            progressBar.currentValue = 0;
            progressBar.UpdateUI();
        }

        if (backgroundImage != null && backgroundImages != null && backgroundImages.Length > 0)
        {
            if (randomizeBackground)
            {
                backgroundImage.sprite = backgroundImages[Random.Range(0, backgroundImages.Length)];
            }
            else
            {
                backgroundImage.sprite = backgroundImages[0];
            }
        }

        StartCoroutine(CycleLoadingIcon());
    }

    public override void Close(bool objActive = true)
    {
        UIManager.Instance.Panels.Remove(this);
        Destroy(gameObject);
    }

    private void Start()
    {
        if (LoadingManager.Instance != null)
        {
            LoadingManager.Instance.OnProgressUpdated += UpdateProgress;
        }

        StartCoroutine(CycleLoadingIcon());
    }

    private void OnDestroy()
    {
        if (LoadingManager.Instance != null)
        {
            LoadingManager.Instance.OnProgressUpdated -= UpdateProgress;
        }
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
            }
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
