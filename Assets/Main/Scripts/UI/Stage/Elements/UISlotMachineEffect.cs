using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class UISlotMachineEffect : MonoBehaviour
{
    [Header("UI 패널 설정")]
    [SerializeField] private Image panel1Background;
    [SerializeField] private Image panel2Background;

    [Header("색상 설정")]
    [SerializeField] private Color originalColor = Color.white;
    [SerializeField] private Color blinkColor = Color.gray;

    [Header("효과 설정")]
    [SerializeField] private float initialInterval = 0.05f;
    [SerializeField] private float finalInterval = 0.5f;
    [SerializeField] private float totalDuration = 3f;
    [SerializeField] private AnimationCurve slowdownCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("파티클 설정")]
    [SerializeField] private GameObject particlePrefab;    // 파티클 프리팹
    [SerializeField] private Vector3 particleOffset = new Vector3(0, 0, -1f); // 파티클 위치 오프셋

    private bool isSpinning = false;
    private bool currentPanel = true; // true = panel1, false = panel2
    private Color panel1OriginalColor;
    private Color panel2OriginalColor;
    private bool finalSelection;

    // 결과를 받아서 효과를 시작하는 메서드
    public void StartSpinningWithResult(bool selectPanel1)
    {
        if (!isSpinning)
        {
            finalSelection = selectPanel1;
            StartCoroutine(SpinEffect());
        }
    }

    private void Start()
    {
        if (panel1Background != null) panel1OriginalColor = panel1Background.color;
        if (panel2Background != null) panel2OriginalColor = panel2Background.color;
    }

    private IEnumerator SpinEffect()
    {
        isSpinning = true;
        float elapsedTime = 0f;
        float currentInterval = initialInterval;

        // 초기 상태 설정
        SetPanelColor(panel1Background, panel1OriginalColor);
        SetPanelColor(panel2Background, blinkColor);
        currentPanel = true;

        while (elapsedTime < totalDuration)
        {
            float progress = elapsedTime / totalDuration;
            currentInterval = Mathf.Lerp(initialInterval, finalInterval, slowdownCurve.Evaluate(progress));

            // 패널 전환 (한 번에 하나의 패널만 활성화)
            currentPanel = !currentPanel;

            // 현재 활성화된 패널만 원래 색상으로, 나머지는 회색으로
            if (currentPanel)
            {
                SetPanelColor(panel1Background, panel1OriginalColor);
                SetPanelColor(panel2Background, blinkColor);
            }
            else
            {
                SetPanelColor(panel1Background, blinkColor);
                SetPanelColor(panel2Background, panel2OriginalColor);
            }

            elapsedTime += currentInterval;
            yield return new WaitForSeconds(currentInterval);
        }

        // 최종 선택 상태 설정
        SetPanelColor(panel1Background, finalSelection ? panel1OriginalColor : blinkColor);
        SetPanelColor(panel2Background, finalSelection ? blinkColor : panel2OriginalColor);

        // 선택된 패널에 파티클 생성
        SpawnSelectionParticle();

        isSpinning = false;
    }
    private void SpawnSelectionParticle()
    {
        if (particlePrefab != null)
        {
            // 선택된 패널의 위치 가져오기
            RectTransform selectedPanel = finalSelection ?
                panel1Background.rectTransform :
                panel2Background.rectTransform;

            // 파티클 생성 및 위치 설정
            GameObject particleObj = Instantiate(particlePrefab,
                selectedPanel.position + particleOffset,
                Quaternion.identity,
                transform);

        }
    }

    private void SetPanelColor(Image panel, Color targetColor)
    {
        if (panel != null)
        {
            panel.color = targetColor;
        }
    }

    // 현재 상태 확인
    public bool IsSpinning()
    {
        return isSpinning;
    }
}

