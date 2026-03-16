using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class KickAimGuide : MonoBehaviour
{
    public NeonFieldGoalConfig config;
    public Transform landingMarker;
    public LineRenderer lineRenderer;
    public Color guideColor = new Color(0.22f, 0.95f, 1f, 0.9f);

    private Vector3[] points = new Vector3[0];

    private void Awake()
    {
        if (lineRenderer == null)
        {
            lineRenderer = GetComponent<LineRenderer>();
        }

        ApplyGuideStyle();
        Hide();
    }

    public void ApplyConfig(NeonFieldGoalConfig fieldGoalConfig)
    {
        config = fieldGoalConfig;
        EnsurePointBuffer();
        ApplyGuideStyle();
    }

    public void UpdatePreview(Transform origin, Vector3 targetPosition, float power, float aim)
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
        FieldGoalKickMath.SampleTrajectory(origin.position, initialVelocity, config.trajectorySampleStep, points);

        lineRenderer.enabled = true;
        lineRenderer.positionCount = points.Length;
        lineRenderer.SetPositions(points);

        if (landingMarker != null)
        {
            landingMarker.gameObject.SetActive(true);
            landingMarker.position = points[points.Length - 1];
        }
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
        lineRenderer.widthMultiplier = 0.06f;
        lineRenderer.numCapVertices = 6;
        lineRenderer.numCornerVertices = 4;
        lineRenderer.startColor = guideColor;
        lineRenderer.endColor = new Color(guideColor.r, guideColor.g, guideColor.b, 0.25f);
    }
}
