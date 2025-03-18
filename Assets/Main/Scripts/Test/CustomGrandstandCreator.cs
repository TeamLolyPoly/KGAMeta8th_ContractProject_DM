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
    public bool optimizeForIntegratedGPU = false; // 내장 그래픽용 최적화

    // 내부 변수
    private Camera mainCamera;
    private float distanceToCamera;
    private Matrix4x4[] matrices;
    private MaterialPropertyBlock propertyBlock;
    private Material[] instancedMaterials;
    private Mesh[] meshes;
    private int currentLODLevel = 0;
    private int totalInstances;

    // 이전 값들 저장용 변수들
    private int previousRows;
    private int previousColumns;
    private float previousHorizontalSpacing;
    private float previousVerticalSpacing;
    private float previousHeightOffset;
    private float previousSpectatorHeight;
    private float previousSpectatorForward;
    private GameObject[] previousLodPrefabs;
    private bool previousEnableInstancing;
    private bool previousOptimizeForIntegratedGPU;
    private List<GameObject> previousSpectators = new List<GameObject>();

    void Start()
    {
        mainCamera = Camera.main;

        // 초기 값 저장
        StoreCurrentValues();

        // 관중 프리팹 설정 확인
        if (geometry.spectator.Count == 0)
        {
            Debug.LogWarning(
                "관중 프리팹이 설정되지 않았습니다. 관중이 표시되지 않을 수 있습니다."
            );
        }
        else if (lodPrefabs.Length == 0)
        {
            // 관중 프리팹을 LOD 프리팹으로 사용
            Debug.Log("LOD 프리팹이 설정되지 않아 관중 프리팹을 사용합니다.");

            lodPrefabs = new GameObject[1];
            lodPrefabs[0] = geometry.spectator[0];
        }

        totalInstances = rows * columns;

        // 행렬 미리 계산
        CalculateMatrices();

        // 메시와 머티리얼 준비
        PrepareResources();

        // 프로퍼티 블록 초기화
        propertyBlock = new MaterialPropertyBlock();
    }

    void Update()
    {
        // 값이 변경되었는지 확인하고 업데이트
        CheckAndUpdateValues();

        if (!enableLOD)
            return;

        // 카메라와 거리 계산
        CalculateDistanceToCamera();

        // LOD 레벨 업데이트
        UpdateLODLevel();
    }

    // 값 변경 확인 및 업데이트
    private void CheckAndUpdateValues()
    {
        bool valuesChanged = false;
        bool resourcesChanged = false;

        // 관중 프리팹 변경 확인
        if (previousSpectators.Count != geometry.spectator.Count)
        {
            resourcesChanged = true;
        }
        else
        {
            for (int i = 0; i < geometry.spectator.Count; i++)
            {
                if (previousSpectators[i] != geometry.spectator[i])
                {
                    resourcesChanged = true;
                    break;
                }
            }
        }

        // 그리드 설정 변경 확인
        if (previousRows != rows || previousColumns != columns)
        {
            totalInstances = rows * columns;
            valuesChanged = true;
        }

        // 간격 설정 변경 확인
        if (
            previousHorizontalSpacing != horizontalSpacing
            || previousVerticalSpacing != verticalSpacing
            || previousHeightOffset != heightOffset
        )
        {
            valuesChanged = true;
        }

        // 관중 위치 설정 변경 확인
        if (
            previousSpectatorHeight != spectatorHeight
            || previousSpectatorForward != spectatorForward
        )
        {
            valuesChanged = true;
        }

        // LOD 프리팹 변경 확인
        if (previousLodPrefabs == null || previousLodPrefabs.Length != lodPrefabs.Length)
        {
            resourcesChanged = true;
            previousLodPrefabs = new GameObject[lodPrefabs.Length];
        }

        for (int i = 0; i < lodPrefabs.Length; i++)
        {
            if (i >= previousLodPrefabs.Length || previousLodPrefabs[i] != lodPrefabs[i])
            {
                resourcesChanged = true;
                if (i < previousLodPrefabs.Length)
                {
                    previousLodPrefabs[i] = lodPrefabs[i];
                }
            }
        }

        // 인스턴싱 또는 최적화 설정 변경 확인
        if (
            previousEnableInstancing != enableInstancing
            || previousOptimizeForIntegratedGPU != optimizeForIntegratedGPU
        )
        {
            resourcesChanged = true;
            previousEnableInstancing = enableInstancing;
            previousOptimizeForIntegratedGPU = optimizeForIntegratedGPU;
        }

        // 값이 변경되었으면 업데이트
        if (valuesChanged)
        {
            // 행렬 다시 계산
            CalculateMatrices();

            // 현재 값들 저장
            StoreCurrentValues();

            // 디버그 메시지
            Debug.Log("인스펙터 값이 변경되어 관중 배치가 업데이트되었습니다.");
        }

        // 리소스가 변경되었으면 업데이트
        if (resourcesChanged)
        {
            // 관중 프리팹이 변경되고 LOD 프리팹이 설정되지 않은 경우
            if (geometry.spectator.Count > 0 && (lodPrefabs == null || lodPrefabs.Length == 0))
            {
                // 관중 프리팹을 LOD 프리팹으로 사용
                lodPrefabs = new GameObject[1];
                lodPrefabs[0] = geometry.spectator[0];
                Debug.Log("관중 프리팹을 LOD 프리팹으로 설정했습니다.");
            }

            // 현재 관중 프리팹 저장
            previousSpectators.Clear();
            foreach (var spectator in geometry.spectator)
            {
                previousSpectators.Add(spectator);
            }

            // 메시와 머티리얼 다시 준비
            PrepareResources();

            // 디버그 메시지
            Debug.Log("리소스가 변경되어 업데이트되었습니다.");
        }
    }

    // 현재 값들 저장
    private void StoreCurrentValues()
    {
        previousRows = rows;
        previousColumns = columns;
        previousHorizontalSpacing = horizontalSpacing;
        previousVerticalSpacing = verticalSpacing;
        previousHeightOffset = heightOffset;
        previousSpectatorHeight = spectatorHeight;
        previousSpectatorForward = spectatorForward;

        // LOD 프리팹 저장
        if (previousLodPrefabs == null || previousLodPrefabs.Length != lodPrefabs.Length)
        {
            previousLodPrefabs = new GameObject[lodPrefabs.Length];
        }

        for (int i = 0; i < lodPrefabs.Length; i++)
        {
            previousLodPrefabs[i] = lodPrefabs[i];
        }

        // 관중 프리팹 저장
        previousSpectators.Clear();
        foreach (var spectator in geometry.spectator)
        {
            previousSpectators.Add(spectator);
        }

        previousEnableInstancing = enableInstancing;
        previousOptimizeForIntegratedGPU = optimizeForIntegratedGPU;
    }

    // 행렬 미리 계산
    private void CalculateMatrices()
    {
        matrices = new Matrix4x4[totalInstances];
        int index = 0;

        for (int i = 0; i < columns; i++)
        {
            for (int j = 0; j < rows; j++)
            {
                // 위치 계산
                float xPos = i * horizontalSpacing;
                float yPos = j * heightOffset;
                float zPos = j * verticalSpacing;

                // 행렬 생성
                matrices[index++] = Matrix4x4.TRS(
                    new Vector3(xPos, yPos + spectatorHeight, zPos + spectatorForward),
                    Quaternion.Euler(0, 180, 0),
                    Vector3.one
                );
            }
        }
    }

    // 리소스 준비
    private void PrepareResources()
    {
        // 배열 크기가 다르면 새로 생성
        if (meshes == null || meshes.Length != lodPrefabs.Length)
        {
            meshes = new Mesh[lodPrefabs.Length];
        }

        if (instancedMaterials == null || instancedMaterials.Length != lodPrefabs.Length)
        {
            instancedMaterials = new Material[lodPrefabs.Length];
        }

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
                    // 스킨드 메시 사용
                    meshes[i] = skinnedMeshRenderer.sharedMesh;
                    Debug.Log($"LOD{i}: 스킨드 메시를 사용합니다 - {skinnedMeshRenderer.name}");
                }
            }

            // 머티리얼 가져오기 - 먼저 MeshRenderer 체크
            MeshRenderer renderer = lodPrefabs[i].GetComponent<MeshRenderer>();
            Material sourceMaterial = null;

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

                // 내장 그래픽 최적화
                if (optimizeForIntegratedGPU)
                {
                    instancedMaterials[i].DisableKeyword("_EMISSION");
                    instancedMaterials[i].DisableKeyword("_METALLICGLOSSMAP");
                    instancedMaterials[i].DisableKeyword("_NORMALMAP");
                }
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
        Vector3 centerPoint =
            transform.position
            + new Vector3(
                columns * horizontalSpacing * 0.5f,
                rows * heightOffset * 0.5f,
                rows * verticalSpacing * 0.5f
            );

        distanceToCamera = Vector3.Distance(mainCamera.transform.position, centerPoint);
    }

    // LOD 레벨 업데이트
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

    // 현재 LOD 레벨로 렌더링
    private void RenderCurrentLOD()
    {
        int lodLevel = currentLODLevel;

        // 메시나 머티리얼이 없으면 렌더링 불가
        if (meshes[lodLevel] == null || instancedMaterials[lodLevel] == null)
            return;

        // 프로퍼티 블록 초기화
        propertyBlock.Clear();

        // 그림자 설정
        bool receiveShadows = !(disableShadowsForDistant && lodLevel > 0);
        UnityEngine.Rendering.ShadowCastingMode shadowMode =
            (disableShadowsForDistant && lodLevel > 0)
                ? UnityEngine.Rendering.ShadowCastingMode.Off
                : UnityEngine.Rendering.ShadowCastingMode.On;

        // 인스턴스 그리기
        Graphics.DrawMeshInstanced(
            meshes[lodLevel],
            0,
            instancedMaterials[lodLevel],
            matrices,
            matrices.Length,
            propertyBlock,
            shadowMode,
            receiveShadows,
            gameObject.layer
        );
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
            // 에디터에서만 실행
            EditorInitialize();

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
        else
        {
            // 플레이 모드에서는 값 변경 확인 함수 호출
            CheckAndUpdateValues();
        }
    }

    // 에디터에서 실제로 게임 오브젝트를 생성하는 함수
    public void CreateSpectators()
    {
        if (geometry.spectator.Count == 0)
            return;

        GameObject spectatorPrefab = geometry.spectator[0];
        int spectatorIndex = 0;

        for (int i = 0; i < columns; i++)
        {
            for (int j = 0; j < rows; j++)
            {
                // 위치 계산
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
                GameObject spectator = Instantiate(spectatorPrefab, transform);
                spectator.transform.localPosition = pos;
                spectator.transform.localRotation = Quaternion.Euler(0, 180, 0);
                spectator.transform.localScale = Vector3.one;
                spectator.name = "Spectator_" + i + "_" + j;
            }
        }
    }

    // 에디터 전용 초기화
    private void EditorInitialize()
    {
        // 카메라가 없으면 씬 카메라 사용
        if (mainCamera == null)
        {
            if (Camera.main != null)
            {
                mainCamera = Camera.main;
            }
            else
            {
                // 에디터에서는 카메라 없어도 계속 진행
                mainCamera = FindObjectOfType<Camera>();
            }
        }

        // 총 인스턴스 수 계산
        totalInstances = rows * columns;

        // 행렬 계산
        if (matrices == null || matrices.Length != totalInstances)
        {
            CalculateMatrices();
        }
        else
        {
            // 행렬 업데이트
            int index = 0;
            for (int i = 0; i < columns; i++)
            {
                for (int j = 0; j < rows; j++)
                {
                    float xPos = i * horizontalSpacing;
                    float yPos = j * heightOffset;
                    float zPos = j * verticalSpacing;

                    matrices[index++] = Matrix4x4.TRS(
                        new Vector3(xPos, yPos + spectatorHeight, zPos + spectatorForward),
                        Quaternion.Euler(0, 180, 0),
                        Vector3.one
                    );
                }
            }
        }

        // 프로퍼티 블록 초기화
        if (propertyBlock == null)
        {
            propertyBlock = new MaterialPropertyBlock();
        }

        // 리소스 준비
        if (lodPrefabs != null && lodPrefabs.Length > 0)
        {
            PrepareResources();
        }

        // 에디터에서 씬 뷰를 다시 그리도록 함
        UnityEditor.SceneView.RepaintAll();
    }
#endif
}
