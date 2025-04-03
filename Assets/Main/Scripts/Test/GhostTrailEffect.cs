using UnityEngine;

public class GhostTrailEffect : MonoBehaviour
{
    [Header("트레일 설정")]
    [SerializeField]
    private float trailTime = 0.5f; // 트레일 지속 시간

    [SerializeField]
    private float startWidth = 0.5f; // 시작 너비

    [SerializeField]
    private float endWidth = 0.1f; // 끝 너비

    [SerializeField]
    private float minVertexDistance = 0.1f; // 최소 버텍스 간격

    [SerializeField]
    private Color trailColor = Color.white; // 트레일 색상

    private TrailRenderer trailRenderer;
    private Material trailMaterial;

    private void Start()
    {
        InitializeTrail();
    }

    private void InitializeTrail()
    {
        // TrailRenderer 컴포넌트 추가
        trailRenderer = gameObject.AddComponent<TrailRenderer>();

        // 트레일 기본 설정
        trailRenderer.time = trailTime;
        trailRenderer.startWidth = startWidth;
        trailRenderer.endWidth = endWidth;
        trailRenderer.minVertexDistance = minVertexDistance;
        trailRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        // 트레일 머티리얼 설정
        trailMaterial = new Material(Shader.Find("Sprites/Default"));
        trailMaterial.color = trailColor;
        trailMaterial.SetFloat("_Mode", 2); // Transparent 모드
        trailMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        trailMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        trailMaterial.SetInt("_ZWrite", 0);
        trailMaterial.DisableKeyword("_ALPHATEST_ON");
        trailMaterial.EnableKeyword("_ALPHABLEND_ON");
        trailMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        trailMaterial.renderQueue = 3000;
        trailRenderer.material = trailMaterial;

        // 트레일 색상 설정
        trailRenderer.startColor = new Color(trailColor.r, trailColor.g, trailColor.b, 1f);
        trailRenderer.endColor = new Color(trailColor.r, trailColor.g, trailColor.b, 0f);
    }

    // 트레일 색상 변경
    public void SetTrailColor(Color color)
    {
        trailColor = color;
        trailMaterial.color = color;
        trailRenderer.startColor = new Color(color.r, color.g, color.b, 1f);
        trailRenderer.endColor = new Color(color.r, color.g, color.b, 0f);
    }

    // 트레일 너비 변경
    public void SetTrailWidth(float start, float end)
    {
        trailRenderer.startWidth = start;
        trailRenderer.endWidth = end;
    }

    // 트레일 시간 변경
    public void SetTrailTime(float time)
    {
        trailRenderer.time = time;
    }

    // 트레일 활성화/비활성화
    public void SetTrailActive(bool active)
    {
        trailRenderer.enabled = active;
    }
}
