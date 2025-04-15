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

    private Color localPanelOriginalColor = Color.white;
    private Color remotePlayerOriginalColor = Color.white;

    public void StartSpinningWithResult(bool selectLocal)
    {
        if (!isSpinning)
        {
            isFinished = false;

            StopAllCoroutines();

            StartCoroutine(SpinEffect(selectLocal));
        }
    }

    private IEnumerator SpinEffect(bool selectLocal)
    {
        isSpinning = true;
        isFinished = false;
        float elapsedTime = 0f;

        Image selectedPanel = selectLocal ? localPlayerBG : remotePlayerBG;
        Image unselectedPanel = selectLocal ? remotePlayerBG : localPlayerBG;

        Color selectedOriginalColor = selectLocal
            ? localPanelOriginalColor
            : remotePlayerOriginalColor;
        Color unselectedOriginalColor = selectLocal
            ? remotePlayerOriginalColor
            : localPanelOriginalColor;

        SetPanelColor(selectedPanel, selectedOriginalColor);
        SetPanelColor(unselectedPanel, unselectedOriginalColor);

        yield return new WaitForSeconds(0.1f);

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
                SetPanelColor(selectedPanel, selectedOriginalColor);
                SetPanelColor(unselectedPanel, blinkColor);
            }
            else
            {
                SetPanelColor(selectedPanel, blinkColor);
                SetPanelColor(unselectedPanel, unselectedOriginalColor);
            }

            elapsedTime += currentInterval;
            yield return new WaitForSeconds(currentInterval);
        }

        SetPanelColor(selectedPanel, selectedOriginalColor);
        SetPanelColor(unselectedPanel, blinkColor);

        SpawnSelectionParticle(selectLocal);

        isSpinning = false;
        isFinished = true;
    }

    private void SpawnSelectionParticle(bool isLocalPanel)
    {
        if (
            particlePrefabs != null
            && particlePositions != null
            && particlePrefabs.Length > 0
            && particlePositions.Length > 0
        )
        {
            RectTransform selectedPanel = isLocalPanel
                ? localPlayerBG.rectTransform
                : remotePlayerBG.rectTransform;

            for (int i = 0; i < particlePositions.Length; i++)
            {
                Vector3 spawnPosition = selectedPanel.position + particlePositions[i];

                GameObject prefabToUse =
                    i < particlePrefabs.Length ? particlePrefabs[i] : particlePrefabs[0];

                GameObject particleObj = Instantiate(
                    prefabToUse,
                    spawnPosition,
                    Quaternion.Euler(0, 0, 0),
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
