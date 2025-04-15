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
            // 이전 상태 초기화
            isFinished = false;
            StopAllCoroutines(); // 실행 중인 모든 코루틴 중지

            StartCoroutine(SpinEffect(selectLocal));
        }
    }

    private IEnumerator SpinEffect(bool selectLocal)
    {
        isSpinning = true;
        isFinished = false;
        float elapsedTime = 0f;

        // 패널 위치 설정
        Image selectedPanel = selectLocal ? localPlayerBG : remotePlayerBG;
        Image unselectedPanel = selectLocal ? remotePlayerBG : localPlayerBG;

        Color selectedOriginalColor = selectLocal ? localPanelOriginalColor : remotePlayerOriginalColor;
        Color unselectedOriginalColor = selectLocal ? remotePlayerOriginalColor : localPanelOriginalColor;

        // 초기 설정 - 두 패널 모두 원래 색상으로 설정
        SetPanelColor(selectedPanel, selectedOriginalColor);
        SetPanelColor(unselectedPanel, unselectedOriginalColor);

        // 잠시 대기 후 효과 시작
        yield return new WaitForSeconds(0.1f);

        bool currentSide = true;
        while (elapsedTime < totalDuration)
        {
            float progress = elapsedTime / totalDuration;
            float currentInterval = Mathf.Lerp(initialInterval, finalInterval, slowdownCurve.Evaluate(progress));
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

        // 최종 선택 색상 고정
        SetPanelColor(selectedPanel, selectedOriginalColor);
        SetPanelColor(unselectedPanel, blinkColor);

        // 선택된 패널에 파티클 생성
        SpawnSelectionParticle(selectLocal);

        isSpinning = false;
        isFinished = true;
    }

    private void SpawnSelectionParticle(bool isLocalPanel)
    {
        if (particlePrefabs != null && particlePositions != null &&
            particlePrefabs.Length > 0 && particlePositions.Length > 0)
        {
            // 선택된 패널의 위치 가져오기
            RectTransform selectedPanel = isLocalPanel ? localPlayerBG.rectTransform : remotePlayerBG.rectTransform;

            // 각 위치에 해당하는 파티클 생성
            for (int i = 0; i < particlePositions.Length; i++)
            {
                // 파티클 생성 위치 계산
                Vector3 spawnPosition = selectedPanel.position + particlePositions[i];

                // 해당 위치에 맞는 파티클 프리팹 선택 (배열 범위 체크)
                GameObject prefabToUse = i < particlePrefabs.Length ? particlePrefabs[i] : particlePrefabs[0];

                // 파티클 생성
                GameObject particleObj = Instantiate(prefabToUse, spawnPosition, Quaternion.Euler(0, 0, 0), transform);

                // 파티클 재생이 끝나면 자동으로 제거
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
