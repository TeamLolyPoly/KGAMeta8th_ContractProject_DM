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
    private GameObject[] particlePrefabs;

    [SerializeField]
    private Vector3[] particlePositions;

    [Header("화면 구조 설정")]
    [SerializeField]
    private bool isLocalPanelOnLeft = true;

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

        Image leftPanel = isLocalPanelOnLeft ? localPlayerBG : remotePlayerBG;
        Image rightPanel = isLocalPanelOnLeft ? remotePlayerBG : localPlayerBG;

        Color leftOriginal = isLocalPanelOnLeft
            ? localPanelOriginalColor
            : remotePlayerOriginalColor;
        Color rightOriginal = isLocalPanelOnLeft
            ? remotePlayerOriginalColor
            : localPanelOriginalColor;

        SetPanelColor(leftPanel, leftOriginal);
        SetPanelColor(rightPanel, blinkColor);
        bool currentSide = true;

        while (elapsedTime < totalDuration)
        {
            float progress = elapsedTime / totalDuration;
            float currentInterval = Mathf.Lerp(
                initialInterval,
                finalInterval,
                slowdownCurve.Evaluate(progress)
            );
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

        bool isLeftSelected =
            (selectLocal && isLocalPanelOnLeft) || (!selectLocal && !isLocalPanelOnLeft);
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

        yield return new WaitForSeconds(2f);

        isSpinning = false;
        isFinished = true;
    }

    private void SpawnSelectionParticle(bool isLeftPanel)
    {
        if (
            particlePrefabs != null
            && particlePositions != null
            && particlePrefabs.Length > 0
            && particlePositions.Length > 0
        )
        {
            RectTransform selectedPanel = isLeftPanel
                ? (isLocalPanelOnLeft ? localPlayerBG.rectTransform : remotePlayerBG.rectTransform)
                : (isLocalPanelOnLeft ? remotePlayerBG.rectTransform : localPlayerBG.rectTransform);

            for (int i = 0; i < particlePositions.Length; i++)
            {
                Vector3 spawnPosition = selectedPanel.position + particlePositions[i];

                GameObject prefabToUse =
                    i < particlePrefabs.Length ? particlePrefabs[i] : particlePrefabs[0];

                GameObject particleObj = Instantiate(
                    prefabToUse,
                    spawnPosition,
                    Quaternion.Euler(90, 0, 0),
                    transform
                );

                Destroy(particleObj, 2f);
            }
        }
    }

    private void SetPanelColor(Image panel, Color targetColor)
    {
        if (panel != null)
        {
            panel.color = targetColor;
        }
    }

    public void CleanUp()
    {
        StopAllCoroutines();
        isSpinning = false;
        isFinished = false;
        localPlayerBG.color = localPanelOriginalColor;
        remotePlayerBG.color = remotePlayerOriginalColor;
    }
}
