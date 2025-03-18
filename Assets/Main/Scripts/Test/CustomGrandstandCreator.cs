using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CustomGeometry
{
    public GameObject seat;
    public List<GameObject> spectator = new List<GameObject>();
    public GameObject lowPolySpectator;
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

    [Header("VR Performance Settings")]
    public bool useVR = true;
    public float vrNearDistance = 10f; // VR에서는 더 가까운 거리
    public float vrMidDistance = 20f;
    public float vrFarDistance = 30f;
    public float vrCullingDistance = 40f;
    public bool useAsyncCompute = true; // 비동기 컴퓨트 사용
    public bool useMultiView = true; // 멀티뷰 렌더링 사용

    [Header("Performance Settings")]
    public float nearDistance = 15f;
    public float midDistance = 30f;
    public float farDistance = 45f;
    public float cullingDistance = 60f;

    [Header("Material Settings")]
    public Material highQualityMaterial;
    public Material lowQualityMaterial;

    [Header("Animation Settings")]
    public float nearAnimationInterval = 0.033f;
    public float midAnimationInterval = 0.066f;
    public float farAnimationInterval = 0.1f;

    private Camera mainCamera;
    private float distanceToCamera;
    private MaterialPropertyBlock propertyBlock;
    private SkinnedMeshRenderer[] spectatorRenderers;
    private Matrix4x4[] instanceMatrices;
    private int totalInstances;
    private float nextAnimationUpdate = 0f;
    private float currentAnimationInterval;
    private Mesh cachedBakedMesh;
    private Dictionary<int, Material> materialCache;
    private ComputeBuffer spectatorBuffer;
    private bool isVRInitialized = false;

    void Start()
    {
        mainCamera = Camera.main;
        propertyBlock = new MaterialPropertyBlock();
        materialCache = new Dictionary<int, Material>();

        if (useVR)
        {
            InitializeVROptimizations();
        }

        InitializeOptimizedRendering();
        PrepareMaterials();
    }

    private void InitializeVROptimizations()
    {
        // VR 특화 초기화
        isVRInitialized = true;

        // VR 성능 설정
        if (useAsyncCompute)
        {
            // 비동기 컴퓨트 초기화
            InitializeAsyncCompute();
        }

        if (useMultiView)
        {
            // 멀티뷰 렌더링 초기화
            InitializeMultiView();
        }
    }

    private void InitializeAsyncCompute()
    {
        // 컴퓨트 버퍼 초기화
        totalInstances = rows * columns;
        spectatorBuffer = new ComputeBuffer(totalInstances, sizeof(float) * 8);

        // 초기 데이터 설정
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

    private void InitializeMultiView()
    {
        // VR 멀티뷰 렌더링 설정
        foreach (var material in materialCache.Values)
        {
            if (material != null)
            {
                material.EnableKeyword("MULTIVIEW");
            }
        }
    }

    private void InitializeOptimizedRendering()
    {
        if (geometry.spectator.Count == 0)
            return;

        spectatorRenderers = geometry.spectator[0].GetComponentsInChildren<SkinnedMeshRenderer>();
        totalInstances = rows * columns;
        instanceMatrices = new Matrix4x4[totalInstances];
        PreCalculateMatrices();
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

    void Update()
    {
        UpdateDistanceToCamera();

        if (Time.time > nextAnimationUpdate)
        {
            UpdateBasedOnDistance();
            nextAnimationUpdate = Time.time + currentAnimationInterval;
        }
    }

    private void UpdateDistanceToCamera()
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

    private void UpdateBasedOnDistance()
    {
        if (distanceToCamera > (useVR ? vrCullingDistance : cullingDistance))
            return;

        if (distanceToCamera <= (useVR ? vrNearDistance : nearDistance))
        {
            RenderHighQuality();
            currentAnimationInterval = nearAnimationInterval;
        }
        else if (distanceToCamera <= (useVR ? vrMidDistance : midDistance))
        {
            RenderMediumQuality();
            currentAnimationInterval = midAnimationInterval;
        }
        else
        {
            RenderLowQuality();
            currentAnimationInterval = farAnimationInterval;
        }
    }

    private void RenderHighQuality()
    {
        if (spectatorRenderers == null || spectatorRenderers.Length == 0)
            return;

        var mainRenderer = spectatorRenderers[0];
        if (mainRenderer == null)
            return;

        if (useVR && useAsyncCompute)
        {
            // VR 비동기 컴퓨트 렌더링
            RenderVROptimized();
        }
        else
        {
            // 기존 렌더링 방식
            mainRenderer.BakeMesh(cachedBakedMesh);
            RenderInstanced();
        }
    }

    private void RenderVROptimized()
    {
        if (!isVRInitialized || spectatorBuffer == null)
            return;

        // VR 특화 렌더링
        propertyBlock.Clear();
        propertyBlock.SetBuffer("_SpectatorBuffer", spectatorBuffer);
        propertyBlock.SetFloat("_VRMode", 1.0f);

        if (useMultiView)
        {
            // VR 멀티뷰 렌더링
            Graphics.DrawMeshInstancedProcedural(
                cachedBakedMesh,
                0,
                materialCache[0],
                new Bounds(transform.position, Vector3.one * 100f),
                totalInstances,
                propertyBlock
            );
        }
        else
        {
            // 일반 VR 렌더링
            Graphics.DrawMeshInstancedProcedural(
                cachedBakedMesh,
                0,
                materialCache[0],
                new Bounds(transform.position, Vector3.one * 100f),
                totalInstances,
                propertyBlock
            );
        }
    }

    private void RenderInstanced()
    {
        for (int i = 0; i < cachedBakedMesh.subMeshCount; i++)
        {
            if (!materialCache.ContainsKey(i))
                continue;

            propertyBlock.Clear();
            propertyBlock.SetFloat("_LODLevel", 1.0f);

            Graphics.DrawMeshInstanced(
                cachedBakedMesh,
                i,
                materialCache[i],
                instanceMatrices,
                totalInstances,
                propertyBlock,
                UnityEngine.Rendering.ShadowCastingMode.On,
                true,
                LayerMask.NameToLayer("Default")
            );
        }
    }

    private void RenderMediumQuality()
    {
        if (spectatorRenderers == null || spectatorRenderers.Length == 0)
            return;

        var mainRenderer = spectatorRenderers[0];
        if (mainRenderer == null)
            return;

        if (Time.frameCount % 2 == 0)
        {
            mainRenderer.BakeMesh(cachedBakedMesh);
        }

        for (int i = 0; i < mainRenderer.sharedMesh.subMeshCount; i++)
        {
            if (!materialCache.ContainsKey(i))
                continue;

            propertyBlock.Clear();
            propertyBlock.SetFloat("_LODLevel", 0.5f);

            Graphics.DrawMeshInstanced(
                cachedBakedMesh,
                i,
                materialCache[i],
                instanceMatrices,
                totalInstances,
                propertyBlock,
                UnityEngine.Rendering.ShadowCastingMode.Off,
                false,
                LayerMask.NameToLayer("Default")
            );
        }
    }

    private void RenderLowQuality()
    {
        if (geometry.lowPolySpectator == null)
            return;

        var lowPolyRenderer = geometry.lowPolySpectator.GetComponent<MeshRenderer>();
        var meshFilter = geometry.lowPolySpectator.GetComponent<MeshFilter>();

        if (lowPolyRenderer == null || meshFilter == null)
            return;

        propertyBlock.Clear();
        propertyBlock.SetFloat("_LODLevel", 0.0f);

        var material = lowPolyRenderer.sharedMaterial;
        if (!material.enableInstancing)
        {
            material = new Material(material);
            material.enableInstancing = true;
        }

        Graphics.DrawMeshInstanced(
            meshFilter.sharedMesh,
            0,
            material,
            instanceMatrices,
            totalInstances,
            propertyBlock,
            UnityEngine.Rendering.ShadowCastingMode.Off,
            false,
            LayerMask.NameToLayer("Default")
        );
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

        if (useVR)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, vrNearDistance);

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, vrMidDistance);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, vrFarDistance);
        }
        else
        {
            // 기존 디버그 시각화
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, nearDistance);

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, midDistance);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, farDistance);
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
