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

        // 머티리얼 최적화
        OptimizeMaterials();
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
        for (int i = 0; i < lodMaterials.Length; i++)
        {
            if (lodMaterials[i] != null)
            {
                // GPU 인스턴싱 활성화
                lodMaterials[i].enableInstancing = true;

                // LOD 레벨에 따라 그림자 설정 최적화
                if (useShadowsOnlyForLOD0 && i > 0)
                {
                    lodMaterials[i].SetFloat("_CastShadows", 0);
                    lodMaterials[i].SetFloat("_ReceiveShadows", 0);
                }

                // 원거리 LOD는 저해상도 텍스처 사용
                if (i > 1)
                {
                    // 텍스처 퀄리티 감소
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
        UpdateLODLevel();

        // 애니메이터 최적화 (거리에 따라 업데이트 속도 조절)
        if (optimizeAnimators)
        {
            UpdateAnimators();
        }
    }

    private void UpdateDistanceToCamera()
    {
        distanceToCamera = Vector3.Distance(
            mainCamera.transform.position,
            transform.position + centerOffset
        );
    }

    private void UpdateLODLevel()
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
                // 현재 LOD 레벨이 바뀌는 경우만 새로 렌더링
                if (currentLODLevel != i)
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

        Graphics.DrawMeshInstancedProcedural(
            lodMeshes[lodLevel],
            0,
            lodMaterials[lodLevel],
            renderBounds,
            totalInstances,
            propertyBlock
        );
    }

    private void RenderStandard(int lodLevel)
    {
        if (lodMeshes[lodLevel] == null)
            return;

        propertyBlock.Clear();
        propertyBlock.SetFloat("_LODLevel", lodLevel);

        // 그림자 설정
        UnityEngine.Rendering.ShadowCastingMode shadowMode =
            (useShadowsOnlyForLOD0 && lodLevel > 0)
                ? UnityEngine.Rendering.ShadowCastingMode.Off
                : UnityEngine.Rendering.ShadowCastingMode.On;

        Graphics.DrawMeshInstanced(
            lodMeshes[lodLevel],
            0,
            lodMaterials[lodLevel],
            instanceMatrices,
            totalInstances,
            propertyBlock,
            shadowMode,
            lodLevel == 0, // 원거리 LOD는 그림자 안받음
            LayerMask.NameToLayer("Default")
        );
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
}

// VR 특화 데이터 구조
public struct SpectatorData
{
    public Vector3 position;
    public Quaternion rotation;
    public float animationTime;
}
