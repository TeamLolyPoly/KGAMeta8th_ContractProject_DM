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

    private bool isSpinning = false;
    private bool currentPanel = true;
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
        float progress;
        float currentInterval;

        SetPanelColor(localPlayerBG, localPanelOriginalColor);
        SetPanelColor(remotePlayerBG, blinkColor);
        currentPanel = true;

        while (elapsedTime < totalDuration)
        {
            progress = elapsedTime / totalDuration;
            currentInterval = Mathf.Lerp(
                initialInterval,
                finalInterval,
                slowdownCurve.Evaluate(progress)
            );

            currentPanel = !currentPanel;

            if (currentPanel)
            {
                SetPanelColor(localPlayerBG, localPanelOriginalColor);
                SetPanelColor(remotePlayerBG, blinkColor);
            }
            else
            {
                SetPanelColor(localPlayerBG, blinkColor);
                SetPanelColor(remotePlayerBG, remotePlayerOriginalColor);
            }

            elapsedTime += currentInterval;
            yield return new WaitForSeconds(currentInterval);
        }

        SetPanelColor(localPlayerBG, selectLocal ? localPanelOriginalColor : blinkColor);
        SetPanelColor(remotePlayerBG, selectLocal ? blinkColor : remotePlayerOriginalColor);

        SpawnSelectionParticle(selectLocal);

        isSpinning = false;
        isFinished = true;
    }

    private void SpawnSelectionParticle(bool selectLocal)
    {
        if (particlePrefab != null)
        {
            RectTransform selectedPanel = selectLocal
                ? localPlayerBG.rectTransform
                : remotePlayerBG.rectTransform;

            Instantiate(
                particlePrefab,
                selectedPanel.position + particleOffset,
                Quaternion.identity,
                transform
            );
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
