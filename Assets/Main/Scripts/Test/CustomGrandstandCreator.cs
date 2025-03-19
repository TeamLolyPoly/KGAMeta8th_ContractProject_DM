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

    // 내부 변수
    private Camera mainCamera;
    private float distanceToCamera;
    private Matrix4x4[] matrices;
    private MaterialPropertyBlock propertyBlock;
    private Material[] instancedMaterials;
    private Mesh[] meshes;
    private int currentLODLevel = 0;
    private int totalInstances;

    void Start()
    {
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
    }

    void Update()
    {
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
        Vector3 centerPoint =
            transform.position
            + new Vector3(
                columns * horizontalSpacing * 0.5f,
                rows * heightOffset * 0.5f,
                rows * verticalSpacing * 0.5f
            );

        distanceToCamera = Vector3.Distance(mainCamera.transform.position, centerPoint);
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

        // 모든 관중 그리기 (밀도 = 1)
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

                // 의자 생성
                if (geometry.seat != null)
                {
                    GameObject seat = Instantiate(geometry.seat, transform);
                    seat.transform.localPosition = new Vector3(xPos, yPos, zPos);
                    seat.transform.localRotation = Quaternion.identity;
                    seat.transform.localScale = Vector3.one;
                    seat.name = "Seat_" + i + "_" + j;
                }
            }
        }

        Debug.Log($"관중 생성 완료: {rows * columns} 명");
    }
#endif
}
