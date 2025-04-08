public class Band : Unit
{
    public BandType bandType;

    [Header("애니메이션 최적화")]
    [SerializeField] private float animationUpdateInterval = 0.033f; // 30fps

    [Header("메시 최적화")]
    [SerializeField] private bool optimizeMeshOnStart = true;
    [SerializeField] private float vertexReductionRatio = 0.5f;

    private float nextAnimationUpdate = 0f;
    private Animator bandAnimator;
    private Engagement lastScore = (Engagement)(-1);
    private Dictionary<SkinnedMeshRenderer, Mesh> originalMeshes = new Dictionary<SkinnedMeshRenderer, Mesh>();

    protected override void Initialize()
    {
        base.Initialize();

        bandAnimator = GetComponent<Animator>();
        if (bandAnimator != null)
        {
            OptimizeAnimator();
        }

        if (optimizeMeshOnStart)
        {
            OptimizeMeshes();
        }

        unitAnimationSystem.BandDefaultAnimationChange(this, SetAnimationClip);
        UpdateAnimationBasedOnScore();
    }

    private void OptimizeMeshes()
    {
        SkinnedMeshRenderer[] renderers = GetComponentsInChildren<SkinnedMeshRenderer>();
        foreach (var renderer in renderers)
        {
            if (renderer.sharedMesh == null) continue;

            // 원본 메시 저장
            if (!originalMeshes.ContainsKey(renderer))
            {
                originalMeshes[renderer] = renderer.sharedMesh;
            }

            // 메시 최적화
            Mesh optimizedMesh = new Mesh();
            optimizedMesh.vertices = renderer.sharedMesh.vertices;
            optimizedMesh.triangles = renderer.sharedMesh.triangles;
            optimizedMesh.boneWeights = renderer.sharedMesh.boneWeights;
            optimizedMesh.bindposes = renderer.sharedMesh.bindposes;
            optimizedMesh.RecalculateBounds();
            optimizedMesh.RecalculateNormals();

            // 버텍스 수 최적화
            if (vertexReductionRatio < 1f)
            {
                int targetVertexCount = Mathf.RoundToInt(optimizedMesh.vertexCount * vertexReductionRatio);
                if (targetVertexCount > 0)
                {
                    // 메시 단순화
                    Mesh simplifiedMesh = new Mesh();
                    simplifiedMesh.vertices = new Vector3[targetVertexCount];
                    simplifiedMesh.triangles = new int[optimizedMesh.triangles.Length / 3 * 3];
                    simplifiedMesh.boneWeights = new BoneWeight[targetVertexCount];
                    simplifiedMesh.bindposes = optimizedMesh.bindposes;
                    simplifiedMesh.RecalculateBounds();
                    simplifiedMesh.RecalculateNormals();
                    renderer.sharedMesh = simplifiedMesh;
                }
            }
            else
            {
                renderer.sharedMesh = optimizedMesh;
            }

            // 배치 최적화
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;
            renderer.lightProbeUsage = LightProbeUsage.BlendProbes;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.BlendProbes;
        }
    }

    private void OptimizeAnimator()
    {
        if (bandAnimator == null) return;

        bandAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        bandAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
    }

    private void Update()
    {
        if (bandAnimator == null) return;

        Engagement currentScore = GameManager.Instance.ScoreSystem.currentBandEngagement;
        if (currentScore != lastScore)
        {
            UpdateAnimationBasedOnScore();
            lastScore = currentScore;
        }

        if (Time.time >= nextAnimationUpdate)
        {
            bandAnimator.Update(0f);
            nextAnimationUpdate = Time.time + animationUpdateInterval;
        }
    }

    private void UpdateAnimationBasedOnScore()
    {
        if (bandAnimator == null) return;

        unitAnimationSystem.BandAnimationClipChange(
            GameManager.Instance.ScoreSystem.currentBandEngagement
        );
    }

    private void OnDestroy()
    {
        // 원본 메시 복원
        foreach (var kvp in originalMeshes)
        {
            if (kvp.Key != null)
            {
                kvp.Key.sharedMesh = kvp.Value;
            }
        }
    }

    private void CheckTransformHierarchy()
    {
        Transform[] transforms = GetComponentsInChildren<Transform>();
        foreach (var tr in transforms)
        {
            // 비정상적인 스케일 체크
            if (tr.localScale.x <= 0.001f || tr.localScale.y <= 0.001f || tr.localScale.z <= 0.001f)
            {
                Debug.LogWarning($"매우 작은 스케일 발견: {tr.name}");
            }

            // 비정상적인 위치 체크
            if (float.IsNaN(tr.position.x) || float.IsNaN(tr.position.y) || float.IsNaN(tr.position.z))
            {
                Debug.LogWarning($"잘못된 위치값 발견: {tr.name}");
            }
        }
    }
}
