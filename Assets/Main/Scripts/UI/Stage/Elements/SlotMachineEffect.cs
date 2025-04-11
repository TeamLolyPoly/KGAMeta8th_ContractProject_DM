using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SlotMachineEffect : MonoBehaviour
{
    [Header("UI 패널 설정")]
    [SerializeField]
    private Image localPlayerBG;

    [SerializeField]
    private Image remotePlayerBG;

    [Header("색상 설정")]
    [SerializeField]
    private Color blinkColor = Color.gray;

    [Header("효과 설정")]
    [SerializeField]
    private float initialInterval = 0.05f;

    [SerializeField]
    private float finalInterval = 0.5f;

    [SerializeField]
    private float totalDuration = 3f;

    [SerializeField]
    private AnimationCurve slowdownCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("파티클 설정")]
    [SerializeField]
    private GameObject particlePrefab;

    [SerializeField]
    private Vector3 particleOffset = new Vector3(0, 0, -1f);

    [Header("화면 구조 설정")]
    [SerializeField]
    private bool isLocalPanelOnLeft = true; // ✅ 왼쪽이 로컬 플레이어인지 설정

    private bool isSpinning = false;
    private bool isFinished = false;
    public bool IsFinished => isFinished;

    private Color localPanelOriginalColor;
    private Color remotePlayerOriginalColor;

    private void Start()
    {
        if (localPlayerBG != null)
            localPanelOriginalColor = localPlayerBG.color;

        if (remotePlayerBG != null)
            remotePlayerOriginalColor = remotePlayerBG.color;
    }

    public void StartSpinningWithResult(bool selectLocal)
    {
        if (!isSpinning)
        {
            StartCoroutine(SpinEffect(selectLocal));
        }
    }

    private IEnumerator SpinEffect(bool selectLocal)
    {
        isSpinning = true;
        isFinished = false;
        float elapsedTime = 0f;

        // 🔁 패널을 위치 기준으로 고정
        Image leftPanel = isLocalPanelOnLeft ? localPlayerBG : remotePlayerBG;
        Image rightPanel = isLocalPanelOnLeft ? remotePlayerBG : localPlayerBG;

        Color leftOriginal = isLocalPanelOnLeft ? localPanelOriginalColor : remotePlayerOriginalColor;
        Color rightOriginal = isLocalPanelOnLeft ? remotePlayerOriginalColor : localPanelOriginalColor;

        // 초기 설정
        SetPanelColor(leftPanel, leftOriginal);
        SetPanelColor(rightPanel, blinkColor);
        bool currentSide = true;

        while (elapsedTime < totalDuration)
        {
            float progress = elapsedTime / totalDuration;
            float currentInterval = Mathf.Lerp(initialInterval, finalInterval, slowdownCurve.Evaluate(progress));
            currentSide = !currentSide;

            if (currentSide)
            {
                SetPanelColor(leftPanel, leftOriginal);
                SetPanelColor(rightPanel, blinkColor);
            }
            else
            {
                SetPanelColor(leftPanel, blinkColor);
                SetPanelColor(rightPanel, rightOriginal);
            }

            elapsedTime += currentInterval;
            yield return new WaitForSeconds(currentInterval);
        }

        // 최종 선택 색상 고정
        bool isLeftSelected = (selectLocal && isLocalPanelOnLeft) || (!selectLocal && !isLocalPanelOnLeft);
        if (isLeftSelected)
        {
            SetPanelColor(leftPanel, leftOriginal);
            SetPanelColor(rightPanel, blinkColor);
        }
        else
        {
            SetPanelColor(leftPanel, blinkColor);
            SetPanelColor(rightPanel, rightOriginal);
        }

        SpawnSelectionParticle(isLeftSelected);

        isSpinning = false;
        isFinished = true;
    }

    private void SpawnSelectionParticle(bool isLeftPanel)
    {
        if (particlePrefab != null)
        {
            RectTransform selectedPanel = isLeftPanel
                ? (isLocalPanelOnLeft ? localPlayerBG.rectTransform : remotePlayerBG.rectTransform)
                : (isLocalPanelOnLeft ? remotePlayerBG.rectTransform : localPlayerBG.rectTransform);

            Instantiate(particlePrefab, selectedPanel.position + particleOffset, Quaternion.identity, transform);
        }
    }

    private void SetPanelColor(Image panel, Color targetColor)
    {
        if (panel != null)
        {
            panel.color = targetColor;
        }
    }
}
