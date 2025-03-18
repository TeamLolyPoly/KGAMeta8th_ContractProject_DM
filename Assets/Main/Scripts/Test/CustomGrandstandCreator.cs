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

    [Header("Grid Settings")]
    [Range(0, 32)]
    public int rows = 10;

    [Range(0, 50)]
    public int columns = 10;

    [Header("Grid Spacing")]
    [Range(0.5f, 2.0f)]
    public float horizontalSpacing = 1.0f;

    [Range(0.5f, 2.0f)]
    public float verticalSpacing = 1.0f;

    [Range(0.1f, 2.0f)]
    public float heightOffset = 1.0f;

    [Header("Spectator Position")]
    [Range(-1f, 1f)]
    public float spectatorHeight = -0.2f;

    [Range(-1f, 1f)]
    public float spectatorForward = 0.3f;

    [Header("VR Settings")]
    public bool useVR = true;
    public bool useAsyncCompute = true;

    [Header("LOD Settings")]
    public float[] lodDistances = new float[] { 10f, 20f, 30f, 40f, 50f }; // 5단계 LOD
    public GameObject[] lodPrefabs; // 각 LOD 레벨별 메시
    public Material[] lodMaterials; // 각 LOD 레벨별 머티리얼

    [Header("최적화 설정")]
    public bool combineByMaterial = true; // 동일 머티리얼 그룹화
    public bool useShadowsOnlyForLOD0 = true; // LOD0에서만 그림자 사용
    public bool disableDynamicBatching = true; // 동적 배칭 비활성화
    public int visibleCullingDistance = 60; // 보이는 거리 제한
    public bool optimizeAnimators = true; // 애니메이터 최적화
    public float animationCullingDistance = 40f; // 애니메이션 컬링 거리

    [Range(1, 10)]
    public int batchSize = 5; // 하나의 배치로 묶을 객체 수
    public bool useMeshGroups = true; // 메시 그룹화 사용

    [Range(1, 10)]
    public int meshGroupSize = 5; // 하나의 메시 그룹 크기

    private Camera mainCamera;
    private float distanceToCamera;
    private MaterialPropertyBlock propertyBlock;
    private SkinnedMeshRenderer[] spectatorRenderers;
    private Matrix4x4[] instanceMatrices;
    private int totalInstances;
    private Mesh cachedBakedMesh;
    private Dictionary<int, Material> materialCache;
    private ComputeBuffer spectatorBuffer;
    private bool isVRInitialized = false;
    private int currentLODLevel = 0;
    private Vector3 centerOffset;
    private Bounds renderBounds;
    private Mesh[] lodMeshes;
    private Mesh[] groupedMeshes; // 그룹화된 메시

    void Start()
    {
        mainCamera = Camera.main;
        propertyBlock = new MaterialPropertyBlock();
        materialCache = new Dictionary<int, Material>();

        // 동적 배칭 비활성화 (GPU 인스턴싱으로 대체)
        if (disableDynamicBatching)
        {
            Physics.autoSyncTransforms = false;
        }

        // 애니메이터 최적화
        if (optimizeAnimators)
        {
            OptimizeAnimators();
        }

        InitializeOptimizedRendering();
        if (useVR && useAsyncCompute)
        {
            InitializeVROptimizations();
        }

        // 중심점 오프셋 미리 계산
        centerOffset = new Vector3(
            columns * horizontalSpacing * 0.5f,
            rows * heightOffset * 0.5f,
            rows * verticalSpacing * 0.5f
        );

        // Bounds 미리 계산
        renderBounds = new Bounds(transform.position, Vector3.one * 100f);

        // LOD 메시 캐싱
        CacheLODMeshes();

        // 메시 그룹화
        if (useMeshGroups)
        {
            GroupMeshes();
        }

        // 머티리얼 최적화
        OptimizeMaterials();

        // 첫 프레임에 한 번 렌더링
        UpdateLODLevel(true);
    }

    private void CacheLODMeshes()
    {
        lodMeshes = new Mesh[lodPrefabs.Length];
        for (int i = 0; i < lodPrefabs.Length; i++)
        {
            if (lodPrefabs[i] != null)
            {
                var meshFilter = lodPrefabs[i].GetComponent<MeshFilter>();
                if (meshFilter != null)
                {
                    lodMeshes[i] = meshFilter.sharedMesh;
                }
            }
        }
    }

    private void InitializeVROptimizations()
    {
        isVRInitialized = true;
        if (useAsyncCompute)
        {
            InitializeAsyncCompute();
        }
    }

    private void InitializeAsyncCompute()
    {
        totalInstances = rows * columns;
        spectatorBuffer = new ComputeBuffer(totalInstances, sizeof(float) * 8);

        SpectatorData[] initialData = new SpectatorData[totalInstances];
        int index = 0;
        for (int i = 0; i < columns; i++)
        {
            for (int j = 0; j < rows; j++)
            {
                float xPos = i * horizontalSpacing;
                float yPos = j * heightOffset;
                float zPos = j * verticalSpacing;

                initialData[index].position = new Vector3(
                    xPos,
                    yPos + spectatorHeight,
                    zPos + spectatorForward
                );
                initialData[index].rotation = Quaternion.Euler(0, 180, 0);
                initialData[index].animationTime = Random.Range(0f, 2f * Mathf.PI);
                index++;
            }
        }
        spectatorBuffer.SetData(initialData);
    }

    private void InitializeOptimizedRendering()
    {
        if (geometry.spectator.Count == 0)
            return;

        totalInstances = rows * columns;
        instanceMatrices = new Matrix4x4[totalInstances];
        PreCalculateMatrices();

        spectatorRenderers = geometry.spectator[0].GetComponentsInChildren<SkinnedMeshRenderer>();
        if (spectatorRenderers.Length > 0 && spectatorRenderers[0] != null)
        {
            cachedBakedMesh = new Mesh();
            spectatorRenderers[0].BakeMesh(cachedBakedMesh);
        }
    }

    private void PreCalculateMatrices()
    {
        int index = 0;
        for (int i = 0; i < columns; i++)
        {
            for (int j = 0; j < rows; j++)
            {
                float xPos = i * horizontalSpacing;
                float yPos = j * heightOffset;
                float zPos = j * verticalSpacing;

                instanceMatrices[index++] = Matrix4x4.TRS(
                    new Vector3(xPos, yPos + spectatorHeight, zPos + spectatorForward),
                    Quaternion.Euler(0, 180, 0),
                    Vector3.one
                );
            }
        }
    }

    private void PrepareMaterials()
    {
        if (spectatorRenderers == null || spectatorRenderers.Length == 0)
            return;

        foreach (var renderer in spectatorRenderers)
        {
            if (renderer == null)
                continue;

            for (int i = 0; i < renderer.sharedMaterials.Length; i++)
            {
                var originalMaterial = renderer.sharedMaterials[i];
                if (originalMaterial == null)
                    continue;

                if (!materialCache.ContainsKey(i))
                {
                    Material instancedMaterial = new Material(originalMaterial);
                    instancedMaterial.enableInstancing = true;
                    materialCache[i] = instancedMaterial;
                }
            }
        }

        if (spectatorRenderers[0] != null)
        {
            cachedBakedMesh = new Mesh();
            spectatorRenderers[0].BakeMesh(cachedBakedMesh);
        }
    }

    private void OptimizeMaterials()
    {
        // 원본 머티리얼을 저장 (디버깅용)
        Material[] originalMaterials = new Material[lodMaterials.Length];
        for (int i = 0; i < lodMaterials.Length; i++)
        {
            if (lodMaterials[i] != null)
            {
                originalMaterials[i] = lodMaterials[i];

                // 새 머티리얼 인스턴스 생성 (수정 가능하도록)
                lodMaterials[i] = new Material(originalMaterials[i]);

                // GPU 인스턴싱 활성화 (핵심)
                lodMaterials[i].enableInstancing = true;

                // 셰이더 키워드 최소화
                lodMaterials[i].DisableKeyword("_EMISSION");
                lodMaterials[i].DisableKeyword("_METALLICGLOSSMAP");
                lodMaterials[i].DisableKeyword("_DETAIL_MULX2");

                // 그림자 최적화
                if (useShadowsOnlyForLOD0 && i > 0)
                {
                    // 쉐이더가 이 프로퍼티들을 지원하는지 확인
                    if (lodMaterials[i].HasProperty("_CastShadows"))
                        lodMaterials[i].SetFloat("_CastShadows", 0);
                    if (lodMaterials[i].HasProperty("_ReceiveShadows"))
                        lodMaterials[i].SetFloat("_ReceiveShadows", 0);
                }

                // 원거리 LOD 최적화
                if (i > 1)
                {
                    // 텍스처 필터링 모드 변경
                    Texture mainTex = lodMaterials[i].mainTexture;
                    if (mainTex != null)
                    {
                        mainTex.filterMode = FilterMode.Bilinear;
                        mainTex.mipMapBias = 1f; // 낮은 해상도 미립맵 사용
                    }

                    // 품질 관련 설정
                    if (lodMaterials[i].HasProperty("_TextureQuality"))
                        lodMaterials[i].SetFloat("_TextureQuality", 0.5f);
                }
            }
        }
    }

    private void OptimizeAnimators()
    {
        // 모든 spectator prefab에서 Animator 컴포넌트 찾기
        foreach (GameObject spectator in geometry.spectator)
        {
            if (spectator == null)
                continue;

            // 모든 애니메이터 가져오기
            Animator[] animators = spectator.GetComponentsInChildren<Animator>();
            foreach (Animator animator in animators)
            {
                // 컬링 모드 설정
                animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;

                // 애니메이션 품질 설정 (낮은 품질, 높은 성능)
                animator.updateMode = AnimatorUpdateMode.Normal;

                // 루트 모션 비활성화
                animator.applyRootMotion = false;
            }
        }
    }

    void Update()
    {
        UpdateDistanceToCamera();

        // 5프레임마다 한 번씩 LOD 업데이트 (성능 개선)
        if (Time.frameCount % 5 == 0)
        {
            UpdateLODLevel(false);
        }

        // 10프레임마다 한 번씩 애니메이터 업데이트 (성능 개선)
        if (optimizeAnimators && Time.frameCount % 10 == 0)
        {
            UpdateAnimators();
        }
    }

    private void UpdateDistanceToCamera()
    {
        distanceToCamera = Vector3.SqrMagnitude(
            mainCamera.transform.position - (transform.position + centerOffset)
        );

        // 제곱 거리를 실제 거리로 변환 (최적화를 위해 필요한 경우에만 제곱근 계산)
        distanceToCamera = Mathf.Sqrt(distanceToCamera);
    }

    private void UpdateLODLevel(bool forceUpdate = false)
    {
        // 가시성 컬링 - 설정된 거리를 넘어가면 렌더링 안함
        if (distanceToCamera > visibleCullingDistance)
        {
            return;
        }

        // 컬링 거리를 벗어난 경우
        if (distanceToCamera > lodDistances[lodDistances.Length - 1])
        {
            return;
        }

        // LOD 레벨 결정
        for (int i = 0; i < lodDistances.Length; i++)
        {
            if (distanceToCamera <= lodDistances[i])
            {
                // 현재 LOD 레벨이 바뀌는 경우 또는 강제 업데이트
                if (currentLODLevel != i || forceUpdate)
                {
                    currentLODLevel = i;
                    RenderLOD(i);
                }
                break;
            }
        }
    }

    private void RenderLOD(int lodLevel)
    {
        if (useVR && useAsyncCompute)
        {
            RenderVROptimized(lodLevel);
        }
        else
        {
            RenderStandard(lodLevel);
        }
    }

    private void RenderVROptimized(int lodLevel)
    {
        if (!isVRInitialized || spectatorBuffer == null || lodMeshes[lodLevel] == null)
            return;

        propertyBlock.Clear();
        propertyBlock.SetBuffer("_SpectatorBuffer", spectatorBuffer);
        propertyBlock.SetFloat("_LODLevel", lodLevel);

        // 프로시저럴 드로우 최적화
        if (totalInstances <= 1023) // 한 번에 그릴 수 있는 최대 인스턴스 수
        {
            Graphics.DrawMeshInstancedProcedural(
                lodMeshes[lodLevel],
                0,
                lodMaterials[lodLevel],
                renderBounds,
                totalInstances,
                propertyBlock
            );
        }
        else
        {
            // 배치 크기에 따라 나누어 그리기
            int maxBatchSize = 1023;
            int batchCount = Mathf.CeilToInt((float)totalInstances / maxBatchSize);

            for (int batch = 0; batch < batchCount; batch++)
            {
                int startIdx = batch * maxBatchSize;
                int count = Mathf.Min(maxBatchSize, totalInstances - startIdx);

                if (count <= 0)
                    continue;

                // 오프셋 설정
                propertyBlock.SetInt("_InstanceOffset", startIdx);

                Graphics.DrawMeshInstancedProcedural(
                    lodMeshes[lodLevel],
                    0,
                    lodMaterials[lodLevel],
                    renderBounds,
                    count,
                    propertyBlock
                );
            }
        }
    }

    private void RenderStandard(int lodLevel)
    {
        Mesh meshToRender;
        int instanceCount;
        Matrix4x4[] matricesToUse;

        if (useMeshGroups && groupedMeshes != null && groupedMeshes[lodLevel] != null)
        {
            // 그룹화된 메시 사용
            meshToRender = groupedMeshes[lodLevel];
            instanceCount = Mathf.CeilToInt((float)totalInstances / meshGroupSize);

            // 그룹화된 인스턴스 매트릭스 생성
            matricesToUse = new Matrix4x4[instanceCount];
            for (int i = 0; i < instanceCount; i++)
            {
                int baseIndex = i * meshGroupSize;
                if (baseIndex < instanceMatrices.Length)
                {
                    matricesToUse[i] = instanceMatrices[baseIndex];
                }
            }
        }
        else
        {
            // 원본 메시 사용
            if (lodMeshes[lodLevel] == null || lodMaterials[lodLevel] == null)
                return;

            meshToRender = lodMeshes[lodLevel];
            instanceCount = totalInstances;
            matricesToUse = instanceMatrices;
        }

        propertyBlock.Clear();
        propertyBlock.SetFloat("_LODLevel", lodLevel);

        // 그림자 설정
        UnityEngine.Rendering.ShadowCastingMode shadowMode =
            (useShadowsOnlyForLOD0 && lodLevel > 0)
                ? UnityEngine.Rendering.ShadowCastingMode.Off
                : UnityEngine.Rendering.ShadowCastingMode.On;

        // 배치 크기에 따라 인스턴스 그룹화
        int batchCount = Mathf.CeilToInt((float)instanceCount / batchSize);

        for (int batch = 0; batch < batchCount; batch++)
        {
            int startIdx = batch * batchSize;
            int count = Mathf.Min(batchSize, instanceCount - startIdx);

            if (count <= 0)
                continue;

            // 현재 배치의 매트릭스 배열 생성
            Matrix4x4[] batchMatrices = new Matrix4x4[count];
            System.Array.Copy(matricesToUse, startIdx, batchMatrices, 0, count);

            // 인스턴스 그리기
            Graphics.DrawMeshInstanced(
                meshToRender,
                0,
                lodMaterials[lodLevel],
                batchMatrices,
                count,
                propertyBlock,
                shadowMode,
                lodLevel == 0, // 원거리 LOD는 그림자 안받음
                LayerMask.NameToLayer("Default")
            );
        }
    }

    private void UpdateAnimators()
    {
        // 애니메이션 컬링 거리를 벗어나면 애니메이터 비활성화
        bool enableAnimations = distanceToCamera <= animationCullingDistance;

        // spectator의 모든 애니메이터 활성화/비활성화
        foreach (GameObject spectator in geometry.spectator)
        {
            if (spectator == null)
                continue;

            Animator[] animators = spectator.GetComponentsInChildren<Animator>();
            foreach (Animator animator in animators)
            {
                // 이미 같은 상태라면 상태 변경 스킵
                if (animator.enabled != enableAnimations)
                {
                    animator.enabled = enableAnimations;
                }

                // 거리에 따라 업데이트 모드 변경
                if (enableAnimations)
                {
                    // 거리에 따라 애니메이션 품질 조절
                    if (distanceToCamera < lodDistances[0])
                    {
                        animator.updateMode = AnimatorUpdateMode.Normal;
                    }
                    else if (distanceToCamera < lodDistances[1])
                    {
                        animator.updateMode = AnimatorUpdateMode.AnimatePhysics;
                    }
                    else
                    {
                        animator.updateMode = AnimatorUpdateMode.AnimatePhysics;

                        // 먼 거리의 애니메이터는 스킵 프레임 설정
                        if (Time.frameCount % 2 != 0)
                        {
                            continue;
                        }
                    }
                }
            }
        }
    }

    private void OnDestroy()
    {
        if (cachedBakedMesh != null)
            Destroy(cachedBakedMesh);

        foreach (var material in materialCache.Values)
        {
            if (material != null)
                Destroy(material);
        }

        if (spectatorBuffer != null)
        {
            spectatorBuffer.Release();
            spectatorBuffer.Dispose();
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying)
            return;

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
    }

    private void GroupMeshes()
    {
        if (lodMeshes == null || lodMeshes.Length == 0)
            return;

        groupedMeshes = new Mesh[lodMeshes.Length];

        for (int lodLevel = 0; lodLevel < lodMeshes.Length; lodLevel++)
        {
            if (lodMeshes[lodLevel] == null)
                continue;

            // 원본 메시 복사
            Mesh originalMesh = lodMeshes[lodLevel];

            // 메시 그룹 생성
            Mesh groupedMesh = new Mesh();
            groupedMesh.name = originalMesh.name + "_Grouped";

            // 원본 메시의 데이터 가져오기
            Vector3[] vertices = originalMesh.vertices;
            Vector3[] normals = originalMesh.normals;
            Vector2[] uvs = originalMesh.uv;
            int[] triangles = originalMesh.triangles;

            // 새 그룹 메시 데이터 준비
            List<Vector3> newVertices = new List<Vector3>();
            List<Vector3> newNormals = new List<Vector3>();
            List<Vector2> newUVs = new List<Vector2>();
            List<int> newTriangles = new List<int>();

            // meshGroupSize개의 메시를 하나로 병합
            for (int i = 0; i < meshGroupSize; i++)
            {
                int vertexOffset = newVertices.Count;

                // 버텍스 오프셋 계산 (간격 유지를 위해)
                Vector3 posOffset = new Vector3(i * 0.5f, 0, 0);

                // 버텍스, 노말, UV 복사
                for (int v = 0; v < vertices.Length; v++)
                {
                    newVertices.Add(vertices[v] + posOffset);
                    if (normals.Length > v)
                        newNormals.Add(normals[v]);
                    if (uvs.Length > v)
                        newUVs.Add(uvs[v]);
                }

                // 삼각형 인덱스 복사 (버텍스 오프셋 적용)
                for (int t = 0; t < triangles.Length; t++)
                {
                    newTriangles.Add(triangles[t] + vertexOffset);
                }
            }

            // 새 메시에 데이터 할당
            groupedMesh.vertices = newVertices.ToArray();
            groupedMesh.normals = newNormals.ToArray();
            groupedMesh.uv = newUVs.ToArray();
            groupedMesh.triangles = newTriangles.ToArray();

            // 메시 최적화
            groupedMesh.RecalculateBounds();
            groupedMesh.Optimize();

            // 그룹 메시 저장
            groupedMeshes[lodLevel] = groupedMesh;
        }
    }
}

// VR 특화 데이터 구조
public struct SpectatorData
{
    public Vector3 position;
    public Quaternion rotation;
    public float animationTime;
}
