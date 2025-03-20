using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CustomGeometry
{
    public GameObject seat;
    public List<GameObject> spectator = new List<GameObject>();
}

[System.Serializable]
public class CustomGrandstandCreator : MonoBehaviour
{
    public CustomGeometry geometry;

    [Header("그리드 설정")]
    [Range(0, 32)]
    public int rows = 10;

    [Range(0, 50)]
    public int columns = 10;

    [Range(0.5f, 2.0f)]
    public float horizontalSpacing = 1.0f;

    [Range(0.5f, 2.0f)]
    public float verticalSpacing = 1.0f;

    [Range(0.1f, 2.0f)]
    public float heightOffset = 1.0f;

    [Header("관중 위치 설정")]
    [Range(-1f, 1f)]
    public float spectatorHeight = -0.2f;

    [Range(-1f, 1f)]
    public float spectatorForward = 0.3f;

    [Header("에디터 설정")]
    public bool autoGenerateInEditor = false; // 에디터에서 자동 생성 여부

    [Header("LOD 설정")]
    public float[] lodDistances = new float[] { 15f, 30f, 45f }; // 3단계 LOD
    public GameObject[] lodPrefabs; // 각 LOD 레벨별 메시

    [Header("최적화 설정")]
    public bool enableLOD = true; // LOD 시스템 사용
    public bool enableInstancing = true; // GPU 인스턴싱 사용
    public bool disableShadowsForDistant = true; // 원거리 그림자 비활성화
    public int cullingDistance = 60; // 컬링 거리

    [Header("개발 설정")]
    [Tooltip("이 값이 true이면 에디터에서 생성된 관중만 사용하고 인스턴싱을 사용하지 않습니다.")]
    public bool useOnlyEditorSpectators = true; // 기본값은 true로 설정

    // 내부 변수
    private Camera mainCamera;
    private float distanceToCamera;
    private Matrix4x4[] matrices;
    private MaterialPropertyBlock propertyBlock;
    private Material[] instancedMaterials;
    private Mesh[] meshes;
    private int currentLODLevel = 0;
    private int totalInstances;

    // 초기 검사를 위한 Awake 함수 추가
    void Awake()
    {
        // 에디터에서 생성된 관중만 사용하도록 강제 설정된 경우
        if (useOnlyEditorSpectators)
        {
            Debug.Log(
                "[관중시스템] 에디터에서 생성된 관중만 사용하도록 설정되었습니다. 인스턴싱을 비활성화합니다."
            );
            enableLOD = false;
            enabled = false;
            return;
        }

        // 그 외에는 기존 관중 검사 수행
        CheckForExistingSpectators();
    }

    // 기존 관중 검사 함수
    private bool CheckForExistingSpectators()
    {
        // 에디터에서 생성된 관중이 있는지 검사
        Transform spectatorsContainer = transform.Find("Spectators");

        // 컨테이너가 있고 그 안에 관중이 있는 경우
        if (spectatorsContainer != null && spectatorsContainer.childCount > 0)
        {
            Debug.Log(
                $"[관중시스템] 기존 관중 {spectatorsContainer.childCount}명이 발견되었습니다. 인스턴싱을 비활성화합니다."
            );
            enableLOD = false;
            enabled = false; // 스크립트 자체를 비활성화
            return true;
        }

        // 이전 버전 호환성을 위해 직접 자식들을 검사
        int spectatorCount = 0;
        foreach (Transform child in transform)
        {
            if (child.name.StartsWith("Spectator_"))
            {
                spectatorCount++;
            }
        }

        if (spectatorCount > 0)
        {
            Debug.Log(
                $"[관중시스템] 이전 버전으로 생성된 관중 {spectatorCount}명이 발견되었습니다. 인스턴싱을 비활성화합니다."
            );
            enableLOD = false;
            enabled = false; // 스크립트 자체를 비활성화
            return true;
        }

        return false;
    }

    void Start()
    {
        // 이미 Awake에서 기존 관중이 발견되었다면 중단
        if (!enabled || !enableLOD)
        {
            return;
        }

        // 다시 한번 검사 (안전 장치)
        if (CheckForExistingSpectators())
        {
            return;
        }

        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogWarning("메인 카메라를 찾을 수 없습니다.");
            return;
        }

        // 관중 프리팹 설정 확인
        if (geometry.spectator.Count == 0)
        {
            Debug.LogWarning(
                "관중 프리팹이 설정되지 않았습니다. 관중이 표시되지 않을 수 있습니다."
            );
            return;
        }

