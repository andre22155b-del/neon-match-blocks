using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class KickAimGuide : MonoBehaviour
{
    public NeonFieldGoalConfig config;
    public Transform landingMarker;
    public LineRenderer lineRenderer;
    public Color guideColor = new Color(0.22f, 0.95f, 1f, 0.9f);
    public Color accentGuideColor = new Color(1f, 0.38f, 0.88f, 0.92f);
    public Color landingPulseColor = new Color(1f, 0.88f, 0.42f, 0.96f);

    private Vector3[] points = new Vector3[0];
    private Renderer landingRenderer;
    private int emissionColorId;
    private MaterialPropertyBlock landingPropertyBlock;

    private void Awake()
    {
        if (lineRenderer == null)
        {
            lineRenderer = GetComponent<LineRenderer>();
        }

        if (landingMarker != null)
        {
            landingRenderer = landingMarker.GetComponent<Renderer>();
        }

        emissionColorId = Shader.PropertyToID("_EmissionColor");
        landingPropertyBlock = new MaterialPropertyBlock();
        ApplyGuideStyle();
        Hide();
    }

    public void ApplyConfig(NeonFieldGoalConfig fieldGoalConfig)
    {
        config = fieldGoalConfig;
        EnsurePointBuffer();
        if (landingMarker != null && landingRenderer == null)
        {
            landingRenderer = landingMarker.GetComponent<Renderer>();
        }

        ApplyGuideStyle();
    }

    public void UpdatePreview(Transform origin, Vector3 targetPosition, float power, float aim, Vector3 windAcceleration)
    {
        if (origin == null || lineRenderer == null || config == null)
        {
            Hide();
            return;
        }

        EnsurePointBuffer();

        Vector3 launchDirection = FieldGoalKickMath.ComputeLaunchDirection(origin, targetPosition, config, aim);
        float impulse = FieldGoalKickMath.ComputeImpulse(config, power);
        Vector3 initialVelocity = FieldGoalKickMath.ComputeInitialVelocity(config, launchDirection, impulse);
        FieldGoalKickMath.SampleTrajectory(config, origin.position, initialVelocity, config.trajectorySampleStep, windAcceleration, points);

        lineRenderer.enabled = true;
        lineRenderer.positionCount = points.Length;
        lineRenderer.SetPositions(points);

        if (landingMarker != null)
        {
            landingMarker.gameObject.SetActive(true);
            landingMarker.position = points[points.Length - 1];
            landingMarker.rotation = Quaternion.identity;
        }

        UpdateGuidePulse();
    }

    public void Hide()
    {
        if (lineRenderer != null)
        {
            lineRenderer.positionCount = 0;
            lineRenderer.enabled = false;
        }

        if (landingMarker != null)
        {
            landingMarker.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (lineRenderer == null || !lineRenderer.enabled)
        {
            return;
        }

        UpdateGuidePulse();
    }

    private void EnsurePointBuffer()
    {
        int sampleCount = config != null ? Mathf.Max(8, config.trajectorySampleCount) : 16;
        if (points == null || points.Length != sampleCount)
        {
            points = new Vector3[sampleCount];
        }
    }

    private void ApplyGuideStyle()
    {
        if (lineRenderer == null)
        {
            return;
        }

        lineRenderer.useWorldSpace = true;
        lineRenderer.widthMultiplier = 0.095f;
        lineRenderer.numCapVertices = 10;
        lineRenderer.numCornerVertices = 8;
        lineRenderer.textureMode = LineTextureMode.Stretch;
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;
        lineRenderer.colorGradient = BuildGuideGradient(1f);
        lineRenderer.widthCurve = new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(0.58f, 0.76f),
            new Keyframe(1f, 0.18f));
        lineRenderer.alignment = LineAlignment.View;
        UpdateGuidePulse();
    }

    private void UpdateGuidePulse()
    {
        float pulse = 0.9f + Mathf.Abs(Mathf.Sin(Time.unscaledTime * 4.2f)) * 0.22f;
        if (lineRenderer != null)
        {
            lineRenderer.widthMultiplier = 0.095f * pulse;
        }

        if (landingMarker != null)
        {
            float markerPulse = 0.92f + Mathf.Abs(Mathf.Sin(Time.unscaledTime * 5.6f + 0.3f)) * 0.26f;
            landingMarker.localScale = Vector3.one * (0.28f * markerPulse);
        }

        if (landingRenderer != null && landingPropertyBlock != null)
        {
            Color emission = landingPulseColor * Mathf.LinearToGammaSpace(2.6f * pulse);
            landingRenderer.GetPropertyBlock(landingPropertyBlock);
            landingPropertyBlock.SetColor(emissionColorId, emission);
            landingRenderer.SetPropertyBlock(landingPropertyBlock);
        }
    }

    private Gradient BuildGuideGradient(float pulse)
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(Color.Lerp(guideColor, Color.white, 0.1f) * pulse, 0f),
                new GradientColorKey(Color.Lerp(guideColor, accentGuideColor, 0.42f) * pulse, 0.45f),
                new GradientColorKey(accentGuideColor * pulse, 0.82f),
                new GradientColorKey(landingPulseColor * pulse, 1f)
            },
            new[]
            {
                new GradientAlphaKey(0.92f, 0f),
                new GradientAlphaKey(0.54f, 0.58f),
                new GradientAlphaKey(0.05f, 1f)
            });
        return gradient;
    }
}
