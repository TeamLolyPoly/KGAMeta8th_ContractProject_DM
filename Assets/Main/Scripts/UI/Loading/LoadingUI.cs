using System.Collections;
using Michsky.UI.Heat;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoadingPanel : MonoBehaviour
{
    [Header("참조")]
    [SerializeField]
    private ProgressBar progressBar;

    [SerializeField]
    private TextMeshProUGUI loadingText;

    [SerializeField]
    private TextMeshProUGUI tipText;

    [SerializeField]
    private CanvasGroup canvasGroup;

    [SerializeField]
    private RectTransform loadingIcon;

    [SerializeField]
    private Image backgroundImage;

    [Header("설정")]
    [SerializeField]
    private string[] loadingTips = new string[]
    {
        "오디오 파일을 로드하는 동안 잠시만 기다려주세요.",
        "대용량 오디오 파일은 로드 시간이 더 오래 걸릴 수 있습니다.",
        "웨이브폼 생성은 오디오 파일의 길이에 따라 시간이 달라집니다.",
        "고품질 오디오 파일을 사용하면 더 정확한 웨이브폼을 볼 수 있습니다.",
        "앨범 아트는 트랙 정보와 함께 저장됩니다.",
        "트랙 정보는 자동으로 저장되므로 다음에도 사용할 수 있습니다.",
        "웨이브폼을 통해 오디오의 진폭을 시각적으로 확인할 수 있습니다.",
        "MP3, WAV, OGG 형식의 오디오 파일을 지원합니다.",
        "앨범 아트는 PNG, JPG, JPEG 형식을 지원합니다.",
        "트랙을 선택하면 웨이브폼이 자동으로 업데이트됩니다.",
    };

    [SerializeField]
    private Sprite[] backgroundImages;

    [SerializeField]
    private float tipChangeInterval = 5f;

    [SerializeField]
    private bool randomizeTips = true;

    [SerializeField]
    private bool randomizeBackground = true;

    [SerializeField]
    private bool isSceneMode = false;

    private Coroutine tipCoroutine;

    private void Awake()
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

        if (
            isSceneMode
            && backgroundImage != null
            && backgroundImages != null
            && backgroundImages.Length > 0
        )
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
    }

    private void Start()
    {
        if (isSceneMode && LoadingManager.Instance != null)
        {
            LoadingManager.Instance.OnProgressUpdated += UpdateProgress;
        }

        StartLoadingTips();
        StartCoroutine(CycleLoadingIcon());
    }

    private void OnEnable()
    {
        if (!isSceneMode)
        {
            StartLoadingTips();
        }
    }

    private void OnDisable()
    {
        StopLoadingTips();
    }

    private void OnDestroy()
    {
        if (isSceneMode && LoadingManager.Instance != null)
        {
            LoadingManager.Instance.OnProgressUpdated -= UpdateProgress;
        }

        StopLoadingTips();
    }

    private void StartLoadingTips()
    {
        if (
            tipText != null
            && loadingTips != null
            && loadingTips.Length > 0
            && tipCoroutine == null
        )
        {
            tipCoroutine = StartCoroutine(CycleTips());
        }
    }

    private void StopLoadingTips()
    {
        if (tipCoroutine != null)
        {
            StopCoroutine(tipCoroutine);
            tipCoroutine = null;
        }
    }

    public void UpdateProgress(float progress)
    {
        if (progressBar != null)
        {
            progressBar.currentValue = progress * 100f;
            progressBar.UpdateUI();
        }
    }

    public void SetLoadingText(string text)
    {
        if (loadingText != null)
        {
            loadingText.text = text;
        }
    }

    private IEnumerator CycleTips()
    {
        int currentTipIndex = 0;

        while (true)
        {
            if (randomizeTips)
            {
                currentTipIndex = Random.Range(0, loadingTips.Length);
            }
            else
            {
                currentTipIndex = (currentTipIndex + 1) % loadingTips.Length;
            }

            if (tipText != null)
            {
                tipText.text = loadingTips[currentTipIndex];
            }

            yield return new WaitForSeconds(tipChangeInterval);
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

    public IEnumerator FadeIn(float duration)
    {
        return FadeCanvasGroup(0, 1, duration);
    }

    public IEnumerator FadeOut(float duration)
    {
        return FadeCanvasGroup(1, 0, duration);
    }

    private IEnumerator FadeCanvasGroup(float startAlpha, float endAlpha, float duration)
    {
        if (canvasGroup == null)
            yield break;

        float startTime = Time.time;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime = Time.time - startTime;
            float normalizedTime = elapsedTime / duration;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, normalizedTime);
            yield return null;
        }

        canvasGroup.alpha = endAlpha;
    }
}
