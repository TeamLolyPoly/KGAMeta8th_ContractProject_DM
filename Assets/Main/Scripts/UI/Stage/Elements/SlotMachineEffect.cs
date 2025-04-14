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
    private Vector3[] particlePositions; // 여러 위치에 대한 오프셋 배열

    [SerializeField]
    private Vector3 particleRotation = new Vector3(0, 0, 0); // 회전값 설정

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
        if (particlePrefab != null && particlePositions != null && particlePositions.Length > 0)
        {
            // 선택된 패널의 위치 가져오기
            RectTransform selectedPanel = isLeftPanel
                ? (isLocalPanelOnLeft ? localPlayerBG.rectTransform : remotePlayerBG.rectTransform)
                : (isLocalPanelOnLeft ? remotePlayerBG.rectTransform : localPlayerBG.rectTransform);

            // 각 위치에 파티클 생성
            foreach (Vector3 position in particlePositions)
            {
                // 파티클 생성 위치 계산
                Vector3 spawnPosition = selectedPanel.position + position;

                // Inspector에서 설정한 회전값 사용
                Quaternion specificRotation = Quaternion.Euler(particleRotation);
                GameObject particleObj = Instantiate(particlePrefab, spawnPosition, specificRotation, transform);

                // 파티클 시스템 설정 수정
                var particleSystem = particleObj.GetComponent<ParticleSystem>();
                if (particleSystem != null)
                {
                    // 파티클 시스템의 회전 설정
                    var main = particleSystem.main;
                    main.startRotation3D = true;
                    main.startRotationX = 0;
                    main.startRotationY = 0;
                    main.startRotationZ = 90; // Z축 회전 90도

                    // 중력 설정 (위로 올라가는 효과를 위해)
                    main.gravityModifier = -1f; // 중력을 위쪽으로 설정

                    // 파티클 방향 설정
                    var emission = particleSystem.emission;
                    emission.rateOverTime = 0; // 연속 방출 비활성화

                    // 파티클 모양 설정
                    var shape = particleSystem.shape;
                    shape.shapeType = ParticleSystemShapeType.Cone;
                    shape.angle = 15f; // 방출 각도 설정
                    shape.rotation = new Vector3(0, 0, 90); // 모양의 회전 설정

                    // 파티클 재생이 끝나면 자동으로 제거
                    Destroy(particleObj, particleSystem.main.duration);
                }
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
}