        // LOD 프리팹 설정
        if (lodPrefabs == null || lodPrefabs.Length == 0)
        {
            lodPrefabs = new GameObject[1];
            lodPrefabs[0] = geometry.spectator[0];
            Debug.Log("LOD 프리팹이 설정되지 않아 관중 프리팹을 사용합니다.");
        }

        // 행렬 및 리소스 초기화
        InitializeResources();
        Debug.Log("[관중시스템] 인스턴싱 방식으로 관중을 렌더링합니다.");
    }

    void Update()
    {
        // 이미 생성된 관중 오브젝트가 있거나 LOD가 비활성화되었거나 카메라가 없는 경우 건너뜀
        if (!enableLOD || mainCamera == null)
            return;

        // 카메라와 거리 계산
        CalculateDistanceToCamera();

        // LOD 레벨 업데이트 및 렌더링
        UpdateLODLevel();
    }

    // 리소스 초기화 및 준비
    private void InitializeResources()
    {
        totalInstances = rows * columns;

        // 행렬 계산
        CalculateMatrices();

        // 메시와 머티리얼 준비
        PrepareResources();

        // 프로퍼티 블록 초기화
        propertyBlock = new MaterialPropertyBlock();
    }

    // 행렬 계산
    private void CalculateMatrices()
    {
        matrices = new Matrix4x4[totalInstances];
        int index = 0;

        // 부모 오브젝트의 위치와 회전을 고려
        Vector3 parentPosition = transform.position;
        Quaternion parentRotation = transform.rotation;

        // 디버그 정보
        if (Debug.isDebugBuild)
        {
            Debug.Log(
                $"인스턴싱 행렬 계산 - 부모 위치: {parentPosition}, 총 관중 수: {totalInstances}"
            );
        }

        for (int i = 0; i < columns; i++)
        {
            for (int j = 0; j < rows; j++)
            {
                // 위치 계산
                float xPos = i * horizontalSpacing;
                float yPos = j * heightOffset;
                float zPos = j * verticalSpacing;

                // 로컬 좌표 계산
                Vector3 localPosition = new Vector3(
                    xPos,
                    yPos + spectatorHeight,
                    zPos + spectatorForward
                );

                // 월드 좌표로 변환 (부모 위치와 회전 적용)
                Vector3 worldPosition = parentPosition + parentRotation * localPosition;

                // 행렬 생성 (월드 좌표 기준)
                matrices[index++] = Matrix4x4.TRS(
                    worldPosition,
                    parentRotation * Quaternion.Euler(0, 180, 0),
                    Vector3.one
                );

                // 10행 10열마다 위치 정보 기록 (디버그용)
                if (Debug.isDebugBuild && i % 10 == 0 && j % 10 == 0)
                {
                    Debug.Log(
                        $"관중 위치 계산 [{i},{j}] - 로컬: {localPosition}, 월드: {worldPosition}"
                    );
                }
            }
        }
    }

    // 리소스 준비
    private void PrepareResources()
    {
        // 배열 초기화
        meshes = new Mesh[lodPrefabs.Length];
        instancedMaterials = new Material[lodPrefabs.Length];

        for (int i = 0; i < lodPrefabs.Length; i++)
        {
            if (lodPrefabs[i] == null)
                continue;

            // 메시 가져오기
            MeshFilter meshFilter = lodPrefabs[i].GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                meshes[i] = meshFilter.sharedMesh;
            }
            else
            {
                // 스킨드 메시 렌더러 체크
                SkinnedMeshRenderer skinnedMeshRenderer = lodPrefabs[i]
                    .GetComponentInChildren<SkinnedMeshRenderer>();
                if (skinnedMeshRenderer != null && skinnedMeshRenderer.sharedMesh != null)
                {
                    meshes[i] = skinnedMeshRenderer.sharedMesh;
                }
            }

            // 머티리얼 가져오기
            Material sourceMaterial = null;

            // MeshRenderer 체크
            MeshRenderer renderer = lodPrefabs[i].GetComponent<MeshRenderer>();
            if (renderer != null && renderer.sharedMaterial != null)
            {
                sourceMaterial = renderer.sharedMaterial;
            }
            else
            {
                // 스킨드 메시 렌더러에서 머티리얼 찾기
                SkinnedMeshRenderer skinnedRenderer = lodPrefabs[i]
                    .GetComponentInChildren<SkinnedMeshRenderer>();
                if (skinnedRenderer != null && skinnedRenderer.sharedMaterial != null)
                {
                    sourceMaterial = skinnedRenderer.sharedMaterial;
                }
            }

            if (sourceMaterial != null)
            {
                // 인스턴싱 가능한 머티리얼 생성
                instancedMaterials[i] = new Material(sourceMaterial);
                instancedMaterials[i].enableInstancing = enableInstancing;
            }
            else
            {
                Debug.LogWarning($"LOD{i}: 머티리얼을 찾을 수 없습니다 - {lodPrefabs[i].name}");
            }
        }
    }

    // 카메라와 거리 계산
    private void CalculateDistanceToCamera()
    {
        // 관중석의 중심점 계산 (월드 좌표)
        Vector3 localCenterPoint = new Vector3(
            columns * horizontalSpacing * 0.5f,
            rows * heightOffset * 0.5f,
            rows * verticalSpacing * 0.5f
        );

        // 로컬에서 월드 좌표로 변환
        Vector3 worldCenterPoint = transform.TransformPoint(localCenterPoint);

        // 카메라와의 거리 계산
        distanceToCamera = Vector3.Distance(mainCamera.transform.position, worldCenterPoint);
    }

    // LOD 레벨 업데이트 및 렌더링
    private void UpdateLODLevel()
    {
        // 컬링 거리를 벗어난 경우
        if (distanceToCamera > cullingDistance)
            return;

        // LOD 레벨 결정
        for (int i = 0; i < lodDistances.Length; i++)
        {
            if (distanceToCamera <= lodDistances[i])
            {
                // 현재 LOD 레벨이 바뀌는 경우만 새로 렌더링
                if (currentLODLevel != i)
                {
                    currentLODLevel = i;
                }
                break;
            }
        }

        // 현재 LOD 레벨로 렌더링
        RenderCurrentLOD();
    }

    // 관중 렌더링
    private void RenderCurrentLOD()
    {
        int lodLevel = currentLODLevel;

        // 디버그: 인스턴싱 정보 확인
        if (Debug.isDebugBuild && Time.frameCount % 300 == 0)
        {
            Debug.Log(
                $"관중 인스턴싱 렌더링 - LOD 레벨: {lodLevel}, 총 인스턴스: {matrices.Length}"
            );
        }

        // 메시나 머티리얼이 없으면 렌더링 불가
        if (
            meshes == null
            || meshes.Length <= lodLevel
            || meshes[lodLevel] == null
            || instancedMaterials == null
            || instancedMaterials.Length <= lodLevel
            || instancedMaterials[lodLevel] == null
        )
        {
            Debug.LogWarning($"LOD{lodLevel}에 필요한 메시 또는 머티리얼이 없습니다.");
            return;
        }

        // 프로퍼티 블록 초기화
        propertyBlock.Clear();

        // 그림자 설정
        bool receiveShadows = !(disableShadowsForDistant && lodLevel > 0);
        UnityEngine.Rendering.ShadowCastingMode shadowMode =
            (disableShadowsForDistant && lodLevel > 0)
                ? UnityEngine.Rendering.ShadowCastingMode.Off
                : UnityEngine.Rendering.ShadowCastingMode.On;

        // 메시 인스턴싱 성능 최적화 - 배치 크기 조정
        int batchSize = 1023; // 최대 배치 크기 (Unity에서 권장하는 최대값)

        try
        {
            for (int i = 0; i < matrices.Length; i += batchSize)
            {
                int count = Mathf.Min(batchSize, matrices.Length - i);
                Matrix4x4[] batchMatrices = new Matrix4x4[count];
                System.Array.Copy(matrices, i, batchMatrices, 0, count);

                // 관중 그리기 (배치 단위로)
                Graphics.DrawMeshInstanced(
                    meshes[lodLevel],
                    0,
                    instancedMaterials[lodLevel],
                    batchMatrices,
                    count,
                    propertyBlock,
                    shadowMode,
                    receiveShadows,
                    gameObject.layer
                );
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"관중 인스턴싱 렌더링 중 오류 발생: {e.Message}");
        }
    }

    // 디버그용 기즈모
    private void OnDrawGizmosSelected()
    {
        if (!enableLOD || lodDistances == null || lodDistances.Length == 0)
            return;

        // LOD 거리 표시
        Color[] colors = new Color[]
        {
            Color.green,
            Color.yellow,
            Color.red,
            Color.magenta,
            Color.cyan,
        };

        for (int i = 0; i < lodDistances.Length; i++)
        {
            Gizmos.color = colors[i % colors.Length];
            Gizmos.DrawWireSphere(transform.position, lodDistances[i]);
        }

        // 컬링 거리 표시
        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(transform.position, cullingDistance);
    }

