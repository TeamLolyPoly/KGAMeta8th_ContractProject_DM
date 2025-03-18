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

    void Start()
    {
        mainCamera = Camera.main;
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
        if (!enableLOD)
            return;

        // 카메라와 거리 계산
        CalculateDistanceToCamera();

        // LOD 레벨 업데이트
        UpdateLODLevel();
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

            // 머티리얼 가져오기
            MeshRenderer renderer = lodPrefabs[i].GetComponent<MeshRenderer>();
            if (renderer != null && renderer.sharedMaterial != null)
            {
                // 인스턴싱 가능한 머티리얼 생성
                instancedMaterials[i] = new Material(renderer.sharedMaterial);
                instancedMaterials[i].enableInstancing = enableInstancing;

                // 내장 그래픽 최적화
                if (optimizeForIntegratedGPU)
                {
                    instancedMaterials[i].DisableKeyword("_EMISSION");
                    instancedMaterials[i].DisableKeyword("_METALLICGLOSSMAP");
                    instancedMaterials[i].DisableKeyword("_NORMALMAP");
                }
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
}