#if UNITY_EDITOR
    // 에디터에서 인스펙터 값이 변경될 때 호출됨
    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            // 자동 생성 옵션이 활성화된 경우에만 실행
            if (autoGenerateInEditor)
            {
                // DelayCall을 사용하여 실행 큐에 작업 추가
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    if (this == null)
                        return; // 객체가 파괴된 경우 처리

                    // 관중이 있는지 확인하고 자동 업데이트
                    if (geometry.spectator.Count > 0)
                    {
                        // 자식 오브젝트를 모두 삭제
                        while (transform.childCount > 0)
                        {
                            DestroyImmediate(transform.GetChild(0).gameObject);
                        }

                        // 관중 생성
                        CreateSpectators();
                    }
                };
            }
        }
    }

    // 에디터에서 실제로 게임 오브젝트를 생성하는 함수
    public void CreateSpectators()
    {
        if (geometry.spectator.Count == 0)
            return;

        GameObject spectatorPrefab = geometry.spectator[0];
        int spectatorIndex = 0;
        int createdSpectators = 0;
        int createdSeats = 0;

        // 관중을 담을 빈 컨테이너 생성
        GameObject spectatorsContainer = new GameObject("Spectators");
        spectatorsContainer.transform.SetParent(transform);
        spectatorsContainer.transform.localPosition = Vector3.zero;

        // 의자를 담을 빈 컨테이너 생성 (의자가 있는 경우에만)
        GameObject seatsContainer = null;
        if (geometry.seat != null)
        {
            seatsContainer = new GameObject("Seats");
            seatsContainer.transform.SetParent(transform);
            seatsContainer.transform.localPosition = Vector3.zero;
        }

        for (int i = 0; i < columns; i++)
        {
            for (int j = 0; j < rows; j++)
            {
                // 위치 계산 - CalculateMatrices()와 동일한 방식으로 계산
                float xPos = i * horizontalSpacing;
                float yPos = j * heightOffset;
                float zPos = j * verticalSpacing;
                Vector3 pos = new Vector3(xPos, yPos + spectatorHeight, zPos + spectatorForward);

                // 약간의 랜덤성 추가
                pos += new Vector3(
                    Random.Range(-0.05f, 0.05f),
                    Random.Range(-0.05f, 0.05f),
                    Random.Range(-0.05f, 0.05f)
                );

                // 순환하면서 다양한 관중 사용
                if (geometry.spectator.Count > 1)
                {
                    spectatorIndex = (spectatorIndex + 1) % geometry.spectator.Count;
                    spectatorPrefab = geometry.spectator[spectatorIndex];
                }

                // 관중 생성
                GameObject spectator = Instantiate(spectatorPrefab, spectatorsContainer.transform);
                spectator.transform.localPosition = pos;
                spectator.transform.localRotation = Quaternion.Euler(0, 180, 0);
                spectator.transform.localScale = Vector3.one;
                spectator.name = "Spectator_" + i + "_" + j;
                createdSpectators++;

                // 의자 생성
                if (geometry.seat != null && seatsContainer != null)
                {
                    GameObject seat = Instantiate(geometry.seat, seatsContainer.transform);
                    seat.transform.localPosition = new Vector3(xPos, yPos, zPos);
                    seat.transform.localRotation = Quaternion.identity;
                    seat.transform.localScale = Vector3.one;
                    seat.name = "Seat_" + i + "_" + j;
                    createdSeats++;
                }
            }
        }

        Debug.Log($"관중 생성 완료: {createdSpectators}명, 의자: {createdSeats}개");
    }
#endif

    void OnDisable()
    {
        // 메시와 머티리얼 리소스 정리
        if (instancedMaterials != null)
        {
            for (int i = 0; i < instancedMaterials.Length; i++)
            {
                if (instancedMaterials[i] != null)
                {
                    if (Application.isPlaying)
                        Destroy(instancedMaterials[i]);
                }
            }
        }
    }
}
